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
            textures.Add(Texture2D.whiteTexture);textures.Add(Texture2D.blackTexture);
            textures.Add(Texture2D.grayTexture);textures.Add(Texture2D.normalTexture);
            var book=AssetDatabase.LoadAssetAtPath<SceneryTextureIdentities>(AssetPath);
            if(book==null){book=ScriptableObject.CreateInstance<SceneryTextureIdentities>();AssetDatabase.CreateAsset(book,AssetPath);}
            book.Entries=textures.OrderBy(t=>t.name,StringComparer.Ordinal).ThenBy(t=>t.width).ThenBy(t=>t.height)
                .Select(t=>new SceneryTextureIdentities.Entry{Texture=t,Hash=t.imageContentsHash.ToString()}).ToArray();
            EditorUtility.SetDirty(book);AssetDatabase.SaveAssets();
            Debug.Log("[SceneryTextureIdentities] prepared "+book.Entries.Length+" exact imported/builtin texture identities; no scene, model, texture or material was modified.");
        }
    }
}
