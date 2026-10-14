using UnityEngine;

// Stage 6.12.4: water-only voxel wildlife. Fish swim below sea level and occasionally jump.
public sealed class InfiniteVoxelFish : MonoBehaviour {
    public InfiniteVoxelWorld world;
    public Transform target;
    public int fishCount=7;
    class Fish { public GameObject go; public float speed,turn,jumpWait,jumpT; public bool jumping; public Vector3 jumpStart,jumpEnd; }
    Fish[] fish;
    Material[] mats;

    void Start(){
        if(world==null) world=GetComponent<InfiniteVoxelWorld>();
        if(target==null && Camera.main!=null) target=Camera.main.transform;
        Shader sh=Shader.Find("Standard"); if(sh==null) sh=Shader.Find("Sprites/Default");
        Color[] cs={new Color(.85f,.32f,.12f),new Color(.12f,.48f,.72f),new Color(.88f,.68f,.16f),new Color(.32f,.62f,.34f),new Color(.62f,.28f,.65f)};
        mats=new Material[cs.Length]; for(int i=0;i<cs.Length;i++){mats[i]=new Material(sh);mats[i].color=cs[i];}
        fish=new Fish[fishCount]; for(int i=0;i<fish.Length;i++){fish[i]=new Fish(); Build(fish[i],i); Respawn(fish[i]);}
    }
    GameObject Cube(string n,Transform p,Vector3 lp,Vector3 sc,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=lp;g.transform.localScale=sc;var c=g.GetComponent<Collider>();if(c)Destroy(c);g.GetComponent<Renderer>().sharedMaterial=m;return g;}
    void Build(Fish f,int i){
        f.go=new GameObject("Voxel_Fish_"+(i+1)); f.go.transform.SetParent(transform,false); Material m=mats[i%mats.Length];
        Cube("Body",f.go.transform,Vector3.zero,new Vector3(.58f,.28f,.25f),m);
        var tail=Cube("Tail",f.go.transform,new Vector3(-.39f,0,0),new Vector3(.22f,.36f,.08f),m); tail.transform.localRotation=Quaternion.Euler(0,0,45);
        Cube("Head",f.go.transform,new Vector3(.34f,.02f,0),new Vector3(.22f,.23f,.22f),m);
        f.speed=Random.Range(.9f,1.8f); f.turn=Random.Range(-18f,18f); f.jumpWait=Random.Range(8f,24f);
    }
    bool Water(float x,float y,float z){return world!=null && world.GetBlock(Mathf.FloorToInt(x),Mathf.FloorToInt(y),Mathf.FloorToInt(z)).IsFluid();}
    bool FindWaterNear(out Vector3 p){
        Vector3 c=target?target.position:Vector3.zero; int sea=world.generator.seaLevel;
        for(int n=0;n<80;n++){Vector2 r=Random.insideUnitCircle*Random.Range(7f,34f);float x=c.x+r.x,z=c.z+r.y;float y=sea-Random.Range(.7f,3.2f);if(Water(x,sea-.2f,z)&&Water(x,y,z)){p=new Vector3(x,y,z);return true;}}
        p=new Vector3(c.x,sea-1.2f,c.z);return false;
    }
    void Respawn(Fish f){Vector3 p;if(!FindWaterNear(out p)){f.go.SetActive(false);return;}f.go.SetActive(true);f.go.transform.position=p;f.go.transform.rotation=Quaternion.Euler(0,Random.Range(0,360f),0);f.jumpWait=Random.Range(7f,25f);f.jumping=false;}
    void BeginJump(Fish f){
        int sea=world.generator.seaLevel; Vector3 s=f.go.transform.position; s.y=sea+.05f; Vector3 e=s+f.go.transform.forward*Random.Range(1.2f,2.4f); e.y=s.y;
        // Only jump when the complete landing area is still water.
        if(!Water(e.x,sea-.2f,e.z)){f.go.transform.Rotate(0,Random.Range(90f,220f),0);f.jumpWait=Random.Range(5f,16f);return;}
        f.jumpStart=s;f.jumpEnd=e;f.jumpT=0;f.jumping=true;
    }
    void Update(){
        if(world==null||world.generator==null)return;if(target==null&&Camera.main)target=Camera.main.transform;int sea=world.generator.seaLevel;
        foreach(Fish f in fish){if(f==null||f.go==null)continue;if(!f.go.activeSelf){Respawn(f);continue;}
            if(f.jumping){f.jumpT+=Time.deltaTime/.85f;float t=Mathf.Clamp01(f.jumpT);Vector3 p=Vector3.Lerp(f.jumpStart,f.jumpEnd,t);p.y+=Mathf.Sin(t*Mathf.PI)*1.15f;f.go.transform.position=p;f.go.transform.Rotate(0,0,Time.deltaTime*180f);if(t>=1){f.jumping=false;f.go.transform.position=new Vector3(f.jumpEnd.x,sea-.75f,f.jumpEnd.z);f.go.transform.rotation=Quaternion.Euler(0,f.go.transform.eulerAngles.y,0);f.jumpWait=Random.Range(10f,30f);}continue;}
            Vector3 pos=f.go.transform.position; Vector3 next=pos+f.go.transform.forward*f.speed*Time.deltaTime; next.y=Mathf.Clamp(next.y+Mathf.Sin(Time.time*.8f+f.speed)*Time.deltaTime*.12f,sea-3.4f,sea-.55f);
            if(!Water(next.x,next.y,next.z)||!Water(next.x,sea-.2f,next.z)){f.go.transform.Rotate(0,Random.Range(90f,240f),0);f.turn=Random.Range(-20f,20f);}else{f.go.transform.position=next;f.go.transform.Rotate(0,f.turn*Time.deltaTime,0);}
            f.jumpWait-=Time.deltaTime;if(f.jumpWait<=0)BeginJump(f);
            if(target){Vector3 d=f.go.transform.position-target.position;d.y=0;if(d.magnitude>55f)Respawn(f);}
        }
    }
}
