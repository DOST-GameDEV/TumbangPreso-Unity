using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ THE DISORIENTED MARK, PRESENTED FROM THE STATUS ITSELF, ON EVERY PEER (HERO-10). A player who is Disoriented wears a
    /// small sigil spinning CLOCKWISE over their head while it lasts. Nothing here sends or decides anything; the host's
    /// `SyncUnit` timers are the whole input, so a rejoiner sees it too.
    ///
    /// ⚠️⚠️ MOVED OUT OF `PhaisterMoonlight.cs` WHEN HER RETIRED KIT'S PRESENTATION WAS DELETED (2026-10-29). This used to also
    /// drop the moonlight column on a Vulnerable player and hand the nearest flying manika to a newly Disoriented one. Both
    /// belonged to SPOTLIGHT PIN and MANIKA MISCHIEF, which the voodoo kit retired (`PhaisterHeroKit`): nothing in the live
    /// game calls `ApplyVulnerable`, and no manika is ever thrown, so this ran a per-frame check for two things that could
    /// not happen. The mark stays because Disoriented is still a status in `StatusRules`.
    /// </summary>
    public sealed class PhaisterStatusPresenter : MonoBehaviour
    {
        private CharacterMotor _body;
        private Transform _mark;

        // Body-owned, like StatusBodyMarks: joining peers receive timers without
        // replaying the original cast, and despawning a body must remove its tells.
        private void Awake() => _body = GetComponent<CharacterMotor>();

        private void LateUpdate()
        {
            if (_body == null) { Clear(); return; }
            bool dis = _body.IsDisoriented;
            if (dis && _mark == null)
            {
                var go = VfxShapes.Lay(null, "HexMark", VfxShapes.TwoSided(VfxShapes.Rune(21, 0.12f)), 0.32f, 0f);
                VfxMaterial.Ghost(go.GetComponent<Renderer>(), new Color(0.80f, 0.50f, 1.0f, 0.85f), 1.1f);
                go.transform.localScale = Vector3.one * 0.42f; // An upright glyph needs scale in all three axes.
                _mark = go.transform;
            }
            if (_mark == null) return;
            if (!dis) { Destroy(_mark.gameObject); _mark = null; return; }
            _mark.position = _body.transform.position + Vector3.up * 2.25f;
            _mark.rotation = Quaternion.Euler(0f, -140f * Time.time, 0f);
        }

        private void OnDisable() => Clear();
        private void OnDestroy() => Clear();

        private void Clear()
        {
            if (_mark != null) Destroy(_mark.gameObject);
            _mark = null;
        }
    }
}
