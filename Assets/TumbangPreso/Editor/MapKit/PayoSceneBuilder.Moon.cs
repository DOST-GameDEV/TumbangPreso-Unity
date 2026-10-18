using TumbangPreso.Map;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// THE NIGHT'S MOON (owner, 2026-10-19: "maybe instead of her showing at all times, do an easter egg similar to the
    /// balloon slipper in Arena where we have a cartoony moon with a cute face, hit it enough times and mayari appears in
    /// the sky"). Runs after `Night`: the art's always-shown Mayari (or the placeholder card) is switched off and taken out
    /// of `PayoNight`'s list, so she is no longer drawn at night; she is kept in the scene, unused, should the owner want
    /// her back. In her place `PayoMoon` (the cute moon Buwan, and Mayari of the stars as its reward) hangs over the
    /// valley's open end on the moon light's bearing, built at run time from `Resources/Map/PayoMoon/`.
    /// </summary>
    public static partial class PayoSceneBuilder
    {
        /// <summary>The moon's middle: degrees up, metres out, and its radius (the crescent about 12 degrees tall from the court, so its face reads; Mayari is drawn at `PayoMoon.MayariScale`).</summary>
        private const float MoonSkyElevation = 22.0f, MoonSkyDistance = 2900.0f, MoonSkyRadius = 320.0f;

        private static string Moon(Transform root)
        {
            var night = root.GetComponentInChildren<PayoNight>(true);
            if (night == null) { Warn("no PayoNight: no moon"); return "NO moon (no PayoNight)"; }
            int hidden = 0;
            foreach (var r in night.Mayari)
            {
                if (r == null) continue;
                r.gameObject.SetActive(false);
                hidden++;
            }
            night.Mayari = System.Array.Empty<Renderer>();

            var holder = new GameObject("Moon");
            holder.transform.SetParent(night.transform, false);
            var moon = holder.AddComponent<PayoMoon>();
            moon.Bearing = MoonYaw - 180.0f;
            moon.Elevation = MoonSkyElevation;
            moon.Distance = MoonSkyDistance;
            moon.Radius = MoonSkyRadius;
            moon.Court = new Vector3(0.0f, 1.7f, 0.0f);
            return $"the moon Buwan at bearing {moon.Bearing:0}, {MoonSkyElevation:0} degrees up, {MoonSkyDistance:0} m out, radius {MoonSkyRadius:0} m; " +
                   $"the old always-shown Mayari switched off ({hidden} renderers), she is the moon's reward now";
        }
    }
}
