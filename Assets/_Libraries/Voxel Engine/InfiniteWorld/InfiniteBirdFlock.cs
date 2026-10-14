using System.Collections.Generic;
using UnityEngine;

// Stage 6.12.0: lightweight ambient wildlife for the infinite world.
public sealed class InfiniteBirdFlock : MonoBehaviour {
    public Transform target;
    public int birdCount = 8;
    public float minRadius = 18f, maxRadius = 55f;
    public float minHeight = 12f, maxHeight = 28f;
    readonly List<Bird> birds = new List<Bird>();
    Material[] birdMaterials;

    sealed class Bird {
        public GameObject root; public Transform leftWing, rightWing;
        public float speed, turn, flap, phase, flightHeight; public Material material;
    }

    void Start() {
        if(target==null && Camera.main!=null) target=Camera.main.transform;
        Shader sh=Shader.Find("Standard"); if(sh==null) sh=Shader.Find("Sprites/Default");
        Color[] palette={
            new Color(.10f,.10f,.09f,1f), new Color(.32f,.16f,.07f,1f),
            new Color(.16f,.30f,.48f,1f), new Color(.62f,.18f,.12f,1f),
            new Color(.72f,.58f,.18f,1f), new Color(.24f,.42f,.25f,1f),
            new Color(.72f,.72f,.68f,1f), new Color(.36f,.24f,.42f,1f)
        };
        birdMaterials=new Material[palette.Length];
        for(int i=0;i<palette.Length;i++){ birdMaterials[i]=new Material(sh); birdMaterials[i].color=palette[i]; }
        for(int i=0;i<birdCount;i++) SpawnBird(i);
    }

    void SpawnBird(int i) {
        Vector3 center=target!=null?target.position:Vector3.zero;
        float a=(i/(float)Mathf.Max(1,birdCount))*Mathf.PI*2f + Random.Range(-0.25f,0.25f);
        float r=Random.Range(minRadius,maxRadius);
        var b=new Bird();
        b.root=new GameObject("Bird_"+(i+1)); b.root.transform.SetParent(transform,false);
        b.root.transform.position=center+new Vector3(Mathf.Cos(a)*r,Random.Range(minHeight,maxHeight),Mathf.Sin(a)*r);
        b.root.transform.rotation=Quaternion.Euler(0,Random.Range(0f,360f),0);
        b.speed=Random.Range(4.0f,7.0f); b.turn=Random.Range(-10f,10f); b.flap=Random.Range(5.5f,8.5f); b.phase=Random.Range(0f,6.28f);
        b.flightHeight=Random.Range(minHeight,maxHeight); b.material=birdMaterials[i%birdMaterials.Length];
        MakeBody(b.root.transform,b.material);
        b.leftWing=MakeWing(b.root.transform,"LeftWing",-1f,b.material); b.rightWing=MakeWing(b.root.transform,"RightWing",1f,b.material);
        birds.Add(b);
    }

    void MakeBody(Transform p,Material mat) {
        var g=GameObject.CreatePrimitive(PrimitiveType.Sphere); g.name="Body"; g.transform.SetParent(p,false); g.transform.localScale=new Vector3(.28f,.18f,.55f); Destroy(g.GetComponent<Collider>()); g.GetComponent<Renderer>().sharedMaterial=mat;
        var h=GameObject.CreatePrimitive(PrimitiveType.Sphere); h.name="Head"; h.transform.SetParent(p,false); h.transform.localPosition=new Vector3(0,.04f,.31f); h.transform.localScale=new Vector3(.18f,.16f,.18f); Destroy(h.GetComponent<Collider>()); h.GetComponent<Renderer>().sharedMaterial=mat;
    }
    Transform MakeWing(Transform p,string n,float side,Material mat) {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=n; g.transform.SetParent(p,false); g.transform.localPosition=new Vector3(side*.28f,0,0); g.transform.localScale=new Vector3(.55f,.035f,.20f); Destroy(g.GetComponent<Collider>()); g.GetComponent<Renderer>().sharedMaterial=mat; return g.transform;
    }

    void Update() {
        if(target==null) { if(Camera.main!=null) target=Camera.main.transform; else return; }
        Vector3 center=target.position;
        for(int i=0;i<birds.Count;i++) {
            Bird b=birds[i]; if(b.root==null) continue; Transform t=b.root.transform;
            Vector3 flat=t.position-center; flat.y=0; float d=flat.magnitude;
            float steer=b.turn;
            if(d>maxRadius) {
                Vector3 to=(center-t.position); to.y=0;
                if(to.sqrMagnitude>0.1f) {
                    Quaternion wanted=Quaternion.LookRotation(to.normalized,Vector3.up);
                    t.rotation=Quaternion.Slerp(t.rotation,wanted,Time.deltaTime*0.7f);
                }
            } else t.Rotate(0,steer*Time.deltaTime,0,Space.World);
            float desiredY=center.y + b.flightHeight + Mathf.Sin(Time.time*.45f+b.phase)*1.5f;
            Vector3 pos=t.position + t.forward*b.speed*Time.deltaTime;
            pos.y=Mathf.Lerp(pos.y,desiredY,Time.deltaTime*.35f); t.position=pos;
            float flap=Mathf.Sin(Time.time*b.flap+b.phase)*38f;
            b.leftWing.localRotation=Quaternion.Euler(0,0,flap); b.rightWing.localRotation=Quaternion.Euler(0,0,-flap);
            if(d>maxRadius*2.2f) {
                Vector2 ring=Random.insideUnitCircle.normalized*Random.Range(minRadius,maxRadius*.75f);
                t.position=center+new Vector3(ring.x,Random.Range(minHeight,maxHeight),ring.y);
            }
        }
    }
}
