using UnityEngine;
using System.Collections;

public struct Box {
	
	public Vector3 min, max;
	
	public Vector3 center {
		get {
			return (max+min)/2f;
		}
		set {
			Vector3 half = size/2f;
			min = value - half;
			max = value + half;
		}
	}
	
	public Vector3 size {
		get {
			return (max-min);
		}
		set {
			Vector3 c = center;
			min = c - value/2f;
			max = c + value/2f;
		}
	}
	
	public static Box MinMax(Vector3 min, Vector3 max) {
		Box box = new Box();
		box.min = min;
		box.max = max;
		return box;
	}
	
	public static Box CenterSize(Vector3 center, Vector3 size) {
		Box box = new Box();
		box.center = center;
		box.size = size;
		return box;
	}

    public void Add(Vector3 v) {
        min += v;
        max += v;
    }
	
}