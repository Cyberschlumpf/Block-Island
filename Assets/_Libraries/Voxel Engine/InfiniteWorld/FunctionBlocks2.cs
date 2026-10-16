using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

// Function blocks, packages 2-4: real world (weather station, world clocks, moon phase, tides), movement and technology
// (fireworks, elevator, conveyor, spring, door, minecart station and switch), sound (music box, nature sounds) and
// building helpers (waypoint, structure spawner, frost fountain, picture column).
public static partial class FunctionBlocks {
    public const float StationStopSeconds = 6f;
    static readonly HashSet<Vector3i> openCells = new HashSet<Vector3i>();
    public static bool IsOpen(Vector3i p) { return openCells.Contains(p); }
    public static void SetOpen(Vector3i p, bool open) { if(open) openCells.Add(p); else openCells.Remove(p); }

    static readonly Vector3i[] Around = { new Vector3i(1, 0, 0), new Vector3i(-1, 0, 0), new Vector3i(0, 0, 1), new Vector3i(0, 0, -1), new Vector3i(0, -1, 0) };
    static bool Has(InfiniteVoxelWorld w, Vector3i p, Type component, out Vector3i at) {
        foreach(Vector3i d in Around) {
            Vector3i q = p + d; var fb = w.GetBlock(q.x, q.y, q.z).block as FunctionBlock;
            if(fb != null && fb.def != null && fb.def.component == component) { at = q; return true; }
        }
        at = p; return false;
    }
    public static bool StationNear(InfiniteVoxelWorld w, Vector3i rail) { Vector3i at; return w != null && Has(w, rail, typeof(FBStation), out at); }
    public static bool SwitchTurnsNear(InfiniteVoxelWorld w, Vector3i rail) { Vector3i at; return w != null && Has(w, rail, typeof(FBSwitch), out at) && FunctionBlockData.Get(at, "geradeaus") == "abbiegen"; }

    public static GameObject Player() { return GameObject.FindWithTag("Player"); }

    // Lantern brightness (Enter on a lantern): stored per lantern, "Mittel" by default.
    public static readonly string[] LampLevels = { "Aus", "Schwach", "Mittel", "Hell", "Sehr hell" };
    static readonly float[] LampScale = { 0f, .5f, 1f, 1.6f, 2.4f };
    public static float LampStrength(Vector3i cell) { int i = Array.IndexOf(LampLevels, FunctionBlockData.Get(cell, "Mittel")); return LampScale[i < 0 ? 2 : i]; }
    public static void CycleLamp(Vector3i cell) {
        int i = Array.IndexOf(LampLevels, FunctionBlockData.Get(cell, "Mittel")); if(i < 0) i = 2;
        string n = LampLevels[(i + 1) % LampLevels.Length]; FunctionBlockData.Set(cell, n);
        FunctionBlockUI.Toast("Laterne: " + n + (n == "Aus" ? "" : "  (leuchtet, wenn es dunkel ist)"));
    }
    // the old Block Island street lantern (1.0 block "Latern")
    public static bool IsStreetLantern(Block b) { return b is GameObjectBlock && b.name == "Latern"; }
    // the player stands on top of the block in cell c
    public static bool StandsOn(Vector3i c, GameObject pl) {
        if(pl == null) return false; Vector3 p = pl.transform.position;
        return p.x >= c.x - .05f && p.x <= c.x + 1.05f && p.z >= c.z - .05f && p.z <= c.z + 1.05f && p.y > c.y + .8f && p.y < c.y + 2.9f;
    }
    public static AudioSource Speaker(GameObject go, float maxDistance) {
        var a = go.AddComponent<AudioSource>(); a.spatialBlend = 1f; a.rolloffMode = AudioRolloffMode.Linear; a.minDistance = 3f; a.maxDistance = maxDistance; a.playOnAwake = false; return a;
    }
}

public sealed class FBNothing : MonoBehaviour { }

// A full block with a light plate on front and back carrying rows of text (world clocks, tides, ...).
sealed class FBPanel {
    readonly List<TextMesh>[] rows;
    static Font font; static Material textMat;
    public FBPanel(Transform t, Color body, Color plate, float[] y, float[] size, Color[] ink) {
        if(font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if(textMat == null) { Shader ts = Resources.Load<Shader>("Portal/SignText"); textMat = ts != null ? new Material(ts) { mainTexture = font.material.mainTexture } : font.material; }
        FunctionBlocks.Part(t, PrimitiveType.Cube, "Body", Vector3.zero, Vector3.one, FunctionBlocks.Solid(body, FunctionBlocks.Surface.Metal));
        rows = new List<TextMesh>[y.Length]; for(int i = 0; i < y.Length; i++) rows[i] = new List<TextMesh>();
        for(int side = 0; side < 2; side++) {
            float z = (side == 0 ? -1 : 1) * .505f;
            var pl = FunctionBlocks.Part(t, PrimitiveType.Cube, "Plate", new Vector3(0, 0, z), new Vector3(.9f, .9f, .004f), FunctionBlocks.Solid(plate));
            pl.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for(int i = 0; i < y.Length; i++) {
                var g = new GameObject("Row " + i); g.transform.SetParent(t, false); g.transform.localPosition = new Vector3(0, y[i], z * 1.01f);
                g.transform.localRotation = side == 0 ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
                var tm = g.AddComponent<TextMesh>(); tm.font = font; tm.fontSize = 96; tm.characterSize = size[i] / 9.6f; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = ink[i];
                var mr = g.GetComponent<MeshRenderer>(); mr.sharedMaterial = textMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                g.AddComponent<SignTextFit>().Init(tm.characterSize, .82f);
                rows[i].Add(tm);
            }
        }
    }
    public void Set(int row, string text) { foreach(TextMesh t in rows[row]) if(t.text != text) t.text = text; }
}

// ====================================================================== Wetterstation
// Mast with cup anemometer turning with the real wind on Block Island, a wind vane pointing where the wind comes from
// (north = +z in the world) and a small thermometer plate.
public sealed class FBWeatherStation : MonoBehaviour {
    Transform cups, vane; TextMesh temp; float spin;
    void Start() {
        if(!FunctionBlocks.IsPlaced(transform)) return;
        BlockIslandRealTime.EnsureUpdating();
        transform.rotation = Quaternion.identity;                     // the vane needs true north
        Material metal = FunctionBlocks.Solid(new Color(.75f, .76f, .78f), FunctionBlocks.Surface.Metal), dark = FunctionBlocks.Solid(new Color(.2f, .2f, .22f)), red = FunctionBlocks.Solid(new Color(.75f, .15f, .12f));
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Box", new Vector3(0, -.3f, 0), new Vector3(.5f, .4f, .5f), FunctionBlocks.Solid(new Color(.92f, .92f, .9f)));
        FunctionBlocks.Part(transform, PrimitiveType.Cylinder, "Mast", new Vector3(0, .15f, 0), new Vector3(.05f, .4f, .05f), metal);
        cups = new GameObject("Anemometer").transform; cups.SetParent(transform, false); cups.localPosition = new Vector3(0, .5f, 0);
        for(int i = 0; i < 3; i++) {
            Quaternion r = Quaternion.Euler(0, i * 120, 0);
            FunctionBlocks.Part(cups, PrimitiveType.Cube, "Arm", r * new Vector3(0, 0, .12f), new Vector3(.02f, .02f, .24f), metal, r);
            FunctionBlocks.Part(cups, PrimitiveType.Sphere, "Cup", r * new Vector3(0, 0, .25f), new Vector3(.09f, .09f, .09f), dark);
        }
        vane = new GameObject("Vane").transform; vane.SetParent(transform, false); vane.localPosition = new Vector3(0, .37f, 0);
        FunctionBlocks.Part(vane, PrimitiveType.Cube, "Rod", new Vector3(0, 0, 0), new Vector3(.02f, .02f, .5f), dark);
        FunctionBlocks.Part(vane, PrimitiveType.Cube, "Arrow", new Vector3(0, 0, .25f), new Vector3(.08f, .02f, .08f), red, Quaternion.Euler(0, 45, 0));
        FunctionBlocks.Part(vane, PrimitiveType.Cube, "Tail", new Vector3(0, .04f, -.22f), new Vector3(.01f, .12f, .12f), dark);
        var tg = new GameObject("Temperature"); tg.transform.SetParent(transform, false); tg.transform.localPosition = new Vector3(0, -.3f, -.255f);
        temp = tg.AddComponent<TextMesh>(); temp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); temp.fontSize = 96; temp.characterSize = .11f / 9.6f; temp.anchor = TextAnchor.MiddleCenter; temp.color = new Color(.15f, .15f, .2f);
        Shader ts = Resources.Load<Shader>("Portal/SignText"); tg.GetComponent<MeshRenderer>().sharedMaterial = ts != null ? new Material(ts) { mainTexture = temp.font.material.mainTexture } : temp.font.material;
    }
    void Update() {
        if(cups == null) return;
        float wind = BlockIslandRealTime.HasWeather ? BlockIslandRealTime.Wind : 8f;
        spin = Mathf.Lerp(spin, 40f + wind * 28f, Time.deltaTime);
        cups.Rotate(0, spin * Time.deltaTime, 0);
        float dir = BlockIslandRealTime.HasWeather ? BlockIslandRealTime.WindDirection : 0f;
        vane.localRotation = Quaternion.Slerp(vane.localRotation, Quaternion.Euler(0, dir + Mathf.Sin(Time.time * 1.3f) * 4f, 0), Time.deltaTime * 2f);
        temp.text = BlockIslandRealTime.HasWeather ? Mathf.RoundToInt(BlockIslandRealTime.Temperature) + " °C" : "– °C";
    }
}

