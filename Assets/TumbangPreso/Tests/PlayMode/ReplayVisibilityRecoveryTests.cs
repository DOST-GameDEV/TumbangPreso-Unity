using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ReplayVisibilityRecoveryTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [UnityTest] public IEnumerator InterruptedDrawRestoresLiveVisibilityOnFallback() => Check(true);
        [UnityTest] public IEnumerator OrdinaryDrawAndDisposeKeepOriginalVisibility() => Check(false);

        static IEnumerator Check(bool interrupt)
        {
            yield return MapRetrievalProbe.Load("Eskinita");
            var round = GameServices.Round;
            foreach (var actor in round.Players)
                actor.Teleport(new Vector3(6, .12f, -6 + actor.PlayerSlot * 3));
            var taya = round.PlayerAt(0); var victim = round.PlayerAt(1);
            taya.Teleport(new Vector3(0, .12f, -4)); victim.Teleport(new Vector3(0, .12f, -3));
            taya.transform.forward = Vector3.forward;
            yield return new WaitForSeconds(2.5f);
            Assert.IsTrue(taya.GetComponent<CombatVerbs>().HostResolvePunch(taya.transform.position, taya.transform.forward));
            yield return new WaitForSeconds(1.6f);
            var archive = Object.FindAnyObjectByType<MatchReplayArchive>();
            Assert.AreEqual(1, archive.Clips.Count, archive.LastSkip);
            Assert.IsTrue(RecordedMatchClip.TryDecode(archive.Clips[0].Bytes, out var clip, out var error), error);
            var view = new RecordedWorldView(archive.transform, clip);
            var visible = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visible.GetComponent<Collider>().enabled = false;
            Visual.VfxRenderTag.Attach(visible);
            var renderer = visible.GetComponent<Renderer>();
            var canvas = visible.AddComponent<Canvas>(); canvas.enabled = true;
            var light = visible.AddComponent<Light>(); light.enabled = true;
            var hidden = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hidden.transform.SetParent(visible.transform); hidden.GetComponent<Collider>().enabled = false;
            hidden.GetComponent<Renderer>().forceRenderingOff = true;
            try
            {
                Assert.IsTrue(view.Ready, view.UnavailableReason);
                if (interrupt)
                {
                    // Controlled world-owner loss after a real clip was retained and bound.
                    // Halftime's actual fault path disposes the view after Draw throws.
                    typeof(GameServices).GetProperty("Round").SetValue(null, null);
                    Assert.Throws<System.NullReferenceException>(() => view.Draw(clip.Contact, false));
                    typeof(GameServices).GetProperty("Round").SetValue(null, round);
                }
                else view.Draw(clip.Contact, false);
                view.Dispose();
                Assert.IsFalse(renderer.forceRenderingOff, "Replay fallback left a live renderer hidden.");
                Assert.IsTrue(canvas.enabled, "Replay fallback left a live canvas disabled.");
                Assert.IsTrue(light.enabled, "Replay fallback left a live light disabled.");
                Assert.IsTrue(hidden.GetComponent<Renderer>().forceRenderingOff, "Previously hidden live art must stay hidden.");
            }
            finally
            {
                typeof(GameServices).GetProperty("Round").SetValue(null, round);
                view.Dispose(); Object.Destroy(visible);
            }
        }
    }
}
