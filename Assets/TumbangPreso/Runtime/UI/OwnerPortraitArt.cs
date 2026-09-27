using System.Collections;
using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.UI
{
    public static class OwnerPortraitArt
    {
        private static readonly Dictionary<string,Sprite> Cache=new Dictionary<string,Sprite>();
        private static readonly string[] ModeCards =
            { "ClassicCard", "ClassicChoice", "CustomCard", "HeroStrikeChoice", "PracticeCard", "RankedCard" };

        public static IEnumerator Warmup(System.Action<float> completed = null)
        {
            var paths = new List<string>();
            var seen = new HashSet<string>();
            foreach (var entries in new[] { Roster.People, Roster.Cans, Roster.Slippers })
                foreach (var entry in entries)
                    if (entry != null && seen.Add(entry.Id)) paths.Add("UI/portraits/" + entry.Id);
            foreach (var card in ModeCards) paths.Add("UI/mode-cards/" + card);

            for (int i = 0; i < paths.Count; i++)
            {
                string path = paths[i];
                if (!Cache.TryGetValue(path, out var sprite) || sprite == null)
                {
                    var load = Resources.LoadAsync<Sprite>(path);
                    yield return load;
                    sprite = load.asset as Sprite;
                    if (sprite == null)
                    {
                        var textureLoad = Resources.LoadAsync<Texture2D>(path);
                        yield return textureLoad;
                        sprite = FromTexture(textureLoad.asset as Texture2D);
                    }
                    Cache[path] = sprite;
                }
                completed?.Invoke((i + 1f) / paths.Count);
                yield return null;
            }
        }

        private static Sprite FromTexture(Texture2D texture) => texture == null ? null
            : Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
        public static UnityEngine.UI.Image Create(Transform parent,string name,string resource)
        {
            var image=OwnerUiLayout.Rect(parent,name).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite=Get(resource);image.preserveAspect=true;image.raycastTarget=false;image.color=Color.white;
            return image;
        }
        public static Sprite Get(string resource)
        {
            if(string.IsNullOrEmpty(resource))return null;
            if(!Cache.TryGetValue(resource,out var sprite) || sprite==null)
            {
                sprite=Resources.Load<Sprite>(resource);
                if(sprite==null)
                {
                    var texture=Resources.Load<Texture2D>(resource);
                    sprite=FromTexture(texture);
                }
                Cache[resource]=sprite;
            }
            return sprite;
        }
    }
}
