using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Turns a quad to face the camera every frame, about the Y axis only.
    ///
    /// ⚠️⚠️ Y ONLY, AND THAT IS THE WHOLE DESIGN. A full look-at billboard tips the quad back
    /// when the camera is above it, so a flame lying near the floor rotates until the player is
    /// looking at it edge-on from a metre away and it vanishes. Constraining the spin to the
    /// vertical axis keeps every flame standing UP out of the ground, which is the one property
    /// that made the ember columns worth adding to the fire trail in the first place: vertical
    /// is the only direction a floor effect has spare.
    ///
    /// ⚠️ IT USES `Camera.main` AND SURVIVES NOT FINDING ONE. These are attached by
    /// `HeroHazards` inside ability activations, several of which run in EditMode tests with no
    /// camera in the scene at all.
    ///
    /// `docs/Hero_Strike_Balance.md` § 3.2.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Billboard : MonoBehaviour
    {
        private Transform _view;

        private void LateUpdate()
        {
            if (_view == null)
            {
                var cam = Camera.main;
                if (cam == null) return;
                _view = cam.transform;
            }

            Vector3 to = _view.position - transform.position;
            to.y = 0.0f;

            if (to.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.LookRotation(-to.normalized, Vector3.up);
        }
    }
}
