using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// A body CONTINENTAL DRIFT's ground strike has launched, as everyone sees it (owner, 2026-10-17, on v9: *"make it feel like
    /// it hits harder both for POV ng player na natamaan and what everyone else sees"*): a "BOOM!" off the hit, grit and broken
    /// stone streaming off them through the air, and a hard landing (a burst, stone thrown off round them, the lens of the
    /// one it hit slammed and anyone near shaken). Their body tumbles meanwhile (`WindTumble`, a car's launch).
    /// The landing is timed from the rules (`GeoRules.DriftLandSeconds` after the strike), so every peer lands them together.
    /// Presentation only, on every peer; it never moves the body. Idle it is disabled.
    /// </summary>
    public sealed class DanteDriftLaunch : MonoBehaviour
    {
        private CharacterMotor _body;
        private float _at, _nextGrit;
        private bool _landed;
        private int _grain;

        public static void Begin(CharacterMotor body, bool viewed)
        {
            if (body == null) return;
            var launch = body.GetComponent<DanteDriftLaunch>();
            if (launch == null) launch = body.gameObject.AddComponent<DanteDriftLaunch>();
            launch._body = body; launch._at = Time.time; launch._landed = false; launch._nextGrit = 0f;
            launch.enabled = true;
            WindTumble.Attach(body)?.Throw(GeoRules.DriftLandSeconds + .5f);
            // Everyone else reads the hit off the body; the one it hit has it on their own screen instead.
            if (!viewed) ComicPopup.Spawn(body.transform.position + Vector3.up * 1.9f, "BOOM!", new Color(1f, .55f, .12f), 1.35f);
        }

        private void Update()
        {
            if (_body == null) { enabled = false; return; }
            float a = Time.time - _at, air = GeoRules.DriftLandSeconds;
            var at = _body.transform.position;
            if (a < air)
            {
                // Grit and broken stone streaming off them through the air.
                if (a >= _nextGrit && !Settings.SettingsStore.Current.ReducedEffects)
                {
                    _nextGrit = a + .045f;
                    float turn = _grain++ * 2.39f;
                    DanteBoulderGrit.Drop(at + new Vector3(Mathf.Sin(turn) * .3f, .5f + .4f * (_grain % 3), Mathf.Cos(turn) * .3f), true);
                }
                return;
            }
            if (!_landed)
            {
                _landed = true;
                Land(at);
            }
            if (a > air + .3f) enabled = false;
        }

        private void Land(Vector3 at)
        {
            ImpactBurst.SpawnAt(at);
            for (int i = 0; i < 8; i++)
            {
                float turn = i * .785f + .3f;
                DanteBoulderGrit.Drop(at + new Vector3(Mathf.Sin(turn) * .55f, .35f + .15f * (i % 3), Mathf.Cos(turn) * .55f), false);
            }
            var cam = Camera.main;
            var rig = cam != null ? cam.GetComponent<CameraRig>() : null;
            if (rig == null) return;
            if (rig.Following == _body)
            {
                rig.Shake(.75f, .35f);
                rig.BeginGroundRumble(.8f);
                cam.GetComponent<DanteHitLens>()?.Landed();
                return;
            }
            float distance = Vector3.Distance(cam.transform.position, at);
            if (distance < 12f) rig.Shake(.3f * (1f - distance / 12f), .2f);
        }
    }
}
