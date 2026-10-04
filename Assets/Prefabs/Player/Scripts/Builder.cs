using UnityEngine;
using System.Collections;

public class Builder : MonoBehaviour {

    public GameObject cursor;

	private CharacterCollider character;
	private Transform viewCamera;
	private Block selectedBlock;
	
	void Start() {
		character = GetComponent<CharacterCollider>();
		viewCamera = transform.GetComponentInChildren<Camera>().transform;
		// 0.9.9.5c: first-person player geometry may remain visible, but must not cast the
		// large capsule/body shadow seen under the crosshair. This affects only renderers
		// belonging to this player hierarchy; world/custom-block shadows remain enabled.
		foreach(Renderer r in GetComponentsInChildren<Renderer>(true))
			r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		cursor = (GameObject)GameObject.Instantiate(cursor);
        CreateCopyVisualMaterials();
        CreateWorkshopVisualMaterial();
	}
	
	public void SetSelectedBlock(Block block) {
		selectedBlock = block;
        // 1.08: Copy/Paste markings are tool UI, not world content. As soon as the
        // player selects a normal block, remove the selection/ghost immediately while
        // keeping the captured copy buffer available for a later Paste selection.
        if(!CopyToolSelected()) {
            if(copySelectionVisual!=null) { Destroy(copySelectionVisual); copySelectionVisual=null; }
            HidePastePreview();
            lastPastePreviewAnchor=null;
        }
	}

	public Block GetSelectedBlock() {
		return selectedBlock;
	}
	
	
	private const float holdDelay = 3f;
	private const float buildRepeatInterval = 0.25f;     // 4 blocks / second after hold delay
	private const float removeRepeatInterval = 0.25f;    // 4 blocks / second after hold delay
	private float leftPressedAt = -1f;
	private float rightPressedAt = -1f;
	private float nextBuildTime = float.MaxValue;
	private float nextRemoveTime = float.MaxValue;
	private bool tntRadiusDialog;
	private Vector3i pendingTntPos;
	private int tntRadius = 8;
	private bool tntArmed;
	private float tntDetonateAt;
	private string tntStatus = "";

    // 0.7 Copy/Paste: tool selections are coordinates, not permanent marker voxels.
    private bool copyStartSet, copyEndSet, copyReady;
    private Vector3i copyStart, copyEnd;
    private DataBlock[] copyBuffer;
    private int copySizeX, copySizeY, copySizeZ;
    private string copyStatus="Copy: START setzen";

    // 0.7.2 visual selection + paste ghost. These objects are render-only and never enter the voxel world/save.
    private GameObject copySelectionVisual, pastePreviewVisual;
    private Mesh pastePreviewMesh;
    private Material selectionFillMaterial, selectionLineMaterial, pasteGhostMaterial;
    private Vector3i? lastPastePreviewAnchor;
    private bool pastePreviewDirty;
    // 1.08: rotation of the copied volume around world Y, in clockwise 90 degree steps.
    private int copyRotationQuarterTurns;
    // 1.0.32 sphere world: manual 6-axis orientation for plants/CrossBlocks.
    private int sphereCrossAxis=0;

    // 0.8 Custom Block Workshop: an 8x8x8 world-space authoring box compressed into one block.
    private bool workshopSet;
    private Vector3i workshopOrigin;
    private int workshopSlot=0;
    private string workshopStatus="Workshop: 8x8x8 Box setzen";
    private GameObject workshopVisual;
    private Material workshopLineMaterial;
    private GameObject workshopPreview;
    private int workshopPreviewHash=int.MinValue;
    private bool workshopUiMode;

