using UnityEngine;
// 1.0.28: RELATIVITY HOUSE is a pure free-flight level. No gravity sectors.
// The character/camera orientation defines the local capsule axis only; no force is applied.
public sealed class RelativityGravityController : MonoBehaviour {
 public static Vector3 Up=Vector3.up;
 public static float GravityFactor=0f;
 public static bool Active { get { return InfiniteWorldSave.CurrentWorldType==InfiniteWorldSave.WorldType.Relativity; } }
 public static bool ZeroGravity { get { return Active; } }
 public static bool SectorChangeGrace { get { return Active; } }
 void OnEnable(){ if(Active){ GravityFactor=0f; Up=transform.up; } }
 void Update(){ if(!Active){Up=Vector3.up;GravityFactor=1f;return;} GravityFactor=0f; Up=transform.up.normalized; }
 public static float Vertical(Vector3 v){return Vector3.Dot(v,Up);}
 public static Vector3 VerticalPart(Vector3 v){return Up*Vertical(v);}
 public static Vector3 Planar(Vector3 v){return v-VerticalPart(v);}
}
