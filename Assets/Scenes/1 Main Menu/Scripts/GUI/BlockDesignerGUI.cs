using UnityEngine;
using System.IO;
public static class BlockDesignerGUI {
 static int target=1,source=1,sourceTab=0,customSelected=-1,atlasSelected=0; static Vector2 leftScroll,rightScroll; static string status=""; static GameObject menuBlockSet; static string[] customFiles=new string[0]; static string[] atlasNames={"BASE ATLAS","TERRAIN ATLAS","FORTRESSCRAFT"}; static string[] atlasResources={"BlockSet/Atlases/BlockIslandLegacy/Recovered Base Atlas","BlockSet/Atlases/BlockIslandLegacy/Recovered Terrain Atlas","BlockSet/Atlases/BlockIslandLegacy/FortressCraft Designer Atlas"}; static float atlasZoom=1f; static Vector2 atlasPan=Vector2.zero,atlasDragStart; static bool atlasSelecting=false; static int atlasHandleDrag=0; static Rect atlasSelection=new Rect(0,0,1f/8f,1f/8f);
 // 0.9.9.7f: live 3D preview renderer. It uses the block's real Build() mesh and
 // BlockSet materials, so stairs/fences/spheres/plants are shown as geometry, not as a flat icon.
 static GameObject previewRoot; static Camera previewCamera; static Light previewKey,previewFill; static MeshFilter previewFilter; static MeshRenderer previewRenderer; static RenderTexture previewRT; static Mesh previewMesh; static int previewBlock=-1; static float previewYaw=32f,previewPitch=20f,previewZoom=2.35f;
 static void Ensure3DPreview(){
  if(previewRoot!=null&&previewCamera!=null&&previewRT!=null)return;
  previewRoot=new GameObject("Block Designer 3D Preview"); previewRoot.hideFlags=HideFlags.HideAndDontSave; previewRoot.layer=31;
  previewFilter=previewRoot.AddComponent<MeshFilter>(); previewRenderer=previewRoot.AddComponent<MeshRenderer>(); previewRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On; previewRenderer.receiveShadows=true;
  GameObject cg=new GameObject("Block Designer Preview Camera");cg.hideFlags=HideFlags.HideAndDontSave;cg.layer=31;previewCamera=cg.AddComponent<Camera>();previewCamera.enabled=false;previewCamera.cullingMask=1<<31;previewCamera.clearFlags=CameraClearFlags.SolidColor;previewCamera.backgroundColor=new Color(.025f,.035f,.04f,1);previewCamera.fieldOfView=31f;previewCamera.nearClipPlane=.05f;previewCamera.farClipPlane=20f;
  previewRT=new RenderTexture(320,320,24,RenderTextureFormat.ARGB32);previewRT.name="Block Designer Preview RT";previewRT.hideFlags=HideFlags.HideAndDontSave;previewRT.Create();previewCamera.targetTexture=previewRT;
  GameObject kg=new GameObject("Block Designer Preview Key");kg.hideFlags=HideFlags.HideAndDontSave;kg.layer=31;previewKey=kg.AddComponent<Light>();previewKey.type=LightType.Directional;previewKey.intensity=1.05f;previewKey.cullingMask=1<<31;kg.transform.rotation=Quaternion.Euler(42f,-38f,0);
  GameObject fg=new GameObject("Block Designer Preview Fill");fg.hideFlags=HideFlags.HideAndDontSave;fg.layer=31;previewFill=fg.AddComponent<Light>();previewFill.type=LightType.Directional;previewFill.intensity=.42f;previewFill.cullingMask=1<<31;fg.transform.rotation=Quaternion.Euler(320f,145f,0);
 }
 static bool Rebuild3DPreview(BlockSet set,int id){
  Ensure3DPreview();if(set==null||id<0||id>=set.Count||set[id]==null)return false;Block b=set[id];MeshBuilder mb=null;try{mb=b.Build();}catch(System.Exception e){Debug.LogWarning("Block Designer 3D preview: "+e.Message);}if(mb==null||mb.vertices.Count==0){previewFilter.sharedMesh=null;previewRenderer.sharedMaterials=new Material[0];previewBlock=id;return false;}
  previewMesh=mb.ToMesh(previewMesh);if(previewMesh==null)return false;previewMesh.name="Block Designer Preview Mesh";previewMesh.hideFlags=HideFlags.HideAndDontSave;previewFilter.sharedMesh=previewMesh;previewRenderer.sharedMaterials=mb.GetMaterials(set.GetMaterials());Bounds bo=previewMesh.bounds;previewRoot.transform.position=-bo.center;previewBlock=id;return true;
 }
 static void Draw3DPreview(BlockSet set,int id,Rect r){
  if(previewBlock!=id||previewFilter==null||previewFilter.sharedMesh==null)Rebuild3DPreview(set,id);Ensure3DPreview();
  Event e=Event.current;if(r.Contains(e.mousePosition)){if(e.type==EventType.MouseDrag&&e.button==0){previewYaw+=e.delta.x*.8f;previewPitch=Mathf.Clamp(previewPitch-e.delta.y*.65f,-70f,70f);e.Use();}else if(e.type==EventType.ScrollWheel){previewZoom=Mathf.Clamp(previewZoom+e.delta.y*.10f,1.45f,4.2f);e.Use();}}
  GUI.Box(r,GUIContent.none);if(previewFilter.sharedMesh==null){GUI.Label(new Rect(r.x+8,r.y+55,r.width-16,45),"Für diesen Slot ist kein Block-Mesh hinterlegt.",VoxelBoxUI.Small);return;}
  Quaternion orbit=Quaternion.Euler(previewPitch,previewYaw,0);Vector3 dir=orbit*new Vector3(0,0,-1);previewCamera.transform.position=dir*previewZoom;previewCamera.transform.LookAt(Vector3.zero,Vector3.up);previewCamera.Render();GUI.DrawTexture(new Rect(r.x+4,r.y+4,r.width-8,r.height-8),previewRT,ScaleMode.ScaleToFit,false);GUI.Label(new Rect(r.x+7,r.y+r.height-23,r.width-14,20),"Ziehen: drehen  •  Mausrad: Zoom",VoxelBoxUI.Small);
 }
 static BlockSet EnsureBlockSet(){if(BlockSet.instance!=null)return BlockSet.instance;GameObject prefab=Resources.Load<GameObject>("BlockSet");if(prefab==null)return null;menuBlockSet=Object.Instantiate(prefab);menuBlockSet.name="BlockSet (Main Menu Preview)";RefreshFiles();return BlockSet.instance!=null?BlockSet.instance:menuBlockSet.GetComponent<BlockSet>();}
 static void RefreshFiles(){BlockTextureDesigner.ReloadCustomTextures();customFiles=BlockTextureDesigner.CustomFiles();if(customSelected>=customFiles.Length)customSelected=customFiles.Length-1;status=customFiles.Length+" eigene PNG-Textur(en) gefunden.";}
 public static void Draw(){
  BlockSet set=EnsureBlockSet();if(set==null){GUILayout.Label("BlockSet konnte nicht geladen werden.",VoxelBoxUI.Label);return;}if(set.Count<=0)return;target=Mathf.Clamp(target,0,set.Count-1);source=Mathf.Clamp(source,0,set.Count-1);Block tb=set[target];
  GUILayout.BeginHorizontal();
  GUILayout.BeginVertical(VoxelBoxUI.Card,GUILayout.Width(250));VoxelBoxUI.SectionTitle("1. BLOCK AUSWÄHLEN");leftScroll=GUILayout.BeginScrollView(leftScroll,GUILayout.Height(520));for(int i=0;i<set.Count;i++){Block b=set[i];if(b==null)continue;if(GUILayout.Button("ID "+i.ToString("000")+"   "+b.name,(i==target?VoxelBoxUI.TabActive:VoxelBoxUI.Tab),GUILayout.Height(30))){target=i;tb=b;if(!BlockTextureDesigner.Compatible(tb,set[source]))source=target;status="";}}GUILayout.EndScrollView();GUILayout.EndVertical();GUILayout.Space(8);
  GUILayout.BeginVertical(VoxelBoxUI.Card,GUILayout.Width(300));VoxelBoxUI.SectionTitle("VORSCHAU");GUILayout.Label("ID "+target.ToString("000")+"  •  "+tb.name,VoxelBoxUI.Label);GUILayout.Label("Grundstruktur: "+BlockTextureDesigner.Structure(tb),VoxelBoxUI.Small);Rect pr=GUILayoutUtility.GetRect(176,176,GUILayout.ExpandWidth(false));Draw3DPreview(set,target,pr);string cf=BlockTextureDesigner.OverrideCustom(target);string ar=BlockTextureDesigner.OverrideAtlas(target);int ov=BlockTextureDesigner.OverrideSource(target);GUILayout.Label(!string.IsNullOrEmpty(ar)?"Atlas-Ausschnitt: "+ar.Substring(ar.LastIndexOf('/')+1):(!string.IsNullOrEmpty(cf)?"Eigene Textur: "+cf:(ov<0?"Textur: ORIGINAL":"Vorlage: ID "+ov.ToString("000")+" – "+set[ov].name)),VoxelBoxUI.Label);if(GUILayout.Button("ORIGINAL WIEDERHERSTELLEN",VoxelBoxUI.Danger,GUILayout.Height(38))){status=BlockTextureDesigner.Restore(set,target)?"Originaltextur wiederhergestellt.":"Original ist bereits aktiv.";previewBlock=-1;}GUILayout.Space(5);GUILayout.Label("Globale Zuordnung – gilt in allen Welten.",VoxelBoxUI.Small);GUILayout.EndVertical();GUILayout.Space(8);
  GUILayout.BeginVertical(VoxelBoxUI.Card,GUILayout.Width(500));VoxelBoxUI.SectionTitle("2. TEXTURQUELLE");GUILayout.BeginHorizontal();if(GUILayout.Button("SPIEL-ATLANTEN",sourceTab==0?VoxelBoxUI.TabActive:VoxelBoxUI.Tab,GUILayout.Height(36),GUILayout.ExpandWidth(true)))sourceTab=0;if(GUILayout.Button("EIGENE PNG",sourceTab==1?VoxelBoxUI.TabActive:VoxelBoxUI.Tab,GUILayout.Height(36),GUILayout.ExpandWidth(true)))sourceTab=1;if(GUILayout.Button("ATLAS-AUSSCHNITT",sourceTab==2?VoxelBoxUI.TabActive:VoxelBoxUI.Tab,GUILayout.Height(36),GUILayout.ExpandWidth(true)))sourceTab=2;GUILayout.EndHorizontal();GUILayout.Space(5);
  if(sourceTab==0)DrawBuiltIns(set,tb);else if(sourceTab==1)DrawCustom(set,tb);else DrawAtlasPicker(set,tb);
  if(!string.IsNullOrEmpty(status))GUILayout.Label(status,VoxelBoxUI.Small);GUILayout.EndVertical();GUILayout.EndHorizontal();
 }
 static void DrawBuiltIns(BlockSet set,Block tb){GUILayout.Label("Vorhandene Texturen des Spiels. Es werden passende Grundstrukturen angeboten.",VoxelBoxUI.Small);rightScroll=GUILayout.BeginScrollView(rightScroll,GUILayout.Height(350));for(int i=0;i<set.Count;i++){Block b=set[i];if(b==null||!BlockTextureDesigner.Compatible(tb,b))continue;GUILayout.BeginHorizontal();Rect rr=GUILayoutUtility.GetRect(38,38,GUILayout.Width(38));b.DrawPreview(rr);if(GUILayout.Button("ID "+i.ToString("000")+"  "+b.name,(i==source?VoxelBoxUI.TabActive:VoxelBoxUI.Tab),GUILayout.Height(38)))source=i;GUILayout.EndHorizontal();}GUILayout.EndScrollView();GUILayout.Label("Vorlage: ID "+source.ToString("000")+" – "+set[source].name,VoxelBoxUI.Label);if(GUILayout.Button("TEXTUR ÜBERNEHMEN",VoxelBoxUI.Button,GUILayout.Height(44))){status=BlockTextureDesigner.Apply(set,target,source)?"Globale Textur gespeichert.":"Grundstrukturen nicht kompatibel.";previewBlock=-1;}}
 static void DrawCustom(BlockSet set,Block tb){GUILayout.Label("Eigene PNG-Texturen",VoxelBoxUI.Label);GUILayout.Label("Ordner: Dokumente / The Voxel Box / BlockTextures / Textures",VoxelBoxUI.Small);GUILayout.Label("PNG hineinlegen und danach NEU LADEN wählen.",VoxelBoxUI.Small);GUILayout.BeginHorizontal();if(GUILayout.Button("ORDNER ÖFFNEN",VoxelBoxUI.Button,GUILayout.Height(34),GUILayout.ExpandWidth(true)))BlockTextureDesigner.OpenTextureFolder();if(GUILayout.Button("NEU LADEN",VoxelBoxUI.Tab,GUILayout.Height(34),GUILayout.Width(120)))RefreshFiles();GUILayout.EndHorizontal();GUILayout.Space(5);rightScroll=GUILayout.BeginScrollView(rightScroll,GUILayout.Height(300));if(customFiles.Length==0)GUILayout.Label("Noch keine PNG-Dateien gefunden.",VoxelBoxUI.Label);for(int i=0;i<customFiles.Length;i++){Texture2D t=BlockTextureDesigner.LoadCustomTexture(customFiles[i]);GUILayout.BeginHorizontal();Rect rr=GUILayoutUtility.GetRect(48,48,GUILayout.Width(48));if(t!=null)GUI.DrawTexture(rr,t,ScaleMode.ScaleToFit,true);string n=Path.GetFileName(customFiles[i]);if(GUILayout.Button(n,(i==customSelected?VoxelBoxUI.TabActive:VoxelBoxUI.Tab),GUILayout.Height(48)))customSelected=i;GUILayout.EndHorizontal();}GUILayout.EndScrollView();if(customSelected>=0&&customSelected<customFiles.Length){GUILayout.Label("Ausgewählt: "+Path.GetFileName(customFiles[customSelected]),VoxelBoxUI.Small);if(GUILayout.Button("EIGENE TEXTUR ÜBERNEHMEN",VoxelBoxUI.Button,GUILayout.Height(44))){status=BlockTextureDesigner.ApplyCustom(set,target,customFiles[customSelected])?"Eigene Textur global gespeichert.":"PNG konnte nicht angewendet werden.";previewBlock=-1;}}else GUILayout.Label("PNG auswählen, um sie auf den gewählten Block zu legen.",VoxelBoxUI.Small);}
 static void DrawAtlasPicker(BlockSet set,Block tb){
  GUILayout.Label("Atlas wählen • Mausrad: zoomen • Rechts/Mitte ziehen: verschieben • Links ziehen: Ausschnitt markieren • rote Punkte: Ecken präzise nachziehen",VoxelBoxUI.Small);
  GUILayout.BeginHorizontal();for(int i=0;i<atlasNames.Length;i++){if(GUILayout.Button(atlasNames[i],i==atlasSelected?VoxelBoxUI.TabActive:VoxelBoxUI.Tab,GUILayout.Height(30))){atlasSelected=i;atlasZoom=1f;atlasPan=Vector2.zero;}}if(GUILayout.Button("ZENTRIEREN",VoxelBoxUI.Tab,GUILayout.Width(90),GUILayout.Height(30))){atlasZoom=1f;atlasPan=Vector2.zero;}GUILayout.EndHorizontal();
  Texture2D tex=Resources.Load<Texture2D>(atlasResources[atlasSelected]);if(tex==null){GUILayout.Label("Atlas konnte nicht geladen werden.",VoxelBoxUI.Label);return;}
  Rect view=GUILayoutUtility.GetRect(470,300,GUILayout.ExpandWidth(true));GUI.Box(view,GUIContent.none,VoxelBoxUI.Card);Rect clip=new Rect(view.x+4,view.y+4,view.width-8,view.height-8);
  float fit=Mathf.Min(clip.width/tex.width,clip.height/tex.height);float dw=tex.width*fit*atlasZoom,dh=tex.height*fit*atlasZoom;Rect img=new Rect(clip.center.x-dw*.5f+atlasPan.x,clip.center.y-dh*.5f+atlasPan.y,dw,dh);
  GUI.BeginClip(clip);Rect localImg=new Rect(img.x-clip.x,img.y-clip.y,img.width,img.height);GUI.DrawTexture(localImg,tex,ScaleMode.StretchToFill,false);
  Rect sr=new Rect(localImg.x+atlasSelection.x*localImg.width,localImg.y+(1f-atlasSelection.y-atlasSelection.height)*localImg.height,atlasSelection.width*localImg.width,atlasSelection.height*localImg.height);
  Color oc=GUI.color;GUI.color=new Color(.05f,1f,.18f,.22f);GUI.DrawTexture(sr,Texture2D.whiteTexture);GUI.color=new Color(.15f,1f,.25f,1f);float bw=3f;GUI.DrawTexture(new Rect(sr.x,sr.y,sr.width,bw),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(sr.x,sr.yMax-bw,sr.width,bw),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(sr.x,sr.y,bw,sr.height),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(sr.xMax-bw,sr.y,bw,sr.height),Texture2D.whiteTexture);GUI.color=new Color(1f,.15f,.08f,1f);float hs=7f;GUI.DrawTexture(new Rect(sr.x-hs*.5f,sr.y-hs*.5f,hs,hs),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(sr.xMax-hs*.5f,sr.yMax-hs*.5f,hs,hs),Texture2D.whiteTexture);GUI.color=oc;GUI.EndClip();
  Event e=Event.current;bool inside=clip.Contains(e.mousePosition);if(inside&&e.type==EventType.ScrollWheel){float old=atlasZoom;atlasZoom=Mathf.Clamp(atlasZoom*(e.delta.y<0?1.18f:.85f),1f,12f);float k=atlasZoom/old;Vector2 m=e.mousePosition-clip.center;atlasPan=(atlasPan-m)*k+m;e.Use();}
  if(inside&&e.type==EventType.MouseDown&&(e.button==1||e.button==2)){atlasDragStart=e.mousePosition;e.Use();}
  if(e.type==EventType.MouseDrag&&(e.button==1||e.button==2)){atlasPan+=e.delta;float maxX=Mathf.Max(0f,(dw-clip.width)*.5f),maxY=Mathf.Max(0f,(dh-clip.height)*.5f);atlasPan.x=Mathf.Clamp(atlasPan.x,-maxX,maxX);atlasPan.y=Mathf.Clamp(atlasPan.y,-maxY,maxY);e.Use();}
  Vector2 handleA=new Vector2(img.x+atlasSelection.x*img.width,img.y+(1f-atlasSelection.y-atlasSelection.height)*img.height);
  Vector2 handleB=new Vector2(img.x+(atlasSelection.x+atlasSelection.width)*img.width,img.y+(1f-atlasSelection.y)*img.height);
  const float handleHit=15f;
  if(inside&&e.type==EventType.MouseDown&&e.button==0){
   if(Vector2.Distance(e.mousePosition,handleA)<=handleHit){atlasHandleDrag=1;atlasSelecting=false;e.Use();}
   else if(Vector2.Distance(e.mousePosition,handleB)<=handleHit){atlasHandleDrag=2;atlasSelecting=false;e.Use();}
   else{atlasSelecting=true;atlasHandleDrag=0;atlasDragStart=e.mousePosition;e.Use();}
  }
  if(e.type==EventType.MouseDrag&&e.button==0){
   if(atlasHandleDrag!=0){MoveAtlasHandle(atlasHandleDrag,e.mousePosition,img);e.Use();}
   else if(atlasSelecting){SetAtlasSelection(atlasDragStart,e.mousePosition,img);e.Use();}
  }
  if(e.type==EventType.MouseUp&&e.button==0){
   if(atlasHandleDrag!=0){MoveAtlasHandle(atlasHandleDrag,e.mousePosition,img);atlasHandleDrag=0;e.Use();}
   else if(atlasSelecting){SetAtlasSelection(atlasDragStart,e.mousePosition,img);atlasSelecting=false;e.Use();}
  }
  GUILayout.Label("Auswahl UV: "+atlasSelection.x.ToString("0.000")+", "+atlasSelection.y.ToString("0.000")+"   "+atlasSelection.width.ToString("0.000")+" × "+atlasSelection.height.ToString("0.000"),VoxelBoxUI.Small);
  if(GUILayout.Button("ATLAS-AUSSCHNITT ÜBERNEHMEN",VoxelBoxUI.Button,GUILayout.Height(44))){status=BlockTextureDesigner.ApplyAtlas(set,target,atlasResources[atlasSelected],atlasSelection)?"Atlas-Ausschnitt global gespeichert.":"Atlas-Ausschnitt konnte nicht angewendet werden.";previewBlock=-1;}
 }
 static void MoveAtlasHandle(int handle,Vector2 mouse,Rect img){
  float x=Mathf.Clamp01((mouse.x-img.x)/img.width);float gy=Mathf.Clamp01((mouse.y-img.y)/img.height);float y=1f-gy;
  float x0=atlasSelection.x,y0=atlasSelection.y,x1=atlasSelection.xMax,y1=atlasSelection.yMax;
  if(handle==1){x0=Mathf.Min(x,x1-.002f);y1=Mathf.Max(y,y0+.002f);}
  else{x1=Mathf.Max(x,x0+.002f);y0=Mathf.Min(y,y1-.002f);}
  atlasSelection=Rect.MinMaxRect(Mathf.Clamp01(x0),Mathf.Clamp01(y0),Mathf.Clamp01(x1),Mathf.Clamp01(y1));
 }
 static void SetAtlasSelection(Vector2 a,Vector2 b,Rect img){
  float x0=Mathf.Clamp((Mathf.Min(a.x,b.x)-img.x)/img.width,0,1),x1=Mathf.Clamp((Mathf.Max(a.x,b.x)-img.x)/img.width,0,1);
  float gy0=Mathf.Clamp((Mathf.Min(a.y,b.y)-img.y)/img.height,0,1),gy1=Mathf.Clamp((Mathf.Max(a.y,b.y)-img.y)/img.height,0,1);
  if(x1-x0<.002f||gy1-gy0<.002f)return;atlasSelection=new Rect(x0,1f-gy1,x1-x0,gy1-gy0);
 }

}
