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
    public static void BeginCustom(string regionRoot){ active=true; ClearCache(); mappedBlocks.Clear(); missingBlockNames.Clear(); TanviirNativeWorld.BeginCustom(regionRoot); }
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
            dst[i]=Resolve(id,meta,ox+x,wy,oz+z);
        }
    }

    public static DataBlock Sample(int x,int y,int z){
        if(TanviirNativeWorld.Available) { ushort aid; byte am; if(!TanviirNativeWorld.Raw(x,y,z,out aid,out am) || aid==0) return default(DataBlock); return Resolve(aid,am,x,y,z); }
        Tile t=TileFor(x,z); if(t==null||y<0||y>=256||x<t.minX||x>t.maxX||z<t.minZ||z>t.maxZ) return default(DataBlock);
        int i=(((z-t.minZ)*t.width+(x-t.minX))*256)+y; ushort id=t.id[i]; if(id==0)return default(DataBlock);
        Block b=Map(id,t.meta[i]); return b!=null?new DataBlock(b,Direction(id,t.meta[i],t.meta[i])):default(DataBlock);
    }
    static readonly HashSet<string> missingBlockNames=new HashSet<string>();
    // Legacy Minecraft id/meta (<=175) -> Tanviir texture-pack block ("Tanviir: <name>").
    // A few existing Block Island blocks stay in use where they carry gameplay: Water, Chest, Minecart rails.
    static Block B(string n){
        if(BlockSet.instance==null) return null;
        Block b=BlockSet.instance.FindBlock(n);
        if(b!=null) return b;
        // A missing palette entry must never punch an invisible hole into an imported building.
        if(missingBlockNames.Add(n)) Debug.LogWarning("TANVIIR: missing BlockSet entry '"+n+"' -> visible fallback");
        b=BlockSet.instance.FindBlock(TanviirPackBlocks.Prefix+"Stone");
        if(b==null) b=BlockSet.instance.FindBlock("Stone");
        return b;
    }
    static Block T(string n){ return B(TanviirPackBlocks.Prefix+n); }
    static Block Map(ushort id,byte m){
        int key=(id<<4)|(m&15); Block cached;
        if(mappedBlocks.TryGetValue(key,out cached)) return cached;
        Block result=MapUncached(id,m); mappedBlocks[key]=result; return result;
    }
    static DataBlock Resolve(ushort id,byte m,int x,int y,int z){
        // Door tops store only hinge/top bits; the facing lives in the lower half.
        byte dm=m;
        if((id==64||id==71) && (m&8)!=0){ ushort lid; byte lm; if(TanviirNativeWorld.Raw(x,y-1,z,out lid,out lm) && lid==id) dm=lm; else dm=0; }
        Block b=Map(id,m);
        return b!=null ? new DataBlock(b,Direction(id,m,dm)) : default(DataBlock);
    }
    static string Wood(int i){ return TanviirPackBlocks.Wood[i&3]; }
    static string Dye(int m){ return TanviirPackBlocks.DyeName[m&15]; }
    static Block Slab(string material,bool upper){ return T((upper?"Upper ":"")+material+" Slab"); }
    static readonly string[] DoubleSlab={"Double Stone Slab","Sandstone","Oak Planks","Cobblestone","Bricks","Stone Bricks","Nether Bricks","Quartz Block","Smooth Stone","Smooth Sandstone","Stone","Stone","Stone","Stone","Stone","Quartz Block"};
    static readonly string[] MonsterEgg={"Stone","Cobblestone","Stone Bricks","Mossy Stone Bricks","Cracked Stone Bricks","Chiseled Stone Bricks"};
    static readonly string[] StoneBrick={"Stone Bricks","Mossy Stone Bricks","Cracked Stone Bricks","Chiseled Stone Bricks"};
    // null = not rendered (small redstone/decoration details without a block equivalent).
    static Block MapUncached(ushort id,byte m){ switch(id){
        case 1:return T("Stone"); case 2:return T("Grass Block"); case 3:return T("Dirt"); case 4:return T("Cobblestone");
        case 5:return T((m&7)==4?"Jungle Planks":(m&7)==5?"Spruce Planks":Wood(m)+" Planks");
        case 6:return T(((m&7)>3?"Oak":Wood(m))+" Sapling"); case 7:return T("Bedrock");
        case 8:case 9:return B("Water"); case 10:case 11:return T("Lava");
        case 12:return T("Sand"); case 13:return T("Gravel"); case 14:return T("Gold Ore"); case 15:return T("Iron Ore"); case 16:return T("Coal Ore");
        case 17:return T(Wood(m)+" Log"); case 18:return T(Wood(m)+" Leaves"); case 19:return T("Sponge"); case 20:return T("Glass");
        case 21:return T("Lapis Ore"); case 22:return T("Lapis Block"); case 23:return T("Dispenser");
        case 24:return T(m==1?"Chiseled Sandstone":m==2?"Smooth Sandstone":"Sandstone"); case 25:return T("Note Block");
        case 26:return T((m&8)!=0?"Bed Head":"Bed Foot"); case 27:case 28:case 157:return B("Rail Straight"); case 29:return T("Sticky Piston");
        case 30:return T("Cobweb"); case 31:return T(m==0?"Dead Bush":m==2?"Fern":"Tall Grass"); case 32:return T("Dead Bush");
        case 33:return T("Piston"); case 34:case 36:return null; case 35:return T(Dye(m)+" Wool");
        case 37:return T("Dandelion"); case 38:return T("Rose"); case 39:return T("Brown Mushroom"); case 40:return T("Red Mushroom");
        case 41:return T("Gold Block"); case 42:return T("Iron Block"); case 43:return T(DoubleSlab[m&15]);
        case 44:return Slab(TanviirPackBlocks.StoneSlab[m&7],(m&8)!=0); case 45:return T("Bricks"); case 46:return T("TNT");
        case 47:return T("Bookshelf"); case 48:return T("Mossy Cobblestone"); case 49:return T("Obsidian"); case 50:return T("Torch");
        case 51:return T("Fire"); case 52:return T("Mob Spawner"); case 53:return T("Oak Stairs"); case 54:case 130:case 146:return B("Chest");
        case 55:return T("Redstone Wire"); case 56:return T("Diamond Ore"); case 57:return T("Diamond Block"); case 58:return T("Crafting Table");
        case 59:return T("Wheat"); case 60:return T("Farmland"); case 61:return T("Furnace"); case 62:return T("Lit Furnace");
        case 63:return null; case 64:return T((m&8)!=0?"Oak Door Upper":"Oak Door Lower"); case 65:return T("Ladder");
        case 66:return B((m&15)>=6&&(m&15)<=9?"Rail Curve":"Rail Straight"); case 67:return T("Cobblestone Stairs"); case 68:return T("Wall Sign");
        case 69:return null; case 70:return T("Stone Pressure Plate"); case 71:return T((m&8)!=0?"Iron Door Upper":"Iron Door Lower");
        case 72:return T("Wooden Pressure Plate"); case 73:case 74:return T("Redstone Ore"); case 75:case 76:return T("Redstone Torch");
        case 77:return null; case 78:return T("Snow Layer"); case 79:return T("Ice"); case 80:return T("Snow"); case 81:return T("Cactus");
        case 82:return T("Clay"); case 83:return T("Sugar Cane"); case 84:return T("Jukebox"); case 85:return T("Oak Fence");
        case 86:return T("Pumpkin"); case 87:return T("Netherrack"); case 88:return T("Soul Sand"); case 89:return T("Glowstone");
        case 90:return T("Nether Portal"); case 91:return T("Jack o'Lantern"); case 92:return T("Cake"); case 93:case 94:return T("Repeater");
        case 95:return T(Dye(m)+" Stained Glass"); case 96:return T((m&4)!=0?"Open Trapdoor":"Trapdoor");
        case 97:return T(MonsterEgg[Mathf.Min(m&7,5)]); case 98:return T(StoneBrick[m&3]);
        case 99:return T((m&15)==10||(m&15)==15?"Mushroom Stem":"Brown Mushroom Block"); case 100:return T((m&15)==10||(m&15)==15?"Mushroom Stem":"Red Mushroom Block");
        case 101:return T("Iron Bars"); case 102:return T("Glass Pane"); case 103:return T("Melon"); case 104:return T("Pumpkin Stem"); case 105:return T("Melon Stem");
        case 106:return T("Vines"); case 107:return T("Oak Fence"); case 108:return T("Brick Stairs"); case 109:return T("Stone Brick Stairs");
        case 110:return T("Mycelium"); case 111:return T("Lily Pad"); case 112:return T("Nether Bricks"); case 113:return T("Nether Brick Fence");
        case 114:return T("Nether Brick Stairs"); case 115:return T("Nether Wart"); case 116:return T("Enchanting Table"); case 117:return T("Brewing Stand");
        case 118:return T("Cauldron"); case 119:return null; case 120:return T("End Portal Frame"); case 121:return T("End Stone"); case 122:return T("Dragon Egg");
        case 123:return T("Redstone Lamp"); case 124:return T("Lit Redstone Lamp");
        case 125:return T(((m&7)==4?"Jungle":(m&7)==5?"Spruce":Wood(m))+" Planks"); case 126:return Slab((m&7)==4?"Jungle":(m&7)==5?"Spruce":Wood(m),(m&8)!=0);
        case 127:return null; case 128:return T("Sandstone Stairs"); case 129:return T("Emerald Ore"); case 131:case 132:return null;
        case 133:return T("Emerald Block"); case 134:return T("Spruce Stairs"); case 135:return T("Birch Stairs"); case 136:return T("Jungle Stairs");
        case 137:return T("Command Block"); case 138:return T("Beacon"); case 139:return T(m==1?"Mossy Cobblestone Wall":"Cobblestone Wall");
        case 140:return null; case 141:return T("Carrots"); case 142:return T("Potatoes"); case 143:case 144:return null; case 145:return T("Anvil");
        case 147:return T("Gold Pressure Plate"); case 148:return T("Iron Pressure Plate"); case 149:case 150:return T("Comparator");
        case 151:return T("Stone Slab"); case 152:return T("Redstone Block"); case 153:return T("Quartz Ore"); case 154:return T("Hopper");
        case 155:return T(m==1?"Chiseled Quartz":m>=2?"Quartz Pillar":"Quartz Block"); case 156:return T("Quartz Stairs"); case 158:return T("Dropper");
        case 159:return T(Dye(m)+" Stained Clay"); case 160:return T(Dye(m)+" Stained Glass Pane"); case 161:return T("Oak Leaves");
        case 162:return T((m&3)==1?"Spruce Log":"Jungle Log"); case 163:return T("Jungle Stairs"); case 164:return T("Spruce Stairs");
        case 170:return T("Yellow Wool"); case 171:return T(Dye(m)+" Carpet"); case 172:return T("Hardened Clay"); case 173:return T("Coal Block");
        case 174:return T("Ice"); case 175:return T("Tall Grass");
        default:return UnknownVisible(id,m); }}
    static readonly HashSet<int> unknownReported=new HashSet<int>();
    static Block UnknownVisible(ushort id,byte m){
        int key=(id<<4)|(m&15);
        if(unknownReported.Add(key)) Debug.LogWarning("TANVIIR: unknown Minecraft block id/meta "+id+"/"+m+" -> visible Stone fallback");
        return T("Stone");
    }
    // Directions: FORWARD/BACKWARD/LEFT/RIGHT. Cube fronts face +z/-z/-x/+x;
    // TanviirPackBlock panels sit on the north/south/east/west wall respectively.
    static readonly BlockDirection[] DoorSide={BlockDirection.RIGHT,BlockDirection.FORWARD,BlockDirection.LEFT,BlockDirection.BACKWARD};
    static BlockDirection Direction(ushort id,byte m,byte doorMeta){
        switch(id){
            // Legacy stairs: metadata 0=east,1=west,2=south,3=north; bit 2 is upside-down.
            case 53:case 67:case 108:case 109:case 114:case 128:case 134:case 135:case 136:case 156:case 163:case 164:
                switch(m&3){case 0:return BlockDirection.RIGHT;case 1:return BlockDirection.LEFT;case 2:return BlockDirection.BACKWARD;default:return BlockDirection.FORWARD;}
            // Furnace, dispenser, dropper: 2=north,3=south,4=west,5=east.
            case 23:case 61:case 62:case 158:
                switch(m&7){case 2:return BlockDirection.BACKWARD;case 4:return BlockDirection.LEFT;case 5:return BlockDirection.RIGHT;default:return BlockDirection.FORWARD;}
            // Pumpkins: 0=south,1=west,2=north,3=east.
            case 86:case 91:
                switch(m&3){case 1:return BlockDirection.LEFT;case 2:return BlockDirection.BACKWARD;case 3:return BlockDirection.RIGHT;default:return BlockDirection.FORWARD;}
            // Ladder and wall sign: 2=on the south wall, 3=north, 4=east, 5=west.
            case 65:case 68:
                switch(m&7){case 2:return BlockDirection.BACKWARD;case 4:return BlockDirection.LEFT;case 5:return BlockDirection.RIGHT;default:return BlockDirection.FORWARD;}
            // Doors: lower half bits 0-1 facing, bit 2 open (the door then swings to the next wall).
            case 64:case 71: return DoorSide[((doorMeta&3)+((doorMeta&4)!=0?1:0))&3];
            // Open trapdoor: 0=south wall,1=north,2=east,3=west.
            case 96:
                switch(m&3){case 0:return BlockDirection.BACKWARD;case 2:return BlockDirection.LEFT;case 3:return BlockDirection.RIGHT;default:return BlockDirection.FORWARD;}
            // Vines: bit 1=south,2=west,4=north,8=east.
            case 106:
                if((m&1)!=0)return BlockDirection.BACKWARD; if((m&2)!=0)return BlockDirection.RIGHT; if((m&8)!=0)return BlockDirection.LEFT; return BlockDirection.FORWARD;
            // Rails: 0=north-south (rail model runs along z), 1=east-west, 2-5 slopes, 6-9 curves (66 only).
            case 27:case 28:case 157: { int r=m&7; return r==1||r==2||r==3?BlockDirection.RIGHT:BlockDirection.FORWARD; }
            case 66:
                switch(m&15){case 1:case 2:case 3:return BlockDirection.RIGHT;case 6:return BlockDirection.FORWARD;case 7:return BlockDirection.LEFT;case 8:return BlockDirection.BACKWARD;case 9:return BlockDirection.RIGHT;default:return BlockDirection.FORWARD;}
        }
        return BlockDirection.FORWARD; }
}
