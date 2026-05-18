using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Creature.Editor
{
    /// <summary>
    /// Represents a single editor change (part or color modification) for undo/redo in the creature editor.
    /// </summary>
    public enum CreatureEditorTab
    {
        Body,
        Parts,
        Paint,
        TestDrive,
        Save
    }

    public enum CreatureEditorPreviewAction
    {
        Idle,
        Walk,
        Attack,
        Socialize
    }

    public struct CreatureEditorLiveValidation
    {
        public bool IsValid;
        public int DNACost;
        public CreatureStats Stats;
        public float AnimationImpact;
        public List<string> Errors;

        public string Summary => IsValid
            ? $"Ready | DNA {DNACost} | Animation {AnimationImpact:0.0}"
            : string.Join("; ", Errors ?? new List<string> { "Invalid creature." });
    }

    public class EditorAction
    {
        public enum ActionType { AddPart, RemovePart, MovePart, ScalePart, RotatePart, ChangeColor, ChangeBodyScale, ChangeBodyPlan, ChangePaint }

        public ActionType Type;
        public int PartIndex;
        public List<int> PartIndexes;
        public PartPlacement OldPlacement;
        public PartPlacement NewPlacement;
        public List<PartPlacement> OldPlacements;
        public List<PartPlacement> NewPlacements;
        public CreatureColors OldColors;
        public CreatureColors NewColors;
        public Vector3 OldBodyScale;
        public Vector3 NewBodyScale;
        public float OldFrontPull;
        public float NewFrontPull;
        public BodyPlan OldBodyPlan;
        public BodyPlan NewBodyPlan;
    }

    /// <summary>
    /// Pure C# model of the creature editor, managing DNA, symmetry, selection, and undo/redo stacks.
    /// </summary>
    public class CreatureEditorState
    {
        private CreatureDNA _dna;
        private readonly List<EditorAction> _undoStack = new List<EditorAction>();
        private readonly List<EditorAction> _redoStack = new List<EditorAction>();
        private bool _symmetryEnabled = true;
        private int _selectedPartIndex = -1;
        private CreatureEditorTab _currentTab = CreatureEditorTab.Body;
        private CreatureEditorPreviewAction _previewAction = CreatureEditorPreviewAction.Idle;

        public CreatureDNA DNA => _dna;
        public bool SymmetryEnabled => _symmetryEnabled;
        public SymmetryType SymmetryMode => _dna?.Body?.Symmetry ?? (_symmetryEnabled ? SymmetryType.Bilateral : SymmetryType.None);
        public int SelectedPartIndex => _selectedPartIndex;
        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;
        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
        public CreatureEditorTab CurrentTab => _currentTab;
        public CreatureEditorPreviewAction PreviewAction => _previewAction;

        public CreatureEditorState(CreatureDNA dna = null)
        {
            _dna = dna ?? CreatureDNA.CreateDefault();
            CreatureSkeletonConstraintSolver.Apply(_dna);
        }

        public void SetSymmetry(bool enabled)
        {
            _symmetryEnabled = enabled;
            EnsureBodyPlan();
            _dna.Body.Symmetry = enabled ? SymmetryType.Bilateral : SymmetryType.None;
        }

        public void SetSymmetryMode(SymmetryType mode)
        {
            EnsureBodyPlan();
            _dna.Body.Symmetry = mode;
            _symmetryEnabled = mode != SymmetryType.None;
        }

        public void SetCurrentTab(CreatureEditorTab tab)
        {
            _currentTab = tab;
        }

        public void SetPreviewAction(CreatureEditorPreviewAction action)
        {
            _previewAction = action;
        }

        public void SelectPart(int index)
        {
            if (index >= 0 && index < _dna.Parts.Count)
                _selectedPartIndex = index;
            else
                _selectedPartIndex = -1;
        }

        public void DeselectPart()
        {
            _selectedPartIndex = -1;
        }

        public int AddPart(string partId, int attachBone, Vector3 offset, float scale = 1f, Vector3 rotation = default)
        {
            EnsureBodyPlan();
            int spineSegments = Mathf.Clamp(_dna.Body.SpineSegments, 1, 8);
            attachBone = Mathf.Clamp(attachBone, 0, spineSegments - 1);

            var placement = new PartPlacement
            {
                PartId = partId,
                AttachBone = attachBone,
                Offset = offset,
                Scale = scale,
                Rotation = rotation
            };

            var placements = BuildSymmetryPlacements(placement);
            int index = _dna.Parts.Count;
            for (int i = 0; i < placements.Count; i++)
                _dna.Parts.Add(placements[i]);

            PushUndo(new EditorAction
            {
                Type = EditorAction.ActionType.AddPart,
                PartIndex = index,
                PartIndexes = BuildContiguousIndexes(index, placements.Count),
                NewPlacement = ClonePlacement(placement),
                NewPlacements = ClonePlacements(placements)
            });

            CreatureSkeletonConstraintSolver.Apply(_dna);
            return index;
        }

        public bool RemovePart(int index)
        {
            if (index < 0 || index >= _dna.Parts.Count) return false;

            var affectedIndexes = _symmetryEnabled ? FindSymmetryPartIndexes(index) : new List<int> { index };
            affectedIndexes.Sort();
            var removed = ClonePlacementsAt(affectedIndexes);
            PushUndo(new EditorAction
            {
                Type = EditorAction.ActionType.RemovePart,
                PartIndex = index,
                PartIndexes = new List<int>(affectedIndexes),
                OldPlacement = ClonePlacement(_dna.Parts[index]),
                OldPlacements = removed
            });

            for (int i = affectedIndexes.Count - 1; i >= 0; i--)
            {
                int removeIndex = affectedIndexes[i];
                if (removeIndex >= 0 && removeIndex < _dna.Parts.Count)
                    _dna.Parts.RemoveAt(removeIndex);
            }

            if (affectedIndexes.Contains(_selectedPartIndex)) _selectedPartIndex = -1;
            CreatureSkeletonConstraintSolver.Apply(_dna);
            return true;
        }

        public bool MovePart(int index, Vector3 newOffset)
        {
            if (index < 0 || index >= _dna.Parts.Count) return false;

            var part = _dna.Parts[index];
            var affectedIndexes = _symmetryEnabled ? FindSymmetryPartIndexes(index) : new List<int> { index };
            var oldPlacements = ClonePlacementsAt(affectedIndexes);

            // Find mirror BEFORE updating offset — FindMirrorPart matches on current offset values.
            int mirrorIdx = _symmetryEnabled ? FindMirrorPart(index) : -1;

            part.Offset = newOffset;

            PushUndo(new EditorAction
            {
                Type = EditorAction.ActionType.MovePart,
                PartIndex = index,
                PartIndexes = new List<int>(affectedIndexes),
                OldPlacement = oldPlacements.Count > 0 ? oldPlacements[0] : ClonePlacement(part),
                NewPlacement = ClonePlacement(part)
            });

            if (mirrorIdx >= 0 && mirrorIdx != index)
                _dna.Parts[mirrorIdx].Offset = new Vector3(-newOffset.x, newOffset.y, newOffset.z);
            ApplyRadialSiblingMoveIfNeeded(index, affectedIndexes, newOffset, part.Rotation);
            var action = _undoStack[_undoStack.Count - 1];
            action.OldPlacements = oldPlacements;
            action.NewPlacements = ClonePlacementsAt(affectedIndexes);

            CreatureSkeletonConstraintSolver.Apply(_dna);
            return true;
        }

        public bool MovePart(int index, int attachBone, Vector3 newOffset, Vector3 newRotation)
        {
            if (index < 0 || index >= _dna.Parts.Count) return false;

            var part = _dna.Parts[index];
            var affectedIndexes = _symmetryEnabled ? FindSymmetryPartIndexes(index) : new List<int> { index };
            var oldPlacements = ClonePlacementsAt(affectedIndexes);
            int mirrorIdx = _symmetryEnabled ? FindMirrorPart(index) : -1;
            int spineSegments = _dna?.Body != null ? Mathf.Clamp(_dna.Body.SpineSegments, 1, 8) : 1;
            int safeAttachBone = Mathf.Clamp(attachBone, 0, spineSegments - 1);

            part.AttachBone = safeAttachBone;
            part.Offset = newOffset;
            part.Rotation = newRotation;

            PushUndo(new EditorAction
            {
                Type = EditorAction.ActionType.MovePart,
                PartIndex = index,
                PartIndexes = new List<int>(affectedIndexes),
                OldPlacement = oldPlacements.Count > 0 ? oldPlacements[0] : ClonePlacement(part),
                NewPlacement = ClonePlacement(part)
            });

            if (mirrorIdx >= 0 && mirrorIdx != index)
            {
                _dna.Parts[mirrorIdx].AttachBone = safeAttachBone;
                _dna.Parts[mirrorIdx].Offset = new Vector3(-newOffset.x, newOffset.y, newOffset.z);
                _dna.Parts[mirrorIdx].Rotation = new Vector3(newRotation.x, -newRotation.y, -newRotation.z);
            }
            ApplyRadialSiblingMoveIfNeeded(index, affectedIndexes, newOffset, newRotation, safeAttachBone);
            var action = _undoStack[_undoStack.Count - 1];
            action.OldPlacements = oldPlacements;
            action.NewPlacements = ClonePlacementsAt(affectedIndexes);

            CreatureSkeletonConstraintSolver.Apply(_dna);
            return true;
        }

        public bool ScalePart(int index, float newScale)
        {
            if (index < 0 || index >= _dna.Parts.Count) return false;
            newScale = Mathf.Max(0.1f, Mathf.Min(3f, newScale));

            var part = _dna.Parts[index];
            var affectedIndexes = _symmetryEnabled ? FindSymmetryPartIndexes(index) : new List<int> { index };
            var oldPlacements = ClonePlacementsAt(affectedIndexes);
            part.Scale = newScale;

            PushUndo(new EditorAction
            {
                Type = EditorAction.ActionType.ScalePart,
                PartIndex = index,
                PartIndexes = new List<int>(affectedIndexes),
                OldPlacement = oldPlacements.Count > 0 ? oldPlacements[0] : ClonePlacement(part),
                NewPlacement = ClonePlacement(part)
            });

            if (_symmetryEnabled)
            {
                for (int i = 0; i < affectedIndexes.Count; i++)
                {
                    int partIndex = affectedIndexes[i];
                    if (partIndex >= 0 && partIndex < _dna.Parts.Count)
                        _dna.Parts[partIndex].Scale = newScale;
                }
            }
            var action = _undoStack[_undoStack.Count - 1];
            action.OldPlacements = oldPlacements;
            action.NewPlacements = ClonePlacementsAt(affectedIndexes);

            CreatureSkeletonConstraintSolver.Apply(_dna);
            return true;
        }

        public bool RotatePart(int index, Vector3 newRotation)
        {
            if (index < 0 || index >= _dna.Parts.Count) return false;

            var part = _dna.Parts[index];
            var affectedIndexes = _symmetryEnabled ? FindSymmetryPartIndexes(index) : new List<int> { index };
            var oldPlacements = ClonePlacementsAt(affectedIndexes);
            part.Rotation = newRotation;

            PushUndo(new EditorAction
            {
                Type = EditorAction.ActionType.RotatePart,
                PartIndex = index,
                PartIndexes = new List<int>(affectedIndexes),
                OldPlacement = oldPlacements.Count > 0 ? oldPlacements[0] : ClonePlacement(part),
                NewPlacement = ClonePlacement(part)
            });

            if (_symmetryEnabled)
            {
                int mirrorIdx = FindMirrorPart(index);
                if (mirrorIdx >= 0 && mirrorIdx != index)
                    _dna.Parts[mirrorIdx].Rotation = new Vector3(newRotation.x, -newRotation.y, -newRotation.z);
            }
            ApplyRadialSiblingMoveIfNeeded(index, affectedIndexes, part.Offset, newRotation);
            var action = _undoStack[_undoStack.Count - 1];
            action.OldPlacements = oldPlacements;
            action.NewPlacements = ClonePlacementsAt(affectedIndexes);

            CreatureSkeletonConstraintSolver.Apply(_dna);
            return true;
        }

        public void SetColors(Color primary, Color secondary, Color accent)
        {
            EnsureColors();
            var oldColors = CloneColors(_dna.Colors);

            _dna.Colors.PrimaryColor = primary;
            _dna.Colors.SecondaryColor = secondary;
            _dna.Colors.AccentColor = accent;

            PushUndo(new EditorAction
            {
                Type = EditorAction.ActionType.ChangeColor,
                OldColors = oldColors,
                NewColors = CloneColors(_dna.Colors)
            });

            CreatureSkeletonConstraintSolver.Apply(_dna);
        }

        public void SetPaint(
            Color primary,
            Color secondary,
            Color accent,
            CreaturePaintPattern pattern,
            CreatureMaterialFinish material,
            float patternStrength)
        {
            EnsureColors();
            var oldColors = CloneColors(_dna.Colors);

            _dna.Colors.PrimaryColor = primary;
            _dna.Colors.SecondaryColor = secondary;
            _dna.Colors.AccentColor = accent;
            _dna.Colors.Pattern = pattern;
            _dna.Colors.Material = material;
            _dna.Colors.PatternStrength = Mathf.Clamp01(patternStrength);

            PushUndo(new EditorAction
            {
                Type = EditorAction.ActionType.ChangePaint,
                OldColors = oldColors,
                NewColors = CloneColors(_dna.Colors)
            });

            CreatureSkeletonConstraintSolver.Apply(_dna);
        }

        public void SetPartPaintOverride(
            string partId,
            Color primary,
            Color secondary,
            Color accent,
            bool overridePrimary,
            bool overrideSecondary,
            bool overrideAccent,
            bool overrideMaterial,
            CreatureMaterialFinish material)
        {
            if (string.IsNullOrWhiteSpace(partId))
                return;

            EnsureColors();
            var oldColors = CloneColors(_dna.Colors);
            _dna.Colors.PartOverrides ??= new List<CreaturePartPaintOverride>();

            var existing = _dna.Colors.PartOverrides.Find(o => o != null && o.PartId == partId);
            if (existing == null)
            {
                existing = new CreaturePartPaintOverride { PartId = partId };
                _dna.Colors.PartOverrides.Add(existing);
            }

            existing.PrimaryColor = primary;
            existing.SecondaryColor = secondary;
            existing.AccentColor = accent;
            existing.OverridePrimary = overridePrimary;
            existing.OverrideSecondary = overrideSecondary;
            existing.OverrideAccent = overrideAccent;
            existing.OverrideMaterial = overrideMaterial;
            existing.Material = material;

            PushUndo(new EditorAction
            {
                Type = EditorAction.ActionType.ChangePaint,
                OldColors = oldColors,
                NewColors = CloneColors(_dna.Colors)
            });
        }

        public void SetBodyShape(Vector3 bodyScale, float frontPull)
        {
            if (_dna?.Body == null)
                _dna.Body = new BodyPlan();

            Vector3 clampedScale = new Vector3(
                Mathf.Clamp(bodyScale.x, 0.5f, 2.5f),
                Mathf.Clamp(bodyScale.y, 0.5f, 2.5f),
                Mathf.Clamp(bodyScale.z, 0.5f, 2.5f));
            float clampedFrontPull = Mathf.Clamp(frontPull, -0.3f, 0.8f);

            Vector3 oldScale = _dna.Body.BodyScale;
            float oldFrontPull = _dna.Body.FrontPull;

            if ((oldScale - clampedScale).sqrMagnitude < 0.00001f && Mathf.Abs(oldFrontPull - clampedFrontPull) < 0.0001f)
                return;

            _dna.Body.BodyScale = clampedScale;
            _dna.Body.FrontPull = clampedFrontPull;

            PushUndo(new EditorAction
            {
                Type = EditorAction.ActionType.ChangeBodyScale,
                OldBodyScale = oldScale,
                NewBodyScale = clampedScale,
                OldFrontPull = oldFrontPull,
                NewFrontPull = clampedFrontPull
            });

            CreatureSkeletonConstraintSolver.Apply(_dna);
        }

        public bool SetSpineSegmentCount(int count)
        {
            EnsureBodyPlan();
            int clamped = Mathf.Clamp(count, 1, 8);
            if (_dna.Body.SpineSegments == clamped && _dna.Body.Spine != null && _dna.Body.Spine.Count == clamped)
                return false;

            return ApplyBodyPlanChange(body => body.SpineSegments = clamped);
        }

        public bool MoveSpineSegment(int index, Vector3 offset)
        {
            EnsureBodyPlan();
            CreatureSkeletonConstraintSolver.Apply(_dna);
            if (index < 0 || index >= _dna.Body.Spine.Count)
                return false;

            return ApplyBodyPlanChange(body => body.Spine[index].Offset = offset);
        }

        public bool ScaleSpineSegment(int index, Vector2 scale)
        {
            EnsureBodyPlan();
            CreatureSkeletonConstraintSolver.Apply(_dna);
            if (index < 0 || index >= _dna.Body.Spine.Count)
                return false;

            return ApplyBodyPlanChange(body => body.Spine[index].Scale = scale);
        }

        public bool CurveSpineSegment(int index, float curve)
        {
            EnsureBodyPlan();
            CreatureSkeletonConstraintSolver.Apply(_dna);
            if (index < 0 || index >= _dna.Body.Spine.Count)
                return false;

            return ApplyBodyPlanChange(body => body.Spine[index].Curve = curve);
        }

        public bool ValidatePlacement(
            string partId,
            int attachBone,
            Vector3 offset,
            Dictionary<string, CreaturePartDef> partCatalog,
            out string reason)
        {
            EnsureBodyPlan();
            reason = null;
            if (partCatalog == null || !partCatalog.TryGetValue(partId, out var def))
            {
                reason = "Choose a known part first.";
                return false;
            }

            if (CreaturePartCatalog.TryGetPlacementRuleError(def, attachBone, _dna.Body.SpineSegments, out reason))
                return false;

            float maxOffset = Mathf.Max(1.25f, _dna.Body.BodyScale.magnitude * 1.1f);
            if (offset.magnitude > maxOffset)
            {
                reason = $"{def.DisplayName} is too far from the body surface.";
                return false;
            }

            reason = "valid";
            return true;
        }

        public CreatureEditorLiveValidation GetLiveValidation(Dictionary<string, CreaturePartDef> partCatalog)
        {
            partCatalog ??= CreaturePartCatalog.GetDefaultCatalog();
            EnsureBodyPlan();
            EnsureColors();
            CreatureSkeletonConstraintSolver.Apply(_dna);

            var errors = new List<string>();
            var counts = new Dictionary<string, int>();
            int dnaCost = 0;

            foreach (var placement in _dna.Parts)
            {
                if (placement == null || string.IsNullOrWhiteSpace(placement.PartId))
                {
                    errors.Add("A placed part is missing its id.");
                    continue;
                }

                if (!partCatalog.TryGetValue(placement.PartId, out var def) || def == null)
                {
                    errors.Add($"{placement.PartId} is not in the creature catalog.");
                    continue;
                }

                counts.TryGetValue(placement.PartId, out int count);
                counts[placement.PartId] = count + 1;
                if (def.MaxCount > 0 && counts[placement.PartId] > def.MaxCount)
                    errors.Add($"{def.DisplayName} exceeds max count {def.MaxCount}.");

                if (CreaturePartCatalog.TryGetPlacementRuleError(def, placement.AttachBone, _dna.Body.SpineSegments, out string placementReason))
                    errors.Add(placementReason);

                dnaCost += def.DNACost;
            }

            if (_dna.Body.SpineSegments < 1 || _dna.Body.SpineSegments > 8)
                errors.Add("Spine segment count must stay between 1 and 8.");

            return new CreatureEditorLiveValidation
            {
                IsValid = errors.Count == 0,
                DNACost = dnaCost,
                Stats = StatCalculator.Calculate(_dna, partCatalog),
                AnimationImpact = CalculateAnimationImpact(partCatalog),
                Errors = errors
            };
        }

        public bool Undo()
        {
            if (_undoStack.Count == 0) return false;
            var action = _undoStack[_undoStack.Count - 1];
            _undoStack.RemoveAt(_undoStack.Count - 1);

            switch (action.Type)
            {
                case EditorAction.ActionType.AddPart:
                    RemoveActionParts(action);
                    break;
                case EditorAction.ActionType.RemovePart:
                    InsertActionParts(action.PartIndexes, action.OldPlacements, action.PartIndex, action.OldPlacement);
                    break;
                case EditorAction.ActionType.MovePart:
                case EditorAction.ActionType.ScalePart:
                case EditorAction.ActionType.RotatePart:
                    ApplyActionPlacements(action.PartIndexes, action.OldPlacements, action.PartIndex, action.OldPlacement);
                    break;
                case EditorAction.ActionType.ChangeColor:
                case EditorAction.ActionType.ChangePaint:
                    _dna.Colors = CloneColors(action.OldColors);
                    break;
                case EditorAction.ActionType.ChangeBodyPlan:
                    _dna.Body = CloneBodyPlan(action.OldBodyPlan);
                    break;
                case EditorAction.ActionType.ChangeBodyScale:
                    break;
            }

            if (action.Type == EditorAction.ActionType.ChangeBodyScale)
            {
                if (_dna?.Body == null)
                    _dna.Body = new BodyPlan();
                _dna.Body.BodyScale = action.OldBodyScale;
                _dna.Body.FrontPull = action.OldFrontPull;
            }

            CreatureSkeletonConstraintSolver.Apply(_dna);
            _redoStack.Add(action);
            return true;
        }

        public bool Redo()
        {
            if (_redoStack.Count == 0) return false;
            var action = _redoStack[_redoStack.Count - 1];
            _redoStack.RemoveAt(_redoStack.Count - 1);

            switch (action.Type)
            {
                case EditorAction.ActionType.AddPart:
                    InsertActionParts(action.PartIndexes, action.NewPlacements, action.PartIndex, action.NewPlacement);
                    break;
                case EditorAction.ActionType.RemovePart:
                    RemoveActionParts(action);
                    break;
                case EditorAction.ActionType.MovePart:
                case EditorAction.ActionType.ScalePart:
                case EditorAction.ActionType.RotatePart:
                    ApplyActionPlacements(action.PartIndexes, action.NewPlacements, action.PartIndex, action.NewPlacement);
                    break;
                case EditorAction.ActionType.ChangeColor:
                case EditorAction.ActionType.ChangePaint:
                    _dna.Colors = CloneColors(action.NewColors);
                    break;
                case EditorAction.ActionType.ChangeBodyPlan:
                    _dna.Body = CloneBodyPlan(action.NewBodyPlan);
                    break;
                case EditorAction.ActionType.ChangeBodyScale:
                    break;
            }

            if (action.Type == EditorAction.ActionType.ChangeBodyScale)
            {
                if (_dna?.Body == null)
                    _dna.Body = new BodyPlan();
                _dna.Body.BodyScale = action.NewBodyScale;
                _dna.Body.FrontPull = action.NewFrontPull;
            }

            CreatureSkeletonConstraintSolver.Apply(_dna);
            _undoStack.Add(action);
            return true;
        }

        public int GetDNACost(Dictionary<string, CreaturePartDef> partCatalog)
        {
            int total = 0;
            if (partCatalog == null || _dna?.Parts == null)
                return total;

            foreach (var part in _dna.Parts)
            {
                if (part != null && partCatalog.TryGetValue(part.PartId, out var def))
                    total += def.DNACost;
            }
            return total;
        }

        private bool ApplyBodyPlanChange(Action<BodyPlan> mutate)
        {
            EnsureBodyPlan();
            var oldBody = CloneBodyPlan(_dna.Body);
            mutate?.Invoke(_dna.Body);
            CreatureSkeletonConstraintSolver.Apply(_dna);
            var newBody = CloneBodyPlan(_dna.Body);

            PushUndo(new EditorAction
            {
                Type = EditorAction.ActionType.ChangeBodyPlan,
                OldBodyPlan = oldBody,
                NewBodyPlan = newBody
            });
            return true;
        }

        private void EnsureBodyPlan()
        {
            _dna ??= CreatureDNA.CreateDefault();
            _dna.Parts ??= new List<PartPlacement>();
            _dna.Body ??= new BodyPlan();
            _dna.Body.Spine ??= new List<SpineSegment>();
            CreatureSkeletonConstraintSolver.Apply(_dna);
        }

        private void EnsureColors()
        {
            _dna ??= CreatureDNA.CreateDefault();
            _dna.Colors ??= new CreatureColors();
            _dna.Colors.PartOverrides ??= new List<CreaturePartPaintOverride>();
        }

        private float CalculateAnimationImpact(Dictionary<string, CreaturePartDef> partCatalog)
        {
            var verbs = new HashSet<CreatureVerb>();
            if (_dna?.Parts != null && partCatalog != null)
            {
                for (int i = 0; i < _dna.Parts.Count; i++)
                {
                    var part = _dna.Parts[i];
                    if (part == null || !partCatalog.TryGetValue(part.PartId, out var def) || def?.Verbs == null)
                        continue;

                    for (int v = 0; v < def.Verbs.Length; v++)
                        verbs.Add(def.Verbs[v]);
                }
            }

            int spineSegments = _dna?.Body != null ? Mathf.Clamp(_dna.Body.SpineSegments, 1, 8) : 1;
            return Mathf.Clamp((verbs.Count * 0.45f) + (spineSegments * 0.18f), 0.2f, 8f);
        }

        private List<PartPlacement> BuildSymmetryPlacements(PartPlacement source)
        {
            var placements = new List<PartPlacement> { ClonePlacement(source) };
            if (!_symmetryEnabled || SymmetryMode == SymmetryType.None)
                return placements;

            if (SymmetryMode == SymmetryType.Radial)
            {
                for (int i = 1; i < 4; i++)
                    placements.Add(RotatePlacementAroundBody(source, i * 90f));
                return placements;
            }

            var mirrored = ClonePlacement(source);
            mirrored.Offset = new Vector3(-source.Offset.x, source.Offset.y, source.Offset.z);
            mirrored.Rotation = new Vector3(source.Rotation.x, -source.Rotation.y, -source.Rotation.z);
            placements.Add(mirrored);
            return placements;
        }

        private void ApplyRadialSiblingMoveIfNeeded(
            int sourceIndex,
            List<int> affectedIndexes,
            Vector3 sourceOffset,
            Vector3 sourceRotation,
            int? attachBoneOverride = null)
        {
            if (!_symmetryEnabled || SymmetryMode != SymmetryType.Radial || affectedIndexes == null || affectedIndexes.Count <= 1)
                return;

            affectedIndexes.Sort((a, b) => GetPlanarAngle(_dna.Parts[a].Offset).CompareTo(GetPlanarAngle(_dna.Parts[b].Offset)));
            int sourceSlot = Mathf.Max(0, affectedIndexes.IndexOf(sourceIndex));
            for (int i = 0; i < affectedIndexes.Count; i++)
            {
                int partIndex = affectedIndexes[i];
                if (partIndex < 0 || partIndex >= _dna.Parts.Count)
                    continue;

                float angle = (i - sourceSlot) * 360f / Mathf.Max(1, affectedIndexes.Count);
                var rotated = RotatePlacementAroundBody(new PartPlacement
                {
                    PartId = _dna.Parts[sourceIndex].PartId,
                    AttachBone = attachBoneOverride ?? _dna.Parts[sourceIndex].AttachBone,
                    Offset = sourceOffset,
                    Rotation = sourceRotation,
                    Scale = _dna.Parts[sourceIndex].Scale
                }, angle);
                _dna.Parts[partIndex].AttachBone = rotated.AttachBone;
                _dna.Parts[partIndex].Offset = rotated.Offset;
                _dna.Parts[partIndex].Rotation = rotated.Rotation;
                _dna.Parts[partIndex].Scale = rotated.Scale;
            }
        }

        private static PartPlacement RotatePlacementAroundBody(PartPlacement source, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            Vector3 offset = source.Offset;
            return new PartPlacement
            {
                PartId = source.PartId,
                AttachBone = source.AttachBone,
                Offset = new Vector3(
                    offset.x * cos - offset.z * sin,
                    offset.y,
                    offset.x * sin + offset.z * cos),
                Rotation = new Vector3(source.Rotation.x, source.Rotation.y + degrees, source.Rotation.z),
                Scale = source.Scale
            };
        }

        private static float GetPlanarAngle(Vector3 offset)
        {
            return Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
        }

        private List<int> FindSymmetryPartIndexes(int index)
        {
            var indexes = new List<int>();
            if (index < 0 || index >= _dna.Parts.Count)
                return indexes;

            indexes.Add(index);
            if (!_symmetryEnabled || SymmetryMode == SymmetryType.None)
                return indexes;

            if (SymmetryMode == SymmetryType.Bilateral)
            {
                int mirror = FindMirrorPart(index);
                if (mirror >= 0 && !indexes.Contains(mirror))
                    indexes.Add(mirror);
                return indexes;
            }

            var part = _dna.Parts[index];
            float sourceRadius = new Vector2(part.Offset.x, part.Offset.z).magnitude;
            for (int i = 0; i < _dna.Parts.Count; i++)
            {
                if (i == index)
                    continue;
                var other = _dna.Parts[i];
                if (other == null || other.PartId != part.PartId || other.AttachBone != part.AttachBone)
                    continue;
                if (Mathf.Abs(other.Offset.y - part.Offset.y) > 0.04f)
                    continue;
                float otherRadius = new Vector2(other.Offset.x, other.Offset.z).magnitude;
                if (Mathf.Abs(otherRadius - sourceRadius) <= 0.08f)
                    indexes.Add(i);
            }
            return indexes;
        }

        private static List<int> BuildContiguousIndexes(int startIndex, int count)
        {
            var indexes = new List<int>();
            for (int i = 0; i < count; i++)
                indexes.Add(startIndex + i);
            return indexes;
        }

        private List<PartPlacement> ClonePlacementsAt(List<int> indexes)
        {
            var placements = new List<PartPlacement>();
            if (indexes == null)
                return placements;

            for (int i = 0; i < indexes.Count; i++)
            {
                int partIndex = indexes[i];
                if (partIndex >= 0 && partIndex < _dna.Parts.Count)
                    placements.Add(ClonePlacement(_dna.Parts[partIndex]));
            }
            return placements;
        }

        private List<PartPlacement> ClonePlacements(List<PartPlacement> placements)
        {
            var cloned = new List<PartPlacement>();
            if (placements == null)
                return cloned;

            for (int i = 0; i < placements.Count; i++)
                cloned.Add(ClonePlacement(placements[i]));
            return cloned;
        }

        private void RemoveActionParts(EditorAction action)
        {
            if (action.PartIndexes != null && action.PartIndexes.Count > 0)
            {
                var indexes = new List<int>(action.PartIndexes);
                indexes.Sort();
                for (int i = indexes.Count - 1; i >= 0; i--)
                {
                    int partIndex = indexes[i];
                    if (partIndex >= 0 && partIndex < _dna.Parts.Count)
                        _dna.Parts.RemoveAt(partIndex);
                }
                return;
            }

            if (action.PartIndex < _dna.Parts.Count)
                _dna.Parts.RemoveAt(action.PartIndex);
        }

        private void InsertActionParts(List<int> indexes, List<PartPlacement> placements, int fallbackIndex, PartPlacement fallbackPlacement)
        {
            if (indexes != null && placements != null && indexes.Count == placements.Count)
            {
                for (int i = 0; i < indexes.Count; i++)
                {
                    int insertIndex = Mathf.Clamp(indexes[i], 0, _dna.Parts.Count);
                    _dna.Parts.Insert(insertIndex, ClonePlacement(placements[i]));
                }
                return;
            }

            if (fallbackPlacement != null)
                _dna.Parts.Insert(Mathf.Clamp(fallbackIndex, 0, _dna.Parts.Count), ClonePlacement(fallbackPlacement));
        }

        private void ApplyActionPlacements(List<int> indexes, List<PartPlacement> placements, int fallbackIndex, PartPlacement fallbackPlacement)
        {
            if (indexes != null && placements != null && indexes.Count == placements.Count)
            {
                for (int i = 0; i < indexes.Count; i++)
                {
                    int partIndex = indexes[i];
                    if (partIndex >= 0 && partIndex < _dna.Parts.Count)
                        _dna.Parts[partIndex] = ClonePlacement(placements[i]);
                }
                return;
            }

            if (fallbackIndex < _dna.Parts.Count && fallbackPlacement != null)
                _dna.Parts[fallbackIndex] = ClonePlacement(fallbackPlacement);
        }

        private int FindMirrorPart(int index)
        {
            if (index < 0 || index >= _dna.Parts.Count) return -1;
            var part = _dna.Parts[index];

            for (int i = 0; i < _dna.Parts.Count; i++)
            {
                if (i == index) continue;
                var other = _dna.Parts[i];
                if (other.PartId != part.PartId) continue;
                if (other.AttachBone != part.AttachBone) continue;
                if (Mathf.Abs(other.Offset.x + part.Offset.x) < 0.01f
                    && Mathf.Abs(other.Offset.y - part.Offset.y) < 0.01f
                    && Mathf.Abs(other.Offset.z - part.Offset.z) < 0.01f)
                {
                    return i;
                }
            }
            return -1;
        }

        private PartPlacement ClonePlacement(PartPlacement p)
        {
            if (p == null)
                return null;

            return new PartPlacement
            {
                PartId = p.PartId,
                AttachBone = p.AttachBone,
                Offset = p.Offset,
                Rotation = p.Rotation,
                Scale = p.Scale
            };
        }

        private BodyPlan CloneBodyPlan(BodyPlan body)
        {
            if (body == null)
                return new BodyPlan();

            var clone = new BodyPlan
            {
                SpineSegments = body.SpineSegments,
                Symmetry = body.Symmetry,
                BodyScale = body.BodyScale,
                FrontPull = body.FrontPull,
                Spine = new List<SpineSegment>()
            };

            if (body.Spine != null)
            {
                for (int i = 0; i < body.Spine.Count; i++)
                {
                    var segment = body.Spine[i];
                    clone.Spine.Add(segment == null
                        ? new SpineSegment()
                        : new SpineSegment
                        {
                            Offset = segment.Offset,
                            Scale = segment.Scale,
                            Curve = segment.Curve
                        });
                }
            }

            return clone;
        }

        private CreatureColors CloneColors(CreatureColors colors)
        {
            if (colors == null)
                return new CreatureColors();

            var clone = new CreatureColors
            {
                PrimaryColor = colors.PrimaryColor,
                SecondaryColor = colors.SecondaryColor,
                AccentColor = colors.AccentColor,
                Pattern = colors.Pattern,
                Material = colors.Material,
                PatternStrength = colors.PatternStrength,
                PartOverrides = new List<CreaturePartPaintOverride>()
            };

            if (colors.PartOverrides != null)
            {
                for (int i = 0; i < colors.PartOverrides.Count; i++)
                {
                    var partOverride = colors.PartOverrides[i];
                    if (partOverride == null)
                        continue;

                    clone.PartOverrides.Add(new CreaturePartPaintOverride
                    {
                        PartId = partOverride.PartId,
                        PrimaryColor = partOverride.PrimaryColor,
                        SecondaryColor = partOverride.SecondaryColor,
                        AccentColor = partOverride.AccentColor,
                        OverridePrimary = partOverride.OverridePrimary,
                        OverrideSecondary = partOverride.OverrideSecondary,
                        OverrideAccent = partOverride.OverrideAccent,
                        OverrideMaterial = partOverride.OverrideMaterial,
                        Material = partOverride.Material
                    });
                }
            }

            return clone;
        }

        private void PushUndo(EditorAction action)
        {
            _undoStack.Add(action);
            _redoStack.Clear();
        }
    }
}
