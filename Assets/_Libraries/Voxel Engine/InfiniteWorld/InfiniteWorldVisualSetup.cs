using UnityEngine;
using UnityEngine.Rendering;

// Graphics quality only. Time of Day owns the sun/light values.
public sealed class InfiniteWorldVisualSetup : MonoBehaviour {
 public float shadowDistance=250f;
 void Start(){Apply();Invoke(nameof(Apply),0.5f);Invoke(nameof(Apply),2f);}
 void Apply(){
  QualitySettings.shadows=VoxelBoxSettings.Shadows?ShadowQuality.All:ShadowQuality.Disable;
  QualitySettings.shadowResolution=VoxelBoxSettings.ShadowQualityLevel==0?ShadowResolution.Low:VoxelBoxSettings.ShadowQualityLevel==1?ShadowResolution.High:ShadowResolution.VeryHigh;
  QualitySettings.shadowDistance=VoxelBoxSettings.ShadowDistance; QualitySettings.shadowCascades=4; QualitySettings.shadowProjection=ShadowProjection.StableFit;
  var sky=TOD_Sky.Instance;
  if(sky!=null && sky.Components!=null && sky.Components.LightSource!=null){var l=sky.Components.LightSource;l.enabled=true;l.shadows=VoxelBoxSettings.Shadows?LightShadows.Soft:LightShadows.None;l.shadowBias=.03f;l.shadowNormalBias=.2f;l.cullingMask=~0;}
  GameObject root=GameObject.Find("InfiniteWorldRenderRoot"); if(root!=null)foreach(var rr in root.GetComponentsInChildren<Renderer>(true)){rr.shadowCastingMode=VoxelBoxSettings.Shadows?ShadowCastingMode.On:ShadowCastingMode.Off;rr.receiveShadows=VoxelBoxSettings.Shadows;}
 }
}
