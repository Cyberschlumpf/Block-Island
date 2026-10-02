using UnityEngine;
using System;
using System.IO;

// The Voxel Box 0.5 - H captures a PNG to an easy-to-find Screenshots folder
// beside the standalone game. In the Unity Editor it uses the project root.
public sealed class VoxelBoxScreenshot : MonoBehaviour {
    private string lastPath="";
    private float messageUntil=0f;

    void Update() {
        if(Input.GetKeyDown(KeyCode.H)) Capture();
    }

    public static string GameDirectory {
        get {
            try { return Path.GetFullPath(Path.Combine(Application.dataPath,"..")); }
            catch { return Application.persistentDataPath; }
        }
    }
    public static string ScreenshotDirectory {
        get {
            string p=Path.Combine(GameDirectory,"Screenshots");
            try { Directory.CreateDirectory(p); return p; }
            catch { p=Path.Combine(Application.persistentDataPath,"Screenshots"); Directory.CreateDirectory(p); return p; }
        }
    }
    public void Capture() {
        string name="VoxelBox_"+DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff")+".png";
        lastPath=Path.Combine(ScreenshotDirectory,name);
        ScreenCapture.CaptureScreenshot(lastPath);
        messageUntil=Time.unscaledTime+3f;
        Debug.Log("Screenshot gespeichert: "+lastPath);
    }
    void OnGUI() {
        if(Time.unscaledTime>messageUntil || String.IsNullOrEmpty(lastPath)) return;
        GUI.Box(new Rect(20,20,Mathf.Min(620,Screen.width-40),32),"Screenshot gespeichert: "+lastPath);
    }
}
