using UnityEngine;
using System.Collections.Generic;

// Block Island weather (Time of Day & Weather System asset + Block Island 1.0 content), on top of Time of Day.
// Weather types of the asset: sun, cloudy, rain, thunderstorm, snow, plus a snowstorm (only chosen by hand, never
// automatic) whose lying snow can grow to a winter landscape. Automatic random weather or a chosen one.
// The look stays Time of Day's: sunny weather keeps exactly the configured TOD sky; other weather only moves TOD's
// own cloud and haze values (like TOD's weather example). Rain/snow are the asset's emitters with particle materials,
// they never fall through blocks. 1.0 rain and thunder sounds, lightning flashes, and now and then a rainbow
// (the 1.0 texture) on the horizon opposite the sun after rain. Settings are kept per world.
public sealed class InfiniteCreativeWeather : MonoBehaviour {
    public static InfiniteCreativeWeather Instance { get; private set; }

    public enum Kind { Sun, Cloudy, Rain, Thunderstorm, Snow, Blizzard }
    public static string DisplayName(Kind k) {
        switch(k) { case Kind.Sun: return "Sonne"; case Kind.Cloudy: return "Bewölkt"; case Kind.Rain: return "Regen"; case Kind.Thunderstorm: return "Gewitter"; case Kind.Blizzard: return "Schneesturm"; default: return "Schnee"; }
    }
    static readonly float[] Probability = { 44f, 26f, 15f, 7f, 8f };   // automatic weather, same order as Kind (no snowstorm)
    const float MinMinutes = 4f, MaxMinutes = 12f;                   // real minutes between automatic changes
    const float FadeSeconds = 12f;
    const float RainbowStrength = .4f;               // a soft rainbow, not a painted one

    public bool autoWeather = true;
    public Kind current = Kind.Sun;
    public bool RainbowVisible { get { return rainbowAlpha > .01f; } }

    string prefsKey;
    float nextChange;
    ParticleSystem rain, snow;
    readonly LyingSnow lyingSnow = new LyingSnow();
    AudioSource rainAudio, thunderAudio;
    AudioClip rainLight, rainHeavy, thunderClip;
    Light flash;
    float nextLightning, flashEnd, thunderAt = -1f;
    TOD_Sky sky;
    bool haveBase; float baseCoverage, baseOpacity, baseCloudBright, baseFog, baseAtmoBright, baseLight, baseShadow, baseSunMesh;
    GameObject rainbow; Material rainbowMat; float rainbowAlpha, rainbowTarget, rainbowUntil;
    readonly PrecipitationOcclusion occlusion = new PrecipitationOcclusion();

    void Awake(){ Instance=this; }
    void OnDestroy(){ if(Instance==this) Instance=null; }

    void Start(){
        prefsKey="VoxelWeather_"+(string.IsNullOrEmpty(InfiniteWorldSave.CurrentWorldName)?"NewWorld":InfiniteWorldSave.CurrentWorldName);
        autoWeather=PlayerPrefs.GetInt(prefsKey+"_auto",1)!=0;
        current=(Kind)Mathf.Clamp(PlayerPrefs.GetInt(prefsKey+"_kind",0),0,(int)Kind.Blizzard);
        rain=CreateParticles("Weather/RainParticle","Weather/Rain",true);
        snow=CreateParticles("Weather/SnowParticle","Weather/Snow",false);
        lyingSnow.Create(transform,Resources.Load<Material>("Weather/Snow"));
        rainLight=Resources.Load<AudioClip>("Weather/RainLight"); rainHeavy=Resources.Load<AudioClip>("Weather/RainHeavy"); thunderClip=Resources.Load<AudioClip>("Weather/Thunder");
        rainAudio=gameObject.AddComponent<AudioSource>(); rainAudio.loop=true; rainAudio.playOnAwake=false; rainAudio.spatialBlend=0f; rainAudio.volume=0f;
        thunderAudio=gameObject.AddComponent<AudioSource>(); thunderAudio.playOnAwake=false; thunderAudio.spatialBlend=0f;
        var fl=new GameObject("Weather Lightning Flash"); fl.transform.SetParent(transform,false); fl.transform.rotation=Quaternion.Euler(60f,30f,0f);
        flash=fl.AddComponent<Light>(); flash.type=LightType.Directional; flash.color=new Color(.78f,.84f,1f); flash.intensity=0f; flash.shadows=LightShadows.None; flash.enabled=false;
        CreateRainbow();
        ScheduleNext();
        nextLightning=Time.time+Random.Range(4f,10f);
    }

