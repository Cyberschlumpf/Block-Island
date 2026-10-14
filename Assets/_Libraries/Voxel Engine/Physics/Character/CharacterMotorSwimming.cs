using UnityEngine;
using System.Collections;

public class CharacterMotorSwimming : MonoBehaviour {
	
	private const float moveSpeed = 4f;
	private const float maxAcceleration = 8;
	
	private const float gravity = 10;
	private const float maxFallSpeed = 3;
	
	private CharacterCollider character;
	
	[System.NonSerialized]
	public Vector3 inputMoveDirection = Vector3.zero;
	
	[System.NonSerialized]
	public bool inputEmersion = false;
	
	
	void Awake() {
		character = GetComponent<CharacterCollider>();
	}
	
	
	void FixedUpdate() {
		Vector3 velocity = character.GetVelocity() / Time.deltaTime;
		velocity.y *= 0.98f;
		ApplyMoving(ref velocity);
		ApplyGravity(ref velocity);
		ApplyEmersion(ref velocity);
		
		character.Move( velocity*Time.deltaTime );
	}
	
	
	private void ApplyMoving(ref Vector3 velocity) {
		Vector3 desiredVelocity = inputMoveDirection * moveSpeed;
		Vector3 delta = desiredVelocity - new Vector3(velocity.x, 0, velocity.z);
		velocity += Vector3.ClampMagnitude(delta, maxAcceleration);
	}
	
	private void ApplyGravity(ref Vector3 velocity) {
		velocity.y -= gravity * Time.deltaTime;
		velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
	}
	
	private void ApplyEmersion(ref Vector3 velocity) {
		if(inputEmersion) {
			float waterLevel = GetImmersionLevel();
			
			float cos = (Mathf.Cos(Time.time*3)+1)/2f;
			float hSpeed = new Vector2(velocity.x, velocity.z).magnitude;
			float k = Mathf.Clamp(hSpeed/4f, 0, 0.3f) * cos;
			float force = 2*gravity * waterLevel*(0.75f+k);
			
			//if(character.IsCollisionStay()) {
				//force = 2*gravity * waterLevel*1.2f;
			//}
			
			velocity.y += force * Time.deltaTime;
			velocity.y = Mathf.Min(velocity.y, maxFallSpeed);
		}
	}
	
	
	
	public bool IsInWater() {
		return GetImmersionLevel() > 0;
	}
	
	private float GetImmersionLevel() {
		float waterY = GetWaterMaxY();
		Vector3 pos = transform.position;
		float level = (waterY - pos.y)/character.height;
		return Mathf.Clamp01(level);
	}
	
	private float GetWaterMaxY() {
		Vector3 pos = transform.position;
		int x = Mathf.RoundToInt(pos.x);
		int y = Mathf.RoundToInt(pos.y+0.1f);
		int z = Mathf.RoundToInt(pos.z);
		
		Map map = Map.instance;
		int maxY = 0;
		for(int i=0; i<3; i++) {
			DataBlock block = map.GetBlock(x, y+i, z);
			if(block.IsFluid()) {
				maxY = y+i;
			} else {
				break;
			}
		}
		if(maxY == y && map.GetBlock(x, y-1, z).IsSolid()) return 0;
		return maxY + 0.5f;
	}
	
}
