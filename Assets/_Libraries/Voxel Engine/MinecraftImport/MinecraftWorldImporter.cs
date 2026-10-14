using UnityEngine; using System; using System.IO; using System.Collections; using System.Collections.Generic;
public static class MinecraftWorldImporter {
 public sealed class Result { public bool ok; public string message; public string saveName; public long sourceBlocks,importedBlocks,skippedBlocks; }
 public sealed class Progress { public int doneChunks,totalChunks; public long sourceBlocks,importedBlocks; public float percent { get { return totalChunks<=0?0f:(float)doneChunks/totalChunks; } } }
 public static IEnumerator ImportAsync(string worldPath,string saveName,int radiusChunks,Action<Progress> onProgress,Action<Result> onDone){ return ImportAsync(worldPath,saveName,radiusChunks,false,onProgress,onDone); }
 // World import: converts the Minecraft regions into Block Island native BIR regions next to the save
 // (MinecraftNativeConverter, parallel worker threads). The save file itself only stores world type and
 // player position, so even very large worlds load as fast as the built-in native worlds.
 public static IEnumerator ImportAsync(string worldPath,string saveName,int radiusChunks,bool fullWorld,Action<Progress> onProgress,Action<Result> onDone){
  var r=new Result(); var p=new Progress();
  if(String.IsNullOrEmpty(worldPath)||!Directory.Exists(worldPath)){r.message="Minecraft-Weltordner nicht gefunden.";onDone(r);yield break;}
  if(!Directory.Exists(Path.Combine(worldPath,"region"))){r.message="Kein region-Ordner gefunden.";onDone(r);yield break;}
  saveName=InfiniteWorldSave.SanitizeName(String.IsNullOrWhiteSpace(saveName)?"Minecraft Import":saveName);
  string final=InfiniteWorldSave.PathFor(saveName), tmp=final+".tmp";
  string nativeRoot=final+".native", work=nativeRoot+".tmp", workRegions=Path.Combine(work,"region");
  try{ if(Directory.Exists(work)) Directory.Delete(work,true); }catch{}
  var st=new MinecraftNativeConverter.Status();
  MinecraftNativeConverter.ReadLevel(worldPath,st);
  int radius=fullWorld?0:Mathf.Clamp(radiusChunks,1,64);
  var task=System.Threading.Tasks.Task.Run(()=>MinecraftNativeConverter.Convert(worldPath,workRegions,radius,st));
  while(!st.finished){
   p.totalChunks=(int)Math.Min(int.MaxValue,st.chunksTotal); p.doneChunks=(int)Math.Min(int.MaxValue,st.chunksDone);
   p.sourceBlocks=st.blocks; p.importedBlocks=p.sourceBlocks; if(onProgress!=null)onProgress(p);
   yield return null;
  }
  r.sourceBlocks=r.importedBlocks=st.blocks;
  if(st.error!=null||st.blocks==0){
   r.message=st.error!=null?"Import fehlgeschlagen: "+st.error:"Keine importierbaren Minecraft-Blöcke gefunden.";
   try{ if(Directory.Exists(work)) Directory.Delete(work,true); }catch{}
   onDone(r); yield break;
  }
  try{
   if(Directory.Exists(nativeRoot)) Directory.Delete(nativeRoot,true);
   Directory.Move(work,nativeRoot);
   int surface=st.spawnSurface>=0?st.spawnSurface:Math.Max(0,st.spawnY+st.yShift);
   float py=Math.Max(st.spawnY+st.yShift,surface)+2f;
   using(var fs=new FileStream(tmp,FileMode.Create,FileAccess.Write))using(var bw=new BinaryWriter(fs)){
    bw.Write(0x42495736);bw.Write(saveName);bw.Write((byte)InfiniteWorldSave.WorldType.Minecraft);bw.Write(0);
    bw.Write(true);bw.Write(st.spawnX+.5f);bw.Write(py);bw.Write(st.spawnZ+.5f);bw.Write(0f);bw.Write(0f);bw.Write(0f);bw.Write(1f);
    bw.Write(0);   // no player edits yet
   }
   if(File.Exists(final))File.Delete(final);File.Move(tmp,final);
   File.WriteAllText(Path.Combine(nativeRoot,"import.txt"),
    "source: "+worldPath+"\nDataVersion: "+st.dataVersion+"  yShift: "+st.yShift+"\nregions: "+st.regionsDone+"  chunks: "+st.chunksDone+"  blocks: "+st.blocks+"  skipped details: "+st.skippedDetails+"\n"
    +"stone fallback for: "+string.Join(", ",new List<string>(st.fallbackNames.Keys).ToArray())+"\n");
   r.ok=true;r.saveName=saveName;
   r.message="Import fertig: "+st.blocks.ToString("N0")+" Blöcke aus "+st.chunksDone.ToString("N0")+" Chunks. Welt wird geöffnet …";
  }catch(Exception e){ r.message="Import fehlgeschlagen: "+e.Message; try{if(File.Exists(tmp))File.Delete(tmp);}catch{} }
  onDone(r);
 }
 public static IEnumerator ImportStructureAsync(string filePath,string saveName,Action<Progress> onProgress,Action<Result> onDone){
  var r=new Result();var p=new Progress();if(String.IsNullOrEmpty(filePath)||!File.Exists(filePath)){r.message="Schematic/Litematic-Datei nicht gefunden.";onDone(r);yield break;}BlockSet blockSet=EnsureBlockSet();if(blockSet==null){r.message="Das Block-Island-BlockSet konnte nicht geladen werden.";onDone(r);yield break;}
  MinecraftStructureReader.Structure st;string error;if(!MinecraftStructureReader.TryRead(filePath,out st,out error)){r.message="Import fehlgeschlagen: "+error;onDone(r);yield break;}saveName=InfiniteWorldSave.SanitizeName(String.IsNullOrWhiteSpace(saveName)?Path.GetFileNameWithoutExtension(filePath):saveName);
  int originX=(st.minX+st.maxX)/2,originZ=(st.minZ+st.maxZ)/2,baseY=st.minY;string final=InfiniteWorldSave.PathFor(saveName),body=final+".mcbody.tmp",tmp=final+".tmp";p.totalChunks=Math.Max(1,(st.blocks.Count+4095)/4096);int maxY=0;
  try{using(var bfs=new FileStream(body,FileMode.Create,FileAccess.Write,FileShare.None,1024*1024,FileOptions.SequentialScan))using(var bwBody=new BinaryWriter(bfs)){for(int i=0;i<st.blocks.Count;i++){var mb=st.blocks[i];r.sourceBlocks++;p.sourceBlocks=r.sourceBlocks;Block b=MinecraftBlockMapper.Map(blockSet,mb.name);if(b==null){r.skippedBlocks++;continue;}int x=mb.x-originX,y=mb.y-baseY,z=mb.z-originZ;bwBody.Write(x);bwBody.Write(y);bwBody.Write(z);DataBlock db=new DataBlock(b);db.direction=MinecraftBlockMapper.Direction(mb.state);bwBody.Write(db.blockID);bwBody.Write((byte)db.direction);r.importedBlocks++;p.importedBlocks=r.importedBlocks;if(y>maxY)maxY=y;if((i&4095)==4095){p.doneChunks=Math.Min(p.totalChunks,(i+1)/4096);if(onProgress!=null)onProgress(p);yield return null;}}}
   if(r.importedBlocks==0){r.message="Keine importierbaren Blöcke gefunden.";onDone(r);yield break;}using(var fs=new FileStream(tmp,FileMode.Create,FileAccess.Write,FileShare.None,1024*1024,FileOptions.SequentialScan))using(var bw=new BinaryWriter(fs)){bw.Write(0x42495736);bw.Write(saveName);bw.Write((byte)InfiniteWorldSave.WorldType.Minecraft);bw.Write(0);bw.Write(true);bw.Write(0f);bw.Write((float)(maxY+4));bw.Write(0f);bw.Write(0f);bw.Write(0f);bw.Write(0f);bw.Write(1f);bw.Write((int)r.importedBlocks);/* save format: int32 edit count */using(var src=new FileStream(body,FileMode.Open,FileAccess.Read,FileShare.Read,1024*1024,FileOptions.SequentialScan))src.CopyTo(fs,1024*1024);}if(File.Exists(final))File.Delete(final);File.Move(tmp,final);p.doneChunks=p.totalChunks;if(onProgress!=null)onProgress(p);r.ok=true;r.saveName=saveName;r.message=st.format+" importiert: "+r.importedBlocks.ToString("N0")+" Blöcke. Welt wird geöffnet …";
  }finally{try{if(File.Exists(body))File.Delete(body);}catch{}if(!r.ok)try{if(File.Exists(tmp))File.Delete(tmp);}catch{}}onDone(r);
 }
 static int FloorDiv(int a,int b){int q=a/b,r=a%b;return r!=0&&((r<0)!=(b<0))?q-1:q;}
 static BlockSet EnsureBlockSet(){if(BlockSet.instance!=null)return BlockSet.instance;GameObject prefab=Resources.Load<GameObject>("BlockSet");if(prefab==null)return null;GameObject go=UnityEngine.Object.Instantiate(prefab);go.name="BlockSet (Minecraft Import)";UnityEngine.Object.DontDestroyOnLoad(go);BlockSet set=BlockSet.instance!=null?BlockSet.instance:go.GetComponent<BlockSet>();if(set==null)UnityEngine.Object.Destroy(go);return set;}
}
