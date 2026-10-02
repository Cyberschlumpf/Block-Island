using UnityEngine;
using System.Collections;

public class CharacterMotor : MonoBehaviour {
	
	private CharacterCollider character;
	private CharacterMotorMoving motorMoving = new CharacterMotorMoving();
	private CharacterMotorJumping motorJumping = new CharacterMotorJumping();

	internal Vector3 inputMoveDirection = Vector3.zero;
	internal bool inputJump = false;
	internal bool holdingInputJump = false;
	
	void Awake() {
		character = GetComponent<CharacterCollider>();
	}
	
	
	void FixedUpdate() {
		Vector3 velocity = character.GetVelocity() / Time.deltaTime;
		motorMoving.ApplyMoving(this, ref velocity);
		motorMoving.ApplyGravity(this, ref velocity);
		motorJumping.ApplyJumping(this, ref velocity);
		
		character.Move( velocity*Time.deltaTime );
	}
	
	public bool IsGrounded() {
		return character.IsGrounded();
	}
	
	public bool IsJumping() {
		return motorJumping.jumping;
	}
	
	public CharacterCollider GetCollider() {
		return character;
	}
	
}

class CharacterMotorMoving {
	
	private const float moveSpeed = 6f;
	
	private const float maxGroundAcceleration = 20;
	private const float maxAirAcceleration = 5;

	public const float gravity = 20;
	private const float maxFallSpeed = 5;
	
	public void ApplyMoving(CharacterMotor motor, ref Vector3 velocity) {
		Vector3 targetVelocity = motor.inputMoveDirection * moveSpeed;
		targetVelocity.y = velocity.y;

		float maxAcceleration = GetMaxAcceleration(motor.IsGrounded()) * Time.deltaTime;
		velocity = Vector3.MoveTowards(velocity, targetVelocity, maxAcceleration);
	}
	
	public void ApplyGravity(CharacterMotor motor, ref Vector3 velocity) {
		velocity.y -= gravity * Time.deltaTime;
		velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
		if(!motor.IsJumping()) velocity.y = Mathf.Min(velocity.y, 0);
	}
	
	private static float GetMaxAcceleration(bool grounded) {
		if(grounded) return maxGroundAcceleration;
		return maxAirAcceleration;
	}
	
}

class CharacterMotorJumping {
	
	private const float baseHeight = 0.5f;
	private const float extraHeight = 1.4f;
	
	private float jumpStartTime;
	public bool jumping = false;
	
	public void ApplyJumping(CharacterMotor motor, ref Vector3 velocity) {
		if(motor.IsGrounded() && jumping) {
			jumping = false;
		}

		if (jumping && motor.holdingInputJump) {
			if (Time.time - jumpStartTime < extraHeight / CalculateJumpForce(baseHeight)) {
				velocity += Vector3.up * CharacterMotorMoving.gravity * Time.deltaTime;
			}
		}

		if (motor.IsGrounded() && motor.inputJump) {
			jumping = true;
			jumpStartTime = Time.time;
			
			velocity.y = 0;
			velocity += Vector3.up * CalculateJumpForce(baseHeight);
		}
	}
	
	
	private static float CalculateJumpForce(float targetJumpHeight) {
		return Mathf.Sqrt(2 * targetJumpHeight * CharacterMotorMoving.gravity);
	}
	
}