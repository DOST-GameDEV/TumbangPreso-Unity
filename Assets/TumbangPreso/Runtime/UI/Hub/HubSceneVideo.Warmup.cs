using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace TumbangPreso.UI.Hub
{
    public sealed partial class HubSceneVideo
    {
        private const float PrepareFailureSeconds = 30;
        private static HubSceneVideo _preloaded;
        private static readonly Dictionary<string, VideoClip> Clips = new Dictionary<string, VideoClip>();
        private static readonly Dictionary<string, Texture2D> Posters = new Dictionary<string, Texture2D>();

        private static VideoClip LoadClip(string hero)
        {
            if (!Clips.TryGetValue(hero, out var clip)) Clips[hero] = clip = Resources.Load<VideoClip>(ClipPathFor(hero));
            return clip;
        }

        private static Texture2D LoadPoster(string hero)
        {
            if (!Posters.TryGetValue(hero, out var poster)) Posters[hero] = poster = Resources.Load<Texture2D>(PosterPathFor(hero));
            return poster;
        }

        public static IEnumerator Warmup(System.Action<float> completed = null)
        {
            if (_preloaded == null)
            {
                for (int i = 0; i < Heroes.Length; i++)
                {
                    if (!Clips.ContainsKey(Heroes[i]))
                    {
                        var load = Resources.LoadAsync<VideoClip>(ClipPathFor(Heroes[i]));
                        yield return load;
                        Clips[Heroes[i]] = load.asset as VideoClip;
                    }
                    completed?.Invoke(.35f * (i + 1) / Heroes.Length);
                    yield return null;
                }
                string hero = string.IsNullOrEmpty(ForcedHero) ? Pick(n => Random.Range(0, n)) : ForcedHero;
                yield return WarmPoster(hero);
                if (LoadClip(hero) == null || LoadPoster(hero) == null)
                { hero = "zack"; yield return WarmPoster(hero); }
                completed?.Invoke(.5f);
                // Another caller may have completed while this one awaited assets.
                if (_preloaded == null) _preloaded = Create(null, true, hero);
            }
            var video = _preloaded;
            float until = Time.unscaledTime + PrepareFailureSeconds;
            bool preparationReported = false;
            while (video != null && video.Player != null && !video.FirstFrameReady && !video._failed)
            {
                if (video.Prepared && !preparationReported)
                { preparationReported = true; completed?.Invoke(.75f); }
                if (Time.unscaledTime >= until)
                {
                    video.OnError(video.Player, "Preparation exceeded its failure budget; retaining the poster.");
                    break;
                }
                yield return null;
            }
            completed?.Invoke(1f);
        }

        private static IEnumerator WarmPoster(string hero)
        {
            if (Posters.ContainsKey(hero)) yield break;
            var load = Resources.LoadAsync<Texture2D>(PosterPathFor(hero));
            yield return load;
            Posters[hero] = load.asset as Texture2D;
        }

        private static HubSceneVideo TakePreloaded(RectTransform scene)
        {
            var video = _preloaded;
            if (video == null) return null;
            _preloaded = null;
            if (!string.IsNullOrEmpty(ForcedHero) && ForcedHero != video.Hero)
            { Destroy(video.gameObject); return null; }
            // A failed hidden preload cannot play. Home gets one fresh decoder
            // for the same scene while its poster covers preparation.
            if (video._failed)
            {
                string hero = video.Hero;
                Destroy(video.gameObject);
                return Create(scene, false, hero);
            }
            video.transform.SetParent(scene, false);
            video.gameObject.layer = scene.gameObject.layer;
            video.GetComponent<AspectRatioFitter>().enabled = true;
            video._preloading = false;
            return video;
        }
    }
}
