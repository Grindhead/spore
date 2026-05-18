using NUnit.Framework;
using RoobsWorld.Creature;
using RoobsWorld.Creature.Editor;
using UnityEngine;

namespace RoobsWorld.Tests.EditMode
{
    public class CreatureEditorTests
    {
        [Test]
        public void NewEditor_HasDefaultDNA()
        {
            var state = new CreatureEditorState();
            Assert.IsNotNull(state.DNA);
            Assert.Greater(state.DNA.Parts.Count, 0);
        }

        [Test]
        public void AddPart_IncreasesParts()
        {
            var state = new CreatureEditorState(new CreatureDNA { Parts = new System.Collections.Generic.List<PartPlacement>() });
            state.SetSymmetry(false);
            int before = state.DNA.Parts.Count;
            state.AddPart("leg_basic", 1, new Vector3(0.4f, 0, 0));
            Assert.AreEqual(before + 1, state.DNA.Parts.Count);
        }

        [Test]
        public void AddPart_WithSymmetry_AddsTwoParts()
        {
            var state = new CreatureEditorState(new CreatureDNA { Parts = new System.Collections.Generic.List<PartPlacement>() });
            state.SetSymmetry(true);
            state.AddPart("leg_basic", 1, new Vector3(0.4f, 0, 0));
            Assert.AreEqual(2, state.DNA.Parts.Count);
            Assert.AreEqual(-0.4f, state.DNA.Parts[1].Offset.x, 0.01f);
        }

        [Test]
        public void RemovePart_DecreasesParts()
        {
            var state = new CreatureEditorState(new CreatureDNA { Parts = new System.Collections.Generic.List<PartPlacement>() });
            state.SetSymmetry(false);
            state.AddPart("leg_basic", 1, new Vector3(0.4f, 0, 0));
            Assert.IsTrue(state.RemovePart(0));
            Assert.AreEqual(0, state.DNA.Parts.Count);
        }

        [Test]
        public void RemovePart_InvalidIndex_ReturnsFalse()
        {
            var state = new CreatureEditorState();
            Assert.IsFalse(state.RemovePart(-1));
            Assert.IsFalse(state.RemovePart(999));
        }

        [Test]
        public void MovePart_UpdatesOffset()
        {
            var state = new CreatureEditorState(new CreatureDNA { Parts = new System.Collections.Generic.List<PartPlacement>() });
            state.SetSymmetry(false);
            state.AddPart("leg_basic", 1, Vector3.zero);
            var newPos = new Vector3(0.4f, 0.3f, 0.5f);
            Assert.IsTrue(state.MovePart(0, newPos));
            Assert.AreEqual(newPos, state.DNA.Parts[0].Offset);
        }

        [Test]
        public void MovePart_ToAttachment_UpdatesBoneOffsetAndRotation()
        {
            var state = new CreatureEditorState(new CreatureDNA
            {
                Body = new BodyPlan { SpineSegments = 3, BodyScale = Vector3.one },
                Parts = new System.Collections.Generic.List<PartPlacement>()
            });
            state.SetSymmetry(false);
            state.AddPart("leg_basic", 0, Vector3.zero);

            var newOffset = new Vector3(0.35f, 0.15f, -0.2f);
            var newRotation = new Vector3(12f, 45f, 8f);

            Assert.IsTrue(state.MovePart(0, 2, newOffset, newRotation));
            Assert.AreEqual(2, state.DNA.Parts[0].AttachBone);
            Assert.AreEqual(newOffset, state.DNA.Parts[0].Offset);
            Assert.AreEqual(newRotation, state.DNA.Parts[0].Rotation);
        }

        [Test]
        public void MovePart_ToAttachment_WithSymmetry_MirrorsPlacement()
        {
            var state = new CreatureEditorState(new CreatureDNA
            {
                Body = new BodyPlan { SpineSegments = 3, BodyScale = Vector3.one },
                Parts = new System.Collections.Generic.List<PartPlacement>()
            });
            state.SetSymmetry(true);
            state.AddPart("leg_basic", 0, new Vector3(0.4f, 0f, 0.1f));

            var newOffset = new Vector3(0.3f, 0.2f, -0.15f);
            var newRotation = new Vector3(10f, 30f, 5f);

            Assert.IsTrue(state.MovePart(0, 2, newOffset, newRotation));
            Assert.AreEqual(2, state.DNA.Parts[1].AttachBone);
            Assert.AreEqual(-newOffset.x, state.DNA.Parts[1].Offset.x, 0.01f);
            Assert.AreEqual(newOffset.y, state.DNA.Parts[1].Offset.y, 0.01f);
            Assert.AreEqual(newOffset.z, state.DNA.Parts[1].Offset.z, 0.01f);
            Assert.AreEqual(newRotation.x, state.DNA.Parts[1].Rotation.x, 0.01f);
            Assert.AreEqual(-newRotation.y, state.DNA.Parts[1].Rotation.y, 0.01f);
            Assert.AreEqual(-newRotation.z, state.DNA.Parts[1].Rotation.z, 0.01f);
        }

