using UnityEngine;

// The Voxel Box 0.3: decorative creative-world weather. No survival/gameplay effects.
public sealed class InfiniteCreativeWeather : MonoBehaviour {
    public static InfiniteCreativeWeather Instance { get; private set; }
    public bool rainEnabled;
    public bool mistEnabled;
    ParticleSystem rain;
    Transform target;
    string prefsKey;
    bool oldFog;
    Color oldFogColor;
    float oldFogDensity;

    void Awake(){ Instance=this; }
    void Start(){
        target=Camera.main!=null?Camera.main.transform:null;
        prefsKey="VoxelWeather_"+(string.IsNullOrEmpty(InfiniteWorldSave.CurrentWorldName)?"NewWorld":InfiniteWorldSave.CurrentWorldName);
        rainEnabled=PlayerPrefs.GetInt(prefsKey+"_rain",0)!=0;
        mistEnabled=PlayerPrefs.GetInt(prefsKey+"_mist",0)!=0;
        oldFog=RenderSettings.fog; oldFogColor=RenderSettings.fogColor; oldFogDensity=RenderSettings.fogDensity;
        CreateRain(); Apply();
    }
    void OnDestroy(){ if(Instance==this) Instance=null; }
    void LateUpdate(){
        if(target==null && Camera.main!=null) target=Camera.main.transform;
        if(rain!=null && target!=null) rain.transform.position=target.position+new Vector3(0f,14f,0f);
    }
    void CreateRain(){
        GameObject go=new GameObject("Voxel Box Creative Rain"); go.transform.SetParent(transform,false);
        rain=go.AddComponent<ParticleSystem>();
        var main=rain.main; main.loop=true; main.playOnAwake=false; main.startLifetime=1.35f; main.startSpeed=22f; main.startSize=.045f; main.maxParticles=900; main.simulationSpace=ParticleSystemSimulationSpace.World;
        var emission=rain.emission; emission.rateOverTime=520f;
        var shape=rain.shape; shape.shapeType=ParticleSystemShapeType.Box; shape.scale=new Vector3(34f,.2f,34f);
        var vel=rain.velocityOverLifetime; vel.enabled=true; vel.space=ParticleSystemSimulationSpace.World; vel.y=-7f;
        var r=go.GetComponent<ParticleSystemRenderer>(); r.renderMode=ParticleSystemRenderMode.Stretch; r.lengthScale=2.2f; r.velocityScale=.08f;
        Shader sh=Shader.Find("Particles/Standard Unlit"); if(sh==null) sh=Shader.Find("Sprites/Default");
        if(sh!=null){ Material m=new Material(sh); m.name="Voxel Box Rain Material"; if(m.HasProperty("_Color")) m.SetColor("_Color",new Color(.72f,.82f,.95f,.42f)); r.material=m; }
    }
    public void SetRain(bool on){ rainEnabled=on; Apply(); SavePrefs(); }
    public void SetMist(bool on){ mistEnabled=on; Apply(); SavePrefs(); }
    public void ClearWeather(){ rainEnabled=false; mistEnabled=false; Apply(); SavePrefs(); }
    void Apply(){
        if(rain!=null){ if(rainEnabled && !rain.isPlaying) rain.Play(true); else if(!rainEnabled && rain.isPlaying) rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); }
        if(mistEnabled){ RenderSettings.fog=true; RenderSettings.fogMode=FogMode.ExponentialSquared; RenderSettings.fogColor=new Color(.62f,.68f,.72f,1f); RenderSettings.fogDensity=.0065f; }
        else { RenderSettings.fog=oldFog; RenderSettings.fogColor=oldFogColor; RenderSettings.fogDensity=oldFogDensity; }
    }
    public void SavePrefs(){ if(string.IsNullOrEmpty(prefsKey)) return; PlayerPrefs.SetInt(prefsKey+"_rain",rainEnabled?1:0); PlayerPrefs.SetInt(prefsKey+"_mist",mistEnabled?1:0); PlayerPrefs.Save(); }
}
