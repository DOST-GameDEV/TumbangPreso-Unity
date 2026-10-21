using UnityEngine;
namespace TumbangPreso {
 public class CharacterMotor {public int PresentationTeleportSerial;public bool IsGrounded,RoundActive,IsSwimming,IsStunned,IsPerson;public Vector3 Velocity;public Powers AbilitySystem;}
 public class Powers{public Kit Kit;}public class Kit{public Skill Skill1;}public class Skill{public bool IsActive;}
 public class CombatVerbs{public bool SlideActive;}
 public class Sounds{public void PlayAtVaried(string cue,Vector3 at,float a,float b,float c){}}
 public static class GameServices{public static Sounds Audio=>null;}
 public static class RooftopPool{public static bool TrySurface(Vector3 p,out float y){y=0;return false;}}
 public class LagoonWater{public static LagoonWater Instance=>null;public bool Active;}
}
namespace TumbangPreso.Visual {
 public class CharacterAnimator{public bool IsPlayingAction;public string CurrentClipName;public float FootfallCycleMetres;public GaitStyles Style;}
 public enum GaitStyles{Amihan}public class WorldCueProfile{public static WorldCueProfile Current=>new WorldCueProfile();public float InkEffects=>1;}
}
