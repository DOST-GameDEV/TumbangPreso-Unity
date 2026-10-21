using System;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// What moves in the Arena's HOLO kit: the ad columns' strips scrolling, the globe and its ring
    /// of text turning, the slipper hologram turning, the slipper balloon swaying on its tether,
    /// the logos and figures riding up and down, and their light breathing.
    ///
    /// The kit says what moves (tools/arena_holo_motion.json, written by tools/author_arena_holo.py)
    /// and `ArenaHoloAuthor` bakes that onto this component when the scene is built: nothing is read
    /// at run time.
    ///
    /// PRESENTATION ONLY, AND NOTHING IS SENT. Every pose is a closed-form function of one clock:
    /// in a session the server clock every peer already shares (as `ArenaTraffic` reads it), so
    /// every peer sees the same sky; offline this peer's own game clock, so a pause stops it.
    /// Nothing here has a collider and nothing reads it.
    ///
    /// COST. One Update over a few dozen entries, no allocation after Awake. A strip is scrolled
    /// and a light is breathed through a MaterialPropertyBlock on the renderer, so the kit's six
    /// materials stay shared; the things that move are not static (`ArenaArtPlacer.Moving`).
    /// </summary>
    public sealed class ArenaHoloMotion : MonoBehaviour
    {
        public enum Kind
        {
            /// <summary>Turns about its own up axis, `Amount` degrees a second.</summary>
            Rotate,
            /// <summary>Its material `Material` slides along v, `Amount` tiles a second.</summary>
            Scroll,
            /// <summary>Leans about its pivot, `Amount` degrees each way, one swing every `Period` seconds.</summary>
            Sway,
            /// <summary>Rises and falls `Amount` metres each way every `Period` seconds.</summary>
            Bob,
            /// <summary>Its light runs between `Low` and `Amount` of itself every `Period` seconds.</summary>
            Pulse,
        }

        [Serializable]
        public struct Move
        {
            public Transform Body;
            public Renderer Skin;
            public Kind Kind;
            public float Amount;
            public float Period;
            public float Low;
            /// <summary>For a scroll: which of the renderer's materials.</summary>
            public int Material;
            /// <summary>0 to 1: where in its cycle it starts, so neighbours are out of step.</summary>
            public float Phase;
        }

        public Move[] Moves = Array.Empty<Move>();

        private static readonly int MainTexSt = Shader.PropertyToID("_MainTex_ST");
        private static readonly int EmissionStrength = Shader.PropertyToID("_EmissionStrength");
        private static readonly int ColourId = Shader.PropertyToID("_Color");

        private Vector3[] _restAt;
        private Quaternion[] _restTurn;
        private Vector4[] _restSt;
        private float[][] _restStrength;
        private Color[][] _restColour;
        private MaterialPropertyBlock _block;
        private double _clock;

        private void Awake()
        {
            int n = Moves.Length;
            _restAt = new Vector3[n];
            _restTurn = new Quaternion[n];
            _restSt = new Vector4[n];
            _restStrength = new float[n][];
            _restColour = new Color[n][];
            _block = new MaterialPropertyBlock();

            for (int i = 0; i < n; i++)
            {
                var move = Moves[i];
                if (move.Body != null)
                {
                    _restAt[i] = move.Body.localPosition;
                    _restTurn[i] = move.Body.localRotation;
                }

                _restSt[i] = new Vector4(1.0f, 1.0f, 0.0f, 0.0f);
                if (move.Skin == null) continue;

                var materials = move.Skin.sharedMaterials;
                if (move.Kind == Kind.Scroll)
                {
                    var m = move.Material >= 0 && move.Material < materials.Length ? materials[move.Material] : null;
                    if (m != null && m.HasProperty(MainTexSt)) _restSt[i] = m.GetVector(MainTexSt);
                }
                else if (move.Kind == Kind.Pulse)
                {
                    _restStrength[i] = new float[materials.Length];
                    _restColour[i] = new Color[materials.Length];
                    for (int k = 0; k < materials.Length; k++)
                    {
                        var m = materials[k];
                        // A material with no emission strength (the lit paint of an emitter) is left alone.
                        bool light = m != null && m.HasProperty(EmissionStrength) && m.HasProperty(ColourId);
                        _restStrength[i][k] = light ? m.GetFloat(EmissionStrength) : -1.0f;
                        _restColour[i][k] = light ? m.GetColor(ColourId) : Color.white;
                    }
                }
            }
        }

        private void Update()
        {
            var nm = Unity.Netcode.NetworkManager.Singleton;
            if (NetAuthority.IsNetworked && nm != null && nm.IsListening) _clock = nm.ServerTime.Time;
            else _clock += Time.deltaTime;

            for (int i = 0; i < Moves.Length; i++)
            {
                var move = Moves[i];
                switch (move.Kind)
                {
                    case Kind.Rotate:
                    {
                        if (move.Body == null) break;
                        float degrees = (float)((_clock * move.Amount + move.Phase * 360.0) % 360.0);
                        move.Body.localRotation = _restTurn[i] * Quaternion.AngleAxis(degrees, Vector3.up);
                        break;
                    }
                    case Kind.Sway:
                    {
                        if (move.Body == null || move.Period <= 0.0f) break;
                        // Two level axes out of step, the second slower: it never repeats exactly.
                        float a = Wave(move.Period, move.Phase) * move.Amount;
                        float b = Wave(move.Period * 1.37f, move.Phase + 0.25f) * move.Amount * 0.6f;
                        move.Body.localRotation = _restTurn[i] * Quaternion.Euler(a, 0.0f, b);
                        break;
                    }
                    case Kind.Bob:
                    {
                        if (move.Body == null || move.Period <= 0.0f) break;
                        move.Body.localPosition = _restAt[i] + new Vector3(0.0f, Wave(move.Period, move.Phase) * move.Amount, 0.0f);
                        break;
                    }
                    case Kind.Scroll:
                    {
                        if (move.Skin == null) break;
                        var st = _restSt[i];
                        st.w += (float)((_clock * move.Amount + move.Phase) % 1.0);
                        move.Skin.GetPropertyBlock(_block, move.Material);
                        _block.SetVector(MainTexSt, st);
                        move.Skin.SetPropertyBlock(_block, move.Material);
                        break;
                    }
                    case Kind.Pulse:
                    {
                        var strengths = _restStrength[i];
                        if (move.Skin == null || strengths == null || move.Period <= 0.0f) break;
                        float share = Mathf.Lerp(move.Low, move.Amount, Wave(move.Period, move.Phase) * 0.5f + 0.5f);
                        for (int k = 0; k < strengths.Length; k++)
                        {
                            if (strengths[k] < 0.0f) continue;
                            var colour = _restColour[i][k];
                            move.Skin.GetPropertyBlock(_block, k);
                            _block.SetFloat(EmissionStrength, strengths[k] * share);
                            _block.SetColor(ColourId, new Color(colour.r * share, colour.g * share, colour.b * share, colour.a));
                            move.Skin.SetPropertyBlock(_block, k);
                        }

                        break;
                    }
                }
            }
        }

        /// <summary>A sine of the clock, -1 to 1, one cycle every `period` seconds.</summary>
        private float Wave(float period, float phase)
        {
            return (float)Math.Sin((_clock / period + phase) * (2.0 * Math.PI));
        }
    }
}
