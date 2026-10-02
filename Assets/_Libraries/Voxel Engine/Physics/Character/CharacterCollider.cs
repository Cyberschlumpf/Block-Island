using UnityEngine;
using System.Collections;

public class CharacterCollider : MonoBehaviour {
	
	public float height;
	public float radius;
	
	public Vector3 pos {
		set {
			transform.position = value;
		}
		get {
			return transform.position;
		}
	}
	
	public Vector3 top {
		get {
			return Vector3.up*(height-radius);
		}
	}
	
	public Vector3 bottom {
		get {
			return Vector3.up*radius;
		}
	}
	
	private Vector3 groundPoint, groundNormal;
	private Vector3 velocity;

	
	void OnDrawGizmosSelected() {
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(transform.position+top, radius);
		Gizmos.DrawWireSphere(transform.position+bottom, radius);
		
		if(IsGrounded()) {
			Gizmos.color = Color.red;
			Gizmos.DrawRay(groundPoint, groundNormal*0.5f);
		}
	}
	
	public void Move(Vector3 delta) {
		Vector3 oldPos = transform.position;
		transform.position += delta;
		velocity = delta;
		groundPoint = groundNormal = Vector3.zero;

		MapCharacterCollision.Collision(Map.instance, this);

		velocity = transform.position - oldPos;
		if(velocity.y > 0) {
			velocity.y = Mathf.Min(velocity.y, delta.y);
		}
	}
	
	public void OnCollision(Vector3 point, Vector3 normal, Vector3i blockPos) {
		Debug.DrawRay(point, normal/2f, Color.blue);
		if( velocity.y < 0 && normal.y > 0.001f && normal.y > groundNormal.y) {
			groundPoint = point;
			groundNormal = normal;
		}
	}
	
	public Vector3 GetVelocity() {
		return velocity;
	}
	
	public Vector3 GetGroundNormal() {
		return groundNormal;
	}
	
	public bool IsGrounded() {
		return groundNormal.y > 0;
	}
	
}
