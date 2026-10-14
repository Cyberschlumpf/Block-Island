using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

// Block Island - non-destructive global texture overrides by stable Block ID.
public static class BlockTextureDesigner {
 [Serializable] public class Entry { public int targetId; public int sourceId=-1; public string customFile=""; }
 [Serializable] class Store { public List<Entry> entries=new List<Entry>(); }
 class Original { public Face[] faces; }
 static Dictionary<int,Original> originals=new Dictionary<int,Original>();
 static Dictionary<string,Texture2D> customTextures=new Dictionary<string,Texture2D>();
 static Store store=new Store();
 public static string DirectoryPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"Block Island","BlockTextures"); } }
 public static string CustomTexturePath { get { return Path.Combine(DirectoryPath,"Textures"); } }
 public static string FilePath { get { return Path.Combine(DirectoryPath,"block_textures.json"); } }
 static Face CopyFace(Face f){ if(f==null)return null; Face n=new Face(); n.material=f.material;n.rect=f.rect;n.materialID=f.materialID;return n; }
 static Face[] GetFaces(Block b){
  if(b==null)return null; Face[] raw=Face.GetFaceList(b); if(raw==null||raw.Length==0)return null; Face[] copy=new Face[raw.Length]; for(int i=0;i<raw.Length;i++)copy[i]=CopyFace(raw[i]); return copy;
 }
 static void SetFaces(Block b,Face[] f){
  if(b==null||f==null)return; var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic; FieldInfo[] all=b.GetType().GetFields(flags); List<FieldInfo> fields=new List<FieldInfo>(); foreach(FieldInfo fi in all)if(fi.FieldType==typeof(Face))fields.Add(fi); int n=Mathf.Min(fields.Count,f.Length); for(int i=0;i<n;i++)fields[i].SetValue(b,CopyFace(f[i]));
 }
 public static string Structure(Block b){if(b==null)return "–";if(b is StairBlock)return "Treppe";if(b is FenceBlock)return "Zaun";if(b is SphereBlock)return "Kugel";if(b is CactusBlock)return "Kaktus";if(b is GroundBlock)return "Bodenblock";if(b is CubeBlock)return "Würfel";if(b is CrossBlock)return "Pflanze / Kreuzfläche";if(b is FluidBlock)return "Flüssigkeit";if(b is MeshBlock)return "3D-Mesh";if(b is GameObjectBlock)return "GameObject / Prefab";return b.GetType().Name;}
 public static bool Compatible(Block a,Block b){if(a==null||b==null)return false;Face[] af=Face.GetFaceList(a),bf=Face.GetFaceList(b);return a.GetType()==b.GetType()&&af!=null&&bf!=null&&af.Length==bf.Length&&af.Length>0;}
 public static void EnsureFolders(){try{
  if(!Directory.Exists(DirectoryPath)){
   string legacy=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"The Voxel Box","BlockTextures");
   if(Directory.Exists(legacy)) CopyLegacyFolder(legacy,DirectoryPath);
  }
  Directory.CreateDirectory(CustomTexturePath);
 }catch(Exception e){Debug.LogWarning("Block Designer folder: "+e.Message);}}
 static void CopyLegacyFolder(string src,string dst){Directory.CreateDirectory(dst);foreach(string f in Directory.GetFiles(src)){string d=Path.Combine(dst,Path.GetFileName(f));if(!File.Exists(d))File.Copy(f,d,false);}foreach(string dir in Directory.GetDirectories(src))CopyLegacyFolder(dir,Path.Combine(dst,Path.GetFileName(dir)));}
 public static void OpenTextureFolder(){EnsureFolders();Application.OpenURL("file://"+CustomTexturePath.Replace("\\","/"));}
 public static string[] CustomFiles(){EnsureFolders();try{string[] f=System.IO.Directory.GetFiles(CustomTexturePath,"*.png");Array.Sort(f,StringComparer.OrdinalIgnoreCase);return f;}catch{return new string[0];}}
 public static Texture2D LoadCustomTexture(string path,bool reload=false){
  if(string.IsNullOrEmpty(path)||!File.Exists(path))return null; Texture2D t;
  if(!reload&&customTextures.TryGetValue(path,out t)&&t!=null)return t;
  try{byte[] bytes=File.ReadAllBytes(path);t=new Texture2D(2,2,TextureFormat.RGBA32,false);t.name=Path.GetFileNameWithoutExtension(path);if(!t.LoadImage(bytes)){UnityEngine.Object.Destroy(t);return null;}t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Point;customTextures[path]=t;return t;}catch(Exception e){Debug.LogWarning("Block Designer PNG: "+e.Message);return null;}
 }
 public static void ReloadCustomTextures(){foreach(Texture2D t in customTextures.Values)if(t!=null)UnityEngine.Object.Destroy(t);customTextures.Clear();}
 public static void CaptureOriginals(BlockSet set){ originals.Clear(); if(set==null)return;for(int i=0;i<set.Count;i++){Face[] f=GetFaces(set[i]);if(f!=null)originals[i]=new Original{faces=f};} }
 public static void LoadAndApply(BlockSet set){
  EnsureFolders();store=new Store();try{if(File.Exists(FilePath)){Store s=JsonUtility.FromJson<Store>(File.ReadAllText(FilePath));if(s!=null&&s.entries!=null)store=s;}}catch(Exception e){Debug.LogWarning("Block Designer: "+e.Message);}
  foreach(Entry e in store.entries){if(!string.IsNullOrEmpty(e.customFile))ApplyCustomInternal(set,e.targetId,Path.Combine(CustomTexturePath,e.customFile),false);else ApplyInternal(set,e.targetId,e.sourceId,false);}
 }
 static bool ApplyInternal(BlockSet set,int target,int source,bool save){
  if(set==null||target<0||source<0||target>=set.Count||source>=set.Count)return false;Block t=set[target],s=set[source];if(!Compatible(t,s))return false;Face[] sf=GetFaces(s);if(sf==null)return false;SetFaces(t,sf);
  if(save){Entry e=store.entries.Find(x=>x.targetId==target);if(e==null){e=new Entry{targetId=target};store.entries.Add(e);}e.sourceId=source;e.customFile="";Save();}return true;
 }
 static Face CustomFace(BlockSet set,Face original,Texture2D tex){
  if(original==null||tex==null)return null;
  Face n=CopyFace(original); Material baseMat=original.material;
  if(baseMat!=null){
   Material m=new Material(baseMat); m.name="Block Designer - "+tex.name; m.mainTexture=tex; m.hideFlags=HideFlags.DontSave;
   n.material=m;
   // 0.9.9.7c: the mesh renderer addresses materials by materialID. A custom PNG
   // therefore has to be registered in this BlockSet, not forced to material slot 0.
   n.materialID=set!=null?set.AddMaterial(m):original.materialID;
  }
  n.rect=new Rect(0,0,1,1); return n;
 }
 static bool ApplyCustomInternal(BlockSet set,int target,string path,bool save){
  if(set==null||target<0||target>=set.Count)return false;Texture2D tex=LoadCustomTexture(path);if(tex==null)return false;Block b=set[target];Face[] current=GetFaces(b);if(current==null)return false;
  Face[] nf=new Face[current.Length];for(int i=0;i<current.Length;i++)nf[i]=CustomFace(set,current[i],tex);SetFaces(b,nf);
  if(save){Entry e=store.entries.Find(x=>x.targetId==target);if(e==null){e=new Entry{targetId=target};store.entries.Add(e);}e.sourceId=-1;e.customFile=Path.GetFileName(path);Save();}return true;
 }
 public static bool Apply(BlockSet set,int target,int source){return ApplyInternal(set,target,source,true);}
 public static bool ApplyCustom(BlockSet set,int target,string path){return ApplyCustomInternal(set,target,path,true);}
 public static bool Restore(BlockSet set,int target){Original o;if(!originals.TryGetValue(target,out o)||set==null||target<0||target>=set.Count)return false;SetFaces(set[target],o.faces);store.entries.RemoveAll(x=>x.targetId==target);Save();return true;}
 public static int OverrideSource(int target){Entry e=store.entries.Find(x=>x.targetId==target);return e==null?-1:e.sourceId;}
 public static string OverrideCustom(int target){Entry e=store.entries.Find(x=>x.targetId==target);return e==null?"":e.customFile;}
 static void Save(){try{EnsureFolders();File.WriteAllText(FilePath,JsonUtility.ToJson(store,true));}catch(Exception e){Debug.LogWarning("Block Designer save: "+e.Message);} }
}
