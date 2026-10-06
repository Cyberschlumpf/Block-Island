using UnityEngine;
using System.Collections.Generic;

// 1.0.6: deterministic world wildlife. Animals belong to world cells, never to the player.
// The player only controls which fixed cells are active for performance.
public sealed class InfiniteWorldWildlife : MonoBehaviour {
    public InfiniteVoxelWorld world;
    public Transform target;
    public int cellSize=64;
    public int activeCellRadius=3;
    public int worldSeed=1337;
    public float spawnChance=.38f;
    public int maxAnimalsPerCell=2;
    public float updateInterval=1.25f;

    readonly Dictionary<long,List<GameObject>> active=new Dictionary<long,List<GameObject>>();
    float timer;
    static readonly string[] species={"Rabbit"};

    void Start(){ if(world==null) world=GetComponent<InfiniteVoxelWorld>(); if(target==null && Camera.main!=null) target=Camera.main.transform; }
    static long Key(int x,int z){ return ((long)x<<32) ^ (uint)z; }
    static uint Hash(int x,int z,int salt){
        unchecked { uint h=(uint)(x*374761393 + z*668265263 + salt*1442695041); h=(h^(h>>13))*1274126177u; return h^(h>>16); }
    }
    static float H01(int x,int z,int salt){ return (Hash(x,z,salt)&0x00ffffff)/16777215f; }

    void Update(){
        timer-=Time.deltaTime; if(timer>0) return; timer=updateInterval;
        if(target==null && Camera.main!=null) target=Camera.main.transform;
        if(target==null || world==null || world.generator==null) return;
        int cx=Mathf.FloorToInt(target.position.x/cellSize), cz=Mathf.FloorToInt(target.position.z/cellSize);
        var wanted=new HashSet<long>();
        for(int dz=-activeCellRadius;dz<=activeCellRadius;dz++) for(int dx=-activeCellRadius;dx<=activeCellRadius;dx++){
            int x=cx+dx,z=cz+dz; long k=Key(x,z); wanted.Add(k);
            if(!active.ContainsKey(k)) ActivateCell(x,z,k);
        }
        var remove=new List<long>();
        foreach(var kv in active) if(!wanted.Contains(kv.Key)){ foreach(var g in kv.Value) if(g!=null) Destroy(g); remove.Add(kv.Key); }
        foreach(var k in remove) active.Remove(k);
    }

    void ActivateCell(int cx,int cz,long key){
        var list=new List<GameObject>(); active[key]=list;
        // Empty wildlife cells are intentional: animals are scattered through the world,
        // not guaranteed around the player.
        if(H01(cx,cz,worldSeed)<1f-spawnChance) return;
        int count=1+(int)(H01(cx,cz,worldSeed+1)*maxAnimalsPerCell);
        for(int i=0;i<count;i++){
            int salt=worldSeed+17+i*19;
            float x=(cx+0.12f+H01(cx,cz,salt)*.76f)*cellSize;
            float z=(cz+0.12f+H01(cx,cz,salt+1)*.76f)*cellSize;
            if(IsWater(x,z)) continue;
            string name=species[(int)(H01(cx,cz,salt+2)*species.Length)%species.Length];
            GameObject prefab=Resources.Load<GameObject>("VoxelBoxWildlife/Prefabs/Animals/"+name);
            if(prefab==null) continue;
            Vector3 p=new Vector3(x,GroundY(x,z),z);
            GameObject g=Instantiate(prefab,p,Quaternion.Euler(0,H01(cx,cz,salt+3)*360f,0),transform);
            g.name="World_"+name+"_"+cx+"_"+cz+"_"+i;
            var a=g.AddComponent<WorldAnimalWander>(); a.world=world; a.home=p; a.seed=(int)Hash(cx,cz,salt+4);
            list.Add(g);
        }
    }

