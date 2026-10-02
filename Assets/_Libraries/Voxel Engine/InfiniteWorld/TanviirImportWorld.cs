using UnityEngine;
using System.IO;
using System.Collections.Generic;

// Stage 6.13.6 - streamed Tanviir import source.
// Import data is divided into independently loadable tiles. 6.13.5 Spawn128 is retained as tile 0
// for compatibility; later Minecraft region tiles can be added without changing world/save IDs.
public static class TanviirImportWorld {
    public static bool active;
    const int TileSize=128;
    class Tile { public int minX,maxX,minZ,maxZ,width; public ushort[] id; public byte[] meta; public int[] top; public int stamp; }
    static readonly Dictionary<long,Tile> tiles=new Dictionary<long,Tile>();
    static readonly HashSet<long> missing=new HashSet<long>();
    static int accessStamp; const int MaxCachedTiles=10;
    // 6.13.11: block-name lookup used to run for virtually every sampled voxel/face.
    // Cache the resolved Block object per legacy id+metadata combination.
    static readonly Dictionary<int,Block> mappedBlocks=new Dictionary<int,Block>();

    public static void Begin(){ active=true; ClearCache(); mappedBlocks.Clear(); missingBlockNames.Clear(); TanviirNativeWorld.Begin(); }
    public static void End(){ active=false; ClearCache(); mappedBlocks.Clear(); TanviirNativeWorld.End(); }
    public static void ClearCache(){ tiles.Clear(); missing.Clear(); }
    static long Key(int tx,int tz){ return ((long)tx<<32) ^ (uint)tz; }
    static int FloorDiv(int a,int b){ int q=a/b,r=a%b; return r!=0 && ((r<0)!=(b<0)) ? q-1:q; }

    static Tile LoadTile(int tx,int tz){
        long k=Key(tx,tz); Tile t; if(tiles.TryGetValue(k,out t)) { t.stamp=++accessStamp; return t; } if(missing.Contains(k)) return null;
        // 6.13.5 preview occupies translated coordinates -64..63, so it is the central compatibility tile.
        string resource=(tx==0 && tz==0) ? "Tanviir/Spawn128" : ("Tanviir/Tiles/tile_"+tx+"_"+tz);
        TextAsset a=Resources.Load<TextAsset>(resource);
        if(a==null){ missing.Add(k); return null; }
        try {
            using(var br=new BinaryReader(new MemoryStream(a.bytes))){
                string magic=new string(br.ReadChars(4)); if(magic!="TVR1" && magic!="TVR2") throw new IOException("bad magic "+magic);
                t=new Tile(); t.minX=br.ReadInt32(); t.maxX=br.ReadInt32(); t.minZ=br.ReadInt32(); t.maxZ=br.ReadInt32();
                t.width=t.maxX-t.minX+1; int depth=t.maxZ-t.minZ+1; int count=t.width*depth*256;
                t.id=new ushort[count]; t.meta=new byte[count]; t.top=new int[t.width*depth]; for(int n=0;n<t.top.Length;n++) t.top[n]=-1;
                for(int z=t.minZ;z<=t.maxZ;z++) for(int x=t.minX;x<=t.maxX;x++){
                    int runs=br.ReadUInt16(); int col=((z-t.minZ)*t.width+(x-t.minX))*256; int ci=(z-t.minZ)*t.width+(x-t.minX);
                    for(int s=0;s<runs;s++){
                        int y=br.ReadByte(),len=br.ReadByte(); ushort id=br.ReadUInt16(); byte meta=br.ReadByte();
                        for(int yy=y;yy<y+len && yy<256;yy++){ t.id[col+yy]=id; t.meta[col+yy]=meta; if(id!=0 && yy>t.top[ci]) t.top[ci]=yy; }
                    }
                }
            }
            t.stamp=++accessStamp; tiles[k]=t; TrimCache(k); Debug.Log("TANVIIR 6.13.8: streamed import tile "+tx+","+tz); return t;
        } catch(System.Exception e){ Debug.LogError("TANVIIR tile load failed: "+e.Message); missing.Add(k); return null; }
    }


    static void TrimCache(long keep) {
        while(tiles.Count>MaxCachedTiles) {
            long victim=0; int oldest=int.MaxValue; bool found=false;
            foreach(var kv in tiles) if(kv.Key!=keep && kv.Value.stamp<oldest) { oldest=kv.Value.stamp; victim=kv.Key; found=true; }
            if(!found) break; tiles.Remove(victim);
        }
    }

