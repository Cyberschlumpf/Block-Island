using UnityEngine;

// Voxel Box 0.8.2: runtime minecart follows connected rails in X/Z and one-block slopes.
// This deliberately does not depend on the rail's stored placement rotation for navigation:
// the visible legacy rail model and placement direction can be perpendicular, while the
// neighbouring rail cells are the authoritative route. Curves therefore turn naturally.
public sealed class MinecartRuntimeController : MonoBehaviour {
    public InfiniteVoxelWorld world;
    public Vector3i railPos;
    public Vector3i travelDir;
    public float speed = 3.25f;

    Vector3 from, to;
    Vector3i nextRailPos;
    float segmentT;
    bool moving;

    // Voxel Box 0.8.1: M toggles riding when the player is close to this cart.
    const float RideDistance = 2.75f;
    const float RiderHeight = 1.05f;
    static MinecartRuntimeController activeRiderCart;
    GameObject rider;
    CharacterInputController riderInput;
    CharacterMotor riderMotor;
    CharacterMotorSwimming riderSwimming;
    CharacterCollider riderCollider;
    static readonly Vector3i[] Dirs = {
        new Vector3i(1,0,0), new Vector3i(-1,0,0),
        new Vector3i(0,0,1), new Vector3i(0,0,-1)
    };

    public static bool TrySpawnFromPlacedBlock(Map map, Vector3i cartBlockPos, DataBlock cartData) {
        if(map==null || map.infiniteWorld==null || cartData.IsEmpty()) return false;
        MinecartRailBlock cartBlock=cartData.block as MinecartRailBlock;
        if(cartBlock==null || cartBlock.kind!=MinecartRailBlock.RailKind.Minecart) return false;

        // Normally the rail is one voxel below the cart. Accept two voxels below as a
        // tolerance for the legacy cursor/low-profile rail placement path.
        Vector3i rp=cartBlockPos + new Vector3i(0,-1,0);
        DataBlock rail=map.GetBlock(rp);
        if(!IsRail(rail)) {
            Vector3i rp2=cartBlockPos + new Vector3i(0,-2,0);
            DataBlock rail2=map.GetBlock(rp2);
            if(!IsRail(rail2)) return false;
            rp=rp2; rail=rail2;
        }

        map.SetBlock(cartBlockPos,new DataBlock());
        GameObject go=new GameObject("Moving Minecart");
        MinecartRuntimeController c=go.AddComponent<MinecartRuntimeController>();
        c.world=map.infiniteWorld;
        c.railPos=rp;
        c.travelDir=c.ChooseInitialDirection(cartData.direction);
        c.BuildVisual(cartBlock,cartData.direction);
        c.SnapToRail();
        c.BeginNextSegment();
        return true;
    }

    Vector3i ChooseInitialDirection(BlockDirection placedDirection) {
        Vector3i facing=DirectionVector(placedDirection);
        Vector3i best=new Vector3i(0,0,0); int bestDot=-999;
        for(int i=0;i<Dirs.Length;i++) {
            Vector3i candidate;
            if(!TryRailInDirection(railPos,Dirs[i],out candidate)) continue;
            int dot=Dirs[i].x*facing.x+Dirs[i].z*facing.z;
            if(dot>bestDot) { bestDot=dot; best=Dirs[i]; }
        }
        // If the cart was placed facing away from the built track, any connected rail is valid.
        if(IsZero(best)) for(int i=0;i<Dirs.Length;i++) { Vector3i candidate; if(TryRailInDirection(railPos,Dirs[i],out candidate)) return Dirs[i]; }
        return best;
    }

