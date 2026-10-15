using UnityEngine;
using System.Collections.Generic;

// Key P in game: prefab importer. Lists the prefabs exported from Unity (~/Block Island/Prefabs/*.biprefab),
// shows a rotatable 3D preview on a block grid, and saves a prefab as a buildable block with name, size,
// animation and height (PrefabBlockLibrary). Saved blocks can be changed again here; every placed copy follows.
public class PrefabImporterGUI : MonoBehaviour {
    const KeyCode Key = KeyCode.P;
    const int PreviewLayer = 30;
    static readonly Vector3 Stage = new Vector3(0f, -5000f, 0f);

    bool open;
    string[] files = new string[0];
    int selected = -1;
    GameObject prefab;
    List<AnimationClip> clips = new List<AnimationClip>();
    PrefabBlockLibrary.Entry edit;          // settings being edited (a copy until saved)
    PrefabBlockLibrary.Entry registered;    // the saved block for this file, if any
    string status = "";
    Vector2 listScroll, animScroll;

    // preview
    Camera cam; Light key, fill; RenderTexture rt; GameObject stageRoot, model, grid;
    float yaw = 35f, pitch = 18f, zoom = 1f; bool dragging;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() { var go = new GameObject("PrefabImporterGUI"); DontDestroyOnLoad(go); go.AddComponent<PrefabImporterGUI>(); }

    void Update() {
        bool inGame = Map.instance != null && Map.instance.infiniteWorld != null;
        if(open && (!inGame || GameState.IsPause || Input.GetKeyDown(KeyCode.Escape))) { Close(); return; }
        if(!inGame || !Input.GetKeyDown(Key)) { if(open) UpdatePreview(); return; }
        if(open) { Close(); return; }
        if(!GameState.IsPlaying || Cursor.visible) return;
        Open();
    }

    void Open() {
        open = true; status = "";
        Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
        Refresh();
    }

    void Close() {
        open = false; DestroyPreview();
        if(GameState.IsPlaying) { Cursor.visible = false; Cursor.lockState = CursorLockMode.Locked; }
    }

    void Refresh() {
        files = PrefabBlockLibrary.ListBundles();
        if(files.Length == 0) { Select(-1); return; }
        Select(Mathf.Clamp(selected, 0, files.Length - 1));
    }

    void Select(int i) {
        selected = i; prefab = null; clips.Clear(); registered = null; edit = null;
        DestroyModel();
        if(i < 0 || i >= files.Length) return;
        prefab = PrefabBlockLibrary.LoadPrefab(files[i]);
        if(prefab == null) { status = "Datei kann nicht geladen werden (mit anderer Unity-Version exportiert?)."; return; }
        clips = PrefabBlockLibrary.Clips(prefab);
        foreach(var e in PrefabBlockLibrary.Entries) if(!e.removed && e.bundle == files[i]) registered = e;
        edit = Copy(registered) ?? new PrefabBlockLibrary.Entry { name = prefab.name, bundle = files[i] };
        CreateModel();
    }

    static PrefabBlockLibrary.Entry Copy(PrefabBlockLibrary.Entry e) {
        if(e == null) return null;
        return new PrefabBlockLibrary.Entry { slot = e.slot, name = e.name, bundle = e.bundle, scale = e.scale, animation = e.animation, speed = e.speed, loop = e.loop, height = e.height, removed = e.removed };
    }
    static void CopyInto(PrefabBlockLibrary.Entry from, PrefabBlockLibrary.Entry to) {
        to.name = from.name; to.scale = from.scale; to.animation = from.animation; to.speed = from.speed; to.loop = from.loop; to.height = from.height;
    }

