using UnityEngine;
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

// Permanent storage of the generated Block Island surroundings (everything outside the 1.0 area).
// The first time a 32x32x32 chunk (or a column's height map) is generated it is written to
// "<save>.infinite.terrain/r.<rx>.<rz>.bit"; afterwards it is always read from there. Later changes
// to the island generator therefore never move the land under the player's buildings.
// Player edits stay in the normal save file and are overlaid on top of this terrain.
//
// Region file: 16 x 16 chunk columns (512 x 512 blocks), append-only records:
//   byte type (1 = chunk, 2 = column heights), int cx, int cy (chunks only), int cz, int length, deflate payload
//   chunk payload:  run-length list (int count, int blockID, byte direction) over the 32^3 voxels (x fastest, then y, then z)
//   heights payload: 32 x 32 int surface heights (x fastest)
public static class BlockIslandTerrainStore {
    const int RegionColumns = 16;
    const int N = Chunk.X_SIZE * Chunk.Y_SIZE * Chunk.Z_SIZE;
    const int MaxCachedChunks = 384, MaxCachedHeights = 2048;
    const int Magic = 0x31544942; // "BIT1"

    sealed class ChunkData { public int[] ids; public byte[] dirs; public bool empty; public int stamp; }
    sealed class Region { public string path; public readonly Dictionary<long, long> chunks = new Dictionary<long, long>(); public readonly Dictionary<long, long> heights = new Dictionary<long, long>(); }

    static readonly object gate = new object();
    static string root;
    static readonly Dictionary<long, Region> regions = new Dictionary<long, Region>();
    static readonly Dictionary<long, ChunkData> chunkCache = new Dictionary<long, ChunkData>();
    static readonly Dictionary<long, int[]> heightCache = new Dictionary<long, int[]>();
    static int stamp;
    static long hotKey = long.MinValue; static ChunkData hot;

    public static int ChunksWritten { get; private set; }
    public static int ChunksRead { get; private set; }

    // Drop everything held in memory; the next access reads the region files again.
    public static void ForgetCache() { lock(gate) { root = null; rootWorldName = null; regions.Clear(); chunkCache.Clear(); heightCache.Clear(); hotKey = long.MinValue; hot = null; } }

    static string CurrentRoot { get { return InfiniteWorldSave.PathFor(InfiniteWorldSave.CurrentWorldName) + ".terrain"; } }
    public static string DirectoryFor(string worldName) { return InfiniteWorldSave.PathFor(worldName) + ".terrain"; }

    // Follow the current save: switching worlds (or loading another save) starts a fresh index.
    static string rootWorldName;
    static void EnsureRoot() {
        // Resolving the save path touches the disk; only do it when the world changes.
        string world = InfiniteWorldSave.CurrentWorldName;
        if(root != null && (object)world == (object)rootWorldName) return;
        rootWorldName = world;
        string r = CurrentRoot;
        if(r == root) return;
        root = r; regions.Clear(); chunkCache.Clear(); heightCache.Clear(); hotKey = long.MinValue; hot = null; ChunksWritten = ChunksRead = 0;
    }

    static int FD(int a, int b) { int q = a / b, r = a % b; return r != 0 && ((r < 0) != (b < 0)) ? q - 1 : q; }
    static long K2(int x, int z) { return ((long)x << 32) ^ (uint)z; }
    static long K3(int x, int y, int z) { unchecked { return ((long)x << 40) ^ ((long)(y & 0xff) << 32) ^ (uint)z; } }

    // ------------------------------------------------------------------ public API
    public static DataBlock Sample(InfiniteTerrainGenerator g, int x, int y, int z) {
        int cy = FD(y, Chunk.Y_SIZE);
        if(cy < 0 || cy >= 256 / Chunk.Y_SIZE) return default(DataBlock);
        int cx = FD(x, Chunk.X_SIZE), cz = FD(z, Chunk.Z_SIZE);
        ChunkData c;
        lock(gate) { c = GetChunk(g, cx, cy, cz); }
        if(c == null || c.empty) return default(DataBlock);
        int i = ((z - cz * Chunk.Z_SIZE) * Chunk.Y_SIZE + (y - cy * Chunk.Y_SIZE)) * Chunk.X_SIZE + (x - cx * Chunk.X_SIZE);
        DataBlock b = default(DataBlock); b.blockID = c.ids[i]; b.direction = (BlockDirection)c.dirs[i]; return b;
    }

