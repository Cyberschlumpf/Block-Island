using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

// Builds the Tanviir texture-pack atlas (Resources/BlockSet/Atlases/TanviirPack) used by TanviirPackBlocks.
// Batch mode:
//   -executeMethod TanviirPackImport.BuildAtlasBatch -tvpack <pack>/assets/minecraft/textures/blocks
public static class TanviirPackImport {
    public const string OutDir = "Assets/Resources/BlockSet/Atlases/TanviirPack";
    const int Tile = 64, Gutter = 16, Cell = Tile + 2 * Gutter, AtlasSize = 2048, Cols = AtlasSize / Cell;

    static string Arg(string name) {
        string[] a = System.Environment.GetCommandLineArgs();
        for(int i = 0; i < a.Length - 1; i++) if(a[i] == name) return a[i + 1];
        return null;
    }
    public static void BuildAtlasBatch() { BuildAtlas(Arg("-tvpack")); }

    // ---------------------------------------------------------------- atlas
    static readonly string[] DyeNames = { "white", "orange", "magenta", "light_blue", "yellow", "lime", "pink", "gray", "silver", "cyan", "purple", "blue", "brown", "green", "red", "black" };
    static readonly int[] ClayRgb = { 0xD1B2A1, 0xA15325, 0x95586C, 0x716C89, 0xBA8523, 0x677534, 0xA14E4E, 0x392A23, 0x876B62, 0x565B5B, 0x764656, 0x4A3B5B, 0x4D3323, 0x4C532A, 0x8F3D2E, 0x251610 };
    static readonly int[] DyeRgb = { 0xFFFFFF, 0xD87F33, 0xB24CD8, 0x6699D8, 0xE5E533, 0x7FCC19, 0xF27FA5, 0x4C4C4C, 0x999999, 0x4C7F99, 0x7F3FB2, 0x334CB2, 0x664C33, 0x667F33, 0x993333, 0x191919 };
    static Color Rgb(int c) { return new Color(((c >> 16) & 255) / 255f, ((c >> 8) & 255) / 255f, (c & 255) / 255f, 1f); }

    // Grayscale textures that Minecraft colours with the biome tint.
    static readonly Dictionary<string, int> Tints = new Dictionary<string, int> {
        { "grass_top", 0x91BD59 }, { "grass_side_overlay", 0x91BD59 }, { "tallgrass", 0x91BD59 }, { "fern", 0x91BD59 },
        { "leaves_oak", 0x77AB2F }, { "leaves_oak_opaque", 0x77AB2F }, { "leaves_jungle", 0x55C93F }, { "leaves_jungle_opaque", 0x55C93F },
        { "leaves_birch", 0x80A755 }, { "leaves_birch_opaque", 0x80A755 }, { "leaves_spruce", 0x619961 }, { "leaves_spruce_opaque", 0x619961 },
        { "vine", 0x77AB2F }, { "waterlily", 0x208030 },
        { "melon_stem_connected", 0x8FB040 }, { "melon_stem_disconnected", 0x8FB040 }, { "pumpkin_stem_connected", 0x8FB040 }, { "pumpkin_stem_disconnected", 0x8FB040 },
        { "redstone_dust_cross", 0xD01808 }, { "redstone_dust_line", 0xD01808 },
    };

