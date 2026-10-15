using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Automated check of the Block Island weather (InfiniteCreativeWeather). Only active with:  -weathertest -bi1out <folder>
public class WeatherSystemTest : MonoBehaviour {
    const string World = "BI1 Test Wetter";
    string outDir;
    readonly List<string> report = new List<string>();
    readonly List<string> errors = new List<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() {
        string[] a = Environment.GetCommandLineArgs();
        if(Array.IndexOf(a, "-weathertest") < 0) return;
        Application.runInBackground = true;
        var go = new GameObject("WeatherSystemTest"); DontDestroyOnLoad(go);
        var t = go.AddComponent<WeatherSystemTest>();
        int i = Array.IndexOf(a, "-bi1out"); t.outDir = i >= 0 ? a[i + 1] : Path.Combine(Application.persistentDataPath, "weathertest");
        Directory.CreateDirectory(t.outDir);
    }
    void OnEnable() { Application.logMessageReceived += OnLog; }
    void OnDisable() { Application.logMessageReceived -= OnLog; }
    void OnLog(string msg, string stack, LogType type) {
        if(stack != null && stack.Contains("AmbientOcclusion")) return;
        if(type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(type + ": " + msg + "\n" + stack);
    }
    void Start() { StartCoroutine(Run()); Invoke(nameof(Timeout), 420f); }
    void Timeout() { report.Add("TIMEOUT"); Finish(); }

    IEnumerator Run() {
        yield return new WaitForSecondsRealtime(2f);
        InfiniteWorldSave.BeginNewWorld(InfiniteWorldSave.WorldType.BlockIsland); InfiniteWorldSave.CurrentWorldName = World; InfiniteWorldSave.LoadRequested = false;
        SceneManager.LoadScene("Game");
        float until = Time.realtimeSinceStartup + 60f;
        while((Map.instance == null || InfiniteCreativeWeather.Instance == null || TOD_Sky.Instance == null) && Time.realtimeSinceStartup < until) yield return null;
        float settle = Time.realtimeSinceStartup + 10f;
        while(Time.realtimeSinceStartup < settle) { foreach(InfiniteWorldAutosave s in FindObjectsByType<InfiniteWorldAutosave>()) Destroy(s); foreach(AutosaveManager s in FindObjectsByType<AutosaveManager>()) { s.saveOnQuit = false; Destroy(s); } yield return null; }
        bool userEffects = VoxelBoxSettings.WeatherEffects; VoxelBoxSettings.WeatherEffects = true; restoreEffects = userEffects ? 1 : 0;
        var w = InfiniteCreativeWeather.Instance; var sky = TOD_Sky.Instance; var day = InfiniteDayNightCycle.Instance;
        GameObject player = GameObject.FindWithTag("Player"); Camera cam = player.GetComponentInChildren<Camera>();
        foreach(MonoBehaviour mb in player.GetComponentsInChildren<MonoBehaviour>()) if(mb is MouseLook || mb is CharacterMotor || mb is CharacterInputController) mb.enabled = false;
        report.Add("weather: auto " + w.autoWeather + " current " + w.current + "  effects setting " + VoxelBoxSettings.WeatherEffects);

        if(System.Environment.GetEnvironmentVariable("BI_SNOW_DIAG") == "1") {
            // Diagnosis at the player's place on the town square (715, 56, 715), looking west
            var lyingF = typeof(InfiniteCreativeWeather).GetField("lyingSnow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var passM = typeof(InfiniteCreativeWeather).GetMethod("PassesThrough", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            InfiniteVoxelWorld wd = Map.instance.infiniteWorld;
            player.transform.position = new Vector3(714.7f, 56.4f, 715.4f); player.transform.rotation = Quaternion.Euler(0, -88, 0); cam.transform.localRotation = Quaternion.Euler(25, 0, 0);
            day.SetTime(12f, true); w.SetWeather(InfiniteCreativeWeather.Kind.Snow);
            Debug.Log("SNOW DIAG: snowing at the square");
            for(int sec = 0; sec < 60; sec += 10) { yield return new WaitForSecondsRealtime(10f); Debug.Log("SNOW DIAG: " + (sec + 10) + " s, lying " + lyingF.FieldType.GetProperty("Count").GetValue(lyingF.GetValue(w))); }
            yield return Shot("diag_platz.png");
            foreach(var vp in new[] { new Vector2(.75f, .35f), new Vector2(.85f, .2f), new Vector2(.65f, .5f), new Vector2(.3f, .35f) }) {
                Ray ray = cam.ViewportPointToRay(new Vector3(vp.x, vp.y, 0f)); string hit = "nothing";
                for(float t = .5f; t < 120f; t += .05f) { Vector3 q = ray.GetPoint(t); var b = wd.GetBlock(Mathf.FloorToInt(q.x), Mathf.FloorToInt(q.y), Mathf.FloorToInt(q.z));
                    if(!b.IsEmpty()) { int hx = Mathf.FloorToInt(q.x), hz = Mathf.FloorToInt(q.z); var col = new System.Text.StringBuilder();
                        for(int y = Mathf.FloorToInt(q.y) + 40; y >= Mathf.FloorToInt(q.y) - 2; y--) { var cb = wd.GetBlock(hx, y, hz); if(!cb.IsEmpty()) col.Append(y + ":" + cb.block.name + "[" + cb.block.GetType().Name + "] "); }
                        hit = "hit " + b.block.name + " at " + q.ToString("0.0") + " column: " + col; break; } }
                report.Add("screen point " + vp + ": " + hit); }
            report.Add("camera " + cam.transform.position + " forward " + cam.transform.forward);
            var kinds = new Dictionary<string, int>();
            for(int x = 690; x <= 714; x++) for(int z = 705; z <= 725; z++) {
                int top = -1; DataBlock tb = default(DataBlock);
                for(int y = 120; y >= 0; y--) { var b = wd.GetBlock(x, y, z); if(!b.IsEmpty()) { top = y; tb = b; break; } }
                string key = (tb.block != null ? tb.block.name + " [" + tb.block.GetType().Name + "]" : "none") + " y=" + top + " pass=" + (tb.block != null && (bool)passM.Invoke(null, new object[] { tb }));
                int c; kinds.TryGetValue(key, out c); kinds[key] = c + 1; }
            foreach(var kv in kinds) report.Add("column top " + kv.Key + ": " + kv.Value);
            var lsObj = lyingF.GetValue(w); var lps = (ParticleSystem)lsObj.GetType().GetField("ps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(lsObj);
            var lb = new ParticleSystem.Particle[lps.particleCount]; int ln = lps.GetParticles(lb); int inArea = 0; var ys = new Dictionary<string, int>();
            for(int i = 0; i < ln; i++) { Vector3 q = lb[i].position; if(q.x >= 690 && q.x < 715 && q.z >= 705 && q.z < 726) { inArea++; string k2 = q.y.ToString("0.00") + " on " + wd.GetBlock(Mathf.FloorToInt(q.x), Mathf.FloorToInt(q.y) - 1, Mathf.FloorToInt(q.z)).block; int c; ys.TryGetValue(k2, out c); ys[k2] = c + 1; } }
            report.Add("lying total " + ln + ", in the square area " + inArea);
            foreach(var kv in ys) if(kv.Value > 20) report.Add("  lying at y " + kv.Key + ": " + kv.Value);
            var snowPs = (ParticleSystem)typeof(InfiniteCreativeWeather).GetField("snow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(w);
            { float mnx = 1e9f, mxx = -1e9f, mnz = 1e9f, mxz = -1e9f; Vector3 cp = cam.transform.position;
              for(int i = 0; i < ln; i++) { Vector3 q = lb[i].position - cp; mnx = Mathf.Min(mnx, q.x); mxx = Mathf.Max(mxx, q.x); mnz = Mathf.Min(mnz, q.z); mxz = Mathf.Max(mxz, q.z); }
              var sh = snowPs.shape; report.Add("lying snow relative to camera: x " + mnx.ToString("0") + ".." + mxx.ToString("0") + ", z " + mnz.ToString("0") + ".." + mxz.ToString("0") + "; shape pos " + sh.position + " rot " + sh.rotation + " scale " + sh.scale + " type " + sh.shapeType + ", emitter rotation " + snowPs.transform.eulerAngles + " lossy " + snowPs.transform.lossyScale); }
            { // map: lying flakes per column around the camera (x -> right, z -> up), digits = count (9 = 9+), '.' = 0, '~' fluid, '#' high
              var cnt = new Dictionary<long, int>(); Vector3 cp = cam.transform.position; int mcx = Mathf.FloorToInt(cp.x), mcz = Mathf.FloorToInt(cp.z);
              for(int i = 0; i < ln; i++) { long k = ((long)Mathf.FloorToInt(lb[i].position.x) << 32) ^ (uint)Mathf.FloorToInt(lb[i].position.z); int c; cnt.TryGetValue(k, out c); cnt[k] = c + 1; }
              report.Add("map around camera (C), north up, x from " + (mcx - 30) + " to " + (mcx + 30));
              for(int z = mcz + 30; z >= mcz - 30; z--) { var sb = new System.Text.StringBuilder();
                for(int x = mcx - 30; x <= mcx + 30; x++) { if(x == mcx && z == mcz) { sb.Append('C'); continue; }
                  int top = -1; DataBlock tb = default(DataBlock); for(int y = 120; y >= 0; y--) { var b = wd.GetBlock(x, y, z); if(!b.IsEmpty() && !(bool)passM.Invoke(null, new object[] { b })) { top = y; tb = b; break; } }
                  int c; cnt.TryGetValue(((long)x << 32) ^ (uint)z, out c);
                  sb.Append(tb.IsFluid() ? '~' : c > 0 ? (char)('0' + Mathf.Min(9, c)) : top > 56 ? '#' : '.'); }
                report.Add(sb.ToString()); } }
            player.transform.position = new Vector3(714.7f, 110f, 715.4f); cam.transform.localRotation = Quaternion.Euler(90, 0, 0); player.transform.rotation = Quaternion.Euler(0, 0, 0);
            yield return new WaitForSecondsRealtime(2f); yield return Shot("diag_von_oben.png");
            report.Add("falling snow " + snowPs.particleCount + " emitter at " + snowPs.transform.position + ", camera " + cam.transform.position);
            Debug.Log("SNOW DIAG: done");
            Finish(); yield break;
        }

        // Sunny = Time of Day look unchanged
        day.SetTime(12f, true); w.SetWeather(InfiniteCreativeWeather.Kind.Sun);
        float cov0 = sky.Clouds.Coverage, fog0 = sky.Atmosphere.Fogginess, ab0 = sky.Atmosphere.Brightness;
        Light sun = sky.Components.LightSource; float sunI0 = sun.intensity; Color sunC0 = sun.color;
        Vector3 view = new Vector3(600.5f, 0f, 704.5f); view.y = Map.instance.infiniteWorld.generator.SurfaceY(600, 704) + 12f;
        player.transform.position = view; player.transform.rotation = Quaternion.Euler(0, 30, 0); cam.transform.localRotation = Quaternion.Euler(8, 0, 0);
        yield return new WaitForSecondsRealtime(5f);
        report.Add("sun: TOD coverage " + cov0.ToString("0.00") + "->" + sky.Clouds.Coverage.ToString("0.00") + ", fog " + fog0.ToString("0.00") + "->" + sky.Atmosphere.Fogginess.ToString("0.00")
            + ", atmosphere brightness " + ab0.ToString("0.00") + "->" + sky.Atmosphere.Brightness.ToString("0.00") + ", sun light " + sunI0.ToString("0.00") + "->" + sun.intensity.ToString("0.00") + " colour unchanged " + (sun.color == sunC0));
        yield return Shot("1_sonne.png");

        foreach(var k in new[] { InfiniteCreativeWeather.Kind.Cloudy, InfiniteCreativeWeather.Kind.Rain, InfiniteCreativeWeather.Kind.Thunderstorm, InfiniteCreativeWeather.Kind.Snow }) {
            w.SetWeather(k); yield return new WaitForSecondsRealtime(14f);
            int particles = 0; foreach(ParticleSystem ps in w.GetComponentsInChildren<ParticleSystem>()) particles += ps.particleCount;
            AudioSource a = (AudioSource)typeof(InfiniteCreativeWeather).GetField("rainAudio", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(w);
            report.Add("   sun light " + sun.intensity.ToString("0.00") + " shadow " + sun.shadowStrength.ToString("0.00") + " (TOD day light " + sky.Day.LightIntensity.ToString("0.00") + ", sun disc " + sky.Sun.MeshBrightness.ToString("0.00") + ")");
            report.Add(k + ": particles " + particles + ", clouds " + sky.Clouds.Coverage.ToString("0.00") + ", haze " + sky.Atmosphere.Fogginess.ToString("0.00") + ", rain sound " + (a.isPlaying ? a.clip.name + " vol " + a.volume.ToString("0.00") : "off"));
            var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var rp = (ParticleSystem)typeof(InfiniteCreativeWeather).GetField(k == InfiniteCreativeWeather.Kind.Snow ? "snow" : "rain", bf).GetValue(w);
            var lc = (AudioClip)typeof(InfiniteCreativeWeather).GetField("rainLight", bf).GetValue(w);
            if(rp == null) report.Add("   particle system: NULL");
            else { var em = rp.emission; report.Add("   particle system " + rp.name + " playing " + rp.isPlaying + " emitting " + rp.isEmitting + " rate " + em.rateOverTimeMultiplier.ToString("0") + " enabled " + em.enabled + " count " + rp.particleCount + " pos " + rp.transform.position + " rot " + rp.transform.eulerAngles + " active " + rp.gameObject.activeInHierarchy + " renderer " + rp.GetComponent<ParticleSystemRenderer>().enabled + " mat " + rp.GetComponent<ParticleSystemRenderer>().sharedMaterial); }
            report.Add("   rain clip " + (lc != null ? lc.name + " " + lc.loadState + " len " + lc.length : "NULL") + "");
            yield return Shot("2_" + k + ".png");
        }

        // Close-up at eye level in snow and rain
        Vector3 ground = new Vector3(560.5f, 0f, 690.5f); ground.y = Map.instance.infiniteWorld.generator.SurfaceY(560, 690) + 2.6f;
        player.transform.position = ground; player.transform.rotation = Quaternion.Euler(0, 60, 0); cam.transform.localRotation = Quaternion.Euler(-5, 0, 0);
        w.SetWeather(InfiniteCreativeWeather.Kind.Snow); yield return new WaitForSecondsRealtime(14f); yield return Shot("2b_schnee_nah.png");
        { var sp = (ParticleSystem)typeof(InfiniteCreativeWeather).GetField("snow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(w);
          var buf = new ParticleSystem.Particle[sp.particleCount]; int n = sp.GetParticles(buf); int near = 0; float minD = 1e9f; Vector3 cp = cam.transform.position;
          for(int i = 0; i < n; i++) { float dd = (buf[i].position - cp).magnitude; if(dd < 20f) near++; minD = Mathf.Min(minD, dd); }
          var rr = sp.GetComponent<ParticleSystemRenderer>();
          report.Add("snow close-up: " + n + " flakes, within 20 m " + near + ", nearest " + minD.ToString("0.0") + " m, size " + (n > 0 ? buf[0].GetCurrentSize(sp).ToString("0.00") : "-") + ", colour " + (n > 0 ? buf[0].GetCurrentColor(sp).ToString() : "-")
            + ", start size " + sp.main.startSize.constant + " 3D " + sp.main.startSize3D + " scale " + sp.transform.lossyScale + " scaling " + sp.main.scalingMode + " sizeBySpeed " + sp.sizeBySpeed.enabled + " sizeOverLifetime " + sp.sizeOverLifetime.enabled + ", shader " + rr.sharedMaterial.shader.name + " supported " + rr.sharedMaterial.shader.isSupported + ", layer " + sp.gameObject.layer + " in camera mask " + ((cam.cullingMask & (1 << sp.gameObject.layer)) != 0)); }
        // Lying snow: flakes that landed stay on the ground (3-5 min) after the snow stops
        var lyingField = typeof(InfiniteCreativeWeather).GetField("lyingSnow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        yield return new WaitForSecondsRealtime(40f);
        cam.transform.localRotation = Quaternion.Euler(35, 0, 0); yield return new WaitForSecondsRealtime(1f); yield return Shot("2d_schnee_liegend.png");
        int lying = (int)lyingField.FieldType.GetProperty("Count").GetValue(lyingField.GetValue(w));
        report.Add("lying snow after 54 s snowfall: " + lying + " flakes");
        if(lying < 200) errors.Add("ERROR: lying snow missing (" + lying + ")");
        w.SetWeather(InfiniteCreativeWeather.Kind.Sun); yield return new WaitForSecondsRealtime(20f); yield return Shot("2e_schnee_nach_ende.png");
        int lyingAfter = (int)lyingField.FieldType.GetProperty("Count").GetValue(lyingField.GetValue(w));
        report.Add("lying snow 20 s after the snow stopped: " + lyingAfter + " flakes");
        if(lyingAfter < lying * 0.9f) errors.Add("ERROR: lying snow disappears too early (" + lying + " -> " + lyingAfter + ")");
        cam.transform.localRotation = Quaternion.Euler(-5, 0, 0);
        w.SetWeather(InfiniteCreativeWeather.Kind.Rain); yield return new WaitForSecondsRealtime(14f); yield return Shot("2c_regen_nah.png");

        // Snowstorm with the maximum of 1.000.000 lying flakes: winter landscape and frame rate
        { bool hadKey = PlayerPrefs.HasKey("VB.BlizzardFlakes"); int before = VoxelBoxSettings.BlizzardFlakes;
          VoxelBoxSettings.BlizzardFlakes = VoxelBoxSettings.BlizzardFlakesMax;
          w.SetWeather(InfiniteCreativeWeather.Kind.Blizzard);
          float start = Time.realtimeSinceStartup; int frames = 0; float worst = 0f; int countAt60 = 0;
          while(Time.realtimeSinceStartup - start < 150f) {
            yield return null; frames++; if(Time.realtimeSinceStartup - start > 5f) worst = Mathf.Max(worst, Time.unscaledDeltaTime);
            if(countAt60 == 0 && Time.realtimeSinceStartup - start > 60f) countAt60 = (int)lyingField.FieldType.GetProperty("Count").GetValue(lyingField.GetValue(w));
          }
          int stormLying = (int)lyingField.FieldType.GetProperty("Count").GetValue(lyingField.GetValue(w));
          report.Add("snowstorm (limit 1.000.000): lying after 60 s " + countAt60 + ", after 150 s " + stormLying + ", average " + (frames / 150f).ToString("0") + " fps, slowest frame " + (worst * 1000f).ToString("0") + " ms");
          if(stormLying <= 40000) errors.Add("ERROR: snowstorm does not exceed 40.000 lying flakes (" + stormLying + ")");
          { var lsObj = lyingField.GetValue(w); var lps = (ParticleSystem)lsObj.GetType().GetField("ps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(lsObj);
            var lb = new ParticleSystem.Particle[lps.particleCount]; int ln = lps.GetParticles(lb); int onPlant = 0, betweenPlants = 0;
            var pass = typeof(InfiniteCreativeWeather).GetMethod("PassesThrough", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            for(int i = 0; i < ln; i++) { Vector3 q = lb[i].position; int x = Mathf.FloorToInt(q.x), y = Mathf.FloorToInt(q.y), z = Mathf.FloorToInt(q.z);
              if((bool)pass.Invoke(null, new object[] { Map.instance.infiniteWorld.GetBlock(x, y - 1, z) })) onPlant++;
              else if((bool)pass.Invoke(null, new object[] { Map.instance.infiniteWorld.GetBlock(x, y, z) })) betweenPlants++; }
            report.Add("snowstorm: flakes lying on top of plants " + onPlant + ", on the lawn between plants " + betweenPlants);
            if(onPlant > 0) errors.Add("ERROR: snow lies on top of flowers (" + onPlant + ")"); }
          yield return Shot("2f_schneesturm.png");
          cam.transform.localRotation = Quaternion.Euler(30, 0, 0); yield return new WaitForSecondsRealtime(1f); yield return Shot("2g_schneesturm_boden.png");
          w.SetWeather(InfiniteCreativeWeather.Kind.Sun);
          start = Time.realtimeSinceStartup; frames = 0; while(Time.realtimeSinceStartup - start < 20f) { yield return null; frames++; }
          report.Add("after the snowstorm (sun, " + (int)lyingField.FieldType.GetProperty("Count").GetValue(lyingField.GetValue(w)) + " flakes lying): " + (frames / 20f).ToString("0") + " fps");
          cam.transform.localRotation = Quaternion.Euler(-5, 0, 0); yield return Shot("2h_winterlandschaft.png");
          if(hadKey) VoxelBoxSettings.BlizzardFlakes = before; else PlayerPrefs.DeleteKey("VB.BlizzardFlakes"); }

        // Roof: rain must not pass through blocks
        w.SetWeather(InfiniteCreativeWeather.Kind.Rain);
        InfiniteVoxelWorld world = Map.instance.infiniteWorld;
        int cx = 640, cz = 700, gy = world.generator.SurfaceY(cx, cz), roofY = gy + 7;
        Block stone = BlockSet.instance.FindBlock("Tanviir: Stone Bricks") ?? BlockSet.instance.FindBlock("Stone");
        for(int x = cx - 10; x <= cx + 10; x++) for(int z = cz - 10; z <= cz + 10; z++) { world.SetBlock(x, roofY, z, new DataBlock(stone)); Map.instance.infiniteRenderer.MarkDirtyAtWorld(x, roofY, z); }
        player.transform.position = new Vector3(cx + .5f, gy + 2.6f, cz + .5f); cam.transform.localRotation = Quaternion.Euler(8, 0, 0);
        yield return new WaitForSecondsRealtime(8f);
        int under = 0, total = 0;
        for(int f = 0; f < 30; f++) {
            yield return null;
            foreach(ParticleSystem ps in w.GetComponentsInChildren<ParticleSystem>()) {
                if(ps.name == "Weather Lying Snow") continue;   // snow that lay there before the roof was built
                var buf = new ParticleSystem.Particle[ps.particleCount]; int n = ps.GetParticles(buf);
                for(int i = 0; i < n; i++) { Vector3 p = buf[i].position; total++; if(p.x >= cx - 9 && p.x < cx + 10 && p.z >= cz - 9 && p.z < cz + 10 && p.y < roofY && buf[i].remainingLifetime > 0f) under++; }
            }
        }
        report.Add("roof test: particles " + total + ", under the roof " + under);
        yield return Shot("3_unter_dach.png");

        // Rainbow: rain ends in the late afternoon
        player.transform.position = view;
        day.SetTime(17.2f, true); yield return new WaitForSecondsRealtime(2f);
        w.SetWeather(InfiniteCreativeWeather.Kind.Sun); w.ShowRainbow(120f);
        Vector3 anti = -sky.SunDirection; anti.y = 0f; player.transform.rotation = Quaternion.LookRotation(anti.normalized); cam.transform.localRotation = Quaternion.Euler(-4, 0, 0);
        yield return new WaitForSecondsRealtime(12f);
        report.Add("rainbow: visible " + w.RainbowVisible + ", sun elevation " + (90f - sky.SunZenith).ToString("0.0"));
        yield return Shot("4_regenbogen.png");
        var rbMat = (Material)typeof(InfiniteCreativeWeather).GetField("rainbowMat", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(w);
        report.Add("rainbow material: shader " + rbMat.shader.name + " texture " + (rbMat.mainTexture != null ? rbMat.mainTexture.name + " " + rbMat.mainTexture.width + "x" + rbMat.mainTexture.height : "none") + " colour " + rbMat.color + " queue " + rbMat.renderQueue + "  camera hdr " + cam.allowHDR);
        var pp = new List<Behaviour>(); foreach(Component c in cam.GetComponents<Component>()) { Behaviour bh = c as Behaviour; if(bh != null && bh.enabled && !(bh is Camera) && !(bh is AudioListener) && !(bh is MouseLook)) { pp.Add(bh); } }
        report.Add("camera effects: " + string.Join(", ", pp.ConvertAll(x => x.GetType().Name).ToArray()));
        foreach(var bh in pp) if(bh.GetType().Name.Contains("PostProcess")) bh.enabled = false;
        yield return new WaitForSecondsRealtime(1f); yield return Shot("4b_regenbogen_ohne_postprocessing.png");
        foreach(var bh in pp) bh.enabled = true;

        // Automatic weather
        w.SetAuto(true);
        report.Add("auto weather on: " + w.autoWeather);
        Finish();
    }

    int restoreEffects = -1;
    void Finish() {
        if(restoreEffects >= 0) PlayerPrefs.SetInt("VB.WeatherEffects", restoreEffects);
        report.Add("errors: " + errors.Count);
        File.WriteAllText(Path.Combine(outDir, "bericht.txt"), string.Join("\n", report) + "\n\n--- ERRORS ---\n" + string.Join("\n\n", errors));
        foreach(string f in Directory.GetFiles(InfiniteWorldSave.SaveDirectory, "BI1 Test*")) { try { File.Move(f, Path.Combine(outDir, Path.GetFileName(f))); } catch { } }
        foreach(string d in Directory.GetDirectories(InfiniteWorldSave.SaveDirectory, "BI1 Test*")) { try { Directory.Move(d, Path.Combine(outDir, Path.GetFileName(d))); } catch { } }
        Application.Quit();
    }

    IEnumerator Shot(string file) {
        yield return new WaitForEndOfFrame();
        Texture2D t = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(outDir, file), t.EncodeToPNG());
        Destroy(t);
    }
}