    // ------------------------------------------------------------------ public API (pause menu)
    public void SetAuto(bool on){ autoWeather=on; if(on) ScheduleNext(); SavePrefs(); }
    public void SetWeather(Kind k){ autoWeather=false; Change(k); SavePrefs(); }
    // Old pause-menu calls (rain/mist toggles) kept working.
    public bool rainEnabled { get { return current==Kind.Rain||current==Kind.Thunderstorm; } }
    public bool mistEnabled { get { return false; } }
    public void SetRain(bool on){ SetWeather(on?Kind.Rain:Kind.Sun); }
    public void SetMist(bool on){ SetWeather(on?Kind.Cloudy:Kind.Sun); }
    public void ClearWeather(){ SetWeather(Kind.Sun); }

    void Change(Kind k){
        Kind before=current; current=k;
        // After rain, now and then a rainbow appears on the horizon opposite the sun.
        bool wasWet=before==Kind.Rain||before==Kind.Thunderstorm;
        if((k==Kind.Sun||k==Kind.Cloudy) && Random.value<(wasWet?.6f:.08f)) ShowRainbow(Random.Range(90f,200f));
        if(k==Kind.Snow||k==Kind.Blizzard||k==Kind.Thunderstorm) rainbowTarget=0f;
    }
    public void ShowRainbow(float seconds){ rainbowUntil=Time.time+seconds; }

    void ScheduleNext(){ nextChange=Time.time+Random.Range(MinMinutes,MaxMinutes)*60f; }
    Kind RandomKind(){
        float sum=0f; foreach(float p in Probability) sum+=p;
        float r=Random.value*sum;
        for(int i=0;i<Probability.Length;i++){ r-=Probability[i]; if(r<=0f) return (Kind)i; }
        return Kind.Sun;
    }

    public void SavePrefs(){ if(string.IsNullOrEmpty(prefsKey)) return; PlayerPrefs.SetInt(prefsKey+"_auto",autoWeather?1:0); PlayerPrefs.SetInt(prefsKey+"_kind",(int)current); PlayerPrefs.Save(); }

    // ------------------------------------------------------------------ update
    void Update(){
        bool effects=VoxelBoxSettings.WeatherEffects;
        if(autoWeather && Time.time>=nextChange){ Kind k=RandomKind(); if(k!=current) Change(k); ScheduleNext(); SavePrefs(); }
        Kind k2=effects?current:Kind.Sun;
        float t=Mathf.Clamp01(Time.deltaTime/FadeSeconds*3f);
        UpdateSky(k2,t);
        UpdateParticles(k2,t);
        UpdateSound(k2,t);
        UpdateLightning(k2);
        UpdateRainbow(effects);
    }

