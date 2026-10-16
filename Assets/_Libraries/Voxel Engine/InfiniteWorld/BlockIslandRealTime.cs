using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

// Real time and real weather for the town clock:
// - the clock shows Central European time (Europe/Berlin, with summer time),
// - the info board below it shows the time on the real Block Island (Rhode Island, USA, America/New_York) and the
//   current weather there from Open-Meteo (free, no account or key; only the island's coordinates are sent).
public static class BlockIslandRealTime {
    public const float Latitude = 41.1729f, Longitude = -71.5578f;   // Block Island, Rhode Island

    public static DateTime CentralEurope { get { return Convert(DateTime.UtcNow, "Europe/Berlin", EuOffset); } }
    public static DateTime BlockIsland { get { return Convert(DateTime.UtcNow, "America/New_York", UsEastOffset); } }
    public static DateTime Tokyo { get { return Convert(DateTime.UtcNow, "Asia/Tokyo", u => 9); } }
    public static DateTime Sydney { get { return Convert(DateTime.UtcNow, "Australia/Sydney", u => (u.Month >= 10 || u.Month <= 3) ? 11 : 10); } }

    static DateTime Convert(DateTime utc, string zone, Func<DateTime, int> fallback) {
        try { return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById(zone)); }
        catch { return utc.AddHours(fallback(utc)); }
    }
    // Summer time rules, used only if the system has no time zone data.
    static int EuOffset(DateTime utc) {
        DateTime start = LastSunday(utc.Year, 3).AddHours(1), end = LastSunday(utc.Year, 10).AddHours(1);
        return utc >= start && utc < end ? 2 : 1;
    }
    static int UsEastOffset(DateTime utc) {
        DateTime start = NthSunday(utc.Year, 3, 2).AddHours(7), end = NthSunday(utc.Year, 11, 1).AddHours(6);
        return utc >= start && utc < end ? -4 : -5;
    }
    static DateTime LastSunday(int y, int m) { var d = new DateTime(y, m, DateTime.DaysInMonth(y, m)); while(d.DayOfWeek != DayOfWeek.Sunday) d = d.AddDays(-1); return d; }
    static DateTime NthSunday(int y, int m, int n) { var d = new DateTime(y, m, 1); while(d.DayOfWeek != DayOfWeek.Sunday) d = d.AddDays(1); return d.AddDays(7 * (n - 1)); }

    // ------------------------------------------------------------------ weather
    public static bool HasWeather { get; private set; }
    public static bool Failed { get; private set; }
    public static float Temperature { get; private set; }
    public static float Wind { get; private set; }
    public static int Code { get; private set; }
    public static float WindDirection { get; private set; }   // degrees, where the wind comes from (0 = north)
    public static bool IsDay { get; private set; }
    public static DateTime FetchedAt { get; private set; }
    const float RefreshSeconds = 15 * 60f, RetrySeconds = 60f;

    [Serializable] class Current { public float temperature_2m; public int weather_code; public float wind_speed_10m; public float wind_direction_10m; public int is_day; }
    [Serializable] class Reply { public Current current; }

    static Fetcher fetcher;
    public static void EnsureUpdating() {
        if(fetcher != null) return;
        var go = new GameObject("Block Island Weather (Internet)"); UnityEngine.Object.DontDestroyOnLoad(go); go.hideFlags = HideFlags.HideInHierarchy;
        fetcher = go.AddComponent<Fetcher>();
    }
    sealed class Fetcher : MonoBehaviour {
        IEnumerator Start() {
            while(true) {
                string url = "https://api.open-meteo.com/v1/forecast?latitude=" + Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + "&longitude=" + Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + "&current=temperature_2m,weather_code,wind_speed_10m,wind_direction_10m,is_day&timezone=America%2FNew_York";
                bool ok = false;
                using(UnityWebRequest r = UnityWebRequest.Get(url)) {
                    r.timeout = 15;
                    yield return r.SendWebRequest();
                    if(r.result == UnityWebRequest.Result.Success) {
                        try {
                            Reply rep = JsonUtility.FromJson<Reply>(r.downloadHandler.text);
                            if(rep != null && rep.current != null) {
                                Temperature = rep.current.temperature_2m; Wind = rep.current.wind_speed_10m; Code = rep.current.weather_code; IsDay = rep.current.is_day != 0; WindDirection = rep.current.wind_direction_10m;
                                HasWeather = true; Failed = false; FetchedAt = DateTime.Now; ok = true;
                            }
                        } catch(Exception e) { Debug.LogWarning("BLOCK ISLAND WEATHER: reply not readable: " + e.Message); }
                    }
                    if(!ok) { Failed = true; Debug.Log("BLOCK ISLAND WEATHER: no connection (" + r.error + ")"); }
                    else Debug.Log("BLOCK ISLAND WEATHER: " + Temperature + " °C, code " + Code + ", wind " + Wind + " km/h");
                }
                yield return new WaitForSecondsRealtime(ok ? RefreshSeconds : RetrySeconds);
            }
        }
    }

    // WMO weather codes in German
    public static string Describe(int code) {
        switch(code) {
            case 0: return IsDay ? "Sonnig" : "Klar";
            case 1: return "Überwiegend klar";
            case 2: return "Teilweise bewölkt";
            case 3: return "Bedeckt";
            case 45: case 48: return "Nebel";
            case 51: case 53: case 55: return "Nieselregen";
            case 56: case 57: return "Gefrierender Niesel";
            case 61: return "Leichter Regen";
            case 63: return "Regen";
            case 65: return "Starker Regen";
            case 66: case 67: return "Gefrierender Regen";
            case 71: return "Leichter Schneefall";
            case 73: return "Schneefall";
            case 75: return "Starker Schneefall";
            case 77: return "Schneegriesel";
            case 80: case 81: return "Regenschauer";
            case 82: return "Heftige Schauer";
            case 85: case 86: return "Schneeschauer";
            case 95: return "Gewitter";
            case 96: case 99: return "Gewitter mit Hagel";
            default: return "Wetter " + code;
        }
    }

    static readonly string[] Days = { "Sonntag", "Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag", "Samstag" };
    static readonly string[] Months = { "Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember" };
    public static string GermanDate(DateTime d) { return Days[(int)d.DayOfWeek] + ", " + d.Day + ". " + Months[d.Month - 1]; }
}

