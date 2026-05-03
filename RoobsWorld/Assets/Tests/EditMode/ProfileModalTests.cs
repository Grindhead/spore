using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.UI;
using UnityEngine;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// EditMode tests for ProfileModal helper logic and RunRecord formatting.
    /// Full UI construction tests require PlayMode (MonoBehaviour + Canvas).
    /// </summary>
    public class ProfileModalTests
    {
        // ---- RunRecord personal bests ----

        [Test]
        public void RunRecord_Empty_Returns_Zero_Bests()
        {
            var r = RunRecord.Empty();
            Assert.AreEqual(0f, r.BestTimeSurvived);
            Assert.AreEqual(0f, r.BestPeakMass);
            Assert.AreEqual(0,  r.BestDNAEarned);
            Assert.AreEqual(0,  r.BestSpeciesCount);
            Assert.AreEqual(0,  r.BestTraitCount);
            Assert.AreEqual(0,  r.TotalRunsCompleted);
        }

        [Test]
        public void RunRecord_Update_Sets_Bests_On_First_Run()
        {
            var r = RunRecord.Empty();
            var flags = r.Update(timeSurvived: 120f, peakMass: 5f, dnaEarned: 30,
                                  speciesCount: 4, traitCount: 3);
            Assert.AreEqual(120f, r.BestTimeSurvived);
            Assert.AreEqual(5f,   r.BestPeakMass);
            Assert.AreEqual(30,   r.BestDNAEarned);
            Assert.IsTrue(flags.Any);
            Assert.AreEqual(1,    r.TotalRunsCompleted);
        }

        [Test]
        public void RunRecord_Update_Does_Not_Lower_Bests()
        {
            var r = RunRecord.Empty();
            r.Update(200f, 10f, 50, 6, 4);
            var flags = r.Update(50f, 3f, 10, 2, 1); // worse run
            Assert.IsFalse(flags.Any);
            Assert.AreEqual(200f, r.BestTimeSurvived);
            Assert.AreEqual(10f,  r.BestPeakMass);
            Assert.AreEqual(50,   r.BestDNAEarned);
            Assert.AreEqual(2,    r.TotalRunsCompleted); // still increments
        }

        [Test]
        public void RunRecord_Update_Flags_Correctly()
        {
            var r = RunRecord.Empty();
            r.Update(100f, 5f, 20, 3, 2);
            // Beat only time and DNA
            var flags = r.Update(200f, 4f, 40, 2, 1);
            Assert.IsTrue(flags.Time);
            Assert.IsFalse(flags.PeakMass);
            Assert.IsTrue(flags.DNA);
            Assert.IsFalse(flags.Species);
            Assert.IsFalse(flags.Traits);
        }

        [Test]
        public void RunRecord_Roundtrip_Json()
        {
            var r = RunRecord.Empty();
            r.Update(90f, 8f, 42, 5, 3);
            r.TotalLeviathansSurvived = 2;
            r.TotalLeviathanDefeated  = 1;

            var json    = r.ToJson();
            var loaded  = RunRecord.FromJson(json);
            Assert.AreEqual(r.BestTimeSurvived,       loaded.BestTimeSurvived,   0.001f);
            Assert.AreEqual(r.BestPeakMass,           loaded.BestPeakMass,       0.001f);
            Assert.AreEqual(r.BestDNAEarned,          loaded.BestDNAEarned);
            Assert.AreEqual(r.TotalLeviathansSurvived, loaded.TotalLeviathansSurvived);
            Assert.AreEqual(r.TotalLeviathanDefeated,  loaded.TotalLeviathanDefeated);
        }

        [Test]
        public void RunRecord_FromJson_Null_Returns_Empty()
        {
            var r = RunRecord.FromJson(null);
            Assert.IsNotNull(r);
            Assert.AreEqual(0, r.TotalRunsCompleted);
        }

        [Test]
        public void RunRecord_TryFromJson_Garbage_Returns_Error()
        {
            bool parsed = RunRecord.TryFromJson("{not valid json}", out var r, out string error);

            Assert.IsFalse(parsed);
            Assert.IsNotNull(r);
            Assert.IsFalse(string.IsNullOrWhiteSpace(error));
        }

        // ---- time formatting (via reflection on private static) ----

        [Test]
        public void FormatTime_Zero_Returns_Dash()
        {
            var result = InvokeFormatTime(0f);
            Assert.AreEqual("—", result);
        }

        [Test]
        public void FormatTime_Under_Minute_Shows_Seconds()
        {
            var result = InvokeFormatTime(45f);
            Assert.AreEqual("45s", result);
        }

        [Test]
        public void FormatTime_Over_Minute_Shows_Minutes_And_Seconds()
        {
            var result = InvokeFormatTime(125f); // 2m 05s
            Assert.AreEqual("2m 05s", result);
        }

        [Test]
        public void FormatTime_Exact_Minute_Shows_Zero_Seconds()
        {
            var result = InvokeFormatTime(60f);
            Assert.AreEqual("1m 00s", result);
        }

        private static string InvokeFormatTime(float seconds)
        {
            var method = typeof(ProfileModal).GetMethod("FormatTime",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return (string)method.Invoke(null, new object[] { seconds });
        }

        // ---- ModalDragHandler — create and verify no-throw ----

        [Test]
        public void ModalDragHandler_OnDrag_With_Null_Target_Does_Not_Throw()
        {
            var go  = new GameObject("DragTest");
            var drg = go.AddComponent<ModalDragHandler>();
            drg.Target       = null;
            drg.ParentCanvas = null;

            var evtData = new UnityEngine.EventSystems.PointerEventData(
                UnityEngine.EventSystems.EventSystem.current)
            {
                delta = new Vector2(10f, 20f)
            };

            Assert.DoesNotThrow(() => drg.OnDrag(evtData));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ModalDragHandler_MovesDelta_In_Both_Axes()
        {
            var canvasGo = new GameObject("Canvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var panelGo = new GameObject("Panel");
            var panel   = panelGo.AddComponent<RectTransform>();
            panel.anchorMin        = new Vector2(0.5f, 0.5f);
            panel.anchorMax        = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;

            var headerGo = new GameObject("Header");
            var handler  = headerGo.AddComponent<ModalDragHandler>();
            handler.Target       = panel;
            handler.ParentCanvas = canvas;

            var evtData = new UnityEngine.EventSystems.PointerEventData(
                UnityEngine.EventSystems.EventSystem.current)
            {
                delta = new Vector2(50f, -30f)
            };
            handler.OnDrag(evtData);

            // After drag, panel should have moved in both axes
            float scaleFactor = canvas.scaleFactor > 0 ? canvas.scaleFactor : 1f;
            Assert.AreNotEqual(0f, panel.anchoredPosition.x);
            Assert.AreNotEqual(0f, panel.anchoredPosition.y);

            Object.DestroyImmediate(canvasGo);
            Object.DestroyImmediate(panelGo);
            Object.DestroyImmediate(headerGo);
        }
    }
}