	void Update () {
        // 0.9.9.5d: TAB is a dedicated Workshop UI toggle. It freezes mouse-look by releasing
        // the cursor without opening the pause menu, so the camera stays aimed at the live preview.
        if(workshopSet && Input.GetKeyDown(KeyCode.Tab) && (workshopUiMode || !Cursor.visible)) {
            workshopUiMode=!workshopUiMode;
            Cursor.visible=workshopUiMode;
            Cursor.lockState=workshopUiMode?CursorLockMode.None:CursorLockMode.Locked;
            ResetMouseRepeat();
        }
        if(tntArmed && Time.unscaledTime >= tntDetonateAt) DetonateTNT();
        // In the hollow sphere R cycles the six possible local "up" axes for plants.
        // This is deliberately manual: nothing is inferred from the sphere or hit face.
        if(InfiniteWorldSave.CurrentWorldType==InfiniteWorldSave.WorldType.Relativity && selectedBlock is CrossBlock && !Cursor.visible && Input.GetKeyDown(KeyCode.R)) {
            sphereCrossAxis=(sphereCrossAxis+1)%6;
        }
        // 1.0.7: while PASTE is selected the clipboard can be rotated before placement.
        // R cycles 0 -> 90 -> 180 -> 270 -> 0. Arrow keys remain as direct/alternate controls.
        if(selectedBlock!=null && selectedBlock.name=="Paste Selection" && copyReady && !Cursor.visible) {
            int oldRotation=copyRotationQuarterTurns;
            if(Input.GetKeyDown(KeyCode.R)) copyRotationQuarterTurns=(copyRotationQuarterTurns+1)&3;
            if(Input.GetKeyDown(KeyCode.RightArrow)) copyRotationQuarterTurns=(copyRotationQuarterTurns+1)&3;
            if(Input.GetKeyDown(KeyCode.LeftArrow)) copyRotationQuarterTurns=(copyRotationQuarterTurns+3)&3;
            if(Input.GetKeyDown(KeyCode.UpArrow)) copyRotationQuarterTurns=(copyRotationQuarterTurns+2)&3;
            if(Input.GetKeyDown(KeyCode.DownArrow)) copyRotationQuarterTurns=0;
            if(oldRotation!=copyRotationQuarterTurns) {
                pastePreviewDirty=true; lastPastePreviewAnchor=null;
                copyStatus="PASTE Drehung: "+(copyRotationQuarterTurns*90)+"°  •  R = +90°  •  ←/→ ±90°  •  ↑ 180°  •  ↓ 0°";
            }
        }
        if(tntRadiusDialog) { cursor.SetActive(false); ResetMouseRepeat(); return; }
		if(Cursor.visible) {
			cursor.SetActive(false);
			ResetMouseRepeat();
			return;
		}
		Map map = Map.instance;
        SunLightmap sunmap = SunLightmap.instance;
        GlowLightmap glowmap = GlowLightmap.instance;

		if( Input.GetKeyDown(KeyCode.LeftControl) ) {
			Vector3i? pos = GetCursor(false);
			if(pos.HasValue) {
                bool direct = sunmap.raymap.IsRay( pos.Value );
                int sun = sunmap.GetLight( pos.Value );
                int glow = glowmap.GetLight( pos.Value );
				Debug.Log( string.Format("Sun:{0} {1} Glow:{2}", sun, direct, glow) );
			}
		}

		if( Input.GetKeyDown(KeyCode.RightControl) ) {
			Vector3i? pos = GetCursor(true);
			if(pos.HasValue) {
				string name = map.GetBlock( pos.Value ).ToString();
                int sun = sunmap.GetLight( pos.Value );
                int glow = glowmap.GetLight( pos.Value );
				Debug.Log( string.Format("{0} Sun:{1} Glow:{2}", name, sun, glow) );
			}
		}

        // 1.0.62 Pick Block: middle mouse copies the block under the crosshair into the normal build selection.
        // It does not modify the world. The inventory can then focus this exact block when opened with E.
        if(Input.GetMouseButtonDown(2)) {
            PickBlockUnderCrosshair();
        }

		// Left mouse button: build once. After 3 seconds held: 4 blocks/second.
		if(Input.GetMouseButtonDown(0)) {
			BuildBlock();
			leftPressedAt = Time.unscaledTime;
			nextBuildTime = leftPressedAt + holdDelay;
		}
		if(Input.GetMouseButton(0) && leftPressedAt >= 0f && Time.unscaledTime >= nextBuildTime) {
			BuildBlock();
			nextBuildTime = Time.unscaledTime + buildRepeatInterval;
		}
		if(Input.GetMouseButtonUp(0)) {
			leftPressedAt = -1f;
			nextBuildTime = float.MaxValue;
		}

		// Right mouse button: remove once. After 3 seconds held: 4 blocks/second.
		if(Input.GetMouseButtonDown(1)) {
			RemoveBlock();
			rightPressedAt = Time.unscaledTime;
			nextRemoveTime = rightPressedAt + holdDelay;
		}
		if(Input.GetMouseButton(1) && rightPressedAt >= 0f && Time.unscaledTime >= nextRemoveTime) {
			RemoveBlock();
			nextRemoveTime = Time.unscaledTime + removeRepeatInterval;
		}
		if(Input.GetMouseButtonUp(1)) {
			rightPressedAt = -1f;
			nextRemoveTime = float.MaxValue;
		}

		Vector3i? cursorPos = GetCursor(true);
		cursor.SetActive(cursorPos.HasValue);
		if(cursorPos.HasValue) cursor.transform.position = cursorPos.Value + Vector3.one/2f;
        UpdateCopyPasteVisuals();
	}

