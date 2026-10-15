using System.Collections.Generic;

// Copy & Paste clipboard for selections up to 512 x 512 x 512 voxels.
// Stored run-length encoded in x-fastest order (x, then y, then z): long runs of air or of one
// material cost a single entry, so a 512^3 selection (134 M voxels) normally needs only a few MB.
public sealed class VoxelClipboard {
    public const int MaxSize = 512;
    public const int MaxRuns = 40000000;          // safety limit for extremely noisy selections

    public readonly int sizeX, sizeY, sizeZ;
    public long solidCount;
    readonly List<int> runLength = new List<int>();
    readonly List<int> runId = new List<int>();
    readonly List<byte> runDir = new List<byte>();

    public VoxelClipboard(int sx, int sy, int sz) { sizeX = sx; sizeY = sy; sizeZ = sz; }

    public long Volume { get { return (long)sizeX * sizeY * sizeZ; } }
    public int RunCount { get { return runLength.Count; } }
    public bool TooComplex { get { return runLength.Count > MaxRuns; } }

    public void Append(DataBlock b) {
        int id = b.IsEmpty() ? 0 : b.blockID;
        byte dir = id == 0 ? (byte)0 : (byte)b.direction;
        if(id != 0) solidCount++;
        int last = runLength.Count - 1;
        if(last >= 0 && runId[last] == id && runDir[last] == dir && runLength[last] < int.MaxValue) { runLength[last]++; return; }
        runLength.Add(1); runId.Add(id); runDir.Add(dir);
    }

    // Used when loading a saved structure (VoxelStructureLibrary).
    public void AddRun(int length, int id, byte dir) {
        if(length <= 0) return;
        if(id != 0) solidCount += length;
        runLength.Add(length); runId.Add(id); runDir.Add(id == 0 ? (byte)0 : dir);
    }

    public void GetRun(int i, out int length, out DataBlock block) {
        length = runLength[i];
        block = default(DataBlock);
        block.blockID = runId[i];
        block.direction = (BlockDirection)runDir[i];
    }
}
