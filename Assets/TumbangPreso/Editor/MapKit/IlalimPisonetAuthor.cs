using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>Fitted missing controls and quiet displays on the retained pisonet cabinets.</summary>
    public static class IlalimPisonetAuthor
    {
        public const string RootName="Pisonet controls and lower glazing";
        private const string Folder="Assets/TumbangPreso/Art/IlalimPisonet";
        private static readonly Color[] Colours={new Color(.74f,.73f,.66f),new Color(.22f,.27f,.27f),
            new Color(.46f,.50f,.49f),new Color(.82f,.79f,.66f),new Color(.26f,.39f,.46f),
            new Color(.59f,.62f,.59f),new Color(.51f,.24f,.19f),new Color(.60f,.56f,.43f)};
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity");var report=new StringBuilder();FinishLoadedScene(report);IlalimShopSignAuthor.FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/ilalim-pisonet");File.WriteAllText("Logs/ilalim-pisonet/author.txt",report.ToString());Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("IlalimNgTulay");var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var room=map.transform.Find("Dressing/PlaceRework/Frontage_Pisonet");if(room==null)throw new InvalidOperationException("Missing Pisonet frontage.");
            var prior=room.Find(RootName);if(prior!=null)Object.DestroyImmediate(prior.gameObject);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            foreach(var r in room.GetComponentsInChildren<MeshRenderer>())if(r.name=="Shopfront lower wall")r.enabled=false;
            var g=new Geometry();
            var screenSource=AssetDatabase.LoadAllAssetsAtPath("Assets/TumbangPreso/Art/models/env_pisonet_kiosk.obj").OfType<Material>().Single(m=>m.name=="piso_screen");
            var screenDraft=new Material(screenSource);screenDraft.color=new Color(.13f,.26f,.28f);
            screenDraft.DisableKeyword("_EMISSION");if(screenDraft.HasProperty("_EmissionColor"))screenDraft.SetColor("_EmissionColor",Color.black);
            screenDraft.SetOverrideTag("TumpPisonetQuietScreen","1");var screen=Save(screenDraft,Folder+"/QuietScreen.mat");
            int screens=0;
            foreach(Transform machine in room)if(machine.name.StartsWith("InteriorPisonet_"))
            {
                Vector3 P(float x,float y,float z)=>room.InverseTransformPoint(machine.TransformPoint(new Vector3(x,y,z)));
                var q=Quaternion.Inverse(room.rotation)*machine.rotation;
                void Box(Vector3 at,Vector3 size,int colour)=>g.Box(room.InverseTransformPoint(machine.TransformPoint(at)),Vector3.Scale(size,machine.localScale),colour,q);
                // Measured native OBJ coordinates; keyboard rests on its existing tray.
                Box(new Vector3(-.065f,.822f,-.43f),new Vector3(.64f,.035f,.26f),1);
                for(int row=0;row<3;row++)for(int key=0;key<9;key++)
                    Box(new Vector3(-.335f+key*.067f,.846f,-.51f+row*.072f),new Vector3(.052f,.014f,.052f),0);
                Box(new Vector3(-.065f,.846f,-.285f),new Vector3(.26f,.014f,.035f),0);
                Box(new Vector3(.365f,.827f,-.43f),new Vector3(.11f,.056f,.165f),1);
                Box(new Vector3(.365f,.857f,-.46f),new Vector3(.011f,.010f,.032f),2);
                g.Beam(P(.365f,.819f,-.35f),P(.39f,.819f,-.16f),.012f,1);
                g.Beam(P(.39f,.819f,-.16f),P(.30f,.819f,-.08f),.012f,1);
                // Coin slot and access latch fit the existing metal coin box, not a new device.
                Box(new Vector3(.32f,1.79f,-.239f),new Vector3(.098f,.019f,.015f),1);
                Box(new Vector3(.39f,1.79f,-.24f),new Vector3(.026f,.031f,.015f),2);
                // Quiet abstract desktop panes at the actual screen plane, no readable personal data.
                Box(new Vector3(-.105f,1.235f,-.056f),new Vector3(.30f,.24f,.008f),4);
                Box(new Vector3(-.105f,1.35f,-.063f),new Vector3(.30f,.019f,.008f),0);
                Box(new Vector3(.18f,1.19f,-.056f),new Vector3(.20f,.18f,.008f),2);
                for(int i=0;i<3;i++)Box(new Vector3(-.14f,1.27f-i*.051f,-.063f),new Vector3(.16f-i*.028f,.012f,.008f),0);
                foreach(var kioskRenderer in machine.GetComponentsInChildren<MeshRenderer>())
                {
                    var mats=kioskRenderer.sharedMaterials;
                    for(int i=0;i<mats.Length;i++)if(mats[i].name=="piso_screen"||mats[i].GetTag("TumpPisonetQuietScreen",false)=="1")
                    {mats[i]=screen;screens++;}
                    kioskRenderer.sharedMaterials=mats;
                }
            }
            if(screens!=2)throw new InvalidOperationException("Expected the two retained kiosk screens.");
            g.Box(new Vector3(0,.18f,.21f),new Vector3(5.68f,.23f,.025f),7);
            foreach(float y in new[]{.32f,.95f})g.Box(new Vector3(0,y,.21f),new Vector3(5.68f,.032f,.045f),2);
            foreach(float x in new[]{-2.81f,0,2.81f})g.Box(new Vector3(x,.635f,.21f),new Vector3(.032f,.63f,.045f),2);
            var face=room.Find("Sign face Pisonet");
            foreach(float dx in new[]{-.36f,.36f})g.Box(new Vector3(face.localPosition.x+face.localScale.x*dx,2.96f,.50f),new Vector3(.055f,.95f,.065f),2);
            var go=new GameObject(RootName);go.transform.SetParent(room,false);go.isStatic=true;
            var mesh=g.Build();int vertices=mesh.vertexCount;go.AddComponent<MeshFilter>().sharedMesh=Save(mesh,Folder+"/PisonetControls.asset");
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=Palette();renderer.shadowCastingMode=ShadowCastingMode.Off;
            AirborneByDesign.Attach(go,"Controls sit on measured original keyboard trays/coin boxes; display panes sit on native screens; lower glazing/fascia supports attach to existing frontage.");
            var glass=GameObject.CreatePrimitive(PrimitiveType.Cube);glass.name="Pisonet lower glazing";glass.transform.SetParent(go.transform,false);
            glass.transform.localPosition=new Vector3(0,.635f,.21f);glass.transform.localScale=new Vector3(5.64f,.59f,.008f);
            Object.DestroyImmediate(glass.GetComponent<Collider>());glass.isStatic=true;
            var glassRenderer=glass.GetComponent<MeshRenderer>();glassRenderer.sharedMaterial=room.Find("Glazed private shop boundary").GetComponent<MeshRenderer>().sharedMaterial;glassRenderer.shadowCastingMode=ShadowCastingMode.Off;
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Pisonet changed private boundary/gameplay collision.");
            report.AppendLine("Pisonet only: original cabinets/chairs/coin boxes retained, measured keyboard/mouse/coin-slot additions, quieter per-instance screens and lower glazing. "+vertices+" solid vertices plus glass; two added renderers, no new collider/shadow caster. Original source/importer and private collision unchanged.");
        }
        private static Material Palette()
        {
            var texture=new Texture2D(8,1,TextureFormat.RGBA32,false);texture.SetPixels(Colours);texture.Apply();string path=Folder+"/PisonetPalette.png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var i=(TextureImporter)AssetImporter.GetAtPath(path);i.npotScale=TextureImporterNPOTScale.None;i.filterMode=FilterMode.Point;i.mipmapEnabled=false;i.wrapMode=TextureWrapMode.Clamp;i.textureCompression=TextureImporterCompression.Uncompressed;i.SaveAndReimport();
            var m=new Material(Shader.Find("Standard")){color=Color.white,mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path)};m.SetFloat("_Glossiness",.10f);return Save(m,Folder+"/PisonetControls.mat");
        }
        private static T Save<T>(T draft,string path) where T:Object
        {var saved=AssetDatabase.LoadAssetAtPath<T>(path);if(saved==null){AssetDatabase.CreateAsset(draft,path);return draft;}EditorUtility.CopySerialized(draft,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(draft);return saved;}
        private sealed class Geometry
        {
            private readonly List<Vector3> points=new List<Vector3>();private readonly List<Vector2> uv=new List<Vector2>();private readonly List<int> indices=new List<int>();
            private void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int colour)
            {int n=points.Count;points.AddRange(new[]{a,b,c,d});indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});for(int i=0;i<4;i++)uv.Add(new Vector2((colour+.5f)/8,.5f));}
            public void Box(Vector3 at,Vector3 size,int colour,Quaternion? rotation=null)
            {
                var h=size*.5f;var q=rotation??Quaternion.identity;Vector3 P(float x,float y,float z)=>at+q*Vector3.Scale(h,new Vector3(x,y,z));
                Quad(P(-1,-1,1),P(1,-1,1),P(1,1,1),P(-1,1,1),colour);Quad(P(1,-1,-1),P(-1,-1,-1),P(-1,1,-1),P(1,1,-1),colour);
                Quad(P(-1,-1,-1),P(-1,-1,1),P(-1,1,1),P(-1,1,-1),colour);Quad(P(1,-1,1),P(1,-1,-1),P(1,1,-1),P(1,1,1),colour);
                Quad(P(-1,1,-1),P(-1,1,1),P(1,1,1),P(1,1,-1),colour);Quad(P(-1,-1,1),P(-1,-1,-1),P(1,-1,-1),P(1,-1,1),colour);
            }
            public void Beam(Vector3 a,Vector3 b,float thickness,int colour)=>Box((a+b)*.5f,new Vector3(thickness,Vector3.Distance(a,b),thickness),colour,Quaternion.FromToRotation(Vector3.up,b-a));
            public Mesh Build(){var m=new Mesh{name="Fitted pisonet keyboard mouse coin slot and screen panes"};m.SetVertices(points);m.SetUVs(0,uv);m.SetTriangles(indices,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
        }
    }
}