        [Test]
        public void MovePart_OutOfEnvelope_IsClampedBySkeleton()
        {
            var state = new CreatureEditorState(new CreatureDNA { Parts = new System.Collections.Generic.List<PartPlacement>() });
            state.SetSymmetry(false);
            state.AddPart("leg_basic", 1, Vector3.zero);
            Assert.IsTrue(state.MovePart(0, new Vector3(9f, 9f, 9f)));
            var offset = state.DNA.Parts[0].Offset;
            Assert.Less(offset.x, 9f);
            Assert.Less(offset.y, 9f);
            Assert.Less(offset.z, 9f);
        }

        [Test]
        public void ScalePart_ClampsToRange()
        {
            var state = new CreatureEditorState(new CreatureDNA { Parts = new System.Collections.Generic.List<PartPlacement>() });
            state.SetSymmetry(false);
            state.AddPart("leg_basic", 1, Vector3.zero);
            state.ScalePart(0, 5f);
            Assert.LessOrEqual(state.DNA.Parts[0].Scale, 3f);
            state.ScalePart(0, -1f);
            Assert.GreaterOrEqual(state.DNA.Parts[0].Scale, 0.1f);
        }

        [Test]
        public void RotatePart_UpdatesRotation()
        {
            var state = new CreatureEditorState(new CreatureDNA { Parts = new System.Collections.Generic.List<PartPlacement>() });
            state.SetSymmetry(false);
            state.AddPart("leg_basic", 1, Vector3.zero);
            var rot = new Vector3(10, 20, 30);
            Assert.IsTrue(state.RotatePart(0, rot));
            Assert.AreEqual(rot, state.DNA.Parts[0].Rotation);
        }

        [Test]
        public void AddPart_PersistsInitialRotation()
        {
            var state = new CreatureEditorState(new CreatureDNA { Parts = new System.Collections.Generic.List<PartPlacement>() });
            state.SetSymmetry(false);

            var rot = new Vector3(14f, 222f, 39f);
            state.AddPart("horn_basic", 1, new Vector3(0.35f, 0.2f, 0.3f), 1f, rot);

            Assert.AreEqual(1, state.DNA.Parts.Count);
            Assert.AreEqual(rot, state.DNA.Parts[0].Rotation);
        }

        [Test]
        public void Undo_RevertsAddPart()
        {
            var state = new CreatureEditorState(new CreatureDNA { Parts = new System.Collections.Generic.List<PartPlacement>() });
            state.SetSymmetry(false);
            state.AddPart("leg_basic", 1, Vector3.zero);
            Assert.AreEqual(1, state.DNA.Parts.Count);
            Assert.IsTrue(state.Undo());
            Assert.AreEqual(0, state.DNA.Parts.Count);
        }

        [Test]
        public void Redo_ReappliesAddPart()
        {
            var state = new CreatureEditorState(new CreatureDNA { Parts = new System.Collections.Generic.List<PartPlacement>() });
            state.SetSymmetry(false);
            state.AddPart("leg_basic", 1, Vector3.zero);
            state.Undo();
            Assert.IsTrue(state.Redo());
            Assert.AreEqual(1, state.DNA.Parts.Count);
        }

        [Test]
        public void Undo_RevertsMovePart()
        {
            var state = new CreatureEditorState(new CreatureDNA { Parts = new System.Collections.Generic.List<PartPlacement>() });
            state.SetSymmetry(false);
            state.AddPart("leg_basic", 1, Vector3.zero);
            state.MovePart(0, new Vector3(5, 5, 5));
            state.Undo();
            Assert.AreEqual(Vector3.zero, state.DNA.Parts[0].Offset);
        }

        [Test]
        public void Undo_RevertsColorChange()
        {
            var state = new CreatureEditorState();
            var origColor = state.DNA.Colors.PrimaryColor;
            state.SetColors(Color.red, Color.blue, Color.green);
            state.Undo();
            Assert.AreEqual(origColor, state.DNA.Colors.PrimaryColor);
        }

        [Test]
        public void CanUndo_FalseInitially()
        {
            var state = new CreatureEditorState();
            Assert.IsFalse(state.CanUndo);
        }

        [Test]
        public void CanRedo_FalseAfterNewAction()
        {
            var state = new CreatureEditorState(new CreatureDNA { Parts = new System.Collections.Generic.List<PartPlacement>() });
            state.SetSymmetry(false);
            state.AddPart("a", 0, Vector3.zero);
            state.Undo();
            state.AddPart("b", 0, Vector3.zero);
            Assert.IsFalse(state.CanRedo);
        }

        [Test]
        public void SelectPart_SetsIndex()
        {
            var state = new CreatureEditorState();
            state.SelectPart(0);
            Assert.AreEqual(0, state.SelectedPartIndex);
        }

        [Test]
        public void DeselectPart_ResetsIndex()
        {
            var state = new CreatureEditorState();
            state.SelectPart(0);
            state.DeselectPart();
            Assert.AreEqual(-1, state.SelectedPartIndex);
        }

