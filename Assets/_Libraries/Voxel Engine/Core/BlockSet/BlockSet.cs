using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class BlockSet : MonoBehaviour {
	
	private static BlockSet _instance;
	public static BlockSet instance {
		get {
			return _instance;
		}
	}

    public static Block[] blockArray;

	[SerializeField] private List<Block> blocks = new List<Block>();

	private readonly List<Material> materials = new List<Material>();

	public int Count {
		get {
			return blocks.Count;
		}
	}

	public Block this[int index] {
		get {
			if(index < 0 || index >= blocks.Count) return null;
			return blocks[index];
		}
	}
	
	void Awake() {
        _instance = this;

        materials.Clear();

        // Stage 6.10.0 - append rail system while preserving legacy serialized block IDs.
        EnsureMinecartBlocks();
        EnsureBlockIslandLegacyPalette();
        EnsureBlockIslandLegacySpecials();
        EnsureBlockIslandLegacyFlora();
        EnsureBlockIslandLegacyTrees();
        EnsureBlockIslandRestorePalette();
        EnsureShapeBlocks();
        EnsureTanviirImportPalette();
        EnsureTanviirArchitecturePalette();
        EnsureTanviirArchitecturalShapes();
        EnsureTanviirFullPalette();
        // Voxel Box 0.9.9.7h: 16 numbered construction cubes only. Legacy GO 01-16 slots were removed to stay below the 255 block ceiling.
        EnsureVoxelBoxCreativeSlots();
        EnsureVoxelBoxTNT(); // 0.6
        EnsureVoxelBoxCopyPaste(); // 0.7: appended last, never shifts older IDs.
        EnsureVoxelBoxCustomWorkshop(); // 0.9.9.7h: 32 custom mini-voxel slots; 17-32 replace removed GO 01-16 capacity.

        // Block IDs are 32-bit in the modernized engine. List/array indexing makes int.MaxValue
        // the practical ceiling instead of the legacy 256-block byte limit.
        for(int i=0; i<blocks.Count; i++) {
			blocks[i].Init(this, i);
		}
        blockArray = blocks.ToArray();
        // 0.9.9.7: preserve factory faces, then apply optional global Block Designer overrides.
        BlockTextureDesigner.CaptureOriginals(this);
        BlockTextureDesigner.LoadAndApply(this);
	}
	
	
    private void EnsureMinecartBlocks() {
        if(FindBlock("Rail Straight") == null) {
            MinecartRailBlock b = ScriptableObject.CreateInstance<MinecartRailBlock>();
            b.name = "Rail Straight"; b.kind = MinecartRailBlock.RailKind.Straight; blocks.Add(b);
        }
        if(FindBlock("Rail Curve") == null) {
            MinecartRailBlock b = ScriptableObject.CreateInstance<MinecartRailBlock>();
            b.name = "Rail Curve"; b.kind = MinecartRailBlock.RailKind.Curve; blocks.Add(b);
        }
        if(FindBlock("Minecart") == null) {
            MinecartRailBlock b = ScriptableObject.CreateInstance<MinecartRailBlock>();
            b.name = "Minecart"; b.kind = MinecartRailBlock.RailKind.Minecart; blocks.Add(b);
        }
    }


    // Stage 6.12.7 - first recovered Block Island building palette.
    // These are appended at runtime so every existing serialized block ID remains untouched.
    // The source artwork comes from the user's recovered original atlases; special legacy
    // geometry (glass/iron mesh/ladder/etc.) will be restored in later isolated stages.
    private void EnsureBlockIslandLegacyPalette() {
        string[] names = {
            "BI Stained Glass", "BI Green", "BI Blue", "BI Red",
            "BI Blue Brick", "BI Orange", "BI Sandstone", "BI Mosaic",
            "BI Pebble", "BI Grass Paver", "BI Red Brick", "BI Stone Paver"
        };
        Texture2D atlas = Resources.Load<Texture2D>("BlockSet/Atlases/BlockIslandLegacy/Block Island Legacy Atlas");
        if(atlas == null) {
            Debug.LogWarning("Stage 6.12.7: recovered Block Island atlas could not be loaded; legacy palette skipped.");
            return;
        }
        Shader shader = Shader.Find("VoxelEngine/Diffuse");
        if(shader == null) shader = Shader.Find("Standard");
        if(shader == null) {
            Debug.LogWarning("Stage 6.12.7: no compatible block shader found; legacy palette skipped.");
            return;
        }
        Material material = new Material(shader);
        material.name = "Block Island Legacy Atlas Runtime Material";
        material.mainTexture = atlas;
        material.hideFlags = HideFlags.DontSave;

        for(int i=0;i<names.Length;i++) {
            if(FindBlock(names[i]) != null) continue;
            CubeBlock b = ScriptableObject.CreateInstance<CubeBlock>();
            b.name = names[i];
            b.hideFlags = HideFlags.DontSave;
            int col=i%4, row=i/4;
            // Unity UV origin is bottom-left; our atlas rows were authored top-to-bottom.
            Rect uv = new Rect(col/4f, 1f-(row+1)/3f, 1f/4f, 1f/3f);
            b.front = LegacyFace(material,uv); b.back = LegacyFace(material,uv);
            b.right = LegacyFace(material,uv); b.left = LegacyFace(material,uv);
            b.top = LegacyFace(material,uv); b.bottom = LegacyFace(material,uv);
            blocks.Add(b);
        }
        Debug.Log("STAGE 6.12.7: recovered Block Island palette ready (12 build blocks, existing IDs preserved).");
    }

    private static Face LegacyFace(Material material, Rect uv) {
        Face f = new Face(); f.material=material; f.rect=uv; return f;
    }


    // Stage 6.12.8 - recovered Block Island special blocks. Appended only; old IDs stay stable.
    private void EnsureBlockIslandLegacySpecials() {
        Texture2D atlas = Resources.Load<Texture2D>("BlockSet/Atlases/BlockIslandLegacy/Block Island Special Atlas");
        if(atlas == null) { Debug.LogWarning("Stage 6.12.8: special atlas missing; special blocks skipped."); return; }
        Shader alphaShader = Shader.Find("VoxelEngine/Alpha-Diffuse_cull_off");
        if(alphaShader == null) alphaShader = Shader.Find("VoxelEngine/Alpha-Diffuse");
        if(alphaShader == null) alphaShader = Shader.Find("Standard");
        Material mat = new Material(alphaShader); mat.name="Block Island Special Runtime Material"; mat.mainTexture=atlas; mat.hideFlags=HideFlags.DontSave;

        AddLegacySpecial("BI Glass Panel",BlockIslandLegacySpecialBlock.LegacyKind.GlassPanel,mat,new Rect(0f,0f,.25f,1f));
        AddLegacySpecial("BI Iron Mesh",BlockIslandLegacySpecialBlock.LegacyKind.IronMesh,mat,new Rect(.25f,0f,.25f,1f));
        AddLegacySpecial("BI Ladder",BlockIslandLegacySpecialBlock.LegacyKind.Ladder,mat,new Rect(.5f,0f,.25f,1f));

        if(FindBlock("BI Red Flower") == null) {
            CrossBlock c=ScriptableObject.CreateInstance<CrossBlock>(); c.name="BI Red Flower"; c.hideFlags=HideFlags.DontSave;
            c.face=LegacyFace(mat,new Rect(.75f,0f,.25f,1f)); blocks.Add(c);
        }
        Debug.Log("STAGE 6.12.8: Block Island specials ready: connected glass, iron mesh, ladder, red flower.");
    }

    private void AddLegacySpecial(string n, BlockIslandLegacySpecialBlock.LegacyKind kind, Material mat, Rect uv) {
        if(FindBlock(n)!=null) return;
        BlockIslandLegacySpecialBlock b=ScriptableObject.CreateInstance<BlockIslandLegacySpecialBlock>();
        b.name=n; b.kind=kind; b.hideFlags=HideFlags.DontSave; b.face=LegacyFace(mat,uv); blocks.Add(b);
    }


    // Stage 6.12.9 - recovered Block Island flora + cactus.
    // Kept in a separate atlas and appended after 6.12.8 so all existing world IDs remain stable.
    private void EnsureBlockIslandLegacyFlora() {
        Texture2D atlas=Resources.Load<Texture2D>("BlockSet/Atlases/BlockIslandLegacy/Block Island Flora Atlas");
        if(atlas==null) { Debug.LogWarning("Stage 6.12.9: flora atlas missing; legacy flora skipped."); return; }
        Shader alphaShader=Shader.Find("VoxelEngine/Alpha-Diffuse_cull_off");
        if(alphaShader==null) alphaShader=Shader.Find("VoxelEngine/Alpha-Diffuse");
        if(alphaShader==null) alphaShader=Shader.Find("Standard");
        Material mat=new Material(alphaShader); mat.name="Block Island Flora Runtime Material"; mat.mainTexture=atlas; mat.hideFlags=HideFlags.DontSave;

        if(FindBlock("BI Cactus")==null) {
            CactusBlock c=ScriptableObject.CreateInstance<CactusBlock>(); c.name="BI Cactus"; c.hideFlags=HideFlags.DontSave;
            Face f=LegacyFace(mat,new Rect(0f,0f,1f/8f,1f)); c.side=f; c.top=LegacyFace(mat,new Rect(0f,0f,1f/8f,1f)); c.bottom=LegacyFace(mat,new Rect(0f,0f,1f/8f,1f));
            blocks.Add(c);
        }
        string[] flora={"BI Poppy","BI Grass Tuft","BI Dandelion","BI Red Tulips","BI Pink Flowers","BI Blue Orchid","BI Orange Tulips"};
        for(int i=0;i<flora.Length;i++) {
            if(FindBlock(flora[i])!=null) continue;
            CrossBlock c=ScriptableObject.CreateInstance<CrossBlock>(); c.name=flora[i]; c.hideFlags=HideFlags.DontSave;
            c.face=LegacyFace(mat,new Rect((i+1)/8f,0f,1f/8f,1f)); blocks.Add(c);
        }
        Debug.Log("STAGE 6.12.9: Block Island flora ready: cactus + 7 recovered plants.");
    }

    // Stage 6.13.0 - recovered Block Island tree materials.
    // Appended only: existing IDs stay stable. Side/top bark and alpha leaves are kept separate.
    private void EnsureBlockIslandLegacyTrees() {
        Texture2D atlas=Resources.Load<Texture2D>("BlockSet/Atlases/BlockIslandLegacy/Block Island Tree Atlas");
        if(atlas==null) { Debug.LogWarning("Stage 6.13.0: Block Island tree atlas missing; recovered trees skipped."); return; }
        Shader solidShader=Shader.Find("VoxelEngine/Diffuse"); if(solidShader==null) solidShader=Shader.Find("Standard");
        Shader alphaShader=Shader.Find("VoxelEngine/Alpha-Diffuse_cull_off"); if(alphaShader==null) alphaShader=Shader.Find("VoxelEngine/Alpha-Diffuse"); if(alphaShader==null) alphaShader=solidShader;
        Material solid=new Material(solidShader); solid.name="Block Island Tree Bark Runtime Material"; solid.mainTexture=atlas; solid.hideFlags=HideFlags.DontSave;
        Material alpha=new Material(alphaShader); alpha.name="Block Island Tree Leaves Runtime Material"; alpha.mainTexture=atlas; alpha.hideFlags=HideFlags.DontSave;
        AddBITreeWood("BI Oak Wood",solid,0,1); AddBITreeLeaf("BI Oak Leaf",alpha,2);
        AddBITreeWood("BI Birch Wood",solid,3,4); AddBITreeLeaf("BI Birch Leaf",alpha,5);
        AddBITreeWood("BI Pine Wood",solid,6,7); AddBITreeLeaf("BI Pine Leaf",alpha,8);
        Debug.Log("STAGE 6.13.0: recovered Block Island Oak/Birch/Pine materials ready.");
    }
    private Rect BITreeUV(int column) { return new Rect(column/9f,0f,1f/9f,1f); }
    private void AddBITreeWood(string n,Material mat,int side,int top) {
        if(FindBlock(n)!=null)return; CubeBlock b=ScriptableObject.CreateInstance<CubeBlock>(); b.name=n; b.hideFlags=HideFlags.DontSave;
        Face s=LegacyFace(mat,BITreeUV(side)); Face t=LegacyFace(mat,BITreeUV(top));
        b.front=s;b.back=LegacyFace(mat,BITreeUV(side));b.right=LegacyFace(mat,BITreeUV(side));b.left=LegacyFace(mat,BITreeUV(side));b.top=t;b.bottom=LegacyFace(mat,BITreeUV(top)); blocks.Add(b);
    }
    private void AddBITreeLeaf(string n,Material mat,int col) {
        if(FindBlock(n)!=null)return; CubeBlock b=ScriptableObject.CreateInstance<CubeBlock>(); b.name=n; b.hideFlags=HideFlags.DontSave; Rect uv=BITreeUV(col);
        b.front=LegacyFace(mat,uv);b.back=LegacyFace(mat,uv);b.right=LegacyFace(mat,uv);b.left=LegacyFace(mat,uv);b.top=LegacyFace(mat,uv);b.bottom=LegacyFace(mat,uv);blocks.Add(b);
    }


    // 0.9.9.4 - restoration materials used only by the fixed Block Island source mapper.
    // Appended at runtime, so no existing BlockSet IDs are reordered.
    private void EnsureBlockIslandRestorePalette() {
        Texture2D atlas=Resources.Load<Texture2D>("BlockSet/Atlases/BlockIslandLegacy/Block Island Restore Atlas");
        if(atlas==null) { Debug.LogWarning("Block Island restore atlas missing."); return; }
        Shader shader=Shader.Find("VoxelEngine/Diffuse"); if(shader==null) shader=Shader.Find("Standard"); if(shader==null)return;
        Material mat=new Material(shader); mat.name="Block Island Restore Runtime Material"; mat.mainTexture=atlas; mat.hideFlags=HideFlags.DontSave;
        AddBIRestoreCube("Block Island White Stone",mat,new Rect(0f,0f,0.5f,1f));
        AddBIRestoreCube("Block Island Red Stone",mat,new Rect(0.5f,0f,0.5f,1f));
        Debug.Log("BLOCK ISLAND 0.9.9.4: white wall + red roof restoration blocks appended.");
    }
    private void AddBIRestoreCube(string n,Material mat,Rect uv) {
        if(FindBlock(n)!=null)return;
        CubeBlock b=ScriptableObject.CreateInstance<CubeBlock>(); b.name=n; b.hideFlags=HideFlags.DontSave;
        b.front=LegacyFace(mat,uv);b.back=LegacyFace(mat,uv);b.left=LegacyFace(mat,uv);b.right=LegacyFace(mat,uv);b.top=LegacyFace(mat,uv);b.bottom=LegacyFace(mat,uv);blocks.Add(b);
    }


    // Stage 6.13.1 - extra building shapes. Appended only, preserving every older block ID.
    private void EnsureShapeBlocks() {
        StairBlock sourceStair=FindBlock<StairBlock>("Stair");
        Face shapeFace=null;
        if(sourceStair!=null) shapeFace=sourceStair.face;
        if(shapeFace==null) {
            CubeBlock stone=FindBlock<CubeBlock>("Stone");
            if(stone!=null) shapeFace=stone.top;
        }
        if(shapeFace==null) { Debug.LogWarning("Stage 6.13.1: no source face for shape blocks."); return; }

        if(FindBlock("Double Stair")==null) {
            StairBlock stair=ScriptableObject.CreateInstance<StairBlock>(); stair.name="Double Stair"; stair.hideFlags=HideFlags.DontSave;
            stair.face=shapeFace; blocks.Add(stair);
        }
        if(FindBlock("Sphere")==null) {
            SphereBlock sphere=ScriptableObject.CreateInstance<SphereBlock>(); sphere.name="Sphere"; sphere.hideFlags=HideFlags.DontSave;
            sphere.face=shapeFace; blocks.Add(sphere);
        }
        Debug.Log("STAGE 6.13.1: Double Stair + Sphere appended to BlockSet.");
    }


    // Stage 6.13.2 - original fantasy building palette for the Tanviir/Minecraft import world.
    // IMPORTANT: these are new Block Island textures, not copied Minecraft or Tanviir resource-pack art.
    // Everything is appended after the existing palette so old Block Island save IDs stay stable.
    private void EnsureTanviirImportPalette() {
        Texture2D atlas=Resources.Load<Texture2D>("BlockSet/Atlases/TanviirImport/Tanviir Import Atlas");
        if(atlas==null) { Debug.LogWarning("Stage 6.13.2: Tanviir import atlas missing; import palette skipped."); return; }
        Shader shader=Shader.Find("VoxelEngine/Diffuse"); if(shader==null) shader=Shader.Find("Standard");
        if(shader==null) { Debug.LogWarning("Stage 6.13.2: no solid shader for Tanviir palette."); return; }
        Material mat=new Material(shader); mat.name="Tanviir Import Runtime Material"; mat.mainTexture=atlas; mat.hideFlags=HideFlags.DontSave;

        string[] names={
            "Tanviir Pale Stone","Tanviir Weathered Stone","Tanviir Dark Stone","Tanviir Mossy Stone",
            "Tanviir Warm Sandstone","Tanviir Ivory Brick","Tanviir Slate","Tanviir Roof Tile",
            "Tanviir Oak Beam","Tanviir Dark Timber","Tanviir Old Plank","Tanviir Cobble",
            "Tanviir Carved Stone","Tanviir Gold Trim","Tanviir Blue Tile","Tanviir Green Tile"
        };
        for(int i=0;i<names.Length;i++) AddTanviirCube(names[i],mat,TanviirUV(i));

        AddTanviirStair("Tanviir Pale Stone Stair",mat,TanviirUV(0));
        AddTanviirStair("Tanviir Dark Stone Stair",mat,TanviirUV(2));
        AddTanviirStair("Tanviir Oak Stair",mat,TanviirUV(8));
        AddTanviirStair("Tanviir Roof Stair",mat,TanviirUV(7));
        // 6.13.15: visible compatibility blocks for legacy Minecraft IDs that previously
        // fell back to generic/special blocks. Reuse the Tanviir atlas so no old IDs move.
        AddTanviirCube("Tanviir Lava",mat,TanviirUV(14));
        AddTanviirCube("Tanviir Spawner",mat,TanviirUV(2));
        AddTanviirCube("Tanviir Crop",mat,TanviirUV(15));
        AddTanviirCube("Tanviir Glass Pane",mat,TanviirUV(3));
        AddTanviirCube("Tanviir Utility Wood",mat,TanviirUV(10));
        AddTanviirCube("Tanviir Utility Stone",mat,TanviirUV(12));
        // 6.13.16: exact names requested by the Minecraft mapper. These two were proven missing
        // by the runtime diagnostics and previously fell back to Weathered Stone.
        AddTanviirCube("Glass",mat,TanviirUV(3));
        AddTanviirCube("Tanviir Redstone Block",mat,TanviirUV(14));
        Debug.Log("STAGE 6.13.16: Tanviir import palette ready + Glass/Redstone compatibility entries; old IDs preserved.");
    }
    private Rect TanviirUV(int i) {
        int col=i%4, row=i/4;
        return new Rect(col/4f,1f-(row+1)/4f,1f/4f,1f/4f);
    }
    private void AddTanviirCube(string n,Material mat,Rect uv) {
        if(FindBlock(n)!=null)return;
        CubeBlock b=ScriptableObject.CreateInstance<CubeBlock>(); b.name=n; b.hideFlags=HideFlags.DontSave;
        b.front=LegacyFace(mat,uv); b.back=LegacyFace(mat,uv); b.right=LegacyFace(mat,uv); b.left=LegacyFace(mat,uv); b.top=LegacyFace(mat,uv); b.bottom=LegacyFace(mat,uv);
        blocks.Add(b);
    }
    private void AddTanviirStair(string n,Material mat,Rect uv) {
        if(FindBlock(n)!=null)return;
        StairBlock b=ScriptableObject.CreateInstance<StairBlock>(); b.name=n; b.hideFlags=HideFlags.DontSave; b.face=LegacyFace(mat,uv); blocks.Add(b);
    }


    // Stage 6.13.3 - second Tanviir architecture pass: castle/city materials plus matching stairs and fences.
    // Appended after 6.13.2 only; no existing block IDs are reordered.
    private void EnsureTanviirArchitecturePalette() {
        Texture2D atlas=Resources.Load<Texture2D>("BlockSet/Atlases/TanviirImport/Tanviir Architecture Atlas");
        if(atlas==null) { Debug.LogWarning("Stage 6.13.3: Tanviir architecture atlas missing; architecture palette skipped."); return; }
        Shader shader=Shader.Find("VoxelEngine/Diffuse"); if(shader==null) shader=Shader.Find("Standard");
        if(shader==null) return;
        Material mat=new Material(shader); mat.name="Tanviir Architecture Runtime Material"; mat.mainTexture=atlas; mat.hideFlags=HideFlags.DontSave;
        string[] names={
            "Tanviir Marble","Tanviir Granite","Tanviir Black Brick","Tanviir White Brick",
            "Tanviir Red Roof","Tanviir Blue Roof","Tanviir Copper Roof","Tanviir Timber Plaster",
            "Tanviir Dark Plaster","Tanviir Limestone","Tanviir Castle Wall","Tanviir Mossy Cobble",
            "Tanviir Rune Stone","Tanviir Purple Tile","Tanviir Red Tile","Tanviir Gold Mosaic"
        };
        for(int i=0;i<names.Length;i++) AddTanviirCube(names[i],mat,TanviirUV(i));
        AddTanviirStair("Tanviir Marble Stair",mat,TanviirUV(0));
        AddTanviirStair("Tanviir Granite Stair",mat,TanviirUV(1));
        AddTanviirStair("Tanviir Red Roof Stair",mat,TanviirUV(4));
        AddTanviirStair("Tanviir Blue Roof Stair",mat,TanviirUV(5));
        AddTanviirStair("Tanviir Limestone Stair",mat,TanviirUV(9));
        AddTanviirStair("Tanviir Castle Stair",mat,TanviirUV(10));
        AddTanviirFence("Tanviir Timber Railing",mat,TanviirUV(7));
        AddTanviirFence("Tanviir Stone Railing",mat,TanviirUV(10));
        AddTanviirFence("Tanviir Dark Railing",mat,TanviirUV(2));
        Debug.Log("STAGE 6.13.3: Tanviir architecture palette ready: 16 blocks + 6 stairs + 3 railings; old IDs preserved.");
    }
    private void AddTanviirFence(string n,Material mat,Rect uv) {
        if(FindBlock(n)!=null)return;
        FenceBlock b=ScriptableObject.CreateInstance<FenceBlock>(); b.name=n; b.hideFlags=HideFlags.DontSave; b.face=LegacyFace(mat,uv); blocks.Add(b);
    }


    // Stage 6.13.4 - architectural shapes for fantasy cities/castles.
    // Appended after every 6.13.3 entry so existing save IDs remain stable.
    private void EnsureTanviirArchitecturalShapes() {
        Texture2D atlas=Resources.Load<Texture2D>("BlockSet/Atlases/TanviirImport/Tanviir Architecture Atlas");
        if(atlas==null) { Debug.LogWarning("Stage 6.13.4: Tanviir architecture atlas missing; shapes skipped."); return; }
        Shader shader=Shader.Find("VoxelEngine/Diffuse"); if(shader==null) shader=Shader.Find("Standard");
        if(shader==null) return;
        Material mat=new Material(shader); mat.name="Tanviir Shapes Runtime Material"; mat.mainTexture=atlas; mat.hideFlags=HideFlags.DontSave;
        AddTanviirShape("Tanviir Marble Column",TanviirArchitecturalBlock.Shape.Column,mat,TanviirUV(0));
        AddTanviirShape("Tanviir Castle Column",TanviirArchitecturalBlock.Shape.Column,mat,TanviirUV(10));
        AddTanviirShape("Tanviir Stone Arch",TanviirArchitecturalBlock.Shape.Arch,mat,TanviirUV(9));
        AddTanviirShape("Tanviir Castle Arch",TanviirArchitecturalBlock.Shape.Arch,mat,TanviirUV(10));
        AddTanviirShape("Tanviir Timber Window Frame",TanviirArchitecturalBlock.Shape.WindowFrame,mat,TanviirUV(7));
        AddTanviirShape("Tanviir Stone Window Frame",TanviirArchitecturalBlock.Shape.WindowFrame,mat,TanviirUV(9));
        AddTanviirShape("Tanviir Castle Gate Frame",TanviirArchitecturalBlock.Shape.GateFrame,mat,TanviirUV(10));
        AddTanviirShape("Tanviir Red Roof Ridge",TanviirArchitecturalBlock.Shape.RoofRidge,mat,TanviirUV(4));
        AddTanviirShape("Tanviir Blue Roof Ridge",TanviirArchitecturalBlock.Shape.RoofRidge,mat,TanviirUV(5));
        AddTanviirShape("Tanviir Stone Buttress",TanviirArchitecturalBlock.Shape.Buttress,mat,TanviirUV(10));
        AddTanviirShape("Tanviir Timber Cross Beam",TanviirArchitecturalBlock.Shape.BeamCross,mat,TanviirUV(7));
        AddTanviirShape("Tanviir Marble Pillar Cap",TanviirArchitecturalBlock.Shape.PillarCap,mat,TanviirUV(0));
        // The Voxel Box 0.1 creative expansion. Append-only keeps every established save ID stable.
        AddTanviirShape("Voxel Box Stone Slab",TanviirArchitecturalBlock.Shape.Slab,mat,TanviirUV(9));
        AddTanviirShape("Voxel Box Timber Wall Panel",TanviirArchitecturalBlock.Shape.WallPanel,mat,TanviirUV(7));
        AddTanviirShape("Voxel Box Timber Bench",TanviirArchitecturalBlock.Shape.Bench,mat,TanviirUV(7));
        AddTanviirShape("Voxel Box Stone Planter",TanviirArchitecturalBlock.Shape.Planter,mat,TanviirUV(0));
        Material glowMat=mat;
        Shader glowShader=Shader.Find("VoxelEngine/VoxelBoxGlow");
        if(glowShader!=null) { glowMat=new Material(glowShader); glowMat.name="Voxel Box Warm Glow Material"; glowMat.mainTexture=atlas; glowMat.hideFlags=HideFlags.DontSave; if(glowMat.HasProperty("_GlowColor")) glowMat.SetColor("_GlowColor",new Color(1f,.68f,.22f,1f)); }
        AddTanviirShape("Voxel Box Lantern Frame",TanviirArchitecturalBlock.Shape.LanternFrame,glowMat,TanviirUV(12));
        // The Voxel Box 0.2: append-only creative furniture and landscape architecture.
        AddTanviirShape("Voxel Box Timber Table",TanviirArchitecturalBlock.Shape.Table,mat,TanviirUV(7));
        AddTanviirShape("Voxel Box Timber Chair",TanviirArchitecturalBlock.Shape.Chair,mat,TanviirUV(7));
        AddTanviirShape("Voxel Box Stone Flower Box",TanviirArchitecturalBlock.Shape.FlowerBox,mat,TanviirUV(9));
        AddTanviirShape("Voxel Box Marble Fountain Basin",TanviirArchitecturalBlock.Shape.FountainBasin,mat,TanviirUV(0));
        AddTanviirShape("Voxel Box Rune Lamp Post",TanviirArchitecturalBlock.Shape.LampPost,glowMat,TanviirUV(12));
        AddTanviirShape("Voxel Box Timber Pergola Post",TanviirArchitecturalBlock.Shape.PergolaPost,mat,TanviirUV(7));
        AddTanviirShape("Voxel Box Stone Bridge Rail",TanviirArchitecturalBlock.Shape.BridgeRail,mat,TanviirUV(10));
        Debug.Log("THE VOXEL BOX 0.3: creative palette + warm glow lantern materials ready; old IDs preserved.");
    }
    private void AddTanviirShape(string n,TanviirArchitecturalBlock.Shape shape,Material mat,Rect uv) {
        if(FindBlock(n)!=null)return;
        TanviirArchitecturalBlock b=ScriptableObject.CreateInstance<TanviirArchitecturalBlock>();
        b.name=n; b.shape=shape; b.hideFlags=HideFlags.DontSave; b.face=LegacyFace(mat,uv); blocks.Add(b);
    }

	public void AddBlock(Block block) {
		blocks.Add(block);
	}
	
	public Block GetBlock(int index) {
		if(index < 0 || index >= blocks.Count) return null;
		return blocks[index];
	}
	
	public Block FindBlock(string name) {
		foreach(Block block in blocks) {
			if( string.Equals(block.name, name, System.StringComparison.OrdinalIgnoreCase) ) 
				return block;
		}
		return null;
	}
	
	public T FindBlock<T>(string name) where T : Block {
		foreach(Block block in blocks) {
			if(string.Equals(block.name, name, System.StringComparison.OrdinalIgnoreCase) && block is T) 
				return (T) block;
		}
		return null;
	}
	
	public Block[] FindBlocks(string name) {
		List<Block> list = new List<Block>();
		foreach(Block block in blocks) {
			if( string.Equals(block.name, name, System.StringComparison.OrdinalIgnoreCase) ) 
				list.Add(block);
		}
		return list.ToArray();
	}

    public int AddMaterial(Material material) {
        if (material == null) return -1;
        int id = materials.IndexOf(material);
        if (id == -1) {
            materials.Add(material);
            id = materials.Count - 1;
        }
        return id;
    }


    public List<Block> GetBlocks() {
        return blocks;
    }

    public List<Material> GetMaterials() {
        return materials;
    }
	

    // Stage 6.13.9 - materials required by the complete Tanviir Anvil world.
    private void EnsureTanviirFullPalette() {
        Texture2D atlas=Resources.Load<Texture2D>("BlockSet/Atlases/TanviirFull/Tanviir Full Atlas");
        if(atlas==null){Debug.LogWarning("Stage 6.13.9: Tanviir full atlas missing.");return;}
        Shader shader=Shader.Find("VoxelEngine/Diffuse");if(shader==null)shader=Shader.Find("Standard");if(shader==null)return;
        Material mat=new Material(shader);mat.name="Tanviir Full Runtime Material";mat.mainTexture=atlas;mat.hideFlags=HideFlags.DontSave;
        string[] n={"Tanviir Iron Ore","Tanviir Coal Ore","Tanviir Sponge Stone","Tanviir Lapis Ore","Tanviir Diamond Ore","Tanviir Crystal Block","Tanviir Redstone Ore","Tanviir Clay","Tanviir Nether Brick","Tanviir Runed Stone","Tanviir Portal Stone","Tanviir Lamp Stone","Tanviir Emerald Ore","Tanviir Emerald Block","Tanviir Iron Block","Tanviir Quartz"};
        for(int i=0;i<n.Length;i++)AddTanviirCube(n[i],mat,TanviirUV(i));
        AddTanviirStair("Tanviir Quartz Stair",mat,TanviirUV(15));
        if(FindBlock("Tanviir Dark Railing")==null){ FenceBlock f=ScriptableObject.CreateInstance<FenceBlock>();f.name="Tanviir Dark Railing";f.hideFlags=HideFlags.DontSave; CubeBlock src=FindBlock<CubeBlock>("Tanviir Dark Stone"); if(src!=null)f.face=src.top;blocks.Add(f);}
        // Small flora fallback used by old Minecraft dead-shrub blocks.
        if(FindBlock("Tanviir Dead Shrub")==null){ CubeBlock src=FindBlock<CubeBlock>("Tanviir Old Plank"); if(src!=null){CubeBlock b=ScriptableObject.CreateInstance<CubeBlock>();b.name="Tanviir Dead Shrub";b.hideFlags=HideFlags.DontSave;b.front=src.front;b.back=src.back;b.left=src.left;b.right=src.right;b.top=src.top;b.bottom=src.bottom;blocks.Add(b);}}
        Debug.Log("STAGE 6.13.9: full Tanviir material palette appended; legacy IDs preserved.");
    }


    // The Voxel Box 0.9.9.7h - numbered construction cubes only.
    // IMPORTANT: the old Creative GameObject 01-16 block entries are deliberately NOT created.
    // Their 16 block IDs are reclaimed for Custom Voxel 17-32 so the total block count does not grow by 16.
    private void EnsureVoxelBoxCreativeSlots() {
        // Defensive cleanup for projects/scenes serialized with an earlier 0.5-0.9.9.7g runtime list.
        for(int i=blocks.Count-1;i>=0;i--) {
            Block old=blocks[i];
            if(old is GameObjectBlock && old.name.StartsWith("Creative GameObject ")) blocks.RemoveAt(i);
        }

        Texture2D atlas=Resources.Load<Texture2D>("BlockSet/Atlases/VoxelBoxCreativeSlots/Creative Cube Slots 01-16");
        Shader shader=Shader.Find("VoxelEngine/Diffuse"); if(shader==null) shader=Shader.Find("Standard");
        if(atlas!=null && shader!=null) {
            Material mat=new Material(shader); mat.name="Voxel Box Creative Slots 01-16"; mat.mainTexture=atlas; mat.hideFlags=HideFlags.DontSave;
            for(int i=0;i<16;i++) {
                string n="Creative Cube "+(i+1).ToString("00");
                if(FindBlock(n)!=null) continue;
                CubeBlock b=ScriptableObject.CreateInstance<CubeBlock>(); b.name=n; b.hideFlags=HideFlags.DontSave;
                int col=i%4,row=i/4;
                Rect uv=new Rect(col/4f,1f-(row+1)/4f,1f/4f,1f/4f);
                Face f=LegacyFace(mat,uv); b.front=f;b.back=f;b.left=f;b.right=f;b.top=f;b.bottom=f;
                blocks.Add(b);
            }
        } else Debug.LogWarning("Voxel Box: numbered Creative Cube atlas missing.");
        Debug.Log("THE VOXEL BOX 0.9.9.7h: GO 01-16 removed; their capacity is reclaimed by Custom Voxel 17-32.");
    }

    // The Voxel Box 0.6 - Creative TNT. Appended last so every established ID stays stable.
    private void EnsureVoxelBoxTNT() {
        if(FindBlock("Creative TNT")!=null) return;
        Texture2D tex=Resources.Load<Texture2D>("BlockSet/Atlases/VoxelBoxCreativeSlots/Voxel Box TNT");
        Shader shader=Shader.Find("VoxelEngine/Diffuse"); if(shader==null) shader=Shader.Find("Standard");
        if(tex==null || shader==null) { Debug.LogWarning("Voxel Box 0.6: TNT texture/shader missing."); return; }
        Material mat=new Material(shader); mat.name="Voxel Box Creative TNT"; mat.mainTexture=tex; mat.hideFlags=HideFlags.DontSave;
        CubeBlock b=ScriptableObject.CreateInstance<CubeBlock>(); b.name="Creative TNT"; b.hideFlags=HideFlags.DontSave;
        Face face=LegacyFace(mat,new Rect(0,0,1,1)); b.front=face;b.back=face;b.left=face;b.right=face;b.top=face;b.bottom=face; b.icon=tex;
        blocks.Add(b);
        Debug.Log("THE VOXEL BOX 0.6: Creative TNT appended to BlockSet.");
    }

    // The Voxel Box 0.7 - Copy/Paste selection tools. Appended after all established IDs.
    private void EnsureVoxelBoxCopyPaste() {
        string[] names={"Copy Start","Copy End","Paste Selection"};
        string[] texNames={"Voxel Box Copy Start","Voxel Box Copy End","Voxel Box Paste"};
        Shader shader=Shader.Find("VoxelEngine/Diffuse"); if(shader==null) shader=Shader.Find("Standard");
        if(shader==null) return;
        for(int i=0;i<names.Length;i++) {
            if(FindBlock(names[i])!=null) continue;
            Texture2D tex=Resources.Load<Texture2D>("BlockSet/Atlases/VoxelBoxCreativeSlots/"+texNames[i]);
            if(tex==null) { Debug.LogWarning("Voxel Box 0.7: missing tool texture "+texNames[i]); continue; }
            Material mat=new Material(shader); mat.name="Voxel Box "+names[i]; mat.mainTexture=tex; mat.hideFlags=HideFlags.DontSave;
            CubeBlock b=ScriptableObject.CreateInstance<CubeBlock>(); b.name=names[i]; b.hideFlags=HideFlags.DontSave;
            Face face=LegacyFace(mat,new Rect(0,0,1,1)); b.front=face;b.back=face;b.left=face;b.right=face;b.top=face;b.bottom=face;b.icon=tex;
            blocks.Add(b);
        }
        Debug.Log("THE VOXEL BOX 0.7: Copy Start + Copy End + Paste Selection appended.");
    }

    // The Voxel Box 0.8 - FortressCraft-inspired 8x8x8 custom voxel workshop.
    private void EnsureVoxelBoxCustomWorkshop() {
        Shader shader=Shader.Find("VoxelEngine/Diffuse"); if(shader==null) shader=Shader.Find("Standard");
        string[] tools={"Custom Workshop 8x8","Bake Custom Block"};
        string[] tex={"Voxel Box Workshop","Voxel Box Bake Custom"};
        if(shader!=null) for(int i=0;i<tools.Length;i++) if(FindBlock(tools[i])==null) {
            Texture2D t=Resources.Load<Texture2D>("BlockSet/Atlases/VoxelBoxCreativeSlots/"+tex[i]); if(t==null) continue;
            Material m=new Material(shader);m.name=tools[i];m.mainTexture=t;m.hideFlags=HideFlags.DontSave; CubeBlock b=ScriptableObject.CreateInstance<CubeBlock>();b.name=tools[i];b.hideFlags=HideFlags.DontSave;Face f=LegacyFace(m,new Rect(0,0,1,1));b.front=f;b.back=f;b.left=f;b.right=f;b.top=f;b.bottom=f;b.icon=t;blocks.Add(b);
        }
        for(int i=0;i<32;i++) { string n="Custom Voxel "+(i+1).ToString("00"); if(FindBlock(n)!=null) continue; CustomVoxelBlock b=ScriptableObject.CreateInstance<CustomVoxelBlock>(); b.name=n;b.slot=i;b.hideFlags=HideFlags.DontSave;b.icon=Resources.Load<Texture2D>("BlockSet/Atlases/VoxelBoxCreativeSlots/Custom Voxel "+(i+1).ToString("00"));blocks.Add(b); }
        Debug.Log("THE VOXEL BOX 0.9.9.7h: 32 Custom Voxel slots active; GO 01-16 removed from BlockSet.");
    }

}
