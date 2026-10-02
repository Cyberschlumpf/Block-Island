using UnityEngine;
using System;

public static class PlayerProfileManager {
 const string P="VoxelBox.Profile.";
 static double sessionSeconds, fpsSum; static long fpsSamples; static float nextSave;
 public static void Tick(){ if(Time.unscaledDeltaTime<=0f)return; sessionSeconds+=Time.unscaledDeltaTime; float fps=1f/Time.unscaledDeltaTime; if(fps>0&&fps<2000){fpsSum+=fps;fpsSamples++;} if(Time.unscaledTime>=nextSave){nextSave=Time.unscaledTime+30f;SaveSession();}}
 public static void RecordPlaced(Block b){PlayerPrefs.SetInt(P+"Placed",Placed+1); string n=b!=null?b.name.ToLowerInvariant():""; if(n.Contains("stone")||n.Contains("stein")||n.Contains("cobble"))PlayerPrefs.SetInt(P+"Stone",Stone+1); if(n.Contains("wood")||n.Contains("holz")||n.Contains("oak")||n.Contains("plank")||n.Contains("tree"))PlayerPrefs.SetInt(P+"Wood",Wood+1);}
 public static void RecordRemoved(){PlayerPrefs.SetInt(P+"Removed",Removed+1);} public static void RecordPaste(int n){PlayerPrefs.SetInt(P+"Paste",Paste+n);} public static void RecordTnt(int n){PlayerPrefs.SetInt(P+"TNT",Tnt+n);}
 public static int Placed=>PlayerPrefs.GetInt(P+"Placed",0); public static int Removed=>PlayerPrefs.GetInt(P+"Removed",0); public static int Stone=>PlayerPrefs.GetInt(P+"Stone",0); public static int Wood=>PlayerPrefs.GetInt(P+"Wood",0); public static int Paste=>PlayerPrefs.GetInt(P+"Paste",0); public static int Tnt=>PlayerPrefs.GetInt(P+"TNT",0);
 public static double TotalSeconds=>PlayerPrefs.GetFloat(P+"Seconds",0)+sessionSeconds; public static double SessionSeconds=>sessionSeconds;
 public static float SessionFPS=>fpsSamples>0?(float)(fpsSum/fpsSamples):0f; public static float LifetimeFPS {get{float old=PlayerPrefs.GetFloat(P+"FpsAvg",0);int s=PlayerPrefs.GetInt(P+"FpsSessions",0);return s==0?SessionFPS:(old*s+SessionFPS)/(s+1);}}
 public static void SaveSession(){PlayerPrefs.SetFloat(P+"Seconds",(float)TotalSeconds);sessionSeconds=0;if(fpsSamples>0){int s=PlayerPrefs.GetInt(P+"FpsSessions",0);float old=PlayerPrefs.GetFloat(P+"FpsAvg",0);PlayerPrefs.SetFloat(P+"FpsAvg",s==0?SessionFPS:(old*s+SessionFPS)/(s+1));PlayerPrefs.SetInt(P+"FpsSessions",s+1);fpsSum=0;fpsSamples=0;}PlayerPrefs.Save();}
 public static string TimeText(double s){var t=TimeSpan.FromSeconds(s);return ((int)t.TotalHours)+" h "+t.Minutes.ToString("00")+" min";}
 public static string Hardware(){return SystemInfo.operatingSystem+"\nCPU: "+SystemInfo.processorType+" ("+SystemInfo.processorCount+" Kerne)\nGPU: "+SystemInfo.graphicsDeviceName+"\nRAM: "+SystemInfo.systemMemorySize+" MB\nGrafik-API: "+SystemInfo.graphicsDeviceType+"\nAuflösung: "+Screen.width+" × "+Screen.height;}
}

public class PlayerProfileRuntime : MonoBehaviour { static PlayerProfileRuntime i; [RuntimeInitializeOnLoadMethod] static void Boot(){if(i!=null)return;var g=new GameObject("VoxelBox Player Profile");DontDestroyOnLoad(g);i=g.AddComponent<PlayerProfileRuntime>();} void Update(){PlayerProfileManager.Tick();} void OnApplicationQuit(){PlayerProfileManager.SaveSession();} void OnApplicationPause(bool p){if(p)PlayerProfileManager.SaveSession();}}
