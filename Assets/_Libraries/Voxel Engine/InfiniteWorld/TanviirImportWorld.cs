using UnityEngine;
using System.IO;

// Tanviir: fixed Block Island world in StreamingAssets/TanviirNative (BIR1 regions read by TanviirNativeWorld).
// Every voxel id is an index into TanviirNative/palette.txt (line 1 = id 1; the Block Island block names,
// mostly "Tanviir: ..." texture-pack blocks) and the meta byte is the BlockDirection. No Minecraft data
// or translation is involved any more.
public static class TanviirImportWorld {
    public static bool active;

    static string[] paletteNames = new string[0];
    static Block[] palette = new Block[0];
    static BlockSet resolvedFor;

    public static void Begin(){ active=true; LoadPalette(); TanviirNativeWorld.Begin(); }
    public static void End(){ active=false; TanviirNativeWorld.End(); }

    static void LoadPalette(){
        string p=Path.Combine(Application.streamingAssetsPath,"TanviirNative","palette.txt");
        try { paletteNames=File.Exists(p)?File.ReadAllLines(p):new string[0]; }
        catch(System.Exception e){ Debug.LogError("TANVIIR palette unreadable: "+e.Message); paletteNames=new string[0]; }
        resolvedFor=null;
    }

    // Block objects belong to the BlockSet of the current scene: resolve the names again when it changes.
    static void Resolve(){
        BlockSet set=BlockSet.instance;
        if(set==resolvedFor) return;
        resolvedFor=set;
        palette=new Block[paletteNames.Length+1];
        if(set==null) return;
        for(int i=0;i<paletteNames.Length;i++){
            Block b=set.FindBlock(paletteNames[i]);
            if(b==null) Debug.LogWarning("TANVIIR: block '"+paletteNames[i]+"' not in BlockSet");
            palette[i+1]=b;
        }
    }

    static DataBlock Make(ushort id,byte meta){
        Block b=id<palette.Length?palette[id]:null;
        return b!=null?new DataBlock(b,(BlockDirection)meta):default(DataBlock);
    }

    public static bool Contains(int x,int y,int z){ return TanviirNativeWorld.Available && TanviirNativeWorld.Contains(x,y,z); }
    public static int SurfaceY(int x,int z){ return TanviirNativeWorld.Available ? TanviirNativeWorld.SurfaceY(x,z) : 108; }

    // Bulk read of one streamed 32^3 chunk.
    public static void FillChunk(DataBlock[] dst,int ox,int oy,int oz) {
        if(dst==null || dst.Length < Chunk.X_SIZE*Chunk.Y_SIZE*Chunk.Z_SIZE) return;
        Resolve();
        int i=0;
        for(int z=0;z<Chunk.Z_SIZE;z++) for(int y=0;y<Chunk.Y_SIZE;y++) for(int x=0;x<Chunk.X_SIZE;x++,i++) {
            int wy=oy+y;
            ushort id; byte meta;
            if(wy<0 || wy>=256 || !TanviirNativeWorld.Raw(ox+x,wy,oz+z,out id,out meta) || id==0) { dst[i]=default(DataBlock); continue; }
            dst[i]=Make(id,meta);
        }
    }

    public static DataBlock Sample(int x,int y,int z){
        ushort id; byte meta;
        if(!TanviirNativeWorld.Available || !TanviirNativeWorld.Raw(x,y,z,out id,out meta) || id==0) return default(DataBlock);
        Resolve();
        return Make(id,meta);
    }
}
