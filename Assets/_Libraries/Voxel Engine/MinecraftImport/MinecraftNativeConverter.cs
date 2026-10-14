using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

// Converts a Minecraft Java world (region/*.mca or *.mcr) into Block Island native BIR1 regions,
// the same streaming format as Tanviir / Imperial City. One Minecraft region becomes one BIR region
// (32 x 32 columns of 16 x 256 x 16). Block ids are the legacy Minecraft ids rendered by
// TanviirImportWorld.Map; modern 1.13+ names are translated by MinecraftLegacyIds.
// Pure file/data work: runs on worker threads, one region per thread.
public static class MinecraftNativeConverter {
    public sealed class Status {
        public int regionsTotal, regionsDone;
        public long chunksTotal, chunksDone, blocks, skippedDetails;
        public int yShift, spawnX, spawnY, spawnZ, spawnSurface = -1, dataVersion;
        public volatile bool finished;
        public string error;
        public readonly ConcurrentDictionary<string,long> fallbackNames = new ConcurrentDictionary<string,long>();
    }

    public static void ReadLevel(string worldPath, Status st) {
        st.spawnY = 80;
        try {
            byte[] raw;
            using(var fs = File.OpenRead(Path.Combine(worldPath, "level.dat")))
            using(var gz = new GZipStream(fs, CompressionMode.Decompress))
            using(var ms = new MemoryStream()) { gz.CopyTo(ms); raw = ms.ToArray(); }
            var root = MinecraftNbt.Read(raw); var data = root != null ? (root.Get("Data") ?? root) : null;
            if(data == null) return;
            var t = data.Get("SpawnX"); if(t != null) st.spawnX = t.Int();
            t = data.Get("SpawnY"); if(t != null) st.spawnY = t.Int(80);
            t = data.Get("SpawnZ"); if(t != null) st.spawnZ = t.Int();
            t = data.Get("DataVersion"); if(t != null) st.dataVersion = t.Int();
        } catch(Exception) { }
        // 1.18+ worlds reach from y -64 to 319: shift up by 64 so the usual terrain fits into 0..255.
        st.yShift = st.dataVersion >= 2860 ? 64 : 0;
    }