    public static void FillChunk(InfiniteTerrainGenerator g, int ox, int oy, int oz, DataBlock[] dst) {
        int cx = FD(ox, Chunk.X_SIZE), cy = FD(oy, Chunk.Y_SIZE), cz = FD(oz, Chunk.Z_SIZE);
        ChunkData c = null;
        if(cy >= 0 && cy < 256 / Chunk.Y_SIZE) lock(gate) { c = GetChunk(g, cx, cy, cz); }
        if(c == null || c.empty) { Array.Clear(dst, 0, N); return; }
        for(int i = 0; i < N; i++) { dst[i].blockID = c.ids[i]; dst[i].direction = (BlockDirection)c.dirs[i]; }
    }

    public static int SurfaceY(InfiniteTerrainGenerator g, int x, int z) {
        int cx = FD(x, Chunk.X_SIZE), cz = FD(z, Chunk.Z_SIZE);
        int[] h;
        lock(gate) { h = GetHeights(g, cx, cz); }
        return h[(z - cz * Chunk.Z_SIZE) * Chunk.X_SIZE + (x - cx * Chunk.X_SIZE)];
    }

    // ------------------------------------------------------------------ chunks
    static ChunkData GetChunk(InfiniteTerrainGenerator g, int cx, int cy, int cz) {
        EnsureRoot();
        long key = K3(cx, cy, cz);
        if(key == hotKey && hot != null) return hot;
        ChunkData c;
        if(!chunkCache.TryGetValue(key, out c)) {
            Region reg = RegionFor(cx, cz);
            long off;
            if(reg.chunks.TryGetValue(key, out off)) { c = ReadChunk(reg, off); if(c != null) ChunksRead++; }
            if(c == null) { c = Generate(g, cx, cy, cz); reg.chunks[key] = Append(reg, 1, cx, cy, cz, EncodeChunk(c)); ChunksWritten++; }
            if(chunkCache.Count >= MaxCachedChunks) Trim();
            chunkCache[key] = c;
        }
        c.stamp = ++stamp; hotKey = key; hot = c;
        return c;
    }

    // Generated terrain only exists between the sea floor / island bottom and the tallest tree
    // above the surface; everything outside that band is air and is not sampled at all.
    const int AboveSurface = 64, BelowSurface = 9;
    static ChunkData Generate(InfiniteTerrainGenerator g, int cx, int cy, int cz) {
        var c = new ChunkData { ids = new int[N], dirs = new byte[N], empty = true };
        if(g == null) return c;
        int ox = cx * Chunk.X_SIZE, oy = cy * Chunk.Y_SIZE, oz = cz * Chunk.Z_SIZE;
        var lo = new int[Chunk.X_SIZE * Chunk.Z_SIZE]; var hi = new int[lo.Length];
        int minLo = int.MaxValue, maxHi = int.MinValue;
        int[] h = GetHeights(g, cx, cz);
        for(int i = 0; i < lo.Length; i++) {
            lo[i] = Mathf.Min(h[i], g.seaLevel) - BelowSurface; hi[i] = Mathf.Max(h[i], g.seaLevel) + AboveSurface;
            if(lo[i] < minLo) minLo = lo[i]; if(hi[i] > maxHi) maxHi = hi[i];
        }
        if(oy > maxHi || oy + Chunk.Y_SIZE - 1 < minLo) return c;
        for(int z = 0; z < Chunk.Z_SIZE; z++) for(int x = 0; x < Chunk.X_SIZE; x++) {
            int col = z * Chunk.X_SIZE + x;
            int y0 = Mathf.Max(0, lo[col] - oy), y1 = Mathf.Min(Chunk.Y_SIZE - 1, hi[col] - oy);
            for(int y = y0; y <= y1; y++) {
                DataBlock b = g.SampleProcedural(ox + x, oy + y, oz + z);
                if(b.blockID == 0) continue;
                int i = (z * Chunk.Y_SIZE + y) * Chunk.X_SIZE + x;
                c.ids[i] = b.blockID; c.dirs[i] = (byte)b.direction; c.empty = false;
            }
        }
        return c;
    }

