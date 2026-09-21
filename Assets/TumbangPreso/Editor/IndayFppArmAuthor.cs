using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.CameraSystem;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools
{
    public static class IndayFppArmAuthor
    {
        public static void Run()
        {
            try
            {
                var entry=RosterBook.Load().FindPersonArt("inday");
                foreach(string side in new[]{"left","right"})
                {
                    var mesh=Extract(entry.Model,side);
                    string path="Assets/TumbangPreso/Resources/Models/FppDetails/inday_"+side+"_arm.asset";
                    var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if(existing==null)AssetDatabase.CreateAsset(mesh,path);
                    else{EditorUtility.CopySerialized(mesh,existing);EditorUtility.SetDirty(existing);Object.DestroyImmediate(mesh);}
                    var rosterMesh=ViewmodelArmAuthor.Extract(entry.Model,"arm-"+side);
                    string rosterPath=ViewmodelArmAuthor.Folder+"/inday_"+side+".asset";
                    var roster=AssetDatabase.LoadAssetAtPath<Mesh>(rosterPath);
                    if(roster==null)AssetDatabase.CreateAsset(rosterMesh,rosterPath);
                    else{EditorUtility.CopySerialized(rosterMesh,roster);EditorUtility.SetDirty(roster);Object.DestroyImmediate(rosterMesh);}
                }
                AssetDatabase.SaveAssets();EditorApplication.Exit(0);
            }
            catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
        }

        public static Mesh Extract(GameObject model,string side)
        {
            var mesh=ViewmodelArmAuthor.Extract(model,"arm-"+side);
            if(mesh==null)throw new InvalidOperationException("Inday's plain source arm is missing: "+side);
            foreach(var uv in mesh.uv)
            {
                int slot=Mathf.FloorToInt(uv.x*16)/2+(Mathf.FloorToInt(uv.y*16)<=3?8:0);
                if(slot!=14)throw new InvalidOperationException("Inday's arm still contains a sleeve, guard or prop: slot "+slot);
            }
            mesh.name="IndayPlainBrown_"+side+"_SourceArm";
            return mesh;
        }
    }
}
