using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using TumbangPreso.UI;

namespace TumbangPreso.EditorTools
{
    // Explicit source import. Sprite regions are runtime references to original
    // pixels; no generated bitmap, sprite-metadata edits or redraw is involved.
    public static class OwnerUiArtAuthor
    {
        public static void Prepare()
        {
            try
            {
                const string folder="Assets/TumbangPreso/Resources/UI/owner-painted";
                Directory.CreateDirectory(folder);
                Copy("TUMP (3).png",folder+"/background.png");
                Copy("TUMP (5).png",folder+"/artwork.png");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                foreach(var file in new[]{"background.png","artwork.png"})
                {
                    var importer=(TextureImporter)AssetImporter.GetAtPath(folder+"/"+file);
                    importer.textureType=TextureImporterType.Default;
                    importer.sRGBTexture=true;importer.alphaSource=TextureImporterAlphaSource.FromInput;
                    importer.alphaIsTransparency=file=="artwork.png";
                    importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
                    importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;
                    importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
                    importer.SaveAndReimport();
                }
                string path=folder+"/OwnerUiTheme.asset";
                var theme=AssetDatabase.LoadAssetAtPath<OwnerUiTheme>(path);
                if(theme==null){theme=ScriptableObject.CreateInstance<OwnerUiTheme>();AssetDatabase.CreateAsset(theme,path);}
                theme.Artwork=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/artwork.png");
                theme.Pattern=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/background.png");
                const string fonts="Assets/TumbangPreso/Resources/UI/fonts/";
                theme.DisplayFont=AssetDatabase.LoadAssetAtPath<Font>(fonts+"DarumadropOne-Regular.ttf");
                theme.AccentFont=AssetDatabase.LoadAssetAtPath<Font>(fonts+"KawitExtended.ttf");
                theme.ReadingFont=AssetDatabase.LoadAssetAtPath<Font>(fonts+"Lydian-Regular.ttf");
                EditorUtility.SetDirty(theme);AssetDatabase.SaveAssets();
                Debug.Log("[OwnerUI] Original1920x1080 sources imported losslessly; exact fonts and theme assigned.");
                EditorApplication.Exit(0);
            }
            catch(Exception exception){Debug.LogException(exception);EditorApplication.Exit(1);}
        }
        private static void Copy(string name,string destination)
        {
            string source="ArtSource/ui/owner-handdrawn-2026-09-15/"+name;
            if(!File.Exists(source))throw new FileNotFoundException("Owner source missing",source);
            File.Copy(source,destination,true);
        }
    }
}
