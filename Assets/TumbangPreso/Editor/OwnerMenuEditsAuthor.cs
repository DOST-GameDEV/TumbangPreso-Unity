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
                File.Copy(source+"/login-background-woven.png",target+"/login-background.png",true);
                File.Copy(source+"/derived/main-sky-cutout.png",target+"/main-sky-cutout.png",true);
                File.Copy(source+"/derived/main-sky-background-data.png",target+"/main-sky-background-data.png",true);
                File.Copy(source+"/derived/main-ground-mask.png",target+"/main-ground-mask.png",true);
                foreach(var file in Directory.GetFiles(source+"/generated-clouds","*.png"))
                    File.Copy(file,target+"/"+Path.GetFileName(file),true);
                foreach(var file in Directory.GetFiles(source+"/login-v2","*.png"))
                    File.Copy(file,target+"/"+Path.GetFileName(file),true);
                File.Copy(source+"/login-v2/login-layout-v2.json",target+"/login-layout-v2.json",true);
                foreach(var file in Directory.GetFiles(source+"/extracted","*.png"))
                    File.Copy(file,target+"/"+Path.GetFileName(file),true);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                foreach(var file in Directory.GetFiles(target,"*.png"))
                {
                    string name=Path.GetFileName(file);
                    var importer=(TextureImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));
                    importer.textureType=TextureImporterType.Default;
                    // ⚠️⚠️ A MASK IS DATA AND A PICTURE IS COLOUR, AND IMPORTING ONE AS THE OTHER
                    // IS SILENT. An sRGB import applies a gamma curve to a coverage value, so a
                    // shadow authored at 0.5 arrives at about 0.73 and every strength the shader
                    // samples is wrong by an amount that reads as a tuning choice, not a defect.
                    bool mask=name=="main-sky-mask.png" || name=="main-sky-cutout.png"
                        || name=="main2-sky-mask.png" || name=="main2-shadow.png";
                    // ⚠️ A FULL-FRAME PLATE HAS NO ALPHA TO HONOUR. Marking one as transparency
                    // asks Unity to pre-multiply an opaque image and can shift its edges.
                    bool plate=name=="main-background.png" || name=="main2-background.png";
                    // ⚠️ MIPMAPS ONLY WHERE SOMETHING IS DRAWN SMALLER THAN IT WAS AUTHORED. The
                    // far cloud bank runs at 0.70 and a leaf at about half, and both shimmer
                    // without them; everything else is drawn at or above its own size, where a
                    // mipmap only costs sharpness.
                    bool minified=name.Contains("cloud-bank-") || name=="main2-cloud.png" || name=="main2-leaf.png";
                    importer.sRGBTexture=!mask;
                    importer.alphaSource=TextureImporterAlphaSource.FromInput;
                    importer.alphaIsTransparency=!plate && !mask;
                    importer.mipmapEnabled=minified;importer.textureCompression=TextureImporterCompression.Uncompressed;
                    importer.maxTextureSize=name.Contains("cloud-bank-")?4096:2048;
                    importer.npotScale=TextureImporterNPOTScale.None;
                    // ⚠️ CLAMP EVEN FOR THE DRIFTING SHADOW. `OwnerMenuAir` mirrors in the shader
                    // rather than relying on a wrap mode, so the seam behaviour is written down in
                    // one place instead of living in an import setting nobody reads.
                    importer.wrapMode=TextureWrapMode.Clamp;
                    importer.filterMode=minified?FilterMode.Trilinear:FilterMode.Bilinear;
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
