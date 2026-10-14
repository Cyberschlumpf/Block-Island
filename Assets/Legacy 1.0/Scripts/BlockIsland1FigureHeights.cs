using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// Manually chosen model heights for Block Island 1.0 figure blocks (key J in game, see BlockIsland1HeightMenu).
// Value -1..+1 = height of the model's lowest point relative to its block floor, in blocks
// (0 = block bottom, +1 = block top, -1 = one block lower).
// Stored per 1.0 slot, so one setting applies to every placed copy of that figure.
// Defaults ship with the game (Resources/BlockIsland1/figure_heights.txt, written when edited in the Editor);
// the player's own changes are kept in persistentDataPath and override them.
public static class BlockIsland1FigureHeights {
    const string ResourceName = "BlockIsland1/figure_heights";
    static Dictionary<int, float> heights;
    static readonly HashSet<int> cleared = new HashSet<int>();   // reset to automatic (overrides shipped values)

    static string PlayerFile { get { return Path.Combine(Application.persistentDataPath, "BlockIsland1_figure_heights.txt"); } }
    static string ProjectFile { get { return Path.Combine(Application.dataPath, "Legacy 1.0/Resources/BlockIsland1/figure_heights.txt"); } }

    static void Load() {
        if(heights != null) return;
        heights = new Dictionary<int, float>();
        TextAsset shipped = Resources.Load<TextAsset>(ResourceName);
        if(shipped != null) Parse(shipped.text);
        try { if(File.Exists(PlayerFile)) Parse(File.ReadAllText(PlayerFile)); } catch(System.Exception e) { Debug.LogWarning("Figure heights not readable: " + e.Message); }
    }

    static void Parse(string text) {
        foreach(string raw in text.Split('\n')) {
            string[] c = raw.Trim().Split('\t');
            int slot; float h;
            if(c.Length < 2 || !int.TryParse(c[0], out slot)) continue;
            if(c[1] == "auto") { heights.Remove(slot); cleared.Add(slot); }
            else if(float.TryParse(c[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out h)) heights[slot] = Mathf.Clamp(h, -1f, 1f);
        }
    }

    public static int SlotOf(Block b) { return b == null ? -1 : System.Array.IndexOf(BlockSet.blockIsland1Slots, b); }

    public static bool TryGet(Block b, out float h) { Load(); h = 0f; int s = SlotOf(b); return s >= 0 && heights.TryGetValue(s, out h); }

    public static void Set(Block b, float h) { Load(); int s = SlotOf(b); if(s >= 0) { heights[s] = Mathf.Clamp(h, -1f, 1f); cleared.Remove(s); } }

    public static void Clear(Block b) { Load(); int s = SlotOf(b); if(s >= 0) { heights.Remove(s); cleared.Add(s); } }

    public static void Save() {
        Load();
        StringBuilder sb = new StringBuilder("# Block Island 1.0 figure heights: slot<TAB>-1..+1 blocks<TAB>name\n");
        Block[] slots = BlockSet.blockIsland1Slots;
        var keys = new List<int>(heights.Keys); keys.Sort();
        foreach(int s in keys)
            sb.Append(s).Append('\t').Append(heights[s].ToString("0.###", System.Globalization.CultureInfo.InvariantCulture))
              .Append('\t').Append(s < slots.Length && slots[s] != null ? slots[s].name : "").Append('\n');
        string project = sb.ToString();
        foreach(int s in cleared) sb.Append(s).Append("\tauto\n");
        try { File.WriteAllText(PlayerFile, sb.ToString()); } catch(System.Exception e) { Debug.LogWarning("Figure heights not saved: " + e.Message); }
        // In the Editor the values also go into the project, so every build ships with them.
        if(Application.isEditor) { try { File.WriteAllText(ProjectFile, project); } catch(System.Exception e) { Debug.LogWarning("Figure heights not written to project: " + e.Message); } }
    }
}
