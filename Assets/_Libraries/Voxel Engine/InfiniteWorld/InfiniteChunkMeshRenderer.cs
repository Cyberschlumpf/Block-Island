using UnityEngine;
using System.Collections.Generic;

// Renders streamed chunks directly from InfiniteVoxelWorld. It deliberately does not use
// legacy Chunk/ChunkGrid, so signed/unbounded chunk coordinates work in every direction.
public sealed class InfiniteChunkMeshRenderer : MonoBehaviour {
    public InfiniteVoxelWorld world;
    readonly Dictionary<InfiniteChunkKey, GameObject> objects = new Dictionary<InfiniteChunkKey, GameObject>();
    readonly HashSet<InfiniteChunkKey> dirty = new HashSet<InfiniteChunkKey>();
    int diagnosticBuilds=0;
    Transform renderRoot;
    // Stage 6.7.2: geometry isolation. Terrain blocks render TOP faces only.
    // If the giant vertical wall disappears, its source is proven to be terrain side-face generation.
    public bool terrainTopFacesOnly = false;
    // 6.13.11: real-time shadows on dozens of dense imported chunk meshes were a major GPU cost.
    // Keep receiving the scene light/shadows, but do not make every Tanviir chunk a shadow caster.
    public bool tanviirChunkShadows = false;

    void Awake() {
        if(world==null) world=GetComponent<InfiniteVoxelWorld>();
        // Never inherit the legacy Map transform. The old finite map was 512x512 and its
        // transform hierarchy must not scale/offset streamed chunk coordinates.
        GameObject root=GameObject.Find("InfiniteWorldRenderRoot");
        if(root==null) root=new GameObject("InfiniteWorldRenderRoot");
        renderRoot=root.transform;
        renderRoot.SetParent(null,false);
        renderRoot.position=Vector3.zero;
        renderRoot.rotation=Quaternion.identity;
        renderRoot.localScale=Vector3.one;
    }

    public void OnInfiniteChunkNeeded(InfiniteChunkKey key) { Build(key); }
    public void OnInfiniteChunkReleased(InfiniteChunkKey key) {
        GameObject go;
        if(objects.TryGetValue(key,out go)) { objects.Remove(key); if(go!=null) Destroy(go); }
    }

    public void MarkDirtyAtWorld(int x,int y,int z) {
        var k=InfiniteWorldMath.WorldToChunk(x,y,z);
        // 6.14.2 RESTORABLE HOLES: an edit is allowed to create a chunk that had no
        // original Tanviir source geometry. Build it immediately instead of waiting for
        // the streamer to discover a non-empty source section. This makes missing 16x16
        // / 32x32 areas genuine editable Block Island space.
        if(BlockIslandWorldSource.FixedWorldActive && !objects.ContainsKey(k)) Build(k);
        dirty.Add(k);
        var l=InfiniteWorldMath.WorldToLocal(x,y,z);
        if(l.x==0) dirty.Add(new InfiniteChunkKey(k.x-1,k.y,k.z)); if(l.x==Chunk.X_SIZE-1) dirty.Add(new InfiniteChunkKey(k.x+1,k.y,k.z));
        if(l.y==0) dirty.Add(new InfiniteChunkKey(k.x,k.y-1,k.z)); if(l.y==Chunk.Y_SIZE-1) dirty.Add(new InfiniteChunkKey(k.x,k.y+1,k.z));
        if(l.z==0) dirty.Add(new InfiniteChunkKey(k.x,k.y,k.z-1)); if(l.z==Chunk.Z_SIZE-1) dirty.Add(new InfiniteChunkKey(k.x,k.y,k.z+1));
    }

    // 0.6: batch invalidation for large creative edits (TNT). Avoids rebuilding once per voxel.
    // 1.0.16: rebuild only chunks that are already visible after a live Block Designer texture change.
    public void MarkAllVisibleDirty() {
        foreach(var k in objects.Keys) dirty.Add(k);
    }