    static Tile TileFor(int x,int z){
        // Central preview has exact bounds -64..63. Future tiles use 128-block grid.
        if(x>=-64&&x<=63&&z>=-64&&z<=63) return LoadTile(0,0);
        return LoadTile(FloorDiv(x+64,TileSize),FloorDiv(z+64,TileSize));
    }
    public static bool Contains(int x,int y,int z){ if(TanviirNativeWorld.Available) return TanviirNativeWorld.Contains(x,y,z); Tile t=TileFor(x,z); return t!=null && y>=0&&y<256&&x>=t.minX&&x<=t.maxX&&z>=t.minZ&&z<=t.maxZ; }
    public static int SurfaceY(int x,int z){ if(TanviirNativeWorld.Available) return TanviirNativeWorld.SurfaceY(x,z); Tile t=TileFor(x,z); if(t==null||x<t.minX||x>t.maxX||z<t.minZ||z>t.maxZ) return 108; return t.top[(z-t.minZ)*t.width+(x-t.minX)]; }
    // 6.14.13: bulk native read for streamed 32^3 Block Island chunks.
    // Avoids routing every voxel through InfiniteVoxelWorld.GetBlock(), which repeated
    // chunk/local coordinate math and edit-dictionary lookups 32,768 times per mesh build.
    public static void FillChunk(DataBlock[] dst,int ox,int oy,int oz) {
        if(dst==null || dst.Length < Chunk.X_SIZE*Chunk.Y_SIZE*Chunk.Z_SIZE) return;
        int i=0;
        for(int z=0;z<Chunk.Z_SIZE;z++) for(int y=0;y<Chunk.Y_SIZE;y++) for(int x=0;x<Chunk.X_SIZE;x++,i++) {
            int wy=oy+y;
            if(wy<0 || wy>=256) { dst[i]=default(DataBlock); continue; }
            ushort id; byte meta;
            if(!TanviirNativeWorld.Raw(ox+x,wy,oz+z,out id,out meta) || id==0) { dst[i]=default(DataBlock); continue; }
            Block b=Map(id,meta);
            dst[i]=b!=null ? new DataBlock(b,Direction(id,meta)) : default(DataBlock);
        }
    }

