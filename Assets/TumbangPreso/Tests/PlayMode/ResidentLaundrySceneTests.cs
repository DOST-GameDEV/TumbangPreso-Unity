using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ResidentLaundrySceneTests
    {
        [UnitySetUp] public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After()=>PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator FourSupportedLinesBillowBelowFixedPegsAndRemainReadableFromTheStreet()
        {
            yield return MapRetrievalProbe.Load("Eskinita");
            var cloth=Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>f.name.StartsWith("Cloth_")).ToArray();
            Assert.AreEqual(23,cloth.Length);
            var before=cloth.Select(f=>f.sharedMesh.vertices).ToArray();
            var fixedParts=Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
                .Where(f=>f.name=="Fixed clothesline"||f.name.StartsWith("Peg_")||f.name.StartsWith("Supported rope hitch")).ToArray();
            // Static batching deliberately releases CPU vertex data for fixed pieces.
            // Their world transforms and rendered bounds remain the observable contract.
            var fixedBefore=fixedParts.Select(f=>(f.transform.position,f.transform.rotation,f.transform.lossyScale,f.GetComponent<Renderer>().bounds)).ToArray();
            yield return new WaitForSeconds(.25f);
            for(int c=0;c<cloth.Length;c++)
            {
                var after=cloth[c].sharedMesh.vertices;float moving=0;int pins=0;
                for(int v=0;v<after.Length;v++)
                {
                    float distance=Vector3.Distance(before[c][v],after[v]);moving=Mathf.Max(moving,distance);
                    if(Mathf.Abs(before[c][v].y)<.001f){pins++;Assert.Less(distance,.0001f,cloth[c].name+" top detached");}
                }
                Assert.Greater(pins,0);Assert.Greater(moving,.002f,cloth[c].name+" did not billow");
            }
            for(int i=0;i<fixedParts.Length;i++)
                Assert.AreEqual(fixedBefore[i],(fixedParts[i].transform.position,fixedParts[i].transform.rotation,
                    fixedParts[i].transform.lossyScale,fixedParts[i].GetComponent<Renderer>().bounds),fixedParts[i].name+" moved");
            var actor=GameServices.Round.PlayerAt(GameLaunch.SoloSeat);
            var rig=Object.FindObjectsByType<CameraRig>(FindObjectsSortMode.None).Single(r=>r.IsLocalFpp);
            yield return Look("street-wide",actor,rig,new Vector3(0,.2f,-18),new Vector3(0,4,3));
            yield return Look("east-pole-attachment",actor,rig,new Vector3(5.6f,.2f,1.1f),new Vector3(7.75f,4.45f,3));
            yield return Look("family-yard",actor,rig,new Vector3(-6.8f,.2f,-10),new Vector3(-9.04f,2,-8.178f));
            yield return Look("sheets-yard",actor,rig,new Vector3(6.8f,.2f,6.2f),new Vector3(9.04f,2.25f,8.8f));
        }

        private static IEnumerator Look(string name,CharacterMotor actor,CameraRig rig,Vector3 at,Vector3 target)
        {
            actor.Teleport(at);var direction=target-at;
            actor.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg,0);
            yield return null;
            direction=target-rig.Camera.transform.position;
            typeof(CameraRig).GetField("_pitchDeg",BindingFlags.Instance|BindingFlags.NonPublic)
                .SetValue(rig,-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg);
            yield return null;yield return null;
            Directory.CreateDirectory("Logs/laundry-review");
            var camera=rig.Camera;var previous=camera.targetTexture;var active=RenderTexture.active;
            var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();
                File.WriteAllBytes("Logs/laundry-review/"+name+".png",pixels.EncodeToPNG());
                Debug.Log("[LaundryFpp] "+name+" camera="+camera.transform.position.ToString("F3"));
            }
            finally{camera.targetTexture=previous;RenderTexture.active=active;rt.Release();Object.Destroy(rt);Object.Destroy(pixels);}
        }
    }
}
