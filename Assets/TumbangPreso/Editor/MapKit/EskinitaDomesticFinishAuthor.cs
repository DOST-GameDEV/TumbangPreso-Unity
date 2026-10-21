using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>A household window at3_W, preserving0_W's actual shop display.</summary>
    public static class EskinitaDomesticFinishAuthor
    {
        public const string RootName = "EskinitaDomestic3W";
        private const string Folder = "Assets/TumbangPreso/Art/EskinitaHouseFinishes/Domestic3W";
        private const string SourceTag = "TumpRefineOriginalMesh";

        public static void ClearPrevious(string map)
        {
            if (map != "Eskinita") return;
            var previous = GameObject.Find("Eskinita/Dressing/" + RootName);
            if (previous != null) Object.DestroyImmediate(previous);
        }

        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/Eskinita.unity");
            var report = new StringBuilder();
            EskinitaTimberFinishAuthor.FinishLoadedScene(report);
            FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Logs/eskinita-house-finish");
            File.WriteAllText("Logs/eskinita-house-finish/domestic3w.txt", report.ToString());
            Debug.Log(report.ToString()); EditorApplication.Exit(0);
        }

        public static void FinishLoadedScene(StringBuilder report)
        {
            var dressing = GameObject.Find("Eskinita/Dressing").transform;
            var details = dressing.Find("NeighborhoodRework/HouseFinish_Bahay_3_W");
            if (details == null) throw new InvalidOperationException("The measured3_Wdetail set is missing.");
            var filter = details.GetComponentInChildren<MeshFilter>();
            var renderer = filter.GetComponent<MeshRenderer>();
            var original = filter.sharedMesh;
            var previous = dressing.Find(RootName);
            if (previous != null)
            {
                string source = previous.GetComponent<MeshRenderer>().sharedMaterial.GetTag(SourceTag, false);
                original = AssetDatabase.LoadAssetAtPath<Mesh>(source);
                if (original == null) throw new InvalidOperationException("Lost original household detail mesh.");
                Object.DestroyImmediate(previous.gameObject);
            }
            string originalPath = AssetDatabase.GetAssetPath(original);
            if (string.IsNullOrEmpty(originalPath)) throw new InvalidOperationException("Original detail mesh must be persistent.");
            var colliders = dressing.GetComponentsInChildren<Collider>(true).ToDictionary(c => c, c => c.bounds);
            var root = new GameObject(RootName).transform;
            root.SetParent(dressing, false); root.SetPositionAndRotation(details.position, details.rotation);
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();

            var sourcePoints = original.vertices;
            var materials = renderer.sharedMaterials;
            var goods = new List<int>();
            var points = new List<Vector3>();
            var woodTops = new List<Vector3>();
            var matrix = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            for (int sub = 0; sub < original.subMeshCount; sub++)
            {
                string name = materials[sub].name;
                bool product = name.StartsWith("Cream goods", StringComparison.Ordinal) || name.StartsWith("Oxblood goods", StringComparison.Ordinal);
                if (product)
                {
                    goods.Add(sub);
                    points.AddRange(original.GetTriangles(sub).Select(i => matrix.MultiplyPoint3x4(sourcePoints[i])));
                }
                if (name.StartsWith("Warm aged timber", StringComparison.Ordinal))
                    woodTops.AddRange(original.GetTriangles(sub).Select(i => matrix.MultiplyPoint3x4(sourcePoints[i])));
            }
            if (goods.Count != 2 || points.Count == 0) throw new InvalidOperationException("Expected only the two sachet material regions.");
            var bounds = new Bounds(points[0], Vector3.zero); foreach (var p in points) bounds.Encapsulate(p);
            float sillY = woodTops.Where(p => p.y < bounds.min.y - .06f && p.y > .4f).Max(p => p.y);
            float outward = Mathf.Sign(bounds.center.z);
            float front = bounds.center.z + outward * .08f;
            float cx = bounds.center.x;
            var domestic = Object.Instantiate(original); domestic.name = "Eskinita3W_DomesticDetails";
            foreach (int sub in goods) domestic.SetTriangles(Array.Empty<int>(), sub);
            filter.sharedMesh = SaveMesh(domestic, Folder + "/DomesticDetails.asset");

            var vertices = new List<Vector3>(); var normals = new List<Vector3>();
            var uv = new List<Vector2>(); var triangles = new List<int>();
            float top = bounds.max.y + .08f;
            float width = bounds.size.x + .06f;
            // A raised shade uses the existing hanging rail. Large slats survive game scale.
            for (int row = 0; row < 7; row++)
                Box(new Vector3(cx, top - row * .055f, front), new Vector3(width, .047f, .048f), row % 3 == 0 ? 1 : 0);
            Box(new Vector3(cx, top + .055f, front), new Vector3(width + .06f, .11f, .115f), 0);
            foreach (float x in new[] { cx - width * .32f, cx + width * .32f })
                Box(new Vector3(x, top - .13f, front + outward * .033f), new Vector3(.02f, .38f, .016f), 1);

            // Readable open basket rests on the already supported sill, beside folded cloth.
            Vector3 basket = new Vector3(cx - width * .20f, sillY, front + outward * .12f);
            const float bw = .45f, bd = .27f, bh = .23f;
            Box(basket + Vector3.up * .016f, new Vector3(bw, .032f, bd), 1);
            for (int band = 0; band < 3; band++)
            {
                float y = sillY + .07f + band * .075f;
                foreach (float z in new[] { basket.z - bd * .5f, basket.z + bd * .5f })
                    Box(new Vector3(basket.x, y, z), new Vector3(bw + .025f, .034f, .027f), band == 2 ? 1 : 0);
                foreach (float x in new[] { basket.x - bw * .5f, basket.x + bw * .5f })
                    Box(new Vector3(x, y, basket.z), new Vector3(.027f, .034f, bd), band == 2 ? 1 : 0);
            }
            for (int i = 0; i < 7; i++)
                foreach (float z in new[] { basket.z - bd * .5f, basket.z + bd * .5f })
                    Box(new Vector3(basket.x - bw * .5f + i * bw / 6, sillY + bh * .5f, z), new Vector3(.021f, bh, .025f), 0);
            foreach (float x in new[] { basket.x - bw * .5f, basket.x + bw * .5f })
                for (int i = 0; i < 4; i++) Box(new Vector3(x, sillY + bh * .5f, basket.z - bd * .5f + i * bd / 3), new Vector3(.025f, bh, .021f), 0);
            Box(basket + new Vector3(0, .18f, 0), new Vector3(.34f, .06f, .20f), 2);
            Box(basket + new Vector3(.03f, .22f, 0), new Vector3(.28f, .03f, .18f), 2);
            Box(new Vector3(cx + width * .22f, sillY + .025f, basket.z), new Vector3(.35f, .05f, .24f), 3);
            Box(new Vector3(cx + width * .22f, sillY + .06f, basket.z - outward * .02f), new Vector3(.32f, .02f, .20f), 3);

            void Box(Vector3 center, Vector3 size, int color)
            {
                Vector3[] corners = { new Vector3(-1,-1,-1),new Vector3(1,-1,-1),new Vector3(1,1,-1),new Vector3(-1,1,-1),
                    new Vector3(-1,-1,1),new Vector3(1,-1,1),new Vector3(1,1,1),new Vector3(-1,1,1) };
                int[][] faces = { new[]{0,3,2,1},new[]{5,6,7,4},new[]{4,7,3,0},new[]{1,2,6,5},new[]{3,7,6,2},new[]{4,0,1,5} };
                foreach (var face in faces)
                {
                    var a=Vector3.Scale(corners[face[0]],size);var b=Vector3.Scale(corners[face[1]],size);var c=Vector3.Scale(corners[face[2]],size);
                    var normal=Vector3.Cross(b-a,c-a).normalized;int start=vertices.Count;
                    foreach(int index in face) { vertices.Add(center+Vector3.Scale(corners[index],size*.5f));normals.Add(normal);uv.Add(new Vector2((color%2+.5f)/2,(color/2+.5f)/2)); }
                    triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
                }
            }

            var mesh=new Mesh { name="Eskinita3W_ShadeAndBasket" };
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            root.gameObject.AddComponent<MeshFilter>().sharedMesh=SaveMesh(mesh,Folder+"/ShadeAndBasket.asset");
            var added=root.gameObject.AddComponent<MeshRenderer>();added.sharedMaterial=MakeMaterial(originalPath);
            root.gameObject.isStatic=true;
            AirborneByDesign.Attach(root.gameObject,"Raised shade fitted to the retained rail; basket and cloth rest on the measured existing household sill.");
            if(colliders.Count!=dressing.GetComponentsInChildren<Collider>(true).Length || colliders.Any(p=>p.Key==null||p.Key.bounds!=p.Value))
                throw new InvalidOperationException("Domestic detail changed original collision.");
            AssetDatabase.SaveAssets();
            report.AppendLine($"3_W only: removed2product submeshes, retained all other detail indices; {vertices.Count} added vertices/one renderer/material.");
            report.AppendLine($"Measured opening: {bounds}; sillY={sillY}; original mesh={originalPath}.0_Wshop and original masters unchanged.");
        }

        private static Mesh SaveMesh(Mesh draft,string path)
        {
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(draft,path);return draft;}
            EditorUtility.CopySerialized(draft,saved);Object.DestroyImmediate(draft);return saved;
        }

        private static Material MakeMaterial(string original)
        {
            string texturePath=Folder+"/DomesticPalette.png";
            var texture=new Texture2D(2,2,TextureFormat.RGB24,false);
            texture.SetPixels(new[]{new Color(.64f,.47f,.26f),new Color(.40f,.29f,.17f),new Color(.79f,.75f,.61f),new Color(.59f,.38f,.31f)});texture.Apply();
            File.WriteAllBytes(texturePath,texture.EncodeToPNG());Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.sRGBTexture=true;importer.SaveAndReimport();
            string path=Folder+"/Domestic3W.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("TumbangPreso/NearFade"));AssetDatabase.CreateAsset(material,path);}
            material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);material.color=Color.white;
            material.SetFloat("_SurfaceKind",0);material.SetFloat("_Glossiness",.12f);material.SetOverrideTag(SourceTag,original);
            EditorUtility.SetDirty(material);return material;
        }
    }
}
