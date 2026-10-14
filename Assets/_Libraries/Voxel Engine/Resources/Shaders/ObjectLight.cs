using UnityEngine;
using System.Collections;

public class ObjectLight : MonoBehaviour {
	
	private Material material;
	
	void Start() {
		material = FindMaterial( transform );
	}
	
	private static Material FindMaterial(Transform trans) {
		if(trans.GetComponent<Renderer>() != null) return trans.GetComponent<Renderer>().material;
		foreach(Transform child in trans) {
			Material mat = FindMaterial(child);
			if(mat != null) return mat;
		}
		return null;
	}
	
	
	void Update () {
        Vector3i pos = Vector3i.Round( transform.localPosition );
        Color32 color = LightComputerUtils.GetBlockLight( pos );
        material.SetColor( "_Light", color );
	}
	
	
	void OnBecameVisible() {
		enabled = false;
	}
	
	void OnBecameInvisible() {
		enabled = true;
	}
	
	
}