    void Update() {
        HandleRideInput();
        if(!moving || world==null) return;
        float distance=Mathf.Max(.001f,Vector3.Distance(from,to));
        segmentT += (speed/distance)*Time.deltaTime;
        float t=Mathf.Clamp01(segmentT);
        transform.position=Vector3.Lerp(from,to,t);
        Vector3 delta=to-from;
        if(delta.sqrMagnitude>.0001f) transform.rotation=Quaternion.LookRotation(delta.normalized,Vector3.up);
        if(segmentT>=1f) {
            railPos=nextRailPos;
            SnapToRail();
            BeginNextSegment();
        }
    }


    void LateUpdate() {
        if(rider==null || activeRiderCart!=this) return;
        // Keep the player upright and let the normal MouseLook/camera continue to work.
        rider.transform.position=transform.position + Vector3.up*RiderHeight;
    }

    void HandleRideInput() {
        if(!GameState.IsPlaying || Cursor.visible || !Input.GetKeyDown(KeyCode.M)) return;

        if(activeRiderCart==this) {
            ExitCart();
            return;
        }
        if(activeRiderCart!=null) return;

        GameObject player=GameObject.FindGameObjectWithTag("Player");
        if(player==null) return;
        if((player.transform.position-transform.position).sqrMagnitude > RideDistance*RideDistance) return;
        EnterCart(player);
    }

    void EnterCart(GameObject player) {
        rider=player;
        activeRiderCart=this;
        riderInput=player.GetComponent<CharacterInputController>();
        riderMotor=player.GetComponent<CharacterMotor>();
        riderSwimming=player.GetComponent<CharacterMotorSwimming>();
        riderCollider=player.GetComponent<CharacterCollider>();
        if(riderInput!=null) riderInput.enabled=false;
        if(riderMotor!=null) riderMotor.enabled=false;
        if(riderSwimming!=null) riderSwimming.enabled=false;
        if(riderCollider!=null) riderCollider.enabled=false;
        rider.transform.position=transform.position + Vector3.up*RiderHeight;
    }

    void ExitCart() {
        if(rider==null) { if(activeRiderCart==this) activeRiderCart=null; return; }
        // Put the player beside and slightly above the cart, not on the rail.
        Vector3 side=transform.right;
        if(side.sqrMagnitude<.01f) side=Vector3.right;
        rider.transform.position=transform.position + side*1.15f + Vector3.up*.65f;
        if(riderCollider!=null) riderCollider.enabled=true;
        if(riderInput!=null) riderInput.enabled=true;
        if(riderMotor!=null) riderMotor.enabled=true;
        // Swimming motor is intentionally left disabled; CharacterInputController selects it
        // automatically on the next frame if the exit point is in water.
        if(riderSwimming!=null) riderSwimming.enabled=false;
        rider=null; riderInput=null; riderMotor=null; riderSwimming=null; riderCollider=null;
        if(activeRiderCart==this) activeRiderCart=null;
    }

    void OnDestroy() { if(activeRiderCart==this) ExitCart(); }

    void OnGUI() {
        if(!GameState.IsPlaying || Cursor.visible) return;
        if(activeRiderCart==this) {
            GUI.Label(new Rect(Screen.width*.5f-105f,Screen.height-82f,210f,28f),"M - Minecart verlassen");
            return;
        }
        if(activeRiderCart!=null) return;
        GameObject player=GameObject.FindGameObjectWithTag("Player");
        if(player!=null && (player.transform.position-transform.position).sqrMagnitude<=RideDistance*RideDistance)
            GUI.Label(new Rect(Screen.width*.5f-105f,Screen.height-82f,210f,28f),"M - Minecart mitfahren");
    }

    void SnapToRail() { transform.position=RailCenter(railPos); }

