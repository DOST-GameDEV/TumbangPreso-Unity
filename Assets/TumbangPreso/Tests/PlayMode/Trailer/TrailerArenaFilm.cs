using TumbangPreso.Map;
using UnityEngine;

namespace TumbangPreso.PlayTests.Trailer
{
    /// <summary>
    /// TRAILER, ACT 1, THE STADIUM (Philippine Arena). The street throw's tsinelas arrives here on a match cut.
    ///   spin   0.0 1.6   The tsinelas fills the lens and spins away into the floodlit bowl.
    ///   crane  1.6 4.6   Up and back over the field: the whole stadium, packed.
    ///   crack  4.6 6.0   Low by the can: the tsinelas hits it and the can flies.
    /// The four seats are hidden; the can is a drawn-only copy so the real one never moves.
    /// </summary>
    public sealed class TrailerArenaFilm : MapFilm
    {
        public static readonly (string name, float from, float to)[] Shots = { ("spin", 0.0f, 1.6f), ("crane", 1.6f, 4.6f), ("crack", 4.6f, 6.0f) };
        public const float Length = 6.0f;

        protected override string Region => "TRAILER";
        protected override Color Accent => new Color(1.0f, 0.8f, 0.3f);
        protected override string Score => null;
        protected override float CloseAt(bool full) => 999.0f;

        private GameObject _slipper, _can;
        private Renderer[] _realCan = new Renderer[0];

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
            if (lata != null)
            {
                _can = Ghost(lata.gameObject, "Trailer can");
                _realCan = lata.GetComponentsInChildren<Renderer>();
                Hide(_realCan);
            }
        }

        protected override void Stage(float age)
        {
            for (int s = 0; s < Seats; s++) ShowSeat(s, false);
            if (age < 1.6f) Spin(age);
            else if (age < 4.6f) Crane(age - 1.6f);
            else Crack(age - 4.6f);
        }

        private void Spin(float t)
        {
            Beat = "spin";
            Vector3 eye = Centre + new Vector3(0.0f, 1.7f, -24.0f);
            Vector3 aim = Centre + new Vector3(0.0f, 9.0f, 60.0f);
            Vector3 dir = (aim - eye).normalized;
            float away = Mathf.Pow(t / 1.6f, 1.8f);
            Place(_slipper, eye + dir * Mathf.Lerp(0.32f, 22.0f, away) + Vector3.up * Mathf.Sin(away * Mathf.PI) * 1.5f, Quaternion.Euler(t * 520.0f + 200.0f, 25.0f, t * 140.0f));
            Place(_can, Centre, Quaternion.identity);
            Shoot(eye + Vector3.forward * t * 0.6f, aim, Mathf.Lerp(32.0f, 40.0f, Smooth(t / 1.6f)));
        }

        private void Crane(float t)
        {
            Beat = "crane";
            if (_slipper != null) _slipper.SetActive(false);
            Place(_can, Centre, Quaternion.identity);
            float p = Smooth(t / 3.0f);
            Vector3 eye = Centre + new Vector3(Mathf.Lerp(-6.0f, 8.0f, p), Mathf.Lerp(3.0f, 32.0f, p), Mathf.Lerp(-34.0f, -78.0f, p));
            Shoot(eye, Centre + Vector3.up * Mathf.Lerp(8.0f, 2.0f, p), Mathf.Lerp(46.0f, 58.0f, p));
        }

        private void Crack(float t)
        {
            Beat = "crack";
            const float Contact = 0.35f;
            Vector3 from = Centre + new Vector3(4.5f, 1.4f, 3.0f);
            if (t < Contact)
            {
                float k = t / Contact;
                Place(_slipper, Vector3.Lerp(from, Centre + Vector3.up * 0.2f, k), Quaternion.Euler(t * 900.0f, 40.0f, 0.0f));
                Place(_can, Centre, Quaternion.identity);
            }
            else
            {
                float k = t - Contact;
                Place(_slipper, Centre + new Vector3(-1.4f * k, 0.2f + 1.6f * k - 4.9f * k * k, 0.4f * k), Quaternion.Euler(t * 700.0f, 40.0f, 0.0f));
                Vector3 fly = new Vector3(-2.2f, 6.5f, -2.6f);
                Place(_can, Centre + fly * k + Vector3.down * 4.9f * k * k, Quaternion.Euler(k * 900.0f, k * 300.0f, k * 500.0f));
                if (k < 0.1f) Punch(1.0f);
            }
            Vector3 eye = Centre + new Vector3(1.1f, 0.45f, -1.9f);
            float up = Mathf.Max(0.0f, t - Contact);
            Shoot(eye + new Vector3(0.0f, up * 0.8f, -up * 1.2f), Centre + new Vector3(-0.9f * Mathf.Min(up, 0.6f), 0.35f + Mathf.Min(up, 0.55f) * 3.4f, -up * 1.2f), 34.0f + up * 10.0f);
        }

        private static void Place(GameObject thing, Vector3 at, Quaternion turn)
        {
            if (thing == null) return;
            thing.SetActive(true);
            thing.transform.SetPositionAndRotation(at, turn);
        }
    }
}
