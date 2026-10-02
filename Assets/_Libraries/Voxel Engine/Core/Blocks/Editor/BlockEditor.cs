using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.IO;

[CustomEditor( typeof(Block), true )]
public class BlockEditor : Editor {
	
	private int selectedFace = 0;
	private Matrix4x4 atlasMatrix = Matrix4x4.identity;
	
	[MenuItem ("Assets/Create/BlockSet/GroundBlock")]
	private static void CreateGroundBlock() {
		CreateAsset<GroundBlock>();
	}
	
	[MenuItem ("Assets/Create/BlockSet/CubeBlock")]
	private static void CreateCubeBlock() {
		CreateAsset<CubeBlock>();
	}
	
	[MenuItem ("Assets/Create/BlockSet/CrossBlock")]
	private static void CreateCrossBlock() {
		CreateAsset<CrossBlock>();
	}
	
	[MenuItem ("Assets/Create/BlockSet/CactusBlock")]
	private static void CreateCactusBlock() {
		CreateAsset<CactusBlock>();
	}
	
	[MenuItem ("Assets/Create/BlockSet/FluidBlock")]
	private static void CreateFluidBlock() {
		CreateAsset<FluidBlock>();
	}
	
	[MenuItem ("Assets/Create/BlockSet/StairBlock")]
	private static void CreateStairBlock() {
		CreateAsset<StairBlock>();
	}
	
	[MenuItem ("Assets/Create/BlockSet/MeshBlock")]
	private static void CreateMeshBlock() {
		CreateAsset<MeshBlock>();
	}
	
	[MenuItem ("Assets/Create/BlockSet/GameObjectBlock")]
	private static void CreateGameObjectBlock() {
		CreateAsset<GameObjectBlock>();
	}
	
	[MenuItem ("Assets/Create/BlockSet/FenceBlock")]
	private static void CreateFenceBlock() {
		CreateAsset<FenceBlock>();
	}
	
	
	public static void CreateAsset<T>() where T : ScriptableObject {
		string path = AssetDatabase.GetAssetPath( Selection.activeObject );
		if(path == "") path = "Assets";
		if(File.Exists(path)) {
			path = Path.GetDirectoryName(path);
		}
		
		path = string.Format( "{0}/{1}.asset", path, typeof(T).Name );
		path =  AssetDatabase.GenerateUniqueAssetPath(path);
		
		T obj = ScriptableObject.CreateInstance<T>();
		ProjectWindowUtil.CreateAsset( obj, path );
	}
	
	
	private Material[] materials;
	private Mesh mesh;
	private Vector3 angles = new Vector3(0, 180, 0);
	
	void OnEnable() {
		BuildPreviewMesh();
	}
	
	public override void OnInspectorGUI() {
		Block block = (Block) target;

		MonoScript script = MonoScript.FromScriptableObject(block);
		EditorGUIUtils.AssetField("Script", script);

		using( new VerticalLayout(GUI.skin.box) ) {
			block.glow = EditorGUILayout.IntField("Light", block.glow);
			block.glow = Mathf.Clamp(block.glow, 0, 15);
			
			block.icon = EditorGUIUtils.AssetField<Texture2D>("Icon", block.icon);
			
			if(block is MeshBlock) {
				MeshBlock mesh = (MeshBlock) block;
				mesh.mesh = EditorGUIUtils.AssetField<Mesh>("Mesh", mesh.mesh);
			}
		}

		Face face = FaceListEditor.DrawFaceList(block, ref selectedFace);
		if(face != null) FaceEditor.DrawFaceEditor(face, ref atlasMatrix);


		if(GUI.changed) {
			EditorUtility.SetDirty( target );
			BuildPreviewMesh();
		}
	}


	private void BuildPreviewMesh() {
		var block = (Block) target;

        var blockSet = BlockSet.instance;
		if(blockSet == null) return;
		
		int id = blockSet.GetBlocks().IndexOf(block);
		block.Init( blockSet, id );
		
		var builder = block.Build();
		if(builder != null) {
			materials = builder.GetMaterials(blockSet.GetMaterials());
			mesh = builder.ToMesh(mesh);
		}
	}
	
	public override bool HasPreviewGUI() {
		return mesh != null;
	}
	
	public override void OnPreviewGUI(Rect rect, GUIStyle background) {
		var block = (Block) target;
		block.DrawPreview(rect);
	}
	
	public override void OnInteractivePreviewGUI(Rect rect, GUIStyle background) {
		if(Event.current.type == EventType.MouseDrag) {
			angles.x += Event.current.delta.y;
			angles.x = Mathf.Clamp(angles.x, -60, 60);
			angles.y += Event.current.delta.x;
			Event.current.Use();
		}
		
		Quaternion rotation = Quaternion.Euler(angles);
		EditorGUIUtils.DrawMeshPreview(rect, mesh, materials, rotation, 2);
	}
	
	
}