// ====================================================================== Weltzeituhren
public sealed class FBWorldClocks : MonoBehaviour {
    FBPanel panel; float next;
    void Start() {
        if(!FunctionBlocks.IsPlaced(transform)) return;
        Color ink = new Color(.12f, .12f, .14f);
        panel = new FBPanel(transform, new Color(.14f, .16f, .22f), new Color(.95f, .95f, .92f), new[] { .3f, .1f, -.1f, -.3f }, new[] { .13f, .13f, .13f, .13f }, new[] { ink, ink, ink, ink });
    }
    void Update() {
        if(panel == null || Time.unscaledTime < next) return; next = Time.unscaledTime + 1f;
        panel.Set(0, "Berlin  " + BlockIslandRealTime.CentralEurope.ToString("HH:mm"));
        panel.Set(1, "New York  " + BlockIslandRealTime.BlockIsland.ToString("HH:mm"));
        panel.Set(2, "Tokio  " + BlockIslandRealTime.Tokyo.ToString("HH:mm"));
        panel.Set(3, "Sydney  " + BlockIslandRealTime.Sydney.ToString("HH:mm"));
    }
}

// ====================================================================== Mondphase
// The real moon of today: a disc lit like the moon (northern hemisphere view) and its German name with the lit share.
public sealed class FBMoon : MonoBehaviour {
    Texture2D tex; FBPanel panel; float next;
    static readonly DateTime NewMoon = new DateTime(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc);
    public static float Phase { get { double d = (DateTime.UtcNow - NewMoon).TotalDays / 29.530588853; return (float)(d - Math.Floor(d)); } }
    public static string Name(float p) {
        if(p < .03f || p > .97f) return "Neumond";
        if(p < .22f) return "Zunehmende Sichel"; if(p < .28f) return "Erstes Viertel"; if(p < .47f) return "Zunehmender Mond";
        if(p < .53f) return "Vollmond"; if(p < .72f) return "Abnehmender Mond"; if(p < .78f) return "Letztes Viertel"; return "Abnehmende Sichel";
    }
    void Start() {
        if(!FunctionBlocks.IsPlaced(transform)) return;
        panel = new FBPanel(transform, new Color(.1f, .11f, .18f), new Color(.06f, .07f, .14f), new[] { -.36f }, new[] { .085f }, new[] { new Color(.85f, .85f, .9f) });
        tex = new Texture2D(128, 128, TextureFormat.RGB24, false) { wrapMode = TextureWrapMode.Clamp };
        var mat = FunctionBlocks.Solid(Color.white); mat.mainTexture = tex;
        for(int side = 0; side < 2; side++) {
            float z = (side == 0 ? -1 : 1) * .508f;
            var q = FunctionBlocks.Part(transform, PrimitiveType.Quad, "Moon", new Vector3(0, .08f, z), new Vector3(.62f, .62f, 1), mat, side == 0 ? Quaternion.identity : Quaternion.Euler(0, 180, 0));
            q.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
    void Update() {
        if(panel == null || Time.unscaledTime < next) return; next = Time.unscaledTime + 60f;
        float p = Phase;
        int lit = Mathf.RoundToInt((1 - Mathf.Cos(2 * Mathf.PI * p)) * 50f);
        panel.Set(0, Name(p) + "  ·  " + lit + " %");
        Paint(p);
    }
    void Paint(float p) {
        int n = tex.width; var px = new Color[n * n];
        Color sky = new Color(.05f, .06f, .13f), dark = new Color(.13f, .13f, .17f), light = new Color(.93f, .92f, .85f);
        float c = Mathf.Cos(2 * Mathf.PI * (p < .5f ? p : p - .5f));
        for(int y = 0; y < n; y++) for(int x = 0; x < n; x++) {
            float u = (x + .5f) / n * 2 - 1, v = (y + .5f) / n * 2 - 1, d = u * u + v * v;
            if(d > .9f) { px[y * n + x] = sky; continue; }
            float r = Mathf.Sqrt(.9f - v * v), uu = u;
            bool on = p < .5f ? uu > r * c : uu < r * c;
            float crater = Mathf.PerlinNoise(x * .09f, y * .09f) * .12f;
            px[y * n + x] = (on ? light : dark) * (1 - crater);
        }
        tex.SetPixels(px); tex.Apply();
    }
}

// ====================================================================== Gezeitentafel Block Island
// Next high and low water at Block Island (NOAA station 8459681, Great Salt Pond), refreshed every 3 hours.
public sealed class FBTides : MonoBehaviour {
    FBPanel panel; float next;
    void Start() {
        if(!FunctionBlocks.IsPlaced(transform)) return;
        TideData.EnsureUpdating();
        Color blue = new Color(.1f, .25f, .45f), ink = new Color(.12f, .12f, .14f);
        panel = new FBPanel(transform, new Color(.1f, .22f, .36f), new Color(.93f, .96f, .98f), new[] { .3f, .08f, -.12f, -.32f }, new[] { .09f, .11f, .11f, .1f }, new[] { blue, ink, ink, blue });
    }
    void Update() {
        if(panel == null || Time.unscaledTime < next) return; next = Time.unscaledTime + 5f;
        panel.Set(0, "Gezeiten Block Island");
        DateTime now = BlockIslandRealTime.BlockIsland;
        TideData.Event hi, lo;
        if(!TideData.Next(now, true, out hi) || !TideData.Next(now, false, out lo)) {
            panel.Set(1, TideData.Failed ? "Keine Internet-" : "Gezeiten werden"); panel.Set(2, TideData.Failed ? "verbindung" : "geladen …"); panel.Set(3, ""); return;
        }
        panel.Set(1, "Hochwasser  " + hi.time.ToString("HH:mm") + "  (" + hi.height.ToString("0.00").Replace('.', ',') + " m)");
        panel.Set(2, "Niedrigwasser  " + lo.time.ToString("HH:mm") + "  (" + lo.height.ToString("0.00").Replace('.', ',') + " m)");
        panel.Set(3, hi.time < lo.time ? "Das Wasser steigt (Flut)" : "Das Wasser fällt (Ebbe)");
    }
}

public static class TideData {
    public struct Event { public DateTime time; public float height; public bool high; }
    static readonly List<Event> events = new List<Event>();
    public static bool Failed { get; private set; }
    [Serializable] class P { public string t; public string v; public string type; }
    [Serializable] class R { public P[] predictions; }
    static Fetcher fetcher;
    public static void EnsureUpdating() { if(fetcher != null) return; var go = new GameObject("Block Island Tides (Internet)"); UnityEngine.Object.DontDestroyOnLoad(go); go.hideFlags = HideFlags.HideInHierarchy; fetcher = go.AddComponent<Fetcher>(); }
    public static bool Next(DateTime after, bool high, out Event e) {
        foreach(Event x in events) if(x.high == high && x.time > after) { e = x; return true; }
        e = default(Event); return false;
    }
    sealed class Fetcher : MonoBehaviour {
        IEnumerator Start() {
            while(true) {
                DateTime day = BlockIslandRealTime.BlockIsland.AddDays(-1);
                string url = "https://api.tidesandcurrents.noaa.gov/api/prod/datagetter?product=predictions&application=BlockIslandGame&begin_date=" + day.ToString("yyyyMMdd")
                    + "&range=96&datum=MLLW&station=8459681&time_zone=lst_ldt&units=metric&interval=hilo&format=json";
                bool ok = false;
                using(UnityWebRequest r = UnityWebRequest.Get(url)) {
                    r.timeout = 20; yield return r.SendWebRequest();
                    if(r.result == UnityWebRequest.Result.Success) {
                        try {
                            R rep = JsonUtility.FromJson<R>(r.downloadHandler.text);
                            if(rep != null && rep.predictions != null && rep.predictions.Length > 0) {
                                events.Clear();
                                foreach(P p in rep.predictions) {
                                    DateTime t; float h;
                                    if(DateTime.TryParse(p.t, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out t) && float.TryParse(p.v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out h))
                                        events.Add(new Event { time = t, height = h, high = p.type == "H" });
                                }
                                ok = true; Failed = false; Debug.Log("BLOCK ISLAND TIDES: " + events.Count + " events");
                            }
                        } catch(Exception e) { Debug.LogWarning("BLOCK ISLAND TIDES: " + e.Message); }
                    }
                    if(!ok) { Failed = events.Count == 0; Debug.Log("BLOCK ISLAND TIDES: no connection (" + r.error + ")"); }
                }
                yield return new WaitForSecondsRealtime(ok ? 3 * 3600f : 120f);
            }
        }
    }
}

// ====================================================================== Feuerwerk
// Launcher with four tubes. Enter fires a salvo; at midnight (Central European time) it fires a big show by itself.
public sealed class FBFireworks : MonoBehaviour {
    static readonly List<FBFireworks> all = new List<FBFireworks>();
    Vector3i cell; AudioSource speaker; int lastMidnightDay = -1; int toLaunch; float nextLaunch;
    static AudioClip boom; static Material sparkMat;
    void Start() {
        if(!FunctionBlocks.IsPlaced(transform)) return;
        cell = FunctionBlocks.CellOf(transform);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Crate", new Vector3(0, -.25f, 0), new Vector3(.9f, .5f, .9f), FunctionBlocks.Solid(new Color(.45f, .12f, .1f), FunctionBlocks.Surface.Wood));
        for(int i = 0; i < 4; i++) {
            float x = (i % 2 == 0 ? -1 : 1) * .2f, z = (i < 2 ? -1 : 1) * .2f;
            FunctionBlocks.Part(transform, PrimitiveType.Cylinder, "Tube", new Vector3(x, .12f, z), new Vector3(.22f, .25f, .22f), FunctionBlocks.Solid(i % 2 == 0 ? new Color(.85f, .75f, .2f) : new Color(.2f, .35f, .7f)));
        }
        speaker = FunctionBlocks.Speaker(gameObject, 120f); speaker.volume = .9f;
        DateTime now = BlockIslandRealTime.CentralEurope; if(now.Hour == 0 && now.Minute == 0) lastMidnightDay = now.DayOfYear;
        all.Add(this);
        FunctionBlockUI.Ensure();
    }
    void OnDestroy() { all.Remove(this); }
    public static void LaunchAt(Vector3i p) { foreach(FBFireworks f in all) if(f.cell.Equals(p)) { f.toLaunch += 5; FunctionBlockUI.Toast("Feuerwerk!"); } }
    void Update() {
        if(speaker == null) return;
        DateTime now = BlockIslandRealTime.CentralEurope;
        if(now.Hour == 0 && now.Minute == 0 && lastMidnightDay != now.DayOfYear) { lastMidnightDay = now.DayOfYear; toLaunch += 20; }
        if(toLaunch > 0 && Time.time >= nextLaunch) { toLaunch--; nextLaunch = Time.time + UnityEngine.Random.Range(.4f, 1.1f); StartCoroutine(Rocket()); }
    }
    IEnumerator Rocket() {
        if(sparkMat == null) sparkMat = Resources.Load<Material>("Portal/PortalStars");
        Vector3 start = transform.position + new Vector3(UnityEngine.Random.Range(-.2f, .2f), .4f, UnityEngine.Random.Range(-.2f, .2f));
        Vector3 top = start + new Vector3(UnityEngine.Random.Range(-4f, 4f), UnityEngine.Random.Range(18f, 28f), UnityEngine.Random.Range(-4f, 4f));
        var rocket = new GameObject("Rocket"); rocket.transform.position = start;
        var trail = rocket.AddComponent<ParticleSystem>(); trail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var tm = trail.main; tm.startLifetime = .6f; tm.startSpeed = 0f; tm.startSize = .12f; tm.simulationSpace = ParticleSystemSimulationSpace.World; tm.startColor = new Color(1f, .8f, .45f);
        var te = trail.emission; te.rateOverTime = 70f; var ts = trail.shape; ts.enabled = false;
        if(sparkMat != null) rocket.GetComponent<ParticleSystemRenderer>().sharedMaterial = sparkMat;
        trail.Play();
        for(float t = 0; t < 1.3f; t += Time.deltaTime) { rocket.transform.position = Vector3.Lerp(start, top, 1 - (1 - t / 1.3f) * (1 - t / 1.3f)); yield return null; }
        // burst
        Color c = Color.HSVToRGB(UnityEngine.Random.value, .75f, 1f);
        var burst = new GameObject("Burst"); burst.transform.position = top;
        var ps = burst.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var m = ps.main; m.loop = false; m.duration = .1f; m.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.2f); m.startSpeed = new ParticleSystem.MinMaxCurve(6f, 9f); m.startSize = new ParticleSystem.MinMaxCurve(.15f, .3f);
        m.gravityModifier = .35f; m.simulationSpace = ParticleSystemSimulationSpace.World; m.startColor = new ParticleSystem.MinMaxGradient(c, Color.Lerp(c, Color.white, .5f)); m.maxParticles = 250;
        var e = ps.emission; e.rateOverTime = 0; e.SetBursts(new[] { new ParticleSystem.Burst(0f, 180) });
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = .2f;
        var col = ps.colorOverLifetime; col.enabled = true; var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, .6f), new GradientAlphaKey(0, 1) }); col.color = g;
        var damp = ps.limitVelocityOverLifetime; damp.enabled = true; damp.drag = 1.2f;
        if(sparkMat != null) burst.GetComponent<ParticleSystemRenderer>().sharedMaterial = sparkMat;
        ps.Play();
        var flash = burst.AddComponent<Light>(); flash.color = c; flash.range = 30f; flash.intensity = 2.5f; flash.shadows = LightShadows.None;
        trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        var a = FunctionBlocks.Speaker(burst, 140f); a.volume = .8f; a.clip = Boom(); a.pitch = UnityEngine.Random.Range(.85f, 1.15f); a.Play();
        for(float t = 0; t < .5f; t += Time.deltaTime) { flash.intensity = 2.5f * (1 - t / .5f); yield return null; }
        Destroy(flash);
        Destroy(rocket, 1f); Destroy(burst, 5f);
    }
    static AudioClip Boom() {
        if(boom != null) return boom;
        const int rate = 44100; int n = rate * 2; var d = new float[n]; var rnd = new System.Random(3); float lp = 0;
        for(int i = 0; i < n; i++) { float t = i / (float)rate; float noise = (float)rnd.NextDouble() * 2 - 1; lp += (noise - lp) * .08f; d[i] = (lp * 3f + Mathf.Sin(2 * Mathf.PI * 55 * t) * .5f) * Mathf.Exp(-t * 3.2f) * Mathf.Min(1, t * 2000); }
        float mx = 0; foreach(float v in d) mx = Mathf.Max(mx, Mathf.Abs(v)); for(int i = 0; i < n; i++) d[i] /= mx;
        boom = AudioClip.Create("Boom", n, 1, rate, false); boom.SetData(d, 0); return boom;
    }
}

