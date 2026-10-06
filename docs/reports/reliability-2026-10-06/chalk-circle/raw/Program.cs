using System;using System.Collections.Generic;using System.Text.Json;using UnityEngine;using TumbangPreso.Core;using TumbangPreso.Visual;
class Program {
 static int Main(){var cases=new List<object>();int fail=0;bool round=Confinement.Round;float old=Confinement.Radius;
 void Check(string name,bool ok,bool hit,Vector3 at){cases.Add(new{name,passed=ok,hit,x=at.x,y=at.y,z=at.z});if(!ok)fail++;}
 try {foreach(float radius in new[]{6.4f,8.6f,7.9f,9.3f,7.5f}) {Confinement.Use(true,radius);float k=1/MathF.Sqrt(2);
 var from=new Vector3((radius-.2f)*k,2,(radius-.2f)*k);var to=new Vector3((radius+.2f)*k,4,(radius+.2f)*k);bool hit=MotionFoley.TryChalkCrossing(from,to,out var at);
 Check("Round diagonal crossing "+radius,hit&&MathF.Abs(MathF.Sqrt(at.x*at.x+at.z*at.z)-radius)<.001f&&MathF.Abs(at.y-3)<.001f,hit,at);
 hit=MotionFoley.TryChalkCrossing(new Vector3(radius-.2f,0,radius-.2f),new Vector3(radius+.2f,0,radius-.2f),out at);Check("No phantom square edge "+radius,!hit,hit,at);}
 Confinement.Use(true,7);bool h=MotionFoley.TryChalkCrossing(new Vector3(6.8f,0,0),new Vector3(7.2f,0,0),out var q);Check("Axis round crossing preserved",h&&MathF.Abs(q.x-7)<.001f,h,q);
 h=MotionFoley.TryChalkCrossing(Vector3.zero,Vector3.zero,out q);Check("Stationary has no crossing",!h,h,q);
 h=MotionFoley.TryChalkCrossing(Vector3.zero,Vector3.right,out q);Check("Interior has no crossing",!h,h,q);
 h=MotionFoley.TryChalkCrossing(new Vector3(-8,2,0),new Vector3(8,6,0),out q);Check("Round pass-through uses first contact",h&&MathF.Abs(q.x+7)<.001f&&MathF.Abs(q.y-2.25f)<.001f,h,q);
 h=MotionFoley.TryChalkCrossing(new Vector3(-1,0,7),new Vector3(1,0,7),out q);Check("Round tangent is not crossing",!h,h,q);
 h=MotionFoley.TryChalkCrossing(new Vector3(7,0,0),new Vector3(8,0,0),out q);Check("Starting on line does not repeat contact",!h,h,q);
 h=MotionFoley.TryChalkCrossing(new Vector3(6.99f,0,0),new Vector3(7.01f,0,0),out q);Check("Short crossing remains stable",h&&MathF.Abs(q.x-7)<.0001f,h,q);
 Confinement.Use(false,7);h=MotionFoley.TryChalkCrossing(new Vector3(6.8f,0,6.8f),new Vector3(7.2f,0,6.8f),out q);Check("Square corner crossing preserved",h&&MathF.Abs(q.x-7)<.001f,h,q);
 h=MotionFoley.TryChalkCrossing(new Vector3(6.8f,0,9),new Vector3(7.2f,0,9),out q);Check("Square beyond edge rejected",!h,h,q);
 }finally{Confinement.Use(round,old);}Console.WriteLine(JsonSerializer.Serialize(new{fail,cases,scope="Actual source-linked MotionFoley.TryChalkCrossing and Core; non-invoked audio/actor dependencies shimmed. No native audio or physics acceptance."}));return fail==0?0:1;}
}