    // Time of Day values: sun = exactly the configured look; other weather moves only clouds and haze.
    void UpdateSky(Kind k,float t){
        if(sky==null){ sky=TOD_Sky.Instance; if(sky==null) return; }
        if(!haveBase){ haveBase=true; baseCoverage=sky.Clouds.Coverage; baseOpacity=sky.Clouds.Opacity; baseCloudBright=sky.Clouds.Brightness; baseFog=sky.Atmosphere.Fogginess; baseAtmoBright=sky.Atmosphere.Brightness; baseLight=sky.Day.LightIntensity; baseShadow=sky.Day.ShadowStrength; baseSunMesh=sky.Sun.MeshBrightness; }
        float cov=baseCoverage, cb=baseCloudBright, fog=baseFog, ab=baseAtmoBright;
        // Sunlight, shadows and sun disc: full only in sunny weather, dimmed under clouds, rain and snow.
        float li=1f, sh=1f, disc=1f;
        switch(k){
            case Kind.Cloudy:       cov=Mathf.Max(baseCoverage,.65f); cb=baseCloudBright*.9f; li=.8f; sh=.65f; disc=.5f; break;
            case Kind.Rain:         cov=Mathf.Max(baseCoverage,.9f); cb=baseCloudBright*.55f; fog=Mathf.Max(baseFog,.35f); ab=baseAtmoBright*.8f; li=.45f; sh=.2f; disc=0f; break;
            case Kind.Thunderstorm: cov=1f; cb=baseCloudBright*.35f; fog=Mathf.Max(baseFog,.55f); ab=baseAtmoBright*.6f; li=.3f; sh=.1f; disc=0f; break;
            case Kind.Snow:         cov=Mathf.Max(baseCoverage,.85f); cb=baseCloudBright*.95f; fog=Mathf.Max(baseFog,.45f); ab=baseAtmoBright*.9f; li=.55f; sh=.25f; disc=0f; break;
            case Kind.Blizzard:     cov=1f; cb=baseCloudBright*.85f; fog=Mathf.Max(baseFog,.7f); ab=baseAtmoBright*.8f; li=.45f; sh=.12f; disc=0f; break;
        }
        sky.Clouds.Coverage=Mathf.Lerp(sky.Clouds.Coverage,cov,t);
        sky.Clouds.Opacity=Mathf.Lerp(sky.Clouds.Opacity,baseOpacity,t);
        sky.Clouds.Brightness=Mathf.Lerp(sky.Clouds.Brightness,cb,t);
        sky.Atmosphere.Fogginess=Mathf.Lerp(sky.Atmosphere.Fogginess,fog,t);
        sky.Atmosphere.Brightness=Mathf.Lerp(sky.Atmosphere.Brightness,ab,t);
        sky.Day.LightIntensity=Mathf.Lerp(sky.Day.LightIntensity,baseLight*li,t);
        sky.Day.ShadowStrength=Mathf.Lerp(sky.Day.ShadowStrength,baseShadow*sh,t);
        sky.Sun.MeshBrightness=Mathf.Lerp(sky.Sun.MeshBrightness,baseSunMesh*disc,t);
    }

