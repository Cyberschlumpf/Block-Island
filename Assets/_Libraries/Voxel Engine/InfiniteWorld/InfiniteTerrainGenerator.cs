using UnityEngine;
using System.Collections.Generic;

// Stage 6.9.7 - large-island mountain archipelago: dense varied islands, highlands and mountains.
// Keeps the proven 6.9.5 coast/water thresholds while expanding only island scale/interior relief.
// Preserves 6.8.9 column cache/performance and 6.8.4 legacy guard.
// Stage 6.8.0 - unified infinite world generator. IslandGenerator/finite 512x512 maps are not used.
// Root finding from 6.6.9: the orange "sand wall" was the SIDE face of the Grass GroundBlock
// on a height-field cliff. The proof build also introduced a rectangular hard cutoff.
// This version removes that cutoff and restores a smooth, layered island volume.
public class InfiniteTerrainGenerator : MonoBehaviour {
    // Stage 6.7.3 diagnostic: encode GLOBAL chunk coordinates directly into the surface material.
    // Even chunk parity = Grass, odd chunk parity = Dirt. A correct world path must show a 32x32 checkerboard.
    public bool worldCoordinateProof = false;
    public bool hardCoordinatePlane = false;
    public int seed=1337;
    public int seaLevel=25;
    public int dirtDepth=4;
    public int startIslandEdgeOceanBand=24;
    public bool generateTrees=true;
    // 0.9.9.8a SKY ISLANDS: true 3D voxel volumes, not elevated heightmaps.
    public bool skyIslands=false;
    // 0.9.9.9b LICHTGARTEN EXPANSION: enlarged floating garden, connected water system, denser landmark layout.
    public bool lightGarden=false;
    public int skyBaseHeight=92;
    public int skyCellSize=170;
    public int skyMinRadius=46;
    public int skyMaxRadius=92;
    public int startIslandSizeX=0;
    public int startIslandSizeZ=0;
    public float previewNoiseScale=1f/20f;       // retained for inspector compatibility only
    public Vector2 previewNoiseOffset=Vector2.zero;
    public AnimationCurve previewCurve;          // retained for inspector compatibility only
    public int islandCellSize=460;
    [Range(0,100)] public int islandChancePercent=92;
    public int minIslandRadius=55;
    public int maxIslandRadius=300;
    public int spawnIslandRadius=220;
    public int beachWidth=8;
    public float beachLandMask=0.10f;
    public int maxBeachHeight=2;
    public float mountainStrength=24f;
    public int startOceanClearance=230;

    Block water,sand,dirt,grass,rock,wood,leaf;
    Block lgStone,lgDark,lgGold,lgGlass,lgRed,lgYellow,lgPink;
    Block[] flora=new Block[4];
    LegacyInfiniteTrees variedTrees;

    // Stage 6.8.9: X/Z column cache. Mesh generation asks for the same columns many times
    // (solid voxel + six neighbour checks). Terrain/noise/beach classification must therefore
    // be computed once per world column, not once per voxel.
    struct ColumnInfo { public float land; public float relief; public int top; public bool beach; public bool vegetated; }
    readonly Dictionary<long,ColumnInfo> columnCache = new Dictionary<long,ColumnInfo>(32768);
    const int MaxColumnCache = 262144;
    static readonly int[] BeachDX={ 1,-1,0,0,1,1,-1,-1 };
    static readonly int[] BeachDZ={ 0,0,1,-1,1,-1,1,-1 };
    static long ColumnKey(int x,int z){ unchecked { return ((long)x<<32) ^ (uint)z; } }

