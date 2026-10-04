using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

// The Voxel Box 0.9.9.7b - non-destructive global texture overrides by stable Block ID.
public static class BlockTextureDesigner {
 [Serializable] public class Entry { public int targetId; public int sourceId=-1; public string customFile=""; public string atlasResource=""; public Rect atlasRect=new Rect(0,0,1,1); }
 [Serializable] class Store { public List<Entry> entries=new List<Entry>(); }
 class Original { public Face[] faces; }
 static Dictionary<int,Original> originals=new Dictionary<int,Original>();
 static Dictionary<string,Texture2D> customTextures=new Dictionary<string,Texture2D>();
 static Store store=new Store();
 static Dictionary<int,Face> customVoxelAtlasFaces=new Dictionary<int,Face>();
 static Dictionary<Texture2D,Material> designerMaterials=new Dictionary<Texture2D,Material>();
 public static string DirectoryPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"The Voxel Box","BlockTextures"); } }
 public static string CustomTexturePath { get { return Path.Combine(DirectoryPath,"Textures"); } }
 public static string FilePath { get { return Path.Combine(DirectoryPath,"block_textures.json"); } }
 static Face CopyFace(Face f){ if(f==null)return null; Face n=new Face(); n.material=f.material;n.rect=f.rect;n.materialID=f.materialID;return n; }
 static Face[] GetFaces(Block b){
  if(b==null)return null; Face[] raw=Face.GetFaceList(b); if(raw==null||raw.Length==0)return null; Face[] copy=new Face[raw.Length]; for(int i=0;i<raw.Length;i++)copy[i]=CopyFace(raw[i]); return copy;
 }
 static void SetFaces(Block b,Face[] f){
  if(b==null||f==null)return; var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic; FieldInfo[] all=b.GetType().GetFields(flags); List<FieldInfo> fields=new List<FieldInfo>(); foreach(FieldInfo fi in all)if(fi.FieldType==typeof(Face))fields.Add(fi); int n=Mathf.Min(fields.Count,f.Length); for(int i=0;i<n;i++)fields[i].SetValue(b,CopyFace(f[i]));
 }
 static void SetFacesLikeEngine(BlockSet set,int blockID,Block b,Face[] f){
  // Runtime equivalent of the engine's internal FaceEditor path: assign the real
  // Material + UV Rect first, then let Block.Init rebuild materialID through
  // BlockSet.AddMaterial. Never trust/copy a stale materialID from another block.
  SetFaces(b,f);
  if(set!=null&&b!=null)b.Init(set,blockID);
 }
 public static string Structure(Block b){if(b==null)return "–";if(b is StairBlock)return "Treppe";if(b is FenceBlock)return "Zaun";if(b is SphereBlock)return "Kugel";if(b is CactusBlock)return "Kaktus";if(b is GroundBlock)return "Bodenblock";if(b is CubeBlock)return "Würfel";if(b is CrossBlock)return "Pflanze / Kreuzfläche";if(b is FluidBlock)return "Flüssigkeit";if(b is MeshBlock)return "3D-Mesh";if(b is GameObjectBlock)return "GameObject / Prefab";return b.GetType().Name;}
 public static bool Compatible(Block a,Block b){if(a==null||b==null)return false;Face[] af=Face.GetFaceList(a),bf=Face.GetFaceList(b);return a.GetType()==b.GetType()&&af!=null&&bf!=null&&af.Length==bf.Length&&af.Length>0;}
 public static void EnsureFolders(){try{System.IO.Directory.CreateDirectory(CustomTexturePath);}catch(Exception e){Debug.LogWarning("Block Designer folder: "+e.Message);}}
 public static void OpenTextureFolder(){EnsureFolders();Application.OpenURL("file://"+CustomTexturePath.Replace("\\","/"));}
 public static string[] CustomFiles(){EnsureFolders();try{string[] f=System.IO.Directory.GetFiles(CustomTexturePath,"*.png");Array.Sort(f,StringComparer.OrdinalIgnoreCase);return f;}catch{return new string[0];}}
 public static Texture2D LoadCustomTexture(string path,bool reload=false){
  if(string.IsNullOrEmpty(path)||!File.Exists(path))return null; Texture2D t;
  if(!reload&&customTextures.TryGetValue(path,out t)&&t!=null)return t;
  try{byte[] bytes=File.ReadAllBytes(path);t=new Texture2D(2,2,TextureFormat.RGBA32,false);t.name=Path.GetFileNameWithoutExtension(path);if(!t.LoadImage(bytes)){UnityEngine.Object.Destroy(t);return null;}t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Point;customTextures[path]=t;return t;}catch(Exception e){Debug.LogWarning("Block Designer PNG: "+e.Message);return null;}
 }
 public static void ReloadCustomTextures(){foreach(Texture2D t in customTextures.Values)if(t!=null)UnityEngine.Object.Destroy(t);customTextures.Clear();}
 public static void CaptureOriginals(BlockSet set){ originals.Clear();customVoxelAtlasFaces.Clear();designerMaterials.Clear(); if(set==null)return;for(int i=0;i<set.Count;i++){Face[] f=GetFaces(set[i]);if(f!=null)originals[i]=new Original{faces=f};} }
 public static void LoadAndApply(BlockSet set){
  EnsureFolders();store=new Store();try{if(File.Exists(FilePath)){Store s=JsonUtility.FromJson<Store>(File.ReadAllText(FilePath));if(s!=null&&s.entries!=null)store=s;}}catch(Exception e){Debug.LogWarning("Block Designer: "+e.Message);}
  foreach(Entry e in store.entries){if(!string.IsNullOrEmpty(e.atlasResource))ApplyAtlasInternal(set,e.targetId,e.atlasResource,e.atlasRect,false);else if(!string.IsNullOrEmpty(e.customFile))ApplyCustomInternal(set,e.targetId,Path.Combine(CustomTexturePath,e.customFile),false);else ApplyInternal(set,e.targetId,e.sourceId,false);}
 }
 static bool ApplyInternal(BlockSet set,int target,int source,bool save){
  if(set==null||target<0||source<0||target>=set.Count||source>=set.Count)return false;Block t=set[target],s=set[source];if(!Compatible(t,s))return false;Face[] sf=GetFaces(s);if(sf==null)return false;SetFacesLikeEngine(set,target,t,sf);
  if(save){Entry e=store.entries.Find(x=>x.targetId==target);if(e==null){e=new Entry{targetId=target};store.entries.Add(e);}e.sourceId=source;e.customFile="";e.atlasResource="";Save();}return true;
 }
 static Material InternalMaterialForTexture(BlockSet set,Texture2D tex,Material template,string label){
  if(set==null||tex==null)return null;
  // Use the engine's own material table first. This is the same path normal Block.Init faces use:
  // Face.material + Face.materialID + Face.rect. Never create one material per face.
  List<Material> mats=set.GetMaterials();
  if(mats!=null)for(int i=0;i<mats.Count;i++){Material m=mats[i];if(m!=null&&m.mainTexture==tex){designerMaterials[tex]=m;return m;}}
  Material cached;if(designerMaterials.TryGetValue(tex,out cached)&&cached!=null)return cached;
  if(template==null)return null;
  Material created=new Material(template);created.name="Block Designer "+label+" - "+tex.name;created.mainTexture=tex;created.hideFlags=HideFlags.DontSave;
  set.AddMaterial(created);designerMaterials[tex]=created;return created;
 }
 static Face InternalTextureFace(BlockSet set,Face original,Texture2D tex,Rect uv,string label){
  if(original==null||tex==null)return null;Material m=InternalMaterialForTexture(set,tex,original.material,label);if(m==null)return null;
  Face n=CopyFace(original);n.material=m;n.materialID=set.GetMaterials().IndexOf(m);n.rect=uv;return n;
 }
 static bool ApplyCustomInternal(BlockSet set,int target,string path,bool save){
  if(set==null||target<0||target>=set.Count)return false;Texture2D tex=LoadCustomTexture(path);if(tex==null)return false;Block b=set[target];Face[] current=GetFaces(b);if(current==null||current.Length==0)return false;
  // Same internal representation as every normal engine texture: one material slot + UV rect.
  Face first=InternalTextureFace(set,current[0],tex,new Rect(0,0,1,1),"PNG");if(first==null)return false;Face[] nf=new Face[current.Length];
  for(int i=0;i<current.Length;i++){nf[i]=CopyFace(first);nf[i].rect=new Rect(0,0,1,1);}SetFacesLikeEngine(set,target,b,nf);
  if(save){Entry e=store.entries.Find(x=>x.targetId==target);if(e==null){e=new Entry{targetId=target};store.entries.Add(e);}e.sourceId=-1;e.customFile=Path.GetFileName(path);e.atlasResource="";Save();}return true;
 }
 static Face AtlasFace(BlockSet set,Face original,Texture2D atlas,Rect uv){return InternalTextureFace(set,original,atlas,uv,"Atlas");}
 static bool ApplyAtlasInternal(BlockSet set,int target,string resource,Rect uv,bool save){
  if(set==null||target<0||target>=set.Count||string.IsNullOrEmpty(resource))return false;
  Texture2D atlas=Resources.Load<Texture2D>(resource);if(atlas==null)return false;
  Block targetBlock=set[target];Face[] current=GetFaces(targetBlock);Face customBase=null;
  if(targetBlock is CustomVoxelBlock){customBase=targetBlock.GetPreviewFace();if(customBase==null)return false;}
  else if(current==null||current.Length==0)return false;
  uv.x=Mathf.Clamp01(uv.x);uv.y=Mathf.Clamp01(uv.y);uv.width=Mathf.Clamp(uv.width,0.0001f,1f-uv.x);uv.height=Mathf.Clamp(uv.height,0.0001f,1f-uv.y);
  if(targetBlock is CustomVoxelBlock){customVoxelAtlasFaces[target]=AtlasFace(set,customBase,atlas,uv);}
  else{Face[] nf=new Face[current.Length];for(int i=0;i<current.Length;i++)nf[i]=AtlasFace(set,current[i],atlas,uv);SetFacesLikeEngine(set,target,targetBlock,nf);}
  if(save){Entry e=store.entries.Find(x=>x.targetId==target);if(e==null){e=new Entry{targetId=target};store.entries.Add(e);}e.sourceId=-1;e.customFile="";e.atlasResource=resource;e.atlasRect=uv;Save();}
  return true;
 }
 static void RefreshLiveVisuals(){
  // 1.0.16: texture overrides change UV/material data used when meshes are built.
  // Clear cached thumbnails immediately and invalidate already-rendered chunks so no restart is needed.
  try{BlockSet3DPreview.Clear();}catch{}
  try{
   Map map=Map.instance;if(map==null)return;
   if(map.infiniteRenderer!=null)map.infiniteRenderer.MarkAllVisibleDirty();
   else if(map.grid!=null)foreach(Chunk c in map.grid.chunks)if(c!=null)c.GetChunkRendererInstance().SetDirty();
  }catch(Exception ex){Debug.LogWarning("Block Designer live refresh: "+ex.Message);}
 }
 public static bool ApplyAtlas(BlockSet set,int target,string resource,Rect uv){bool ok=ApplyAtlasInternal(set,target,resource,uv,true);if(ok)RefreshLiveVisuals();return ok;}
 public static Face CustomVoxelAtlasFace(Block b){if(b==null)return null;Face f;return customVoxelAtlasFaces.TryGetValue(b.blockID,out f)?f:null;}
 public static string OverrideAtlas(int target){Entry e=store.entries.Find(x=>x.targetId==target);return e==null?"":e.atlasResource;}
 public static Rect OverrideAtlasRect(int target){Entry e=store.entries.Find(x=>x.targetId==target);return e==null?new Rect(0,0,1,1):e.atlasRect;}
 public static bool Apply(BlockSet set,int target,int source){bool ok=ApplyInternal(set,target,source,true);if(ok)RefreshLiveVisuals();return ok;}
 public static bool ApplyCustom(BlockSet set,int target,string path){bool ok=ApplyCustomInternal(set,target,path,true);if(ok)RefreshLiveVisuals();return ok;}
 public static bool Restore(BlockSet set,int target){Original o;if(!originals.TryGetValue(target,out o)||set==null||target<0||target>=set.Count)return false;SetFacesLikeEngine(set,target,set[target],o.faces);customVoxelAtlasFaces.Remove(target);store.entries.RemoveAll(x=>x.targetId==target);Save();RefreshLiveVisuals();return true;}
 public static int OverrideSource(int target){Entry e=store.entries.Find(x=>x.targetId==target);return e==null?-1:e.sourceId;}
 public static string OverrideCustom(int target){Entry e=store.entries.Find(x=>x.targetId==target);return e==null?"":e.customFile;}
 static void Save(){try{EnsureFolders();File.WriteAllText(FilePath,JsonUtility.ToJson(store,true));}catch(Exception e){Debug.LogWarning("Block Designer save: "+e.Message);} }
}