    // ------------------------------------------------------------------ particles
    ParticleSystem CreateParticles(string prefabPath,string materialPath,bool isRain){
        var prefab=Resources.Load<GameObject>(prefabPath);
        if(prefab==null){ Debug.LogWarning("WEATHER: "+prefabPath+" missing (menu Block Island/Wetter einrichten)"); return null; }
        var go=Instantiate(prefab,transform); go.name=prefab.name;
        go.SetActive(true);                                   // the asset saves its emitters inactive
        go.transform.localScale=Vector3.one;                  // ...and scaled down, which shrank flakes and the falling area
        var ps=go.GetComponent<ParticleSystem>();
        if(ps==null) ps=go.GetComponentInChildren<ParticleSystem>();
        var mat=Resources.Load<Material>(materialPath);
        foreach(ParticleSystem p in go.GetComponentsInChildren<ParticleSystem>(true)){
            if(p!=ps){ var em=p.emission; em.enabled=false; var sr=p.GetComponent<ParticleSystemRenderer>(); if(sr!=null) sr.enabled=false; continue; }   // the asset's splash sub-emitter has no material
        }
        var main=ps.main; main.scalingMode=ParticleSystemScalingMode.Local; main.simulationSpace=ParticleSystemSimulationSpace.World; main.loop=true; main.playOnAwake=false; main.maxParticles=isRain?4000:3000;
        main.startLifetime=isRain?2.2f:11f; main.startSpeed=isRain?20f:1.6f; main.gravityModifier=isRain?0f:.02f;
        var shape=ps.shape; shape.shapeType=ParticleSystemShapeType.Box; shape.scale=new Vector3(46f,46f,1f);   // local Z points down after the rotation below
        go.transform.rotation=Quaternion.Euler(90f,0f,0f);     // emit downwards
        // The asset's Unity 5 colour/size curves and start colour can leave the particles invisible: set them explicitly.
        main.startColor=Color.white; main.startRotation=0f; main.startSize3D=false; main.startRotation3D=false;
        float size=isRain?.11f:.26f;
        main.startSize=new ParticleSystem.MinMaxCurve(size); main.startSizeX=new ParticleSystem.MinMaxCurve(size); main.startSizeY=new ParticleSystem.MinMaxCurve(size); main.startSizeZ=new ParticleSystem.MinMaxCurve(size);
        var col=ps.colorOverLifetime; col.enabled=false; var sol=ps.sizeOverLifetime; sol.enabled=false; var vol=ps.velocityOverLifetime; vol.enabled=false;
        var lim=ps.limitVelocityOverLifetime; lim.enabled=false; var sub=ps.subEmitters; sub.enabled=false; var sbs=ps.sizeBySpeed; sbs.enabled=false; var rbs=ps.rotationBySpeed; rbs.enabled=false; var cbs=ps.colorBySpeed; cbs.enabled=false; var fol=ps.forceOverLifetime; fol.enabled=false; var inh=ps.inheritVelocity; inh.enabled=false; var col2=ps.collision; col2.enabled=false; var tsa=ps.textureSheetAnimation; tsa.enabled=false;
        var noise=ps.noise; noise.enabled=!isRain; noise.strength=.6f; noise.frequency=.25f;
        var r=ps.GetComponent<ParticleSystemRenderer>();
        if(mat!=null) r.sharedMaterial=mat;
        r.renderMode=isRain?ParticleSystemRenderMode.Stretch:ParticleSystemRenderMode.Billboard;
        if(isRain){ r.lengthScale=3f; r.velocityScale=.05f; }
        r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows=false;
        var e=ps.emission; e.rateOverTimeMultiplier=0f;
        ps.Play(true);
        return ps;
    }

    void UpdateParticles(Kind k,float t){
        Camera cam=Camera.main;
        bool storm=k==Kind.Blizzard;
        float rainRate=k==Kind.Rain?1300f:k==Kind.Thunderstorm?2400f:0f, snowRate=k==Kind.Snow?900f:storm?Mathf.Clamp(VoxelBoxSettings.BlizzardFlakes/45f,9000f,20000f):0f;
        ConfigureSnow(storm);
        lyingSnow.Limit=storm?VoxelBoxSettings.BlizzardFlakes:LyingSnow.NormalFlakes;
        Follow(rain,cam,22f); Follow(snow,cam,16f);
        if(storm && snow!=null) snow.transform.position+=new Vector3(-12f,0f,-5f);   // upwind, so the drifting snow lands around the player
        SetRate(rain,rainRate,t); SetRate(snow,snowRate,t);
        InfiniteVoxelWorld world=Map.instance!=null?Map.instance.infiniteWorld:null;
        if(world!=null && cam!=null){
            occlusion.Begin(world,cam); occlusion.Apply(rain,null); occlusion.Apply(snow,lyingSnow);
            SettleAround(cam,k);
            lyingSnow.Update(world,VoxelBoxSettings.WeatherEffects);
        }
    }
    // The visible falling flakes only cover the area right around the player. So that the snow lies everywhere in
    // view (squares, roofs, lawns), flakes are also laid directly onto the surface in a wide circle around the player.
    float settleCarry;
    void SettleAround(Camera cam,Kind k){
        bool storm=k==Kind.Blizzard;
        if((k!=Kind.Snow && !storm) || snow==null){ settleCarry=0f; return; }
        float ramp=Mathf.Clamp01(snow.emission.rateOverTimeMultiplier/(storm?9000f:900f));   // follows the snowfall fading in/out
        float perSecond=(storm?Mathf.Clamp(VoxelBoxSettings.BlizzardFlakes/50f,3000f,20000f):450f)*ramp;
        settleCarry+=perSecond*Time.deltaTime;
        int n=Mathf.Min((int)settleCarry,1500); settleCarry-=n;
        float radius=storm?96f:64f; Vector3 c=cam.transform.position;
        for(int i=0;i<n;i++){
            Vector2 d=Random.insideUnitCircle*radius;
            float x=c.x+d.x, z=c.z+d.y;
            int top; bool fluid;
            if(occlusion.Surface(Mathf.FloorToInt(x),Mathf.FloorToInt(z),out top,out fluid) && !fluid) lyingSnow.Add(new Vector3(x,top+1f,z));
        }
    }

