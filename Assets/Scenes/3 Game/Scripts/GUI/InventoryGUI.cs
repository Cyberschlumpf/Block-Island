using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InventoryGUI : MonoBehaviour {
	
	private Builder builder;
	
	private bool show = false;
	private Vector2 scrollPosition = Vector3.zero;
    private int category = 0;
    private static readonly string[] categoryNames = { "BLÖCKE", "NATUR", "FUNKTION", "CUSTOM", "FAVORITEN" };
    private Block heldBlock;
    private float heldSince;
    private bool longPressOpened;
    private Block categoryMenuBlock;
    private Rect categoryMenuRect;
    private const float longPressSeconds = 3f;
    private const string categoryPrefPrefix = "VoxelBox.BlockCategory.";
    private const string favoritePrefPrefix = "VoxelBox.BlockFavorite.";
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
	
    private static int CategoryOf(Block b) {
        if(b==null) return -1;
        string n=b.name ?? "";
        if(b is CustomVoxelBlock || b is FenceBlock || b is StairBlock || b is SphereBlock || b is TanviirArchitecturalBlock || n=="Custom Workshop 8x8" || n=="Bake Custom Block") return 3;
        if(b is MinecartRailBlock || n=="Copy Start" || n=="Copy End" || n=="Paste Selection" || n=="Creative TNT" || n=="TNT") return 2;
        if(IsNatureBlock(n)) return 1;
        return 0;
    }

    private static bool IsNatureBlock(string name) {
        if(string.IsNullOrEmpty(name)) return false;
        string n=name.ToLowerInvariant();
        // Terrain, fluids, trunks/wood and foliage belong in the dedicated nature browser.
        return n=="grass" || n=="dirt" || n=="ground" || n=="sand" || n=="snow" || n=="water" ||
               n.Contains("water ") || n.Contains("waterfall") || n.Contains("leaf") || n.Contains("leaves") ||
               n.Contains("wood") || n.Contains("log") || n.Contains("baum") || n.Contains("tree");
    }

    private static string CategoryDescription(int c) {
        if(c==1) return "Erde, Gras, Sand, Wasser, Bäume, Holz und Blätter";
        if(c==2) return "TNT, Minecart, Schienen, Copy / Paste und weitere Werkzeuge";
        if(c==3) return "Zäune, Stufen, Formen und selbst gebackene Custom Blocks";
        if(c==4) return "Deine Favoriten  •  Block 3 Sekunden halten, um ihn hinzuzufügen oder zu entfernen";
        return "Normale Bau- und Materialblöcke";
    }

    private static int BlockStableId(Block b) {
        BlockSet set=BlockSet.instance; if(set==null||b==null)return -1;
        for(int i=0;i<set.Count;i++) if(set[i]==b)return i;
        return -1;
    }
    private static int EffectiveCategoryOf(Block b) {
        int id=BlockStableId(b); int automatic=CategoryOf(b); if(id<0)return automatic;
        return PlayerPrefs.GetInt(categoryPrefPrefix+id,automatic);
    }
    private static bool IsFavorite(Block b) {
        int id=BlockStableId(b); return id>=0 && PlayerPrefs.GetInt(favoritePrefPrefix+id,0)==1;
    }
    private static void SetPersonalCategory(Block b,int c) {
        int id=BlockStableId(b); if(id<0)return; PlayerPrefs.SetInt(categoryPrefPrefix+id,c); PlayerPrefs.Save();
    }
    private static void ToggleFavorite(Block b) {
        int id=BlockStableId(b); if(id<0)return; PlayerPrefs.SetInt(favoritePrefPrefix+id,IsFavorite(b)?0:1); PlayerPrefs.Save();
    }

    private Block DrawBlockSet(BlockSet blockSet, ref Vector2 scrollPosition, Block selected) {
        GUILayout.Space(6);
        using(new HorizontalLayout()) {
            for(int c=0;c<categoryNames.Length;c++) {
                bool active=category==c;
                if(GUILayout.Button(categoryNames[c], active ? VoxelBoxUI.Selected : VoxelBoxUI.Hotbar, GUILayout.Height(42))) {
                    if(category!=c) { category=c; scrollPosition=Vector2.zero; }
                }
            }
        }
        GUILayout.Label(CategoryDescription(category),VoxelBoxUI.Small,GUILayout.Height(24));

        List<Block> visible=new List<Block>();
        for(int i=0;i<blockSet.Count;i++) {
            Block b=blockSet[i]; if(b==null||b is GameObjectBlock) continue;
            if(category==4 ? IsFavorite(b) : EffectiveCategoryOf(b)==category) visible.Add(b);
        }
        using(new ScrollView(ref scrollPosition)) {
            for(int i=0;i<visible.Count;) {
                using(new HorizontalLayout()) {
                    for(int x=0;x<8;x++,i++) {
                        if(i>=visible.Count){GUILayout.Space(100);continue;}
                        Block block=visible[i];
                        if(DrawBlockItem(block,block==selected && selected!=null)) selected=block;
                    }
                }
            }
        }
        return selected;
    }
	
	private bool DrawBlockItem(Block block, bool selected) {
        Rect rect = GUILayoutUtility.GetAspectRect(1);
        if(block == null) return false;
        if(selected) GUI.Box(rect, GUIContent.none, VoxelBoxUI.Selected); else GUI.Box(rect, GUIContent.none, VoxelBoxUI.Hotbar);
        Rect inner=RectUtils.ResizeAroundCenter(rect,-8,-8); Texture preview=BlockSet3DPreview.Get(BlockSet.instance,block);
        if(preview!=null) GUI.DrawTexture(inner,preview,ScaleMode.ScaleToFit,false); else block.DrawPreview(inner);
        CustomVoxelBlock cb=block as CustomVoxelBlock;
        if(cb!=null){ string label="CUSTOM "+(cb.slot+1).ToString("00"); GUI.Label(new Rect(rect.x+3,rect.y+rect.height-22,rect.width-6,19),label,VoxelBoxUI.Small); if(!cb.HasDesign) GUI.Label(new Rect(rect.x+3,rect.y+3,rect.width-6,18),"LEER",VoxelBoxUI.Small); }
        if(IsFavorite(block)) GUI.Label(new Rect(rect.x+rect.width-24,rect.y+3,20,20),"★",VoxelBoxUI.Small);

        Event e=Event.current; bool clicked=false;
        if(e.type==EventType.MouseDown && e.button==0 && rect.Contains(e.mousePosition)) {
            heldBlock=block; heldSince=Time.unscaledTime; longPressOpened=false; clicked=true; e.Use();
        }
        if(heldBlock==block && Input.GetMouseButton(0) && !longPressOpened && Time.unscaledTime-heldSince>=longPressSeconds) {
            categoryMenuBlock=block; longPressOpened=true;
            float w=310f,h=286f; float px=Mathf.Clamp(rect.x+rect.width+8,8,900f-w-8); float py=Mathf.Clamp(rect.y,8,620f-h-8);
            categoryMenuRect=new Rect(px,py,w,h);
        }
        if(e.type==EventType.MouseUp && e.button==0 && heldBlock==block) { heldBlock=null; }
        return clicked;
    }

    private void DrawCategoryMenu() {
        if(categoryMenuBlock==null)return;
        GUI.depth=-100;
        GUI.Box(categoryMenuRect,GUIContent.none,VoxelBoxUI.Panel);
        GUILayout.BeginArea(new Rect(categoryMenuRect.x+12,categoryMenuRect.y+10,categoryMenuRect.width-24,categoryMenuRect.height-20));
        GUILayout.Label("BLOCK SORTIEREN",VoxelBoxUI.Header);
        GUILayout.Label(categoryMenuBlock.name,VoxelBoxUI.Small);
        GUILayout.Space(5);
        for(int c=0;c<4;c++) if(GUILayout.Button("Verschieben nach  "+categoryNames[c],GUILayout.Height(34))) { SetPersonalCategory(categoryMenuBlock,c); categoryMenuBlock=null; heldBlock=null; GUILayout.EndArea(); return; }
        string fav=IsFavorite(categoryMenuBlock)?"★  Aus FAVORITEN entfernen":"☆  Zu FAVORITEN hinzufügen";
        if(GUILayout.Button(fav,GUILayout.Height(36))) { ToggleFavorite(categoryMenuBlock); categoryMenuBlock=null; heldBlock=null; GUILayout.EndArea(); return; }
        if(GUILayout.Button("Abbrechen",GUILayout.Height(28))) { categoryMenuBlock=null; heldBlock=null; GUILayout.EndArea(); return; }
        GUILayout.EndArea();
    }


	
	
}
