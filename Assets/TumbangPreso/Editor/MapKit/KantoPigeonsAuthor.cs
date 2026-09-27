using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// KANTO'S PIGEONS (owner, 2026-09-27: "now it needs pigeons too, similar to how you made
    /// seagulls", then "no i wanna use a newer model for pigeons"). The Lagoon Cove's flock system
    /// (`LagoonFlocks`) with the fauna kit's new `fauna_pigeon` (tools/lagoon_prop_fauna.py), tuned
    /// for a city park instead of open water:
    ///   * two flocks of eight wheel over the park and the rooftops at 14 to 30 m, slower than the
    ///     terns (5 to 8 m/s, so a turn fits over a 26 m park) and steering around buildings
    ///     (AvoidObstacles: read-only sphere casts);
    ///   * they come down in GROUPS of two to five every 5 to 12 s, up to eight on the ground, peck
    ///     around the court and lawns, and scatter together when a player comes near;
    ///   * a thrown slipper bursts one into grey feathers, as with the seagulls;
    ///   * the wings fold swept back 84 degrees and rolled 22 over the flank (the pigeon fold study,
    ///     Logs/lagoon-blender/pigeon_fold_v4.png).
    /// Kanto has no sea, so the flock's "water" is the ground: the court clearance, sky band and
    /// where feathers settle are all measured from y = 0. Cosmetic and local only.
    /// </summary>
    internal static class KantoPigeonsAuthor
    {
        public static void Build(Transform root)
        {
            var life = new GameObject("Pigeons").transform;
            life.SetParent(root, false);
            var templates = new GameObject("Templates").transform;
            templates.SetParent(life, false);
            var flocks = life.gameObject.AddComponent<LagoonFlocks>();
            flocks.BirdTemplate = LagoonCoveLife.Template("fauna_pigeon", templates);
            flocks.FishTemplates = new Transform[0];
            flocks.SchoolCentres = new Vector3[0];
            flocks.SchoolFloor = new float[0];
            flocks.WaterY = 0f;
            flocks.SkyCentre = Vector3.zero;
            flocks.SkyRadius = 45f;
            flocks.SkyHeight = new Vector2(14f, 30f);
            flocks.Flocks = 2; flocks.BirdsPerFlock = 8;
            flocks.FlightSpeed = new Vector2(5f, 8f);
            flocks.CourtCentre = Vector3.zero; flocks.CourtKeepOut = 18f;
            flocks.CourtHalf = 13f; flocks.CourtGroundY = 0.12f;
            flocks.MaxGroundedBirds = 8;
            flocks.LandEverySeconds = new Vector2(5f, 12f);
            flocks.LandGroup = new Vector2Int(2, 5);
            flocks.AvoidObstacles = true;
            flocks.WingFoldSweep = 84f; flocks.WingFoldDroop = 22f;
            // The pigeon's own greys (fauna_pigeon_grey #a6a3a8, _pale #c6c3c7, _dark #5f5c63).
            flocks.FeatherMaterials = LagoonCoveLife.FeatherMaterials(
                ("feather_pigeon_grey", new Color(0.651f, 0.639f, 0.659f)),
                ("feather_pigeon_pale", new Color(0.776f, 0.765f, 0.780f)),
                ("feather_pigeon_dark", new Color(0.373f, 0.361f, 0.388f)));
            Debug.Log($"[Kanto] Pigeons: template {(flocks.BirdTemplate != null ? "fauna_pigeon" : "MISSING")}, " +
                      $"{flocks.Flocks} x {flocks.BirdsPerFlock}, {flocks.FeatherMaterials.Length} feather materials.");
        }
    }
}
