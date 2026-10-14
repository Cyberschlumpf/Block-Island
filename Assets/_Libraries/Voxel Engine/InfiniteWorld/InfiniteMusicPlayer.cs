using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

// Stage 6.12.5: runtime MP3 folder player. Files stay on disk; only the current track is decoded.
public sealed class InfiniteMusicPlayer : MonoBehaviour {
    public static InfiniteMusicPlayer Instance { get; private set; }
    public const int MaxTracks = 600;
    public readonly List<string> tracks = new List<string>();
    public int currentIndex = -1;
    public bool shuffle;
    public string folderPath = "";
    public string status = "Kein Musikordner geladen";
    public float volume = .65f;

    AudioSource source;
    Coroutine loadRoutine;
    readonly System.Random rng = new System.Random();

    public bool IsPlaying { get { return source != null && source.isPlaying; } }
    public bool IsPaused { get; private set; }
    public string CurrentTitle { get { return currentIndex>=0 && currentIndex<tracks.Count ? Path.GetFileNameWithoutExtension(tracks[currentIndex]) : "-"; } }

    void Awake(){
        if(Instance!=null && Instance!=this){ Destroy(this); return; }
        Instance=this;
        source=gameObject.AddComponent<AudioSource>();
        source.playOnAwake=false; source.loop=false; source.spatialBlend=0f; source.volume=volume;
        folderPath=PlayerPrefs.GetString("VoxelMusic_Folder","");
        shuffle=PlayerPrefs.GetInt("VoxelMusic_Shuffle",0)!=0;
        volume=PlayerPrefs.GetFloat("VoxelMusic_Volume",.65f); source.volume=volume;
        if(!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath)) ScanFolder(folderPath,false);
    }
    void OnDestroy(){ if(Instance==this) Instance=null; }
    void Update(){
        // Automatic next track. Pausing the game does not pause music.
        if(source!=null && source.clip!=null && !source.isPlaying && !IsPaused && source.time >= Mathf.Max(0f,source.clip.length-.15f)) Next();
    }

    public void SetVolume(float v){ volume=Mathf.Clamp01(v); if(source!=null) source.volume=volume; PlayerPrefs.SetFloat("VoxelMusic_Volume",volume); PlayerPrefs.Save(); }
    public void ToggleShuffle(){ shuffle=!shuffle; PlayerPrefs.SetInt("VoxelMusic_Shuffle",shuffle?1:0); PlayerPrefs.Save(); }
    public void Play(){
        if(tracks.Count==0){ status="Bitte zuerst einen MP3-Ordner laden."; return; }
        if(source.clip!=null && IsPaused){ source.UnPause(); IsPaused=false; status="Wiedergabe"; return; }
        if(currentIndex<0) currentIndex=0;
        LoadAndPlay(currentIndex);
    }
    public void Pause(){ if(source!=null && source.isPlaying){ source.Pause(); IsPaused=true; status="Pause"; } }
    public void Stop(){ if(source!=null){ source.Stop(); source.time=0f; } IsPaused=false; status="Gestoppt"; }
    public void Next(){ if(tracks.Count==0)return; int n=shuffle && tracks.Count>1 ? RandomOtherIndex() : (currentIndex+1+tracks.Count)%tracks.Count; LoadAndPlay(n); }
    public void Previous(){ if(tracks.Count==0)return; int n=shuffle && tracks.Count>1 ? RandomOtherIndex() : (currentIndex-1+tracks.Count)%tracks.Count; LoadAndPlay(n); }
    int RandomOtherIndex(){ int n=currentIndex; for(int i=0;i<8 && n==currentIndex;i++) n=rng.Next(tracks.Count); return n; }

    public void ScanFolder(string path,bool autoPlay){
        try{
            if(string.IsNullOrEmpty(path) || !Directory.Exists(path)){ status="Ordner nicht gefunden."; return; }
            var found=Directory.GetFiles(path,"*.mp3",SearchOption.AllDirectories);
            Array.Sort(found,StringComparer.OrdinalIgnoreCase);
            tracks.Clear();
            for(int i=0;i<found.Length && tracks.Count<MaxTracks;i++) tracks.Add(found[i]);
            folderPath=path; currentIndex=tracks.Count>0?0:-1;
            PlayerPrefs.SetString("VoxelMusic_Folder",folderPath); PlayerPrefs.Save();
            status=tracks.Count+" MP3-Titel geladen"+(found.Length>MaxTracks?" (Limit 600)":"");
            if(autoPlay && tracks.Count>0) Play();
        } catch(Exception e){ status="Ordner konnte nicht gelesen werden: "+e.Message; }
    }

    public void ChooseFolder(){ StartCoroutine(ChooseFolderRoutine()); }
    IEnumerator ChooseFolderRoutine(){
        status="Ordnerauswahl wird geöffnet ...";
        string result="";
        bool done=false;
        System.Threading.Thread t=new System.Threading.Thread(()=>{ try{ result=OpenNativeFolderDialog(); }catch{} done=true; });
        t.IsBackground=true; t.Start();
        while(!done) yield return null;
        if(!string.IsNullOrEmpty(result)) ScanFolder(result,false); else status="Ordnerauswahl abgebrochen.";
    }
    string OpenNativeFolderDialog(){
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        return RunPicker("/usr/bin/osascript", "-e \"POSIX path of (choose folder with prompt \\\"MP3-Musikordner auswählen\\\")\"");
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        string ps="$f=New-Object System.Windows.Forms.FolderBrowserDialog;$f.Description='MP3-Musikordner auswählen';if($f.ShowDialog() -eq 'OK'){$f.SelectedPath}";
        return RunPicker("powershell.exe", "-NoProfile -STA -Command \"Add-Type -AssemblyName System.Windows.Forms;"+ps.Replace("\"","\\\"")+"\"");
#else
        return RunPicker("zenity", "--file-selection --directory --title=MP3-Musikordner auswählen");
#endif
    }
    string RunPicker(string exe,string args){
        var p=new Process(); p.StartInfo.FileName=exe; p.StartInfo.Arguments=args; p.StartInfo.UseShellExecute=false; p.StartInfo.RedirectStandardOutput=true; p.StartInfo.CreateNoWindow=true;
        p.Start(); string s=p.StandardOutput.ReadToEnd(); p.WaitForExit(); return (s??"").Trim().TrimEnd(Path.DirectorySeparatorChar);
    }

    void LoadAndPlay(int index){
        if(index<0 || index>=tracks.Count)return;
        if(loadRoutine!=null) StopCoroutine(loadRoutine);
        loadRoutine=StartCoroutine(LoadTrack(index));
    }
    IEnumerator LoadTrack(int index){
        currentIndex=index; IsPaused=false; status="Lade: "+CurrentTitle;
        string uri=new Uri(tracks[index]).AbsoluteUri;
        using(UnityWebRequest req=UnityWebRequestMultimedia.GetAudioClip(uri,AudioType.MPEG)){
            yield return req.SendWebRequest();
            if(req.result!=UnityWebRequest.Result.Success){ status="MP3 konnte nicht geladen werden: "+req.error; yield break; }
            AudioClip old=source.clip; source.clip=DownloadHandlerAudioClip.GetContent(req); source.volume=volume; source.Play();
            if(old!=null) Destroy(old);
            status="Wiedergabe";
        }
        loadRoutine=null;
    }
}
