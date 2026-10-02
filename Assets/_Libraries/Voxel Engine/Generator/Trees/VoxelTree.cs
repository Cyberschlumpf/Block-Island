using UnityEngine;
using System.Collections;

public abstract class VoxelTree : MonoBehaviour {

	
	public abstract void Generate(Map map, Vector3i pos);
	
}