    void Awake(){ ResolveBlocks(); variedTrees=new LegacyInfiniteTrees(this); }
    void ResolveBlocks(){
        var bs=BlockSet.instance; if(bs==null)return;
        water=bs.FindBlock("Water");
        sand=FindPreferred(bs,"Sand", typeof(CubeBlock));
        // IMPORTANT: "Rock" is a legacy Ground block whose atlas appearance is the yellow/orange
        // material that looked like the giant sand cuboid. Use the Natural Stone block explicitly.
        // Grass/Dirt names exist more than once in this old project, so resolve by preferred type/order.
        // Use the Natural cube blocks for the coordinate proof. This avoids the legacy GroundBlock
        // side/wings renderer and makes the diagnostic material unambiguous.
        grass=FindPreferred(bs,"Grass", typeof(CubeBlock));
        dirt=FindPreferred(bs,"Dirt", typeof(CubeBlock));
        rock=bs.FindBlock("Stone");
        if(rock==null) rock=bs.FindBlock("Rock");
        wood=bs.FindBlock("Light Wood"); if(wood==null)wood=bs.FindBlock("Birch Wood");
        leaf=bs.FindBlock("Green Leaf"); if(leaf==null)leaf=bs.FindBlock("Birch Leaf");
        flora[0]=bs.FindBlock("Grass 1"); flora[1]=bs.FindBlock("Grass 2");
        flora[2]=bs.FindBlock("Flower 1"); flora[3]=bs.FindBlock("Flower 2");
        lgStone=bs.FindBlock("Tanviir Pale Stone"); if(lgStone==null)lgStone=rock;
        lgDark=bs.FindBlock("Tanviir Dark Stone"); if(lgDark==null)lgDark=rock;
        lgGold=bs.FindBlock("Tanviir Gold Trim"); if(lgGold==null)lgGold=lgStone;
        lgGlass=bs.FindBlock("Glass"); if(lgGlass==null)lgGlass=lgStone;
        lgRed=bs.FindBlock("Red Leaf"); lgYellow=bs.FindBlock("Yellow Leaf"); lgPink=bs.FindBlock("Pink Leaf");
    }


    Block FindPreferred(BlockSet bs,string name,System.Type type){
        Block fallback=null;
        foreach(Block b in bs.GetBlocks()){
            if(b==null || !string.Equals(b.name,name,System.StringComparison.OrdinalIgnoreCase)) continue;
            if(fallback==null) fallback=b;
            if(type.IsInstanceOfType(b)) return b;
        }
        return fallback;
    }

    // Kept only so the existing bootstrap remains compatible. The dangerous legacy voxel
    // snapshot is deliberately NEVER imported into the new procedural world.
    public void SetCapturedStartBlocks(int[] ids,byte[] directions,int width,int height,int depth){
        Debug.LogWarning("STAGE 6.7.0: legacy finite snapshot ignored; smooth procedural coastline active.");
    }

    int Hash(int x,int z,int salt=0){
        unchecked { int h=x*73856093 ^ z*19349663 ^ seed*83492791 ^ salt*1274126177;
        h^=h>>13; h*=1274126177; return h^(h>>16); }
    }
    float H01(int x,int z,int salt){ unchecked { return ((uint)Hash(x,z,salt)&0x00ffffff)/16777215f; } }

    float StartMask(int x,int z){
        // Smooth finite island mask. IMPORTANT: no rectangular X/Z hard cutoff.
        // The ellipse itself reaches zero continuously, so the coast cannot become a 512x512 wall.
        const float rx=190f, rz=165f;
        float nx=x/rx,nz=z/rz;
        float d=Mathf.Sqrt(nx*nx+nz*nz);
        float n1=Mathf.PerlinNoise((x+seed)*0.009f,(z-seed)*0.009f)-0.5f;
        float n2=Mathf.PerlinNoise((x-seed)*0.021f,(z+seed)*0.021f)-0.5f;
        float warped=d-n1*0.20f-n2*0.08f;
        return 1f-Mathf.SmoothStep(0.58f,1.08f,warped);
    }

    float OuterMask(int x,int z,out float relief){
        relief=0f;
        int cx=InfiniteWorldMath.FloorDiv(x,islandCellSize);
        int cz=InfiniteWorldMath.FloorDiv(z,islandCellSize);
        float best=0f;
        for(int dz=-2;dz<=2;dz++)for(int dx=-2;dx<=2;dx++){
            int gx=cx+dx,gz=cz+dz;
            bool spawnCell=(gx==0 && gz==0);
            if(!spawnCell && H01(gx,gz,1)*100f>=islandChancePercent)continue;
            float centerX=spawnCell ? 0f : (gx+0.18f+H01(gx,gz,2)*0.64f)*islandCellSize;
            float centerZ=spawnCell ? 0f : (gz+0.18f+H01(gx,gz,3)*0.64f)*islandCellSize;
            float sizeRoll=H01(gx,gz,4);
            // Stage 6.9.7: broad size spectrum. Medium/large islands are common, tiny islands
            // still occur, and rare landmasses reach roughly 512-600 voxels across.
            float shapedSize=Mathf.Pow(sizeRoll,0.72f);
            float radius=spawnCell ? spawnIslandRadius : Mathf.Lerp(minIslandRadius,maxIslandRadius,shapedSize);
            float aspect=Mathf.Lerp(0.62f,1.55f,H01(gx,gz,5));
            float rx=(x-centerX)/radius,rz=(z-centerZ)/(radius*aspect);
            float d=Mathf.Sqrt(rx*rx+rz*rz);
            float rough=Mathf.Lerp(0.18f,0.38f,H01(gx,gz,6));
            float coast=(Mathf.PerlinNoise((x+gx*37)*0.018f,(z-gz*41)*0.018f)-0.5f)*rough;
            float coast2=(Mathf.PerlinNoise((x-gz*19)*0.041f,(z+gx*23)*0.041f)-0.5f)*Mathf.Lerp(0.06f,0.14f,H01(gx,gz,7));
            d-=coast+coast2;
            if(d>=1f)continue;
            float m=1f-Mathf.SmoothStep(0.48f,1f,d);
            if(m>best){best=m;relief=Mathf.PerlinNoise((x+seed)*0.017f,(z-seed)*0.017f);}
        }
        return Mathf.Clamp01(best);
    }

