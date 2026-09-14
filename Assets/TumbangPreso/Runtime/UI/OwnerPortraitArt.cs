using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.UI
{
    public static class OwnerPortraitArt
    {
        private static readonly Dictionary<string,Sprite> Cache=new Dictionary<string,Sprite>();
        public static UnityEngine.UI.Image Create(Transform parent,string name,string resource)
        {
            var image=OwnerUiLayout.Rect(parent,name).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite=Get(resource);image.preserveAspect=true;image.raycastTarget=false;image.color=Color.white;
            return image;
        }
        public static Sprite Get(string resource)
        {
            if(!Cache.TryGetValue(resource,out var sprite) || sprite==null)
            {
                sprite=Resources.Load<Sprite>(resource);
                if(sprite==null)
                {
                    var texture=Resources.Load<Texture2D>(resource);
                    if(texture!=null)sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
                }
                Cache[resource]=sprite;
            }
            return sprite;
        }
    }
}
