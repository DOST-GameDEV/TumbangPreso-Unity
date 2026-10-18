using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.CameraSystem;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    public sealed class SceneryTextureIdentityBuild : IPreprocessBuildWithReport
    {
        public const string AssetPath="Assets/TumbangPreso/Resources/SceneryTextureIdentities.asset";
        public int callbackOrder=>-900;
        public void OnPreprocessBuild(BuildReport report)=>Prepare();
        public static void Prepare()
        {
            var textures=new HashSet<Texture>();
            var paths=AssetDatabase.GetAllAssetPaths().Where(p=>
                p.StartsWith("Assets/TumbangPreso/Art/models/ambient-life/",StringComparison.Ordinal)
                && p.EndsWith(".glb",StringComparison.OrdinalIgnoreCase)
                || p=="Assets/TumbangPreso/Art/Arena/Props/arena_drone.glb").OrderBy(p=>p,StringComparer.Ordinal);
            foreach(var path in paths)
            {
                foreach(var texture in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Texture>())textures.Add(texture);
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(model==null)continue;
                foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
                foreach(var material in renderer.sharedMaterials)
                    if(material!=null)foreach(var property in material.GetTexturePropertyNames())
                    {var texture=material.GetTexture(property);if(texture!=null)textures.Add(texture);}
            }
            textures.Remove(Texture2D.whiteTexture);textures.Remove(Texture2D.blackTexture);
            textures.Remove(Texture2D.grayTexture);textures.Remove(Texture2D.normalTexture);
            var book=AssetDatabase.LoadAssetAtPath<SceneryTextureIdentities>(AssetPath);
            if(book==null){book=ScriptableObject.CreateInstance<SceneryTextureIdentities>();AssetDatabase.CreateAsset(book,AssetPath);}
            book.WhiteHash=Texture2D.whiteTexture.imageContentsHash.ToString();
            book.BlackHash=Texture2D.blackTexture.imageContentsHash.ToString();
            book.GrayHash=Texture2D.grayTexture.imageContentsHash.ToString();
            book.NormalHash=Texture2D.normalTexture.imageContentsHash.ToString();
            book.Entries=textures.OrderBy(t=>t.name,StringComparer.Ordinal).ThenBy(t=>t.width).ThenBy(t=>t.height)
                .Select(t=>new SceneryTextureIdentities.Entry{Texture=t,Hash=t.imageContentsHash.ToString()}).ToArray();
            EditorUtility.SetDirty(book);AssetDatabase.SaveAssets();
            if(book.Entries.Any(e=>e.Texture==null||string.IsNullOrEmpty(e.Hash)))throw new BuildFailedException("Scenery texture identity has a missing imported reference.");
            foreach(var texture in new[]{Texture2D.whiteTexture,Texture2D.blackTexture,Texture2D.grayTexture,Texture2D.normalTexture})
                if(SceneryTextureIdentities.ResolvePrepared(texture,book)!=texture.imageContentsHash.ToString())throw new BuildFailedException("Builtin texture fingerprint changed during preparation.");
            Debug.Log("[SceneryTextureIdentities] prepared "+book.Entries.Length+" exact imported textures and 4 builtin scalar identities; no scene, model, texture or material was modified.");
        }
    }
}
