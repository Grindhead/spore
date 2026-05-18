using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Creature.Editor
{
    /// <summary>
    /// Represents a single editor change (part or color modification) for undo/redo in the creature editor.
    /// </summary>
    public class EditorAction
    {
        public enum ActionType { AddPart, RemovePart, MovePart, ScalePart, RotatePart, ChangeColor, ChangeBodyScale }

        public ActionType Type;
        public int PartIndex;
        public PartPlacement OldPlacement;
        public PartPlacement NewPlacement;
        public CreatureColors OldColors;
        public CreatureColors NewColors;
        public Vector3 OldBodyScale;
        public Vector3 NewBodyScale;
        public float OldFrontPull;
        public float NewFrontPull;
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

        public CreatureDNA DNA => _dna;
        public bool SymmetryEnabled => _symmetryEnabled;
        public int SelectedPartIndex => _selectedPartIndex;
        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;
        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public CreatureEditorState(CreatureDNA dna = null)
        {
            _dna = dna ?? CreatureDNA.CreateDefault();
            CreatureSkeletonConstraintSolver.Apply(_dna);
        }

        public void SetSymmetry(bool enabled)
        {
            _symmetryEnabled = enabled;
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
            var placement = new PartPlacement
            {
                PartId = partId,
                AttachBone = attachBone,
                Offset = offset,
                Scale = scale,
                Rotation = rotation
            };

            _dna.Parts.Add(placement);
            int index = _dna.Parts.Count - 1;

            PushUndo(new EditorAction
            {
                Type = EditorAction.ActionType.AddPart,
                PartIndex = index,
                NewPlacement = ClonePlacement(placement)
            });

            if (_symmetryEnabled)
            {
                var mirrored = ClonePlacement(placement);
                mirrored.Offset = new Vector3(-offset.x, offset.y, offset.z);
                mirrored.Rotation = new Vector3(rotation.x, -rotation.y, -rotation.z);
                _dna.Parts.Add(mirrored);
            }

            CreatureSkeletonConstraintSolver.Apply(_dna);
            return index;
        }

        public bool RemovePart(int index)
        {
            if (index < 0 || index >= _dna.Parts.Count) return false;

            var removed = ClonePlacement(_dna.Parts[index]);
            PushUndo(new EditorAction
            {
                Type = EditorAction.ActionType.RemovePart,
                PartIndex = index,
                OldPlacement = removed
            });

            if (_symmetryEnabled)
            {
                int mirrorIdx = FindMirrorPart(index);
                if (mirrorIdx >= 0 && mirrorIdx != index)
                {
                    if (mirrorIdx > index)
                    {
                        _dna.Parts.RemoveAt(mirrorIdx);
                        _dna.Parts.RemoveAt(index);
                    }
                    else
                    {
                        _dna.Parts.RemoveAt(index);
                        _dna.Parts.RemoveAt(mirrorIdx);
                    }
                    if (_selectedPartIndex == index || _selectedPartIndex == mirrorIdx)
                        _selectedPartIndex = -1;
                    CreatureSkeletonConstraintSolver.Apply(_dna);
                    return true;
                }
            }

            _dna.Parts.RemoveAt(index);
            if (_selectedPartIndex == index) _selectedPartIndex = -1;
            CreatureSkeletonConstraintSolver.Apply(_dna);
            return true;
        }

        public bool MovePart(int index, Vector3 newOffset)
        {
            if (index < 0 || index >= _dna.Parts.Count) return false;

            var part = _dna.Parts[index];
            var oldPlacement = ClonePlacement(part);

            // Find mirror BEFORE updating offset — FindMirrorPart matches on current offset values.
            int mirrorIdx = _symmetryEnabled ? FindMirrorPart(index) : -1;

            part.Offset = newOffset;

            PushUndo(new EditorAction
            {
                Type = EditorAction.ActionType.MovePart,
                PartIndex = index,
                OldPlacement = oldPlacement,
                NewPlacement = ClonePlacement(part)
            });

            if (mirrorIdx >= 0 && mirrorIdx != index)
                _dna.Parts[mirrorIdx].Offset = new Vector3(-newOffset.x, newOffset.y, newOffset.z);

            CreatureSkeletonConstraintSolver.Apply(_dna);
            return true;
        }

        public bool MovePart(int index, int attachBone, Vector3 newOffset, Vector3 newRotation)
        {
            if (index < 0 || index >= _dna.Parts.Count) return false;

            var part = _dna.Parts[index];
            var oldPlacement = ClonePlacement(part);
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
                OldPlacement = oldPlacement,
                NewPlacement = ClonePlacement(part)
            });

            if (mirrorIdx >= 0 && mirrorIdx != index)
            {
                _dna.Parts[mirrorIdx].AttachBone = safeAttachBone;
                _dna.Parts[mirrorIdx].Offset = new Vector3(-newOffset.x, newOffset.y, newOffset.z);
                _dna.Parts[mirrorIdx].Rotation = new Vector3(newRotation.x, -newRotation.y, -newRotation.z);
            }

            CreatureSkeletonConstraintSolver.Apply(_dna);
            return true;
        }

        public bool ScalePart(int index, float newScale)
        {
            if (index < 0 || index >= _dna.Parts.Count) return false;
            newScale = Mathf.Max(0.1f, Mathf.Min(3f, newScale));

            var part = _dna.Parts[index];
            var oldPlacement = ClonePlacement(part);
            part.Scale = newScale;

            PushUndo(new EditorAction
            {
                Type = EditorAction.ActionType.ScalePart,
                PartIndex = index,
                OldPlacement = oldPlacement,
                NewPlacement = ClonePlacement(part)
            });

            if (_symmetryEnabled)
            {
                int mirrorIdx = FindMirrorPart(index);
                if (mirrorIdx >= 0 && mirrorIdx != index)
                    _dna.Parts[mirrorIdx].Scale = newScale;
            }

            CreatureSkeletonConstraintSolver.Apply(_dna);
            return true;
        }

        public bool RotatePart(int index, Vector3 newRotation)
        {
            if (index < 0 || index >= _dna.Parts.Count) return false;

            var part = _dna.Parts[index];
            var oldPlacement = ClonePlacement(part);
            part.Rotation = newRotation;

            PushUndo(new EditorAction
            {
                Type = EditorAction.ActionType.RotatePart,
                PartIndex = index,
                OldPlacement = oldPlacement,
                NewPlacement = ClonePlacement(part)
            });

            if (_symmetryEnabled)
            {
                int mirrorIdx = FindMirrorPart(index);
                if (mirrorIdx >= 0 && mirrorIdx != index)
                    _dna.Parts[mirrorIdx].Rotation = new Vector3(newRotation.x, -newRotation.y, -newRotation.z);
            }

            CreatureSkeletonConstraintSolver.Apply(_dna);
            return true;
        }

        public void SetColors(Color primary, Color secondary, Color accent)
        {
            var oldColors = new CreatureColors
            {
                PrimaryColor = _dna.Colors.PrimaryColor,
                SecondaryColor = _dna.Colors.SecondaryColor,
                AccentColor = _dna.Colors.AccentColor
            };

            _dna.Colors.PrimaryColor = primary;
            _dna.Colors.SecondaryColor = secondary;
            _dna.Colors.AccentColor = accent;

            PushUndo(new EditorAction
            {
                Type = EditorAction.ActionType.ChangeColor,
                OldColors = oldColors,
                NewColors = new CreatureColors
                {
                    PrimaryColor = primary,
                    SecondaryColor = secondary,
                    AccentColor = accent
                }
            });

            CreatureSkeletonConstraintSolver.Apply(_dna);
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

        public bool Undo()
        {
            if (_undoStack.Count == 0) return false;
            var action = _undoStack[_undoStack.Count - 1];
            _undoStack.RemoveAt(_undoStack.Count - 1);

            switch (action.Type)
            {
                case EditorAction.ActionType.AddPart:
                    if (action.PartIndex < _dna.Parts.Count)
                        _dna.Parts.RemoveAt(action.PartIndex);
                    break;
                case EditorAction.ActionType.RemovePart:
                    if (action.PartIndex <= _dna.Parts.Count)
                        _dna.Parts.Insert(action.PartIndex, ClonePlacement(action.OldPlacement));
                    break;
                case EditorAction.ActionType.MovePart:
                case EditorAction.ActionType.ScalePart:
                case EditorAction.ActionType.RotatePart:
                    if (action.PartIndex < _dna.Parts.Count)
                    {
                        _dna.Parts[action.PartIndex] = ClonePlacement(action.OldPlacement);
                    }
                    break;
                case EditorAction.ActionType.ChangeColor:
                    _dna.Colors.PrimaryColor = action.OldColors.PrimaryColor;
                    _dna.Colors.SecondaryColor = action.OldColors.SecondaryColor;
                    _dna.Colors.AccentColor = action.OldColors.AccentColor;
                    break;
                case EditorAction.ActionType.ChangeBodyScale:
                    if (_dna?.Body == null)
                        _dna.Body = new BodyPlan();
                    _dna.Body.BodyScale = action.OldBodyScale;
                    _dna.Body.FrontPull = action.OldFrontPull;
                    break;
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
                    if (action.NewPlacement != null)
                        _dna.Parts.Insert(Math.Min(action.PartIndex, _dna.Parts.Count), ClonePlacement(action.NewPlacement));
                    break;
                case EditorAction.ActionType.RemovePart:
                    if (action.PartIndex < _dna.Parts.Count)
                        _dna.Parts.RemoveAt(action.PartIndex);
                    break;
                case EditorAction.ActionType.MovePart:
                case EditorAction.ActionType.ScalePart:
                case EditorAction.ActionType.RotatePart:
                    if (action.PartIndex < _dna.Parts.Count && action.NewPlacement != null)
                        _dna.Parts[action.PartIndex] = ClonePlacement(action.NewPlacement);
                    break;
                case EditorAction.ActionType.ChangeColor:
                    if (action.NewColors != null)
                    {
                        _dna.Colors.PrimaryColor = action.NewColors.PrimaryColor;
                        _dna.Colors.SecondaryColor = action.NewColors.SecondaryColor;
                        _dna.Colors.AccentColor = action.NewColors.AccentColor;
                    }
                    break;
                case EditorAction.ActionType.ChangeBodyScale:
                    if (_dna?.Body == null)
                        _dna.Body = new BodyPlan();
                    _dna.Body.BodyScale = action.NewBodyScale;
                    _dna.Body.FrontPull = action.NewFrontPull;
                    break;
            }

            CreatureSkeletonConstraintSolver.Apply(_dna);
            _undoStack.Add(action);
            return true;
        }

        public int GetDNACost(Dictionary<string, CreaturePartDef> partCatalog)
        {
            int total = 0;
            foreach (var part in _dna.Parts)
            {
                if (partCatalog.TryGetValue(part.PartId, out var def))
                    total += def.DNACost;
            }
            return total;
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
            return new PartPlacement
            {
                PartId = p.PartId,
                AttachBone = p.AttachBone,
                Offset = p.Offset,
                Rotation = p.Rotation,
                Scale = p.Scale
            };
        }

        private void PushUndo(EditorAction action)
        {
            _undoStack.Add(action);
            _redoStack.Clear();
        }
    }
}
