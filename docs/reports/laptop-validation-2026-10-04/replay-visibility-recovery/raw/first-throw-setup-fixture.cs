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
                actor.Teleport(new Vector3(6, actor.transform.position.y, -5 + actor.PlayerSlot * 3));
            var thrower = round.PlayerAt(1); var shoe = thrower.GetComponent<Carrier>().Held;
            yield return new WaitForSeconds(2.5f);
            int serial = round.Lata.HostKnockdownSerial;
            shoe.HostThrow(thrower, round.Lata.transform.position + new Vector3(0, 1.2f, -2), Vector3.forward * 12);
            float until = Time.time + 2;
            while (round.Lata.IsUpright && Time.time < until) yield return null;
            Assert.AreEqual(serial + 1, round.Lata.HostKnockdownSerial);
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