    public static DataBlock Sample(int x,int y,int z){
        if(TanviirNativeWorld.Available) { ushort aid; byte am; if(!TanviirNativeWorld.Raw(x,y,z,out aid,out am) || aid==0) return default(DataBlock); Block ab=Map(aid,am); return ab!=null?new DataBlock(ab,Direction(aid,am)):default(DataBlock); }
        Tile t=TileFor(x,z); if(t==null||y<0||y>=256||x<t.minX||x>t.maxX||z<t.minZ||z>t.maxZ) return default(DataBlock);
        int i=(((z-t.minZ)*t.width+(x-t.minX))*256)+y; ushort id=t.id[i]; if(id==0)return default(DataBlock);
        Block b=Map(id,t.meta[i]); return b!=null?new DataBlock(b,Direction(id,t.meta[i])):default(DataBlock);
    }
    static readonly HashSet<string> missingBlockNames=new HashSet<string>();
    static Block B(string n){
        if(BlockSet.instance==null) return null;
        Block b=BlockSet.instance.FindBlock(n);
        if(b!=null) return b;
        // 6.13.12 visibility safety net: a missing palette entry must never punch an invisible
        // hole into an imported building. Keep a visible solid fallback and report each name once.
        if(missingBlockNames.Add(n)) Debug.LogWarning("TANVIIR 6.13.12: missing BlockSet entry '"+n+"' -> visible fallback");
        b=BlockSet.instance.FindBlock("Tanviir Weathered Stone");
        if(b==null) b=BlockSet.instance.FindBlock("Stone");
        return b;
    }
    static Block Map(ushort id,byte m){
        int key=(id<<4)|(m&15); Block cached;
        if(mappedBlocks.TryGetValue(key,out cached)) return cached;
        Block result=MapUncached(id,m); mappedBlocks[key]=result; return result;
    }
    static Block MapUncached(ushort id,byte m){ switch(id){
        case 1:return B("Tanviir Pale Stone"); case 2:return B("Grass"); case 3:return B("Dirt"); case 4:return B("Tanviir Cobble"); case 5:return B("Tanviir Old Plank");
        case 7:return B("Tanviir Dark Stone"); case 8:case 9:return B("Water"); case 12:return B("Sand"); case 13:return B("Tanviir Cobble"); case 14:return B("Tanviir Weathered Stone");
        case 17:return B(m==1?"Spruce Wood":m==2?"Birch Wood":"Light Wood"); case 18:return B("Green Leaf"); case 20:return B("Glass"); case 24:return B("Tanviir Warm Sandstone"); case 31:return B("Grass 1");
        case 35:return B(m==11?"Tanviir Blue Tile":m==13?"Tanviir Green Tile":m==14?"Tanviir Red Tile":"Tanviir Timber Plaster"); case 41:return B("Tanviir Gold Trim"); case 42:return B("Tanviir White Brick");
        case 43:case 44:return B("Tanviir Limestone"); case 45:return B("Tanviir Castle Wall"); case 48:return B("Tanviir Mossy Cobble"); case 49:return B("Tanviir Black Brick"); case 50:return B("Fire");
        case 53:case 134:return B("Tanviir Oak Stair"); case 80:return B("Tanviir White Brick"); case 85:return B("Tanviir Timber Railing"); case 87:return B("Tanviir Dark Stone"); case 89:return B("Tanviir Gold Mosaic");
        case 98:return B(m==1?"Tanviir Mossy Stone":"Tanviir Castle Wall"); case 102:return B("Glass"); case 109:return B("Tanviir Castle Stair"); case 121:return B("Tanviir Ivory Brick"); case 128:return B("Tanviir Warm Sandstone");
        case 6:return B("Grass 1"); case 30:return B("Tanviir Utility Stone"); case 47:return B("Tanviir Old Plank"); case 54:return B("Chest");
        case 64:case 71:return B("Tanviir Dark Timber"); case 65:return B("Tanviir Utility Wood"); case 67:return B("Tanviir Castle Stair");
        case 79:return B("Glass"); case 81:return B("BI Cactus"); case 86:case 91:return B("Pumpkin"); case 95:return B(m==11?"Tanviir Blue Tile":m==13?"Tanviir Green Tile":"Glass");
        case 101:return B("Tanviir Utility Stone"); case 103:return B("Tanviir Green Tile"); case 106:return B("Green Leaf"); case 107:return B("Tanviir Timber Railing");
        case 108:return B("Tanviir Red Roof Stair"); case 114:return B("Tanviir Dark Stone Stair"); case 126:return B("Tanviir Old Plank");
        case 135:return B("Tanviir Oak Stair"); case 136:return B("Tanviir Oak Stair"); case 139:return B("Tanviir Stone Railing");
        case 15:return B("Tanviir Iron Ore"); case 16:return B("Tanviir Coal Ore"); case 19:return B("Tanviir Sponge Stone"); case 21:return B("Tanviir Lapis Ore"); case 22:return B("Tanviir Blue Tile");
        case 23:return B("Tanviir Carved Stone"); case 25:return B("Tanviir Old Plank"); case 32:return B("Tanviir Dead Shrub"); case 37:case 38:return B("Flower 1"); case 39:case 40:return B("Fungus 1");
        case 46:return B("TNT"); case 56:return B("Tanviir Diamond Ore"); case 57:return B("Tanviir Crystal Block"); case 73:case 74:return B("Tanviir Redstone Ore"); case 82:return B("Tanviir Clay");
        case 88:return B("Tanviir Warm Sandstone"); case 112:return B("Tanviir Nether Brick"); case 113:return B("Tanviir Dark Railing"); case 116:return B("Tanviir Runed Stone"); case 120:return B("Tanviir Portal Stone");
        case 123:case 124:return B("Tanviir Lamp Stone"); case 129:return B("Tanviir Emerald Ore"); case 133:return B("Tanviir Emerald Block"); case 145:return B("Tanviir Iron Block"); case 152:return B("Tanviir Redstone Block");
        case 155:return B("Tanviir Quartz"); case 156:return B("Tanviir Quartz Stair"); case 159:return B(m==14?"Tanviir Red Tile":m==11?"Tanviir Blue Tile":m==13?"Tanviir Green Tile":"Tanviir Clay"); case 170:return B("Tanviir Old Plank"); case 172:return B("Tanviir Clay");
        // 6.13.12: additional visible legacy 1.6.x blocks used by detailed adventure maps.
        case 26:return B("Tanviir Old Plank"); case 27:case 28:case 66:return B("Rail Straight");
        case 29:case 33:case 34:case 36:return B("Tanviir Iron Block"); case 51:return B("Fire");
        case 55:return B("Tanviir Redstone Ore"); case 58:return B("Tanviir Old Plank"); case 60:return B("Dirt");
        case 61:case 62:return B("Tanviir Castle Wall"); case 63:case 68:return B("Tanviir Old Plank");
        case 69:case 70:case 72:case 77:return B("Tanviir Carved Stone"); case 75:case 76:return B("Tanviir Lamp Stone");
        case 78:return B("Tanviir White Brick"); case 83:return B("Grass 1"); case 84:return B("Tanviir Old Plank");
        case 90:return B("Tanviir Portal Stone"); case 92:return B("Tanviir White Brick"); case 93:case 94:return B("Tanviir Redstone Ore");
        case 96:return B("Tanviir Dark Timber"); case 99:case 100:return B("Fungus 1"); case 104:case 105:return B("Grass 1");
        case 110:return B("Tanviir Mossy Stone"); case 111:return B("Green Leaf"); case 115:return B("BI Red Flower");
        case 117:case 118:return B("Tanviir Iron Block"); case 119:return B("Tanviir Portal Stone"); case 122:return B("Tanviir Crystal Block");
        case 125:return B("Tanviir Old Plank"); case 127:return B("Tanviir Old Plank"); case 130:return B("Chest");
        case 131:case 132:return B("Tanviir Dark Timber"); case 137:return B("Tanviir Runed Stone"); case 138:return B("Tanviir Crystal Block");
        case 140:return B("Tanviir Clay"); case 143:return B("Tanviir Carved Stone"); case 146:return B("Chest");
        case 147:case 148:case 149:case 150:case 151:return B("Tanviir Redstone Ore"); case 153:return B("Tanviir Quartz");
        case 154:return B("Tanviir Iron Block"); case 157:return B("Rail Straight"); case 158:return B("Tanviir Castle Wall");
        case 160:return B("Tanviir Glass Pane"); case 161:return B("Green Leaf"); case 162:return B("Light Wood");
        case 163:case 164:return B("Tanviir Oak Stair"); case 171:return B("Tanviir Timber Plaster");
        case 173:return B("Tanviir Coal Ore"); case 174:return B("Glass");
        // 6.13.15 COMPLETE VISIBILITY PASS: legacy IDs that were not explicitly mapped before.
        // They are deliberately visible even when Block Island has no exact gameplay equivalent.
        case 10:case 11:return B("Tanviir Lava");
        case 52:return B("Tanviir Spawner");
        case 59:case 141:case 142:return B("Tanviir Crop");
        case 97:return B("Tanviir Weathered Stone");
        case 144:return B("Tanviir Utility Stone");
        case 165:return B("Tanviir Utility Stone");
        case 166:return B("Tanviir Utility Stone");
        case 167:return B("Tanviir Utility Wood");
        case 168:return B("Tanviir Carved Stone");
        case 169:return B("Tanviir Green Tile");
        case 175:return B("Tanviir Crop");
        default:return UnknownVisible(id,m); }}
    static readonly HashSet<int> unknownReported=new HashSet<int>();
    static Block UnknownVisible(ushort id,byte m){
        int key=(id<<4)|(m&15);
        if(unknownReported.Add(key)) Debug.LogWarning("TANVIIR 6.13.15: unknown Minecraft block id/meta "+id+"/"+m+" -> visible Weathered Stone fallback");
        return B("Tanviir Weathered Stone");
    }
    static BlockDirection Direction(ushort id,byte m){
        // Legacy stairs: metadata 0=east,1=west,2=south,3=north; bit 2 is upside-down.
        if(id==53||id==67||id==108||id==109||id==114||id==128||id==134||id==135||id==136||id==156) { switch(m&3){case 0:return BlockDirection.RIGHT;case 1:return BlockDirection.LEFT;case 2:return BlockDirection.BACKWARD;default:return BlockDirection.FORWARD;} }
        return BlockDirection.FORWARD; }
}