    public static void BuildAtlas(string packDir) {
        if(string.IsNullOrEmpty(packDir) || !Directory.Exists(packDir)) { Debug.LogError("TANVIIR PACK: texture folder missing: " + packDir); return; }
        var tex = new SortedDictionary<string, Color[]>(System.StringComparer.Ordinal);
        foreach(string f in Directory.GetFiles(packDir, "*.png")) {
            string n = Path.GetFileNameWithoutExtension(f);
            if(n.StartsWith("destroy_stage_") || n.StartsWith("itemframe")) continue;
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if(!t.LoadImage(File.ReadAllBytes(f))) { Debug.LogWarning("TANVIIR PACK: unreadable texture " + n); continue; }
            tex[n] = Resample(t);
            Object.DestroyImmediate(t);
        }
        // Biome tint for grayscale foliage.
        foreach(var kv in Tints) {
            Color[] px; if(!tex.TryGetValue(kv.Key, out px)) continue;
            if(MeanSaturation(px) < .15f) tex[kv.Key] = Multiply(px, Rgb(kv.Value));
        }
        // Grass side = dirt side + tinted grass overlay.
        Color[] gs, go;
        if(tex.TryGetValue("grass_side", out gs) && tex.TryGetValue("grass_side_overlay", out go)) {
            var r = new Color[gs.Length];
            for(int i = 0; i < r.Length; i++) { Color o = go[i]; r[i] = Color.Lerp(gs[i], new Color(o.r, o.g, o.b, 1f), o.a); r[i].a = 1f; }
            tex["grass_side_tinted"] = r;
        }
        // Blocks newer than this pack (stained/hardened clay, stained glass, coal block) are recoloured pack textures.
        Color[] clay, glass, stone;
        if(tex.TryGetValue("clay", out clay)) {
            tex["hardened_clay"] = Recolor(clay, Rgb(0x965D43));
            for(int i = 0; i < 16; i++) tex["hardened_clay_stained_" + DyeNames[i]] = Recolor(clay, Rgb(ClayRgb[i]));
        }
        if(tex.TryGetValue("glass", out glass))
            for(int i = 0; i < 16; i++) tex["glass_" + DyeNames[i]] = StainGlass(glass, Rgb(DyeRgb[i]));
        if(tex.TryGetValue("stone", out stone)) tex["coal_block"] = Recolor(stone, new Color(.11f, .11f, .12f, 1f));

        if(tex.Count > Cols * Cols) { Debug.LogError("TANVIIR PACK: too many textures for the atlas: " + tex.Count); return; }
        var atlas = new Texture2D(AtlasSize, AtlasSize, TextureFormat.RGBA32, false);
        var clear = new Color[AtlasSize * AtlasSize]; atlas.SetPixels(clear);
        var index = new System.Text.StringBuilder();
        index.AppendLine("# name u v w h alpha  (inner tile rect, normalized; alpha=1 when the tile has cut-out pixels)");
        int k = 0;
        foreach(var kv in tex) {
            int col = k % Cols, row = k / Cols; k++;
            int x0 = col * Cell, y0 = AtlasSize - (row + 1) * Cell;
            Color[] px = kv.Value; var cell = new Color[Cell * Cell];
            for(int y = 0; y < Cell; y++) for(int x = 0; x < Cell; x++) {
                int sx = Mathf.Clamp(x - Gutter, 0, Tile - 1), sy = Mathf.Clamp(y - Gutter, 0, Tile - 1);
                cell[y * Cell + x] = px[sy * Tile + sx];
            }
            atlas.SetPixels(x0, y0, Cell, Cell, cell);
            float s = AtlasSize;
            index.AppendLine(kv.Key + " " + ((x0 + Gutter) / s).ToString("R") + " " + ((y0 + Gutter) / s).ToString("R") + " " + (Tile / s).ToString("R") + " " + (Tile / s).ToString("R") + " " + (HasAlpha(px) ? "1" : "0"));
        }
        atlas.Apply(false);
        Directory.CreateDirectory(OutDir);
        string png = OutDir + "/Tanviir Pack Atlas.png", txt = OutDir + "/Tanviir Pack Atlas Index.txt";
        File.WriteAllBytes(png, atlas.EncodeToPNG());
        File.WriteAllText(txt, index.ToString());
        Object.DestroyImmediate(atlas);
        AssetDatabase.ImportAsset(png, ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(txt, ImportAssetOptions.ForceSynchronousImport);
        var imp = (TextureImporter)AssetImporter.GetAtPath(png);
        imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.alphaIsTransparency = true;
        imp.mipmapEnabled = true; imp.mipMapsPreserveCoverage = true; imp.alphaTestReferenceValue = .5f;
        imp.filterMode = FilterMode.Trilinear; imp.anisoLevel = 8; imp.wrapMode = TextureWrapMode.Clamp;
        imp.textureCompression = TextureImporterCompression.Uncompressed; imp.maxTextureSize = AtlasSize; imp.isReadable = false;
        imp.SaveAndReimport();
        Debug.Log("TANVIIR PACK: atlas built with " + tex.Count + " textures -> " + png);
    }

    // First animation frame (top square), scaled to Tile x Tile (box filter down, nearest up).
    static Color[] Resample(Texture2D t) {
        int w = t.width, h = Mathf.Min(t.height, t.width);
        Color[] src = t.GetPixels(0, t.height - h, w, h);
        var dst = new Color[Tile * Tile];
        if(w >= Tile) {
            int f = w / Tile; float inv = 1f / (f * f);
            for(int y = 0; y < Tile; y++) for(int x = 0; x < Tile; x++) {
                Color c = Color.clear; float a = 0f;
                for(int yy = 0; yy < f; yy++) for(int xx = 0; xx < f; xx++) { Color p = src[(y * f + yy) * w + x * f + xx]; c += new Color(p.r * p.a, p.g * p.a, p.b * p.a, p.a); a += p.a; }
                dst[y * Tile + x] = a > 0f ? new Color(c.r / a, c.g / a, c.b / a, c.a * inv) : Color.clear;
            }
        } else {
            for(int y = 0; y < Tile; y++) for(int x = 0; x < Tile; x++) dst[y * Tile + x] = src[(y * h / Tile) * w + x * w / Tile];
        }
        return dst;
    }
    static bool HasAlpha(Color[] px) { foreach(Color c in px) if(c.a < .5f) return true; return false; }
    static float MeanSaturation(Color[] px) {
        double s = 0; int n = 0;
        foreach(Color c in px) { if(c.a < .5f) continue; float mx = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), mn = Mathf.Min(c.r, Mathf.Min(c.g, c.b)); s += mx > 0f ? (mx - mn) / mx : 0f; n++; }
        return n > 0 ? (float)(s / n) : 0f;
    }
    static Color[] Multiply(Color[] px, Color tint) {
        var r = new Color[px.Length];
        for(int i = 0; i < px.Length; i++) r[i] = new Color(px[i].r * tint.r, px[i].g * tint.g, px[i].b * tint.b, px[i].a);
        return r;
    }
    static float Lum(Color c) { return c.r * .299f + c.g * .587f + c.b * .114f; }
    // Keep the texture's light/dark structure, replace its mean colour.
    static Color[] Recolor(Color[] px, Color target) {
        double m = 0; foreach(Color c in px) m += Lum(c); float mean = Mathf.Max(.01f, (float)(m / px.Length));
        var r = new Color[px.Length];
        for(int i = 0; i < px.Length; i++) { float f = Lum(px[i]) / mean; r[i] = new Color(Mathf.Clamp01(target.r * f), Mathf.Clamp01(target.g * f), Mathf.Clamp01(target.b * f), 1f); }
        return r;
    }
    // Coloured frame, clear centre: stays readable with the cutout shader.
    static Color[] StainGlass(Color[] px, Color dye) {
        var r = new Color[px.Length];
        for(int i = 0; i < px.Length; i++) { Color c = px[i]; float l = Lum(c); r[i] = c.a < .5f ? Color.clear : new Color(Mathf.Clamp01(dye.r * (.45f + l)), Mathf.Clamp01(dye.g * (.45f + l)), Mathf.Clamp01(dye.b * (.45f + l)), 1f); }
        return r;
    }
}