    float LandMask(int x,int z,out float relief){
        // Stage 6.8.3 ROOT FIX: there is no special finite start-island footprint anymore.
        // The old StartMask was a second terrain system centred at world origin and visually
        // recreated the old finite-map slab. Every land mass, including the spawn island, now
        // comes from the same signed world-coordinate archipelago function.
        return OuterMask(x,z,out relief);
    }

    int ComputeSurfaceY(int x,int z,float m,float relief){
        // Stage 6.9.5: restore the ORIGINAL IslandGenerator vertical model.
        // Original finite generator used scale=50, WATER_LEVEL=.50, SAND=.52,
        // DIRT=.83 and ROCK=.90.  The archipelago mask now replaces only the
        // old finite-map footprint; it does not invent a separate beach slope.
        if(m<=0f) return seaLevel-8;

        const float originalScale=50f;

        // Build a deterministic multi-octave height signal, then let the island
        // mask fade that signal naturally through the original waterline.
        float n1=Mathf.PerlinNoise((x+seed)*0.010f,(z-seed)*0.010f);
        float n2=Mathf.PerlinNoise((x-seed*2)*0.021f,(z+seed)*0.021f);
        float n3=Mathf.PerlinNoise((x+seed*3)*0.045f,(z-seed*3)*0.045f);
        float terrainNoise=n1*0.62f+n2*0.27f+n3*0.11f;

        // Outer mask is 0 at ocean and 1 toward an island interior.  This maps
        // the coast through the same normalized thresholds as the original map:
        // water .50, sand .52, dirt .83, rock .90.
        float normalized=0.44f + m*(0.40f + (terrainNoise-0.5f)*0.24f);
        normalized=Mathf.Clamp01(normalized);

        // If the original preview curve was transferred, use it gently for the
        // interior character but never let it recreate a finite 512x512 slab.
        if(previewCurve!=null && previewCurve.length>1 && m>0.20f){
            float curved=Mathf.Clamp01(previewCurve.Evaluate(normalized));
            normalized=Mathf.Lerp(normalized,curved,Mathf.SmoothStep(0.20f,0.75f,m)*0.35f);
        }
        float baseY=normalized*originalScale;

        // Stage 6.9.7: preserve the original coast exactly, then add relief only well inland.
        // This produces broad hills, valleys and mountain cores without lifting the beach.
        float inland=Mathf.SmoothStep(0.34f,0.88f,m);
        float broad=Mathf.PerlinNoise((x+seed*17)*0.0036f,(z-seed*13)*0.0036f);
        float ridges=Mathf.PerlinNoise((x-seed*5)*0.0075f,(z+seed*9)*0.0075f);
        float mountainGate=Mathf.SmoothStep(0.52f,0.84f,broad) * Mathf.SmoothStep(0.42f,0.78f,m);
        float hill=(relief-0.5f)*12f*inland;
        float mountain=mountainGate*(8f + ridges*26f);
        return Mathf.FloorToInt(baseY + hill + mountain);
    }


