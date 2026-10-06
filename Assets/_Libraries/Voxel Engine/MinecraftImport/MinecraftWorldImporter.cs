using UnityEngine; using System; using System.IO; using System.Collections; using System.Collections.Generic;
public static class MinecraftWorldImporter {
 public sealed class Result { public bool ok; public string message; public string saveName; public int sourceBlocks,importedBlocks,skippedBlocks; }
 public sealed class Progress { public int doneChunks,totalChunks,sourceBlocks,importedBlocks; public float percent { get { return totalChunks<=0?0f:(float)doneChunks/totalChunks; } } }
 public static IEnumerator ImportAsync(string worldPath,string saveName,int radiusChunks,Action<Progress> onProgress,Action<Result> onDone){ return ImportAsync(worldPath,saveName,radiusChunks,false,onProgress,onDone); }
 public static IEnumerator ImportAsync(string worldPath,string saveName,int radiusChunks,bool fullWorld,Action<Progress> onProgress,Action<Result> onDone){
  var r=new Result(); var p=new Progress();
  if(String.IsNullOrEmpty(worldPath)||!Directory.Exists(worldPath)){r.message="Minecraft-Weltordner nicht gefunden.";onDone(r);yield break;}
  if(!Directory.Exists(Path.Combine(worldPath,"region"))){r.message="Kein region-Ordner gefunden.";onDone(r);yield break;}
  BlockSet blockSet=EnsureBlockSet();if(blockSet==null){r.message="Das Block-Island-BlockSet konnte nicht geladen werden.";onDone(r);yield break;}
  saveName=InfiniteWorldSave.SanitizeName(String.IsNullOrWhiteSpace(saveName)?"Minecraft Import":saveName);radiusChunks=Mathf.Clamp(radiusChunks,1,32);
  int spawnX=0,spawnY=80,spawnZ=0; MinecraftRegionReader.TryReadSpawn(worldPath,out spawnX,out spawnY,out spawnZ);
  int centerChunkX=FloorDiv(spawnX,16),centerChunkZ=FloorDiv(spawnZ,16); int originX=centerChunkX*16,originZ=centerChunkZ*16;
  var chunks=new List<MinecraftRegionReader.ChunkCoord>();
  if(fullWorld) chunks=MinecraftRegionReader.GetExistingChunks(worldPath);
  else for(int cz=centerChunkZ-radiusChunks;cz<=centerChunkZ+radiusChunks;cz++) for(int cx=centerChunkX-radiusChunks;cx<=centerChunkX+radiusChunks;cx++) chunks.Add(new MinecraftRegionReader.ChunkCoord(cx,cz));
  if(chunks.Count==0){r.message="Keine Minecraft-Chunks gefunden.";onDone(r);yield break;}
  // Region-sortierung: alte/große Welten werden jetzt sequenziell gelesen statt ständig zwischen Dateien zu springen.
  chunks.Sort((a,b)=>{int arx=FloorDiv(a.x,32), brx=FloorDiv(b.x,32); if(arx!=brx)return arx.CompareTo(brx); int arz=FloorDiv(a.z,32), brz=FloorDiv(b.z,32); if(arz!=brz)return arz.CompareTo(brz); if(a.z!=b.z)return a.z.CompareTo(b.z); return a.x.CompareTo(b.x);});
  p.totalChunks=chunks.Count; int maxY=int.MinValue;
  string final=InfiniteWorldSave.PathFor(saveName), body=final+".mcbody.tmp", tmp=final+".tmp";
  try{
   // Fast path: Datensätze direkt in eine temporäre Binärdatei streamen. Keine Millionen String-Keys/Dictionary-Einträge mehr im RAM.
   using(var bfs=new FileStream(body,FileMode.Create,FileAccess.Write,FileShare.None,1024*1024,FileOptions.SequentialScan))
   using(var bwBody=new BinaryWriter(bfs)){
    foreach(var cc in chunks){
     foreach(var mb in MinecraftRegionReader.ReadChunkPublic(worldPath,cc.x,cc.z)){
      r.sourceBlocks++; p.sourceBlocks=r.sourceBlocks; Block b=MinecraftBlockMapper.Map(blockSet,mb.name); if(b==null){r.skippedBlocks++;continue;}
      int x=mb.x-originX,z=mb.z-originZ; bwBody.Write(x);bwBody.Write(mb.y);bwBody.Write(z);DataBlock db=new DataBlock(b); db.direction=MinecraftBlockMapper.Direction(mb.state); bwBody.Write(db.blockID);bwBody.Write((byte)db.direction);
      r.importedBlocks++;p.importedBlocks=r.importedBlocks;if(mb.y>maxY)maxY=mb.y;
     }
     p.doneChunks++;if(onProgress!=null)onProgress(p);yield return null;
    }
   }
   if(r.importedBlocks==0){r.message="Keine importierbaren Minecraft-Blöcke im gewählten Bereich gefunden.";onDone(r);yield break;}
   using(var fs=new FileStream(tmp,FileMode.Create,FileAccess.Write,FileShare.None,1024*1024,FileOptions.SequentialScan))using(var bw=new BinaryWriter(fs)){
    bw.Write(0x42495736);bw.Write(saveName);bw.Write((byte)InfiniteWorldSave.WorldType.Minecraft);bw.Write(0);bw.Write(true);bw.Write((float)(spawnX-originX));bw.Write((float)(Math.Max(spawnY,maxY+4)));bw.Write((float)(spawnZ-originZ));bw.Write(0f);bw.Write(0f);bw.Write(0f);bw.Write(1f);bw.Write(r.importedBlocks);
    using(var src=new FileStream(body,FileMode.Open,FileAccess.Read,FileShare.Read,1024*1024,FileOptions.SequentialScan))src.CopyTo(fs,1024*1024);
   }
   if(File.Exists(final))File.Delete(final);File.Move(tmp,final);r.ok=true;r.saveName=saveName;r.message="Import fertig: "+r.importedBlocks.ToString("N0")+" Blöcke. Welt wird geöffnet …";
  } finally { try{if(File.Exists(body))File.Delete(body);}catch{} if(!r.ok)try{if(File.Exists(tmp))File.Delete(tmp);}catch{} }
  onDone(r);
 }
 static int FloorDiv(int a,int b){int q=a/b,r=a%b;return r!=0&&((r<0)!=(b<0))?q-1:q;}
 static BlockSet EnsureBlockSet(){if(BlockSet.instance!=null)return BlockSet.instance;GameObject prefab=Resources.Load<GameObject>("BlockSet");if(prefab==null)return null;GameObject go=UnityEngine.Object.Instantiate(prefab);go.name="BlockSet (Minecraft Import)";UnityEngine.Object.DontDestroyOnLoad(go);BlockSet set=BlockSet.instance!=null?BlockSet.instance:go.GetComponent<BlockSet>();if(set==null)UnityEngine.Object.Destroy(go);return set;}
}