// Buildable info sign "Infotafel Block Island" (fixed block IDs 4002 + 4003, category FUNKTION): two real blocks,
// 2 wide and 1 high, solid and buildable like any block. Placing it also sets the second half to the right (as seen
// by the player); removing either half removes both. Front and back: row 1 the time on the real Block Island,
// row 2 the weather and temperature there in German.
public class InfoSignBlock : GameObjectBlock {
    public bool secondHalf;
    static bool busy;

    // a real block: the player collides with it and can build on and against it
    public override bool IsSolid() { return true; }

    // The player looks against the block's direction; "right" for the player is the sign's second half.
    public static Vector3i PartnerOffset(BlockDirection dir, bool fromSecondHalf) {
        Vector3 d = dir == BlockDirection.LEFT ? Vector3.left : dir == BlockDirection.RIGHT ? Vector3.right : dir == BlockDirection.BACKWARD ? Vector3.back : Vector3.forward;
        Vector3 right = Quaternion.LookRotation(-d) * Vector3.right;
        if(fromSecondHalf) right = -right;
        return new Vector3i(Mathf.RoundToInt(right.x), 0, Mathf.RoundToInt(right.z));
    }

    public override void OnBlockCreate(Vector3i pos, BlockDirection dir) {
        base.OnBlockCreate(pos, dir);
        if(secondHalf || busy || Map.instance == null || BlockSet.instance == null) return;
        Vector3i p = pos + PartnerOffset(dir, false);
        if(!Map.instance.GetBlock(p).IsEmpty()) return;          // no room: the sign stays one block wide
        busy = true;
        try {
            var half = new DataBlock(BlockSet.instance[InfoSignBlocks.SecondHalfId], dir);
            Map.instance.SetBlockAndRebuild(p, half);
            InfiniteWorldSave.RecordPlayerEdit(p, half);
        } finally { busy = false; }
    }

