using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    [Category("WallClock")]
    public sealed class KuroIdleReviewProbe
    {
        [UnityTest]
        public IEnumerator EachNamedIdleGestureOnTheActualFamiliar()
        {
            yield return PlayModeWorld.Reset();
            var target=new GameObject("Idle review origin");
            var source=Resources.Load<RosterBook>("RosterBook").People.First(p=>p.Id=="nemu").PetModel;
            Assert.IsNotNull(source);
            var pet=Object.Instantiate(source);
            var companion=pet.AddComponent<GhostPetCompanion>();companion.Bind(target.transform,Vector3.zero,CharacterVisual.PersonScale);
            companion.enabled=false; // The deterministic authoring sampler owns this review.
            ToonSkin.Apply(pet,ToonSkin.PersonOutlineWidth*.4f,null);
            foreach(var face in pet.GetComponentsInChildren<Renderer>())
                if(face.name.Contains("eye")||face.name.Contains("mouth"))ToonSkin.Apply(face,0,null);
            foreach(var node in pet.GetComponentsInChildren<Transform>())node.gameObject.layer=30;
            var sunObject=new GameObject("Idle review key");var sun=sunObject.AddComponent<Light>();
            sun.type=LightType.Directional;sun.intensity=.9f;sun.color=new Color(1,.96f,.90f);sun.cullingMask=1<<30;
            sun.transform.rotation=Quaternion.Euler(35,-35,0);
            var cameraObject=new GameObject("Idle review camera");var camera=cameraObject.AddComponent<Camera>();
            camera.enabled=false;camera.allowHDR=true;camera.cullingMask=1<<30;camera.fieldOfView=34;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.14f,.18f);camera.nearClipPlane=.01f;
            cameraObject.AddComponent<ColourGrade>();
            var oldAmbient=RenderSettings.ambientLight;var oldMode=RenderSettings.ambientMode;bool oldFog=RenderSettings.fog;
            try
            {
                RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.58f,.58f,.65f);RenderSettings.fog=false;
                companion.SampleIdleForCapture(GhostPetCompanion.FidgetState.None,0);
                var front=companion.MouthPosition-pet.transform.position;front.y=0;front.Normalize();
                camera.transform.position=front*.95f+Vector3.Cross(Vector3.up,front)*.18f+Vector3.up*.08f;
                camera.transform.LookAt(new Vector3(0,-.025f,0));
                foreach(GhostPetCompanion.FidgetState gesture in Enum.GetValues(typeof(GhostPetCompanion.FidgetState)))
                {
                    float duration=GhostPetCompanion.IdleGestureDuration(gesture);
                    yield return ImprovementEvidenceProbe.Record(camera,"kuro-idle-"+gesture,duration,null,
                        seconds=>Assert.IsTrue(companion.SampleIdleForCapture(gesture,seconds)));
                }
            }
            finally
            {
                RenderSettings.ambientLight=oldAmbient;RenderSettings.ambientMode=oldMode;RenderSettings.fog=oldFog;
                Object.DestroyImmediate(pet);Object.DestroyImmediate(target);Object.DestroyImmediate(cameraObject);Object.DestroyImmediate(sunObject);
            }
            yield return PlayModeWorld.Reset();
        }
    }
}