    // Snowstorm: much more snow over a larger area, faster and driven sideways by the wind.
    bool snowStormMode;
    void ConfigureSnow(bool storm){
        if(snow==null || storm==snowStormMode) return;
        snowStormMode=storm;
        var main=snow.main; main.maxParticles=storm?90000:3000; main.startSpeed=storm?4.5f:1.6f; main.startLifetime=storm?7f:11f;
        var shape=snow.shape; float area=storm?110f:46f; shape.scale=new Vector3(area,area,1f);
        var noise=snow.noise; noise.strength=storm?1.8f:.6f; noise.frequency=storm?.4f:.25f;
        var vol=snow.velocityOverLifetime; vol.enabled=storm; vol.space=ParticleSystemSimulationSpace.World;
        vol.x=new ParticleSystem.MinMaxCurve(storm?3.5f:0f); vol.y=new ParticleSystem.MinMaxCurve(0f); vol.z=new ParticleSystem.MinMaxCurve(storm?1.5f:0f);
    }
    static void Follow(ParticleSystem ps,Camera cam,float height){ if(ps!=null && cam!=null) ps.transform.position=cam.transform.position+new Vector3(0f,height,0f); }
    static void SetRate(ParticleSystem ps,float target,float t){
        if(ps==null) return;
        var e=ps.emission; float v=Mathf.Lerp(e.rateOverTimeMultiplier,target,t); if(target==0f && v<5f) v=0f; e.rateOverTimeMultiplier=v;
    }

    // ------------------------------------------------------------------ sound and lightning
    void UpdateSound(Kind k,float t){
        if(rainAudio==null) return;
        AudioClip want=k==Kind.Thunderstorm?rainHeavy:k==Kind.Rain?rainLight:null;
        float vol=want!=null?(k==Kind.Thunderstorm?.75f:.55f)*AudioListener.volume:0f;
        if(want!=null && rainAudio.clip!=want){ if(rainAudio.volume<.02f||rainAudio.clip==null){ rainAudio.clip=want; rainAudio.Play(); } else vol=0f; }
        rainAudio.volume=Mathf.Lerp(rainAudio.volume,vol,t);
        if(rainAudio.volume<.01f && want==null && rainAudio.isPlaying) rainAudio.Stop();
    }

    void UpdateLightning(Kind k){
        if(flash==null) return;
        if(k==Kind.Thunderstorm && Time.time>=nextLightning){
            nextLightning=Time.time+Random.Range(6f,22f);
            flashEnd=Time.time+Random.Range(.12f,.3f); flash.enabled=true; flash.intensity=Random.Range(.8f,1.6f);
            thunderAt=Time.time+Random.Range(.4f,2.5f);
        }
        if(flash.enabled && Time.time>=flashEnd){ flash.intensity=Mathf.MoveTowards(flash.intensity,0f,Time.deltaTime*8f); if(flash.intensity<=0f) flash.enabled=false; }
        if(thunderAt>0f && Time.time>=thunderAt){ thunderAt=-1f; if(thunderClip!=null) thunderAudio.PlayOneShot(thunderClip,Random.Range(.6f,1f)); }
    }

