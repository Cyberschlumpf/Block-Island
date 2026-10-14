using UnityEngine;

// Stage 6.10.0: lightweight voxel rail/minecart blocks generated at runtime.
// Straight and curve use the placement direction already stored in DataBlock.
public class MinecartRailBlock : Block {
    public enum RailKind { Straight, Curve, Minecart, Locomotive }
    public RailKind kind;
    public Face face = new Face();

    // Own 8-tile atlas (Resources/Minecart/Minecart Atlas.png): 0 wood sleeper, 1 steel rail, 2 cart body, 3 wheel,
    // 4 loco boiler, 5 loco cab, 6 brass, 7 black.
    // The previous version borrowed tiles from the Terrain Atlas and broke when that atlas changed size.
    const int Wood = 0, Steel = 1, Body = 2, Wheel = 3, Boiler = 4, Cab = 5, Brass = 6, Black = 7;
    const int Tiles = 8;
    static Material atlasMaterial;

    // Rail geometry shared by straight and curve so that track pieces line up:
    // rails sit .21 left/right of the track centre line, sleepers are .92 wide.
    const float RailOffset = .21f, RailW = .075f, RailH = .07f, RailY = .135f, SleeperY = .055f, SleeperH = .11f;

    public bool IsVehicle { get { return kind == RailKind.Minecart || kind == RailKind.Locomotive; } }

    public override void Init(BlockSet set, int id) {
        if(atlasMaterial == null) {
            Texture2D tex = Resources.Load<Texture2D>("Minecart/Minecart Atlas");
            Shader shader = Shader.Find("VoxelEngine/Diffuse"); if(shader == null) shader = Shader.Find("Standard");
            if(tex != null && shader != null) {
                tex.filterMode = FilterMode.Point;
                atlasMaterial = new Material(shader); atlasMaterial.name = "Minecart Atlas Runtime Material";
                atlasMaterial.mainTexture = tex; atlasMaterial.hideFlags = HideFlags.DontSave;
            } else Debug.LogError("MinecartRailBlock: Minecart Atlas could not be loaded.");
        }
        face.material = atlasMaterial;
        face.rect = Tile(kind == RailKind.Minecart ? Body : kind == RailKind.Locomotive ? Boiler : Steel);
        icon = Resources.Load<Texture2D>(kind == RailKind.Straight ? "Minecart/Icon Rail Straight" : kind == RailKind.Curve ? "Minecart/Icon Rail Curve"
                                       : kind == RailKind.Locomotive ? "Minecart/Icon Locomotive" : "Minecart/Icon Minecart");
        base.Init(set,id);
    }

    static Rect Tile(int i) { return new Rect(i / (float)Tiles, 0f, 1f / Tiles, 1f); }

    public override void Build(MeshBuilder b, DataBlock data, LocalPosition pos, int index, Chunk chunk) {
        if(kind == RailKind.Straight) BuildStraight(b,pos,data.direction);
        else if(kind == RailKind.Curve) BuildCurve(b,pos,data.direction);
        else if(kind == RailKind.Locomotive) BuildLocomotive(b,pos,data.direction);
        else BuildMinecart(b,pos,data.direction);
    }

    public override MeshBuilder Build() { return null; }
    public override Face GetPreviewFace() { return face; }
    public override bool IsSolid() { return IsVehicle; }
    public override bool IsAlpha() { return true; }

    void BuildStraight(MeshBuilder b, Vector3 p, BlockDirection d) {
        // Rails run along local z = the direction the player looks when placing (the placement direction
        // points back at the player). Rails are exactly one block long, so neighbouring pieces join seamlessly;
        // sleepers at .17/.50/.83 keep an even spacing across block borders.
        for(int i = 0; i < 3; i++) Box(b,p,d,Wood,new Vector3(.5f,SleeperY,.17f+i*.33f),new Vector3(.92f,SleeperH,.16f));
        Box(b,p,d,Steel,new Vector3(.5f-RailOffset,RailY,.5f),new Vector3(RailW,RailH,1f));
        Box(b,p,d,Steel,new Vector3(.5f+RailOffset,RailY,.5f),new Vector3(RailW,RailH,1f));
    }

