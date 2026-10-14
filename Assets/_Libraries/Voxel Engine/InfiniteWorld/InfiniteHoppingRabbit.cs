using UnityEngine;

// Stage 6.12.6: terrain-aware voxel rabbit with hard water safety / dry-land rescue.
public sealed class InfiniteHoppingRabbit : MonoBehaviour {
    public Transform target;
    public InfiniteVoxelWorld world;
    public float stayRadius=32f;
    GameObject rabbit;
    Vector3 hopStart,hopEnd;
    float hopT;
    bool hopping;
    float pause;
    Material fur,belly,eye;

    void Start(){
        if(target==null && Camera.main!=null) target=Camera.main.transform;
        if(world==null) world=GetComponent<InfiniteVoxelWorld>();
        Shader sh=Shader.Find("Standard"); if(sh==null) sh=Shader.Find("Sprites/Default");
        fur=new Material(sh); fur.color=new Color(.55f,.34f,.18f,1f);
        belly=new Material(sh); belly.color=new Color(.82f,.72f,.58f,1f);
        eye=new Material(sh); eye.color=new Color(.035f,.025f,.02f,1f);
        BuildRabbit(); RespawnNearPlayer(); pause=Random.Range(.4f,1.2f);
    }

    GameObject Part(PrimitiveType type,string name,Transform parent,Vector3 pos,Vector3 scale,Material mat){
        var g=GameObject.CreatePrimitive(type); g.name=name; g.transform.SetParent(parent,false);
        g.transform.localPosition=pos; g.transform.localScale=scale;
        var c=g.GetComponent<Collider>(); if(c!=null) Destroy(c);
        var r=g.GetComponent<Renderer>(); if(r!=null) r.sharedMaterial=mat; return g;
    }
    void BuildRabbit(){
        rabbit=new GameObject("Hopping_Rabbit"); rabbit.transform.SetParent(transform,false);
        Part(PrimitiveType.Cube,"Body",rabbit.transform,new Vector3(0,.34f,0),new Vector3(.58f,.48f,.76f),fur);
        Part(PrimitiveType.Cube,"Chest",rabbit.transform,new Vector3(0,.39f,.30f),new Vector3(.34f,.34f,.28f),belly);
        Part(PrimitiveType.Cube,"Head",rabbit.transform,new Vector3(0,.68f,.43f),new Vector3(.44f,.42f,.42f),fur);
        Part(PrimitiveType.Cube,"Muzzle",rabbit.transform,new Vector3(0,.60f,.67f),new Vector3(.28f,.18f,.18f),belly);
        Part(PrimitiveType.Cube,"Tail",rabbit.transform,new Vector3(0,.43f,-.48f),new Vector3(.24f,.24f,.24f),belly);
        var le=Part(PrimitiveType.Cube,"EarL",rabbit.transform,new Vector3(-.13f,1.02f,.40f),new Vector3(.14f,.48f,.12f),fur); le.transform.localRotation=Quaternion.Euler(-8,0,-5);
        var re=Part(PrimitiveType.Cube,"EarR",rabbit.transform,new Vector3(.13f,1.02f,.40f),new Vector3(.14f,.48f,.12f),fur); re.transform.localRotation=Quaternion.Euler(-8,0,5);
        Part(PrimitiveType.Cube,"EyeL",rabbit.transform,new Vector3(-.225f,.75f,.60f),new Vector3(.065f,.065f,.045f),eye);
        Part(PrimitiveType.Cube,"EyeR",rabbit.transform,new Vector3(.225f,.75f,.60f),new Vector3(.065f,.065f,.045f),eye);
        Part(PrimitiveType.Cube,"FootL",rabbit.transform,new Vector3(-.22f,.13f,.20f),new Vector3(.22f,.14f,.36f),fur);
        Part(PrimitiveType.Cube,"FootR",rabbit.transform,new Vector3(.22f,.13f,.20f),new Vector3(.22f,.14f,.36f),fur);
        Part(PrimitiveType.Cube,"HindL",rabbit.transform,new Vector3(-.23f,.22f,-.23f),new Vector3(.25f,.28f,.34f),fur);
        Part(PrimitiveType.Cube,"HindR",rabbit.transform,new Vector3(.23f,.22f,-.23f),new Vector3(.25f,.28f,.34f),fur);
    }
    bool IsWaterColumn(float x,float z){
        if(world==null || world.generator==null) return false;
        int ix=Mathf.FloorToInt(x), iz=Mathf.FloorToInt(z);
        if(TanviirImportWorld.active && TanviirNativeWorld.Available){
            int top=TanviirImportWorld.SurfaceY(ix,iz);
            for(int y=Mathf.Min(255,top+2);y>=Mathf.Max(0,top-3);y--){ ushort id; byte meta; if(TanviirNativeWorld.Raw(ix,y,iz,out id,out meta) && (id==8 || id==9)) return true; }
            return false;
        }
        int sy=world.generator.seaLevel;
        return world.GetBlock(ix,sy,iz).IsFluid();
    }
    float GroundY(float x,float z){
        if(world==null || world.generator==null) return target!=null?target.position.y:0f;
        int ix=Mathf.FloorToInt(x), iz=Mathf.FloorToInt(z);
        // 6.13.17: rabbits in Tanviir must use the imported Minecraft surface, never the
        // procedural island generator. The old path is why rabbits could hop in mid-air.
        if(TanviirImportWorld.active && TanviirNativeWorld.Available){
            int top=Mathf.Clamp(TanviirImportWorld.SurfaceY(ix,iz),0,255);
            for(int y=Mathf.Min(255,top+8);y>=0;y--){
                ushort id; byte meta;
                if(TanviirNativeWorld.Raw(ix,y,iz,out id,out meta) && id!=0 && id!=8 && id!=9 && id!=10 && id!=11) return y+1.02f;
            }
            return top+1.02f;
        }
        int baseY=world.generator.SurfaceY(ix,iz);
        int top2=baseY;
        for(int y=baseY+1;y<=baseY+8;y++) if(!world.GetBlock(ix,y,iz).IsEmpty()) top2=y;
        return top2+1.02f;
    }
    bool TryFindDryLand(Vector3 center,float minRadius,float maxRadius,out Vector3 dry){
        // Deterministic rings first, then random samples. This prevents a rabbit from ever accepting
        // the ocean merely because a limited number of random spawn attempts all hit water.
        for(float r=minRadius;r<=maxRadius;r+=1.5f){
            float offset=Random.Range(0f,360f);
            for(int i=0;i<16;i++){
                float a=(offset+i*(360f/16f))*Mathf.Deg2Rad;
                Vector3 p=new Vector3(center.x+Mathf.Cos(a)*r,0,center.z+Mathf.Sin(a)*r);
                if(!IsWaterColumn(p.x,p.z)){ p.y=GroundY(p.x,p.z); dry=p; return true; }
            }
        }
        for(int i=0;i<96;i++){
            Vector2 v=Random.insideUnitCircle.normalized*Random.Range(minRadius,maxRadius);
            Vector3 p=new Vector3(center.x+v.x,0,center.z+v.y);
            if(!IsWaterColumn(p.x,p.z)){ p.y=GroundY(p.x,p.z); dry=p; return true; }
        }
        dry=center; return false;
    }
    void RespawnNearPlayer(){
        Vector3 c=target!=null?target.position:Vector3.zero;
        Vector3 p;
        if(!TryFindDryLand(c,4f,28f,out p)){
            // Last-resort expanding search: never deliberately place a rabbit on water.
            if(!TryFindDryLand(c,28f,80f,out p)){ rabbit.SetActive(false); hopping=false; return; }
        }
        if(!rabbit.activeSelf) rabbit.SetActive(true);
        rabbit.transform.position=p;
        rabbit.transform.rotation=Quaternion.Euler(0,Random.Range(0,360f),0); hopping=false; pause=Random.Range(.35f,.9f);
    }
    void RescueFromWater(){
        Vector3 p;
        Vector3 center=rabbit.transform.position;
        if(TryFindDryLand(center,1.5f,24f,out p) || (target!=null && TryFindDryLand(target.position,4f,40f,out p))){
            if(!rabbit.activeSelf) rabbit.SetActive(true);
            rabbit.transform.position=p; rabbit.transform.localScale=Vector3.one;
            rabbit.transform.rotation=Quaternion.Euler(0,Random.Range(0,360f),0);
            hopping=false; pause=Random.Range(.5f,1.2f);
        } else {
            // Hide rather than leave an impossible water rabbit visible; the next update retries near player.
            rabbit.SetActive(false); hopping=false; pause=.5f;
        }
    }
    void BeginHop(){
        Vector3 c=target!=null?target.position:rabbit.transform.position;
        Vector3 flat=rabbit.transform.position-c; flat.y=0;
        float yaw=Random.Range(-38f,38f);
        if(flat.magnitude>stayRadius*.72f){ Vector3 toward=c-rabbit.transform.position; toward.y=0; if(toward.sqrMagnitude>.1f) rabbit.transform.rotation=Quaternion.LookRotation(toward.normalized); }
        rabbit.transform.Rotate(0,yaw,0);
        float dist=Random.Range(1.1f,2.3f);
        hopStart=rabbit.transform.position;
        bool found=false;
        // Stage 6.12.4: rabbits are land animals. Probe both the landing point and points along
        // the hop so they cannot cross water by treating its surface as terrain.
        for(int attempt=0;attempt<12 && !found;attempt++){
            Vector3 candidate=hopStart+rabbit.transform.forward*dist;
            bool water=IsWaterColumn(candidate.x,candidate.z);
            for(int q=1;q<=4 && !water;q++){ Vector3 probe=Vector3.Lerp(hopStart,candidate,q/4f); water=IsWaterColumn(probe.x,probe.z); }
            if(!water){ candidate.y=GroundY(candidate.x,candidate.z); if(Mathf.Abs(candidate.y-hopStart.y)<=1.6f){ hopEnd=candidate; found=true; break; } }
            rabbit.transform.Rotate(0,Random.Range(55f,145f),0); dist=Random.Range(.8f,1.7f);
        }
        if(!found){ hopping=false; pause=Random.Range(.25f,.65f); return; }
        hopT=0; hopping=true;
    }
    void Update(){
        if(rabbit==null) return; if(target==null && Camera.main!=null) target=Camera.main.transform;
        // Hard habitat invariant: check continuously, including spawn, teleports and while hopping.
        if(!rabbit.activeSelf){ RespawnNearPlayer(); return; }
        if(IsWaterColumn(rabbit.transform.position.x,rabbit.transform.position.z)){ RescueFromWater(); return; }
        if(target!=null){ Vector3 d=rabbit.transform.position-target.position; d.y=0; if(d.magnitude>stayRadius*1.8f){RespawnNearPlayer();return;} }
        if(!hopping){
            // Keep an idle rabbit glued to the real imported surface as streamed chunks arrive.
            Vector3 rp=rabbit.transform.position; float gy=GroundY(rp.x,rp.z);
            if(Mathf.Abs(rp.y-gy)>.05f){ rp.y=gy; rabbit.transform.position=rp; }
            pause-=Time.deltaTime; if(pause<=0) BeginHop(); return;
        }
        float duration=.52f; hopT+=Time.deltaTime/duration; float t=Mathf.Clamp01(hopT);
        Vector3 p=Vector3.Lerp(hopStart,hopEnd,t); p.y+=Mathf.Sin(t*Mathf.PI)*.72f; rabbit.transform.position=p;
        // A little squash/stretch makes the hop readable without an animation asset.
        float squash=Mathf.Sin(t*Mathf.PI); rabbit.transform.localScale=new Vector3(1f-.07f*squash,1f+.12f*squash,1f-.04f*squash);
        if(t>=1f){ rabbit.transform.position=hopEnd; rabbit.transform.localScale=Vector3.one; hopping=false; pause=Random.Range(.35f,1.15f); }
    }
}