    public void MarkDirtyChunks(IEnumerable<InfiniteChunkKey> keys) {
        if(keys==null) return;
        foreach(var k in keys) {
            dirty.Add(k);
            dirty.Add(new InfiniteChunkKey(k.x-1,k.y,k.z)); dirty.Add(new InfiniteChunkKey(k.x+1,k.y,k.z));
            dirty.Add(new InfiniteChunkKey(k.x,k.y-1,k.z)); dirty.Add(new InfiniteChunkKey(k.x,k.y+1,k.z));
            dirty.Add(new InfiniteChunkKey(k.x,k.y,k.z-1)); dirty.Add(new InfiniteChunkKey(k.x,k.y,k.z+1));
        }
    }

    void LateUpdate() {
        if(dirty.Count==0) return;
        var list=new List<InfiniteChunkKey>(dirty); dirty.Clear();
        foreach(var k in list) if(objects.ContainsKey(k)) Build(k);
    }

    void Build(InfiniteChunkKey key) {
        if(world==null || BlockSet.instance==null) return;
        MeshBuilder mb=new MeshBuilder();
        List<GameObjectBlockPlacement> gameObjectBlocks=new List<GameObjectBlockPlacement>();
        List<CustomBlockPlacement> customBlocks=new List<CustomBlockPlacement>();
        int ox=key.x*Chunk.X_SIZE, oy=key.y*Chunk.Y_SIZE, oz=key.z*Chunk.Z_SIZE;
        // 6.14.6: sample this 16^3 chunk exactly once. Previously every visible cube
        // performed up to six additional world.GetBlock() calls while meshing. In dense
        // Tanviir architecture that multiplied native-world lookups roughly 5-7x.
        // Interior neighbour tests now hit this local array; only the six chunk borders
        // fall back to world.GetBlock(), preserving edits and cross-chunk face culling.
        DataBlock[] localBlocks=new DataBlock[Chunk.X_SIZE*Chunk.Y_SIZE*Chunk.Z_SIZE];
        // 6.14.13: one bulk source read + sparse edit overlay instead of 32,768
        // InfiniteVoxelWorld.GetBlock() calls for every streamed Tanviir chunk.
        world.FillChunk(key,localBlocks);
        for(int z=0;z<Chunk.Z_SIZE;z++) for(int y=0;y<Chunk.Y_SIZE;y++) for(int x=0;x<Chunk.X_SIZE;x++) {
            DataBlock b=localBlocks[LocalCacheIndex(x,y,z)];
            if(b.IsEmpty()) continue;
            Block block=b.block;
            LocalPosition lp=new LocalPosition((sbyte)x,(sbyte)y,(sbyte)z);
            BlockDirection dir=b.direction;
            CubeBlock cube=block as CubeBlock;
            GroundBlock ground=block as GroundBlock;
            FluidBlock fluid=block as FluidBlock;
            CrossBlock cross=block as CrossBlock;
            MeshBlock meshBlock=block as MeshBlock;
            MinecartRailBlock railBlock=block as MinecartRailBlock;
            FenceBlock fenceBlock=block as FenceBlock;
            GameObjectBlock gameObjectBlock=block as GameObjectBlock;
            CustomVoxelBlock customBlock=block as CustomVoxelBlock;
            BlockIslandLegacySpecialBlock biSpecial=block as BlockIslandLegacySpecialBlock;
            if(customBlock!=null) {
                // Static custom sculptures merge into the chunk. Animated ones get a tiny child mesh
                // so only their visual transform moves; the logical voxel stays fixed for save/collision.
                if(customBlock.animation==CustomVoxelBlock.Motion.None && customBlock.particleFx==CustomVoxelBlock.ParticleFx.None) customBlock.Build(mb,b,lp,0,null);
                else customBlocks.Add(new CustomBlockPlacement(customBlock,b,lp));
            } else if(cube!=null) {
                if(CachedBlock(localBlocks,x,y,z+1,ox,oy,oz).IsAlpha()) CubeBuilder.BuildFace(mb,0,GetFace(cube,dir,CubeSide.Front),lp);
                if(CachedBlock(localBlocks,x,y,z-1,ox,oy,oz).IsAlpha()) CubeBuilder.BuildFace(mb,1,GetFace(cube,dir,CubeSide.Back),lp);
                if(CachedBlock(localBlocks,x+1,y,z,ox,oy,oz).IsAlpha()) CubeBuilder.BuildFace(mb,2,GetFace(cube,dir,CubeSide.Right),lp);
                if(CachedBlock(localBlocks,x-1,y,z,ox,oy,oz).IsAlpha()) CubeBuilder.BuildFace(mb,3,GetFace(cube,dir,CubeSide.Left),lp);
                if(CachedBlock(localBlocks,x,y+1,z,ox,oy,oz).IsAlpha()) CubeBuilder.BuildFace(mb,4,cube.top,lp,dir);
                if(CachedBlock(localBlocks,x,y-1,z,ox,oy,oz).IsAlpha()) CubeBuilder.BuildFace(mb,5,cube.bottom,lp,dir);
            } else if(ground!=null) {
                // ROOT-PROOF MODE: suppress all vertical/bottom faces for GroundBlock.
                // A single voxel can only contribute a horizontal 1x1 top quad here.
                if(CachedBlock(localBlocks,x,y+1,z,ox,oy,oz).IsAlpha())
                    CubeBuilder.BuildFace(mb,4,ground.top,lp,dir);
                if(!terrainTopFacesOnly) {
                    if(CachedBlock(localBlocks,x,y,z+1,ox,oy,oz).IsAlpha()) CubeBuilder.BuildFace(mb,0,ground.side,lp);
                    if(CachedBlock(localBlocks,x,y,z-1,ox,oy,oz).IsAlpha()) CubeBuilder.BuildFace(mb,1,ground.side,lp);
                    if(CachedBlock(localBlocks,x+1,y,z,ox,oy,oz).IsAlpha()) CubeBuilder.BuildFace(mb,2,ground.side,lp);
                    if(CachedBlock(localBlocks,x-1,y,z,ox,oy,oz).IsAlpha()) CubeBuilder.BuildFace(mb,3,ground.side,lp);
                    if(CachedBlock(localBlocks,x,y-1,z,ox,oy,oz).IsAlpha()) CubeBuilder.BuildFace(mb,5,ground.bottom,lp,dir);
                }
            } else if(cross!=null) {
                // NATURBLOCK FIX: CrossBlocks keep their original atlas/material IDs and Cutout/Alpha shader.
                // CrossBuilder does not need a legacy Chunk, so it can be reused safely in the streamed world.
                if(InfiniteWorldSave.CurrentWorldType==InfiniteWorldSave.WorldType.Relativity)
                    CrossBuilder.BuildManualAxis(mb,b,lp);
                else CrossBuilder.Build(mb,b,lp,null);
            } else if(biSpecial!=null) {
                BuildBlockIslandSpecial(mb,biSpecial,lp,ox+x,oy+y,oz+z,dir);
            } else if(fenceBlock!=null) {
                // Stage 6.11.4: build fence arms from the ACTUAL infinite-world neighbours.
                // The old preview mesh always contains X+Z rails and therefore looked like a cross.
                BuildInfiniteFence(mb,fenceBlock,lp,ox+x,oy+y,oz+z);
            } else if(gameObjectBlock!=null) {
                // Fire and a few legacy artifact blocks are prefab-backed GameObjectBlocks.
                // They have no mesh from Block.Build(); remember them and instantiate their prefab
                // below the chunk object so particles/animated flames are visible in Infinite World.
                if(gameObjectBlock.gameObject!=null)
                    gameObjectBlocks.Add(new GameObjectBlockPlacement(gameObjectBlock,lp,dir));
            } else if(railBlock!=null) {
                // Stage 6.10.4 ROOT FIX: the streamed Infinite World renderer previously
                // recognized only Cube/Ground/Cross/Mesh/Fluid block subclasses.  The new
                // MinecartRailBlock therefore fell through to the final `continue`, so its
                // Build() method was never called at all.  Render it through its own builder;
                // it does not depend on a legacy finite Chunk instance.
                railBlock.Build(mb,b,lp,0,null);
            } else if(meshBlock!=null && meshBlock.mesh!=null) {
                // Reuse the legacy MeshBlock data without requiring a fixed ChunkGrid.
                int materialID=meshBlock.face.materialID;
                mb.AddIndices(meshBlock.indices,materialID);
                mb.AddVertices(meshBlock.vertices,lp,dir);
                mb.AddNormals(meshBlock.normals,dir);
                mb.AddTexCoords(meshBlock.uvs,meshBlock.face.rect);
                mb.topology.Add(new BlockTopology(lp,BlockTopologyType.Vertex,meshBlock.vertices.Length));
            } else if(fluid!=null) {
                // Simple infinite-world water cube; legacy animated/special fluid behavior can be layered on later.
                if(CachedBlock(localBlocks,x,y,z+1,ox,oy,oz).block!=fluid) CubeBuilder.BuildFace(mb,0,fluid.face,lp);
                if(CachedBlock(localBlocks,x,y,z-1,ox,oy,oz).block!=fluid) CubeBuilder.BuildFace(mb,1,fluid.face,lp);
                if(CachedBlock(localBlocks,x+1,y,z,ox,oy,oz).block!=fluid) CubeBuilder.BuildFace(mb,2,fluid.face,lp);
                if(CachedBlock(localBlocks,x-1,y,z,ox,oy,oz).block!=fluid) CubeBuilder.BuildFace(mb,3,fluid.face,lp);
                if(CachedBlock(localBlocks,x,y+1,z,ox,oy,oz).block!=fluid) CubeBuilder.BuildFace(mb,4,fluid.face,lp,dir);
                if(CachedBlock(localBlocks,x,y-1,z,ox,oy,oz).block!=fluid) CubeBuilder.BuildFace(mb,5,fluid.face,lp,dir);
            } else {
                // Stage 6.11.3 BLOCKSET AUDIT: legacy special blocks (Fence/Stair/Cactus and
                // any other block with a self-contained Build() mesh) used to disappear because
                // InfiniteChunkMeshRenderer simply skipped every unrecognised Block subclass.
                // Merge the block's own preview mesh into the streamed chunk as a safe fallback.
                MeshBuilder legacyBuilt=null;
                try { legacyBuilt=block.Build(); } catch(System.Exception ex) { Debug.LogWarning("Infinite fallback build failed for "+block.name+" ("+block.GetType().Name+"): "+ex.Message); }
                if(legacyBuilt!=null) MergeBuiltBlock(mb,legacyBuilt,lp,dir);
                else continue;
            }
        }
        GameObject go;
        if(!objects.TryGetValue(key,out go) || go==null) {
            go=new GameObject("InfiniteChunk "+key.ToString(),typeof(MeshFilter),typeof(MeshRenderer));
            go.transform.SetParent(renderRoot,false); objects[key]=go;
        }
        if(renderRoot==null) Awake();
        go.transform.SetParent(renderRoot,false);
        go.transform.localPosition=new Vector3(ox,oy,oz);
        go.transform.localRotation=Quaternion.identity;
        go.transform.localScale=Vector3.one;
        // Recreate prefab-backed block visuals for this chunk. They are children of the chunk,
        // so chunk unload/rebuild cleans them up without touching the legacy finite Map tables.
        Transform oldSpecials=go.transform.Find("InfiniteSpecialBlocks");
        if(oldSpecials!=null) DestroyImmediateSafe(oldSpecials.gameObject);
        GameObject specialsRoot=null;
        if(gameObjectBlocks.Count>0) {
            specialsRoot=new GameObject("InfiniteSpecialBlocks");
            specialsRoot.transform.SetParent(go.transform,false);
            foreach(GameObjectBlockPlacement placement in gameObjectBlocks) {
                GameObject prefab=placement.block.gameObject;
                if(prefab==null) continue;
                Quaternion rot=Quaternion.LookRotation(DirectionVector(placement.direction));
                GameObject instance=(GameObject)Instantiate(prefab);
                instance.name=prefab.name+" (Infinite)";
                instance.transform.SetParent(specialsRoot.transform,false);
                instance.transform.localPosition=new Vector3(placement.pos.x+.5f,placement.pos.y+.5f,placement.pos.z+.5f);
                instance.transform.localRotation=rot;
                // Stage 6.11.5: the bundled Fire prefab uses Unity 4.x legacy particle components.
                // Unity 6 still renders its Light but not those old flame particles, so add a modern visual.
                if(placement.block.name=="Fire" || prefab.name=="Flame") InfiniteFireVisual.Ensure(instance);
            }
        }
        Transform oldCustoms=go.transform.Find("InfiniteAnimatedCustomBlocks");
        if(oldCustoms!=null) DestroyImmediateSafe(oldCustoms.gameObject);
        if(customBlocks.Count>0) {
            GameObject customRoot=new GameObject("InfiniteAnimatedCustomBlocks"); customRoot.transform.SetParent(go.transform,false);
            foreach(CustomBlockPlacement placement in customBlocks) {
                MeshBuilder cbm=new MeshBuilder(); placement.block.Build(cbm,placement.data,LocalPosition.zero,0,null);
                // 0.9.9.5b: animate around the exact voxel centre. The generated custom mesh lives
                // in 0..1 coordinates, so rotating/scaling that mesh object directly pivots around a
                // corner and visually shifts stacked blocks. Keep a fixed cell-centre anchor and put
                // the mesh at -0.5 below it. Only the anchor receives the visual animation.
                GameObject anchor=new GameObject(placement.block.name+" (Animated Anchor)"); anchor.transform.SetParent(customRoot.transform,false);
                anchor.transform.localPosition=new Vector3(placement.pos.x+.5f,placement.pos.y+.5f,placement.pos.z+.5f);
                GameObject cg=new GameObject(placement.block.name+" (Animated Mesh)",typeof(MeshFilter),typeof(MeshRenderer)); cg.transform.SetParent(anchor.transform,false);
                cg.transform.localPosition=-Vector3.one*.5f;
                MeshFilter cmf=cg.GetComponent<MeshFilter>(); Mesh cm=cbm.ToMesh(null); cmf.sharedMesh=cm;
                MeshRenderer cmr=cg.GetComponent<MeshRenderer>(); if(cm!=null)cmr.sharedMaterials=cbm.GetMaterials(BlockSet.instance.GetMaterials()); cmr.receiveShadows=true;
                int fxSeed=ox+oy+oz+placement.pos.x*73856093+placement.pos.y*19349663+placement.pos.z*83492791;
                CustomVoxelAnimator a=anchor.AddComponent<CustomVoxelAnimator>(); a.Setup(placement.block,fxSeed);
                if(placement.block.particleFx!=CustomVoxelBlock.ParticleFx.None) anchor.AddComponent<CustomVoxelParticleFX>().Setup(placement.block,fxSeed);
            }
        }
        var mf=go.GetComponent<MeshFilter>();
        Mesh mesh=mb.ToMesh(mf.sharedMesh); mf.sharedMesh=mesh;
        var mr=go.GetComponent<MeshRenderer>();
        // 2.0.01: Runtime rebuilds must keep casting shadows. Previously Tanviir chunks
        // were switched to ShadowCastingMode.Off every time a placed/removed block rebuilt
        // the chunk. That made newly placed blocks lose their shadow until a scene restart.
        mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows=true;
        if(mesh!=null) mr.sharedMaterials=mb.GetMaterials(BlockSet.instance.GetMaterials());
        go.SetActive(mesh!=null || gameObjectBlocks.Count>0 || customBlocks.Count>0);
        if(diagnosticBuilds<12) {
            diagnosticBuilds++;
            int sx=ox+Chunk.X_SIZE/2, sz=oz+Chunk.Z_SIZE/2;
            int sy=(world!=null && world.generator!=null) ? world.generator.SurfaceY(sx,sz) : oy;
            DataBlock rb=world.GetBlock(sx,sy,sz);
            string rn=rb.IsEmpty() ? "EMPTY" : (rb.block!=null ? rb.block.name : "NULL-BLOCK");
            Debug.Log("INFINITE RENDERPATH chunk="+key+
                " mesh="+(mesh!=null)+" verts="+(mesh!=null?mesh.vertexCount:0)+
                " sample=("+sx+","+sy+","+sz+") block="+rn+" id="+rb.blockID);
        }
    }

