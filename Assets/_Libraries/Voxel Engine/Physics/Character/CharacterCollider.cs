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
			return (RelativityGravityController.Active?RelativityGravityController.Up:Vector3.up)*(height-radius);
		}
	}
	
	public Vector3 bottom {
		get {
			return (RelativityGravityController.Active?RelativityGravityController.Up:Vector3.up)*radius;
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
		if(RelativityGravityController.Active){ float vu=Vector3.Dot(velocity,RelativityGravityController.Up), du=Vector3.Dot(delta,RelativityGravityController.Up); if(vu>0) velocity += RelativityGravityController.Up*(Mathf.Min(vu,du)-vu); } else if(velocity.y > 0) { velocity.y = Mathf.Min(velocity.y, delta.y); }
	}
	
	public void OnCollision(Vector3 point, Vector3 normal, Vector3i blockPos) {
		Debug.DrawRay(point, normal/2f, Color.blue);
		float vn=RelativityGravityController.Active?Vector3.Dot(velocity,RelativityGravityController.Up):velocity.y; float nn=RelativityGravityController.Active?Vector3.Dot(normal,RelativityGravityController.Up):normal.y; float gn=RelativityGravityController.Active?Vector3.Dot(groundNormal,RelativityGravityController.Up):groundNormal.y;
		if( vn < 0 && nn > 0.001f && nn > gn) {
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
		return RelativityGravityController.Active ? (!RelativityGravityController.ZeroGravity && !RelativityGravityController.SectorChangeGrace && Vector3.Dot(groundNormal,RelativityGravityController.Up)>0.55f) : groundNormal.y > 0;
	}
	
}
