using UnityEngine;

// 1.0.32: pure hollow build sphere. No terrain, water, vegetation or wildlife.
// Radius 64; only a thin solid shell is generated. The interior is completely empty.
public static class RelativityWorld {
    public const float Radius = 64f;
    const float ShellThickness = 3f;
    static Block shell;

    static void Resolve() {
        if(shell!=null) return;
        var bs=BlockSet.instance; if(bs==null) return;
        shell=bs.FindBlock("Stone") ?? bs.FindBlock("Rock") ?? bs.FindBlock("Dirt");
    }
    static DataBlock B(Block b) { return b!=null ? new DataBlock(b) : default(DataBlock); }

    public static DataBlock Sample(int x,int y,int z) {
        Resolve();
        if(shell==null) return default(DataBlock);
        // Sample voxel centres so the inside surface is visually as round as the voxel grid permits.
        float r=new Vector3(x+.5f,y+.5f,z+.5f).magnitude;
        if(r>=Radius && r<Radius+ShellThickness) return B(shell);
        return default(DataBlock);
    }
}