    void BuildCurve(MeshBuilder b, Vector3 p, BlockDirection d) {
        // Quarter circle like the inventory icon: track centre line has radius .5 around the corner (1,1),
        // so it enters at the middle of the z=1 edge and leaves at the middle of the x=1 edge, exactly where
        // straight rails end. Rails are built from short rotated segments, sleepers point to the centre.
        Vector3 c = new Vector3(1f, 0f, 1f);
        const int Segments = 9, Sleepers = 4;
        float step = 90f / Segments;
        for(int k = 0; k < Sleepers; k++) {
            float a = (k + .5f) * 90f / Sleepers;
            Vector3 m = c + Radial(a) * .5f; m.y = SleeperY;
            BoxRot(b,p,d,Wood,m,new Vector3(.92f,SleeperH,.15f),-a);
        }
        foreach(float r in new[] { .5f - RailOffset, .5f + RailOffset }) {
            float len = 2f * r * Mathf.Sin(step * .5f * Mathf.Deg2Rad) + .02f;   // small overlap: no gaps between segments
            for(int k = 0; k < Segments; k++) {
                float a = (k + .5f) * step;
                Vector3 m = c + Radial(a) * r; m.y = RailY;
                BoxRot(b,p,d,Steel,m,new Vector3(RailW,RailH,len),-a);
            }
        }
    }

    // Direction from the curve centre (1,1) towards the track: 0 deg = towards -x, 90 deg = towards -z.
    static Vector3 Radial(float deg) { float r = deg * Mathf.Deg2Rad; return new Vector3(-Mathf.Cos(r), 0f, -Mathf.Sin(r)); }

    void BuildMinecart(MeshBuilder b, Vector3 p, BlockDirection d) {
        // Low open voxel cart, centred on a rail. Front/back are visually distinct by the lip.
        Box(b,p,d,Body,new Vector3(.50f,.28f,.50f),new Vector3(.82f,.12f,.72f));
        Box(b,p,d,Body,new Vector3(.12f,.52f,.50f),new Vector3(.12f,.48f,.72f));
        Box(b,p,d,Body,new Vector3(.88f,.52f,.50f),new Vector3(.12f,.48f,.72f));
        Box(b,p,d,Body,new Vector3(.50f,.52f,.14f),new Vector3(.64f,.48f,.12f));
        Box(b,p,d,Body,new Vector3(.50f,.52f,.86f),new Vector3(.64f,.48f,.12f));
        Box(b,p,d,Steel,new Vector3(.50f,.76f,.08f),new Vector3(.30f,.08f,.08f)); // direction marker/front lip
        Box(b,p,d,Wheel,new Vector3(.20f,.13f,.22f),new Vector3(.16f,.16f,.16f));
        Box(b,p,d,Wheel,new Vector3(.80f,.13f,.22f),new Vector3(.16f,.16f,.16f));
        Box(b,p,d,Wheel,new Vector3(.20f,.13f,.78f),new Vector3(.16f,.16f,.16f));
        Box(b,p,d,Wheel,new Vector3(.80f,.13f,.78f),new Vector3(.16f,.16f,.16f));
    }

    void BuildLocomotive(MeshBuilder b, Vector3 p, BlockDirection d) {
        // Small steam locomotive; front (smoke box, chimney) at low z like the minecart's front lip.
        Box(b,p,d,Black,new Vector3(.50f,.20f,.50f),new Vector3(.62f,.08f,.96f));   // frame
        Box(b,p,d,Brass,new Vector3(.50f,.20f,.02f),new Vector3(.56f,.06f,.04f));   // front buffer beam
        Box(b,p,d,Boiler,new Vector3(.50f,.45f,.37f),new Vector3(.44f,.40f,.58f));  // boiler
        Box(b,p,d,Black,new Vector3(.50f,.45f,.07f),new Vector3(.46f,.42f,.04f));   // smoke box door
        Box(b,p,d,Brass,new Vector3(.50f,.45f,.24f),new Vector3(.46f,.42f,.03f));   // brass bands
        Box(b,p,d,Brass,new Vector3(.50f,.45f,.50f),new Vector3(.46f,.42f,.03f));
        Box(b,p,d,Black,new Vector3(.50f,.77f,.16f),new Vector3(.13f,.24f,.13f));   // chimney
        Box(b,p,d,Black,new Vector3(.50f,.90f,.16f),new Vector3(.19f,.04f,.19f));   // chimney cap
        Box(b,p,d,Brass,new Vector3(.50f,.70f,.42f),new Vector3(.13f,.10f,.13f));   // steam dome
        Box(b,p,d,Cab,new Vector3(.50f,.52f,.81f),new Vector3(.62f,.56f,.34f));     // cab
        Box(b,p,d,Black,new Vector3(.50f,.83f,.81f),new Vector3(.70f,.06f,.42f));   // cab roof
        foreach(float x in new[] { .17f, .83f })
            foreach(float z in new[] { .20f, .46f, .76f })
                Box(b,p,d,Wheel,new Vector3(x,.14f,z),new Vector3(.08f,.24f,.24f)); // wheels
        Box(b,p,d,Steel,new Vector3(.12f,.14f,.48f),new Vector3(.03f,.04f,.60f));   // coupling rods
        Box(b,p,d,Steel,new Vector3(.88f,.14f,.48f),new Vector3(.03f,.04f,.60f));
    }

