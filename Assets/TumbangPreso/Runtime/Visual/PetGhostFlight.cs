using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ THE PET GHOSTS FLY AROUND WHERE THEY ARE, AND ONLY IN THE DRAWING (owner on the first court film: *"they should be more
    /// lively, make them fly around in loops or something"*). Each ghost's skill keeps its own point exactly as before (the host's
    /// position, the trigger, the hold point, the can); this only says how far from that point the BODY is drawn, and which way it
    /// faces, so nothing here can move a hit, a pickup or a snapshot. Behind `PetGhostModels.PetGhosts` like the rest.
    ///
    /// Each has its own way of flying, all within about a metre of its point, all in arcs (no straight lines), each facing along
    /// its path and banking into its turns, the tail lagging behind (`Trail`, read by `Shaders/PetGhost`):
    ///  * MOUSE (DON'T LOOK): quick darting figure-eights round its hiding spot, and every few seconds a little loop-de-loop.
    ///  * BEAR (SKITTER): playful bobbing circles; the skill adds a happy spin when it grabs the slipper.
    ///  * OWL (LOOKOUT): a slow wide orbit with a glide up and down, and now and then a hop (a dip first, then up, then settle).
    /// `free` is how much it may wander: the skill sets 0 the moment it acts (springs, carries, lifts), and the flight eases back
    /// onto the exact point quickly (`Back`), then eases out again slowly when it is idle (`Out`).
    ///
    /// One instance per ghost, made once; `Apply` allocates nothing.
    /// </summary>
    public sealed class PetGhostFlight
    {
        public enum Kind { Mouse, Bear, Owl }

        private const float Back = 9f, Out = 1.4f;
        private readonly Kind _kind;
        private float _free, _seed;
        private bool _started;

        /// <summary>The tail's lag this frame, in the BODY's own space (metres at the tip): hand it to `NemuGhostBody.Trail`.</summary>
        public Vector3 TrailLocal { get; private set; }
        /// <summary>How much it is wandering right now (0 at its point, 1 flying freely).</summary>
        public float Free => _free;

        public PetGhostFlight(Kind kind, float seed = 0f) { _kind = kind; _seed = seed; }

        /// <summary>
        /// Moves the drawn <paramref name="at"/> and turns <paramref name="facing"/> by this ghost's flight at <paramref name="clock"/>
        /// seconds, scaled by how free it is. <paramref name="freeTarget"/> 0 means "at the point now".
        /// </summary>
        public void Apply(ref Vector3 at, ref Quaternion facing, float clock, float freeTarget, float dt)
        {
            if (!_started) { _free = freeTarget; _started = true; }
            float rate = freeTarget < _free ? Back : Out;
            _free = Mathf.MoveTowards(_free, Mathf.Clamp01(freeTarget), rate * Mathf.Max(dt, 0f) * (freeTarget < _free ? 1f : .7f));
            float f = _free * _free * (3f - 2f * _free);
            if (f <= .0005f) { TrailLocal = Vector3.zero; return; }

            // Its point's yaw is the frame the path is drawn in, so the loops turn with the skill's own facing.
            Vector3 ahead = facing * Vector3.forward; ahead.y = 0f;
            var frame = ahead.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(ahead.normalized, Vector3.up) : Quaternion.identity;
            float t = clock + _seed;
            const float h = 1f / 60f;
            Vector3 p0 = Path(t - h, out _), p1 = Path(t, out Vector3 loopUp), p2 = Path(t + h, out _);
            Vector3 v = (p2 - p0) / (2f * h), a = (p2 - 2f * p1 + p0) / (h * h);

            at += frame * (p1 * f);
            // Facing along the path, banking into the turn (the sideways pull), the head toward a loop's middle while it loops.
            Vector3 dir = v.sqrMagnitude > 1e-4f ? v.normalized : Vector3.forward;
            Vector3 flat = new Vector3(dir.x, 0f, dir.z);
            float bank = 0f;
            if (flat.sqrMagnitude > 1e-4f)
            {
                Vector3 side = Vector3.Cross(Vector3.up, flat.normalized);
                bank = Mathf.Clamp(-Vector3.Dot(a, side) * .035f, -34f, 34f);
            }
            Vector3 up = loopUp.sqrMagnitude > 1e-4f ? loopUp : Vector3.up;
            if (Mathf.Abs(Vector3.Dot(dir, up)) > .98f) up = Vector3.Cross(dir, Vector3.right);
            var fly = Quaternion.LookRotation(dir, up) * Quaternion.AngleAxis(bank, Vector3.forward);
            facing = Quaternion.Slerp(facing, frame * fly, f);
            // The tail drags behind its motion (opposite its velocity, in the body's own space), more the faster it goes.
            Vector3 worldLag = -(frame * v) * (.07f * f);
            if (worldLag.sqrMagnitude > .0225f) worldLag = worldLag.normalized * .15f;
            TrailLocal = Quaternion.Inverse(facing) * worldLag;
        }

        private static float EaseInOut(float u) => u * u * (3f - 2f * u);
        // Back-out: past the end a little, then settles.
        private static float Overshoot(float u) { u -= 1f; return 1f + u * u * (2.7f * u + 1.7f); }

        /// <summary>The offset from its point at time <paramref name="t"/> (its point's own frame, +z ahead), and while it loops, the way
        /// its head points.</summary>
        private Vector3 Path(float t, out Vector3 loopUp)
        {
            loopUp = Vector3.zero;
            switch (_kind)
            {
                case Kind.Mouse:
                {
                    // Darting: the speed surges and eases round each lobe of the figure-eight.
                    float w = 2f * Mathf.PI / 2.4f;
                    float th = w * t + .55f * Mathf.Sin(w * t);
                    var p = new Vector3(.42f * Mathf.Sin(th), .10f + .07f * Mathf.Sin(2f * th + .6f), .22f * Mathf.Sin(2f * th));
                    // Every 4.2 s a loop-de-loop along the way it is going, 0.8 s long, wound up a touch and overshot out.
                    float cycle = Mathf.Repeat(t, 4.2f);
                    if (cycle < .8f)
                    {
                        float u = cycle / .8f;
                        float phi = 2f * Mathf.PI * Mathf.Clamp01(Overshoot(EaseInOut(u)));
                        Vector3 along = new Vector3(.42f * Mathf.Cos(th), 0f, .44f * Mathf.Cos(2f * th));
                        along = along.sqrMagnitude > 1e-4f ? along.normalized : Vector3.forward;
                        const float r = .2f;
                        Vector3 centre = p + Vector3.up * r;
                        p += along * (r * Mathf.Sin(phi)) + Vector3.up * (r * (1f - Mathf.Cos(phi)));
                        loopUp = Vector3.Lerp(Vector3.up, (centre - p).normalized, Mathf.Sin(u * Mathf.PI));
                    }
                    return p;
                }
                case Kind.Bear:
                {
                    // Bouncy circles: each lap speeds up and slows, and it bobs three times a lap like a happy pup.
                    float w = 2f * Mathf.PI / 2.3f;
                    float th = w * t + .3f * Mathf.Sin(w * t);
                    float bob = .05f + .08f * Mathf.Abs(Mathf.Sin(1.5f * th));
                    return new Vector3(.34f * Mathf.Sin(th), bob, .30f * (Mathf.Cos(th) - 1f) + .15f);
                }
                default:
                {
                    // A slow wide orbit with a glide up and down; every 5.5 s a hop: a dip, up, overshoot, settle.
                    float w = 2f * Mathf.PI / 7.5f;
                    float th = w * t + .25f * Mathf.Sin(w * t);
                    float glide = .10f + .11f * Mathf.Sin(2f * th);
                    float c = Mathf.Repeat(t, 5.5f);
                    float hop = 0f;
                    if (c < .3f) hop = -.06f * Mathf.Sin(c / .3f * Mathf.PI);
                    else if (c < 1.3f) { float u = (c - .3f) / 1f; hop = .24f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(Overshoot(u) * .5f + u * .5f)) * (1f - u * .15f); }
                    return new Vector3(.60f * Mathf.Sin(th), glide + hop, .55f * (Mathf.Cos(th) - 1f) + .28f);
                }
            }
        }
    }
}
