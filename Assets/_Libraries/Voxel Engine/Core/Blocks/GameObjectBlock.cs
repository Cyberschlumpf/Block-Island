using UnityEngine;
using System.Collections;

public class GameObjectBlock : Block {
	
	private Hashtable table = new Hashtable();
	
	public GameObject gameObject;
	
	
	public override void OnBlockCreate(Vector3i pos, BlockDirection dir) {
        // Creative GameObject slots are allowed to be empty until a prefab is assigned in BlockSet.
        if(gameObject == null) return;
		Quaternion rotation = Quaternion.LookRotation( ToVector(dir) );
		GameObject go = (GameObject) GameObject.Instantiate( gameObject, pos + Vector3.one/2f, rotation );
		go.transform.parent = Map.instance.transform;
		table[pos] = go;
	}
	
	public override void OnBlockDestroy(Vector3i pos) {
		GameObject go = (GameObject)table[pos];
		table.Remove(pos);
		if(go != null) Destroy(go);
	}
	
	private static Vector3 ToVector(BlockDirection dir) {
		if(dir == BlockDirection.LEFT) return Vector3.left;
		if(dir == BlockDirection.RIGHT) return Vector3.right;
		if(dir == BlockDirection.BACKWARD) return Vector3.back;
		if(dir == BlockDirection.FORWARD) return Vector3.forward;
		return Vector3.zero;
	}

    public override void Build(MeshBuilder builder, DataBlock block, LocalPosition pos, int index, Chunk chunk) {
	}
	
	public override MeshBuilder Build() {
		return null;
	}
	
	public override bool IsSolid() {
		return gameObject != null && gameObject.GetComponent<Collider>() != null;
	}
	
	
}
