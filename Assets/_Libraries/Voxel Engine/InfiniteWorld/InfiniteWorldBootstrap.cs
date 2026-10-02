using UnityEngine;
using UnityEngine.SceneManagement;
public sealed class InfiniteWorldBootstrap : MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void AutoInstall() { InstallNow(); }
 public static InfiniteVoxelWorld InstallNow() {
  if(SceneManager.GetActiveScene().name!="Game") return null;
  // Stage 6.6.6: never bind the infinite world to a Map that survived from the Map Generator
  // through DontDestroyOnLoad. Select the Map that actually belongs to the active Game scene.
  Map map=null;
  var allMaps=Object.FindObjectsByType<Map>(FindObjectsInactive.Include);
  Scene activeScene=SceneManager.GetActiveScene();
  foreach(var candidate in allMaps) {
   if(candidate!=null && candidate.gameObject.scene==activeScene) { map=candidate; break; }
  }
  if(map==null){Debug.LogError("INFINITE WORLD: Game scene loaded but no Game-scene Map was found.");return null;}

  // Any additional Map is a legacy/persistent map and must not participate in rendering or block access.
  int removedForeignMaps=0;
  foreach(var foreign in allMaps) {
   if(foreign==null || foreign==map) continue;
   removedForeignMaps++;
   foreach(var cr in foreign.GetComponentsInChildren<ChunkRenderer>(true)) { if(cr!=null) Object.Destroy(cr.gameObject); }
   for(int i=0;i<foreign.grid.chunks.Length;i++) foreign.grid.chunks[i]=null;
   foreign.enabled=false;
   Object.Destroy(foreign);
  }
  if(!map.gameObject.activeSelf) map.gameObject.SetActive(true);
  // Stage 6.8.4: hard-disable legacy terrain generators as well. They may exist in a persistent root,
  // but they are never allowed to write the Game world.
  foreach(var island in Object.FindObjectsByType<IslandGenerator>(FindObjectsInactive.Include)) if(island!=null) island.enabled=false;
  foreach(var flat in Object.FindObjectsByType<FlatMapGenerator>(FindObjectsInactive.Include)) if(flat!=null) flat.enabled=false;

  // Stage 6.5.8: hard-disable every legacy finite-map builder before installing the infinite renderer.
  foreach(var legacy in Object.FindObjectsByType<RuntimeBuilder>(FindObjectsInactive.Include)) {
   if(legacy==null) continue;
   legacy.StopAllCoroutines();
   legacy.enabled=false;
  }

  // Destroy already-created legacy ChunkRenderer objects.  They are the visual output of
  // the old finite 512x64x512 Map pipeline and must never coexist with InfiniteChunkMeshRenderer.
  int removedLegacyRenderers=0;
  foreach(var r in Object.FindObjectsByType<ChunkRenderer>(FindObjectsInactive.Include)) {
   if(r==null) continue;
   removedLegacyRenderers++;
   r.enabled=false;
   Object.Destroy(r.gameObject);
  }

  var go=map.gameObject; var gen=go.GetComponent<InfiniteTerrainGenerator>(); if(gen==null) gen=go.AddComponent<InfiniteTerrainGenerator>();
  // Stage 6.8.0: the legacy Map Generator / IslandGenerator is not a world source anymore.
  // Only a seed crosses the Main Menu -> Game boundary.
  if(InfiniteWorldLaunchConfig.pending){
   gen.seed=InfiniteWorldLaunchConfig.seed;
   // Stage 6.8.3: preview dimensions are UI metadata only; never feed finite-map size into world terrain.
   gen.previewNoiseScale=InfiniteWorldLaunchConfig.noiseScale;
   gen.previewNoiseOffset=InfiniteWorldLaunchConfig.noiseOffset;
   gen.previewCurve=InfiniteWorldLaunchConfig.previewCurve;
   gen.skyIslands=InfiniteWorldLaunchConfig.skyIslands;
   gen.lightGarden=(InfiniteWorldSave.CurrentWorldType==InfiniteWorldSave.WorldType.LightGarden);
   InfiniteWorldLaunchConfig.pending=false;
  }
  gen.lightGarden=(InfiniteWorldSave.CurrentWorldType==InfiniteWorldSave.WorldType.LightGarden);
  gen.worldCoordinateProof=false;
  gen.hardCoordinatePlane=false;
  gen.generateTrees=true;
  var world=go.GetComponent<InfiniteVoxelWorld>();if(world==null)world=go.AddComponent<InfiniteVoxelWorld>();world.generator=gen;
  var mesh=go.GetComponent<InfiniteChunkMeshRenderer>();if(mesh==null)mesh=go.AddComponent<InfiniteChunkMeshRenderer>();mesh.world=world;
  var stream=go.GetComponent<InfiniteChunkStreamer>();if(stream==null)stream=go.AddComponent<InfiniteChunkStreamer>();if(Camera.main!=null)stream.target=Camera.main.transform;
  // Stage 6.13.1: normalize extreme ultrawide perspective (e.g. 32:9) while leaving 16:9 unchanged.
  if(Camera.main!=null && Camera.main.GetComponent<UltrawideCameraNormalizer>()==null) Camera.main.gameObject.AddComponent<UltrawideCameraNormalizer>();
  if(go.GetComponent<InfiniteWorldVisualSetup>()==null)go.AddComponent<InfiniteWorldVisualSetup>();if(go.GetComponent<InfiniteWorldDebugHUD>()==null)go.AddComponent<InfiniteWorldDebugHUD>();if(go.GetComponent<InfiniteOnlyRenderGuard>()==null)go.AddComponent<InfiniteOnlyRenderGuard>();
  if(go.GetComponent<InfiniteDayNightCycle>()==null) go.AddComponent<InfiniteDayNightCycle>();
  if(go.GetComponent<InfiniteCreativeWeather>()==null) go.AddComponent<InfiniteCreativeWeather>();
  if(go.GetComponent<InfiniteMusicPlayer>()==null) go.AddComponent<InfiniteMusicPlayer>();
  if(go.GetComponent<InfiniteWorldAutosave>()==null) go.AddComponent<InfiniteWorldAutosave>();
  if(go.GetComponent<VoxelBoxScreenshot>()==null) go.AddComponent<VoxelBoxScreenshot>();
  var wildlife=go.GetComponent<InfiniteBirdFlock>(); if(wildlife==null) wildlife=go.AddComponent<InfiniteBirdFlock>(); if(Camera.main!=null) wildlife.target=Camera.main.transform;
  var rabbits=go.GetComponents<InfiniteHoppingRabbit>();
  while(rabbits.Length<2){ go.AddComponent<InfiniteHoppingRabbit>(); rabbits=go.GetComponents<InfiniteHoppingRabbit>(); }
  foreach(var rabbit in rabbits){ rabbit.world=world; if(Camera.main!=null) rabbit.target=Camera.main.transform; }
  var fish=go.GetComponent<InfiniteVoxelFish>(); if(fish==null) fish=go.AddComponent<InfiniteVoxelFish>(); fish.world=world; if(Camera.main!=null) fish.target=Camera.main.transform;
  map.infiniteWorld=world;map.infiniteRenderer=mesh;

  // Stage 6.11.0: load the selected named world after the infinite pipeline exists.
  if(InfiniteWorldSave.LoadRequested && !string.IsNullOrEmpty(InfiniteWorldSave.CurrentWorldName)) {
   // A first Tanviir launch has no save file yet. Keep the reserved world name so the
   // first manual/autosave creates it; subsequent launches restore all repairs/builds.
   InfiniteWorldSave.Load(world,InfiniteWorldSave.CurrentWorldName);
   InfiniteWorldSave.LoadRequested=false;
  }

  // Clear finite-map chunk DATA as well.  From this point Map.GetBlock/SetBlock route to
  // InfiniteVoxelWorld, so the old grid is neither needed nor allowed to be rendered later.
  int clearedLegacyChunks=0;
  for(int i=0;i<map.grid.chunks.Length;i++) {
   Chunk c=map.grid.chunks[i];
   if(c==null) continue;
   clearedLegacyChunks++;
   map.grid.chunks[i]=null;
  }

  Debug.LogWarning("STAGE 6.8.8 NARROW SHORELINE BLOCK IDS: Stone="+(BlockSet.instance!=null&&BlockSet.instance.FindBlock("Stone")!=null?BlockSet.instance.FindBlock("Stone").blockID.ToString():"missing")+" Rock="+(BlockSet.instance!=null&&BlockSet.instance.FindBlock("Rock")!=null?BlockSet.instance.FindBlock("Rock").blockID.ToString():"missing")+" Sand="+(BlockSet.instance!=null&&BlockSet.instance.FindBlock("Sand")!=null?BlockSet.instance.FindBlock("Sand").blockID.ToString():"missing"));
  Debug.LogWarning("=== STAGE 6.8.7 BEACHES + VARIED TREES ACTIVE === seed="+gen.seed+
   " | removed foreign Maps="+removedForeignMaps+
   " | removed legacy renderers="+removedLegacyRenderers+
   " | cleared legacy chunks="+clearedLegacyChunks);
  return world;
 }
}