    // --- LICHTGARTEN fixed sky world -------------------------------------------------
    // Geometry-first interpretation of the user's HDRP garden: 195 x 395 voxel footprint,
    // shallow hills, connected lakes, temples/statues, central Singularitaet and Key of Time.
    float LGMask(int x,int z){
        float nx=x/195f,nz=z/395f; float d=Mathf.Sqrt(nx*nx+nz*nz);
        float edge=(Mathf.PerlinNoise((x+311)*.018f,(z-127)*.018f)-.5f)*.10f;
        return Mathf.Clamp01(1f-Mathf.SmoothStep(.84f,1.02f,d-edge));
    }
    int LGGroundY(int x,int z){
        float broad=(Mathf.PerlinNoise((x+77)*.012f,(z-41)*.012f)-.5f)*7f;
        float fine=(Mathf.PerlinNoise((x-19)*.035f,(z+93)*.035f)-.5f)*2.5f;
        return 96+Mathf.RoundToInt(broad+fine);
    }
    bool LGCircle(int x,int z,int cx,int cz,float r){float dx=x-cx,dz=z-cz;return dx*dx+dz*dz<=r*r;}
    bool LGRing(int x,int z,int cx,int cz,float r,float w){float d=Mathf.Sqrt((x-cx)*(x-cx)+(z-cz)*(z-cz));return Mathf.Abs(d-r)<=w;}
    bool LGPath(int x,int z){
        // broad curved-ish promenade linking the two ritual centres and lakes
        float center=10f*Mathf.Sin(z*.018f); return Mathf.Abs(x-center)<4.2f && z>-315&&z<315;
    }
    bool LGLake(int x,int z){
        // 0.9.9.9b: connected lake/river system inspired by the reference garden.  The broad
        // basins remain organic, while a winding channel joins north, centre and south water.
        bool basins=LGCircle(x,z,-72,95,48)||LGCircle(x,z,62,128,43)||LGCircle(x,z,58,-92,38)||LGCircle(x,z,-88,-130,34);
        float riverX=22f*Mathf.Sin((z+35)*.0105f)-8f*Mathf.Sin(z*.026f);
        bool river=Mathf.Abs(x-riverX)<9f && z>-155 && z<155;
        bool branchN=Mathf.Abs(x+48f-(z-40)*.22f)<7f && z>35&&z<112;
        bool branchS=Mathf.Abs(x-42f-(z+55)*.18f)<7f && z>-135&&z<-42;
        return basins||river||branchN||branchS;
    }
    DataBlock LGBuildings(int x,int y,int z,int ground){
        // SINGULARITAET: circular dais, lotus-like stepped centre and crystal pillars.
        if(LGCircle(x,z,0,82,40)){
            if(y==ground+1 && LGCircle(x,z,0,82,39)) return new DataBlock(lgDark);
            if(y==ground+2 && LGCircle(x,z,0,82,30)) return new DataBlock(lgGold);
            if(y>=ground+3&&y<=ground+5 && LGCircle(x,z,0,82,15-(y-ground-3)*2)) return new DataBlock(lgStone);
            if(LGRing(x,z,0,82,33,1.4f) && y>=ground+2&&y<=ground+4) return new DataBlock(lgGold);
            // eight luminous/crystal pylons around the circle
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4f;int px=Mathf.RoundToInt(Mathf.Cos(a)*27),pz=82+Mathf.RoundToInt(Mathf.Sin(a)*27);if(Mathf.Abs(x-px)<=1&&Mathf.Abs(z-pz)<=1&&y>=ground+3&&y<=ground+9)return new DataBlock((i&1)==0?lgGlass:lgGold);}
        }
        // KEY OF TIME: second ceremonial clock/key circle.
        if(LGCircle(x,z,0,-150,34)){
            if(y==ground+1&&LGCircle(x,z,0,-150,33))return new DataBlock(lgDark);
            if(LGRing(x,z,0,-150,25,1.4f)&&y>=ground+2&&y<=ground+3)return new DataBlock(lgGold);
            if((Mathf.Abs(x)<=2&&z>=-174&&z<=-126)&&y>=ground+2&&y<=ground+4)return new DataBlock(lgGold);
            if(LGCircle(x,z,0,-150,8)&&y>=ground+2&&y<=ground+8)return new DataBlock(lgStone);
        }
        // Four small classical round temples / gazebos.
        int[] tx={-138,132,-128,124},tz={220,205,-245,-260};
        for(int k=0;k<4;k++){int dx=x-tx[k],dz=z-tz[k];float d=Mathf.Sqrt(dx*dx+dz*dz);
            if(d<=10){if(y==ground+1&&d<=10)return new DataBlock(lgStone);if(LGRing(x,z,tx[k],tz[k],8,1)&&y>=ground+2&&y<=ground+9)return new DataBlock(lgStone);if(y>=ground+10&&y<=ground+12&&d<=9-(y-ground-10)*2)return new DataBlock(lgGold);}
        }
        // Monument figures: tall stylised voxel statues at the far garden axis.
        int[] sx={-138,138},sz={315,315}; for(int k=0;k<2;k++){int dx=Mathf.Abs(x-sx[k]),dz=Mathf.Abs(z-sz[k]);if(dx<=3&&dz<=3&&y>=ground+1&&y<=ground+20)return new DataBlock(lgStone);if(dx<=5&&dz<=4&&y>=ground+16&&y<=ground+22)return new DataBlock(lgStone);}
        // Crystal temple: broken stone circle with a bright central pyramid/crystal.
        if(LGCircle(x,z,-105,15,22)){
            if(y==ground+1&&LGCircle(x,z,-105,15,20))return new DataBlock(lgDark);
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4f;int px=-105+Mathf.RoundToInt(Mathf.Cos(a)*16),pz=15+Mathf.RoundToInt(Mathf.Sin(a)*16);if(Mathf.Abs(x-px)<=2&&Mathf.Abs(z-pz)<=2&&y>=ground+2&&y<=ground+13)return new DataBlock(lgDark);}
            int pd=Mathf.Abs(x+105)+Mathf.Abs(z-15);if(pd<=8&&y>=ground+2&&y<=ground+10-pd/2)return new DataBlock(lgGlass);
        }
        // Music pavilion: circular floor, open columns and shallow stepped dome.
        if(LGCircle(x,z,112,-8,18)){
            if(y==ground+1&&LGCircle(x,z,112,-8,17))return new DataBlock(lgStone);
            for(int i=0;i<10;i++){float a=i*Mathf.PI/5f;int px=112+Mathf.RoundToInt(Mathf.Cos(a)*14),pz=-8+Mathf.RoundToInt(Mathf.Sin(a)*14);if(Mathf.Abs(x-px)<=1&&Mathf.Abs(z-pz)<=1&&y>=ground+2&&y<=ground+11)return new DataBlock(lgDark);}
            float dd=Mathf.Sqrt((x-112)*(x-112)+(z+8)*(z+8));if(y>=ground+12&&y<=ground+15&&dd<=17-(y-ground-12)*3)return new DataBlock(lgStone);
        }
        // Small island sanctuary / sphere location in the connected water garden.
        if(LGCircle(x,z,88,178,17)){
            if(y==ground+1&&LGCircle(x,z,88,178,16))return new DataBlock(grass);
            if(LGCircle(x,z,88,178,5)&&y>=ground+2&&y<=ground+8)return new DataBlock(lgGold);
        }
        return default(DataBlock);
    }
    DataBlock SampleLightGarden(int x,int y,int z){
        if(grass==null)ResolveBlocks(); float m=LGMask(x,z); if(m<=0)return default(DataBlock);
        int top=LGGroundY(x,z); bool lake=LGLake(x,z);
        // floating inverted mountain body
        float nx=x/195f,nz=z/395f; float radial=Mathf.Clamp01(1f-Mathf.Sqrt(nx*nx+nz*nz));
        int bottom=top-Mathf.RoundToInt(8+radial*62+(Mathf.PerlinNoise((x+5)*.03f,(z-8)*.03f)-.5f)*8*radial);
        if(y<bottom||y>top+20)return default(DataBlock);
        // lakes are carved 3 voxels into the garden and filled to top-1
        if(lake){int bed=top-4;if(y<=bed&&y>=bottom)return new DataBlock(y>=bed-1?dirt:rock);if(y>bed&&y<=top-1)return new DataBlock(water);}
        else if(y<=top){if(y==top)return new DataBlock(grass);if(y>=top-3)return new DataBlock(dirt);return new DataBlock(rock);}
        DataBlock built=LGBuildings(x,y,z,top); if(!built.IsEmpty())return built;
        if(y==top+1&&LGPath(x,z))return new DataBlock(lgStone);
        // Deterministic garden vegetation; keep ritual circles, paths and lake margins open.
        bool clear=LGLake(x,z)||LGCircle(x,z,0,82,48)||LGCircle(x,z,0,-150,42)||LGPath(x,z);
        if(!clear&&y>top){DataBlock f;/* 1.04: Lichtgarten trees are part of the authored world and must not depend on the generic procedural-tree toggle. */if(variedTrees==null)variedTrees=new LegacyInfiniteTrees(this);if(variedTrees.TryGet(x,y,z,out f))return f;if(y==top+1&&TryFlora(x,y,z,out f))return f;}
        return default(DataBlock);
    }

    struct SkyIslandInfo { public bool found; public float cx,cz,rx,rz; public int cy; public float shape; }
    SkyIslandInfo SkyAt(int x,int z){
        SkyIslandInfo best=default(SkyIslandInfo); float bestQ=999f;
        int cellX=InfiniteWorldMath.FloorDiv(x,skyCellSize), cellZ=InfiniteWorldMath.FloorDiv(z,skyCellSize);
        for(int dz=-1;dz<=1;dz++) for(int dx=-1;dx<=1;dx++){
            int gx=cellX+dx,gz=cellZ+dz; bool spawn=(gx==0&&gz==0);
            if(!spawn && H01(gx,gz,201)>.78f) continue;
            float cx=spawn?0f:(gx+.18f+H01(gx,gz,202)*.64f)*skyCellSize;
            float cz=spawn?0f:(gz+.18f+H01(gx,gz,203)*.64f)*skyCellSize;
            float r=spawn?88f:Mathf.Lerp(skyMinRadius,skyMaxRadius,H01(gx,gz,204));
            float aspect=Mathf.Lerp(.72f,1.32f,H01(gx,gz,205));
            float rx=r,rz=r*aspect;
            float nx=(x-cx)/rx,nz=(z-cz)/rz;
            float rough=(Mathf.PerlinNoise((x+gx*31+seed)*.025f,(z-gz*37-seed)*.025f)-.5f)*.22f;
            float q=Mathf.Sqrt(nx*nx+nz*nz)-rough;
            if(q<1f && q<bestQ){
                bestQ=q; best.found=true; best.cx=cx; best.cz=cz; best.rx=rx; best.rz=rz;
                best.cy=skyBaseHeight + (spawn?0:Mathf.RoundToInt((H01(gx,gz,206)-.5f)*34f));
                best.shape=Mathf.Clamp01(1f-q);
            }
        }
        return best;
    }
    int SkySurfaceY(int x,int z){
        SkyIslandInfo a=SkyAt(x,z); if(!a.found) return skyBaseHeight;
        float dome=Mathf.Pow(a.shape,.48f);
        float topNoise=(Mathf.PerlinNoise((x+seed)*.035f,(z-seed)*.035f)-.5f)*5f;
        return a.cy + Mathf.RoundToInt(5f*dome + topNoise);
    }
    DataBlock SampleSky(int x,int y,int z){
        if(grass==null||dirt==null||rock==null) ResolveBlocks();
        SkyIslandInfo a=SkyAt(x,z); if(!a.found) return default(DataBlock);
        int top=SkySurfaceY(x,z);
        // Deep pointed underside. Near the rim the island is thin; toward the centre it hangs down.
        // 0.9.9.8c: mountain-like inverted profile: thin rim, substantially deeper central rock root.
        float mountainCore=Mathf.Pow(a.shape,1.12f);
        float coreDepth=Mathf.Lerp(48f,78f,Mathf.PerlinNoise((a.cx+seed)*.013f,(a.cz-seed)*.013f));
        float underside=5f + mountainCore*coreDepth;
        // Secondary broad ridges break up the cone without flattening the silhouette.
        float ridge=(Mathf.PerlinNoise((x-seed)*.022f,(z+seed)*.022f)-.5f)*13f;
        underside += ridge*Mathf.Pow(a.shape,.72f);
        underside += (Mathf.PerlinNoise((x-seed)*.055f,(z+seed)*.055f)-.5f)*6f*a.shape;
        int bottom=top-Mathf.Max(3,Mathf.RoundToInt(underside));
        if(y<bottom||y>top) return default(DataBlock);
        // A little erosion/noise on the lower shell creates natural ledges and caves without breaking the top.
        if(y<top-3 && a.shape<.82f){
            float cave=Mathf.PerlinNoise((x+y*2+seed)*.065f,(z-y*3-seed)*.065f);
            if(cave>.79f) return default(DataBlock);
        }
        if(y==top && grass!=null) return new DataBlock(grass);
        if(y>=top-3 && dirt!=null) return new DataBlock(dirt);
        return rock!=null?new DataBlock(rock):default(DataBlock);
    }

    public int SurfaceYProcedural(int x,int z){ return lightGarden ? LGGroundY(x,z) : (skyIslands ? SkySurfaceY(x,z) : GetColumn(x,z).top); }
    public int SurfaceY(int x,int z){ return BlockIslandWorldSource.SurfaceY(this,x,z); }

    ColumnInfo GetColumn(int x,int z){
        long key=ColumnKey(x,z); ColumnInfo c;
        if(columnCache.TryGetValue(key,out c)) return c;
        if(columnCache.Count>=MaxColumnCache) columnCache.Clear();
        c.land=LandMask(x,z,out c.relief);
        c.top=ComputeSurfaceY(x,z,c.land,c.relief);
        c.beach=ComputeBeach(x,z,c.top,c.land);

        // Stage 6.9.4: NEVER change geometry after beach classification.  Beach is a material
        // decision only.  ComputeSurfaceY already supplies the continuous coastal ramp.
        // This removes the artificial sand terrace / vertical dirt wall created by 6.9.3.

        // Stage 6.9.9: vegetation eligibility follows the actual surface material.
        // If the exposed top voxel is Grass (the original Dirt height band y=26..40),
        // vegetation is allowed. Density/placement stays deterministic in the feature
        // generators, so grassy terrain is no longer arbitrarily disabled by climate masks.
        c.vegetated=c.land>0.001f && c.top>=26 && c.top<41 && !c.beach;
        columnCache[key]=c; return c;
    }

    bool IsLand(int x,int z){ float r; return LandMask(x,z,out r)>0.001f; }

    bool ComputeBeach(int x,int z,int top,float land){
        // Original IslandGenerator: sand is an absolute vertical band below .52*50.
        // A visible beach is therefore simply dry terrain at/just above waterline.
        if(land<=0.001f) return false;
        const int originalSandTop=25; // y < 26 in the original loop
        return top>=seaLevel && top<=originalSandTop+1;
    }

    bool IsBeachColumn(int x,int z,int top,float land){ return GetColumn(x,z).beach; }
    public bool IsVegetatedGround(int x,int z){
        if(lightGarden) return LGMask(x,z)>0.15f && !LGLake(x,z) && !LGCircle(x,z,0,82,48) && !LGCircle(x,z,0,-150,42) && !LGPath(x,z);
        if(skyIslands){
            SkyIslandInfo a=SkyAt(x,z);
            // Keep trees away from the razor-thin rim and let the broad upper plateaus breathe.
            return a.found && a.shape>0.22f;
        }
        return GetColumn(x,z).vegetated;
    }

    bool TryTree(int x,int y,int z,out DataBlock result){
        result=default(DataBlock);
        if(!generateTrees||wood==null||leaf==null)return false;
        const int cell=9;
        int cx=InfiniteWorldMath.FloorDiv(x,cell),cz=InfiniteWorldMath.FloorDiv(z,cell);
        for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++){
            int gx=cx+dx,gz=cz+dz;
            if((Hash(gx,gz,70)&255)>=18)continue;
            int h=Hash(gx,gz,71), ah=h==int.MinValue?int.MaxValue:Mathf.Abs(h);
            int tx=gx*cell+2+(ah%5),tz=gz*cell+2+(Mathf.Abs(h/31)%5);
            int ground=SurfaceY(tx,tz);
            if(ground<=seaLevel+2)continue;
            int trunk=4+(ah%3), top=ground+trunk;
            if(x==tx&&z==tz&&y>ground&&y<=top){result=new DataBlock(wood);return true;}
            int rx=Mathf.Abs(x-tx),rz=Mathf.Abs(z-tz),ry=Mathf.Abs(y-top);
            // Rounded layered crown instead of a cube.
            if(y>ground+2 && ry<=3 && rx<=3 && rz<=3 &&
               rx*rx+rz*rz+ry*ry*2<=11){result=new DataBlock(leaf);return true;}
        }
        return false;
    }

    bool TryFlora(int x,int y,int z,out DataBlock result){
        result=default(DataBlock);
        int top=SurfaceY(x,z);
        if(y!=top+1||top<=seaLevel+2)return false;
        int c=Hash(x,z,91)&1023,idx=-1;
        if(c<38)idx=0; else if(c<70)idx=1; else if(c<78)idx=2; else if(c<86)idx=3;
        if(idx<0||flora[idx]==null)return false;
        result=new DataBlock(flora[idx]);return true;
    }

    public DataBlock Sample(int x,int y,int z){ if(InfiniteWorldSave.CurrentWorldType==InfiniteWorldSave.WorldType.Minecraft) return default(DataBlock); if(InfiniteWorldSave.CurrentWorldType==InfiniteWorldSave.WorldType.Relativity) return RelativityWorld.Sample(x,y,z); return BlockIslandWorldSource.Sample(this,x,y,z); }

    // Procedural source implementation. Fixed worlds enter through BlockIslandWorldSource.
    public DataBlock SampleProcedural(int x,int y,int z){
        if(grass==null||water==null) ResolveBlocks();
        if(lightGarden) return SampleLightGarden(x,y,z);
        if(skyIslands){
            DataBlock terrainBlock=SampleSky(x,y,z);
            if(!terrainBlock.IsEmpty()) return terrainBlock;

            // 0.9.9.8d: vegetation is generated on the real top surface of every floating island.
            // It stays sparse enough to preserve large building areas.
            int skyTop=SkySurfaceY(x,z);
            SkyIslandInfo sky=SkyAt(x,z);
            if(sky.found && sky.shape>0.22f && y>skyTop){
                DataBlock skyFeature;
                if(generateTrees){
                    if(variedTrees==null) variedTrees=new LegacyInfiniteTrees(this);
                    if(variedTrees.TryGet(x,y,z,out skyFeature)) return skyFeature;
                }
                if(y==skyTop+1 && TryFlora(x,y,z,out skyFeature)) return skyFeature;
            }
            return default(DataBlock);
        }

        // 6.7.4 HARD COORDINATE PLANE. This bypasses IslandGenerator, LandMask, SurfaceY,
        // water and all legacy finite-map data. Every global 32x32 chunk is one solid square
        // at a fixed world Y. If this is not a regular checkerboard, the fault is downstream
        // of the terrain generator (streaming / chunk placement / transform / mesh path).
        if(worldCoordinateProof && hardCoordinatePlane){
            if(y != seaLevel + 1) return default(DataBlock);
            int proofCx=InfiniteWorldMath.FloorDiv(x,Chunk.X_SIZE);
            int proofCz=InfiniteWorldMath.FloorDiv(z,Chunk.Z_SIZE);
            Block proof=(((proofCx+proofCz)&1)==0) ? grass : dirt;
            return proof!=null ? new DataBlock(proof) : default(DataBlock);
        }

        ColumnInfo column=GetColumn(x,z);
        float land=column.land;
        int top=column.top;

        // 6.9.1: Fill the complete visible water column above submerged terrain, not only
        // the single y==seaLevel surface voxel. The previous surface-only ocean exposed empty
        // grey space wherever the coastal shelf sat below sea level.
        if(top < seaLevel && y > top && y <= seaLevel && water!=null)
            return new DataBlock(water);

        // Open ocean remains deliberately shallow/sparse for performance, but it gets enough
        // water depth to hide the underside when viewed from a beach or while flying.
        if(land<=0.001f) {
            if(y<=seaLevel && y>=seaLevel-8 && water!=null) return new DataBlock(water);
            return default(DataBlock);
        }

        // Only terrain below/equal to the local continuous surface is solid.
        if(y<=top){
            // Keep the island volume shallow. It follows the local surface instead of filling
            // a fixed 512x512 prism down to a global Y coordinate.
            int bottom=top-8;
            if(y<bottom) return default(DataBlock);

            // Stage 6.9.5: ORIGINAL material thresholds, evaluated by absolute Y.
            // This is intentionally NOT a full-column "beach" material.  Sand exists only
            // in the original low vertical band; dirt and rock keep their original heights.
            const int sandExclusive=26; // y < .52*50
            const int dirtExclusive=41; // y < .83*50 (integer loop equivalent)
            const int rockExclusive=45; // y < .90*50

            if(y < sandExclusive && sand!=null) return new DataBlock(sand);
            if(y < dirtExclusive && dirt!=null) {
                // Original GeneratePlants converted only exposed Dirt tops to Grass.
                if(y==top && grass!=null) return new DataBlock(grass);
                return new DataBlock(dirt);
            }
            if(y < rockExclusive && rock!=null) return new DataBlock(rock);
            // Stage 6.9.7: unlike the finite 50-high map, the infinite world may grow real
            // mountains above Y=45. Continue those columns as natural stone instead of AIR.
            if(rock!=null) return new DataBlock(rock);
            return default(DataBlock);
        }

        DataBlock feature;
        // Use the five existing legacy tree families (oak/birch/pine/sakura/red) in the
        // infinite world instead of the old single wood/leaf procedural tree.
        if(!worldCoordinateProof && generateTrees && column.vegetated && y>top){
            if(variedTrees==null) variedTrees=new LegacyInfiniteTrees(this);
            if(variedTrees.TryGet(x,y,z,out feature)) return feature;
        }
        if(!worldCoordinateProof && column.vegetated && y==top+1 && TryFlora(x,y,z,out feature)) return feature;
        return default(DataBlock);
    }



}