    public override void OnBlockDestroy(Vector3i pos) {
        base.OnBlockDestroy(pos);
        if(busy || Map.instance == null) return;
        DataBlock me = Map.instance.GetBlock(pos);
        Vector3i p = pos + PartnerOffset(me.direction, secondHalf);
        DataBlock other = Map.instance.GetBlock(p);
        var otherSign = other.block as InfoSignBlock;
        if(otherSign == null || otherSign.secondHalf == secondHalf || other.direction != me.direction) return;
        busy = true;
        try { var air = new DataBlock(); Map.instance.SetBlockAndRebuild(p, air); InfiniteWorldSave.RecordPlayerEdit(p, air); }
        finally { busy = false; }
    }
}

public static class InfoSignBlocks {
    public const int Id = 4002, SecondHalfId = 4003;
    public const string Name = "Infotafel Block Island";
    static GameObject templateRoot, template, emptyTemplate;

    // Called by BlockSet.Awake right after the portals (4000/4001).
    public static void Ensure(BlockSet set) {
        List<Block> blocks = set.GetBlocks();
        if(blocks.Count != Id) { Debug.LogError("INFO SIGN: BlockSet has " + blocks.Count + " blocks, expected " + Id); return; }
        if(templateRoot == null) { templateRoot = new GameObject("Info Sign Template"); templateRoot.SetActive(false); if(Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(templateRoot); }
        if(template == null) { template = new GameObject(Name); template.transform.SetParent(templateRoot.transform, false); template.AddComponent<BlockIslandInfoSign>(); }
        if(emptyTemplate == null) { emptyTemplate = new GameObject(Name + " (2)"); emptyTemplate.transform.SetParent(templateRoot.transform, false); }
        InfoSignBlock b = ScriptableObject.CreateInstance<InfoSignBlock>();
        b.name = Name; b.hideFlags = HideFlags.DontSave; b.gameObject = template; b.icon = Icon();
        blocks.Add(b);
        // second half: solid and buildable, drawn by the first half; not in the inventory
        InfoSignBlock h = ScriptableObject.CreateInstance<InfoSignBlock>();
        h.name = Name + " (2. Hälfte)"; h.hideFlags = HideFlags.DontSave; h.secondHalf = true; h.gameObject = emptyTemplate; h.retired = true;
        blocks.Add(h);
    }

    static Texture2D Icon() {
        const int w = 64, h = 64;
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, name = "Info Sign Icon" };
        Color frame = new Color(.16f, .15f, .14f), face = new Color(.95f, .93f, .88f), ink = new Color(.15f, .15f, .15f), blue = new Color(.2f, .35f, .6f);
        for(int y = 0; y < h; y++) for(int x = 0; x < w; x++) {
            Color c = Color.clear;
            bool inBoard = x >= 2 && x < 62 && y >= 17 && y < 47, inFace = x >= 5 && x < 59 && y >= 20 && y < 44;
            if(inBoard) c = frame; if(inFace) c = face;
            if(inFace && y >= 33 && y < 39 && x >= 11 && x < 53 && (x / 3) % 3 != 2) c = ink;
            if(inFace && y >= 24 && y < 28 && x >= 17 && x < 47 && (x / 2) % 4 != 3) c = blue;
            t.SetPixel(x, y, c);
        }
        t.Apply(); return t;
    }
}

public sealed class BlockIslandInfoSign : MonoBehaviour {
    readonly List<TextMesh> times = new List<TextMesh>(), weathers = new List<TextMesh>();
    float nextRefresh;
    static Font font; static Material textMat;
    const float MaxTextWidth = 1.8f;

