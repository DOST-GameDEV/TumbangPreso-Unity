using System;
using System.Collections.Generic;
using System.IO;
using TumbangPreso.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.EditorTools
{
    public static class OwnerUiAuthoring
    {
        public const string OverridePath="Assets/TumbangPreso/Resources/UI/owner-painted/OwnerUiOverrides.asset";
        [MenuItem("TUMP/UI/Select layout overrides")]
        public static void SelectOverrides()=>Selection.activeObject=EnsureOverrides();
        public static OwnerUiOverrideBook EnsureOverrides()
        {
            var book=AssetDatabase.LoadAssetAtPath<OwnerUiOverrideBook>(OverridePath);
            if(book==null){book=ScriptableObject.CreateInstance<OwnerUiOverrideBook>();AssetDatabase.CreateAsset(book,OverridePath);AssetDatabase.SaveAssets();}
            return book;
        }
        public static void PrepareOverrides()
        {
            try{EnsureOverrides();PrepareAppIcon();Debug.Log("[OwnerUI] Editable override book and original-art icon ready.");EditorApplication.Exit(0);}
            catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
        }
        public static void PrepareAppIcon()
        {
            // Copy original logo pixels into a transparent square. No redraw or nonuniform resize.
            var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
            var square=new Texture2D(407,407,TextureFormat.RGBA32,false);
            try
            {
                source.LoadImage(File.ReadAllBytes("ArtSource/ui/owner-handdrawn-2026-09-15/TUMP (5).png"));
                var rect=OwnerUiTheme.SourceRect(OwnerUiTheme.Piece.Logo);
                square.SetPixels(new Color[407*407]);
                square.SetPixels(0,(407-(int)rect.height)/2,(int)rect.width,(int)rect.height,
                    source.GetPixels((int)rect.x,source.height-(int)rect.y-(int)rect.height,(int)rect.width,(int)rect.height));
                square.Apply();const string path="Assets/TumbangPreso/Art/ui/brand/owner_app_icon.png";
                File.WriteAllBytes(path,square.EncodeToPNG());AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            }
            finally{UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(square);}
        }
        [Serializable]private sealed class Snapshot { public List<Element> elements=new List<Element>(); }
        [Serializable]private sealed class Element
        {
            public string canvas,path,text,font,ink,sprite,paperTreatment;
            public Vector2 position,size;
            public int fontSize;
        }
        [MenuItem("TUMP/UI/Export visible owner UI paths")]
        public static void ExportVisible()
        {
            var snapshot=new Snapshot();
            foreach(var marker in UnityEngine.Object.FindObjectsByType<OwnerUiCanvas>())
            {
                var canvas=marker.GetComponent<Canvas>();if(canvas==null)continue;
                foreach(var rect in canvas.GetComponentsInChildren<RectTransform>())
                {
                    if(rect==canvas.transform)continue;
                    var element=new Element{canvas=canvas.name,path=PathBelow(rect,canvas.transform),position=rect.anchoredPosition,size=rect.sizeDelta};
                    var text=rect.GetComponent<Text>();if(text!=null){element.text=text.GetComponentInParent<InputField>()==null?text.text:null;element.fontSize=text.fontSize;element.font=text.font!=null?AssetDatabase.GetAssetPath(text.font):"";}
                    var graphic=rect.GetComponent<Graphic>();if(graphic!=null)element.ink="#"+ColorUtility.ToHtmlStringRGBA(graphic.color);
                    var image=rect.GetComponent<Image>();if(image!=null && image.sprite!=null)element.sprite=AssetDatabase.GetAssetPath(image.sprite);
                    var paper=rect.GetComponent<OwnerUiPaper>();if(paper!=null)element.paperTreatment=paper.Style.ToString();
                    snapshot.elements.Add(element);
                }
            }
            Directory.CreateDirectory("Logs");const string path="Logs/owner-ui-authoring-paths.json";
            File.WriteAllText(path,JsonUtility.ToJson(snapshot,true));Debug.Log("[OwnerUI] Exported visible editable paths to "+path);
        }
        private static string PathBelow(Transform target,Transform root)
        {
            var parts=new List<string>();for(var current=target;current!=null && current!=root;current=current.parent)parts.Add(current.name);
            parts.Reverse();return string.Join("/",parts);
        }
    }
}
