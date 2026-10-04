using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    public static class SeanGateAuthor
    {
        public static void BindAuthoredClip()
        {
            const string path="Assets/TumbangPreso/Art/characters/persons/team-sean.glb";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .SingleOrDefault(c=>c.name=="hero-sean-gate");
            if(clip==null||clip.length<.7f)throw new InvalidOperationException("Authored Cinder Gate clip missing.");
            var art=RosterBook.Load()?.FindPersonArt("sean");
            if(art==null)throw new InvalidOperationException("Rago roster asset missing.");
            if(!(art.Clips??Array.Empty<AnimationClip>()).Contains(clip))
            {
                art.Clips=(art.Clips??Array.Empty<AnimationClip>()).Append(clip).ToArray();
                EditorUtility.SetDirty(art);AssetDatabase.SaveAssetIfDirty(art);
            }
            Debug.Log("[CinderGate] Bound one authored Rago clip; existing roster references preserved.");
        }
    }
}
