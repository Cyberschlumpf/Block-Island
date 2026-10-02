using UnityEngine;

// Voxel Box: Time of Day is the single authority for sun, sky, ambient light and world time.
// This class only bridges the existing Voxel Box UI/save system to TOD.
public sealed class InfiniteDayNightCycle : MonoBehaviour {
    public static InfiniteDayNightCycle Instance { get; private set; }
    public const float DayLengthMinutes = 60f;
    [Range(0f,24f)] public float timeOfDay = 10f;
    public bool cycleEnabled = true;

    TOD_Sky sky;
    TOD_Time todTime;
    string prefsKey;

    void Awake(){ Instance=this; }
    void Start(){
        EnsureTOD();
        prefsKey="VoxelDayNight_"+(string.IsNullOrEmpty(InfiniteWorldSave.CurrentWorldName)?"NewWorld":InfiniteWorldSave.CurrentWorldName);
        timeOfDay=PlayerPrefs.GetFloat(prefsKey+"_time",10f);
        cycleEnabled=PlayerPrefs.GetInt(prefsKey+"_cycle",1)!=0;
        ApplyToTOD();
    }
    void OnDestroy(){ if(Instance==this) Instance=null; }

    void EnsureTOD(){
        sky=TOD_Sky.Instance;
        if(sky==null) sky=Object.FindFirstObjectByType<TOD_Sky>(FindObjectsInactive.Include);
        if(sky==null){
            var prefab=Resources.Load<GameObject>("TimeOfDay/Sky Dome");
            if(prefab!=null){ var go=Object.Instantiate(prefab); go.name="Time of Day - MASTER"; sky=go.GetComponent<TOD_Sky>(); }
            else Debug.LogError("VOXEL BOX TOD: Resources/TimeOfDay/Sky Dome prefab missing.");
        }
        if(sky!=null){
            todTime=sky.GetComponent<TOD_Time>();
            if(todTime==null) todTime=sky.gameObject.AddComponent<TOD_Time>();
            todTime.DayLengthInMinutes=DayLengthMinutes;
            todTime.UseDeviceDate=false; todTime.UseDeviceTime=false;
            AttachCamera();
            DisableCompetingDirectionalLights();
        }
    }
    void AttachCamera(){
        if(Camera.main==null || sky==null) return;
        var c=Camera.main.GetComponent<TOD_Camera>();
        if(c==null) c=Camera.main.gameObject.AddComponent<TOD_Camera>();
        c.sky=sky;
    }
    void DisableCompetingDirectionalLights(){
        if(sky==null) return;
        Light master=null;
        if(sky.Components!=null) master=sky.Components.LightSource;
        foreach(var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            if(l!=null && l.type==LightType.Directional && l!=master) l.enabled=false;
    }
    void Update(){
        if(sky==null){ EnsureTOD(); if(sky==null)return; }
        if(todTime!=null){
            todTime.DayLengthInMinutes=DayLengthMinutes;
            todTime.ProgressTime=cycleEnabled && !GameState.IsPause;
        }
        timeOfDay=sky.Cycle.Hour;
    }
    void ApplyToTOD(){
        if(sky==null) EnsureTOD(); if(sky==null)return;
        sky.Cycle.Hour=Mathf.Repeat(timeOfDay,24f);
        if(todTime!=null){todTime.DayLengthInMinutes=DayLengthMinutes;todTime.ProgressTime=cycleEnabled && !GameState.IsPause;}
    }
    public void SetTime(float h,bool stopCycle){timeOfDay=Mathf.Repeat(h,24f);if(stopCycle)cycleEnabled=false;ApplyToTOD();SavePrefs();}
    public void SetCycle(bool on){cycleEnabled=on;ApplyToTOD();SavePrefs();}
    public void OnlyDay(){timeOfDay=12f;cycleEnabled=false;ApplyToTOD();SavePrefs();}
    public string ClockText(){float t=sky!=null?sky.Cycle.Hour:timeOfDay;int h=Mathf.FloorToInt(t);int m=Mathf.FloorToInt((t-h)*60f);return h.ToString("00")+":"+m.ToString("00");}
    public void SavePrefs(){if(string.IsNullOrEmpty(prefsKey))return;float t=sky!=null?sky.Cycle.Hour:timeOfDay;PlayerPrefs.SetFloat(prefsKey+"_time",t);PlayerPrefs.SetInt(prefsKey+"_cycle",cycleEnabled?1:0);PlayerPrefs.Save();}
}