    static int LocalCacheIndex(int x,int y,int z) { return x + Chunk.X_SIZE*(y + Chunk.Y_SIZE*z); }
    DataBlock CachedBlock(DataBlock[] cache,int x,int y,int z,int ox,int oy,int oz) {
        if((uint)x<(uint)Chunk.X_SIZE && (uint)y<(uint)Chunk.Y_SIZE && (uint)z<(uint)Chunk.Z_SIZE)
            return cache[LocalCacheIndex(x,y,z)];
        return world.GetBlock(ox+x,oy+y,oz+z);
    }



    struct CustomBlockPlacement { public CustomVoxelBlock block; public DataBlock data; public LocalPosition pos; public CustomBlockPlacement(CustomVoxelBlock b,DataBlock d,LocalPosition p){block=b;data=d;pos=p;} }

    struct GameObjectBlockPlacement {
        public GameObjectBlock block; public LocalPosition pos; public BlockDirection direction;
        public GameObjectBlockPlacement(GameObjectBlock b,LocalPosition p,BlockDirection d) { block=b; pos=p; direction=d; }
    }

    void BuildInfiniteFence(MeshBuilder mb,FenceBlock fence,LocalPosition pos,int wx,int wy,int wz) {
        int materialID=fence.face.materialID;
        bool left=FenceConnects(world.GetBlock(wx-1,wy,wz).block);
        bool right=FenceConnects(world.GetBlock(wx+1,wy,wz).block);
        bool back=FenceConnects(world.GetBlock(wx,wy,wz-1).block);
        bool front=FenceConnects(world.GetBlock(wx,wy,wz+1).block);
        mb.AddBox(FenceBlock.box,fence.face.rect,pos,materialID);
        if(left || right) {
            Box a=FenceBlock.xBox1, b=FenceBlock.xBox2;
            if(!left) { a.min.x=.5f; b.min.x=.5f; }
            if(!right) { a.max.x=.5f; b.max.x=.5f; }
            mb.AddBox(a,fence.face.rect,pos,materialID); mb.AddBox(b,fence.face.rect,pos,materialID);
        }
        if(back || front) {
            Box a=FenceBlock.zBox1, b=FenceBlock.zBox2;
            if(!back) { a.min.z=.5f; b.min.z=.5f; }
            if(!front) { a.max.z=.5f; b.max.z=.5f; }
            mb.AddBox(a,fence.face.rect,pos,materialID); mb.AddBox(b,fence.face.rect,pos,materialID);
        }
    }