    void Box(MeshBuilder b, Vector3 p, BlockDirection d, int tile, Vector3 c, Vector3 s) { BoxRot(b,p,d,tile,c,s,0f); }

    // Box of size s centred at c, turned by yaw degrees around the vertical axis (inside the block),
    // then rotated with the placement direction by MeshBuilder.
    void BoxRot(MeshBuilder b, Vector3 p, BlockDirection d, int tile, Vector3 c, Vector3 s, float yaw) {
        Vector3 mn=-s*.5f, mx=s*.5f;
        Vector3[] v = {
            new Vector3(mn.x,mn.y,mn.z),new Vector3(mn.x,mx.y,mn.z),new Vector3(mx.x,mx.y,mn.z),new Vector3(mx.x,mn.y,mn.z),
            new Vector3(mx.x,mn.y,mx.z),new Vector3(mx.x,mx.y,mx.z),new Vector3(mn.x,mx.y,mx.z),new Vector3(mn.x,mn.y,mx.z),
            new Vector3(mn.x,mn.y,mx.z),new Vector3(mn.x,mx.y,mx.z),new Vector3(mn.x,mx.y,mn.z),new Vector3(mn.x,mn.y,mn.z),
            new Vector3(mx.x,mn.y,mn.z),new Vector3(mx.x,mx.y,mn.z),new Vector3(mx.x,mx.y,mx.z),new Vector3(mx.x,mn.y,mx.z),
            new Vector3(mn.x,mx.y,mn.z),new Vector3(mn.x,mx.y,mx.z),new Vector3(mx.x,mx.y,mx.z),new Vector3(mx.x,mx.y,mn.z),
            new Vector3(mn.x,mn.y,mx.z),new Vector3(mn.x,mn.y,mn.z),new Vector3(mx.x,mn.y,mn.z),new Vector3(mx.x,mn.y,mx.z)
        };
        Vector3[] n={Vector3.back,Vector3.back,Vector3.back,Vector3.back, Vector3.forward,Vector3.forward,Vector3.forward,Vector3.forward, Vector3.left,Vector3.left,Vector3.left,Vector3.left, Vector3.right,Vector3.right,Vector3.right,Vector3.right, Vector3.up,Vector3.up,Vector3.up,Vector3.up, Vector3.down,Vector3.down,Vector3.down,Vector3.down};
        Quaternion q = Quaternion.Euler(0f, yaw, 0f);
        for(int i=0;i<24;i++) { v[i] = c + q * v[i]; n[i] = q * n[i]; }
        int[] tri={0,1,2,0,2,3,4,5,6,4,6,7,8,9,10,8,10,11,12,13,14,12,14,15,16,17,18,16,18,19,20,21,22,20,22,23};
        Vector2[] uv=new Vector2[24]; for(int i=0;i<24;i++){int k=i&3; uv[i]=k==0?new Vector2(0,0):k==1?new Vector2(0,1):k==2?new Vector2(1,1):new Vector2(1,0);}
        b.AddIndices(tri,face.materialID); b.AddVertices(v,p,d); b.AddNormals(n,d); b.AddTexCoords(uv,face.material==atlasMaterial?Tile(tile):face.rect);   // Block-Designer texture overrides the whole model
    }
}