// ====================================================================== Aufzug
// Elevator pad. Several pads in one column are floors of one elevator: stand on a pad for a moment and it carries
// you to the next floor above (from the top floor back down to the lowest).
public sealed class FBElevator : MonoBehaviour {
    static Vector3i? arrivedOn; static float standSince = -1; static FBElevator standingOn; public static bool Riding;
    Vector3i cell;
    void Start() {
        if(!FunctionBlocks.IsPlaced(transform)) return;
        cell = FunctionBlocks.CellOf(transform);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Base", Vector3.zero, Vector3.one, FunctionBlocks.Solid(new Color(.3f, .31f, .33f), FunctionBlocks.Surface.Stone));
        Material yellow = FunctionBlocks.Solid(new Color(.95f, .78f, .1f)), black = FunctionBlocks.Solid(new Color(.1f, .1f, .1f));
        for(int i = 0; i < 4; i++) {
            Quaternion r = Quaternion.Euler(0, i * 90, 0);
            FunctionBlocks.Part(transform, PrimitiveType.Cube, "Edge", r * new Vector3(0, .503f, .43f), r * new Vector3(1f, .006f, .14f), yellow);
        }
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Arrow", new Vector3(0, .504f, 0), new Vector3(.2f, .006f, .2f), black, Quaternion.Euler(0, 45, 0));
    }
    void Update() {
        if(cell.Equals(default(Vector3i)) && !FunctionBlocks.IsPlaced(transform)) return;
        GameObject pl = FunctionBlocks.Player();
        if(pl == null || Riding || FBTeleportFade.Busy || PortalTransition.Busy || GameState.IsPause) return;
        bool on = FunctionBlocks.StandsOn(cell, pl);
        if(!on) { if(arrivedOn.HasValue && arrivedOn.Value.Equals(cell)) arrivedOn = null; if(standingOn == this) standingOn = null; return; }
        if(arrivedOn.HasValue && arrivedOn.Value.Equals(cell)) return;
        if(standingOn != this) { standingOn = this; standSince = Time.time; }
        if(Time.time - standSince < .6f) return;
        standingOn = null;
        Vector3i? target = NextFloor();
        if(!target.HasValue) { FunctionBlockUI.Toast("Setze einen zweiten Aufzug-Block senkrecht darüber oder darunter."); arrivedOn = cell; return; }
        arrivedOn = target.Value;
        StartCoroutine(Ride(pl, target.Value));
    }
    Vector3i? NextFloor() {
        var floors = new List<int>();
        for(int dy = -96; dy <= 96; dy++) {
            int y = cell.y + dy; if(y < 0 || y > 255) continue;
            var fb = Map.instance.GetBlock(new Vector3i(cell.x, y, cell.z)).block as FunctionBlock;
            if(fb != null && fb.def != null && fb.def.component == typeof(FBElevator)) floors.Add(y);
        }
        if(floors.Count < 2) return null;
        floors.Sort(); int i = floors.IndexOf(cell.y);
        return new Vector3i(cell.x, floors[(i + 1) % floors.Count], cell.z);
    }
    IEnumerator Ride(GameObject pl, Vector3i to) {
        Riding = true;
        var motor = pl.GetComponent<CharacterMotor>(); bool was = motor != null && motor.enabled; if(motor != null) motor.enabled = false;
        Vector3 a = pl.transform.position, b = new Vector3(to.x + .5f, to.y + 2.05f, to.z + .5f);
        float dur = Mathf.Max(.8f, Vector3.Distance(a, b) / 5f);
        for(float t = 0; t < dur; t += Time.deltaTime) { float k = Mathf.SmoothStep(0, 1, t / dur); pl.transform.position = Vector3.Lerp(a, b, k); yield return null; }
        pl.transform.position = b;
        if(motor != null && was) motor.enabled = true;
        Riding = false;
    }
}

