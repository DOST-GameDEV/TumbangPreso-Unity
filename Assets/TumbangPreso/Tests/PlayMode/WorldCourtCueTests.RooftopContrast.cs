using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed partial class WorldCourtCueTests
    {
        [UnityTest]
        public IEnumerator RooftopPreviewComparesOnlyCourtPaint()
        {
            StageWeights(1);
            var root=new GameObject("Roof paint preview",typeof(RectTransform),typeof(RawImage));
            var preview=root.AddComponent<MapPreviewSurface>();string shown=null;
            preview.MapShown+=map=>shown=map;preview.Show(SceneFlow.SaBubong);
            Material candidate=null;Renderer[] lines=null;Material[] originals=null;
            try
            {
                float until=Time.realtimeSinceStartup+40;
                while(shown!=SceneFlow.SaBubong&&Time.realtimeSinceStartup<until)yield return null;
                Assert.AreEqual(SceneFlow.SaBubong,shown);yield return null;yield return null;
                lines=SceneManager.GetSceneByName(SceneFlow.SaBubong).GetRootGameObjects()
                    .SelectMany(go=>go.GetComponentsInChildren<Renderer>(true))
                    .Where(r=>r.name=="Court X"||r.name=="Court Z"||r.name=="Throwing line").ToArray();
                Assert.AreEqual(6,lines.Length,"Only the six roof court lines belong in this study");
                originals=lines.Select(r=>r.sharedMaterial).ToArray();
                candidate=new Material(originals[0]);Time.timeScale=0;
                // No yield between variants: the preview orbit cannot advance.
                SavePreview(preview.Camera,"roof-paint-current");
                foreach(var line in lines)line.sharedMaterial=candidate;
                candidate.color=new Color(1,.94f,.78f);
                SavePreview(preview.Camera,"roof-paint-ivory");
                candidate.color=new Color(.23f,.19f,.15f);
                SavePreview(preview.Camera,"roof-paint-dark");
            }
            finally
            {
                if(originals!=null)for(int i=0;i<lines.Length;i++)if(lines[i]!=null)lines[i].sharedMaterial=originals[i];
                if(candidate!=null)Object.Destroy(candidate);Object.Destroy(root);Time.timeScale=1;
            }
        }
    }
}
