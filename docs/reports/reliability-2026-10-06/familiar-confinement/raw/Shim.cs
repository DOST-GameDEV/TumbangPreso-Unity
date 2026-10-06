using UnityEngine;
namespace TumbangPreso {
 public class CharacterMotor { public bool IsDefender; public Transform transform; public T GetComponent<T>() where T:class =>null; }
 public class Slipper {}
 public static class AIController { public static Vector3 ClampToPlayable(Vector3 p)=>new Vector3(Mathf.Clamp(p.x,-5,9),p.y,Mathf.Clamp(p.z,-8,15)); }
}
namespace TumbangPreso.Visual { public static class VfxShapes { public static Vector3 GroundPoint(Vector3 p)=>p; } }