    void BuildBlockIslandSpecial(MeshBuilder mb, BlockIslandLegacySpecialBlock b, LocalPosition pos, int wx,int wy,int wz, BlockDirection dir) {
        Face f=b.face; if(f==null) return;
        if(b.kind==BlockIslandLegacySpecialBlock.LegacyKind.Ladder) {
            // Thin wall-mounted ladder. Rotate its plane according to placement direction.
            bool alongX=(dir==BlockDirection.LEFT || dir==BlockDirection.RIGHT);
            float plane=(dir==BlockDirection.BACKWARD || dir==BlockDirection.LEFT) ? .08f : .92f;
            if(!alongX) {
                mb.AddBox(Box.CenterSize(new Vector3(.20f,.5f,plane),new Vector3(.10f,1f,.08f)),f.rect,pos,f.materialID);
                mb.AddBox(Box.CenterSize(new Vector3(.80f,.5f,plane),new Vector3(.10f,1f,.08f)),f.rect,pos,f.materialID);
                float[] ys={.16f,.38f,.62f,.84f}; foreach(float yy in ys) mb.AddBox(Box.CenterSize(new Vector3(.5f,yy,plane),new Vector3(.68f,.08f,.10f)),f.rect,pos,f.materialID);
            } else {
                mb.AddBox(Box.CenterSize(new Vector3(plane,.5f,.20f),new Vector3(.08f,1f,.10f)),f.rect,pos,f.materialID);
                mb.AddBox(Box.CenterSize(new Vector3(plane,.5f,.80f),new Vector3(.08f,1f,.10f)),f.rect,pos,f.materialID);
                float[] ys={.16f,.38f,.62f,.84f}; foreach(float yy in ys) mb.AddBox(Box.CenterSize(new Vector3(plane,yy,.5f),new Vector3(.10f,.08f,.68f)),f.rect,pos,f.materialID);
            }
            return;
        }
        bool l=LegacyPanelConnects(world.GetBlock(wx-1,wy,wz).block,b.kind);
        bool r=LegacyPanelConnects(world.GetBlock(wx+1,wy,wz).block,b.kind);
        bool bk=LegacyPanelConnects(world.GetBlock(wx,wy,wz-1).block,b.kind);
        bool fr=LegacyPanelConnects(world.GetBlock(wx,wy,wz+1).block,b.kind);
        float post=b.kind==BlockIslandLegacySpecialBlock.LegacyKind.GlassPanel ? .16f : .08f;
        float thick=b.kind==BlockIslandLegacySpecialBlock.LegacyKind.GlassPanel ? .08f : .035f;
        mb.AddBox(Box.CenterSize(new Vector3(.5f,.5f,.5f),new Vector3(post,1f,post)),f.rect,pos,f.materialID);
        if(l||r) {
            float min=l?0f:.5f, max=r?1f:.5f;
            if(max>min) mb.AddBox(Box.MinMax(new Vector3(min,.04f,.5f-thick*.5f),new Vector3(max,.96f,.5f+thick*.5f)),f.rect,pos,f.materialID);
        }
        if(bk||fr) {
            float min=bk?0f:.5f, max=fr?1f:.5f;
            if(max>min) mb.AddBox(Box.MinMax(new Vector3(.5f-thick*.5f,.04f,min),new Vector3(.5f+thick*.5f,.96f,max)),f.rect,pos,f.materialID);
        }
    }