        [Test]
        public void Symmetry_MovePartMirrors()
        {
            var state = new CreatureEditorState(new CreatureDNA { Parts = new System.Collections.Generic.List<PartPlacement>() });
            state.SetSymmetry(true);
            state.AddPart("leg_basic", 1, new Vector3(0.5f, 0, 0));
            state.MovePart(0, new Vector3(1f, 0.5f, 0.3f));
            Assert.AreEqual(-1f, state.DNA.Parts[1].Offset.x, 0.01f);
            Assert.AreEqual(0.5f, state.DNA.Parts[1].Offset.y, 0.01f);
        }

        [Test]
        public void Symmetry_ScalePartMirrors()
        {
            var state = new CreatureEditorState(new CreatureDNA { Parts = new System.Collections.Generic.List<PartPlacement>() });
            state.SetSymmetry(true);
            state.AddPart("leg_basic", 1, new Vector3(0.5f, 0, 0));
            state.ScalePart(0, 2f);
            Assert.AreEqual(2f, state.DNA.Parts[1].Scale, 0.01f);
        }

        [Test]
        public void AddPart_WithSymmetry_MirrorsInitialRotation()
        {
            var state = new CreatureEditorState(new CreatureDNA { Parts = new System.Collections.Generic.List<PartPlacement>() });
            state.SetSymmetry(true);

            var rot = new Vector3(18f, 36f, 72f);
            state.AddPart("arm_basic", 1, new Vector3(0.5f, 0.1f, 0.2f), 1f, rot);

            Assert.AreEqual(2, state.DNA.Parts.Count);
            Assert.AreEqual(rot.x, state.DNA.Parts[1].Rotation.x, 0.01f);
            Assert.AreEqual(-rot.y, state.DNA.Parts[1].Rotation.y, 0.01f);
            Assert.AreEqual(-rot.z, state.DNA.Parts[1].Rotation.z, 0.01f);
        }

        [Test]
        public void AddPart_AttachBoneIsConstrainedToSpine()
        {
            var dna = new CreatureDNA
            {
                Body = new BodyPlan { SpineSegments = 2, BodyScale = Vector3.one },
                Parts = new System.Collections.Generic.List<PartPlacement>()
            };
            var state = new CreatureEditorState(dna);
            state.SetSymmetry(false);
            state.AddPart("tail_basic", 99, new Vector3(0f, 0f, -0.5f));
            Assert.AreEqual(1, state.DNA.Parts[0].AttachBone);
        }

        [Test]
        public void DefaultCatalog_RegistersAvailableGeneratedPartModels()
        {
            var catalog = CreaturePartCatalog.GetDefaultCatalog();
            var prefabs = Resources.LoadAll<GameObject>("Models/CreatureParts");
            int generatedCount = 0;

            for (int i = 0; i < prefabs.Length; i++)
            {
                var prefab = prefabs[i];
                if (prefab == null || !prefab.name.StartsWith("animal_"))
                    continue;

                generatedCount++;
                Assert.IsTrue(catalog.TryGetValue(prefab.name, out var def), $"{prefab.name} should appear in the creature editor catalog.");
                Assert.AreSame(prefab, def.ModelPrefab, $"{prefab.name} should use its actual model prefab for previews and placement.");
            }

            Assert.Greater(generatedCount, 0, "Expected generated animal creature-part prefabs under Resources/Models/CreatureParts.");
        }

        [Test]
        public void PartPreviewInstance_HidesGeneratedColliderMeshRenderers()
        {
            var prefab = new GameObject("animal_tail_999");
            var visible = new GameObject("animal_tail_999_VIS");
            var collider = new GameObject("animal_tail_999_COL");
            GameObject instance = null;
            CreaturePartDef def = null;

            try
            {
                visible.transform.SetParent(prefab.transform, false);
                collider.transform.SetParent(prefab.transform, false);
                visible.AddComponent<MeshRenderer>();
                collider.AddComponent<MeshRenderer>();

                def = ScriptableObject.CreateInstance<CreaturePartDef>();
                def.PartId = prefab.name;
                def.ModelPrefab = prefab;
                def.DefaultScale = Vector3.one;

                instance = CreatureThumbnail.CreatePartPreviewInstance(def, def.PartId);
                Assert.IsNotNull(instance);

                var visibleRenderer = instance.transform.Find("animal_tail_999_VIS")?.GetComponent<MeshRenderer>();
                var colliderRenderer = instance.transform.Find("animal_tail_999_COL")?.GetComponent<MeshRenderer>();

                Assert.IsNotNull(visibleRenderer);
                Assert.IsNotNull(colliderRenderer);
                Assert.IsTrue(visibleRenderer.enabled);
                Assert.IsFalse(colliderRenderer.enabled);
            }
            finally
            {
                if (instance != null) Object.DestroyImmediate(instance);
                if (def != null) Object.DestroyImmediate(def);
                Object.DestroyImmediate(prefab);
            }
        }
    }
}
