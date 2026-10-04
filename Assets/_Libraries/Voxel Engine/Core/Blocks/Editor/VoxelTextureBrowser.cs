#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class VoxelTextureBrowser : EditorWindow
{
    BlockSet blockSet; Vector2 leftScroll, rightScroll; int selectedIndex=-1; string search="";
    int selectedFace=0; bool applyAllFaces=false;
    bool atlasDragging=false; Vector2 atlasDragStart, atlasDragNow;

    [MenuItem("Tools/The Voxel Box/Texture Browser")]
    public static void Open()=>GetWindow<VoxelTextureBrowser>("Voxel Texture Browser");

    void OnGUI(){
        blockSet=BlockSet.instance!=null?BlockSet.instance:FindFirstObjectByType<BlockSet>();
        if(blockSet==null){EditorGUILayout.HelpBox("Kein BlockSet in der geöffneten Szene gefunden.",MessageType.Info);return;}
        EditorGUILayout.BeginHorizontal(); DrawBlockList(); DrawSelectedBlock(); EditorGUILayout.EndHorizontal();
    }

    void DrawBlockList(){
        EditorGUILayout.BeginVertical(GUILayout.Width(330));
        EditorGUILayout.LabelField("Blöcke",EditorStyles.boldLabel); search=EditorGUILayout.TextField("Suche",search);
        leftScroll=EditorGUILayout.BeginScrollView(leftScroll);
        List<Block> blocks=blockSet.GetBlocks();
        for(int i=0;i<blocks.Count;i++){
            Block b=blocks[i]; if(b==null)continue;
            if(!string.IsNullOrEmpty(search)&&b.name.IndexOf(search,System.StringComparison.OrdinalIgnoreCase)<0)continue;
            EditorGUILayout.BeginHorizontal(selectedIndex==i?"SelectionRect":GUIStyle.none,GUILayout.Height(58));
            Rect p=GUILayoutUtility.GetRect(52,52,GUILayout.Width(52),GUILayout.Height(52)); b.DrawPreview(p);
            string assetPath=AssetDatabase.GetAssetPath(b);
            string typeName=b.GetType().Name;
            string location=string.IsNullOrEmpty(assetPath)?"runtime":assetPath.Replace("Assets/Resources/BlockSet/Blocks/","");
            if(GUILayout.Button("ID "+i+"   "+b.name+"   ["+typeName+"]\n"+location,EditorStyles.label,GUILayout.Height(52))){selectedIndex=i;selectedFace=0;}
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView(); EditorGUILayout.EndVertical();
    }

    void DrawSelectedBlock(){
        EditorGUILayout.BeginVertical();
        if(selectedIndex<0||selectedIndex>=blockSet.Count){EditorGUILayout.HelpBox("Links einen Block auswählen.",MessageType.Info);EditorGUILayout.EndVertical();return;}
        Block block=blockSet[selectedIndex]; EditorGUILayout.LabelField("ID "+selectedIndex+" — "+block.name,EditorStyles.boldLabel);
        EditorGUILayout.ObjectField("Block",block,typeof(Block),false);
        Face[] faces=Face.GetFaceList(block); string[] names=Face.GetFaceNameList(block);
        if(faces==null||faces.Length==0){EditorGUILayout.HelpBox("Dieser Block besitzt keine editierbaren Face-Texturen.",MessageType.Warning);EditorGUILayout.EndVertical();return;}
        selectedFace=Mathf.Clamp(selectedFace,0,faces.Length-1);
        rightScroll=EditorGUILayout.BeginScrollView(rightScroll);
        EditorGUILayout.LabelField("Seite auswählen",EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        for(int i=0;i<faces.Length;i++) if(GUILayout.Toggle(selectedFace==i,i<names.Length?names[i]:"Face "+i,"Button")) selectedFace=i;
        EditorGUILayout.EndHorizontal();
        applyAllFaces=EditorGUILayout.ToggleLeft("Auswahl auf ALLE Seiten des Blocks anwenden",applyAllFaces);
        DrawFaceEditor(block,faces,faces[selectedFace],selectedFace,names);
        EditorGUILayout.EndScrollView(); EditorGUILayout.EndVertical();
    }

    void DrawFaceEditor(Block owner,Face[] all,Face face,int index,string[] names){
        if(face==null)return;
        EditorGUILayout.Space(); EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField((index<names.Length?names[index]:"Face "+index)+" bearbeiten",EditorStyles.boldLabel);
        Material oldMat=face.material;
        EditorGUI.BeginChangeCheck();
        Material newMat=(Material)EditorGUILayout.ObjectField("Atlas-Material",oldMat,typeof(Material),false);
        if(EditorGUI.EndChangeCheck()&&newMat!=oldMat){ApplyMaterial(owner,all,face,newMat);}

        Texture2D tex=face.GetTexture();
        if(tex==null){EditorGUILayout.HelpBox("Das gewählte Material hat keine Texture2D.",MessageType.Warning);EditorGUILayout.EndVertical();return;}
        EditorGUILayout.LabelField("Gewünschten Bereich im Atlas markieren:",EditorStyles.boldLabel);
        DrawAtlasPicker(owner,all,face,tex);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Aktuell: UV "+face.rect+"   |   Material-ID "+face.materialID,EditorStyles.miniLabel);
        Rect preview=GUILayoutUtility.GetRect(128,128,GUILayout.Width(128),GUILayout.Height(128));
        GUI.DrawTextureWithTexCoords(preview,tex,face.rect);
        EditorGUILayout.LabelField("Vorschau",EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.EndVertical();
    }

    void DrawAtlasPicker(Block owner,Face[] all,Face face,Texture2D tex){
        float aspect=(float)tex.width/Mathf.Max(1,tex.height); float maxW=Mathf.Max(240,position.width-370); float w=Mathf.Min(maxW,760); float h=w/aspect;
        Rect r=GUILayoutUtility.GetRect(w,h,GUILayout.ExpandWidth(false)); GUI.DrawTexture(r,tex,ScaleMode.StretchToFill,false);

        // Aktuell verwendeten UV-Bereich immer sichtbar markieren.
        Rect currentScreen=UVToScreenRect(face.rect,r);
        EditorGUI.DrawRect(currentScreen,new Color(1f,.75f,.1f,.28f));
        Handles.BeginGUI(); Handles.color=new Color(1f,.75f,.1f,.95f); Handles.DrawAAPolyLine(2f,
            new Vector3(currentScreen.xMin,currentScreen.yMin),new Vector3(currentScreen.xMax,currentScreen.yMin),
            new Vector3(currentScreen.xMax,currentScreen.yMax),new Vector3(currentScreen.xMin,currentScreen.yMax),
            new Vector3(currentScreen.xMin,currentScreen.yMin)); Handles.EndGUI();

        Event e=Event.current;
        if(e.type==EventType.MouseDown&&e.button==0&&r.Contains(e.mousePosition)){
            atlasDragging=true; atlasDragStart=ClampToRect(e.mousePosition,r); atlasDragNow=atlasDragStart; e.Use(); Repaint();
        }
        if(atlasDragging&&e.type==EventType.MouseDrag&&e.button==0){
            atlasDragNow=ClampToRect(e.mousePosition,r); e.Use(); Repaint();
        }
        if(atlasDragging){
            Rect drag=MakeRect(atlasDragStart,atlasDragNow);
            if(drag.width>1f&&drag.height>1f){
                EditorGUI.DrawRect(drag,new Color(.2f,.65f,1f,.22f));
                Handles.BeginGUI(); Handles.color=new Color(.25f,.75f,1f,1f); Handles.DrawAAPolyLine(2f,
                    new Vector3(drag.xMin,drag.yMin),new Vector3(drag.xMax,drag.yMin),new Vector3(drag.xMax,drag.yMax),
                    new Vector3(drag.xMin,drag.yMax),new Vector3(drag.xMin,drag.yMin)); Handles.EndGUI();
            }
        }
        if(atlasDragging&&e.type==EventType.MouseUp&&e.button==0){
            atlasDragNow=ClampToRect(e.mousePosition,r); Rect drag=MakeRect(atlasDragStart,atlasDragNow); atlasDragging=false;
            // Nur echte Ziehauswahl übernehmen; ein versehentlicher Einzelklick ändert nichts.
            if(drag.width>=3f&&drag.height>=3f){ Rect uv=ScreenToUVRect(drag,r); ApplyRect(owner,all,face,uv); }
            e.Use(); Repaint();
        }

        Rect uvNow=face.rect;
        int px=Mathf.RoundToInt(uvNow.x*tex.width), py=Mathf.RoundToInt(uvNow.y*tex.height);
        int pw=Mathf.RoundToInt(uvNow.width*tex.width), ph=Mathf.RoundToInt(uvNow.height*tex.height);
        EditorGUILayout.HelpBox("Im Atlas mit gedrückter linker Maustaste den gewünschten Bereich aufziehen. Beim Loslassen wird genau dieser Ausschnitt übernommen.",MessageType.Info);
        EditorGUILayout.LabelField("Auswahl: X "+px+"  Y "+py+"  Breite "+pw+"  Höhe "+ph+" Pixel",EditorStyles.miniLabel);
    }

    Vector2 ClampToRect(Vector2 p,Rect r){ return new Vector2(Mathf.Clamp(p.x,r.xMin,r.xMax),Mathf.Clamp(p.y,r.yMin,r.yMax)); }
    Rect MakeRect(Vector2 a,Vector2 b){ float x=Mathf.Min(a.x,b.x), y=Mathf.Min(a.y,b.y); return new Rect(x,y,Mathf.Abs(a.x-b.x),Mathf.Abs(a.y-b.y)); }
    Rect UVToScreenRect(Rect uv,Rect r){
        return new Rect(r.x+uv.x*r.width, r.y+(1f-uv.y-uv.height)*r.height, uv.width*r.width, uv.height*r.height);
    }
    Rect ScreenToUVRect(Rect sr,Rect r){
        float x=Mathf.Clamp01((sr.xMin-r.x)/r.width), w=Mathf.Clamp01(sr.width/r.width);
        float top=Mathf.Clamp01((sr.yMin-r.y)/r.height), h=Mathf.Clamp01(sr.height/r.height);
        float y=Mathf.Clamp01(1f-top-h);
        return new Rect(x,y,Mathf.Min(w,1f-x),Mathf.Min(h,1f-y));
    }

    void ApplyMaterial(Block owner,Face[] all,Face target,Material mat){
        Undo.RecordObject(owner,"Voxel atlas material change");
        if(applyAllFaces){foreach(Face f in all){if(f==null)continue;f.material=mat;f.materialID=blockSet.AddMaterial(mat);}}
        else{target.material=mat;target.materialID=blockSet.AddMaterial(mat);}
        EditorUtility.SetDirty(owner); AssetDatabase.SaveAssets(); RefreshInfiniteWorld(); SceneView.RepaintAll(); Repaint();
    }
    void ApplyRect(Block owner,Face[] all,Face target,Rect rect){
        Undo.RecordObject(owner,"Voxel atlas texture change");
        if(applyAllFaces){foreach(Face f in all)if(f!=null)f.rect=rect;} else target.rect=rect;
        EditorUtility.SetDirty(owner); AssetDatabase.SaveAssets(); RefreshInfiniteWorld(); SceneView.RepaintAll(); Repaint();
    }
    void RefreshInfiniteWorld(){
        if(!Application.isPlaying)return;
        InfiniteChunkMeshRenderer renderer=FindFirstObjectByType<InfiniteChunkMeshRenderer>();
        if(renderer!=null)renderer.MarkAllVisibleDirty();
    }
}
#endif