	private void BuildBlock() {
		if(selectedBlock == null || Cursor.visible) return;
        // 0.7.1: COPY START/END select the voxel that is actually under the crosshair.
        // Normal building and PASTE use the adjacent free build position. In 0.7 all three
        // tools used GetCursor(false), so START/END could capture an AIR cuboid and PASTE
        // faithfully erased the destination.
        bool copyCornerTool = selectedBlock.name == "Copy Start" || selectedBlock.name == "Copy End";
        bool workshopTool = selectedBlock.name == "Custom Workshop 8x8";
		Vector3i? pos = GetCursor(copyCornerTool ? true : false);
        // 6.14.3: Tanviir restoration is now NORMAL placement. If the ray hits no existing
        // block because the source chunk is genuinely missing, create the first repair voxel
        // under the crosshair. No Shift/special mode is required.
        if(!pos.HasValue && TanviirImportWorld.active) pos = GetVoidBuildCursor();
		if(!pos.HasValue) return;
        if(HandleCopyPasteTool(pos.Value)) return;
        if(HandleCustomWorkshopTool(pos.Value)) return;
		character.pos = transform.position;
		if(BoxCollision.GetContactBoxCharacter(pos.Value, character) != null) return;
        BlockDirection direction = GetDirection(-transform.forward);
        if(InfiniteWorldSave.CurrentWorldType==InfiniteWorldSave.WorldType.Relativity && selectedBlock is CrossBlock)
            direction=(BlockDirection)sphereCrossAxis;
		DataBlock placed = new DataBlock(selectedBlock, direction);
		Map.instance.SetBlockAndRebuild(pos.Value, placed);
		InfiniteWorldSave.RecordPlayerEdit(pos.Value, placed);
        PlayerProfileManager.RecordPlaced(selectedBlock);
        if(selectedBlock.name == "Creative TNT") {
            pendingTntPos=pos.Value; tntRadius=8; tntRadiusDialog=true; Cursor.visible=true; Cursor.lockState=CursorLockMode.None;
        }

		// Stage 6.10.5: a Minecart placed directly above a rail becomes a moving runtime cart.
		// If there is no rail below it, it remains a normal placed voxel so building behavior stays predictable.
		MinecartRailBlock minecart = selectedBlock as MinecartRailBlock;
		if(minecart != null && minecart.kind == MinecartRailBlock.RailKind.Minecart)
			MinecartRuntimeController.TrySpawnFromPlacedBlock(Map.instance, pos.Value, placed);
	}

    private void PickBlockUnderCrosshair() {
        if(Cursor.visible) return;
        Vector3i? pos = GetCursor(true);
        if(!pos.HasValue) return;
        DataBlock data = Map.instance.GetBlock(pos.Value);
        Block block = data.block;
        if(block == null || block is GameObjectBlock) return;
        SetSelectedBlock(block);
    }

	private void RemoveBlock() {
		if(Cursor.visible) return;
		Vector3i? pos = GetCursor(true);
		if(pos.HasValue) { DataBlock air=new DataBlock(); Map.instance.SetBlockAndRebuild(pos.Value, air); InfiniteWorldSave.RecordPlayerEdit(pos.Value, air); PlayerProfileManager.RecordRemoved(); }
	}

