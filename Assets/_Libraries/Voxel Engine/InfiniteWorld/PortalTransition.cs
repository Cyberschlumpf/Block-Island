using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;

// The journey through a portal:
// 1. the screen slowly turns black,
// 2. stars and a galaxy form out of the darkness (Galaxy Materials skybox + a star field),
// 3. meanwhile the current world is saved and the other world loads; the stars stay until the area around the
//    arrival point is really built (no missing blocks),
// 4. the stars dissolve and the picture turns into the new world. The player stands in front of the other portal,
//    looking the same way as when entering (or at the saved start point if that world has no portal yet).
// Everything is drawn with OnGUI on top of the game; the stars are rendered by an own camera into a texture.
public sealed class PortalTransition : MonoBehaviour {
    public static bool Busy { get; private set; }
    public static string Phase { get; private set; } = "";   // black, stars, loading, dissolve (for tests and diagnostics)
    const float FadeToBlack = 1.6f, StarsForm = 2.6f, MinStars = 4f, Dissolve = 3f, LoadTimeout = 25f;
    // Easy on the eyes: the star picture is shown dimmed, never at full brightness.
    static readonly Color StarDim = new Color(.62f, .64f, .7f);
    const int StarLayer = 31;

    static bool hasArrival; static Vector3 arrivalPos; static Quaternion arrivalRot;

    // GameInitializer asks for this right after the save is loaded, before the world starts streaming.
    public static bool TakeArrival(out Vector3 pos, out Quaternion rot) {
        pos = arrivalPos; rot = arrivalRot;
        if(!hasArrival) return false;
        hasArrival = false; return true;
    }

    public static void Begin(BlockIslandPortal from) {
        if(Busy || from == null) return;
        Busy = true;
        var go = new GameObject("Portal Transition");
        DontDestroyOnLoad(go);
        go.AddComponent<PortalTransition>().StartCoroutine(go.GetComponent<PortalTransition>().Run(from.Center, from.target));
    }

    float black, starsAlpha;
    RenderTexture rt; Camera starCam; GameObject starRoot; ParticleSystem stars; Material skyMat, starMat;