// ====================================================================== Förderband
// Conveyor belt: carries whoever stands on it in the direction the builder was looking when placing it.
public sealed class FBConveyor : MonoBehaviour {
    Vector3i cell; readonly List<Transform> slats = new List<Transform>(); float offset;
    const float Speed = 2.6f;
    void Start() {
        if(!FunctionBlocks.IsPlaced(transform)) return;
        cell = FunctionBlocks.CellOf(transform);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Frame", Vector3.zero, Vector3.one, FunctionBlocks.Solid(new Color(.38f, .38f, .4f), FunctionBlocks.Surface.Metal));
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Belt", new Vector3(0, .503f, 0), new Vector3(.86f, .006f, 1f), FunctionBlocks.Solid(new Color(.1f, .1f, .11f)));
        Material slat = FunctionBlocks.Solid(new Color(.55f, .55f, .58f), FunctionBlocks.Surface.Metal);
        for(int i = 0; i < 4; i++) slats.Add(FunctionBlocks.Part(transform, PrimitiveType.Cube, "Slat", Vector3.zero, new Vector3(.86f, .01f, .05f), slat).transform);
    }
    void Update() {
        if(slats.Count == 0) return;
        offset = Mathf.Repeat(offset + Speed * .4f * Time.deltaTime, .25f);
        for(int i = 0; i < slats.Count; i++) slats[i].localPosition = new Vector3(0, .507f, .5f - Mathf.Repeat(i * .25f + offset, 1f));   // moving towards local -z
        GameObject pl = FunctionBlocks.Player();
        if(pl == null || !FunctionBlocks.StandsOn(cell, pl) || pl.transform.position.y > cell.y + 2.2f) return;
        var motor = pl.GetComponent<CharacterMotor>();
        Vector3 v = -transform.forward * Speed;
        if(motor != null && motor.enabled) { motor.Push(v); return; }
        // flying (creative default): move the player directly, but never into a wall
        Vector3 next = pl.transform.position + v * Time.deltaTime, probe = next + v.normalized * .35f;
        int footY = cell.y + 1;                                            // the layer above the belt
        bool free = Map.instance.GetBlock(new Vector3i(Mathf.FloorToInt(probe.x), footY, Mathf.FloorToInt(probe.z))).IsEmpty()
                 && Map.instance.GetBlock(new Vector3i(Mathf.FloorToInt(probe.x), footY + 1, Mathf.FloorToInt(probe.z))).IsEmpty();
        if(free) pl.transform.position = next;
    }
}

// ====================================================================== Sprungfeder
public sealed class FBSpring : MonoBehaviour {
    Vector3i cell; Transform plate, coil; float squash, cooldown; AudioSource speaker; static AudioClip boing;
    void Start() {
        if(!FunctionBlocks.IsPlaced(transform)) return;
        cell = FunctionBlocks.CellOf(transform);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Base", new Vector3(0, -.42f, 0), new Vector3(1f, .16f, 1f), FunctionBlocks.Solid(new Color(.25f, .25f, .28f), FunctionBlocks.Surface.Metal));
        coil = new GameObject("Coil").transform; coil.SetParent(transform, false); coil.localPosition = new Vector3(0, -.34f, 0);
        Material steel = FunctionBlocks.Solid(new Color(.7f, .72f, .75f), FunctionBlocks.Surface.Metal);
        for(int i = 0; i < 5; i++) FunctionBlocks.Part(coil, PrimitiveType.Cylinder, "Ring", new Vector3(0, .07f + i * .14f, 0), new Vector3(.6f, .02f, .6f), steel);
        plate = FunctionBlocks.Part(transform, PrimitiveType.Cube, "Plate", new Vector3(0, .45f, 0), new Vector3(1f, .1f, 1f), FunctionBlocks.Solid(new Color(.85f, .2f, .15f))).transform;
        speaker = FunctionBlocks.Speaker(gameObject, 30f);
    }
    void Update() {
        if(plate == null) return;
        squash = Mathf.MoveTowards(squash, 0, Time.deltaTime * 3f);
        plate.localPosition = new Vector3(0, .45f - squash * .25f, 0); coil.localScale = new Vector3(1, 1 - squash * .35f, 1);
        GameObject pl = FunctionBlocks.Player();
        if(pl == null || Time.time < cooldown || !FunctionBlocks.StandsOn(cell, pl) || pl.transform.position.y > cell.y + 2.3f) return;
        var motor = pl.GetComponent<CharacterMotor>();
        if(motor != null && motor.enabled) motor.Launch(15f); else StartCoroutine(FlyUp(pl));   // flying: lift in an arc
        squash = 1f; cooldown = Time.time + .9f;
        speaker.PlayOneShot(Boing(), .7f);
    }
    IEnumerator FlyUp(GameObject pl) {
        Vector3 a = pl.transform.position; float top = 5f;
        for(float t = 0; t < .6f; t += Time.deltaTime) {
            float k = 1 - (1 - t / .6f) * (1 - t / .6f);
            Vector3 p = pl.transform.position; p.y = a.y + top * k;
            if(!Map.instance.GetBlock(new Vector3i(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y + 1f), Mathf.FloorToInt(p.z))).IsEmpty()) yield break;
            pl.transform.position = p; yield return null;
        }
    }
    static AudioClip Boing() {
        if(boing != null) return boing;
        const int rate = 44100; int n = (int)(rate * .5f); var d = new float[n]; float ph = 0;
        for(int i = 0; i < n; i++) { float t = i / (float)rate; float f = 180 + 420 * t + 30 * Mathf.Sin(t * 60); ph += 2 * Mathf.PI * f / rate; d[i] = Mathf.Sin(ph) * Mathf.Exp(-t * 6) * .8f; }
        boing = AudioClip.Create("Boing", n, 1, rate, false); boing.SetData(d, 0); return boing;
    }
}

