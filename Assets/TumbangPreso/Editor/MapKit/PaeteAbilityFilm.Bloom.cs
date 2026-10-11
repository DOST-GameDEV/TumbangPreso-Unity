using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    public static partial class PaeteAbilityFilm
    {
        /// <summary>
        /// BAKYA BLOOM acted out, every beat: the seed thrown from where his hand would be, the landing, the pitcher
        /// rising and shaking itself off, a clog growing (in 1.2 s here; in the game it takes
        /// `PaeteRules.PlantReloadSeconds`), LOADED, the spit, the clog in the air, the knock on the can, the clog
        /// coming down, and the plant pulled out. What `Abilities.PaetePlant` does at each moment, done here by hand
        /// with the same calls (`Visual.PaeteBloomFx`).
        ///
        /// ⚠️ THE CLOG HERE IS A STAND-IN FOR ITS FLIGHT ONLY. `Abilities.PaeteWoodenSlipper` flies on the physics step
        /// and asks the round for the can, neither of which exists outside Play, so this flies the same carved clog
        /// (`PaeteBloomFx.BuildBakya`) from the same muzzle at the same speed under the same gravity, turns it the same
        /// way, and calls the same knock and landing. The can is a red tin stood where one would be.
        /// </summary>
        private sealed class BloomFilm : FxFilm
        {
            private const float Flight = 0.35f, GrowFrom = 0.9f, GrowFor = 1.2f, SpitAt = 2.9f, PullAt = 4.3f;
            private static readonly Vector3 Hand = new Vector3(-1.7f, 1.25f, 0.5f);
            private static readonly Vector3 Can = new Vector3(3.2f, 0f, 1.4f);
            private static readonly Vector3 Muzzle = new Vector3(0f, 0.95f, 0f);
            private static readonly Vector3 Puller = new Vector3(-0.9f, 0f, 1.1f);
            private PaetePlantBody _body;
            private Transform _clog, _clogBody, _can;
            private Vector3 _clogVelocity;
            private bool _broke, _shook, _ready, _spat, _knocked, _clogLanded, _pulled;
            private float _canFallen = -1f;

            public override string Name => "bloomfx";
            public override float Seconds => 5.5f;
            public override float[] StripTimes => new[]
            {
                0.10f, 0.23f, 0.33f, 0.40f, 0.50f, 0.70f, 0.80f, 2.13f, 2.23f, 2.90f, 2.97f, 3.03f, 3.10f, 3.20f, 3.30f, 3.50f, 4.33f, 4.47f, 4.70f,
            };
            public override (Vector3 eye, Vector3 look, float fov)[] Views => new[]
            {
                (new Vector3(1.0f, 2.4f, 8.2f), new Vector3(1.0f, 0.5f, 0.5f), 40f),
                (new Vector3(1.3f, 1.3f, 2.0f), new Vector3(0f, 0.62f, 0f), 44f),
                (new Vector3(4.6f, 1.1f, 3.6f), new Vector3(2.5f, 0.4f, 1.0f), 46f),
            };

            public override void Begin(FxStage stage)
            {
                var host = stage.New("~BloomHost");
                _body = PaetePlantBody.Build(host.transform);
                PaeteSeedArc.Throw(Hand, Vector3.zero, Flight, 0.14f, false);
                // The can: a tin, inked like everything else on the court.
                var canHost = stage.New("~BloomCan");
                canHost.transform.position = Can;
                var mesh = new Mesh { name = "PaeteFilmCan" };
                PaeteInk.Tube(mesh, new List<Vector3> { Vector3.zero, Vector3.up * 0.004f, Vector3.up * 0.02f, Vector3.up * 0.28f, Vector3.up * 0.296f, Vector3.up * 0.30f },
                              new List<float> { 0.002f, 0.09f, 0.10f, 0.10f, 0.09f, 0.002f }, 10);
                PaeteInk.Part(canHost.transform, "tin", mesh, new Color(0.80f, 0.22f, 0.18f));
                _can = canHost.transform;
            }

            public override void Step(FxStage stage, float t, float dt)
            {
                float age = t - Flight;
                if (!_broke && age >= 0f) { _broke = true; PaeteGroundBreak.Spawn(Vector3.zero, 0.7f); PaeteBloomFx.SeedLanding(Vector3.zero); }
                if (!_shook && age >= PaeteBloomFx.RiseShakeAt) { _shook = true; PaeteBloomFx.RiseShake(Muzzle); }
                if (!_ready && t >= GrowFrom + GrowFor) { _ready = true; PaeteBloomFx.Ready(Muzzle); }
                if (!_spat && t >= SpitAt)
                {
                    _spat = true;
                    Vector3 target = Can + Vector3.up * 0.15f;
                    _body.AimAt(target);
                    PaeteBloomFx.Spit(Muzzle, target);
                    _clog = stage.New("~BloomClog").transform;
                    _clog.position = Muzzle;
                    _clogBody = new GameObject("body").transform;
                    _clogBody.SetParent(_clog, false);
                    PaeteBloomFx.BuildBakya(_clogBody);
                    PaeteClogTrail.Follow(_clog);
                    _clogVelocity = Slipper.SolveArc(Muzzle, target, PaeteRules.WoodenSlipperSpeed) * PaeteRules.WoodenSlipperSpeed;
                }
                StepClog(dt);
                if (_canFallen >= 0f)
                {
                    // The tin goes over, away from the clog, and rocks once.
                    _canFallen += dt;
                    float over = Mathf.Clamp01(_canFallen / 0.22f);
                    _can.rotation = Quaternion.AngleAxis(90f * GrowthVfx.Pop(over), Vector3.Cross(Vector3.up, new Vector3(Can.x, 0f, Can.z).normalized));
                    _can.position = Can + new Vector3(Can.x, 0f, Can.z).normalized * (0.5f * over) + Vector3.up * (0.10f * over + 0.12f * Mathf.Sin(over * Mathf.PI));
                }
                if (!_pulled && t >= PullAt)
                {
                    _pulled = true;
                    PaeteLeafBurst.Spawn(Vector3.up * 0.4f, 7, 1.8f);
                    PaeteBloomFx.Uproot(Vector3.zero, Puller);
                }
                if (_pulled)
                {
                    float since = t - PullAt;
                    if (since < 0.95f) _body.PosePulled(since, Puller);
                    else _body.gameObject.SetActive(false);
                    return;
                }
                float sinceShot = t >= SpitAt ? t - SpitAt : 99f;
                // The first clog grows from GrowFrom; after the spit the next one starts from nothing.
                float growth = t >= SpitAt ? Mathf.Clamp01((t - SpitAt) / (GrowFor * 3f)) : Mathf.Clamp01((t - GrowFrom) / GrowFor);
                _body.Pose(age, 0f, false, growth, sinceShot);
            }

            /// <summary>`PaeteWoodenSlipper.FixedUpdate`'s flight, by hand: gravity, the turn, the knock, the landing.</summary>
            private void StepClog(float dt)
            {
                if (_clog == null || _clogLanded || dt <= 0f) return;
                _clogVelocity.y -= Balance.Gravity * dt;
                _clog.position += _clogVelocity * dt;
                _clogBody.Rotate(900f * dt, 0f, 0f, Space.Self);
                if (_clogVelocity.sqrMagnitude > 0.01f) _clog.rotation = Quaternion.LookRotation(_clogVelocity);
                Vector3 toCan = _clog.position - Can; toCan.y = 0f;
                if (!_knocked && (toCan.magnitude < 0.28f || Vector3.Dot(toCan, new Vector3(Can.x, 0f, Can.z)) > 0f) && _clog.position.y < 0.6f)
                {
                    _knocked = true; _canFallen = 0f;
                    PaeteBloomFx.ClogKnock(_clog.position, _clogVelocity);
                    _clogVelocity = -_clogVelocity * 0.25f;
                }
                if (_clog.position.y <= 0.03f && _clogVelocity.y < 0f)
                {
                    _clogLanded = true;
                    _clog.position = new Vector3(_clog.position.x, 0.02f, _clog.position.z);
                    _clog.rotation = Quaternion.Euler(0f, _clog.eulerAngles.y, 0f);
                    _clogBody.localRotation = Quaternion.identity;
                    _clogBody.localPosition = Vector3.up * PaeteBloomFx.BakyaRest;
                    PaeteBloomFx.ClogLand(_clog.position);
                }
            }
        }
    }
}
