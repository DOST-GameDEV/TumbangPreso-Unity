using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    public static class OwnerMenuEditsAuthor
    {
        public static void Prepare()
        {
            try
            {
                const string source="ArtSource/ui/owner-ui-edits-2026-09-15";
                const string target="Assets/TumbangPreso/Resources/UI/owner-menu-edits";
                Directory.CreateDirectory(target);
                File.Copy(source+"/background_mainmenu_clean.png",target+"/main-background.png",true);
                foreach(var file in Directory.GetFiles(source+"/extracted","*.png"))
                    File.Copy(file,target+"/"+Path.GetFileName(file),true);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                foreach(var file in Directory.GetFiles(target,"*.png"))
                {
                    var importer=(TextureImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));
                    importer.textureType=TextureImporterType.Default;
                    importer.sRGBTexture=true;importer.alphaSource=TextureImporterAlphaSource.FromInput;
                    importer.alphaIsTransparency=!file.EndsWith("main-background.png",StringComparison.Ordinal);
                    importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
                    importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;
                    importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
                    importer.SaveAndReimport();
                }
                AssetDatabase.SaveAssets();
                Debug.Log("[OwnerMenuEdits] Clean background and15original-size pieces imported without compression or sprite-metadata edits.");
                EditorApplication.Exit(0);
            }
            catch(Exception exception){Debug.LogException(exception);EditorApplication.Exit(1);}
        }
    }
}