// ====================================================================== Tür (automatisch)
// Two blocks high. Opens by itself when the player comes close and closes again behind him; open cells are passable.
public sealed class FBDoor : MonoBehaviour {
    Vector3i cell; Transform leaf; float open;
    void Start() {
        if(!FunctionBlocks.IsPlaced(transform)) return;
        cell = FunctionBlocks.CellOf(transform);
        Material frame = FunctionBlocks.Solid(new Color(.3f, .2f, .12f), FunctionBlocks.Surface.Wood), wood = FunctionBlocks.Solid(new Color(.52f, .34f, .19f), FunctionBlocks.Surface.Wood), brass = FunctionBlocks.Solid(new Color(.85f, .68f, .3f));
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Jamb L", new Vector3(-.46f, .5f, 0), new Vector3(.08f, 2f, .3f), frame);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Jamb R", new Vector3(.46f, .5f, 0), new Vector3(.08f, 2f, .3f), frame);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Lintel", new Vector3(0, 1.46f, 0), new Vector3(1f, .08f, .3f), frame);
        leaf = new GameObject("Leaf").transform; leaf.SetParent(transform, false); leaf.localPosition = new Vector3(-.42f, .5f, 0);
        FunctionBlocks.Part(leaf, PrimitiveType.Cube, "Panel", new Vector3(.42f, -.04f, 0), new Vector3(.84f, 1.84f, .08f), wood);
        FunctionBlocks.Part(leaf, PrimitiveType.Cube, "Inlay", new Vector3(.42f, .4f, 0), new Vector3(.6f, .7f, .1f), FunctionBlocks.Solid(new Color(.45f, .29f, .16f)));
        FunctionBlocks.Part(leaf, PrimitiveType.Cube, "Inlay", new Vector3(.42f, -.5f, 0), new Vector3(.6f, .7f, .1f), FunctionBlocks.Solid(new Color(.45f, .29f, .16f)));
        FunctionBlocks.Part(leaf, PrimitiveType.Sphere, "Knob", new Vector3(.72f, -.05f, 0), new Vector3(.07f, .07f, .16f), brass);
    }
    void OnDestroy() { if(leaf != null) { FunctionBlocks.SetOpen(cell, false); FunctionBlocks.SetOpen(cell + new Vector3i(0, 1, 0), false); } }
    void Update() {
        if(leaf == null) return;
        GameObject pl = FunctionBlocks.Player();
        bool near = false;
        if(pl != null) { Vector3 d = pl.transform.position - (transform.position + Vector3.up * .5f); near = new Vector2(d.x, d.z).magnitude < 2.3f && Mathf.Abs(d.y) < 2.5f; }
        open = Mathf.MoveTowards(open, near ? 1f : 0f, Time.deltaTime * 3f);
        leaf.localRotation = Quaternion.Euler(0, -95f * Mathf.SmoothStep(0, 1, open), 0);
        bool passable = open > .5f;
        FunctionBlocks.SetOpen(cell, passable); FunctionBlocks.SetOpen(cell + new Vector3i(0, 1, 0), passable);
    }
}

// ====================================================================== Minecart-Bahnhof
// Platform with a station sign. A minecart passing a rail next to it stops for a few seconds.
public sealed class FBStation : MonoBehaviour {
    void Start() {
        if(!FunctionBlocks.IsPlaced(transform)) return;
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Platform", new Vector3(0, -.25f, 0), new Vector3(1f, .5f, 1f), FunctionBlocks.Solid(new Color(.62f, .6f, .56f), FunctionBlocks.Surface.Stone));
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Edge", new Vector3(0, .005f, 0), new Vector3(1f, .01f, 1f), FunctionBlocks.Solid(new Color(.95f, .85f, .2f)));
        FunctionBlocks.Part(transform, PrimitiveType.Cylinder, "Post", new Vector3(.3f, .25f, .3f), new Vector3(.05f, .25f, .05f), FunctionBlocks.Solid(new Color(.15f, .15f, .17f)));
        var sign = FunctionBlocks.Part(transform, PrimitiveType.Cube, "Sign", new Vector3(.3f, .45f, .3f), new Vector3(.36f, .14f, .03f), FunctionBlocks.Solid(new Color(.1f, .3f, .6f)));
        var tg = new GameObject("Name"); tg.transform.SetParent(sign.transform.parent, false); tg.transform.localPosition = new Vector3(.3f, .45f, .28f);
        var tm = tg.AddComponent<TextMesh>(); tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); tm.fontSize = 96; tm.characterSize = .07f / 9.6f; tm.anchor = TextAnchor.MiddleCenter; tm.color = Color.white; tm.text = "Bahnhof";
        Shader ts = Resources.Load<Shader>("Portal/SignText"); tg.GetComponent<MeshRenderer>().sharedMaterial = ts != null ? new Material(ts) { mainTexture = tm.font.material.mainTexture } : tm.font.material;
    }
}

// ====================================================================== Weiche (Minecart)
// Lever next to a rail junction. Enter switches between "geradeaus" and "abbiegen"; a minecart takes the side branch when set.
public sealed class FBSwitch : MonoBehaviour {
    static readonly List<FBSwitch> all = new List<FBSwitch>();
    Vector3i cell; Transform lever; Material knob; int shown = -1;
    void Start() {
        if(!FunctionBlocks.IsPlaced(transform)) return;
        cell = FunctionBlocks.CellOf(transform);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Base", new Vector3(0, -.4f, 0), new Vector3(.6f, .2f, .4f), FunctionBlocks.Solid(new Color(.25f, .25f, .27f), FunctionBlocks.Surface.Metal));
        lever = new GameObject("Lever").transform; lever.SetParent(transform, false); lever.localPosition = new Vector3(0, -.3f, 0);
        FunctionBlocks.Part(lever, PrimitiveType.Cube, "Arm", new Vector3(0, .3f, 0), new Vector3(.05f, .6f, .05f), FunctionBlocks.Solid(new Color(.7f, .7f, .72f)));
        knob = FunctionBlocks.Solid(Color.white);
        FunctionBlocks.Part(lever, PrimitiveType.Sphere, "Knob", new Vector3(0, .62f, 0), new Vector3(.13f, .13f, .13f), knob);
        all.Add(this);
        FunctionBlockUI.Ensure();
    }
    void OnDestroy() { all.Remove(this); }
    public static void ToggleAt(Vector3i p) {
        bool turn = FunctionBlockData.Get(p, "geradeaus") != "abbiegen";
        FunctionBlockData.Set(p, turn ? "abbiegen" : "geradeaus");
        FunctionBlockUI.Toast(turn ? "Weiche: abbiegen" : "Weiche: geradeaus");
    }
    void Update() {
        if(lever == null || shown == FunctionBlockData.Version) return;
        shown = FunctionBlockData.Version;
        bool turn = FunctionBlockData.Get(cell, "geradeaus") == "abbiegen";
        lever.localRotation = Quaternion.Euler(0, 0, turn ? -35f : 35f);
        knob.color = turn ? new Color(.95f, .6f, .1f) : new Color(.2f, .75f, .3f);
    }
}

// ====================================================================== Musikbox
// Plays the MP3s of the music folder (the one chosen in the pause menu, or Documents/Block Island/Musik) around itself.
// Enter starts it / skips to the next title.
public sealed class FBJukebox : MonoBehaviour {
    static readonly List<FBJukebox> all = new List<FBJukebox>();
    Vector3i cell; AudioSource speaker; int index = -1; bool loading, wantPlay; Material glow; AudioClip current;
    void Start() {
        if(!FunctionBlocks.IsPlaced(transform)) return;
        cell = FunctionBlocks.CellOf(transform);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Cabinet", Vector3.zero, new Vector3(.9f, 1f, .7f), FunctionBlocks.Solid(new Color(.4f, .22f, .12f), FunctionBlocks.Surface.Wood));
        FunctionBlocks.Part(transform, PrimitiveType.Sphere, "Top", new Vector3(0, .45f, 0), new Vector3(.9f, .3f, .7f), FunctionBlocks.Solid(new Color(.4f, .22f, .12f)));
        glow = FunctionBlocks.Glow(new Color(1f, .55f, .2f), .3f);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Grill", new Vector3(0, -.1f, .352f), new Vector3(.6f, .5f, .01f), glow);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Grill", new Vector3(0, -.1f, -.352f), new Vector3(.6f, .5f, .01f), glow);
        speaker = FunctionBlocks.Speaker(gameObject, 32f); speaker.volume = .8f;
        all.Add(this);
        FunctionBlockUI.Ensure();
    }
    void OnDestroy() { all.Remove(this); if(current != null) Destroy(current); }
    public static void NextAt(Vector3i p) { foreach(FBJukebox j in all) if(j.cell.Equals(p)) j.Next(); }
    static List<string> Tracks() {
        var mp = InfiniteMusicPlayer.Instance;
        if(mp != null && mp.tracks.Count > 0) return mp.tracks;
        var l = new List<string>();
        try { l.AddRange(Directory.GetFiles(BlockIslandPaths.Folder(BlockIslandPaths.Music), "*.mp3", SearchOption.AllDirectories)); } catch { }
        l.Sort(StringComparer.OrdinalIgnoreCase); return l;
    }
    void Next() {
        var t = Tracks();
        if(t.Count == 0) { FunctionBlockUI.Toast("Keine MP3-Dateien im Musikordner (Dokumente/Block Island/Musik)."); return; }
        index = (index + 1) % t.Count; wantPlay = true;
        StartCoroutine(Load(t[index]));
    }
    IEnumerator Load(string file) {
        if(loading) yield break; loading = true;
        using(var r = UnityWebRequestMultimedia.GetAudioClip(new Uri(file).AbsoluteUri, AudioType.MPEG)) {
            ((DownloadHandlerAudioClip)r.downloadHandler).streamAudio = true;
            yield return r.SendWebRequest();
            if(r.result == UnityWebRequest.Result.Success) {
                if(current != null) Destroy(current);
                current = DownloadHandlerAudioClip.GetContent(r); speaker.clip = current; speaker.Play();
                FunctionBlockUI.Toast("Musikbox: " + Path.GetFileNameWithoutExtension(file));
            } else FunctionBlockUI.Toast("Titel nicht abspielbar: " + Path.GetFileName(file));
        }
        loading = false;
    }
    void Update() {
        if(speaker == null) return;
        glow.SetFloat("_Intensity", speaker.isPlaying ? 1.2f + Mathf.Sin(Time.time * 6f) * .3f : .3f);
        if(wantPlay && !loading && current != null && !speaker.isPlaying && speaker.time == 0f && current.loadState == AudioDataLoadState.Loaded) Next();
    }
}