    // radiusChunks <= 0: whole world.
    public static void Convert(string worldPath, string outRegionDir, int radiusChunks, Status st) {
        try {
            string regionDir = Path.Combine(worldPath, "region");
            var files = new List<string>();
            foreach(string f in Directory.GetFiles(regionDir, "r.*.*.mc?")) { string e = Path.GetExtension(f).ToLowerInvariant(); if(e == ".mca" || e == ".mcr") files.Add(f); }
            int scx = FloorDiv(st.spawnX, 16), scz = FloorDiv(st.spawnZ, 16);
            if(radiusChunks > 0) files.RemoveAll(f => { int rx, rz; return !RegionCoords(f, out rx, out rz) || rx * 32 + 31 < scx - radiusChunks || rx * 32 > scx + radiusChunks || rz * 32 + 31 < scz - radiusChunks || rz * 32 > scz + radiusChunks; });
            st.regionsTotal = files.Count;
            foreach(string f in files) st.chunksTotal += CountChunks(f);
            Directory.CreateDirectory(outRegionDir);
            var opts = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) };
            Parallel.ForEach(files, opts, () => new Worker(), (file, state, w) => {
                w.ConvertRegion(file, outRegionDir, radiusChunks, scx, scz, st);
                Interlocked.Increment(ref st.regionsDone);
                return w;
            }, w => { });
        } catch(Exception e) { st.error = e.GetType().Name + ": " + e.Message; }
        finally { st.finished = true; }
    }

    static bool RegionCoords(string file, out int rx, out int rz) {
        rx = rz = 0; string[] p = Path.GetFileNameWithoutExtension(file).Split('.');
        return p.Length == 3 && int.TryParse(p[1], out rx) && int.TryParse(p[2], out rz);
    }

    static int CountChunks(string file) {
        try { using(var fs = File.OpenRead(file)) { if(fs.Length < 4096) return 0; byte[] h = new byte[4096]; fs.Read(h, 0, 4096); int n = 0; for(int i = 0; i < 1024; i++) if(((h[i*4] << 16) | (h[i*4+1] << 8) | h[i*4+2]) != 0) n++; return n; } }
        catch { return 0; }
    }

    // Per-thread buffers and palette cache.
    sealed class Worker {
        readonly ushort[] ids = new ushort[65536];
        readonly byte[] meta = new byte[65536];
        readonly int[] height = new int[256];
        readonly Dictionary<string,int> cache = new Dictionary<string,int>();   // palette entry -> (id<<4|meta), -1 skip, 0 air
        readonly MemoryStream col = new MemoryStream(1 << 18);

        public void ConvertRegion(string file, string outDir, int radius, int scx, int scz, Status st) {
            int rx, rz; if(!RegionCoords(file, out rx, out rz)) return;
            byte[] region; try { region = File.ReadAllBytes(file); } catch { return; }
            if(region.Length < 8192) return;
            bool mcr = file.EndsWith(".mcr", StringComparison.OrdinalIgnoreCase);
            var blobs = new byte[1024][];
            for(int i = 0; i < 1024; i++) {
                int sector = (region[i*4] << 16) | (region[i*4+1] << 8) | region[i*4+2];
                if(sector == 0) continue;
                int cx = rx * 32 + (i & 31), cz = rz * 32 + (i >> 5);
                if(radius > 0 && (Math.Abs(cx - scx) > radius || Math.Abs(cz - scz) > radius)) continue;
                try {
                    long off = sector * 4096L; if(off + 5 > region.Length) continue;
                    int len = (region[off] << 24) | (region[off+1] << 16) | (region[off+2] << 8) | region[off+3];
                    if(len <= 1 || off + 4 + len > region.Length) continue;
                    byte comp = region[off+4]; byte[] payload = new byte[len - 1]; Buffer.BlockCopy(region, (int)off + 5, payload, 0, len - 1);
                    var root = MinecraftNbt.Read(MinecraftNbt.DecompressChunk(comp, payload));
                    Array.Clear(ids, 0, ids.Length); Array.Clear(meta, 0, meta.Length);
                    long count = mcr ? FillMcRegion(root, st) : FillAnvil(root, st);
                    Interlocked.Add(ref st.blocks, count);
                    if(count > 0) blobs[i] = Encode(cx, cz, st);
                } catch { /* damaged chunk: skipped */ }
                Interlocked.Increment(ref st.chunksDone);
            }
            WriteRegion(Path.Combine(outDir, "r." + rx + "." + rz + ".bir"), blobs);
        }

        long FillAnvil(MinecraftNbt.Tag root, Status st) {
            var level = root.Get("Level") ?? root;
            var sections = level.Get("sections") ?? level.Get("Sections");
            if(sections == null || sections.List == null) return 0;
            long n = 0;
            foreach(var sec in sections.List) {
                var yt = sec.Get("Y"); int sy = yt != null ? (sbyte)yt.Int() : 0;
                int baseY = sy * 16 + st.yShift;
                if(baseY < 0 || baseY > 240) continue;
                var bs = sec.Get("block_states");
                var palette = bs != null ? bs.Get("palette") : sec.Get("Palette");
                var data = bs != null ? bs.Get("data") : sec.Get("BlockStates");
                if(palette != null && palette.List != null && palette.List.Count > 0) {
                    int pc = palette.List.Count; int[] conv = new int[pc];
                    for(int p = 0; p < pc; p++) conv[p] = ConvertEntry(palette.List[p], st);
                    long[] packed = data != null ? data.value as long[] : null;
                    int bits = Math.Max(4, CeilLog2(pc));
                    for(int i = 0; i < 4096; i++) {
                        int pi = packed == null ? 0 : PaletteIndex(packed, i, bits);
                        if(pi < 0 || pi >= pc) continue;
                        int v = conv[pi]; if(v <= 0) { if(v < 0) Interlocked.Increment(ref st.skippedDetails); continue; }
                        int x = i & 15, z = (i >> 4) & 15, y = baseY + ((i >> 8) & 15);
                        int di = (y * 16 + z) * 16 + x; ids[di] = (ushort)(v >> 4); meta[di] = (byte)(v & 15); n++;
                    }
                } else {
                    byte[] blocks = sec.Get("Blocks") != null ? sec.Get("Blocks").value as byte[] : null; if(blocks == null) continue;
                    byte[] md = sec.Get("Data") != null ? sec.Get("Data").value as byte[] : null;
                    byte[] add = sec.Get("Add") != null ? sec.Get("Add").value as byte[] : null;
                    for(int i = 0; i < Math.Min(4096, blocks.Length); i++) {
                        int id = blocks[i]; if(add != null) id |= Nibble(add, i) << 8; if(id == 0) continue;
                        int x = i & 15, z = (i >> 4) & 15, y = baseY + ((i >> 8) & 15);
                        int di = (y * 16 + z) * 16 + x; ids[di] = (ushort)id; meta[di] = (byte)(md != null ? Nibble(md, i) : 0); n++;
                    }
                }
            }
            return n;
        }

        long FillMcRegion(MinecraftNbt.Tag root, Status st) {
            var level = root.Get("Level") ?? root;
            byte[] blocks = level.Get("Blocks") != null ? level.Get("Blocks").value as byte[] : null; if(blocks == null) return 0;
            byte[] md = level.Get("Data") != null ? level.Get("Data").value as byte[] : null; long n = 0;
            for(int x = 0; x < 16; x++) for(int z = 0; z < 16; z++) for(int y = 0; y < 128; y++) {
                int i = (x * 16 + z) * 128 + y; if(i >= blocks.Length || blocks[i] == 0) continue;
                int di = (y * 16 + z) * 16 + x; ids[di] = blocks[i]; meta[di] = (byte)(md != null ? Nibble(md, i) : 0); n++;
            }
            return n;
        }

        int ConvertEntry(MinecraftNbt.Tag entry, Status st) {
            var nt = entry.Get("Name") ?? entry.Get("name"); string name = nt != null ? nt.String() : "minecraft:air";
            var props = entry.Get("Properties") ?? entry.Get("properties");
            Dictionary<string,string> p = null; string key = name;
            if(props != null && props.Compound != null) {
                p = new Dictionary<string,string>();
                foreach(var kv in props.Compound) { p[kv.Key] = kv.Value.String(); }
                string f, h, t; p.TryGetValue("facing", out f); p.TryGetValue("half", out h); p.TryGetValue("type", out t);
                key = name + "|" + f + "|" + h + "|" + t;
            }
            int v; if(cache.TryGetValue(key, out v)) return v;
            int id, m;
            if(!MinecraftLegacyIds.Convert(name, p, out id, out m)) v = 0;
            else if(id == MinecraftLegacyIds.Skip) v = -1;
            else { v = (id << 4) | (m & 15); if(id == 1 && m == 0 && !IsStone(name)) st.fallbackNames.AddOrUpdate(name, 1, (k, c) => c + 1); }
            cache[key] = v; return v;
        }

        static bool IsStone(string n) { return n == "minecraft:stone" || n == "minecraft:smooth_stone" || n == "minecraft:deepslate" || n == "minecraft:infested_stone"; }

        byte[] Encode(int cx, int cz, Status st) {
            int mask = 0;
            for(int s = 0; s < 16; s++) { int b = s * 4096; for(int i = 0; i < 4096; i++) if(ids[b + i] != 0) { mask |= 1 << s; break; } }
            for(int k = 0; k < 256; k++) {
                int h = 0, x = k & 15, z = k >> 4;
                for(int y = 255; y >= 0; y--) if(ids[(y * 16 + z) * 16 + x] != 0) { h = y + 1; break; }
                height[k] = h;
            }
            int sxl = st.spawnX - cx * 16, szl = st.spawnZ - cz * 16;
            if(sxl >= 0 && sxl < 16 && szl >= 0 && szl < 16) st.spawnSurface = height[szl * 16 + sxl];
            col.SetLength(0);
            using(var bw = new BinaryWriter(col, System.Text.Encoding.UTF8, true)) {
                bw.Write((ushort)mask);
                for(int k = 0; k < 256; k++) bw.Write((ushort)height[k]);
                for(int s = 0; s < 16; s++) {
                    if((mask & (1 << s)) == 0) continue;
                    bw.Write((byte)s);
                    int b = s * 4096;
                    for(int i = 0; i < 4096; i++) bw.Write(ids[b + i]);
                    bw.Write(meta, b, 4096);
                }
            }
            using(var outMs = new MemoryStream()) {
                outMs.WriteByte(0x78); outMs.WriteByte(0x9C);                   // zlib header; the BIR reader skips these two bytes
                using(var ds = new DeflateStream(outMs, CompressionLevel.Optimal, true)) ds.Write(col.GetBuffer(), 0, (int)col.Length);
                return outMs.ToArray();
            }
        }
    }

    static void WriteRegion(string path, byte[][] blobs) {
        bool any = false; foreach(var b in blobs) if(b != null) { any = true; break; }
        if(!any) return;
        using(var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using(var bw = new BinaryWriter(fs)) {
            bw.Write((byte)'B'); bw.Write((byte)'I'); bw.Write((byte)'R'); bw.Write((byte)'1');
            long off = 4 + 1024 * 12;
            for(int i = 0; i < 1024; i++) {
                if(blobs[i] == null) { bw.Write(0UL); bw.Write(0U); continue; }
                bw.Write((ulong)off); bw.Write((uint)blobs[i].Length); off += blobs[i].Length;
            }
            for(int i = 0; i < 1024; i++) if(blobs[i] != null) bw.Write(blobs[i]);
        }
    }

    static int Nibble(byte[] a, int index) { int p = index >> 1; if(p >= a.Length) return 0; return (index & 1) == 0 ? (a[p] & 15) : ((a[p] >> 4) & 15); }
    static int CeilLog2(int v) { int b = 0, x = v - 1; while(x > 0) { b++; x >>= 1; } return b; }
    static int FloorDiv(int a, int b) { int q = a / b, r = a % b; return r != 0 && ((r < 0) != (b < 0)) ? q - 1 : q; }
    // 1.16+: values do not span longs (padded); before: tightly packed across long boundaries.
    static int PaletteIndex(long[] a, int index, int bits) {
        ulong mask = (1UL << bits) - 1; int per = 64 / bits; int padded = (4096 + per - 1) / per;
        if(a.Length >= padded) { int li = index / per, off = (index % per) * bits; if(li >= a.Length) return 0; return (int)(((ulong)a[li] >> off) & mask); }
        long bit = (long)index * bits; int word = (int)(bit >> 6), off2 = (int)(bit & 63); if(word >= a.Length) return 0;
        ulong v = (ulong)a[word] >> off2; if(off2 + bits > 64 && word + 1 < a.Length) v |= (ulong)a[word + 1] << (64 - off2);
        return (int)(v & mask);
    }
}