    IEnumerator Run(Vector3 portalCenter, InfiniteWorldSave.WorldType target) {
        GameObject player = GameObject.FindWithTag("Player");
        Camera main = Camera.main;
        Vector3 fwd = main != null ? main.transform.forward : (player != null ? player.transform.forward : Vector3.forward);
        fwd.y = 0; if(fwd.sqrMagnitude < .01f) fwd = Vector3.forward; fwd.Normalize();
        Freeze(player, true);

        // 1. slowly to black
        Phase = "black";
        for(float t = 0; t < FadeToBlack; t += Time.unscaledDeltaTime) { black = Mathf.SmoothStep(0, 1, t / FadeToBlack); yield return null; }
        black = 1;

        // the view out of this portal is what travellers coming the other way will see in their portal
        string source = InfiniteWorldSave.CurrentWorldName;
        InfiniteWorldSave.WorldType sourceType = InfiniteWorldSave.CurrentWorldType;
        PortalSnapshots.Capture(source, portalCenter);

        // 2. stars form out of the darkness
        Phase = "stars";
        SetupStars();
        float starsStart = Time.unscaledTime;
        for(float t = 0; t < StarsForm; t += Time.unscaledDeltaTime) { starsAlpha = Mathf.SmoothStep(0, 1, t / StarsForm); UpdateStars(); yield return null; }
        starsAlpha = 1; UpdateStars();

        // 3. save this world, then load the other one
        Phase = "loading";
        if(Map.instance != null && Map.instance.infiniteWorld != null) {
            InfiniteWorldSave.SaveCurrent(Map.instance.infiniteWorld);
            if(InfiniteDayNightCycle.Instance != null) InfiniteDayNightCycle.Instance.SavePrefs();
        }
        // Unload the old world completely (through an empty scene) BEFORE the save name switches: nothing of it
        // (autosave, streaming, terrain store) can then write into the other world's save.
        Scene oldScene = SceneManager.GetActiveScene();
        Scene transit = SceneManager.CreateScene("Portal Transit");
        SceneManager.SetActiveScene(transit);
        AsyncOperation unload = SceneManager.UnloadSceneAsync(oldScene);
        while(unload != null && !unload.isDone) { UpdateStars(); yield return null; }

        string dest = PortalBlocks.SaveNameOf(target);
        Vector3i cell;
        bool back = PortalRegistry.TryGet(dest, sourceType, out cell) || PortalRegistry.TryGetAny(dest, out cell);
        if(back) {
            Vector3 c = new Vector3(cell.x + 1f, cell.y, cell.z + 1f);
            arrivalPos = c + fwd * (BlockIslandPortal.Size * .5f + 1.6f) + new Vector3(0, 2f, 0);   // feet clear of the cube, like the normal start point
            arrivalRot = Quaternion.LookRotation(fwd);
            hasArrival = true;
        }
        if(File.Exists(InfiniteWorldSave.PathFor(dest))) InfiniteWorldSave.RequestLoad(dest);
        else InfiniteWorldSave.BeginNewWorld(target);
        Debug.Log("PORTALS: travelling from " + source + " to " + dest + (back ? " (arrival at its portal " + cell.x + "," + cell.y + "," + cell.z + ")" : " (saved start point)"));

        AsyncOperation op = SceneManager.LoadSceneAsync("Game");
        while(op != null && !op.isDone) { UpdateStars(); yield return null; }

        // wait until the new world around the player is built
        float loadStart = Time.unscaledTime; GameObject newPlayer = null;
        while(Time.unscaledTime - loadStart < LoadTimeout) {
            UpdateStars();
            newPlayer = GameObject.FindWithTag("Player");
            if(newPlayer != null) Freeze(newPlayer, true);
            if(newPlayer != null && Map.instance != null && Map.instance.infiniteRenderer != null && AreaReady(newPlayer.transform.position)) break;
            yield return null;
        }
        // a moment for the last meshes, and at least the minimum star time
        float settle = Time.unscaledTime + .6f;
        while(Time.unscaledTime < settle || Time.unscaledTime - starsStart < MinStars) { UpdateStars(); yield return null; }

        // the arrival view becomes the picture shown in the portals that lead here
        if(newPlayer != null) {
            BlockIslandPortal near = null; float best = 12f;
            foreach(BlockIslandPortal p in BlockIslandPortal.All) { float d = Vector3.Distance(p.Center, newPlayer.transform.position); if(d < best) { best = d; near = p; } }
            Camera cam = Camera.main;
            PortalSnapshots.Capture(dest, near != null ? near.Center : (cam != null ? cam.transform.position : newPlayer.transform.position + Vector3.up * 1.6f));
        }
        PortalWorldWatcher.Ensure();

        // 4. the stars dissolve into the new world
        Phase = "dissolve";
        black = 0;
        Freeze(newPlayer, false);
        for(float t = 0; t < Dissolve; t += Time.unscaledDeltaTime) { starsAlpha = 1 - Mathf.SmoothStep(0, 1, t / Dissolve); UpdateStars(t / Dissolve); yield return null; }
        starsAlpha = 0;
        Phase = ""; Busy = false;
        Destroy(gameObject);
    }

    // The ground under the player and the chunks around it are built (or nothing is left to stream).
    public static bool AreaReady(Vector3 p) {
        const int S = 32;
        var streamer = FindFirstObjectByType<InfiniteChunkStreamer>();
        if(streamer != null && streamer.ActiveChunkCount > 0 && streamer.QueuedChunkCount == 0) return true;
        int cx = Mathf.FloorToInt(p.x / S), cy = Mathf.FloorToInt((p.y - 2f) / S), cz = Mathf.FloorToInt(p.z / S);
        var r = Map.instance.infiniteRenderer;
        for(int x = -1; x <= 1; x++) for(int z = -1; z <= 1; z++)
            if(cy >= 0 && !r.HasChunk(new InfiniteChunkKey(cx + x, cy, cz + z))) return false;
        return true;
    }

    static readonly List<Behaviour> frozen = new List<Behaviour>();
    static void Freeze(GameObject player, bool on) {
        if(on) {
            if(player == null) return;
            foreach(Behaviour b in player.GetComponentsInChildren<Behaviour>())
                if(b != null && b.enabled && (b is MouseLook || b is CharacterMotor || b is CharacterInputController || b is Builder)) { b.enabled = false; frozen.Add(b); }
        } else {
            foreach(Behaviour b in frozen) if(b != null) b.enabled = true;
            frozen.Clear();
        }
    }

