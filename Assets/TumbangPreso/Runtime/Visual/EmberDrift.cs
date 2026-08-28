using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Lifts a small cube, tumbles it, and shrinks it away. One ember off a scorch mark.
    ///
    /// ⚠️⚠️ IT IS A COMPONENT ON A CUBE AND NOT A PARTICLE SYSTEM, AND THAT IS A MEASURED
    /// CONSTRAINT RATHER THAN LAZINESS. `ZackHeroKit` records what a per-disc emitter costs:
    /// *"one dash drops up to thirty of those, and thirty looping emitters is a different bug
    /// from the one this is for"*. Under the six-disc cap the worst case here is thirty cubes
    /// with a float each, which is nothing, and none of them allocates.
    ///
    /// ⚠️ HEAT IS THE ONLY THING IN THIS GAME THAT GOES UP, which is the whole reason the
    /// embers exist. `docs/VISION.md` § 2 rule 3 says the readability budget is spent on DETAIL
    /// rather than AREA, and vertical is the one direction a floor effect has spare in a 14 by
    /// 14 box that is already full. `DanteHeroKit`'s carapace uses the same negative-gravity
    /// trick on the body for the same reason.
    ///
    /// ⚠️ CUBES BECAUSE THE GAME IS VOXEL ART. A soft photographic flame would be the thing
    /// that looked broken here, not the thing that looked new; `docs/VISION.md` § 6 settles
    /// that his art is the design system. The billboard quads this replaced rendered as literal
    /// yellow rectangles, which `Logs/shots-abilities/ability_fire_trail_v1.png` shows plainly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmberDrift : MonoBehaviour
    {
        private Vector3 _rise;
        private Vector3 _spin;
        private Vector3 _bornScale;
        private float _life;
        private float _left;

        private void Start()
        {
            _bornScale = transform.localScale;

            // ⚠️ THE LIFE IS SHORTER THAN THE MARK'S. A scorch lives 3.0 s and an ember that
            // lived as long would still be hanging in the air over cold ground, which is the
            // opposite of what it is for: the embers say the burn is FRESH.
            _life = Random.Range(0.55f, 1.15f);
            _left = _life;

            _rise = new Vector3(Random.Range(-0.22f, 0.22f),
                                Random.Range(0.55f, 1.25f),
                                Random.Range(-0.22f, 0.22f));

            _spin = new Vector3(Random.Range(-160.0f, 160.0f),
                                Random.Range(-160.0f, 160.0f),
                                Random.Range(-160.0f, 160.0f));
        }

        private void Update()
        {
            _left -= Time.deltaTime;

            if (_left <= 0.0f)
            {
                // ⚠️ THE EMBER GOES, THE MARK STAYS. Destroying the whole hazard here would take
                // the scorch with it and cut the trail's life from 3.0 s to under one.
                Destroy(gameObject);
                return;
            }

            float t = 1.0f - _left / _life;

            transform.localPosition += _rise * Time.deltaTime;
            transform.Rotate(_spin * Time.deltaTime, Space.Self);

            // Shrinking rather than fading, because these are `VfxMaterial.Solid` and an opaque
            // material has no alpha to fade. A chip that gets smaller as it rises is what a real
            // ember does anyway.
            transform.localScale = _bornScale * Mathf.Clamp01(1.0f - t * t);
        }
    }
}
