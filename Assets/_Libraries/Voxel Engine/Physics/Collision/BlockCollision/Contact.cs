using UnityEngine;
using System.Collections;

public class Contact {
	
	public Vector3 blockPoint; // точка на блоке
	public Vector3 capsulePoint; // точка на капсуле
	
	public float sqrDistance { // на сколько капсула входит в блок
		get {
			return delta.sqrMagnitude;
		}
	}
	
	public Vector3 delta {
		get {
			return blockPoint-capsulePoint;
		}
	}
	
	public Vector3 normal {
		get {
			return delta.normalized;
		}
	}
	
	public Contact(Vector3 blockPoint, Vector3 capsulePoint) {
		this.blockPoint = blockPoint;
		this.capsulePoint = capsulePoint;
	}
	
	public static Contact Min(Contact a, Contact b) {
		if(a.sqrDistance < b.sqrDistance) {
			return a;
		}
		if(a.sqrDistance == b.sqrDistance) {
			return a.blockPoint.y > b.blockPoint.y ? a : b;
		}
		return b;
	}
	
	public static Contact Max(Contact a, Contact b) {
		if(a.sqrDistance > b.sqrDistance) {
			return a;
		}
		if(a.sqrDistance == b.sqrDistance) {
			return a.blockPoint.y > b.blockPoint.y ? a : b;
		}
		return b;
	}
	
}