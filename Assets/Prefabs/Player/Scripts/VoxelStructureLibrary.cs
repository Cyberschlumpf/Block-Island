using UnityEngine;
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

// Saved structures ("Bauwerke"): a Copy & Paste clipboard written to ~/Block Island/Bauwerke/<name>.bibau,
// so a building can be pasted into any other world later.
// File: int magic, int version, string name, int sizeX/Y/Z, int palette count, (int blockID, string blockName)*,
//       int run count, deflate[(int length, int blockID, byte direction)*]
// Block names are stored with their IDs: if the blockset ever changes, blocks are found again by name.
public static class VoxelStructureLibrary {
    const int Magic = 0x57414942; // "BIAW"
    const int Version = 1;
    public const string Extension = ".bibau";

    public static string Directory_ {
        get {
            string d = Path.Combine(Path.GetDirectoryName(InfiniteWorldSave.SaveDirectory), "Bauwerke");
            Directory.CreateDirectory(d);
            return d;
        }
    }
    public static string PathFor(string name) { return Path.Combine(Directory_, InfiniteWorldSave.SanitizeName(name) + Extension); }

    public static string[] List() {
        string[] f = Directory.GetFiles(Directory_, "*" + Extension);
        var names = new string[f.Length];
        for(int i = 0; i < f.Length; i++) names[i] = Path.GetFileNameWithoutExtension(f[i]);
        Array.Sort(names, StringComparer.OrdinalIgnoreCase);
        return names;
    }

    public static bool Save(VoxelClipboard clip, string name, out string message) {
        message = "";
        if(clip == null || clip.solidCount == 0) { message = "Nichts kopiert: erst mit COPY START / COPY END etwas kopieren."; return false; }
        name = InfiniteWorldSave.SanitizeName(name);
        string path = PathFor(name), tmp = path + ".tmp";
        try {
            var palette = new SortedDictionary<int, string>();
            for(int r = 0; r < clip.RunCount; r++) {
                int len; DataBlock b; clip.GetRun(r, out len, out b);
                if(b.blockID != 0 && !palette.ContainsKey(b.blockID)) palette[b.blockID] = b.block != null ? b.block.name : "";
            }
            using(var fs = File.Create(tmp)) using(var bw = new BinaryWriter(fs)) {
                bw.Write(Magic); bw.Write(Version); bw.Write(name);
                bw.Write(clip.sizeX); bw.Write(clip.sizeY); bw.Write(clip.sizeZ);
                bw.Write(palette.Count); foreach(var kv in palette) { bw.Write(kv.Key); bw.Write(kv.Value); }
                bw.Write(clip.RunCount); bw.Flush();
                using(var ds = new DeflateStream(fs, System.IO.Compression.CompressionLevel.Optimal, true)) using(var rw = new BinaryWriter(ds)) {
                    for(int r = 0; r < clip.RunCount; r++) { int len; DataBlock b; clip.GetRun(r, out len, out b); rw.Write(len); rw.Write(b.blockID); rw.Write((byte)b.direction); }
                }
            }
            if(File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
            message = "Bauwerk gespeichert: " + name + "  (" + clip.sizeX + " x " + clip.sizeY + " x " + clip.sizeZ + ", " + clip.solidCount.ToString("N0") + " Blöcke)";
            return true;
        } catch(Exception e) {
            try { if(File.Exists(tmp)) File.Delete(tmp); } catch { }
            message = "Speichern fehlgeschlagen: " + e.Message; Debug.LogError("STRUCTURE save failed: " + e);
            return false;
        }
    }

    public static VoxelClipboard Load(string name, out string message) {
        message = "";
        string path = PathFor(name);
        try {
            using(var fs = File.OpenRead(path)) using(var br = new BinaryReader(fs)) {
                if(br.ReadInt32() != Magic) throw new IOException("keine Bauwerk-Datei");
                int version = br.ReadInt32(); if(version > Version) throw new IOException("Datei aus einer neueren Version");
                br.ReadString();
                int sx = br.ReadInt32(), sy = br.ReadInt32(), sz = br.ReadInt32();
                if(sx < 1 || sy < 1 || sz < 1 || sx > VoxelClipboard.MaxSize || sy > VoxelClipboard.MaxSize || sz > VoxelClipboard.MaxSize) throw new IOException("ungültige Größe");
                var remap = new Dictionary<int, int>(); int missing = 0;
                int pc = br.ReadInt32();
                for(int i = 0; i < pc; i++) { int id = br.ReadInt32(); string bn = br.ReadString(); int now = Resolve(id, bn); if(now == 0) missing++; remap[id] = now; }
                int runs = br.ReadInt32();
                var clip = new VoxelClipboard(sx, sy, sz);
                long total = 0;
                using(var ds = new DeflateStream(fs, CompressionMode.Decompress, true)) using(var rr = new BinaryReader(ds)) {
                    for(int r = 0; r < runs; r++) {
                        int len = rr.ReadInt32(), id = rr.ReadInt32(); byte dir = rr.ReadByte();
                        int now = 0; if(id != 0) remap.TryGetValue(id, out now);
                        clip.AddRun(len, now, dir); total += len;
                    }
                }
                if(total != clip.Volume) throw new IOException("Datei unvollständig");
                message = "Bauwerk geladen: " + name + "  (" + sx + " x " + sy + " x " + sz + ", " + clip.solidCount.ToString("N0") + " Blöcke)" + (missing > 0 ? "  –  " + missing + " Blocktyp(en) nicht mehr vorhanden" : "") + ". PASTE SELECTION wählen.";
                return clip;
            }
        } catch(Exception e) {
            message = "Laden fehlgeschlagen: " + e.Message; Debug.LogError("STRUCTURE load failed: " + e);
            return null;
        }
    }

    // Same ID and name -> keep; otherwise look the block up by name; unknown -> left out (air).
    static int Resolve(int id, string blockName) {
        BlockSet set = BlockSet.instance; if(set == null) return id;
        Block b = set[id - 1];
        if(b != null && b.name == blockName) return id;
        Block byName = string.IsNullOrEmpty(blockName) ? null : set.FindBlock(blockName);
        return byName != null ? byName.blockID + 1 : 0;
    }

    public static void OpenFolder() {
        try { Application.OpenURL("file://" + Directory_.Replace("\\", "/")); } catch(Exception e) { Debug.LogWarning("Could not open structure folder: " + e.Message); }
    }
}
