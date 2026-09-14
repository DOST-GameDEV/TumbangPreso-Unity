using UnityEngine;

namespace TumbangPreso.UI
{
    [RequireComponent(typeof(UnityEngine.UI.RawImage))]
    public sealed class OwnerUiBackdrop : MonoBehaviour
    {
        private UnityEngine.UI.RawImage _image;
        private void Awake()
        {
            _image=GetComponent<UnityEngine.UI.RawImage>();_image.texture=OwnerUiTheme.Current.Background;
            _image.raycastTarget=false;_image.color=Color.white;
        }
        private void LateUpdate()
        {
            var texture=_image.texture;if(texture==null)return;
            var size=_image.rectTransform.rect.size;if(size.x<=0 || size.y<=0)return;
            float source=texture.width/(float)texture.height,screen=size.x/size.y;
            var uv=screen>source?new Vector2(1,source/screen):new Vector2(screen/source,1);
            bool reduced=Settings.SettingsStore.Current.ReducedUiMotion;
            if(!reduced)uv/=1.012f;
            var centre=Vector2.one*.5f;
            if(!reduced)centre+=new Vector2(Mathf.Sin(Time.unscaledTime*.085f),Mathf.Cos(Time.unscaledTime*.07f))*.002f;
            _image.uvRect=new Rect(centre-uv*.5f,uv);
        }
        public static void Build(Transform parent)
        {
            var rect=OwnerUiLayout.Rect(parent,"OwnerPattern");OwnerUiLayout.Fill(rect);
            rect.gameObject.AddComponent<UnityEngine.UI.RawImage>();rect.gameObject.AddComponent<OwnerUiBackdrop>();
        }
    }
}
