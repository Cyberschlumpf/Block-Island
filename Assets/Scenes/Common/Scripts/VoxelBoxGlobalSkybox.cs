using UnityEngine;
using UnityEngine.SceneManagement;
// Menu/fallback skybox only. In Game, Time of Day owns sky and ambient lighting.
public static class VoxelBoxGlobalSkybox {
 static Material sky;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Install(){Apply();SceneManager.sceneLoaded-=OnSceneLoaded;SceneManager.sceneLoaded+=OnSceneLoaded;}
 static void OnSceneLoaded(Scene scene,LoadSceneMode mode){Apply();}
 static void Apply(){
  if(SceneManager.GetActiveScene().name=="Game" || TOD_Sky.Instance!=null) return;
  if(sky==null)sky=Resources.Load<Material>("Skybox/DayInTheClouds");
  if(sky!=null && RenderSettings.skybox!=sky){RenderSettings.skybox=sky;DynamicGI.UpdateEnvironment();}
 }
}