    static void Trim() {
        // Drop the older half of the cache; regenerating from disk is cheap.
        var all = new List<KeyValuePair<long, ChunkData>>(chunkCache);
        all.Sort((a, b) => a.Value.stamp.CompareTo(b.Value.stamp));
        for(int i = 0; i < all.Count / 2; i++) chunkCache.Remove(all[i].Key);
        hotKey = long.MinValue; hot = null;
    }

    static byte[] EncodeChunk(ChunkData c) {
        using(var ms = new MemoryStream()) {
            using(var ds = new DeflateStream(ms, System.IO.Compression.CompressionLevel.Fastest, true))
            using(var bw = new BinaryWriter(ds)) {
                if(c.empty) { bw.Write(N); bw.Write(0); bw.Write((byte)0); }
                else {
                    int i = 0;
                    while(i < N) {
                        int id = c.ids[i]; byte d = c.dirs[i]; int n = 1;
                        while(i + n < N && c.ids[i + n] == id && c.dirs[i + n] == d) n++;
                        bw.Write(n); bw.Write(id); bw.Write(d); i += n;
                    }
                }
            }
            return ms.ToArray();
        }
    }

    static ChunkData DecodeChunk(byte[] payload) {
        var c = new ChunkData { ids = new int[N], dirs = new byte[N], empty = true };
        using(var ms = new MemoryStream(payload)) using(var ds = new DeflateStream(ms, CompressionMode.Decompress)) using(var br = new BinaryReader(ds)) {
            int i = 0;
            while(i < N) {
                int n = br.ReadInt32(), id = br.ReadInt32(); byte d = br.ReadByte();
                if(n <= 0 || i + n > N) throw new IOException("bad run");
                if(id != 0) { c.empty = false; for(int k = 0; k < n; k++) { c.ids[i + k] = id; c.dirs[i + k] = d; } }
                i += n;
            }
        }
        return c;
    }

    // ------------------------------------------------------------------ heights
    static int[] GetHeights(InfiniteTerrainGenerator g, int cx, int cz) {
        EnsureRoot();
        long key = K2(cx, cz);
        int[] h;
        if(heightCache.TryGetValue(key, out h)) return h;
        Region reg = RegionFor(cx, cz);
        long off;
        if(reg.heights.TryGetValue(key, out off)) h = ReadHeights(reg, off);
        if(h == null) {
            h = new int[Chunk.X_SIZE * Chunk.Z_SIZE];
            for(int z = 0; z < Chunk.Z_SIZE; z++) for(int x = 0; x < Chunk.X_SIZE; x++)
                h[z * Chunk.X_SIZE + x] = g != null ? g.SurfaceYProcedural(cx * Chunk.X_SIZE + x, cz * Chunk.Z_SIZE + z) : 0;
            using(var ms = new MemoryStream()) {
                using(var ds = new DeflateStream(ms, System.IO.Compression.CompressionLevel.Fastest, true)) using(var bw = new BinaryWriter(ds)) foreach(int v in h) bw.Write(v);
                reg.heights[key] = Append(reg, 2, cx, 0, cz, ms.ToArray());
            }
        }
        if(heightCache.Count >= MaxCachedHeights) heightCache.Clear();
        heightCache[key] = h;
        return h;
    }