    // ------------------------------------------------------------------ rainbow
    void CreateRainbow(){
        rainbowMat=Resources.Load<Material>("Weather/Rainbow");
        if(rainbowMat==null) return;
        rainbowMat=new Material(rainbowMat);
        rainbow=GameObject.CreatePrimitive(PrimitiveType.Quad); rainbow.name="Weather Rainbow";
        Destroy(rainbow.GetComponent<Collider>());
        rainbow.transform.SetParent(transform,false);
        var r=rainbow.GetComponent<MeshRenderer>(); r.sharedMaterial=rainbowMat; r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows=false;
        rainbow.SetActive(false);
    }

    void UpdateRainbow(bool effects){
        if(rainbow==null) return;
        if(sky==null) sky=TOD_Sky.Instance;
        float elevation=sky!=null?90f-sky.SunZenith:-90f;
        // A rainbow needs the sun low enough (below ~40 degrees) and above the horizon, and no rain/snow falling.
        bool possible=effects && sky!=null && elevation>3f && elevation<40f && current!=Kind.Rain && current!=Kind.Thunderstorm && current!=Kind.Snow && current!=Kind.Blizzard;
        rainbowTarget=possible && Time.time<rainbowUntil?1f:0f;
        rainbowAlpha=Mathf.MoveTowards(rainbowAlpha,rainbowTarget,Time.deltaTime/8f);
        rainbow.SetActive(rainbowAlpha>.001f);
        if(rainbowAlpha<=.001f) return;
        Camera cam=Camera.main; if(cam==null) return;
        // Opposite the sun, on the horizon: the arc sits in the lower part of the 1.0 texture; its feet touch the horizon.
        Vector3 anti=-sky.SunDirection; anti.y=0f; if(anti.sqrMagnitude<1e-4f) anti=Vector3.forward; anti.Normalize();
        float d=Mathf.Min(560f,cam.farClipPlane*.55f), size=2f*d;
        Vector3 basePoint=cam.transform.position+anti*d - Vector3.up*(Mathf.Tan(elevation*Mathf.Deg2Rad)*d*.5f);
        rainbow.transform.position=basePoint+Vector3.up*(size*.5f-size*.04f);
        rainbow.transform.rotation=Quaternion.LookRotation(anti,Vector3.up);
        rainbow.transform.localScale=new Vector3(size,size,1f);
        rainbowMat.color=new Color(1f,1f,1f,RainbowStrength*rainbowAlpha);
    }

    // ------------------------------------------------------------------ rain/snow must not pass through blocks
    // The voxel world has no colliders: every particle below the highest block of its column (roof, tree, ground,
    // water) is removed. Roof heights are cached for a few seconds and computed with a per-frame budget.
    // Flowers, tulips, bushes, mushrooms and grass tufts do not catch rain or snow: it falls through to the ground
    // (the lawn) and snow lies there between the plants. Full blocks, trees and leaves keep their snow.
    static readonly Dictionary<Block, bool> passCache = new Dictionary<Block, bool>();
    static readonly string[] PlantWords = { "blume", "flower", "tulpe", "tulip", "strauss", "strauß", "bush", "busch", "pilz", "fung", "mushroom", "poppy", "mohn", "dandelion", "löwenzahn", "orchid", "orchidee", "fern", "farn", "reed", "schilf", "plant", "pflanze", "gras", "tuft", "sapling", "setzling", "rose", "lilie", "lily", "kraut", "herb" };
    static bool PassesThrough(DataBlock d) {
        Block b = d.block; if(b == null) return false;
        bool pass;
        if(passCache.TryGetValue(b, out pass)) return pass;
        pass = b is CrossBlock;
        if(!pass && !(b is CubeBlock) && !(b is GroundBlock) && !(b is FluidBlock)) {
            string n = (b.name ?? "").ToLowerInvariant();
            foreach(string w in PlantWords) if(n.Contains(w)) { pass = true; break; }
        }
        passCache[b] = pass;
        return pass;
    }

