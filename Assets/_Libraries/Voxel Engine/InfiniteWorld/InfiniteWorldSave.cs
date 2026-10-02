using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

// Stage 6.11.1: named saves with an explicit absolute-coordinate player edit journal.
// The journal is written directly by Builder, so saving no longer depends on which
// InfiniteVoxelWorld/Map instance happened to receive the placement call.
public static class InfiniteWorldSave {
    const int Magic=0x42495736; // BIW6: native world type in the normal Block Island save
    const int LegacyMagic=0x42495735; // BIW5
    public enum WorldType : byte { Islands=0, Tanviir=1, BlockIsland=2, LightGarden=3 }
    public const string TanviirSaveName="Tanviir";
    public const string BlockIslandSaveName="Block Island";
    public const string LightGardenSaveName="Lichtgarten";
    public static WorldType CurrentWorldType=WorldType.Islands;
    public static string CurrentWorldName="";
    public static bool LoadRequested=false;
    public static Vector3 LoadedPlayerPosition;
    public static Quaternion LoadedPlayerRotation=Quaternion.identity;
    public static bool HasLoadedPlayerTransform=false;

    struct SavedEdit { public int x,y,z; public DataBlock block; public SavedEdit(int x,int y,int z,DataBlock b){this.x=x;this.y=y;this.z=z;block=b;} }
    static readonly Dictionary<string,SavedEdit> playerEdits=new Dictionary<string,SavedEdit>();
    static string Key(int x,int y,int z) { return x+":"+y+":"+z; }

    static bool saveMigrationChecked=false;
    public static string SaveDirectory {
        get {
            string docs=Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string p=String.IsNullOrEmpty(docs) ? Path.Combine(Application.persistentDataPath,"Worlds") : Path.Combine(docs,"The Voxel Box","Saves");
            Directory.CreateDirectory(p);
            if(!saveMigrationChecked) { saveMigrationChecked=true; MigrateLegacySaves(p); }
            return p;
        }
    }
    static string SaveDir { get { return SaveDirectory; } }
    static void MigrateLegacySaves(string destination) {
        try {
            string legacy=Path.Combine(Application.persistentDataPath,"Worlds");
            if(!Directory.Exists(legacy) || String.Equals(Path.GetFullPath(legacy),Path.GetFullPath(destination),StringComparison.OrdinalIgnoreCase)) return;
            foreach(string src in Directory.GetFiles(legacy,"*.infinite")) {
                string dst=Path.Combine(destination,Path.GetFileName(src));
                if(!File.Exists(dst)) File.Copy(src,dst,false);
            }
        } catch(Exception e) { Debug.LogWarning("Legacy save migration skipped: "+e.Message); }
    }
    public static void OpenSaveDirectory() {
        try { Application.OpenURL("file://"+SaveDirectory.Replace("\\","/")); }
        catch(Exception e) { Debug.LogWarning("Could not open save folder: "+e.Message); }
    }
    public static string SanitizeName(string name) {
        if(String.IsNullOrWhiteSpace(name)) name="My World";
        foreach(char c in Path.GetInvalidFileNameChars()) name=name.Replace(c,'_');
        name=name.Trim(); if(name.Length>48) name=name.Substring(0,48); return name.Length==0?"My World":name;
    }
    public static string PathFor(string name) { return Path.Combine(SaveDir,SanitizeName(name)+".infinite"); }
    public static string[] GetWorldNames() { string[] f=Directory.GetFiles(SaveDir,"*.infinite"); string[] n=new string[f.Length]; for(int i=0;i<f.Length;i++) n[i]=Path.GetFileNameWithoutExtension(f[i]); Array.Sort(n,StringComparer.OrdinalIgnoreCase); return n; }
    public static void BeginNewWorld() { BeginNewWorld(WorldType.Islands); }
    public static void BeginNewWorld(WorldType type) {
        CurrentWorldType=type; CurrentWorldName=""; LoadRequested=false; HasLoadedPlayerTransform=false; playerEdits.Clear();
        ApplyWorldType(type);
    }
    static void ApplyWorldType(WorldType type) {
        if(type==WorldType.Tanviir){ BlockIslandWorldSource.BlockIslandNativeWorldActive=false; BlockIslandNativeWorld.End(); TanviirImportWorld.Begin(); }
        else if(type==WorldType.BlockIsland){ TanviirImportWorld.End(); BlockIslandWorldSource.BlockIslandNativeWorldActive=true; BlockIslandNativeWorld.Begin(); }
        else { TanviirImportWorld.End(); BlockIslandWorldSource.BlockIslandNativeWorldActive=false; BlockIslandNativeWorld.End(); }
    }
    // 6.14.14.1: resolve the base-world identity BEFORE the Game scene is loaded.
    // The streamer/bootstrap must be born in Tanviir mode; switching the immutable BIR1
    // source only after Game components already exist can leave a save with edits but no base world.
    public static void RequestLoad(string name) {
        CurrentWorldName=SanitizeName(name);
        LoadRequested=true; HasLoadedPlayerTransform=false; playerEdits.Clear();
        WorldType type;
        if(TryReadWorldType(CurrentWorldName,out type)) {
            CurrentWorldType=type;
            ApplyWorldType(type);
            Debug.Log("Preactivated save base world: "+CurrentWorldName+" -> "+type);
        }
    }