// ====================================================================== Naturklang
// Ambient nature sounds, made by the game itself: automatically birds by day and crickets at night, or fixed water,
// wind, birds or crickets. Enter switches.
public sealed class FBNatureSound : MonoBehaviour {
    static readonly List<FBNatureSound> all = new List<FBNatureSound>();
    static readonly string[] Modes = { "Automatisch", "Vögel", "Grillen", "Wasser", "Wind", "Aus" };
    static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    Vector3i cell; AudioSource speaker; string playing; float next;
    void Start() {
        if(!FunctionBlocks.IsPlaced(transform)) return;
        cell = FunctionBlocks.CellOf(transform);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Stone", new Vector3(0, -.15f, 0), new Vector3(.9f, .7f, .9f), FunctionBlocks.Solid(new Color(.45f, .47f, .44f), FunctionBlocks.Surface.Stone));
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Moss", new Vector3(0, .21f, 0), new Vector3(.92f, .06f, .92f), FunctionBlocks.Solid(new Color(.25f, .45f, .18f)));
        Material leaf = FunctionBlocks.Solid(new Color(.3f, .6f, .2f));
        for(int i = 0; i < 5; i++) { float a = i * 72 * Mathf.Deg2Rad; FunctionBlocks.Part(transform, PrimitiveType.Cube, "Tuft", new Vector3(Mathf.Cos(a) * .25f, .33f, Mathf.Sin(a) * .25f), new Vector3(.04f, .2f, .12f), leaf, Quaternion.Euler(10, i * 72, 0)); }
        speaker = FunctionBlocks.Speaker(gameObject, 26f); speaker.loop = true; speaker.volume = .7f;
        all.Add(this);
        FunctionBlockUI.Ensure();
    }
    void OnDestroy() { all.Remove(this); }
    public static void CycleAt(Vector3i p) {
        string m = FunctionBlockData.Get(p, Modes[0]); int i = Array.IndexOf(Modes, m);
        string n = Modes[(i + 1) % Modes.Length]; FunctionBlockData.Set(p, n); FunctionBlockUI.Toast("Naturklang: " + n);
    }
    void Update() {
        if(speaker == null || Time.unscaledTime < next) return; next = Time.unscaledTime + 1f;
        string mode = FunctionBlockData.Get(cell, Modes[0]);
        string want = mode == "Automatisch" ? (FunctionBlocks.IsDark ? "Grillen" : "Vögel") : mode;
        if(want == playing) return;
        playing = want;
        if(want == "Aus") { speaker.Stop(); return; }
        speaker.clip = Clip(want); speaker.Play();
    }
    static AudioClip Clip(string kind) {
        AudioClip c; if(clips.TryGetValue(kind, out c)) return c;
        const int rate = 22050; int n = rate * 12; var d = new float[n]; var rnd = new System.Random(kind.Length * 31);
        Func<float> noise = () => (float)rnd.NextDouble() * 2 - 1;
        if(kind == "Wasser") { float a = 0, b = 0; for(int i = 0; i < n; i++) { a += (noise() - a) * .25f; b += (a - b) * .2f; float t = i / (float)rate; d[i] = (a - b * .6f) * (.6f + .25f * Mathf.Sin(t * 1.7f) + .15f * Mathf.Sin(t * 4.3f)); } }
        else if(kind == "Wind") { float a = 0; for(int i = 0; i < n; i++) { float t = i / (float)rate; a += (noise() - a) * (.02f + .015f * Mathf.Sin(t * .9f)); d[i] = a * 4f * (.5f + .5f * Mathf.Sin(t * 2 * Mathf.PI / 6f)); } }
        else if(kind == "Grillen") { for(int i = 0; i < n; i++) { float t = i / (float)rate; float chirp = Mathf.Sin(2 * Mathf.PI * 32 * t) > .3f && (t % .9f) < .35f ? 1 : 0; float chirp2 = Mathf.Sin(2 * Mathf.PI * 29 * t + 1) > .4f && ((t + .4f) % 1.3f) < .3f ? .6f : 0; d[i] = (Mathf.Sin(2 * Mathf.PI * 4400 * t) * chirp + Mathf.Sin(2 * Mathf.PI * 3900 * t) * chirp2) * .35f; } }
        else { // Vögel: random short melodic chirps
            for(int s = 0; s < 40; s++) {
                int start = rnd.Next(0, n - rate); float f0 = 2200 + (float)rnd.NextDouble() * 2400, len = .06f + (float)rnd.NextDouble() * .12f; int notes = 1 + rnd.Next(5); float ph = 0;
                for(int k = 0; k < notes; k++) { int a0 = start + (int)(k * (len + .04f) * rate); for(int i = 0; i < len * rate && a0 + i < n; i++) { float t = i / (len * rate); float f = f0 * (1 + .25f * Mathf.Sin(t * Mathf.PI) * (k % 2 == 0 ? 1 : -1)); ph += 2 * Mathf.PI * f / rate; d[a0 + i] += Mathf.Sin(ph) * Mathf.Sin(t * Mathf.PI) * .35f; } }
            }
        }
        int fade = rate / 4; for(int i = 0; i < fade; i++) { float k = i / (float)fade; d[i] = d[i] * k + d[n - fade + i] * (1 - k); }   // seamless loop
        float mx = 0; for(int i = 0; i < n - fade; i++) mx = Mathf.Max(mx, Mathf.Abs(d[i])); if(mx > 0) for(int i = 0; i < n; i++) d[i] = d[i] / mx * .7f;
        c = AudioClip.Create("Nature " + kind, n - fade, 1, rate, false); var trimmed = new float[n - fade]; Array.Copy(d, trimmed, n - fade); c.SetData(trimmed, 0);
        clips[kind] = c; return c;
    }
}

// ====================================================================== Wegpunkt
// Flag with a name. Key N lists all waypoints of this world; choosing one takes you there.
public sealed class FBWaypoint : MonoBehaviour {
    Vector3i cell; TextMesh label; Material cloth; int shown = -1;
    void Start() {
        if(!FunctionBlocks.IsPlaced(transform)) return;
        cell = FunctionBlocks.CellOf(transform);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Base", new Vector3(0, -.42f, 0), new Vector3(.5f, .16f, .5f), FunctionBlocks.Solid(new Color(.45f, .44f, .42f), FunctionBlocks.Surface.Stone));
        FunctionBlocks.Part(transform, PrimitiveType.Cylinder, "Pole", new Vector3(0, .1f, 0), new Vector3(.05f, .55f, .05f), FunctionBlocks.Solid(new Color(.85f, .85f, .85f)));
        cloth = FunctionBlocks.Solid(Color.red);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Flag", new Vector3(.17f, .48f, 0), new Vector3(.32f, .2f, .015f), cloth);
        var lg = new GameObject("Name"); lg.transform.SetParent(transform, false); lg.transform.localPosition = new Vector3(0, .95f, 0);
        label = lg.AddComponent<TextMesh>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = 96; label.characterSize = .12f / 9.6f; label.anchor = TextAnchor.MiddleCenter; label.color = Color.white;
        Shader ts = Resources.Load<Shader>("Portal/SignText"); lg.GetComponent<MeshRenderer>().sharedMaterial = ts != null ? new Material(ts) { mainTexture = label.font.material.mainTexture } : label.font.material;
        FunctionBlockUI.Ensure();
    }
    void Update() {
        if(label == null) return;
        if(shown != FunctionBlockData.Version) { shown = FunctionBlockData.Version; string n = FunctionBlockData.Get(cell, "Wegpunkt"); label.text = n; cloth.color = Color.HSVToRGB(Mathf.Abs(n.GetHashCode() % 1000) / 1000f, .75f, .9f); }
        Camera cam = Camera.main; if(cam != null) label.transform.rotation = Quaternion.LookRotation(label.transform.position - cam.transform.position);
    }
    public static void ShowList() {
        var points = new List<KeyValuePair<Vector3i, string>>();
        foreach(var kv in FunctionBlockData.All()) {
            var fb = Map.instance.GetBlock(kv.Key).block as FunctionBlock;
            if(fb != null && fb.def != null && fb.def.component == typeof(FBWaypoint)) points.Add(kv);
        }
        if(points.Count == 0) { FunctionBlockUI.Toast("Noch keine Wegpunkte in dieser Welt – setze den Block „Wegpunkt“."); return; }
        points.Sort((a, b) => string.Compare(a.Value, b.Value, StringComparison.CurrentCultureIgnoreCase));
        var names = new string[points.Count]; for(int i = 0; i < points.Count; i++) names[i] = points[i].Value;
        FunctionBlockUI.Choose("WEGPUNKTE  •  wohin?", names, i => {
            GameObject pl = FunctionBlocks.Player(); if(pl == null) return;
            Vector3i c = points[i].Key; Vector3 to = new Vector3(c.x + .5f, c.y + 2.05f, c.z + .5f);
            foreach(Vector3i d in new[] { new Vector3i(1, 0, 0), new Vector3i(-1, 0, 0), new Vector3i(0, 0, 1), new Vector3i(0, 0, -1) }) {
                Vector3i q = c + d;
                if(Map.instance.GetBlock(q).IsEmpty() && Map.instance.GetBlock(q + new Vector3i(0, 1, 0)).IsEmpty()) { to = new Vector3(q.x + .5f, q.y + 1.05f, q.z + .5f); break; }
            }
            FBTeleportFade.Go(pl, to);
        });
    }
}

