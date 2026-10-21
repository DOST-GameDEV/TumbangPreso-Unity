using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.UI
{
    // Screen-scoped copies of the owner's supplied pixels, not a global reskin.
    public static class OwnerMenuArt
    {
        private static readonly Dictionary<string,Texture2D> Textures=new Dictionary<string,Texture2D>();
        private static readonly Dictionary<string,Sprite> Sprites=new Dictionary<string,Sprite>();
        private static readonly string[] TextureOnly = {
            "login-background", "main2-background"
        };
        private static readonly string[] PaintedPieces = {
            "login3-logo", "login3-tabs-track", "login3-tabs-pill", "login3-field-user",
            "login3-field-pass", "login3-field-confirm", "login3-checkbox", "login3-accepted-tick", "login3-key",
            "login3-primary", "login3-guest", "login3-google", "login3-rule-left",
            "login3-rule-right", "login3-eye-open", "login3-eye-shut", "login3-invalid"
        };
        public static Texture2D Texture(string name)
        {
            if(Textures.TryGetValue(name,out var texture) && texture!=null) return texture;
            texture=Resources.Load<Texture2D>("UI/owner-menu-edits/"+name);
            if(texture!=null) Textures[name]=texture;
            return texture;
        }
        public static IEnumerator Warmup()
        {
            for (int i = 0; i < TextureOnly.Length + PaintedPieces.Length; i++)
            {
                bool painted = i >= TextureOnly.Length;
                string name = painted ? PaintedPieces[i - TextureOnly.Length] : TextureOnly[i];
                if (!Textures.TryGetValue(name, out var texture) || texture == null)
                {
                    var request = Resources.LoadAsync<Texture2D>("UI/owner-menu-edits/" + name);
                    yield return request;
                    texture = request.asset as Texture2D;
                    if (texture != null) Textures[name] = texture;
                }
                else yield return null;
                if (painted && texture != null) Piece(name);
            }
        }
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
