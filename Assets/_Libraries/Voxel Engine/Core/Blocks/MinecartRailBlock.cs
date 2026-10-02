using UnityEngine;

// Stage 6.10.0: lightweight voxel rail/minecart blocks generated at runtime.
// Straight and curve use the placement direction already stored in DataBlock.
public class MinecartRailBlock : Block {
    public enum RailKind { Straight, Curve, Minecart }
    public RailKind kind;
    public Face face = new Face();

    public override void Init(BlockSet set, int id) {
        // Stage 6.10.3: use the SAME serialized Terrain Atlas material/render path as
        // the proven terrain blocks.  The rail/cart textures are baked into free atlas
        // slots (x=10 and x=12, 256 px each in the 4096x256 atlas).
        // This avoids runtime-only materials/textures that the legacy renderer did not
        // reliably bind to chunk submeshes.
        Material atlasMaterial = Resources.Load<Material>("BlockSet/Atlases/Terrain/Terrain Atlas");
        if(atlasMaterial != null) {
            face.material = atlasMaterial;
            const float tileWidth = 1.0f / 16.0f;
            int tileX = kind == RailKind.Minecart ? 12 : 10;
            face.rect = new Rect(tileX * tileWidth, 0f, tileWidth, 1f);
        } else {
            Debug.LogError("MinecartRailBlock: Terrain Atlas material could not be loaded.");
        }
        base.Init(set,id);
    }

    public override void Build(MeshBuilder b, DataBlock data, LocalPosition pos, int index, Chunk chunk) {
        if(kind == RailKind.Straight) BuildStraight(b,pos,data.direction);
        else if(kind == RailKind.Curve) BuildCurve(b,pos,data.direction);
        else BuildMinecart(b,pos,data.direction);
    }

    public override MeshBuilder Build() { return null; }
    public override Face GetPreviewFace() { return face; }
    public override bool IsSolid() { return kind == RailKind.Minecart; }
    public override bool IsAlpha() { return true; }

    void BuildStraight(MeshBuilder b, Vector3 p, BlockDirection d) {
        // sleepers plus two raised metal rails; whole model rotates with placement direction
        Box(b,p,d,new Vector3(.50f,.055f,.50f),new Vector3(.96f,.11f,.18f));
        Box(b,p,d,new Vector3(.50f,.055f,.18f),new Vector3(.96f,.11f,.12f));
        Box(b,p,d,new Vector3(.50f,.055f,.82f),new Vector3(.96f,.11f,.12f));
        Box(b,p,d,new Vector3(.50f,.135f,.29f),new Vector3(.98f,.07f,.075f));
        Box(b,p,d,new Vector3(.50f,.135f,.71f),new Vector3(.98f,.07f,.075f));
    }

    void BuildCurve(MeshBuilder b, Vector3 p, BlockDirection d) {
        // quarter-turn made from short voxel rail segments; rotation selects the four curve orientations.
        Box(b,p,d,new Vector3(.18f,.055f,.50f),new Vector3(.12f,.11f,.92f));
        Box(b,p,d,new Vector3(.50f,.055f,.82f),new Vector3(.92f,.11f,.12f));
        Box(b,p,d,new Vector3(.28f,.135f,.29f),new Vector3(.075f,.07f,.55f));
        Box(b,p,d,new Vector3(.40f,.135f,.70f),new Vector3(.55f,.07f,.075f));
        Box(b,p,d,new Vector3(.44f,.135f,.44f),new Vector3(.075f,.07f,.32f));
        Box(b,p,d,new Vector3(.58f,.135f,.58f),new Vector3(.32f,.07f,.075f));
    }

    void BuildMinecart(MeshBuilder b, Vector3 p, BlockDirection d) {
        // Low open voxel cart, centred on a rail. Front/back are visually distinct by the lip.
        Box(b,p,d,new Vector3(.50f,.28f,.50f),new Vector3(.82f,.12f,.72f));
        Box(b,p,d,new Vector3(.12f,.52f,.50f),new Vector3(.12f,.48f,.72f));
        Box(b,p,d,new Vector3(.88f,.52f,.50f),new Vector3(.12f,.48f,.72f));
        Box(b,p,d,new Vector3(.50f,.52f,.14f),new Vector3(.64f,.48f,.12f));
        Box(b,p,d,new Vector3(.50f,.52f,.86f),new Vector3(.64f,.48f,.12f));
        Box(b,p,d,new Vector3(.50f,.76f,.08f),new Vector3(.30f,.08f,.08f)); // direction marker/front lip
        Box(b,p,d,new Vector3(.20f,.13f,.22f),new Vector3(.16f,.16f,.16f));
        Box(b,p,d,new Vector3(.80f,.13f,.22f),new Vector3(.16f,.16f,.16f));
        Box(b,p,d,new Vector3(.20f,.13f,.78f),new Vector3(.16f,.16f,.16f));
        Box(b,p,d,new Vector3(.80f,.13f,.78f),new Vector3(.16f,.16f,.16f));
    }

    void Box(MeshBuilder b, Vector3 p, BlockDirection d, Vector3 c, Vector3 s) {
        Vector3 mn=c-s*.5f, mx=c+s*.5f;
        Vector3[] v = {
            new Vector3(mn.x,mn.y,mn.z),new Vector3(mn.x,mx.y,mn.z),new Vector3(mx.x,mx.y,mn.z),new Vector3(mx.x,mn.y,mn.z),
            new Vector3(mx.x,mn.y,mx.z),new Vector3(mx.x,mx.y,mx.z),new Vector3(mn.x,mx.y,mx.z),new Vector3(mn.x,mn.y,mx.z),
            new Vector3(mn.x,mn.y,mx.z),new Vector3(mn.x,mx.y,mx.z),new Vector3(mn.x,mx.y,mn.z),new Vector3(mn.x,mn.y,mn.z),
            new Vector3(mx.x,mn.y,mn.z),new Vector3(mx.x,mx.y,mn.z),new Vector3(mx.x,mx.y,mx.z),new Vector3(mx.x,mn.y,mx.z),
            new Vector3(mn.x,mx.y,mn.z),new Vector3(mn.x,mx.y,mx.z),new Vector3(mx.x,mx.y,mx.z),new Vector3(mx.x,mx.y,mn.z),
            new Vector3(mn.x,mn.y,mx.z),new Vector3(mn.x,mn.y,mn.z),new Vector3(mx.x,mn.y,mn.z),new Vector3(mx.x,mn.y,mx.z)
        };
        Vector3[] n={Vector3.back,Vector3.back,Vector3.back,Vector3.back, Vector3.forward,Vector3.forward,Vector3.forward,Vector3.forward, Vector3.left,Vector3.left,Vector3.left,Vector3.left, Vector3.right,Vector3.right,Vector3.right,Vector3.right, Vector3.up,Vector3.up,Vector3.up,Vector3.up, Vector3.down,Vector3.down,Vector3.down,Vector3.down};
        int[] tri={0,1,2,0,2,3,4,5,6,4,6,7,8,9,10,8,10,11,12,13,14,12,14,15,16,17,18,16,18,19,20,21,22,20,22,23};
        Vector2[] uv=new Vector2[24]; for(int i=0;i<24;i++){int q=i&3; uv[i]=q==0?new Vector2(0,0):q==1?new Vector2(0,1):q==2?new Vector2(1,1):new Vector2(1,0);}
        b.AddIndices(tri,face.materialID); b.AddVertices(v,p,d); b.AddNormals(n,d); b.AddTexCoords(uv,face.rect);
    }
}