// ====================================================================== Bauwerk-Spawner
// When placed (or on Enter) it lists the saved structures; the chosen one is built right here (the spawner disappears).
public sealed class FBSpawner : MonoBehaviour {
    void Start() {
        if(!FunctionBlocks.IsPlaced(transform)) return;
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Crate", Vector3.zero, new Vector3(.94f, .94f, .94f), FunctionBlocks.Solid(new Color(.2f, .35f, .6f), FunctionBlocks.Surface.Metal));
        Material line = FunctionBlocks.Glow(new Color(.6f, .85f, 1f), 1.1f);
        for(int i = 0; i < 4; i++) { Quaternion r = Quaternion.Euler(0, i * 90, 0); FunctionBlocks.Part(transform, PrimitiveType.Cube, "Line", r * new Vector3(0, 0, .475f), r * new Vector3(.6f, .02f, .01f), line); FunctionBlocks.Part(transform, PrimitiveType.Cube, "Line", r * new Vector3(0, .2f, .475f), r * new Vector3(.4f, .02f, .01f), line); }
    }
    public static void Placed(Vector3i pos, BlockDirection dir) {
        string[] names = VoxelStructureLibrary.List();
        if(names == null || names.Length == 0) { FunctionBlockUI.Toast("Noch keine Bauwerke gespeichert (Pause-Menü → Bauwerke)."); return; }
        FunctionBlockUI.Choose("BAUWERK HIER BAUEN", names, i => {
            string msg; VoxelClipboard clip = VoxelStructureLibrary.Load(names[i], out msg);
            if(clip == null) { FunctionBlockUI.Toast(msg); return; }
            var builder = UnityEngine.Object.FindFirstObjectByType<Builder>(); if(builder == null) return;
            var air = new DataBlock(); Map.instance.SetBlockAndRebuild(pos, air); InfiniteWorldSave.RecordPlayerEdit(pos, air);
            if(builder.PasteStructureAt(clip, pos, names[i])) FunctionBlockUI.Toast("Bauwerk „" + names[i] + "“ wird gebaut …");
        });
    }
}

// ====================================================================== Frost-Brunnen
// A fountain whose jet freezes to ice when it is freezing on the real Block Island.
public sealed class FBFountain : MonoBehaviour {
    ParticleSystem jet; GameObject ice; Material water; bool frozen;
    void Start() {
        if(!FunctionBlocks.IsPlaced(transform)) return;
        BlockIslandRealTime.EnsureUpdating();
        Material stone = FunctionBlocks.Solid(new Color(.6f, .58f, .55f), FunctionBlocks.Surface.Stone);
        FunctionBlocks.Part(transform, PrimitiveType.Cylinder, "Basin", new Vector3(0, -.3f, 0), new Vector3(.96f, .2f, .96f), stone);
        water = FunctionBlocks.Solid(new Color(.25f, .5f, .8f));
        FunctionBlocks.Part(transform, PrimitiveType.Cylinder, "Water", new Vector3(0, -.09f, 0), new Vector3(.82f, .01f, .82f), water);
        FunctionBlocks.Part(transform, PrimitiveType.Cylinder, "Pillar", new Vector3(0, .05f, 0), new Vector3(.14f, .15f, .14f), stone);
        var jg = new GameObject("Jet"); jg.transform.SetParent(transform, false); jg.transform.localPosition = new Vector3(0, .22f, 0); jg.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        jet = jg.AddComponent<ParticleSystem>(); jet.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var m = jet.main; m.startLifetime = 1.1f; m.startSpeed = new ParticleSystem.MinMaxCurve(3.8f, 4.4f); m.startSize = new ParticleSystem.MinMaxCurve(.05f, .12f); m.gravityModifier = .55f; m.maxParticles = 300; m.simulationSpace = ParticleSystemSimulationSpace.World; m.startColor = new Color(.75f, .88f, 1f, .7f);
        var sh = jet.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 7f; sh.radius = .03f;
        var em = jet.emission; em.rateOverTime = 120f;
        var stars = Resources.Load<Material>("Portal/PortalStars"); if(stars != null) jg.GetComponent<ParticleSystemRenderer>().sharedMaterial = stars;
        ice = new GameObject("Ice"); ice.transform.SetParent(transform, false);
        Material iceMat = FunctionBlocks.Solid(new Color(.8f, .92f, 1f));
        for(int i = 0; i < 4; i++) FunctionBlocks.Part(ice.transform, PrimitiveType.Cylinder, "Icicle", new Vector3(0, .3f + i * .15f, 0), new Vector3(.12f - i * .025f, .08f, .12f - i * .025f), iceMat);
        ice.SetActive(false);
        jet.Play();
    }
    void Update() {
        if(jet == null) return;
        bool f = BlockIslandRealTime.HasWeather && BlockIslandRealTime.Temperature <= 0f;
        if(f == frozen) return; frozen = f;
        ice.SetActive(f); water.color = f ? new Color(.82f, .9f, .96f) : new Color(.25f, .5f, .8f);
        if(f) jet.Stop(true, ParticleSystemStopBehavior.StopEmitting); else jet.Play();
    }
}

// ====================================================================== Info-Säule (eigene Bilder)
// Three blocks high, slowly turning; shows the pictures from Documents/Block Island/Bilder one after another.
public sealed class FBPictureColumn : MonoBehaviour {
    static readonly List<Texture2D> pictures = new List<Texture2D>(); static float nextScan; static string signature = "";
    Transform drum; readonly List<Renderer> faces = new List<Renderer>(); Material picMat; int showing = -1; float nextSwap; TextMesh hint;
    void Start() {
        if(!FunctionBlocks.IsPlaced(transform)) return;
        Material dark = FunctionBlocks.Solid(new Color(.2f, .22f, .25f), FunctionBlocks.Surface.Metal);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Foot", new Vector3(0, -.4f, 0), new Vector3(1f, .2f, 1f), dark);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Cap", new Vector3(0, 2.42f, 0), new Vector3(1f, .16f, 1f), dark);
        drum = new GameObject("Drum").transform; drum.SetParent(transform, false); drum.localPosition = new Vector3(0, 1f, 0);
        FunctionBlocks.Part(drum, PrimitiveType.Cube, "Core", Vector3.zero, new Vector3(.8f, 2.6f, .8f), FunctionBlocks.Solid(new Color(.93f, .92f, .9f)));
        picMat = FunctionBlocks.Solid(Color.white);
        for(int i = 0; i < 4; i++) {
            var q = FunctionBlocks.Part(drum, PrimitiveType.Quad, "Picture", Quaternion.Euler(0, i * 90, 0) * new Vector3(0, 0, -.405f), new Vector3(.74f, 1.2f, 1f), picMat, Quaternion.Euler(0, i * 90, 0));
            q.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; faces.Add(q.GetComponent<Renderer>());
        }
        var hg = new GameObject("Hint"); hg.transform.SetParent(drum, false); hg.transform.localPosition = new Vector3(0, 0, -.41f);
        hint = hg.AddComponent<TextMesh>(); hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); hint.fontSize = 96; hint.characterSize = .055f / 9.6f; hint.anchor = TextAnchor.MiddleCenter; hint.alignment = TextAlignment.Center; hint.color = new Color(.2f, .2f, .25f);
        hint.text = "Lege Bilder in\nDokumente/\nBlock Island/\nBilder";
        Shader ts = Resources.Load<Shader>("Portal/SignText"); hg.GetComponent<MeshRenderer>().sharedMaterial = ts != null ? new Material(ts) { mainTexture = hint.font.material.mainTexture } : hint.font.material;
    }
    static void Scan() {
        if(Time.unscaledTime < nextScan) return; nextScan = Time.unscaledTime + 20f;
        string dir = BlockIslandPaths.Folder("Bilder"); var files = new List<string>();
        try { foreach(string f in Directory.GetFiles(dir)) { string e = Path.GetExtension(f).ToLowerInvariant(); if(e == ".png" || e == ".jpg" || e == ".jpeg") files.Add(f); } } catch { }
        files.Sort(StringComparer.OrdinalIgnoreCase);
        string sig = string.Join("|", files.ToArray()); if(sig == signature) return; signature = sig;
        foreach(Texture2D t in pictures) if(t != null) Destroy(t); pictures.Clear();
        foreach(string f in files) { if(pictures.Count >= 40) break; var t = new Texture2D(2, 2, TextureFormat.RGB24, true); try { if(t.LoadImage(File.ReadAllBytes(f))) pictures.Add(t); else Destroy(t); } catch { Destroy(t); } }
    }
    void Update() {
        if(drum == null) return;
        drum.Rotate(0, 7f * Time.deltaTime, 0);
        Scan();
        hint.gameObject.SetActive(pictures.Count == 0);
        foreach(Renderer r in faces) r.enabled = pictures.Count > 0;
        if(pictures.Count == 0 || Time.unscaledTime < nextSwap) return;
        nextSwap = Time.unscaledTime + 8f;
        showing = (showing + 1) % pictures.Count;
        Texture2D p = pictures[showing]; picMat.mainTexture = p;
        float aspect = p.width / (float)Mathf.Max(1, p.height), w = .74f, h = Mathf.Min(2.4f, w / Mathf.Max(.1f, aspect));
        foreach(Renderer r in faces) r.transform.localScale = new Vector3(w, h, 1);
    }
}

