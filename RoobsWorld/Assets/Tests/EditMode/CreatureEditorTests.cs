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
            var newPos = new Vector3(1, 2, 3);
            Assert.IsTrue(state.MovePart(0, newPos));
            Assert.AreEqual(newPos, state.DNA.Parts[0].Offset);
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
    }
}
