using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ EVERY OTHER PLAYER'S STATUSES, ABOVE THEIR HEAD, ON EVERY PEER (owner's status table,
    /// 2026-09-25: each status has an icon). Up to two badges side by side over the nameplate,
    /// billboarded, popping in when the status lands and fading over its last half second, so a
    /// Whirled attacker reads from across the court as the one who cannot pick up. The local
    /// player's own statuses are on the HUD instead (`TumpMatchReadout.Statuses`); a first-person
    /// body never draws its own overhead.
    /// </summary>
    public sealed class StatusOverhead : MonoBehaviour
    {
        private const int Slots = 2;
        private CharacterMotor _body;
        private CharacterController _capsule;
        private readonly SpriteRenderer[] _badges = new SpriteRenderer[Slots];
        private readonly StatusKind[] _shown = new StatusKind[Slots];
        private readonly float[] _since = new float[Slots];
        private readonly List<StatusKind> _live = new List<StatusKind>(4);

        private void Awake()
        {
            _body = GetComponent<CharacterMotor>();
            _capsule = GetComponent<CharacterController>();
            for (int i = 0; i < Slots; i++)
            {
                var go = new GameObject("StatusBadge" + i);
                go.transform.SetParent(transform, false);
                var sprite = go.AddComponent<SpriteRenderer>();
                sprite.enabled = false;
                sprite.sortingOrder = 30;
                sprite.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _badges[i] = sprite;
            }
        }

        private bool IsLocalView()
        {
            if (GameLaunch.Spectator || _body.IsBot) return false;
            int local = NetAuthority.IsNetworked ? NetAuthority.LocalSlot : GameLaunch.SoloSeat;
            return _body.PlayerSlot == local;
        }

        private void LateUpdate()
        {
            if (_body == null) return;
            var view = Camera.main;
            StatusIcons.Live(IsLocalView() ? null : _body, _live);
            float top = (_capsule != null ? _capsule.height * transform.lossyScale.y : 1.8f) + 0.95f;
            for (int i = 0; i < Slots; i++)
            {
                var badge = _badges[i];
                var kind = i < _live.Count ? _live[i] : StatusKind.None;
                if (kind != _shown[i]) { _shown[i] = kind; _since[i] = Time.time; badge.sprite = StatusIcons.For(kind); }
                badge.enabled = kind != StatusKind.None && badge.sprite != null && view != null;
                if (!badge.enabled) continue;
                float left = _body.StatusLeft(kind);
                float pop = Mathf.Clamp01((Time.time - _since[i]) / 0.18f);
                float scale = 0.42f * (pop < 1 ? Mathf.Lerp(1.35f, 1.0f, pop * pop) : 1.0f);
                float fade = Mathf.Clamp01(left / 0.5f);
                float x = _live.Count > 1 ? (i - 0.5f) * 0.5f : 0.0f;
                badge.transform.position = transform.position + Vector3.up * top + view.transform.right * x;
                badge.transform.rotation = Quaternion.LookRotation(badge.transform.position - view.transform.position, view.transform.up);
                badge.transform.localScale = Vector3.one * scale / Mathf.Max(0.01f, transform.lossyScale.x);
                badge.color = new Color(1, 1, 1, fade);
            }
        }
    }
}