    static bool LegacyPanelConnects(Block other, BlockIslandLegacySpecialBlock.LegacyKind kind) {
        BlockIslandLegacySpecialBlock s=other as BlockIslandLegacySpecialBlock;
        return (s!=null && s.kind==kind) || other is GroundBlock || other is CubeBlock;
    }

    static bool FenceConnects(Block b) { return b is FenceBlock || b is GroundBlock || b is CubeBlock; }
    static Vector3 DirectionVector(BlockDirection d) {
        if(d==BlockDirection.LEFT) return Vector3.left; if(d==BlockDirection.RIGHT) return Vector3.right;
        if(d==BlockDirection.BACKWARD) return Vector3.back; return Vector3.forward;
    }
    static void DestroyImmediateSafe(GameObject go) {
        if(go==null) return;
        if(Application.isPlaying) Destroy(go); else DestroyImmediate(go);
    }

    static void MergeBuiltBlock(MeshBuilder target, MeshBuilder source, LocalPosition pos, BlockDirection dir) {
        if(source==null || source.vertices.Count==0) return;
        int baseVertex=target.vertices.Count;
        foreach(Vector3 v in source.vertices)
            target.vertices.Add(BlockDirectionUtils.TransformBlockVertex(v,dir)+new Vector3(pos.x,pos.y,pos.z));
        foreach(Vector3 n in source.normals) target.normals.Add(BlockDirectionUtils.TransformVector(n,dir));
        target.texCoords.AddRange(source.texCoords);
        int materialCount=Mathf.Min(target.indices.Length,source.indices.Length);
        for(int m=0;m<materialCount;m++) {
            var src=source.indices[m]; if(src==null || src.Count==0) continue;
            var dst=target.indices[m]; if(dst==null) target.indices[m]=dst=new List<int>(src.Count);
            foreach(int i in src) dst.Add(baseVertex+i);
        }
    }

    static Face GetFace(CubeBlock b,BlockDirection d,CubeSide s) {
        if(s==CubeSide.Top) return b.top; if(s==CubeSide.Bottom) return b.bottom;
        // Front,Back,Right,Left are enum-specific; explicit mapping avoids relying on enum integer order.
        if(d==BlockDirection.FORWARD) { if(s==CubeSide.Front)return b.front;if(s==CubeSide.Back)return b.back;if(s==CubeSide.Right)return b.right;return b.left; }
        if(d==BlockDirection.RIGHT) { if(s==CubeSide.Front)return b.left;if(s==CubeSide.Back)return b.right;if(s==CubeSide.Right)return b.front;return b.back; }
        if(d==BlockDirection.BACKWARD) { if(s==CubeSide.Front)return b.back;if(s==CubeSide.Back)return b.front;if(s==CubeSide.Right)return b.left;return b.right; }
        if(s==CubeSide.Front)return b.right;if(s==CubeSide.Back)return b.left;if(s==CubeSide.Right)return b.back;return b.front;
    }
}