    // ------------------------------------------------------------------ region files
    static Region RegionFor(int cx, int cz) {
        int rx = FD(cx, RegionColumns), rz = FD(cz, RegionColumns);
        long key = K2(rx, rz);
        Region r;
        if(regions.TryGetValue(key, out r)) return r;
        r = new Region { path = Path.Combine(root, "r." + rx + "." + rz + ".bit") };
        Scan(r);
        regions[key] = r;
        return r;
    }

    // Index all complete records; a record cut off by a crash is removed so later appends stay readable.
    static void Scan(Region r) {
        if(!File.Exists(r.path)) return;
        try {
            using(var fs = new FileStream(r.path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read)) {
                var br = new BinaryReader(fs);
                long good = 0;
                if(fs.Length < 4 || br.ReadInt32() != Magic) { fs.SetLength(0); return; }
                good = 4;
                while(fs.Position < fs.Length) {
                    long start = fs.Position;
                    if(fs.Length - start < 17) break;
                    byte type = br.ReadByte(); int cx = br.ReadInt32(), cy = br.ReadInt32(), cz = br.ReadInt32(), len = br.ReadInt32();
                    if(len < 0 || fs.Position + len > fs.Length || (type != 1 && type != 2)) break;
                    if(type == 1) r.chunks[K3(cx, cy, cz)] = start; else r.heights[K2(cx, cz)] = start;
                    fs.Position += len; good = fs.Position;
                }
                if(good < fs.Length) { Debug.LogWarning("BLOCK ISLAND TERRAIN: incomplete record removed from " + r.path); fs.SetLength(good); }
            }
        } catch(Exception e) { Debug.LogWarning("BLOCK ISLAND TERRAIN: could not read " + r.path + ": " + e.Message); }
    }

    static byte[] ReadPayload(Region r, long off) {
        if(off < 0) return null;   // the write failed; regenerate
        try {
            using(var fs = new FileStream(r.path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                fs.Position = off; var br = new BinaryReader(fs);
                br.ReadByte(); br.ReadInt32(); br.ReadInt32(); br.ReadInt32(); int len = br.ReadInt32();
                return br.ReadBytes(len);
            }
        } catch(Exception e) { Debug.LogWarning("BLOCK ISLAND TERRAIN: read failed in " + r.path + ": " + e.Message); return null; }
    }
    static ChunkData ReadChunk(Region r, long off) {
        byte[] p = ReadPayload(r, off); if(p == null) return null;
        try { return DecodeChunk(p); } catch(Exception e) { Debug.LogWarning("BLOCK ISLAND TERRAIN: damaged chunk in " + r.path + ": " + e.Message); return null; }
    }
    static int[] ReadHeights(Region r, long off) {
        byte[] p = ReadPayload(r, off); if(p == null) return null;
        try {
            var h = new int[Chunk.X_SIZE * Chunk.Z_SIZE];
            using(var ms = new MemoryStream(p)) using(var ds = new DeflateStream(ms, CompressionMode.Decompress)) using(var br = new BinaryReader(ds)) for(int i = 0; i < h.Length; i++) h[i] = br.ReadInt32();
            return h;
        } catch(Exception e) { Debug.LogWarning("BLOCK ISLAND TERRAIN: damaged heights in " + r.path + ": " + e.Message); return null; }
    }

    // Returns the record offset, or -1 when it could not be written.
    static long Append(Region r, byte type, int cx, int cy, int cz, byte[] payload) {
        try {
            Directory.CreateDirectory(root);
            bool fresh = !File.Exists(r.path) || new FileInfo(r.path).Length == 0;
            using(var fs = new FileStream(r.path, FileMode.Append, FileAccess.Write, FileShare.Read)) using(var bw = new BinaryWriter(fs)) {
                if(fresh) bw.Write(Magic);
                bw.Flush(); long start = fs.Position;
                bw.Write(type); bw.Write(cx); bw.Write(cy); bw.Write(cz); bw.Write(payload.Length); bw.Write(payload);
                return start;
            }
        } catch(Exception e) { Debug.LogWarning("BLOCK ISLAND TERRAIN: write failed for " + r.path + ": " + e.Message); return -1; }
    }
}