    bool IsWater(float x,float z){
        int ix=Mathf.FloorToInt(x),iz=Mathf.FloorToInt(z);
        if(TanviirImportWorld.active && TanviirNativeWorld.Available){
            int top=TanviirImportWorld.SurfaceY(ix,iz);
            for(int y=Mathf.Min(255,top+2);y>=Mathf.Max(0,top-3);y--){ ushort id; byte meta; if(TanviirNativeWorld.Raw(ix,y,iz,out id,out meta)&&(id==8||id==9)) return true; }
            return false;
        }
        int sy=world.generator.seaLevel; return world.GetBlock(ix,sy,iz).IsFluid();
    }
    float GroundY(float x,float z){
        int ix=Mathf.FloorToInt(x),iz=Mathf.FloorToInt(z);
        if(TanviirImportWorld.active && TanviirNativeWorld.Available){
            int top=Mathf.Clamp(TanviirImportWorld.SurfaceY(ix,iz),0,255);
            for(int y=Mathf.Min(255,top+8);y>=0;y--){ ushort id; byte meta; if(TanviirNativeWorld.Raw(ix,y,iz,out id,out meta)&&id!=0&&id!=8&&id!=9&&id!=10&&id!=11) return y+1.01f; }
            return top+1.01f;
        }
        int baseY=world.generator.SurfaceY(ix,iz),top2=baseY;
        for(int y=baseY+1;y<=baseY+8;y++) if(!world.GetBlock(ix,y,iz).IsEmpty()) top2=y;
        return top2+1.01f;
    }
}

public sealed class WorldAnimalWander : MonoBehaviour {
    public InfiniteVoxelWorld world; public Vector3 home; public int seed;
    public float homeRadius=18f, speed=1.15f;
    Vector3 destination; float wait; bool moving;

    void Start(){ Choose(); }
    bool Water(float x,float z){
        if(world==null||world.generator==null)return true; int ix=Mathf.FloorToInt(x),iz=Mathf.FloorToInt(z);
        if(TanviirImportWorld.active&&TanviirNativeWorld.Available){int top=TanviirImportWorld.SurfaceY(ix,iz);for(int y=Mathf.Min(255,top+2);y>=Mathf.Max(0,top-3);y--){ushort id;byte m;if(TanviirNativeWorld.Raw(ix,y,iz,out id,out m)&&(id==8||id==9))return true;}return false;}
        return world.GetBlock(ix,world.generator.seaLevel,iz).IsFluid();
    }
    float Ground(float x,float z){
        int ix=Mathf.FloorToInt(x),iz=Mathf.FloorToInt(z);
        if(TanviirImportWorld.active&&TanviirNativeWorld.Available){int top=Mathf.Clamp(TanviirImportWorld.SurfaceY(ix,iz),0,255);for(int y=Mathf.Min(255,top+8);y>=0;y--){ushort id;byte m;if(TanviirNativeWorld.Raw(ix,y,iz,out id,out m)&&id!=0&&id!=8&&id!=9&&id!=10&&id!=11)return y+1.01f;}return top+1.01f;}
        int b=world.generator.SurfaceY(ix,iz),t=b;for(int y=b+1;y<=b+8;y++)if(!world.GetBlock(ix,y,iz).IsEmpty())t=y;return t+1.01f;
    }
    void Choose(){
        var old=Random.state; Random.InitState(seed++); moving=false;
        for(int i=0;i<12;i++){Vector2 v=Random.insideUnitCircle*homeRadius;Vector3 p=home+new Vector3(v.x,0,v.y);if(Water(p.x,p.z))continue;p.y=Ground(p.x,p.z);if(Mathf.Abs(p.y-transform.position.y)>2.1f)continue;destination=p;moving=true;break;}
        wait=Random.Range(1.5f,5f); Random.state=old;
    }
    void Update(){
        if(world==null||world.generator==null)return;
        if(!moving){wait-=Time.deltaTime;if(wait<=0)Choose();return;}
        Vector3 d=destination-transform.position;d.y=0;if(d.sqrMagnitude<.16f){moving=false;wait=2f;return;}
        Vector3 dir=d.normalized; transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(dir),Time.deltaTime*4f);
        Vector3 p=transform.position+dir*speed*Time.deltaTime;
        if(Water(p.x,p.z)){moving=false;wait=.5f;return;} p.y=Ground(p.x,p.z); transform.position=p;
    }
}