// ====================================================================== icons for packages 2-4
static class FBIcons2 {
    delegate Color Painter(float x, float y);
    static Texture2D Paint(string n, Painter p) {
        const int s = 64; var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, name = n };
        for(int y = 0; y < s; y++) for(int x = 0; x < s; x++) t.SetPixel(x, y, p((x + .5f) / s * 2 - 1, (y + .5f) / s * 2 - 1));
        t.Apply(); return t;
    }
    static readonly Color Clear = new Color(0, 0, 0, 0);
    static bool Box(float x, float y, float x0, float y0, float x1, float y1) { return x >= x0 && x <= x1 && y >= y0 && y <= y1; }
    public static Texture2D Panel(Color frame) { return Paint("Panel", (x, y) => Box(x, y, -.9f, -.9f, .9f, .9f) ? (Box(x, y, -.75f, -.75f, .75f, .75f) ? ((Mathf.Abs(y) % .4f) < .1f && Mathf.Abs(x) < .55f ? new Color(.2f, .2f, .25f) : new Color(.95f, .95f, .92f)) : frame) : Clear); }
    public static Texture2D WeatherStation() { return Paint("Weather", (x, y) => Box(x, y, -.4f, -.9f, .4f, -.3f) ? new Color(.92f, .92f, .9f) : Box(x, y, -.05f, -.3f, .05f, .6f) ? new Color(.7f, .7f, .72f) : (new Vector2(x - .45f, y - .65f).magnitude < .14f || new Vector2(x + .45f, y - .65f).magnitude < .14f) ? new Color(.2f, .2f, .22f) : Box(x, y, -.45f, .62f, .45f, .68f) ? new Color(.7f, .7f, .72f) : Clear); }
    public static Texture2D Moon() { return Paint("Moon", (x, y) => { float d = x * x + y * y; if(d > .8f) return Clear; return x > -.2f + Mathf.Sqrt(Mathf.Max(0, .8f - y * y)) * .2f ? new Color(.95f, .93f, .82f) : new Color(.2f, .2f, .28f); }); }
    public static Texture2D Fireworks() { return Paint("Fireworks", (x, y) => { float a = Mathf.Atan2(y - .3f, x), r = new Vector2(x, y - .3f).magnitude; if(r < .6f && Mathf.Abs(Mathf.Repeat(a * 8 / (2 * Mathf.PI), 1) - .5f) < .12f) return Color.HSVToRGB(Mathf.Repeat(a / 6.28f, 1), .8f, 1); if(Box(x, y, -.25f, -.95f, .25f, -.55f)) return new Color(.6f, .15f, .12f); return Clear; }); }
    public static Texture2D Stripes(Color c) { return Paint("Stripes", (x, y) => Box(x, y, -.9f, -.5f, .9f, .5f) ? (Mathf.Repeat((x + y) * 2.5f, 1) < .5f ? c : new Color(.12f, .12f, .12f)) : Clear); }
    public static Texture2D Spring() { return Paint("Spring", (x, y) => Box(x, y, -.8f, .5f, .8f, .65f) ? new Color(.85f, .2f, .15f) : Box(x, y, -.8f, -.9f, .8f, -.75f) ? new Color(.25f, .25f, .28f) : (y > -.75f && y < .5f && Mathf.Abs(x - Mathf.Sin(y * 12) * .4f) < .1f) ? new Color(.75f, .75f, .78f) : Clear); }
    public static Texture2D Door() { return Paint("Door", (x, y) => Box(x, y, -.55f, -.95f, .55f, .95f) ? (Box(x, y, -.45f, -.95f, .45f, .85f) ? (new Vector2(x - .3f, y).magnitude < .07f ? new Color(.9f, .75f, .3f) : new Color(.55f, .36f, .2f)) : new Color(.3f, .2f, .12f)) : Clear); }
    public static Texture2D Station() { return Paint("Station", (x, y) => Box(x, y, -.9f, -.9f, .9f, -.4f) ? new Color(.62f, .6f, .56f) : Box(x, y, -.9f, -.42f, .9f, -.36f) ? new Color(.95f, .85f, .2f) : Box(x, y, .35f, -.36f, .45f, .5f) ? new Color(.15f, .15f, .17f) : Box(x, y, -.1f, .4f, .9f, .75f) ? new Color(.1f, .3f, .6f) : Clear); }
    public static Texture2D Switch() { return Paint("Switch", (x, y) => Box(x, y, -.5f, -.9f, .5f, -.6f) ? new Color(.25f, .25f, .27f) : (y > -.6f && y < .5f && Mathf.Abs(x - (y + .6f) * .4f) < .07f) ? new Color(.7f, .7f, .72f) : new Vector2(x - .45f, y - .55f).magnitude < .17f ? new Color(.95f, .6f, .1f) : Clear); }
    public static Texture2D Jukebox() { return Paint("Jukebox", (x, y) => Box(x, y, -.7f, -.9f, .7f, .6f) || new Vector2(x, y - .6f).magnitude < .7f && y > .6f ? (Box(x, y, -.45f, -.6f, .45f, .2f) ? new Color(1f, .6f, .25f) : new Color(.45f, .25f, .14f)) : Clear); }
    public static Texture2D Nature() { return Paint("Nature", (x, y) => Box(x, y, -.8f, -.9f, .8f, -.1f) ? new Color(.5f, .52f, .48f) : Box(x, y, -.8f, -.1f, .8f, .05f) ? new Color(.25f, .5f, .2f) : (y > .05f && y < .7f && Mathf.Abs(Mathf.Repeat(x * 3, 1) - .5f) < .08f) ? new Color(.35f, .65f, .25f) : Clear); }
    public static Texture2D Flag() { return Paint("Flag", (x, y) => Box(x, y, -.55f, -.9f, -.45f, .9f) ? new Color(.85f, .85f, .85f) : Box(x, y, -.45f, .2f, .65f, .85f) ? new Color(.85f, .15f, .15f) : Box(x, y, -.75f, -.95f, -.25f, -.8f) ? new Color(.45f, .44f, .42f) : Clear); }
    public static Texture2D Spawner() { return Paint("Spawner", (x, y) => Box(x, y, -.85f, -.85f, .85f, .85f) ? ((Mathf.Abs(y - .2f) < .04f && Mathf.Abs(x) < .5f) || (Mathf.Abs(y + .2f) < .04f && Mathf.Abs(x) < .6f) ? new Color(.6f, .85f, 1f) : new Color(.2f, .35f, .6f)) : Clear); }
    public static Texture2D Fountain() { return Paint("Fountain", (x, y) => Box(x, y, -.9f, -.9f, .9f, -.5f) ? new Color(.6f, .58f, .55f) : Box(x, y, -.75f, -.5f, .75f, -.42f) ? new Color(.25f, .5f, .8f) : (y > -.42f && y < .7f && Mathf.Abs(x) < .06f + (y + .42f) * .15f && Mathf.Repeat(y * 6, 1) < .7f) ? new Color(.7f, .85f, 1f) : Clear); }
    public static Texture2D Column() { return Paint("Column", (x, y) => Box(x, y, -.45f, -.95f, .45f, .95f) ? (Box(x, y, -.35f, -.6f, .35f, .6f) ? Color.HSVToRGB(Mathf.Repeat(y + .3f, 1), .5f, .9f) : new Color(.25f, .27f, .3f)) : Clear); }
}
