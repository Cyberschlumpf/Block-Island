using UnityEngine;
using System.Collections.Generic;
using System.Text;

// Stage 6.9.9 real voxel palms + full grass vegetation support.
// Infinite-world adaptation of the original L-system tree idea. It uses the same
// Oak/Birch/Pine/Sakura/Red grammar families, but writes into a cached sparse voxel set.
public sealed class LegacyInfiniteTrees {
    struct TreeData { public HashSet<long> wood, leaf; }
    readonly Dictionary<long,TreeData> cache=new Dictionary<long,TreeData>();
    readonly InfiniteTerrainGenerator terrain;
    Block oakWood,oakLeaf,birchWood,birchLeaf,pineWood,pineLeaf,sakuraWood,sakuraLeaf,redWood,redLeaf;
    const int Cell=18;
    // 1.06: Lichtgarten is an authored world. These trees are deliberately placed once at
    // fixed, irregular coordinates across the whole usable garden instead of being generated
    // from a density/raster rule. Water, promenade and monument clearings were excluded when
    // choosing the coordinates. The seed derived from each coordinate still selects different
    // existing tree families, giving a mix of oak/birch/pine/sakura/palm silhouettes.
    static readonly Vector2Int[] LightGardenTrees = {
        new Vector2Int(-54,-241),new Vector2Int(82,-222),new Vector2Int(-90,-172),new Vector2Int(129,-161),
        new Vector2Int(-103,-76),new Vector2Int(85,-59),new Vector2Int(-77,2),new Vector2Int(39,-20),
        new Vector2Int(-98,41),new Vector2Int(102,65),new Vector2Int(111,100),new Vector2Int(-73,171),
        new Vector2Int(-67,231),new Vector2Int(110,203),new Vector2Int(86,248),new Vector2Int(-128,54),
        new Vector2Int(-112,-204),new Vector2Int(-33,262),new Vector2Int(-139,-79),new Vector2Int(-30,0),
        new Vector2Int(-37,-171),new Vector2Int(133,14),new Vector2Int(-3,-196),new Vector2Int(24,172),
        new Vector2Int(-124,-3),new Vector2Int(21,-295),new Vector2Int(125,44),new Vector2Int(139,82),
        new Vector2Int(124,124),new Vector2Int(18,-96),new Vector2Int(16,248),new Vector2Int(63,-39),
        new Vector2Int(-67,-214),new Vector2Int(127,168),new Vector2Int(-32,-278),new Vector2Int(112,-102),
        new Vector2Int(106,146),new Vector2Int(86,-137),new Vector2Int(38,266),new Vector2Int(32,215),
        new Vector2Int(-48,-53),new Vector2Int(-104,159),new Vector2Int(-77,-54),new Vector2Int(82,40),
        new Vector2Int(-57,37),new Vector2Int(-21,168),new Vector2Int(-42,-141),new Vector2Int(36,-220),
        new Vector2Int(80,7),new Vector2Int(-142,82),new Vector2Int(-93,200),new Vector2Int(64,226)
    };
    // 1.05: Lichtgarten uses a broad deterministic lattice. The 1.04 12-voxel lattice
    // created a dense forest/clump near the centre. ~55 voxels gives roughly 40-60
    // trees over the complete 390 x 790 garden footprint, with natural jitter per cell.
    int ActiveCell { get { return terrain.lightGarden ? 55 : Cell; } }
    public LegacyInfiniteTrees(InfiniteTerrainGenerator t){terrain=t;Resolve();}
    void Resolve(){ var b=BlockSet.instance;if(b==null)return;
        oakWood=Find(b,"BI Oak Wood","Wood","Light Wood"); oakLeaf=Find(b,"BI Oak Leaf","Green Leaf","Leaf");
        birchWood=Find(b,"BI Birch Wood","Birch Wood","Light Wood","Wood"); birchLeaf=Find(b,"BI Birch Leaf","Birch Leaf","Green Leaf");
        pineWood=Find(b,"BI Pine Wood","Spruce Wood","Pine Wood","Dark Wood","Wood"); pineLeaf=Find(b,"BI Pine Leaf","Spruce Leaf","Pine Leaf","Green Leaf");
        sakuraWood=Find(b,"Sakura Wood","Wood"); sakuraLeaf=Find(b,"Sakura Leaf","Pink Leaf","Green Leaf");
        redWood=Find(b,"Red Wood","Wood"); redLeaf=Find(b,"Red Leaf","Orange Leaf","Green Leaf"); }
    Block Find(BlockSet b,params string[] n){foreach(string s in n){Block q=b.FindBlock(s);if(q!=null)return q;}return null;}
    static long K(int x,int y,int z){unchecked{return ((long)(x&0x1fffff)<<42)|((long)(y&0x1fffff)<<21)|(uint)(z&0x1fffff);}}
    static long CK(int x,int z){unchecked{return ((long)x<<32)^(uint)z;}}
    uint Seed(int x,int z){unchecked{uint h=(uint)(x*73856093^z*19349663^terrain.seed*83492791);h^=h>>13;h*=1274126177;return h^(h>>16);}}
    float Next(ref uint s){s^=s<<13;s^=s>>17;s^=s<<5;return (s&0xffffff)/16777215f;}
    int Next(ref uint s,int a,int b){return a+Mathf.FloorToInt(Next(ref s)*(b-a));}
    bool Anchor(int gx,int gz,out int x,out int z,out uint seed){
        seed=Seed(gx,gz); int cell=ActiveCell;
        int margin=terrain.lightGarden?7:2; int span=Mathf.Max(1,cell-margin*2);
        x=gx*cell+margin+(int)(seed%(uint)span); z=gz*cell+margin+(int)((seed>>8)%(uint)span);
        // Version 1.05: one jittered anchor per broad cell. IsVegetatedGround removes water,
        // promenade and protected ritual circles, so trees are distributed across both sides
        // and the full north/south extent instead of forming one central wall.
        int threshold=terrain.lightGarden?700:125;
        return (seed%700)<threshold && terrain.IsVegetatedGround(x,z);}
    public bool TryGet(int x,int y,int z,out DataBlock block){
        block=default(DataBlock); if(oakWood==null)Resolve(); long k=K(x,y,z);
        if(terrain.lightGarden){
            // Fixed authored placement: only inspect nearby entries. 24 voxels safely covers
            // the largest existing tree grammar while avoiding a procedural density pass.
            for(int i=0;i<LightGardenTrees.Length;i++){int ax=LightGardenTrees[i].x,az=LightGardenTrees[i].y;if(Mathf.Abs(x-ax)>24||Mathf.Abs(z-az)>24)continue;uint s=Seed(ax,az);TreeData td=Get(ax,az,s);if(td.wood.Contains(k)){block=new DataBlock(WoodFor(s));return true;}if(td.leaf.Contains(k)){block=new DataBlock(LeafFor(s));return true;}}
            return false;
        }
        int cell=ActiveCell;int gx=InfiniteWorldMath.FloorDiv(x,cell),gz=InfiniteWorldMath.FloorDiv(z,cell);
        for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++){int ax,az;uint s;if(!Anchor(gx+dx,gz+dz,out ax,out az,out s))continue;TreeData td=Get(ax,az,s);if(td.wood.Contains(k)){block=new DataBlock(WoodFor(s));return true;}if(td.leaf.Contains(k)){block=new DataBlock(LeafFor(s));return true;}}
        return false;}
    Block WoodFor(uint s){switch((int)(s%5)){case 1:return birchWood??oakWood;case 2:return pineWood??oakWood;case 3:return sakuraWood??oakWood;case 4:return redWood??oakWood;default:return oakWood;}}
    Block LeafFor(uint s){switch((int)(s%5)){case 1:return birchLeaf??oakLeaf;case 2:return pineLeaf??oakLeaf;case 3:return sakuraLeaf??oakLeaf;case 4:return oakLeaf??redLeaf;default:return oakLeaf;}}
    TreeData Get(int x,int z,uint seed){long key=CK(x,z);TreeData d;if(cache.TryGetValue(key,out d))return d;d=new TreeData{wood=new HashSet<long>(),leaf=new HashSet<long>()};int y=terrain.SurfaceY(x,z)+1;Generate(ref d,x,y,z,seed);if(cache.Count>4096)cache.Clear();cache[key]=d;return d;}
    void Generate(ref TreeData d,int x,int y,int z,uint seed){int type=(int)(seed%5); if(type==4){ GeneratePalm(ref d,x,y,z,seed); return; } string ax,rA,rB;float pA=1f,pB=.8f;switch(type){
        case 1: ax="11111AAA";rA="111B";rB="yy[XXX-2-2-2-2-2]BB";break;
        case 2: ax="111111A 1111A 1111A 1111A";rA="y[B]AAA";rB="xxx22222";break;
        case 3: ax="11111111 AAAAA";rA="YY[B]";rB="yxx22222B";pA=.8f;break;
        default: ax="FFFFFFA";rA="[XFFFA] yyyy [XFFFA] yyyy [XFFFA]";rB="[X222A]yyyy[X222A]yyyy[X222A]";break;}
        uint rng=seed;for(int it=0;it<4;it++){StringBuilder sb=new StringBuilder();foreach(char c in ax){if(c=='A'&&Next(ref rng)<pA)sb.Append(rA);else if(c=='B'&&Next(ref rng)<pB)sb.Append(rB);else sb.Append(c);}ax=sb.ToString();}
        Stack<System.Tuple<Vector3,Quaternion>> st=new Stack<System.Tuple<Vector3,Quaternion>>();Vector3 p=new Vector3(x,y,z);Quaternion rot=Quaternion.identity;int angle=40+Next(ref rng,-4,4);
        foreach(char c in ax){if(c=='1'||c=='F'){Wood(ref d,p);if(c=='F'&&st.Count>1)Leaf(ref d,p);p+=rot*Vector3.up;}else if(c=='2'){Leaf(ref d,p);p+=rot*Vector3.up;}else if(c=='-')p+=rot*Vector3.up;else if(c=='[')st.Push(System.Tuple.Create(p,rot));else if(c==']'&&st.Count>0){var q=st.Pop();p=q.Item1;rot=q.Item2;}else if(c=='X')rot*=Quaternion.AngleAxis(angle,Vector3.right);else if(c=='x')rot*=Quaternion.AngleAxis(angle,-Vector3.right);else if(c=='Y')rot*=Quaternion.AngleAxis(angle,Vector3.up);else if(c=='y')rot*=Quaternion.AngleAxis(angle,-Vector3.up);}
    }

    // Stage 6.9.8: the red/orange family is now a real voxel palm instead of a broad L-system canopy.
    // A narrow slightly leaning trunk supports a compact crown with long radial fronds.
    void GeneratePalm(ref TreeData d,int x,int y,int z,uint seed){
        uint rng=seed ^ 0x9e3779b9u;
        int height=10+Next(ref rng,0,6);
        float phase=Next(ref rng)*Mathf.PI*2f;
        Vector3 basePos=new Vector3(x,y,z);
        Vector3 crown=basePos;
        for(int i=0;i<height;i++){
            float t=height<=1?0f:(float)i/(height-1);
            float lean=2.2f*t*t;
            Vector3 p=basePos + Vector3.up*i + new Vector3(Mathf.Cos(phase)*lean,0f,Mathf.Sin(phase)*lean);
            PalmWood(ref d,p); crown=p;
        }
        crown+=Vector3.up; PalmWood(ref d,crown);
        // Small leafy heart.
        Vector3i cv=Vector3i.Round(crown);
        for(int yy=-1;yy<=1;yy++)for(int xx=-1;xx<=1;xx++)for(int zz=-1;zz<=1;zz++)
            if(Mathf.Abs(xx)+Mathf.Abs(zz)+Mathf.Abs(yy)<=2)d.leaf.Add(K(cv.x+xx,cv.y+yy,cv.z+zz));
        int fronds=7+Next(ref rng,0,3);
        float start=Next(ref rng)*360f;
        for(int f=0;f<fronds;f++){
            float a=(start+360f*f/fronds)*Mathf.Deg2Rad;
            int len=6+Next(ref rng,0,3);
            for(int j=1;j<=len;j++){
                float t=(float)j/len;
                float radial=j;
                float droop=0.18f*j + 2.2f*t*t;
                Vector3 fp=crown + new Vector3(Mathf.Cos(a)*radial, 1.0f-droop, Mathf.Sin(a)*radial);
                PalmLeaf(ref d,fp);
            }
        }
        // A few short upper fronds keep the crown from looking flat.
        for(int f=0;f<4;f++){
            float a=(start+45f+90f*f)*Mathf.Deg2Rad;
            for(int j=1;j<=4;j++){
                Vector3 fp=crown+new Vector3(Mathf.Cos(a)*j,2.0f-j*0.35f,Mathf.Sin(a)*j);
                PalmLeaf(ref d,fp);
            }
        }
    }
    void PalmWood(ref TreeData d,Vector3 p){Vector3i v=Vector3i.Round(p);d.wood.Add(K(v.x,v.y,v.z));}
    void PalmLeaf(ref TreeData d,Vector3 p){
        // Keep each frond narrow; the old +/-Z fill made distant palms read as flat umbrellas.
        Vector3i v=Vector3i.Round(p); d.leaf.Add(K(v.x,v.y,v.z));
    }
    void Wood(ref TreeData d,Vector3 p){Vector3i v=Vector3i.Round(p);d.wood.Add(K(v.x,v.y,v.z));d.wood.Add(K(v.x+1,v.y,v.z));d.wood.Add(K(v.x-1,v.y,v.z));d.wood.Add(K(v.x,v.y,v.z+1));d.wood.Add(K(v.x,v.y,v.z-1));}
    void Leaf(ref TreeData d,Vector3 p){Vector3i v=Vector3i.Round(p);for(int yy=-1;yy<=1;yy++)for(int xx=-2;xx<=2;xx++)for(int zz=-2;zz<=2;zz++)if(Mathf.Abs(xx)+Mathf.Abs(zz)+Mathf.Abs(yy)<=4)d.leaf.Add(K(v.x+xx,v.y+yy,v.z+zz));}
}