    void Start() {
        if(transform.parent == null) return;                 // template
        BlockIslandRealTime.EnsureUpdating();
        if(font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if(textMat == null) { Shader ts = Resources.Load<Shader>("Portal/SignText"); textMat = ts != null ? new Material(ts) { mainTexture = font.material.mainTexture } : font.material; }
        // The instance sits in the middle of its block, local +z toward the player; the second half is on the
        // player's right, which is the instance's local -x. Together they fill 2 x 1 x 1 blocks.
        var root = new GameObject("Sign"); root.transform.SetParent(transform, false); root.transform.localPosition = new Vector3(-.5f, 0, 0);
        Shader diffuse = Shader.Find("Legacy Shaders/Diffuse");
        Box(root.transform, "Body", Vector3.zero, new Vector3(2f, 1f, 1f), new Color(.16f, .15f, .14f), diffuse, true);
        for(int side = 0; side < 2; side++) {
            float z = (side == 0 ? -1 : 1) * .505f;
            Box(root.transform, "Face " + side, new Vector3(0, 0, z), new Vector3(1.88f, .86f, .004f), new Color(.98f, .96f, .91f), diffuse, false);
            Box(root.transform, "Line " + side, new Vector3(0, -.02f, z * 1.004f), new Vector3(1.6f, .01f, .002f), new Color(.35f, .33f, .3f), diffuse, false);
            Quaternion r = side == 0 ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
            times.Add(Text(root.transform, new Vector3(0, .19f, z * 1.01f), r, .26f, new Color(.1f, .1f, .1f)));
            weathers.Add(Text(root.transform, new Vector3(0, -.21f, z * 1.01f), r, .19f, new Color(.12f, .22f, .38f)));
        }
        Refresh();
    }

    void Box(Transform parent, string n, Vector3 pos, Vector3 size, Color c, Shader sh, bool shadows) {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(g.GetComponent<Collider>());
        g.name = n; g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localScale = size;
        var r = g.GetComponent<Renderer>(); r.sharedMaterial = new Material(sh) { color = c };
        r.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = shadows;
    }
    // TextMesh reads from its -z side; 'size' is the line height in blocks
    TextMesh Text(Transform parent, Vector3 pos, Quaternion rot, float size, Color c) {
        var g = new GameObject("Text"); g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localRotation = rot;
        var t = g.AddComponent<TextMesh>();
        t.font = font; t.fontSize = 96; t.characterSize = size / (96 * .1f); t.anchor = TextAnchor.MiddleCenter; t.alignment = TextAlignment.Center; t.color = c;
        var mr = g.GetComponent<MeshRenderer>(); mr.sharedMaterial = textMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
        g.AddComponent<SignTextFit>().Init(t.characterSize, MaxTextWidth);
        return t;
    }

    void Update() { if(times.Count > 0 && Time.unscaledTime >= nextRefresh) Refresh(); }
    void Refresh() {
        nextRefresh = Time.unscaledTime + .5f;
        string time = "Block Island  " + BlockIslandRealTime.BlockIsland.ToString("HH:mm") + " Uhr";
        string weather = BlockIslandRealTime.HasWeather
            ? BlockIslandRealTime.Describe(BlockIslandRealTime.Code) + "  ·  " + Mathf.RoundToInt(BlockIslandRealTime.Temperature) + " °C"
            : BlockIslandRealTime.Failed ? "Wetter: keine Internetverbindung" : "Wetter wird geladen …";
        foreach(TextMesh t in times) t.text = time;
        foreach(TextMesh t in weathers) t.text = weather;
    }
}

// Shrinks a sign text when it would be wider than the sign (long weather descriptions).
public sealed class SignTextFit : MonoBehaviour {
    float baseSize, maxWidth; string fitted; TextMesh t; Renderer r;
    public void Init(float size, float width) { baseSize = size; maxWidth = width; t = GetComponent<TextMesh>(); r = GetComponent<Renderer>(); }
    void LateUpdate() {
        if(t == null || t.text == fitted) return;
        fitted = t.text; t.characterSize = baseSize;
        float w = transform.InverseTransformVector(r.bounds.size).x;   // width along the text, in sign units
        w = Mathf.Abs(w) * transform.lossyScale.x;
        if(w > maxWidth) t.characterSize = baseSize * maxWidth / w;
    }
}