    public static bool TryReadWorldType(string name,out WorldType type) {
        type=WorldType.Islands;
        string path=PathFor(name);
        if(!File.Exists(path)) return false;
        try { using(var fs=File.OpenRead(path)) using(var br=new BinaryReader(fs)) {
            int magic=br.ReadInt32();
            if(magic!=Magic && magic!=LegacyMagic) return false;
            string storedName=br.ReadString();
            type = magic==Magic ? (WorldType)br.ReadByte()
                : (String.Equals(storedName,TanviirSaveName,StringComparison.OrdinalIgnoreCase)?WorldType.Tanviir:WorldType.Islands);
            return true;
        }} catch(Exception e) { Debug.LogError("Save header read failed: "+e); return false; }
    }

    public static void RecordPlayerEdit(Vector3i pos,DataBlock block) { playerEdits[Key(pos.x,pos.y,pos.z)]=new SavedEdit(pos.x,pos.y,pos.z,block); }

    // Also merge the world's sparse edit store. This preserves edits made by systems other than Builder.
    static void MergeWorldEdits(InfiniteVoxelWorld w) {
        if(w==null) return;
        foreach(var c in w.GetEdits()) foreach(var e in c.Value) {
            int idx=e.Key;
            int lx=idx % Chunk.X_SIZE; idx/=Chunk.X_SIZE;
            int ly=idx % Chunk.Y_SIZE; int lz=idx/Chunk.Y_SIZE;
            int x=c.Key.x*Chunk.X_SIZE+lx, y=c.Key.y*Chunk.Y_SIZE+ly, z=c.Key.z*Chunk.Z_SIZE+lz;
            playerEdits[Key(x,y,z)]=new SavedEdit(x,y,z,e.Value);
        }
    }

    public static bool Save(InfiniteVoxelWorld w,string name) {
        if(w==null) return false; name=SanitizeName(name); CurrentWorldName=name; MergeWorldEdits(w);
        string final=PathFor(name), temp=final+".tmp";
        try { using(var fs=File.Create(temp)) using(var bw=new BinaryWriter(fs)) {
            bw.Write(Magic); bw.Write(name); bw.Write((byte)CurrentWorldType); bw.Write(w.generator!=null?w.generator.seed:0);
            GameObject player=null; try { player=GameObject.FindWithTag("Player"); } catch {}
            bool hp=player!=null; bw.Write(hp); if(hp){Vector3 p=player.transform.position;Quaternion q=player.transform.rotation;bw.Write(p.x);bw.Write(p.y);bw.Write(p.z);bw.Write(q.x);bw.Write(q.y);bw.Write(q.z);bw.Write(q.w);}
            bw.Write(playerEdits.Count);
            foreach(var kv in playerEdits){var e=kv.Value;bw.Write(e.x);bw.Write(e.y);bw.Write(e.z);bw.Write(e.block.blockID);bw.Write((byte)e.block.direction);}
        }
        if(File.Exists(final)) File.Delete(final); File.Move(temp,final);
        Debug.Log("World saved: "+name+" persistent edits="+playerEdits.Count+" -> "+final); return true;
        } catch(Exception e){Debug.LogError("World save failed: "+e);try{if(File.Exists(temp))File.Delete(temp);}catch{}return false;}
    }
    public static bool SaveCurrent(InfiniteVoxelWorld w){return !String.IsNullOrEmpty(CurrentWorldName)&&Save(w,CurrentWorldName);}
    public static bool SaveTanviir(InfiniteVoxelWorld w){ CurrentWorldType=WorldType.Tanviir; return Save(w,TanviirSaveName); }
    public static bool SaveBlockIsland(InfiniteVoxelWorld w){ CurrentWorldType=WorldType.BlockIsland; return Save(w,BlockIslandSaveName); }
    public static bool SaveLightGarden(InfiniteVoxelWorld w){ CurrentWorldType=WorldType.LightGarden; return Save(w,LightGardenSaveName); }

    public static bool Load(InfiniteVoxelWorld w,string name) {
        string path=PathFor(name); if(w==null||!File.Exists(path)) return false;
        try { using(var fs=File.OpenRead(path)) using(var br=new BinaryReader(fs)) {
            int magic=br.ReadInt32(); if(magic!=Magic && magic!=LegacyMagic){Debug.LogWarning("Unsupported Block Island save format."); return false;}
            CurrentWorldName=br.ReadString();
            // BIW6 stores the base-world identity in the same normal save header. BIW5 remains readable.
            CurrentWorldType = magic==Magic ? (WorldType)br.ReadByte() : (String.Equals(CurrentWorldName,"Tanviir",StringComparison.OrdinalIgnoreCase)?WorldType.Tanviir:WorldType.Islands);
            ApplyWorldType(CurrentWorldType);
            int seed=br.ReadInt32(); if(w.generator!=null) w.generator.seed=seed;
            bool hp=br.ReadBoolean(); HasLoadedPlayerTransform=hp; if(hp){LoadedPlayerPosition=new Vector3(br.ReadSingle(),br.ReadSingle(),br.ReadSingle());LoadedPlayerRotation=new Quaternion(br.ReadSingle(),br.ReadSingle(),br.ReadSingle(),br.ReadSingle());}
            playerEdits.Clear(); int count=br.ReadInt32();
            for(int i=0;i<count;i++){int x=br.ReadInt32(),y=br.ReadInt32(),z=br.ReadInt32();DataBlock b=default(DataBlock);b.blockID=br.ReadInt32();b.direction=(BlockDirection)br.ReadByte();playerEdits[Key(x,y,z)]=new SavedEdit(x,y,z,b);w.SetBlock(x,y,z,b);}
            LoadRequested=false; Debug.Log("World loaded: "+CurrentWorldName+" persistent edits="+count); return true;
        }} catch(Exception e){Debug.LogError("World load failed: "+e);return false;}
    }
}
