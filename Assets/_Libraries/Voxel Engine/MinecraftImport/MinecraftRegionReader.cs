using System; using System.IO; using System.Collections.Generic;

public static class MinecraftRegionReader {
 public struct McBlock { public int x,y,z; public string name; public Dictionary<string,string> state; }

 public struct ChunkCoord { public int x,z; public ChunkCoord(int x,int z){this.x=x;this.z=z;} }
 public static List<ChunkCoord> GetExistingChunks(string worldPath){
  var result=new List<ChunkCoord>(); string regionDir=Path.Combine(worldPath,"region"); if(!Directory.Exists(regionDir))return result;
  foreach(string file in Directory.GetFiles(regionDir,"r.*.*.mc?")){
   string ext=Path.GetExtension(file).ToLowerInvariant(); if(ext!=".mca"&&ext!=".mcr")continue;
   string[] parts=Path.GetFileNameWithoutExtension(file).Split('.'); int rx,rz; if(parts.Length!=3||!int.TryParse(parts[1],out rx)||!int.TryParse(parts[2],out rz))continue;
   try{using(var fs=File.OpenRead(file))using(var br=new BinaryReader(fs)){if(fs.Length<4096)continue;for(int i=0;i<1024;i++){byte[] loc=br.ReadBytes(4);if(loc.Length<4)break;int sector=(loc[0]<<16)|(loc[1]<<8)|loc[2];if(sector==0)continue;int lx=i%32,lz=i/32;result.Add(new ChunkCoord(rx*32+lx,rz*32+lz));}}}catch(Exception e){UnityEngine.Debug.LogWarning("Minecraft region index skipped: "+e.Message);}
  }
  return result;
 }
 public static IEnumerable<McBlock> ReadWorld(string worldPath,int centerChunkX,int centerChunkZ,int radiusChunks,Action<int,int> progress){
  string region=Path.Combine(worldPath,"region"); if(!Directory.Exists(region))yield break; int done=0,total=(radiusChunks*2+1)*(radiusChunks*2+1);
  for(int cz=centerChunkZ-radiusChunks;cz<=centerChunkZ+radiusChunks;cz++)for(int cx=centerChunkX-radiusChunks;cx<=centerChunkX+radiusChunks;cx++){
   foreach(var b in ReadChunk(region,cx,cz))yield return b; done++;if(progress!=null)progress(done,total);
  }
 }
 public static IEnumerable<McBlock> ReadChunkPublic(string worldPath,int cx,int cz){ return ReadChunk(Path.Combine(worldPath,"region"),cx,cz); }
 public static bool TryReadSpawn(string worldPath,out int x,out int y,out int z){x=0;y=80;z=0;try{string f=Path.Combine(worldPath,"level.dat");if(!File.Exists(f))return false;byte[] raw;using(var fs=File.OpenRead(f))using(var gz=new System.IO.Compression.GZipStream(fs,System.IO.Compression.CompressionMode.Decompress))using(var ms=new MemoryStream()){gz.CopyTo(ms);raw=ms.ToArray();}var root=MinecraftNbt.Read(raw);var data=root!=null?(root.Get("Data")??root):null;if(data==null)return false;var tx=data.Get("SpawnX");var ty=data.Get("SpawnY");var tz=data.Get("SpawnZ");if(tx==null||tz==null)return false;x=tx.Int();if(ty!=null)y=ty.Int(80);z=tz.Int();return true;}catch(Exception e){UnityEngine.Debug.LogWarning("Minecraft spawn could not be read: "+e.Message);return false;}}
 static IEnumerable<McBlock> ReadChunk(string regionDir,int cx,int cz){
  int rx=FloorDiv(cx,32),rz=FloorDiv(cz,32); int lx=Mod(cx,32),lz=Mod(cz,32); string path=Path.Combine(regionDir,"r."+rx+"."+rz+".mca"); if(!File.Exists(path)){ string oldPath=Path.Combine(regionDir,"r."+rx+"."+rz+".mcr"); if(File.Exists(oldPath)) path=oldPath; else yield break; }
  byte[] payload=null;byte comp=0;using(var fs=File.OpenRead(path))using(var br=new BinaryReader(fs)){fs.Position=(lx+lz*32)*4;byte[] loc=br.ReadBytes(4);if(loc.Length<4)yield break;int sector=(loc[0]<<16)|(loc[1]<<8)|loc[2];if(sector==0)yield break;fs.Position=sector*4096L;byte[] lenb=br.ReadBytes(4);if(lenb.Length<4)yield break;int len=(lenb[0]<<24)|(lenb[1]<<16)|(lenb[2]<<8)|lenb[3];if(len<=1||len>16*1024*1024)yield break;comp=br.ReadByte();payload=br.ReadBytes(len-1);}
  MinecraftNbt.Tag root=TryParseChunk(comp,payload,cx,cz); if(root==null)yield break;
  var level=root.Get("Level")??root; var sections=level.Get("sections")??level.Get("Sections");
  if(sections!=null&&sections.List!=null){
   foreach(var sec in sections.List){
    int sy=(sbyte)(sec.Get("Y")!=null?sec.Get("Y").Int():0); var bs=sec.Get("block_states"); var palette=bs!=null?bs.Get("palette"):sec.Get("Palette"); var data=bs!=null?bs.Get("data"):sec.Get("BlockStates");
    if(palette!=null&&palette.List!=null&&palette.List.Count>0){ long[] packed=data!=null?data.value as long[]:null; int bits=Math.Max(4,CeilLog2(palette.List.Count));
     for(int i=0;i<4096;i++){int pi=packed==null?0:PaletteIndex(packed,i,bits);if(pi<0||pi>=palette.List.Count)continue;var pe=palette.List[pi];var nt=pe.Get("Name")??pe.Get("name");string name=nt!=null?nt.String():"minecraft:air";if(IsAir(name))continue;int x=i&15,z=(i>>4)&15,y=(i>>8)&15;var mb=new McBlock();mb.x=cx*16+x;mb.y=sy*16+y;mb.z=cz*16+z;mb.name=name;var props=pe.Get("Properties")??pe.Get("properties");if(props!=null&&props.Compound!=null){mb.state=new Dictionary<string,string>();foreach(var kv in props.Compound)mb.state[kv.Key]=kv.Value.String();}yield return mb;}
    } else {
     byte[] blocks=sec.Get("Blocks")!=null?sec.Get("Blocks").value as byte[]:null; byte[] meta=sec.Get("Data")!=null?sec.Get("Data").value as byte[]:null; byte[] add=sec.Get("Add")!=null?sec.Get("Add").value as byte[]:null; if(blocks==null)continue;
     for(int i=0;i<Math.Min(4096,blocks.Length);i++){int id=blocks[i];if(add!=null)id|=Nibble(add,i)<<8;if(id==0)continue;int md=meta!=null?Nibble(meta,i):0;int x=i&15,z=(i>>4)&15,y=(i>>8)&15;var mb=new McBlock();mb.x=cx*16+x;mb.y=sy*16+y;mb.z=cz*16+z;mb.name=LegacyName(id,md);yield return mb;}
    }
   }
   yield break;
  }
  // Pre-Anvil / McRegion (.mcr): one 16x128x16 Blocks array in Level.
  byte[] oldBlocks=level.Get("Blocks")!=null?level.Get("Blocks").value as byte[]:null; byte[] oldData=level.Get("Data")!=null?level.Get("Data").value as byte[]:null; if(oldBlocks==null)yield break;
  for(int x=0;x<16;x++)for(int z=0;z<16;z++)for(int y=0;y<128;y++){int i=(x*16+z)*128+y;if(i>=oldBlocks.Length)continue;int id=oldBlocks[i];if(id==0)continue;int md=oldData!=null?Nibble(oldData,i):0;var mb=new McBlock();mb.x=cx*16+x;mb.y=y;mb.z=cz*16+z;mb.name=LegacyName(id,md);yield return mb;}
 }