    void BeginNextSegment() {
        if(world==null || !HasRail(railPos)) { moving=false; return; }
        Vector3i incoming=new Vector3i(-travelDir.x,0,-travelDir.z);
        Vector3i chosen=new Vector3i(0,0,0);
        Vector3i chosenPos=new Vector3i(0,0,0);

        // 0.8.2: A connection may be level, one voxel higher or one voxel lower.
        // Straight ahead remains the first choice, so crossings and nearby parallel rails
        // do not make the cart randomly turn.
        if(!IsZero(travelDir) && TryRailInDirection(railPos,travelDir,out chosenPos)) chosen=travelDir;

        // If straight is unavailable, accept a 90 degree connected branch.
        if(IsZero(chosen)) {
            for(int i=0;i<Dirs.Length;i++) {
                Vector3i d=Dirs[i];
                if(Equal(d,incoming)) continue;
                Vector3i candidate;
                if(TryRailInDirection(railPos,d,out candidate)) { chosen=d; chosenPos=candidate; break; }
            }
        }

        // Initial/fallback case. Going back is allowed only when there is no other route.
        if(IsZero(chosen)) {
            for(int i=0;i<Dirs.Length;i++) {
                Vector3i candidate;
                if(TryRailInDirection(railPos,Dirs[i],out candidate)) { chosen=Dirs[i]; chosenPos=candidate; break; }
            }
        }
        if(IsZero(chosen)) { moving=false; return; }

        travelDir=chosen;
        nextRailPos=chosenPos;
        from=RailCenter(railPos);
        to=RailCenter(nextRailPos);
        segmentT=0f; moving=true;
    }

    // Search the neighbouring column in the requested horizontal direction.
    // Same level is preferred, then +1 and -1. This supports ordinary rails,
    // transverse/90-degree layouts and block-step slopes without changing rail data.
    bool TryRailInDirection(Vector3i origin,Vector3i dir,out Vector3i found) {
        Vector3i level=origin+new Vector3i(dir.x,0,dir.z);
        if(HasRail(level)) { found=level; return true; }
        Vector3i up=level+new Vector3i(0,1,0);
        if(HasRail(up)) { found=up; return true; }
        Vector3i down=level+new Vector3i(0,-1,0);
        if(HasRail(down)) { found=down; return true; }
        found=new Vector3i(0,0,0); return false;
    }

    bool HasRail(Vector3i p) { return IsRail(world.GetBlock(p.x,p.y,p.z)); }
    static bool IsRail(DataBlock b) {
        MinecartRailBlock rb=b.block as MinecartRailBlock;
        return rb!=null && rb.kind!=MinecartRailBlock.RailKind.Minecart;
    }
    static bool IsZero(Vector3i a) { return a.x==0 && a.y==0 && a.z==0; }
    static bool Equal(Vector3i a,Vector3i b) { return a.x==b.x && a.y==b.y && a.z==b.z; }
    static Vector3 RailCenter(Vector3i p) { return new Vector3(p.x+.5f,p.y+.18f,p.z+.5f); }
    static Vector3i DirectionVector(BlockDirection d) {
        if(d==BlockDirection.RIGHT)return new Vector3i(1,0,0);
        if(d==BlockDirection.BACKWARD)return new Vector3i(0,0,-1);
        if(d==BlockDirection.LEFT)return new Vector3i(-1,0,0);
        return new Vector3i(0,0,1);
    }

    void BuildVisual(MinecartRailBlock cart,BlockDirection d) {
        MeshBuilder mb=new MeshBuilder();
        DataBlock data=new DataBlock(cart,d);
        cart.Build(mb,data,LocalPosition.zero,0,null);
        Mesh mesh=mb.ToMesh(null);
        MeshFilter mf=gameObject.AddComponent<MeshFilter>(); mf.sharedMesh=mesh;
        MeshRenderer mr=gameObject.AddComponent<MeshRenderer>();
        if(mesh!=null && BlockSet.instance!=null) mr.materials=mb.GetMaterials(BlockSet.instance.GetMaterials());
        mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On; mr.receiveShadows=true;
        if(mesh!=null) {
            Vector3[] v=mesh.vertices; for(int i=0;i<v.Length;i++) v[i]-=new Vector3(.5f,0,.5f); mesh.vertices=v; mesh.RecalculateBounds();
        }
    }
}
