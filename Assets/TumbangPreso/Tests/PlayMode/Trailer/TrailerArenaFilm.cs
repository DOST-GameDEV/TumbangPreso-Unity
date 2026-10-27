using TumbangPreso.Map;
using UnityEngine;

namespace TumbangPreso.PlayTests.Trailer
{
    /// <summary>
    /// TRAILER, BEATS K AND L (docs/trailer/STORY.md): the street throw's tsinelas comes out of the lens in the Philippine
    /// Arena and HITS THE CAN IN THE SAME BREATH (owner, 2026-10-27, of v2's crane before the hit: "ang weird men panget
    /// ng flow"); only then the payoff: Rago finishing the same follow-through, and a crane up and out over the crowd.
    ///   hit     0.0 1.4   Low behind the can: the tsinelas out of the lens, CRACK, the can flies past camera.
    ///   reveal  1.4 6.0   Rago in his follow-through, fist pump; the camera rises behind him and pulls out over the bowl.
    /// The four seats are hidden; the can is a drawn-only copy so the real one never moves.
    /// </summary>
    public sealed class TrailerArenaFilm : MapFilm
    {
        public static readonly (string name, float from, float to)[] Shots = { ("hit", 0.0f, 1.4f), ("reveal", 1.4f, 6.0f) };
        public const float Length = 6.0f;

        protected override string Region => "TRAILER";
        protected override Color Accent => new Color(1.0f, 0.8f, 0.3f);
        protected override string Score => null;
        protected override float CloseAt(bool full) => 999.0f;

        private GameObject _slipper, _can;
        private Extra _rago;
        private TrailerBody _body;

        private Vector3 Thrower => Centre + new Vector3(0.0f, 0.0f, 4.2f);

        protected override void Prepare()
        {
            var book = RosterBook.Load();
            var art = book != null ? book.SlipperArt(0) : null;
            if (art != null && art.Model != null)
            {
                _slipper = Instantiate(art.Model, transform, false);
                foreach (var c in _slipper.GetComponentsInChildren<Collider>(true)) Destroy(c);
                foreach (var b in _slipper.GetComponentsInChildren<MonoBehaviour>(true)) b.enabled = false;
                var body = _slipper.GetComponent<Rigidbody>(); if (body != null) Destroy(body);
                Visual.ToonSkin.ApplySlipper(_slipper, Visual.ToonSkin.PersonOutlineWidth);
                Own(_slipper);
            }
            var lata = GameServices.Round != null ? GameServices.Round.Lata : FindFirstObjectByType<Lata>();
            if (lata != null) { _can = Ghost(lata.gameObject, "Trailer can"); Hide(lata.GetComponentsInChildren<Renderer>()); }
            _rago = Person("Rago", 1.0f, "sean", "zack", "dante");
            Debug.Log($"[TrailerArenaFilm] pro: {(_rago != null ? _rago.Root.name : "none")}");
            if (_rago != null) _body = new TrailerBody(_rago.Model);
        }

        protected override void Stage(float age)
        {
            for (int s = 0; s < Seats; s++) ShowSeat(s, false);
            if (age < 1.4f) Strike(age); else Reveal(age - 1.4f);
        }

        private const float Contact = 0.32f;

        private void Strike(float t)
        {
            Beat = "hit";
            Vector3 eye = Centre + new Vector3(0.25f, 0.42f, -1.7f);
            Vector3 dir = (Centre + Vector3.up * 0.25f - eye).normalized;
            if (t < Contact)
            {
                // Out of the lens, spinning, straight onto the can.
                float k = t / Contact;
                Place(_slipper, Vector3.Lerp(eye + dir * 0.3f, Centre + Vector3.up * 0.28f, k * k * (3 - 2 * k) * 0.3f + k * 0.7f), Quaternion.Euler(200 + t * 1500.0f, 25.0f, t * 400.0f));
                Place(_can, Centre, Quaternion.Euler(0, 180, 0));
            }
            else
            {
                float k = t - Contact;
                Place(_slipper, Centre + new Vector3(0.9f * k, 0.28f + 1.2f * k - 4.9f * k * k, 1.4f * k), Quaternion.Euler(t * 900.0f, 40.0f, 0.0f));
                Vector3 fly = new Vector3(-1.6f, 4.2f, -3.2f);
                Place(_can, Centre + fly * k + Vector3.down * 4.9f * k * k, Quaternion.Euler(k * 1100, k * 300, k * 600));
                if (k < 0.05f) Punch(1.0f);
            }
            float up = Mathf.Max(0.0f, t - Contact);
            Shoot(eye + new Vector3(0, up * 0.25f, -up * 0.5f), Centre + new Vector3(-0.3f * Mathf.Min(up, 0.5f), 0.3f + Mathf.Min(up, 0.6f) * 2.2f, -up * 0.6f), 40.0f + up * 6.0f);
            Rago(t, false);
        }

        private void Reveal(float t)
        {
            Beat = "reveal";
            if (_slipper != null) _slipper.SetActive(false);
            if (_can != null) _can.SetActive(false);
            Rago(t + 1.4f, true);
            // From low behind the can looking at Rago, rising and pulling back over his shoulder to the whole bowl.
            float p = Smooth(t / 4.6f);
            float q = Smooth((t - 0.9f) / 3.7f);
            Vector3 a = Centre + new Vector3(0.6f, 0.9f, -1.2f);
            Vector3 b = Centre + new Vector3(-4.5f, 7.0f, -2.0f);
            Vector3 c = Centre + new Vector3(-10.0f, 26.0f, -34.0f);
            Vector3 eye = q <= 0.0f ? Vector3.Lerp(a, a + new Vector3(0, 0.2f, 0.6f), Smooth(t / 0.9f)) : Vector3.Lerp(Vector3.Lerp(a, b, q), Vector3.Lerp(b, c, q), q);
            Vector3 aim = Vector3.Lerp(Thrower + Vector3.up * 1.2f, Centre + Vector3.up * 1.0f, q);
            Shoot(eye, aim, Mathf.Lerp(30.0f, 60.0f, q));
        }

        /// <summary>Rago: the end of the same throw everyone in the trailer makes, then a fist to the crowd.</summary>
        private void Rago(float t, bool shown)
        {
            if (_rago == null) return;
            _rago.Show(shown);
            if (!shown) return;
            _rago.Pose(Thrower, 180.0f);
            _body?.Reset();
            _rago.Play("idle", t);
            var follow = Trailer.Pose.Follow;
            var pump = new Trailer.Pose { Lean = -8, Nod = -18, Twist = -10, Arm = new Vector3(0.3f, 0.95f, 0.1f), Fore = new Vector3(0.1f, 1.0f, 0.05f), Off = new Vector3(-0.3f, -0.6f, 0.3f), OffFore = new Vector3(-0.1f, -0.2f, 0.9f) };
            var track = new Track().Key(0.0f, follow).Key(2.1f, follow).Key(2.45f, pump, Ease.Back).Key(6.0f, pump);
            var p = track.At(t);
            if (t > 2.45f) p.Hop = 0.05f * Mathf.Abs(Mathf.Sin((t - 2.45f) * 6.0f));
            _body?.Apply(p, _rago.Root.transform);
        }

        private static void Place(GameObject thing, Vector3 at, Quaternion turn)
        {
            if (thing == null) return;
            thing.SetActive(true);
            thing.transform.SetPositionAndRotation(at, turn);
        }
    }
}