 static MinecraftNbt.Tag TryParseChunk(byte comp,byte[] payload,int cx,int cz){try{return MinecraftNbt.Read(MinecraftNbt.DecompressChunk(comp,payload));}catch(Exception e){UnityEngine.Debug.LogWarning("Minecraft chunk "+cx+","+cz+" skipped: "+e.Message);return null;}}
 static int Nibble(byte[] a,int index){int p=index>>1;if(a==null||p<0||p>=a.Length)return 0;return (index&1)==0?(a[p]&15):((a[p]>>4)&15);}
 static string LegacyName(int id,int data){switch(id){case 1:return "minecraft:stone";case 2:return "minecraft:grass_block";case 3:return "minecraft:dirt";case 4:return "minecraft:cobblestone";case 5:return "minecraft:oak_planks";case 7:return "minecraft:bedrock";case 8:case 9:return "minecraft:water";case 10:case 11:return "minecraft:lava";case 12:return "minecraft:sand";case 13:return "minecraft:gravel";case 14:return "minecraft:gold_ore";case 15:return "minecraft:iron_ore";case 16:return "minecraft:coal_ore";case 17:return data==2?"minecraft:birch_log":data==1?"minecraft:spruce_log":"minecraft:oak_log";case 18:return data==2?"minecraft:birch_leaves":data==1?"minecraft:spruce_leaves":"minecraft:oak_leaves";case 20:return "minecraft:glass";case 24:return "minecraft:sandstone";case 31:return "minecraft:grass";case 35:return data==14?"minecraft:red_wool":data==11?"minecraft:blue_wool":data==5?"minecraft:green_wool":"minecraft:white_wool";case 37:return "minecraft:dandelion";case 38:return "minecraft:poppy";case 41:return "minecraft:gold_block";case 42:return "minecraft:iron_block";case 43:case 44:return "minecraft:stone_slab";case 45:return "minecraft:bricks";case 47:return "minecraft:oak_planks";case 48:return "minecraft:mossy_cobblestone";case 49:return "minecraft:obsidian";case 50:return "minecraft:torch";case 53:return "minecraft:oak_stairs";case 56:return "minecraft:diamond_ore";case 57:return "minecraft:diamond_block";case 65:return "minecraft:ladder";case 66:return "minecraft:rail";case 67:return "minecraft:cobblestone_stairs";case 73:case 74:return "minecraft:redstone_ore";case 78:case 80:return "minecraft:snow";case 79:return "minecraft:ice";case 81:return "minecraft:cactus";case 82:return "minecraft:clay";case 85:return "minecraft:oak_planks";case 87:return "minecraft:netherrack";case 88:return "minecraft:soul_sand";case 89:return "minecraft:glowstone";case 98:return "minecraft:stone_bricks";case 103:return "minecraft:melon";case 108:return "minecraft:brick_stairs";case 109:return "minecraft:stone_brick_stairs";case 112:return "minecraft:nether_bricks";case 121:return "minecraft:end_stone";case 129:return "minecraft:emerald_ore";case 133:return "minecraft:emerald_block";case 155:return "minecraft:quartz_block";case 159:return data==14?"minecraft:red_terracotta":data==11?"minecraft:blue_terracotta":data==5?"minecraft:green_terracotta":"minecraft:terracotta";case 172:return "minecraft:terracotta";case 174:return "minecraft:packed_ice";default:return "minecraft:stone";}}
 static int PaletteIndex(long[] a,int index,int bits){ulong mask=(1UL<<bits)-1;int per=64/bits;int paddedLen=(4096+per-1)/per;if(a.Length>=paddedLen){int li=index/per,off=(index%per)*bits;if(li>=a.Length)return 0;return (int)(((ulong)a[li]>>off)&mask);}long bit=(long)index*bits;int word=(int)(bit>>6),off2=(int)(bit&63);if(word>=a.Length)return 0;ulong v=(ulong)a[word]>>off2;if(off2+bits>64&&word+1<a.Length)v|=(ulong)a[word+1]<<(64-off2);return (int)(v&mask);}
 static bool IsAir(string n){return n=="minecraft:air"||n=="minecraft:cave_air"||n=="minecraft:void_air";}
 static int CeilLog2(int v){int b=0,x=v-1;while(x>0){b++;x>>=1;}return b;}
 static int FloorDiv(int a,int b){int q=a/b,r=a%b;return r!=0&&((r<0)!=(b<0))?q-1:q;} static int Mod(int a,int b){int m=a%b;return m<0?m+b:m;}
}
