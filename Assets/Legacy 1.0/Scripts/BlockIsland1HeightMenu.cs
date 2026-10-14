using UnityEngine;

// Key J on a Block Island 1.0 figure/object: menu to set the model height inside its block by hand
// (one block below the floor .. block top). The value applies to every placed copy of that block and is saved
// (BlockIsland1FigureHeights). "Automatisch" returns to the measured floor contact.
public class BlockIsland1HeightMenu : MonoBehaviour {
    const KeyCode Key = KeyCode.J;
    const float Reach = 16f;
    static BlockIsland1HeightMenu instance;

    BlockIsland1Figure target;
    float value;
    bool open;

    public static void Ensure() {
        if(instance == null) instance = new GameObject("BlockIsland1HeightMenu").AddComponent<BlockIsland1HeightMenu>();
    }

    void Update() {
        if(open && (target == null || GameState.IsPause)) { Close(false); return; }
        if(!Input.GetKeyDown(Key)) return;
        if(open) { Close(true); return; }
        if(!GameState.IsPlaying || Cursor.visible) return;   // not while another menu is open
        BlockIsland1Figure f = Pick();
        if(f == null || f.block == null) return;
        target = f;
        float h; value = BlockIsland1FigureHeights.TryGet(f.block, out h) ? h : AutomaticHeight(f);
        open = true;
        Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
    }

    // The figure whose model is under the crosshair.
    static BlockIsland1Figure Pick() {
        Camera cam = Camera.main;
        if(cam == null) return null;
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        BlockIsland1Figure best = null; float bestD = Reach;
        foreach(BlockIsland1Figure f in BlockIsland1Figure.All) {
            if(f == null) continue;
            foreach(Renderer r in f.GetComponentsInChildren<Renderer>()) {
                if(!(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
                float d;
                if(r.bounds.IntersectRay(ray, out d) && d < bestD) { bestD = d; best = f; }
            }
        }
        return best;
    }

    // Where the automatic placement puts the model, expressed on the same -1..+1 scale.
    static float AutomaticHeight(BlockIsland1Figure f) {
        float contact;
        return BlockIsland1Placement.TryContact(f.prefab, out contact) && float.IsNaN(contact) ? .5f : 0f;
    }

    // Clearly visible slider: light track with a gold fill and a large gold handle.
    static GUIStyle sliderStyle, thumbStyle;
    static Texture2D fillTex, trackTex, trackMarkTex;
    static Texture2D Tex(int w, int h, Color fill, Color border) {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false); t.hideFlags = HideFlags.HideAndDontSave; t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Clamp;
        for(int y = 0; y < h; y++) for(int x = 0; x < w; x++)
            t.SetPixel(x, y, (x < 2 || y < 2 || x >= w - 2 || y >= h - 2) ? border : fill);
        t.Apply(); return t;
    }
    static void EnsureSliderStyles() {
        if(sliderStyle != null) return;
        Color gold = new Color(.95f, .72f, .25f), dark = new Color(.18f, .11f, .05f);
        sliderStyle = new GUIStyle();                       // no background: track and fill are drawn by OnGUI
        trackTex = Tex(4, 4, new Color(.82f, .78f, .70f), new Color(.82f, .78f, .70f));   // no border: it would be stretched
        thumbStyle = new GUIStyle();
        thumbStyle.normal.background = Tex(26, 30, gold, dark);
        thumbStyle.hover.background = thumbStyle.active.background = Tex(26, 30, new Color(1f, .85f, .4f), dark);
        thumbStyle.border = new RectOffset(3, 3, 3, 3);
        thumbStyle.fixedWidth = 26; thumbStyle.fixedHeight = 30;
        fillTex = Tex(4, 4, gold, gold);
        trackMarkTex = Tex(2, 2, dark, dark);
    }

    void Close(bool relock) {
        BlockIsland1FigureHeights.Save();
        open = false; target = null;
        if(relock) { Cursor.visible = false; Cursor.lockState = CursorLockMode.Locked; }
    }

    void OnGUI() {
        if(!open || target == null) return;
        VoxelBoxUI.Ensure();
        GUI.depth = -50;
        Rect r = new Rect(Screen.width * .5f - 270f, Screen.height * .5f - 130f, 540f, 260f);
        GUI.Box(r, GUIContent.none, VoxelBoxUI.Panel);
        GUI.Label(new Rect(r.x + 24, r.y + 18, r.width - 48, 30), "MODELL-HÖHE  •  " + target.block.name, VoxelBoxUI.Header);
        GUI.Label(new Rect(r.x + 24, r.y + 52, r.width - 48, 22), "Gilt für alle „" + target.block.name + "“ in der Welt und wird gespeichert.", VoxelBoxUI.Small);

        GUI.Label(new Rect(r.x + 24, r.y + 86, 120, 22), "−1 Block", VoxelBoxUI.Small);
        GUI.Label(new Rect(r.x + r.width * .5f - 50, r.y + 86, 100, 22), "Blockboden", new GUIStyle(VoxelBoxUI.Small) { alignment = TextAnchor.UpperCenter });
        GUI.Label(new Rect(r.x + r.width - 104, r.y + 86, 80, 22), "+1 Block", new GUIStyle(VoxelBoxUI.Small) { alignment = TextAnchor.UpperRight });
        EnsureSliderStyles();
        Rect track = new Rect(r.x + 24, r.y + 106, r.width - 48, 30);
        GUI.DrawTexture(new Rect(track.x, track.y + 9, track.width, 12), trackTex, ScaleMode.StretchToFill);              // light track
        // gold fill from the block floor (centre) to the handle
        float zeroX = track.x + 13 + (track.width - 26) * .5f, handleX = track.x + 13 + (track.width - 26) * (value + 1f) * .5f;
        GUI.DrawTexture(new Rect(Mathf.Min(zeroX, handleX), track.y + 9, Mathf.Abs(handleX - zeroX), 12), fillTex);
        GUI.DrawTexture(new Rect(zeroX - 1, track.y + 4, 2, 22), trackMarkTex);                   // floor mark
        float v = GUI.HorizontalSlider(track, value, -1f, 1f, sliderStyle, thumbStyle);        // invisible track, large handle
        if(!Mathf.Approximately(v, value)) {
            value = v;
            BlockIsland1FigureHeights.Set(target.block, value);
            BlockIsland1Figure.ApplyAll(target.block);
        }
        float h; bool manual = BlockIsland1FigureHeights.TryGet(target.block, out h);
        GUI.Label(new Rect(r.x + 24, r.y + 140, r.width - 48, 24),
            (manual ? "Manuell" : "Automatisch") + "  •  Höhe: " + (value >= 0f ? "+" : "−") + Mathf.Abs(value).ToString("0.00", System.Globalization.CultureInfo.GetCultureInfo("de-DE")) + " Block", VoxelBoxUI.Label);

        if(GUI.Button(new Rect(r.x + 24, r.y + 186, 230, 48), "AUTOMATISCH", VoxelBoxUI.Tab)) {
            BlockIsland1FigureHeights.Clear(target.block);
            BlockIsland1Figure.ApplyAll(target.block);
            value = AutomaticHeight(target);
        }
        if(GUI.Button(new Rect(r.x + r.width - 254, r.y + 186, 230, 48), "FERTIG  (J)", VoxelBoxUI.Button)) Close(true);
    }
}