    // ------------------------------------------------------------------ preview
    void EnsureStage() {
        if(stageRoot != null) return;
        stageRoot = new GameObject("Prefab Importer Stage"); stageRoot.transform.position = Stage;
        var c = new GameObject("Prefab Importer Camera"); c.transform.SetParent(stageRoot.transform, false);
        cam = c.AddComponent<Camera>(); cam.enabled = false; cam.cullingMask = 1 << PreviewLayer;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.13f, .15f, .19f, 1f); cam.nearClipPlane = .05f; cam.farClipPlane = 2000f;
        key = NewLight("Key", 1.1f, Quaternion.Euler(50f, -30f, 0f)); fill = NewLight("Fill", .45f, Quaternion.Euler(20f, 150f, 0f));
        rt = new RenderTexture(768, 640, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 }; rt.Create();
        cam.targetTexture = rt;
    }
    Light NewLight(string n, float intensity, Quaternion rot) {
        var g = new GameObject("Prefab Importer " + n); g.transform.SetParent(stageRoot.transform, false); g.transform.rotation = rot;
        var l = g.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = intensity; l.cullingMask = 1 << PreviewLayer; l.shadows = LightShadows.Soft;
        return l;
    }

    void CreateModel() {
        EnsureStage();
        model = Instantiate(prefab, Stage + new Vector3(0f, .5f, 0f), Quaternion.identity);
        model.name = "Prefab Importer Model";
        foreach(Camera c in model.GetComponentsInChildren<Camera>(true)) c.enabled = false;
        foreach(AudioListener l in model.GetComponentsInChildren<AudioListener>(true)) l.enabled = false;
        foreach(Rigidbody r in model.GetComponentsInChildren<Rigidbody>(true)) r.isKinematic = true;
        SetLayer(model.transform);
        model.AddComponent<PrefabBlockInstance>().Init(edit, Stage.y, true);
        RebuildGrid();
    }

    static void SetLayer(Transform t) { t.gameObject.layer = PreviewLayer; foreach(Transform c in t) SetLayer(c); }

    // Size of the model in blocks (renderer bounds of the posed model).
    Vector3 ModelSize() {
        if(model == null) return Vector3.zero;
        bool any = false; Bounds b = new Bounds();
        foreach(Renderer r in model.GetComponentsInChildren<Renderer>()) {
            if(!(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
            if(!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        return any ? b.size : Vector3.zero;
    }

    // Floor of 1 x 1 block tiles under the model as size reference.
    void RebuildGrid() {
        if(grid != null) Destroy(grid);
        Vector3 s = ModelSize();
        int n = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(s.x, s.z)) + 4, 6, 200);
        grid = new GameObject("Prefab Importer Grid", typeof(MeshFilter), typeof(MeshRenderer));
        grid.layer = PreviewLayer; grid.transform.position = Stage;
        float h = n * .5f;
        var m = new Mesh();
        m.vertices = new[] { new Vector3(-h, 0, -h), new Vector3(-h, 0, h), new Vector3(h, 0, h), new Vector3(h, 0, -h) };
        m.uv = new[] { new Vector2(0, 0), new Vector2(0, n), new Vector2(n, n), new Vector2(n, 0) };
        m.triangles = new[] { 0, 1, 2, 0, 2, 3 }; m.RecalculateNormals();
        grid.GetComponent<MeshFilter>().sharedMesh = m;
        var mat = new Material(Shader.Find("Sprites/Default")) { mainTexture = GridTexture() };
        grid.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }
    static Texture2D gridTex;
    static Texture2D GridTexture() {
        if(gridTex != null) return gridTex;
        gridTex = new Texture2D(32, 32, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 8 };
        for(int y = 0; y < 32; y++) for(int x = 0; x < 32; x++)
            gridTex.SetPixel(x, y, (x == 0 || y == 0) ? new Color(.95f, .72f, .25f, 1f) : new Color(.30f, .33f, .38f, 1f));
        gridTex.Apply();
        return gridTex;
    }

    void UpdatePreview() {
        if(model == null || cam == null) return;
        if(!dragging) yaw += Time.unscaledDeltaTime * 12f;
        Vector3 s = ModelSize();
        float radius = Mathf.Max(1.2f, s.magnitude * .6f);
        Vector3 target = Stage + new Vector3(0f, Mathf.Max(.5f, s.y * .45f), 0f);
        Quaternion q = Quaternion.Euler(pitch, yaw, 0f);
        cam.transform.position = target + q * new Vector3(0f, 0f, -radius * 2.4f * zoom);
        cam.transform.LookAt(target);
        cam.Render();
    }

    void DestroyModel() { if(model != null) Destroy(model); model = null; if(grid != null) Destroy(grid); grid = null; }
    void DestroyPreview() {
        DestroyModel();
        if(rt != null) { rt.Release(); Destroy(rt); rt = null; }
        if(stageRoot != null) Destroy(stageRoot); stageRoot = null; cam = null;
    }

    Texture2D RenderIcon() {
        if(cam == null || model == null) return null;
        var small = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32); small.Create();
        float y = yaw; yaw = 35f; UpdatePreviewTo(small); yaw = y;
        var prev = RenderTexture.active; RenderTexture.active = small;
        var t = new Texture2D(128, 128, TextureFormat.RGBA32, false); t.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); t.Apply();
        RenderTexture.active = prev; small.Release(); Destroy(small);
        return t;
    }
    void UpdatePreviewTo(RenderTexture target) {
        var old = cam.targetTexture; cam.targetTexture = target; Color bg = cam.backgroundColor; cam.backgroundColor = new Color(0, 0, 0, 0);
        if(grid != null) grid.SetActive(false);
        UpdatePreview();
        if(grid != null) grid.SetActive(true);
        cam.targetTexture = old; cam.backgroundColor = bg;
    }

    // ------------------------------------------------------------------ GUI
    void OnGUI() {
        if(!open) return;
        VoxelBoxUI.BeginResponsive();
        VoxelBoxUI.Backdrop(.6f);
        Rect r = VoxelBoxUI.Safe(1500, 860, 30);
        GUILayout.BeginArea(r, VoxelBoxUI.Panel);
        GUILayout.BeginHorizontal();
        GUILayout.Label("PREFAB-IMPORTER", VoxelBoxUI.Header);
        GUILayout.FlexibleSpace();
        if(GUILayout.Button("✕  SCHLIESSEN (P)", VoxelBoxUI.Button, GUILayout.Width(220), GUILayout.Height(36))) Close();
        GUILayout.EndHorizontal();
        GUILayout.Label("Prefab in Unity im Ordner „Block Island Prefabs“ speichern – es erscheint automatisch hier.  •  Vorschau mit der Maus drehen, Mausrad = Zoom.", VoxelBoxUI.Small);
        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        DrawList();
        DrawPreviewArea(r);
        DrawSettings();
        GUILayout.EndHorizontal();
        if(!string.IsNullOrEmpty(status)) GUILayout.Label(status, VoxelBoxUI.Label);
        GUILayout.EndArea();
        VoxelBoxUI.EndResponsive();
    }

    void DrawList() {
        GUILayout.BeginVertical(VoxelBoxUI.Card, GUILayout.Width(300));
        VoxelBoxUI.SectionTitle("PREFABS");
        listScroll = GUILayout.BeginScrollView(listScroll, GUILayout.Height(560));
        if(files.Length == 0) GUILayout.Label("Noch keine Prefabs.\nIn Unity ein Prefab im Ordner „Assets/Block Island Prefabs“ speichern.", VoxelBoxUI.Small);
        for(int i = 0; i < files.Length; i++) {
            bool inSet = false; foreach(var e in PrefabBlockLibrary.Entries) if(!e.removed && e.bundle == files[i]) inSet = true;
            string label = (inSet ? "✓ " : "   ") + System.IO.Path.GetFileNameWithoutExtension(files[i]);
            if(GUILayout.Button(label, i == selected ? VoxelBoxUI.TabActive : VoxelBoxUI.Tab, GUILayout.Height(32))) { Select(i); status = ""; }
        }
        GUILayout.EndScrollView();
        if(GUILayout.Button("↻  AKTUALISIEREN", VoxelBoxUI.Button, GUILayout.Height(34))) Refresh();
        if(GUILayout.Button("▤  ORDNER ÖFFNEN", VoxelBoxUI.Button, GUILayout.Height(34))) Application.OpenURL("file://" + PrefabBlockLibrary.Folder);
        GUILayout.EndVertical();
    }

    void DrawPreviewArea(Rect panel) {
        GUILayout.BeginVertical(VoxelBoxUI.Card, GUILayout.Width(700));
        Rect pr = GUILayoutUtility.GetRect(680, 580, GUILayout.Width(680), GUILayout.Height(580));
        if(rt != null && model != null) {
            GUI.DrawTexture(pr, rt, ScaleMode.ScaleToFit, false);
            Event ev = Event.current;
            if(ev.type == EventType.MouseDown && pr.Contains(ev.mousePosition)) { dragging = true; ev.Use(); }
            if(ev.type == EventType.MouseUp) dragging = false;
            if(ev.type == EventType.MouseDrag && dragging) { yaw += ev.delta.x * .4f; pitch = Mathf.Clamp(pitch + ev.delta.y * .3f, -10f, 80f); ev.Use(); }
            if(ev.type == EventType.ScrollWheel && pr.Contains(ev.mousePosition)) { zoom = Mathf.Clamp(zoom * (ev.delta.y > 0 ? 1.1f : .9f), .3f, 4f); ev.Use(); }
            Vector3 s = ModelSize();
            GUILayout.Label("Größe in der Welt:  " + s.x.ToString("0.0") + " × " + s.y.ToString("0.0") + " × " + s.z.ToString("0.0") + " Blöcke  (Breite × Höhe × Tiefe; ein Gitterfeld = 1 Block)", VoxelBoxUI.Small);
        } else GUILayout.Label(prefab == null ? "Kein Prefab gewählt." : "", VoxelBoxUI.Label);
        GUILayout.EndVertical();
    }

    void DrawSettings() {
        GUILayout.BeginVertical(VoxelBoxUI.Card, GUILayout.Width(420));
        if(edit == null) { GUILayout.Label("Links ein Prefab wählen.", VoxelBoxUI.Label); GUILayout.EndVertical(); return; }
        bool changed = false;
        VoxelBoxUI.SectionTitle("NAME IM BLOCKSET");
        edit.name = GUILayout.TextField(edit.name, 48, GUILayout.Width(396), GUILayout.Height(30));

        VoxelBoxUI.SectionTitle("GRÖSSE");
        float lg = Mathf.Log(Mathf.Max(.01f, edit.scale), 2f);
        float nlg = VoxelBoxUI.Slider(lg, -6f, 5f);
        if(Mathf.Abs(nlg - lg) > .0001f) { edit.scale = Mathf.Pow(2f, nlg); changed = true; }
        GUILayout.Label("Faktor " + edit.scale.ToString("0.###") + "  •  Höhe " + ModelSize().y.ToString("0.0") + " Blöcke", VoxelBoxUI.Small);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Höhe:", VoxelBoxUI.Small, GUILayout.Width(46));
        foreach(float target in new[] { .5f, 1f, 2f, 4f, 8f }) {
            if(GUILayout.Button(target.ToString("0.#"), VoxelBoxUI.Button, GUILayout.Width(64), GUILayout.Height(28))) { float h = ModelSize().y; if(h > .001f) { edit.scale *= target / h; changed = true; } }
        }
        GUILayout.EndHorizontal();

        VoxelBoxUI.SectionTitle("ANIMATION");
        animScroll = GUILayout.BeginScrollView(animScroll, GUILayout.Width(400), GUILayout.Height(150));
        if(AnimButton("Standard (wie im Prefab)", PrefabBlockLibrary.StandardAnimation)) changed = true;
        if(AnimButton("Keine", PrefabBlockLibrary.NoAnimation)) changed = true;
        foreach(AnimationClip c in clips) if(AnimButton(c.name + "  (" + c.length.ToString("0.0") + " s)", c.name)) changed = true;
        if(clips.Count == 0) GUILayout.Label("Dieses Prefab enthält keine Animationen.", VoxelBoxUI.Small);
        GUILayout.EndScrollView();
        GUILayout.Label("Tempo " + edit.speed.ToString("0.00") + "×", VoxelBoxUI.Small);
        float sp = VoxelBoxUI.Slider(edit.speed, 0f, 3f);
        if(Mathf.Abs(sp - edit.speed) > .0001f) { edit.speed = sp; changed = true; }
        bool lp = VoxelBoxUI.Toggle(edit.loop, "Endlos wiederholen");
        if(lp != edit.loop) { edit.loop = lp; changed = true; }

        VoxelBoxUI.SectionTitle("HÖHE IM BLOCK");
        float hh = VoxelBoxUI.Slider(edit.height, -1f, 1f);
        if(Mathf.Abs(hh - edit.height) > .0001f) { edit.height = hh; changed = true; }
        GUILayout.Label((edit.height >= 0 ? "+" : "") + edit.height.ToString("0.00") + " Blöcke über dem Boden", VoxelBoxUI.Small);

        if(changed && model != null) { model.GetComponent<PrefabBlockInstance>().Apply(); }
        GUILayout.Space(8);
        if(registered == null) {
            if(GUILayout.Button("▣  IM BLOCKSET SPEICHERN", VoxelBoxUI.Button, GUILayout.Height(42))) SaveNew();
        } else {
            if(GUILayout.Button("▣  ÄNDERUNGEN SPEICHERN", VoxelBoxUI.Button, GUILayout.Height(40))) SaveChanges();
            if(GUILayout.Button("▶  ZUM BAUEN WÄHLEN", VoxelBoxUI.Button, GUILayout.Height(36))) SelectForBuilding(registered);
            if(GUILayout.Button("＋  ALS WEITEREN BLOCK SPEICHERN", VoxelBoxUI.Button, GUILayout.Height(34))) SaveNew();
            if(GUILayout.Button("✕  AUS DEM BLOCKSET NEHMEN", VoxelBoxUI.Danger, GUILayout.Height(34))) { PrefabBlockLibrary.Remove(registered); status = "„" + registered.name + "“ ist nicht mehr im Blockset (gebaute Exemplare werden nicht mehr angezeigt)."; Select(selected); }
        }
        GUILayout.EndVertical();
    }

    bool AnimButton(string label, string value) {
        bool on = edit.animation == value;
        if(GUILayout.Button((on ? "● " : "○ ") + label, on ? VoxelBoxUI.TabActive : VoxelBoxUI.Tab, GUILayout.Width(372), GUILayout.Height(28)) && !on) { edit.animation = value; return true; }
        return false;
    }

    void SaveNew() {
        string n = (edit.name ?? "").Trim();
        if(n.Length == 0) { status = "Bitte einen Namen eingeben."; return; }
        if(PrefabBlockLibrary.NameTaken(n, null)) { status = "Der Name „" + n + "“ ist im Blockset schon vergeben."; return; }
        var e = Copy(edit); e.name = n; e.bundle = files[selected];
        Block b = PrefabBlockLibrary.Register(e, RenderIcon());
        registered = e; edit = Copy(e);
        model.GetComponent<PrefabBlockInstance>().entry = edit;
        status = b != null ? "„" + n + "“ ist jetzt im Blockset (Kategorie FIGUREN) und zum Bauen ausgewählt." : "Gespeichert.";
        SelectForBuilding(e);
    }

    void SaveChanges() {
        string n = (edit.name ?? "").Trim();
        if(n.Length == 0) { status = "Bitte einen Namen eingeben."; return; }
        if(PrefabBlockLibrary.NameTaken(n, registered)) { status = "Der Name „" + n + "“ ist im Blockset schon vergeben."; return; }
        edit.name = n;
        CopyInto(edit, registered);
        PrefabBlockLibrary.Update(registered, RenderIcon());
        status = "Gespeichert – alle gebauten „" + n + "“ übernehmen die Einstellungen.";
    }

    void SelectForBuilding(PrefabBlockLibrary.Entry e) {
        Builder b = FindAnyObjectByType<Builder>();
        Block block = BlockSet.instance != null ? BlockSet.instance[PrefabBlockLibrary.FirstIndex + e.slot] : null;
        if(b != null && block != null) b.SetSelectedBlock(block);
    }
}