    // ------------------------------------------------------------------ stars
    void SetupStars() {
        rt = new RenderTexture(Mathf.Max(64, Screen.width), Mathf.Max(64, Screen.height), 16) { name = "Portal Stars" };
        starRoot = new GameObject("Portal Stars"); DontDestroyOnLoad(starRoot);
        starRoot.transform.position = new Vector3(0, -20000, 0);
        starCam = starRoot.AddComponent<Camera>();
        starCam.cullingMask = 1 << StarLayer; starCam.targetTexture = rt; starCam.fieldOfView = 70; starCam.nearClipPlane = .05f; starCam.farClipPlane = 200;
        Material[] skies = { Resources.Load<Material>("Portal/Galaxy/GalaxyMaterialSkybox_21"), Resources.Load<Material>("Portal/Galaxy/GalaxyMaterialSkybox_27"),
                             Resources.Load<Material>("Portal/Galaxy/GalaxyMaterialSkybox_33"), Resources.Load<Material>("Portal/Galaxy/GalaxyMaterialSkybox_39") };
        // calm nebula: the star cubemap of a random galaxy, drawn darkened on a sphere around the star camera
        starCam.clearFlags = CameraClearFlags.SolidColor; starCam.backgroundColor = Color.black;
        Material sky = skies[Random.Range(0, skies.Length)];
        Texture nebula = null;
        if(sky != null) foreach(string n in sky.GetTexturePropertyNames()) { Texture t = sky.GetTexture(n); if(t is Cubemap && t.name.StartsWith("BakedNebulae")) { nebula = t; break; } }
        Shader skySh = Resources.Load<Shader>("Portal/StarSky");
        if(nebula != null && skySh != null) {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(sphere.GetComponent<Collider>());
            sphere.name = "Nebula"; sphere.layer = StarLayer;
            sphere.transform.SetParent(starRoot.transform, false); sphere.transform.localScale = Vector3.one * 150f;
            skyMat = new Material(skySh); skyMat.SetTexture("_Cube", nebula);
            var sr = sphere.GetComponent<Renderer>(); sr.sharedMaterial = skyMat; sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // star field: points of light gathering around the viewer and slowly drifting past
        var sgo = new GameObject("Star Field"); sgo.layer = StarLayer;
        sgo.transform.SetParent(starRoot.transform, false);
        stars = sgo.AddComponent<ParticleSystem>();
        stars.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = stars.main; main.loop = true; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = 9f; main.startSpeed = 0f; main.maxParticles = 3000; main.useUnscaledTime = true;
        main.startSize = new ParticleSystem.MinMaxCurve(.03f, .13f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(.55f, .62f, .78f), new Color(.75f, .7f, .62f));
        var sh = stars.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 40f; sh.radiusThickness = .7f;
        var em = stars.emission; em.rateOverTime = 110f;
        var vel = stars.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local; vel.radial = new ParticleSystem.MinMaxCurve(-2.2f);
        var col = stars.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                  new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .2f), new GradientAlphaKey(1, .8f), new GradientAlphaKey(0, 1) });
        col.color = new ParticleSystem.MinMaxGradient(g);
        var r = stars.GetComponent<ParticleSystemRenderer>();
        Material sm = Resources.Load<Material>("Portal/PortalStars");
        if(sm != null) { starMat = new Material(sm); r.sharedMaterial = starMat; }
        r.renderMode = ParticleSystemRenderMode.Billboard;
        stars.Play();
        stars.Simulate(3f, true, true, true);   // the field starts already partly formed
        stars.Play();
    }

    // dissolve 0..1: in the last phase the stars rush past and fade
    void UpdateStars(float dissolve = 0f) {
        if(starRoot == null) return;
        starRoot.transform.Rotate(0, Time.unscaledDeltaTime * 2.5f, 0, Space.World);
        if(skyMat != null) skyMat.SetFloat("_Fade", Mathf.Clamp01(starsAlpha * 1.2f));
        if(stars != null) {
            var main = stars.main; main.simulationSpeed = 1f + dissolve * 1.5f;
            if(dissolve > 0) { var em = stars.emission; em.rateOverTime = 0f; }
        }
        if(starMat != null && starMat.HasProperty("_TintColor")) starMat.SetColor("_TintColor", new Color(.5f, .5f, .5f, .22f * Mathf.Clamp01(starsAlpha * 1.5f)));
    }

    static Texture2D blackTex;
    void OnGUI() {
        GUI.depth = -10000;
        if(blackTex == null) { blackTex = new Texture2D(1, 1); blackTex.SetPixel(0, 0, Color.black); blackTex.Apply(); }
        Rect screen = new Rect(0, 0, Screen.width, Screen.height);
        Color old = GUI.color;
        if(black > 0) { GUI.color = new Color(1, 1, 1, black); GUI.DrawTexture(screen, blackTex); }
        if(rt != null && starsAlpha > 0) { GUI.color = new Color(StarDim.r, StarDim.g, StarDim.b, starsAlpha); GUI.DrawTexture(screen, rt, ScaleMode.StretchToFill, false); }
        GUI.color = old;
    }

    void OnDestroy() {
        Freeze(null, false);
        if(starRoot != null) Destroy(starRoot);
        if(rt != null) { rt.Release(); Destroy(rt); }
        if(skyMat != null) Destroy(skyMat);
        if(starMat != null) Destroy(starMat);
        Busy = false;
    }
}
