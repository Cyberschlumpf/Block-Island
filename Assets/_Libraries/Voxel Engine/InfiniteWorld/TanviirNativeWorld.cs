using UnityEngine;
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Threading.Tasks;

// Stage 6.14.3 - native Block Island Tanviir world reader + hot voxel cache.
// Runtime source is BIR1 only. Minecraft .mca/NBT files are not required or read.
public static class TanviirNativeWorld {
    const int OffsetX=193, OffsetZ=27, MaxCachedChunks=4096;
    class C { public ushort[] id=new ushort[16*16*256]; public byte[] meta=new byte[16*16*256]; public int[] height=new int[256]; public ushort sectionMask; public int stamp; }
    static readonly Dictionary<long,C> cache=new Dictionary<long,C>();
    static readonly HashSet<long> missing=new HashSet<long>();
    static readonly HashSet<long> pending=new HashSet<long>();
    static readonly object cacheLock=new object(); static int stamp; static string root;
    // Mesh generation asks tens of thousands of neighboring voxels from the same few source
    // chunks. Avoid taking cacheLock for every voxel; retain the last resolved source chunk.
    // Loaded C instances are immutable, so holding this reference is safe even if LRU evicts it.
    static int hotX=int.MinValue, hotZ=int.MinValue; static C hotC;
    static string Root { get { return root ?? Path.Combine(Application.streamingAssetsPath,"TanviirNative/region"); } }
    static long Key(int x,int z){return ((long)x<<32)^(uint)z;}
    static int FD(int a,int b){int q=a/b,r=a%b;return r!=0&&((r<0)!=(b<0))?q-1:q;}
    static int Mod(int a,int b){int r=a%b;return r<0?r+b:r;}
    public static bool Available { get { return Directory.Exists(Root); } }
    public static void Begin(){root=Path.Combine(Application.streamingAssetsPath,"TanviirNative/region");lock(cacheLock){cache.Clear();missing.Clear();pending.Clear();stamp=0;} hotX=hotZ=int.MinValue; hotC=null; Debug.Log("TANVIIR 6.14.3 NATIVE: BIR1 world online; no Minecraft MCA/NBT runtime dependency. root="+Root);}
    public static void End(){lock(cacheLock){cache.Clear();missing.Clear();pending.Clear();}}
    public static bool IsWorldColumnReady(int wx,int wz){
        // Block Island chunks are 32x32 in X/Z, while BIR1 stores Minecraft-style 16x16 columns.
        // A single BI chunk can therefore overlap up to 3x3 BIR source columns when the import
        // offset is not 16-aligned.  The old wx*16/+15 math only prepared half of the BI chunk.
        int x0=wx*Chunk.X_SIZE, z0=wz*Chunk.Z_SIZE;
        int a=FD(x0-OffsetX,16),b=FD(x0+Chunk.X_SIZE-1-OffsetX,16);
        int c=FD(z0-OffsetZ,16),d=FD(z0+Chunk.Z_SIZE-1-OffsetZ,16);
        lock(cacheLock){for(int x=a;x<=b;x++)for(int z=c;z<=d;z++){long k=Key(x,z);if(!cache.ContainsKey(k)&&!missing.Contains(k))return false;}return true;}
    }
    public static void RequestWorldColumn(int wx,int wz){
        int x0=wx*Chunk.X_SIZE, z0=wz*Chunk.Z_SIZE;
        int a=FD(x0-OffsetX,16),b=FD(x0+Chunk.X_SIZE-1-OffsetX,16);
        int c=FD(z0-OffsetZ,16),d=FD(z0+Chunk.Z_SIZE-1-OffsetZ,16);
        for(int x=a;x<=b;x++)for(int z=c;z<=d;z++)Request(x,z);
    }
    static void Request(int x,int z){long k=Key(x,z);lock(cacheLock){if(cache.ContainsKey(k)||missing.Contains(k)||pending.Contains(k))return;pending.Add(k);}Task.Run(()=>{C q=Load(x,z);lock(cacheLock){pending.Remove(k);if(q==null)missing.Add(k);else{q.stamp=++stamp;cache[k]=q;Trim(k);}}});}
    public static bool Contains(int x,int y,int z){if(y<0||y>255)return false;return Get(FD(x-OffsetX,16),FD(z-OffsetZ,16))!=null;}
    public static int SurfaceY(int x,int z){int mx=x-OffsetX,mz=z-OffsetZ;C c=Get(FD(mx,16),FD(mz,16));if(c==null)return 108;int h=c.height[Mod(mz,16)*16+Mod(mx,16)];return Mathf.Clamp(h>0?h-1:108,0,255);}
    public static bool Raw(int x,int y,int z,out ushort id,out byte meta){id=0;meta=0;if(y<0||y>255)return false;int mx=x-OffsetX,mz=z-OffsetZ;int cx=FD(mx,16),cz=FD(mz,16);C c;if(cx==hotX&&cz==hotZ)c=hotC;else{c=Get(cx,cz);hotX=cx;hotZ=cz;hotC=c;}if(c==null)return false;int i=(y*16+Mod(mz,16))*16+Mod(mx,16);id=c.id[i];meta=c.meta[i];return true;}
    public static bool HasBlocksInWorldChunk(int wx,int wy,int wz){
        // BI chunks are 32 high; BIR1 sectionMask bits represent 16-block source sections.
        // Test every BIR section overlapped by this 32^3 BI chunk instead of treating wy as
        // a BIR section index.  The old code was the direct cause of apparently random 32x32 holes.
        int y0=wy*Chunk.Y_SIZE, y1=y0+Chunk.Y_SIZE-1;
        if(y1<0 || y0>255) return false;
        int s0=Mathf.Clamp(FD(y0,16),0,15), s1=Mathf.Clamp(FD(y1,16),0,15);
        int x0=wx*Chunk.X_SIZE,z0=wz*Chunk.Z_SIZE;
        int a=FD(x0-OffsetX,16),b=FD(x0+Chunk.X_SIZE-1-OffsetX,16);
        int c=FD(z0-OffsetZ,16),d=FD(z0+Chunk.Z_SIZE-1-OffsetZ,16);
        ushort mask=0; for(int s=s0;s<=s1;s++) mask|=(ushort)(1<<s);
        for(int x=a;x<=b;x++)for(int z=c;z<=d;z++){C q=Get(x,z);if(q!=null&&(q.sectionMask&mask)!=0)return true;}
        return false;
    }
    public static int CountBlocksInWorldChunk(int wx,int wy,int wz){
        int y0=wy*Chunk.Y_SIZE, y1=y0+Chunk.Y_SIZE;
        if(y1<=0 || y0>255)return 0;
        int n=0;
        for(int y=Mathf.Max(0,y0);y<Mathf.Min(256,y1);y++)
        for(int z=wz*Chunk.Z_SIZE;z<wz*Chunk.Z_SIZE+Chunk.Z_SIZE;z++)
        for(int x=wx*Chunk.X_SIZE;x<wx*Chunk.X_SIZE+Chunk.X_SIZE;x++){
            ushort id;byte m;if(Raw(x,y,z,out id,out m)&&id!=0)n++;
        }
        return n;
    }
    static C Get(int x,int z){long k=Key(x,z);C c;lock(cacheLock){if(cache.TryGetValue(k,out c)){c.stamp=++stamp;return c;}if(missing.Contains(k))return null;}c=Load(x,z);lock(cacheLock){if(c==null){missing.Add(k);return null;}c.stamp=++stamp;cache[k]=c;Trim(k);return c;}}
    static void Trim(long keep){while(cache.Count>MaxCachedChunks){long v=0;int old=int.MaxValue;bool f=false;foreach(var q in cache)if(q.Key!=keep&&q.Value.stamp<old){old=q.Value.stamp;v=q.Key;f=true;}if(!f)break;cache.Remove(v);}}
    static C Load(int cx,int cz){try{int rx=FD(cx,32),rz=FD(cz,32),lx=Mod(cx,32),lz=Mod(cz,32);string p=Path.Combine(Root,"r."+rx+"."+rz+".bir");if(!File.Exists(p))return null;byte[] packed;using(FileStream f=File.OpenRead(p)){byte[] magic=new byte[4];if(f.Read(magic,0,4)!=4||magic[0]!='B'||magic[1]!='I'||magic[2]!='R'||magic[3]!='1')return null;f.Position=4L+(lx+lz*32)*12L;ulong off=ReadU64(f);uint len=ReadU32(f);if(off==0||len==0)return null;f.Position=(long)off;packed=new byte[len];ReadFully(f,packed,0,packed.Length);}byte[] raw=InflateZlib(packed);using(BinaryReader br=new BinaryReader(new MemoryStream(raw))){C c=new C();c.sectionMask=br.ReadUInt16();for(int i=0;i<256;i++)c.height[i]=br.ReadUInt16();for(int s=0;s<16;s++)if((c.sectionMask&(1<<s))!=0){int sy=br.ReadByte();for(int i=0;i<4096;i++){int ly=(i>>8)&15,localZ=(i>>4)&15,lx2=i&15,di=((sy*16+ly)*16+localZ)*16+lx2;c.id[di]=br.ReadUInt16();}for(int i=0;i<4096;i++){int ly=(i>>8)&15,localZ=(i>>4)&15,lx2=i&15,di=((sy*16+ly)*16+localZ)*16+lx2;c.meta[di]=br.ReadByte();}}return c;}}catch(Exception e){Debug.LogWarning("TANVIIR NATIVE BIR read failed "+cx+","+cz+": "+e.Message);return null;}}
    static byte[] InflateZlib(byte[] p){using(var input=new MemoryStream(p)){if(p.Length>2)input.Position=2;using(var ds=new DeflateStream(input,CompressionMode.Decompress))using(var output=new MemoryStream()){ds.CopyTo(output);return output.ToArray();}}}
    static ulong ReadU64(Stream s){byte[] b=new byte[8];ReadFully(s,b,0,8);return BitConverter.ToUInt64(b,0);} static uint ReadU32(Stream s){byte[] b=new byte[4];ReadFully(s,b,0,4);return BitConverter.ToUInt32(b,0);} static void ReadFully(Stream s,byte[] b,int o,int n){while(n>0){int r=s.Read(b,o,n);if(r<=0)throw new EndOfStreamException();o+=r;n-=r;}}
}
