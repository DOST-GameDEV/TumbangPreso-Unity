using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.UI
{
    // Screen-scoped copies of the owner's supplied pixels, not a global reskin.
    public static class OwnerMenuArt
    {
        private static readonly Dictionary<string,Sprite> Sprites=new Dictionary<string,Sprite>();
        public static Texture2D Texture(string name)=>Resources.Load<Texture2D>("UI/owner-menu-edits/"+name);
        public static Sprite Piece(string name)
        {
            if(Sprites.TryGetValue(name,out var sprite) && sprite!=null) return sprite;
            var texture=Texture(name);if(texture==null) return null;
            sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            sprite.name="OwnerMenu_"+name;sprite.hideFlags=HideFlags.DontSave;Sprites[name]=sprite;
            return sprite;
        }
        public static UnityEngine.UI.Image Image(Transform parent,string name,string piece)
        {
            var image=OwnerUiLayout.Rect(parent,name).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite=Piece(piece);image.preserveAspect=true;image.raycastTarget=false;image.color=Color.white;
            if(image.sprite!=null) image.rectTransform.sizeDelta=image.sprite.rect.size;
            return image;
        }
    }
}
