using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// BANK SHOT'S FLIGHT IN A MATCH: the approved b13d look (silk trail, circle head) on the real loaded tsinelas. Owner
    /// 2026-10-18: "zacks bankshot is good, lock it in"; and of this batch, "the ones that ive said are good to go should be
    /// put in game already". The drawing is the bench's own (`FxStudyZackBank.Live`); this only feeds it the shoe.
    ///
    /// ⚠️ LOOK ONLY, FROM WHAT EVERY PEER ALREADY HAS. It reads the shoe's `State`, `Affinity` (`BankShot` is one powered bank
    /// left, `OverclockBank` two), its position and its `Velocity`. The host simulates those and every client is sent them
    /// with the shoe's state, a late joiner included, so the host, a client and a spectator draw the same flight with no
    /// message of its own and no change to the wire. It decides nothing: the banks, their speed and their credit are
    /// `Slipper`'s.
    ///
    /// ⚠️ ONE SWITCH, <see cref="Silk"/>. On, the loaded flight wears the silk trail and head, and a powered bank is marked by
    /// the approved star and sparks. Off, everything is as it was: the two thin rails a client drew (`Slipper
    /// .CreateZapFlightVisual`) and the gold flick at the contact (`BankShotContact`). Read every frame, so it can be
    /// flipped in Play; flipped off mid-flight, what is drawn dissipates where it lies.
    ///
    /// Reached from `Slipper.Awake`, so every shoe carries one; it makes its pieces the first time its shoe flies loaded,
    /// and keeps them for the next throw.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(1600)]
    public sealed class ZackBankFlight : MonoBehaviour
    {
        /// <summary>The approved silk look (true, the default) or the old look (false).</summary>
        public static bool Silk = true;

        private Slipper _shoe;
        private FxStudyZackBank.Live _fx;

        public static void TrackWorld(Slipper shoe)
        {
            if (shoe == null) return;
            var flight = shoe.GetComponent<ZackBankFlight>();
            if (flight == null) flight = shoe.gameObject.AddComponent<ZackBankFlight>();
            flight._shoe = shoe;
        }

        /// <summary>The powered banks a shoe still carries.</summary>
        public static int PoweredLeft(SlipperAffinity affinity)
            => affinity == SlipperAffinity.OverclockBank ? 2 : affinity == SlipperAffinity.BankShot ? 1 : 0;

        private void LateUpdate()
        {
            if (_shoe == null) return;
            bool flying = Silk && _shoe.State == SlipperState.InFlight;
            int left = PoweredLeft(_shoe.Affinity);
            if (_fx != null && !_fx.Intact) { _fx.Dispose(); _fx = null; }
            if (_fx == null)
            {
                if (!flying || left == 0) return;
                _fx = new FxStudyZackBank.Live(null, gameObject.scene, gameObject.scene.GetPhysicsScene());
            }
            _fx.Observe(Time.time, transform.position, _shoe.Velocity, left, flying);
            var lens = Camera.main;
            _fx.Pose(Time.time, lens != null ? lens.transform.position : transform.position + new Vector3(0f, 1.6f, -6f));
        }

        // A shoe taken out of play mid-flight stops being updated: nothing of it may hang in the air.
        private void OnDisable() => _fx?.Hide();

        private void OnDestroy() { _fx?.Dispose(); _fx = null; }
    }
}
