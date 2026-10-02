using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InventoryGUI : MonoBehaviour {
	
	private Builder builder;
	
	private bool show = false;
	private Vector2 scrollPosition = Vector3.zero;
	private bool showHotbar = true;
	private const int hotbarSlots = 10;

	
	void Start() {
		builder = FindFirstObjectByType<Builder>();
	}
	
	void Update () {
        // 0.9.9.7i: B toggles the unobtrusive build hotbar for clean screenshots.
        if(Input.GetKeyDown(KeyCode.B) && GameState.IsPlaying && !show) showHotbar = !showHotbar;
        // Mouse wheel cycles the complete buildable BlockSet while playing. It only changes selection;
        // placement/removal remain exclusively on the existing mouse buttons.
        if(GameState.IsPlaying && !show && !Cursor.visible && builder!=null) {
            float wheel=Input.GetAxis("Mouse ScrollWheel");
            if(Mathf.Abs(wheel)>0.01f) CycleSelection(wheel>0f?-1:1);
            for(int k=0;k<hotbarSlots;k++) {
                KeyCode key = k==9 ? KeyCode.Alpha0 : (KeyCode)((int)KeyCode.Alpha1+k);
                if(Input.GetKeyDown(key)) SelectHotbarSlot(k);
            }
        }
		if( Input.GetKeyDown(KeyCode.E) && GameState.IsPlaying ) {
			show = !show;
			Cursor.visible = show;
			Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
		}
		if(GameState.IsPause) show = false;
	}
	
	void OnGUI() {
        if(!show && showHotbar && GameState.IsPlaying && builder!=null) DrawHotbar();
        if(show) { VoxelBoxUI.BeginResponsive(); Rect window=VoxelBoxUI.Center(900,620); GUILayout.Window(0,window,DoInventoryWindow,"THE VOXEL BOX  •  BLOCK-AUSWAHL",VoxelBoxUI.Panel); VoxelBoxUI.EndResponsive(); } }
	
	
    private static List<Block> GetVisibleBlocks() {
        List<Block> visible=new List<Block>(); BlockSet set=BlockSet.instance; if(set==null)return visible;
        for(int i=0;i<set.Count;i++){ Block b=set[i]; if(b==null||b is GameObjectBlock)continue; visible.Add(b); }
        return visible;
    }
    private void CycleSelection(int delta) {
        List<Block> v=GetVisibleBlocks(); if(v.Count==0)return; Block cur=builder.GetSelectedBlock(); int i=v.IndexOf(cur); if(i<0)i=0; else i=(i+delta+v.Count)%v.Count; builder.SetSelectedBlock(v[i]);
    }
    private void SelectHotbarSlot(int slot) {
        List<Block> v=GetVisibleBlocks(); if(v.Count==0)return; Block cur=builder.GetSelectedBlock(); int ci=Mathf.Max(0,v.IndexOf(cur)); int start=Mathf.Clamp(ci-hotbarSlots/2,0,Mathf.Max(0,v.Count-hotbarSlots)); int i=start+slot; if(i<v.Count)builder.SetSelectedBlock(v[i]);
    }
    private void DrawHotbar() {
        List<Block> v=GetVisibleBlocks(); if(v.Count==0)return; Block cur=builder.GetSelectedBlock(); int ci=v.IndexOf(cur); if(ci<0)ci=0;
        int start=Mathf.Clamp(ci-hotbarSlots/2,0,Mathf.Max(0,v.Count-hotbarSlots));
        float size=64f,gap=3f,total=hotbarSlots*size+(hotbarSlots-1)*gap; float x=(Screen.width-total)*.5f,y=Screen.height-size-18f;
        GUI.depth=-20;
        for(int s=0;s<hotbarSlots;s++){
            int i=start+s; if(i>=v.Count)break; Block b=v[i]; Rect r=new Rect(x+s*(size+gap),y,size,size);
            GUI.Box(r,GUIContent.none,b==cur?VoxelBoxUI.Selected:VoxelBoxUI.Hotbar);
            Rect inner=RectUtils.ResizeAroundCenter(r,-8,-8); Texture t=BlockSet3DPreview.Get(BlockSet.instance,b); if(t!=null)GUI.DrawTexture(inner,t,ScaleMode.ScaleToFit,false); else b.DrawPreview(inner);
            GUI.Label(new Rect(r.x+3,r.y+r.height-20,18,17),s==9?"0":(s+1).ToString(),VoxelBoxUI.Small);
        }
    }

	private void DoInventoryWindow(int windowID) {
		Block selected = builder.GetSelectedBlock();
		if(selected != null) {
			GUILayout.Label("AUSGEWÄHLT  •  "+selected.name, VoxelBoxUI.Header, GUILayout.ExpandWidth(true));
		} else {
			GUILayout.Label("KEIN BLOCK AUSGEWÄHLT", VoxelBoxUI.Header, GUILayout.ExpandWidth(true));
		}
		selected = DrawBlockSet(BlockSet.instance, ref scrollPosition, selected);
		builder.SetSelectedBlock(selected);
    }
	
	private static Block DrawBlockSet(BlockSet blockSet, ref Vector2 scrollPosition, Block selected) {
        // 0.9.9.7h: legacy GO 01-16 no longer exist in the BlockSet; this filter remains defensive only.
        // Custom Voxel 01-32 remain visible even while empty, so all 32 bake targets are selectable.
        List<Block> visible=new List<Block>();
        for(int i=0;i<blockSet.Count;i++) { Block b=blockSet[i]; if(b==null||b is GameObjectBlock) continue; visible.Add(b); }
		using( new ScrollView(ref scrollPosition) ) {
			for(int i=0; i<visible.Count; ) {
				using( new HorizontalLayout() ) {
					for(int x=0; x<8; x++, i++) {
                        if(i>=visible.Count){GUILayout.Space(100);continue;}
						Block block = visible[i];
						if( DrawBlockItem(block, block == selected && selected != null) ) selected = block;
					}
				}
			}
		}
		return selected;
	}
	
	private static bool DrawBlockItem(Block block, bool selected) {
		Rect rect = GUILayoutUtility.GetAspectRect(1);
		if(block != null) {
			if(selected) GUI.Box(rect, GUIContent.none, VoxelBoxUI.Selected); else GUI.Box(rect, GUIContent.none, VoxelBoxUI.Hotbar);
            Rect inner=RectUtils.ResizeAroundCenter(rect,-8,-8); Texture preview=BlockSet3DPreview.Get(BlockSet.instance,block);
            if(preview!=null) GUI.DrawTexture(inner,preview,ScaleMode.ScaleToFit,false); else block.DrawPreview(inner);
            CustomVoxelBlock cb=block as CustomVoxelBlock;
            if(cb!=null){ string label="CUSTOM "+(cb.slot+1).ToString("00"); GUI.Label(new Rect(rect.x+3,rect.y+rect.height-22,rect.width-6,19),label,VoxelBoxUI.Small); if(!cb.HasDesign) GUI.Label(new Rect(rect.x+3,rect.y+3,rect.width-6,18),"LEER",VoxelBoxUI.Small); }
            return Event.current.IsMouseDown(0) && rect.Contains(Event.current.mousePosition);
		}
		return false;
	}

	
	
}
