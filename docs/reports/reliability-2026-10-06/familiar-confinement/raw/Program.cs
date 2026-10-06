using System;using System.Collections.Generic;using System.Text.Json;using UnityEngine;using TumbangPreso;using TumbangPreso.Core;using TumbangPreso.Visual;
class Program {
 static int Main(){var results=new List<object>();int fail=0;bool oldRound=Confinement.Round;float oldRadius=Confinement.Radius;
 void Check(string name,bool ok,Vector3 actual){results.Add(new{name,passed=ok,x=actual.x,y=actual.y,z=actual.z});if(!ok)fail++;}
 try { var defender=new CharacterMotor{IsDefender=true};
 foreach(float radius in new[]{6.4f,8.6f,7.9f,9.3f,7.5f}) {Confinement.Use(true,radius);var p=GhostPetMotion.ClampToCourt(defender,new Vector3(radius*.9f,4.75f,radius*.9f));float length=MathF.Sqrt(p.x*p.x+p.z*p.z);Check("Circular defender diagonal radius "+radius,length<=radius+.0001f&&p.y==4.75f,p);}
 Confinement.Use(false,7);var sq=GhostPetMotion.ClampToCourt(defender,new Vector3(9,2,9));Check("Square defender corner remains unchanged",sq==new Vector3(7,2,7),sq);
 Confinement.Use(true,9.3f);var axis=GhostPetMotion.ClampToCourt(defender,new Vector3(2,3,4));Check("Circular already-inside point unchanged",axis==new Vector3(2,3,4),axis);
 var attacker=GhostPetMotion.ClampToCourt(new CharacterMotor{IsDefender=false},new Vector3(20,8,-20));Check("Attacker retains playable court branch",attacker==new Vector3(9,8,-8),attacker);
 var noOwner=GhostPetMotion.ClampToCourt(null,new Vector3(20,8,-20));Check("Ownerless retains playable court branch",noOwner==new Vector3(9,8,-8),noOwner);
 } finally {Confinement.Use(oldRound,oldRadius);}
 Console.WriteLine(JsonSerializer.Serialize(new{fail,cases=results,scope="Actual linked GhostPetMotion.ClampToCourt and Core.Confinement with role/map shims; Unity managed math; no native physics or integrated role acceptance"}));return fail==0?0:1; }
}