    private Vector3i? GetVoidBuildCursor() {
        Ray ray=new Ray(viewCamera.position,viewCamera.forward);
        // Put the repair block near the far end of normal build reach. Repeated Shift-clicks
        // can bridge/fill a completely source-less area; once a block exists, normal face
        // placement takes over.
        for(float d=9.5f; d>=2.0f; d-=0.25f) {
            Vector3 p=ray.GetPoint(d);
            Vector3i v=new Vector3i(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.y),Mathf.FloorToInt(p.z));
            if(Map.instance.GetBlock(v).IsEmpty()) return v;
        }
        return null;
    }

	private void ResetMouseRepeat() {
		leftPressedAt = rightPressedAt = -1f;
		nextBuildTime = nextRemoveTime = float.MaxValue;
	}

    bool CopyToolSelected() {
        if(selectedBlock==null) return false;
        string n=selectedBlock.name;
        return n=="Copy Start" || n=="Copy End" || n=="Paste Selection";
    }

    void OnGUI() {
        VoxelBoxUI.Ensure();
        if(InfiniteWorldSave.CurrentWorldType==InfiniteWorldSave.WorldType.Relativity && selectedBlock is CrossBlock && !Cursor.visible) {
            string[] a={"OBEN","RECHTS","UNTEN","LINKS","VORNE","HINTEN"};
            GUI.Box(new Rect(18,Screen.height-76,360,42),"Pflanzen-Ausrichtung: "+a[Mathf.Clamp(sphereCrossAxis,0,5)]+"   •   R = drehen");
        }
        if(tntRadiusDialog || tntArmed) {
            float w=500f,h=tntRadiusDialog?225f:105f; Rect r=new Rect((Screen.width-w)/2f,35f,w,h);
            GUI.Box(r,GUIContent.none,VoxelBoxUI.Panel);
            GUI.Label(new Rect(r.x+24,r.y+18,w-48,28),"CREATIVE TNT",VoxelBoxUI.Header);
            if(tntRadiusDialog) {
                GUI.Label(new Rect(r.x+24,r.y+58,w-48,25),"Sprengradius auswählen: " + tntRadius + " Blöcke",VoxelBoxUI.Label);
                tntRadius=Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(r.x+24,r.y+92,w-48,24),tntRadius,8f,32f));
                if(GUI.Button(new Rect(r.x+24,r.y+142,210,48),"ZÜNDEN (3 Sekunden)",VoxelBoxUI.Button)) ArmTNT();
                if(GUI.Button(new Rect(r.x+258,r.y+142,218,48),"ABBRECHEN",VoxelBoxUI.Tab)) { tntRadiusDialog=false; Cursor.visible=false; Cursor.lockState=CursorLockMode.Locked; }
            } else {
                float left=Mathf.Max(0f,tntDetonateAt-Time.unscaledTime);
                GUI.Label(new Rect(r.x+24,r.y+58,w-48,28),"Explosion in " + left.ToString("0.0") + " s   •   Radius " + tntRadius,VoxelBoxUI.Label);
            }
        }
        // 1.01: the copy status belongs to the copy/paste tools, not to the whole session.
        // Keep the captured buffer, but hide this HUD immediately when another block is selected.
        if((copyStartSet || copyReady) && CopyToolSelected()) {
            Rect c=new Rect(20f,Screen.height-92f,610f,66f);
            GUI.Box(c,GUIContent.none,VoxelBoxUI.Card);
            GUI.Label(new Rect(c.x+16,c.y+10,c.width-32,20),"COPY / PASTE",VoxelBoxUI.Section);
            GUI.Label(new Rect(c.x+16,c.y+34,c.width-32,24),copyStatus,VoxelBoxUI.Small);
        }
        DrawWorkshopGUI();
    }

    void DrawWorkshopGUI() {
        if(!workshopSet) return;
        Rect r=new Rect(Screen.width-475f,25f,450f,365f); GUI.Box(r,GUIContent.none,VoxelBoxUI.Panel);
        GUI.Label(new Rect(r.x+20,r.y+16,410,28),"CUSTOM WORKSHOP 8×8×8",VoxelBoxUI.Header);
        GUI.Label(new Rect(r.x+20,r.y+52,410,22),"Custom Slot: "+(workshopSlot+1).ToString("00")+"     TAB: "+(workshopUiMode?"UI AKTIV":"UI BEDIENEN"),VoxelBoxUI.Label);
        workshopSlot=Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(r.x+20,r.y+80,410,22),workshopSlot,0,31));
        CustomVoxelBlock cb=BlockSet.instance!=null?BlockSet.instance.FindBlock<CustomVoxelBlock>("Custom Voxel "+(workshopSlot+1).ToString("00")):null;
        if(cb!=null){
            UpdateWorkshopPreview(cb);
            GUI.Label(new Rect(r.x+20,r.y+108,410,22),"BEWEGUNG  •  "+cb.animation.ToString(),VoxelBoxUI.Section);
            if(GUI.Button(new Rect(r.x+20,r.y+136,118,32),"< EFFEKT",VoxelBoxUI.Tab)) { cb.CycleAnimation(-1); workshopPreviewHash=int.MinValue; }
            if(GUI.Button(new Rect(r.x+146,r.y+136,118,32),"EFFEKT >",VoxelBoxUI.Tab)) { cb.CycleAnimation(1); workshopPreviewHash=int.MinValue; }
            GUI.Label(new Rect(r.x+280,r.y+136,150,20),"Speed "+cb.animationSpeed.ToString("0.0"),VoxelBoxUI.Small);
            cb.animationSpeed=GUI.HorizontalSlider(new Rect(r.x+280,r.y+158,150,18),cb.animationSpeed,.1f,4f);
            GUI.Label(new Rect(r.x+20,r.y+188,410,22),"PARTIKEL  •  "+cb.particleFx.ToString(),VoxelBoxUI.Section);
            if(GUI.Button(new Rect(r.x+20,r.y+216,118,32),"< PARTIKEL",VoxelBoxUI.Tab)) { cb.CycleParticle(-1); workshopPreviewHash=int.MinValue; }
            if(GUI.Button(new Rect(r.x+146,r.y+216,118,32),"PARTIKEL >",VoxelBoxUI.Tab)) { cb.CycleParticle(1); workshopPreviewHash=int.MinValue; }
            GUI.Label(new Rect(r.x+280,r.y+216,150,20),"Stärke "+cb.particleStrength.ToString("0.0"),VoxelBoxUI.Small);
            cb.particleStrength=GUI.HorizontalSlider(new Rect(r.x+280,r.y+238,150,18),cb.particleStrength,.2f,3f);
            GUI.Label(new Rect(r.x+20,r.y+268,410,42),workshopStatus,VoxelBoxUI.Small);
        }
        if(GUI.Button(new Rect(r.x+20,r.y+315,195,36),"JETZT BACKEN",VoxelBoxUI.Button)) BakeCustomWorkshop();
        if(GUI.Button(new Rect(r.x+235,r.y+315,195,36),"BOX AUSBLENDEN",VoxelBoxUI.Tab)) { workshopSet=false; workshopUiMode=false; Cursor.visible=false; Cursor.lockState=CursorLockMode.Locked; if(workshopVisual!=null) workshopVisual.SetActive(false); if(workshopPreview!=null) Destroy(workshopPreview); }
    }

    void ArmTNT() {
        tntRadiusDialog=false; tntArmed=true; tntDetonateAt=Time.unscaledTime+3f;
        Cursor.visible=false; Cursor.lockState=CursorLockMode.Locked;
    }

    void DetonateTNT() {
        tntArmed=false;
        Map map=Map.instance; if(map==null) return;
        DataBlock air=new DataBlock(); int r=tntRadius, rr=r*r, removed=0;
        System.Collections.Generic.HashSet<InfiniteChunkKey> dirtyChunks=new System.Collections.Generic.HashSet<InfiniteChunkKey>();
        // Write all voxel edits first; rebuild affected chunks only once afterwards.
        for(int z=-r;z<=r;z++) for(int y=-r;y<=r;y++) for(int x=-r;x<=r;x++) {
            if(x*x+y*y+z*z>rr) continue;
            int wx=pendingTntPos.x+x, wy=pendingTntPos.y+y, wz=pendingTntPos.z+z;
            if(map.infiniteWorld!=null) {
                if(map.infiniteWorld.GetBlock(wx,wy,wz).IsEmpty()) continue;
                map.infiniteWorld.SetBlock(wx,wy,wz,air);
                InfiniteWorldSave.RecordPlayerEdit(new Vector3i(wx,wy,wz),air);
                dirtyChunks.Add(InfiniteWorldMath.WorldToChunk(wx,wy,wz)); removed++;
            } else {
                Vector3i p=new Vector3i(wx,wy,wz); if(map.GetBlock(p).IsEmpty()) continue;
                map.SetBlock(p,air); removed++;
            }
        }
        if(map.infiniteRenderer!=null) map.infiniteRenderer.MarkDirtyChunks(dirtyChunks);
        Debug.Log("THE VOXEL BOX 0.6 TNT: radius="+r+", removed="+removed+", chunks="+dirtyChunks.Count);
    }


    bool HandleCustomWorkshopTool(Vector3i pos) {
        if(selectedBlock==null) return false;
        if(selectedBlock.name=="Custom Workshop 8x8") {
            workshopOrigin=pos; workshopSet=true; workshopStatus="Baue innerhalb der Box. Danach Slot wählen und BACKEN.";
            ShowWorkshopVisual(); Debug.Log("THE VOXEL BOX 0.8 Workshop origin "+Fmt(pos)); return true;
        }
        if(selectedBlock.name=="Bake Custom Block") { if(!workshopSet){workshopStatus="Zuerst Custom Workshop 8x8 setzen.";return true;} BakeCustomWorkshop(); return true; }
        return false;
    }

    void CreateWorkshopVisualMaterial(){ Shader sh=Shader.Find("Sprites/Default");if(sh==null)sh=Shader.Find("Unlit/Color");workshopLineMaterial=new Material(sh);workshopLineMaterial.color=new Color(1f,0.72f,0.12f,0.95f); }
    void ShowWorkshopVisual(){ if(workshopVisual!=null)Destroy(workshopVisual);workshopVisual=new GameObject("CUSTOM_WORKSHOP_8x8x8");workshopVisual.transform.position=new Vector3(workshopOrigin.x,workshopOrigin.y,workshopOrigin.z)+Vector3.one*4f;AddWorkshopEdges(workshopVisual.transform,new Vector3(8,8,8)); }
    void AddWorkshopEdges(Transform root,Vector3 s){Vector3 h=s*.5f;Vector3[] c={new Vector3(-h.x,-h.y,-h.z),new Vector3(h.x,-h.y,-h.z),new Vector3(h.x,-h.y,h.z),new Vector3(-h.x,-h.y,h.z),new Vector3(-h.x,h.y,-h.z),new Vector3(h.x,h.y,-h.z),new Vector3(h.x,h.y,h.z),new Vector3(-h.x,h.y,h.z)};int[,] e={{0,1},{1,2},{2,3},{3,0},{4,5},{5,6},{6,7},{7,4},{0,4},{1,5},{2,6},{3,7}};for(int i=0;i<12;i++){GameObject g=new GameObject("Workshop Edge");g.transform.SetParent(root,false);LineRenderer l=g.AddComponent<LineRenderer>();l.sharedMaterial=workshopLineMaterial;l.useWorldSpace=false;l.positionCount=2;l.startWidth=l.endWidth=.065f;l.SetPosition(0,c[e[i,0]]);l.SetPosition(1,c[e[i,1]]);}}
    void UpdateWorkshopPreview(CustomVoxelBlock settings){
        if(!workshopSet || Map.instance==null || settings==null) return;
        // Hash the live 8x8x8 workshop contents + selected effect. Rebuild only when something changed.
        unchecked {
            int h=17; DataBlock[] d=new DataBlock[CustomVoxelBlock.COUNT]; int i=0,solid=0;
            for(int z=0;z<8;z++)for(int y=0;y<8;y++)for(int x=0;x<8;x++,i++){
                d[i]=Map.instance.GetBlock(new Vector3i(workshopOrigin.x+x,workshopOrigin.y+y,workshopOrigin.z+z));
                if(!d[i].IsEmpty()){solid++;h=h*31+d[i].blockID;h=h*31+(int)d[i].direction;}
            }
            h=h*31+workshopSlot; h=h*31+(int)settings.animation; h=h*31+Mathf.RoundToInt(settings.animationSpeed*100f); h=h*31+(int)settings.particleFx; h=h*31+Mathf.RoundToInt(settings.particleStrength*100f);
            if(h==workshopPreviewHash) return; workshopPreviewHash=h;
            if(workshopPreview!=null) Destroy(workshopPreview); if(solid==0) return;
            CustomVoxelBlock temp=ScriptableObject.CreateInstance<CustomVoxelBlock>(); temp.design=d; temp.animation=settings.animation; temp.animationSpeed=settings.animationSpeed; temp.particleFx=settings.particleFx; temp.particleStrength=settings.particleStrength;
            MeshBuilder mb=new MeshBuilder(); temp.Build(mb,new DataBlock(temp,BlockDirection.FORWARD),LocalPosition.zero,0,null);
            GameObject anchor=new GameObject("CUSTOM WORKSHOP LIVE PREVIEW"); workshopPreview=anchor;
            // Preview floats just outside the workshop box, at normal 1-block size.
            anchor.transform.position=new Vector3(workshopOrigin.x+8.75f,workshopOrigin.y+4.0f,workshopOrigin.z+4.0f);
            GameObject model=new GameObject("Preview Mesh",typeof(MeshFilter),typeof(MeshRenderer)); model.transform.SetParent(anchor.transform,false); model.transform.localPosition=-Vector3.one*.5f;
            Mesh m=mb.ToMesh(null); model.GetComponent<MeshFilter>().sharedMesh=m; if(m!=null) model.GetComponent<MeshRenderer>().sharedMaterials=mb.GetMaterials(BlockSet.instance.GetMaterials());
            CustomVoxelAnimator a=anchor.AddComponent<CustomVoxelAnimator>(); a.Setup(settings,0); if(settings.particleFx!=CustomVoxelBlock.ParticleFx.None) anchor.AddComponent<CustomVoxelParticleFX>().Setup(settings,0); Destroy(temp);
        }
    }

    void BakeCustomWorkshop(){
        if(!workshopSet||Map.instance==null)return; DataBlock[] d=new DataBlock[CustomVoxelBlock.COUNT];int solid=0,i=0;
        for(int z=0;z<8;z++)for(int y=0;y<8;y++)for(int x=0;x<8;x++,i++){d[i]=Map.instance.GetBlock(new Vector3i(workshopOrigin.x+x,workshopOrigin.y+y,workshopOrigin.z+z)); if(!d[i].IsEmpty())solid++;}
        if(solid==0){workshopStatus="Box ist leer - nichts gebacken.";return;} CustomVoxelBlock target=BlockSet.instance.FindBlock<CustomVoxelBlock>("Custom Voxel "+(workshopSlot+1).ToString("00"));
        if(target==null){workshopStatus="Custom Slot nicht gefunden.";return;} target.SetDesign(d); workshopStatus="Slot "+(workshopSlot+1).ToString("00")+" gebacken: "+solid+" Mini-Voxel. Block im BlockSet auswählen und setzen."; BlockSet3DPreview.Clear(); Debug.Log("THE VOXEL BOX 0.9.9.7g baked slot "+(workshopSlot+1)+" with "+solid+" mini voxels");
        // Existing placed copies of this custom slot may be in loaded chunks: request a visible refresh around workshop/player via normal dirty path on next edits.
    }

    bool HandleCopyPasteTool(Vector3i pos) {
        if(selectedBlock==null) return false;
        string n=selectedBlock.name;
        if(n=="Copy Start") {
            copyStart=pos; copyStartSet=true; copyEndSet=false; copyReady=false; copyBuffer=null; copyRotationQuarterTurns=0;
            pastePreviewDirty=true; HidePastePreview(); ShowSelectionVisual(copyStart,copyStart);
            copyStatus="START: "+Fmt(copyStart)+"  - jetzt COPY END an der gegenüberliegenden Ecke setzen.";
            Debug.Log("THE VOXEL BOX 0.7 COPY START "+Fmt(copyStart));
            return true;
        }
        if(n=="Copy End") {
            if(!copyStartSet) { copyStatus="Erst COPY START setzen."; return true; }
            copyEnd=pos; copyEndSet=true; CaptureSelection(); return true;
        }
        if(n=="Paste Selection") {
            if(!copyReady || copyBuffer==null) { copyStatus="Noch nichts kopiert: COPY START und COPY END setzen."; return true; }
            PasteSelection(pos); return true;
        }
        return false;
    }

    void CaptureSelection() {
        int minX=Mathf.Min(copyStart.x,copyEnd.x), minY=Mathf.Min(copyStart.y,copyEnd.y), minZ=Mathf.Min(copyStart.z,copyEnd.z);
        int maxX=Mathf.Max(copyStart.x,copyEnd.x), maxY=Mathf.Max(copyStart.y,copyEnd.y), maxZ=Mathf.Max(copyStart.z,copyEnd.z);
        copySizeX=maxX-minX+1; copySizeY=maxY-minY+1; copySizeZ=maxZ-minZ+1;
        if(copySizeX>32 || copySizeY>32 || copySizeZ>32) {
            copyReady=false; copyBuffer=null;
            copyStatus="Auswahl zu groß: maximal 32 x 32 x 32 Blöcke (aktuell "+copySizeX+" x "+copySizeY+" x "+copySizeZ+").";
            return;
        }
        copyBuffer=new DataBlock[copySizeX*copySizeY*copySizeZ]; int i=0, solidCount=0;
        Map map=Map.instance;
        for(int z=0;z<copySizeZ;z++) for(int y=0;y<copySizeY;y++) for(int x=0;x<copySizeX;x++,i++) {
            copyBuffer[i]=map.GetBlock(new Vector3i(minX+x,minY+y,minZ+z));
            if(!copyBuffer[i].IsEmpty()) solidCount++;
        }
        if(solidCount==0) {
            copyReady=false; copyBuffer=null;
            copyStatus="Auswahl enthält keine Blöcke - START/END bitte direkt auf vorhandene Blöcke setzen.";
            Debug.LogWarning("THE VOXEL BOX 0.7.1 COPY rejected empty selection");
            return;
        }
        copyReady=true; pastePreviewDirty=true; ShowSelectionVisual(copyStart,copyEnd);
        copyStatus="Kopiert: "+copySizeX+" x "+copySizeY+" x "+copySizeZ+" = "+copyBuffer.Length+" Voxel, davon "+solidCount+" Blöcke. PASTE wählen.";
        Debug.Log("THE VOXEL BOX 0.7 COPY captured "+copyStatus);
    }

    void PasteSelection(Vector3i anchor) {
        Map map=Map.instance; if(map==null || copyBuffer==null) return;
        var dirty=new System.Collections.Generic.HashSet<InfiniteChunkKey>(); int i=0, changed=0;
        for(int z=0;z<copySizeZ;z++) for(int y=0;y<copySizeY;y++) for(int x=0;x<copySizeX;x++,i++) {
            int dx,dz; RotatedXZ(x,z,out dx,out dz);
            Vector3i p=new Vector3i(anchor.x+dx,anchor.y+y,anchor.z+dz); DataBlock b=copyBuffer[i];
            if(!b.IsEmpty()) b.direction=(BlockDirection)(((int)b.direction+copyRotationQuarterTurns)&3);
            if(map.infiniteWorld!=null) {
                map.infiniteWorld.SetBlock(p.x,p.y,p.z,b); InfiniteWorldSave.RecordPlayerEdit(p,b);
                dirty.Add(InfiniteWorldMath.WorldToChunk(p.x,p.y,p.z)); changed++;
            } else { map.SetBlock(p,b); changed++; }
        }
        if(map.infiniteRenderer!=null) map.infiniteRenderer.MarkDirtyChunks(dirty);
        int outX=(copyRotationQuarterTurns%2==0)?copySizeX:copySizeZ; int outZ=(copyRotationQuarterTurns%2==0)?copySizeZ:copySizeX;
        copyStatus="Eingefügt bei "+Fmt(anchor)+": "+outX+" x "+copySizeY+" x "+outZ+" ("+changed+" Voxel), Drehung "+(copyRotationQuarterTurns*90)+"°.";
        Debug.Log("THE VOXEL BOX 0.7 PASTE: "+copyStatus+" chunks="+dirty.Count);
    }


    void CreateCopyVisualMaterials() {
        Shader sh=Shader.Find("Sprites/Default");
        if(sh==null) sh=Shader.Find("Unlit/Color");
        selectionFillMaterial=new Material(sh); selectionFillMaterial.color=new Color(0.15f,0.75f,1f,0.10f);
        selectionLineMaterial=new Material(sh); selectionLineMaterial.color=new Color(0.1f,0.9f,1f,0.95f);
        pasteGhostMaterial=new Material(sh); pasteGhostMaterial.color=new Color(0.35f,1f,0.45f,0.28f);
    }

    void ShowSelectionVisual(Vector3i a, Vector3i b) {
        if(copySelectionVisual!=null) Destroy(copySelectionVisual);
        int minX=Mathf.Min(a.x,b.x), minY=Mathf.Min(a.y,b.y), minZ=Mathf.Min(a.z,b.z);
        int maxX=Mathf.Max(a.x,b.x), maxY=Mathf.Max(a.y,b.y), maxZ=Mathf.Max(a.z,b.z);
        Vector3 size=new Vector3(maxX-minX+1,maxY-minY+1,maxZ-minZ+1);
        Vector3 center=new Vector3(minX,minY,minZ)+(size*0.5f);
        copySelectionVisual=new GameObject("COPY_SELECTION_VISUAL");
        copySelectionVisual.transform.position=center;
        GameObject fill=GameObject.CreatePrimitive(PrimitiveType.Cube); fill.name="Selection Fill"; fill.transform.SetParent(copySelectionVisual.transform,false); fill.transform.localScale=size;
        Collider col=fill.GetComponent<Collider>(); if(col!=null) Destroy(col);
        fill.GetComponent<Renderer>().sharedMaterial=selectionFillMaterial;
        AddSelectionEdges(copySelectionVisual.transform,size);
    }

    void AddSelectionEdges(Transform root, Vector3 s) {
        Vector3 h=s*0.5f; Vector3[] c={new Vector3(-h.x,-h.y,-h.z),new Vector3(h.x,-h.y,-h.z),new Vector3(h.x,-h.y,h.z),new Vector3(-h.x,-h.y,h.z),new Vector3(-h.x,h.y,-h.z),new Vector3(h.x,h.y,-h.z),new Vector3(h.x,h.y,h.z),new Vector3(-h.x,h.y,h.z)};
        int[,] e={{0,1},{1,2},{2,3},{3,0},{4,5},{5,6},{6,7},{7,4},{0,4},{1,5},{2,6},{3,7}};
        for(int i=0;i<12;i++){ GameObject g=new GameObject("Edge"); g.transform.SetParent(root,false); LineRenderer l=g.AddComponent<LineRenderer>(); l.sharedMaterial=selectionLineMaterial; l.useWorldSpace=false; l.positionCount=2; l.startWidth=l.endWidth=0.045f; l.SetPosition(0,c[e[i,0]]); l.SetPosition(1,c[e[i,1]]); }
    }

    void UpdateCopyPasteVisuals() {
        if(selectedBlock==null || selectedBlock.name!="Paste Selection" || !copyReady || copyBuffer==null || Cursor.visible) { HidePastePreview(); return; }
        Vector3i? anchor=GetCursor(false); if(!anchor.HasValue){ HidePastePreview(); return; }
        if(pastePreviewDirty || pastePreviewVisual==null) BuildPastePreviewMesh();
        if(pastePreviewVisual!=null) { pastePreviewVisual.SetActive(true); pastePreviewVisual.transform.position=new Vector3(anchor.Value.x,anchor.Value.y,anchor.Value.z); lastPastePreviewAnchor=anchor; }
    }

    void BuildPastePreviewMesh() {
        HidePastePreview();
        if(copyBuffer==null) return;
        pastePreviewVisual=new GameObject("PASTE_GHOST_PREVIEW");
        MeshFilter mf=pastePreviewVisual.AddComponent<MeshFilter>(); MeshRenderer mr=pastePreviewVisual.AddComponent<MeshRenderer>(); mr.sharedMaterial=pasteGhostMaterial;
        var v=new System.Collections.Generic.List<Vector3>(); var t=new System.Collections.Generic.List<int>();
        Vector3[] cv={new Vector3(0,0,0),new Vector3(1,0,0),new Vector3(1,1,0),new Vector3(0,1,0),new Vector3(0,0,1),new Vector3(1,0,1),new Vector3(1,1,1),new Vector3(0,1,1)};
        int[] ct={0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,3,7,6,3,6,2,0,4,7,0,7,3,1,2,6,1,6,5};
        int i=0;
        for(int z=0;z<copySizeZ;z++) for(int y=0;y<copySizeY;y++) for(int x=0;x<copySizeX;x++,i++) {
            if(copyBuffer[i].IsEmpty()) continue;
            int dx,dz; RotatedXZ(x,z,out dx,out dz);
            int o=v.Count; Vector3 p=new Vector3(dx,y,dz); for(int k=0;k<8;k++) v.Add(p+cv[k]); for(int k=0;k<ct.Length;k++) t.Add(o+ct[k]);
        }
        pastePreviewMesh=new Mesh(); pastePreviewMesh.name="VoxelBox Paste Ghost"; if(v.Count>65535) pastePreviewMesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
        pastePreviewMesh.SetVertices(v); pastePreviewMesh.SetTriangles(t,0); pastePreviewMesh.RecalculateNormals(); pastePreviewMesh.RecalculateBounds(); mf.sharedMesh=pastePreviewMesh;
        pastePreviewDirty=false;
    }


    void RotatedXZ(int x,int z,out int dx,out int dz) {
        switch(copyRotationQuarterTurns&3) {
            default: dx=x; dz=z; break;
            case 1: dx=copySizeZ-1-z; dz=x; break;
            case 2: dx=copySizeX-1-x; dz=copySizeZ-1-z; break;
            case 3: dx=z; dz=copySizeX-1-x; break;
        }
    }

    void HidePastePreview() { if(pastePreviewVisual!=null) pastePreviewVisual.SetActive(false); }

    void OnDestroy() {
        if(copySelectionVisual!=null) Destroy(copySelectionVisual); if(pastePreviewVisual!=null) Destroy(pastePreviewVisual); if(pastePreviewMesh!=null) Destroy(pastePreviewMesh);
        if(selectionFillMaterial!=null) Destroy(selectionFillMaterial); if(selectionLineMaterial!=null) Destroy(selectionLineMaterial); if(pasteGhostMaterial!=null) Destroy(pasteGhostMaterial);
        if(workshopVisual!=null) Destroy(workshopVisual); if(workshopLineMaterial!=null) Destroy(workshopLineMaterial);
    }

    static string Fmt(Vector3i p) { return "("+p.x+", "+p.y+", "+p.z+")"; }

	void OnDrawGizmos() {
		if(!Application.isPlaying) return;
		
		Map map = Map.instance;
		Ray ray = new Ray(viewCamera.position, viewCamera.forward);

		float distance = 20;
		if( MapRayIntersection.Raycast(map, ray, ref distance) ) {
			Vector3 point = ray.GetPoint(distance);
			Gizmos.color = Color.red;
			Gizmos.DrawSphere(point, 0.1f);
		}
	}
	
	public Vector3i? GetCursor(bool inside) {
		Ray ray = new Ray(viewCamera.position, viewCamera.forward);
		Vector3i pos, prevPos;
		bool intersect = MapRayIntersection.Raycast(Map.instance, ray, 20, out pos, out prevPos);
		if( intersect ) {
			Block block = Map.instance.GetBlock( pos ).block;
			if(block is CrossBlock) inside = true;
			
			if(inside) {
				return pos;
			} else {
				return prevPos;
			}
		}
		return null;
	}
	
	private static BlockDirection GetDirection(Vector3 dir) {
		if( Mathf.Abs(dir.z) >= Mathf.Abs(dir.x) ) {
			// 0 или 180
			if(dir.z >= 0) return BlockDirection.FORWARD;
			return BlockDirection.BACKWARD;
		} else {
			// 90 или 270
			if(dir.x >= 0) return BlockDirection.RIGHT;
			return BlockDirection.LEFT;
		}
	}
	
}
