using UnityEngine;
public static class VoxelBoxSettings {
 public static bool Shadows{get=>PlayerPrefs.GetInt("VB.Shadows",1)==1;set{PlayerPrefs.SetInt("VB.Shadows",value?1:0);Apply();}}
 public static int ShadowQualityLevel{get=>PlayerPrefs.GetInt("VB.ShadowQuality",2);set{PlayerPrefs.SetInt("VB.ShadowQuality",Mathf.Clamp(value,0,2));Apply();}}
 public static float ShadowDistance{get=>PlayerPrefs.GetFloat("VB.ShadowDistance",220f);set{PlayerPrefs.SetFloat("VB.ShadowDistance",Mathf.Clamp(value,40f,500f));Apply();}}
 public static bool PostProcessing{get=>PlayerPrefs.GetInt("VB.PostProcessing",1)==1;set{PlayerPrefs.SetInt("VB.PostProcessing",value?1:0);Apply();}}
 public static bool Bloom{get=>PlayerPrefs.GetInt("VB.Bloom",1)==1;set{PlayerPrefs.SetInt("VB.Bloom",value?1:0);Apply();}}
 public static float BloomIntensity{get=>PlayerPrefs.GetFloat("VB.BloomIntensity",1.1f);set{PlayerPrefs.SetFloat("VB.BloomIntensity",Mathf.Clamp(value,0f,5f));Apply();}}
 public static bool AmbientOcclusion{get=>PlayerPrefs.GetInt("VB.AO",1)==1;set{PlayerPrefs.SetInt("VB.AO",value?1:0);Apply();}}
 public static int AntiAliasing{get=>PlayerPrefs.GetInt("VB.AA",2);set{PlayerPrefs.SetInt("VB.AA",Mathf.Clamp(value,0,3));Apply();}}
 public static bool VSync{get=>PlayerPrefs.GetInt("VB.VSync",1)==1;set{PlayerPrefs.SetInt("VB.VSync",value?1:0);Apply();}}
 public static bool WaterReflection{get=>PlayerPrefs.GetInt("VB.WaterReflection",1)==1;set{PlayerPrefs.SetInt("VB.WaterReflection",value?1:0);Apply();}}
 public static bool WaterEffects{get=>PlayerPrefs.GetInt("VB.WaterEffects",1)==1;set{PlayerPrefs.SetInt("VB.WaterEffects",value?1:0);Apply();}}
 public static bool WeatherEffects{get=>PlayerPrefs.GetInt("VB.WeatherEffects",1)==1;set{PlayerPrefs.SetInt("VB.WeatherEffects",value?1:0);Apply();}}
 public static float Brightness{get=>PlayerPrefs.GetFloat("VB.Brightness",1f);set{PlayerPrefs.SetFloat("VB.Brightness",Mathf.Clamp(value,.65f,1.35f));Apply();}}
 public static int ViewDistance{get=>PlayerPrefs.GetInt("VB.ViewDistance",16);set{PlayerPrefs.SetInt("VB.ViewDistance",Mathf.Clamp(value,4,32));PlayerPrefs.Save();}}
 public static void Apply(){
  QualitySettings.shadows=Shadows?ShadowQuality.All:ShadowQuality.Disable;
  QualitySettings.shadowResolution=ShadowQualityLevel==0?ShadowResolution.Low:ShadowQualityLevel==1?ShadowResolution.High:ShadowResolution.VeryHigh;
  QualitySettings.shadowDistance=ShadowDistance; QualitySettings.shadowCascades=Shadows?4:0;
  QualitySettings.vSyncCount=VSync?1:0;
  Shader.SetGlobalFloat("_VoxelBoxWaterEffects",WaterEffects?1f:0f); Shader.SetGlobalFloat("_VoxelBoxWaterReflection",WaterReflection?1f:0f); Shader.SetGlobalFloat("_VoxelBoxBrightness",Brightness);
  foreach(var p in Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None))p.enabled=WaterReflection;
  VoxelBoxGraphicsRuntime.ApplyCurrent(); PlayerPrefs.Save();
 }
}