    sealed class PrecipitationOcclusion {
        const int ColumnsPerFrame = 900; const float CacheSeconds = 3f, SettleCacheSeconds = 20f; const int ScanAbove = 96;
        struct Roof { public int top; public bool fluid; public float time; }
        bool lastFluid, lastKnown;
        bool LastFluid(out bool fluid) { fluid = lastFluid; return !lastKnown; }
        readonly Dictionary<long, Roof> roofs = new Dictionary<long, Roof>();
        ParticleSystem.Particle[] buffer = new ParticleSystem.Particle[4096];
        InfiniteVoxelWorld world; int budget, scanTop; float nextPrune;
        public void Begin(InfiniteVoxelWorld w, Camera cam) {
            world = w; budget = ColumnsPerFrame; scanTop = Mathf.Min(255, Mathf.FloorToInt(cam.transform.position.y) + ScanAbove);
            if(Time.unscaledTime > nextPrune) { nextPrune = Time.unscaledTime + 10f; Prune(); }
        }
        // Particles that reach a block are removed; snow that lands on a solid block (not water) stays there as lying snow.
        public void Apply(ParticleSystem ps, LyingSnow settle) {
            if(ps == null) return;
            int n = ps.particleCount; if(n == 0) return;
            if(buffer.Length < n) buffer = new ParticleSystem.Particle[Mathf.NextPowerOfTwo(n)];
            n = ps.GetParticles(buffer, n); bool changed = false;
            for(int i = 0; i < n; i++) {
                Vector3 p = buffer[i].position;
                int top = RoofTop(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.z)); bool fluid;
                if(p.y < top + 1f) {
                    if(settle != null && top > -1000 && top < int.MaxValue - 1 && !LastFluid(out fluid) && !fluid) settle.Add(new Vector3(p.x, top + 1f, p.z));
                    buffer[i].remainingLifetime = -1f; changed = true;
                }
            }
            if(changed) ps.SetParticles(buffer, n);
        }
        // Surface for lying snow far from the player: a roof height up to 20 s old is good enough there.
        public bool Surface(int x, int z, out int top, out bool fluid) {
            Roof r; long k = ((long)x << 32) ^ (uint)z; top = 0; fluid = false;
            if(roofs.TryGetValue(k, out r) && Time.time - r.time < SettleCacheSeconds) { top = r.top; fluid = r.fluid; return r.top > -1000; }
            if(budget <= 0) return false;
            top = RoofTop(x, z); fluid = lastFluid;
            return lastKnown && top > -1000 && top < int.MaxValue - 1;
        }
        int RoofTop(int x, int z) {
            long k = ((long)x << 32) ^ (uint)z; Roof r; float now = Time.time;
            lastKnown = true;
            if(roofs.TryGetValue(k, out r) && now - r.time < CacheSeconds) { lastFluid = r.fluid; return r.top; }
            if(budget <= 0) { if(roofs.TryGetValue(k, out r)) { lastFluid = r.fluid; return r.top; } lastKnown = false; return int.MaxValue - 1; }
            budget--;
            int top = -1000; bool fluid = false;
            for(int y = scanTop; y >= 0; y--) { var b = world.GetBlock(x, y, z); if(!b.IsEmpty() && !PassesThrough(b)) { top = y; fluid = b.IsFluid(); break; } }
            roofs[k] = new Roof { top = top, fluid = fluid, time = now };
            lastFluid = fluid;
            return top;
        }
        void Prune() {
            if(roofs.Count < 60000) return;
            float now = Time.time; var old = new List<long>();
            foreach(var kv in roofs) if(now - kv.Value.time > SettleCacheSeconds * 2f) old.Add(kv.Key);
            foreach(long k in old) roofs.Remove(k);
        }
    }

    // ------------------------------------------------------------------ lying snow
    // Flakes that land stay on the block they hit for 3-5 minutes and then melt (fade out). They lie flat on the
    // surface and disappear early if the block below them is removed or a block is built where they lie.
    sealed class LyingSnow {
        public const int NormalFlakes = 40000; const int CheckPerFrame = 20000; const float MinSeconds = 180f, MaxSeconds = 300f;
        public int Limit = NormalFlakes;
        int checkOffset;
        ParticleSystem ps; ParticleSystemRenderer rend;
        ParticleSystem.Particle[] buffer = new ParticleSystem.Particle[1024];
        public void Create(Transform parent, Material mat) {
            var go = new GameObject("Weather Lying Snow"); go.transform.SetParent(parent, false);
            go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.identity;
            ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = VoxelBoxSettings.BlizzardFlakesMax; main.gravityModifier = 0f; main.startSpeed = 0f; main.duration = 1f;
            var em = ps.emission; em.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            // Fade in on landing, stay white, melt away during the last part of the lifetime.
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .01f), new GradientAlphaKey(1f, .9f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var sol = ps.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(.9f, 1f), new Keyframe(1f, .55f)));
            rend = go.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.HorizontalBillboard;    // flat on the block
            var sh = Resources.Load<Shader>("Weather/LyingSnow");
            if(sh != null) { var m = new Material(sh); m.name = "Lying Snow"; if(mat != null) m.mainTexture = mat.mainTexture; rend.sharedMaterial = m; }
            else if(mat != null) rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; rend.receiveShadows = false;
            rend.sortMode = ParticleSystemSortMode.None;
            ps.Play();
        }
        public int Count { get { return ps != null ? ps.particleCount : 0; } }
        public void Add(Vector3 surface) {
            if(ps == null || ps.particleCount >= Limit) return;
            var ep = new ParticleSystem.EmitParams();
            ep.position = surface + new Vector3(0f, .015f, 0f);
            ep.velocity = Vector3.zero;
            ep.startLifetime = Random.Range(MinSeconds, MaxSeconds);
            ep.startSize = Random.Range(.35f, .55f);
            ep.rotation = Random.Range(0f, 360f);
            ep.startColor = Color.white;
            ps.Emit(ep, 1);
        }
        public void Update(InfiniteVoxelWorld world, bool visible) {
            if(ps == null) return;
            if(rend.enabled != visible) rend.enabled = visible;
            if(!ps.isPlaying) ps.Play();
            // A slice of the flakes per frame (even 1.000.000 flakes are all checked within a few seconds).
            int total = ps.particleCount; if(total == 0) return;
            if(checkOffset >= total) checkOffset = 0;
            int n = Mathf.Min(CheckPerFrame, total - checkOffset);
            if(buffer.Length < n) buffer = new ParticleSystem.Particle[Mathf.NextPowerOfTwo(n)];
            int offset = checkOffset; checkOffset += n;
            n = ps.GetParticles(buffer, n, offset); bool changed = false;
            for(int i = 0; i < n; i++) {
                Vector3 p = buffer[i].position;
                int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y), z = Mathf.FloorToInt(p.z);
                var below = world.GetBlock(x, y - 1, z); var here = world.GetBlock(x, y, z);
                if(below.IsEmpty() || PassesThrough(below) || (!here.IsEmpty() && !PassesThrough(here))) { buffer[i].remainingLifetime = -1f; changed = true; }
            }
            if(changed) ps.SetParticles(buffer, n, offset);
        }
    }
}
