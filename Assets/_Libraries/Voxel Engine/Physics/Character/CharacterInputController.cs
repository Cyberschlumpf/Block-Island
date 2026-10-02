using UnityEngine;
using System.Collections;

public class CharacterInputController : MonoBehaviour {
	
	private CharacterMotor movementMotor;
	private CharacterMotorSwimming swimmingMotor;
	private CharacterCollider characterCollider;
	[SerializeField] private float flySpeed = 10f;
	// 0.9.9.8b: Creative/Sky workflow starts in flight mode by default. F still toggles flight.
	private bool flying = true;
	
	private float jumpPressedTime = -100;

	// Use this for initialization
	void Awake() {
		movementMotor = GetComponent<CharacterMotor>();
		swimmingMotor = GetComponent<CharacterMotorSwimming>();
		characterCollider = GetComponent<CharacterCollider>();
		// Apply the default immediately so gravity does not pull the player down on the first frame.
		if(flying) { movementMotor.enabled = false; swimmingMotor.enabled = false; }
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetKeyDown(KeyCode.F) && GameState.IsPlaying && !Cursor.visible) {
			flying = !flying;
			movementMotor.enabled = !flying;
			swimmingMotor.enabled = false;
		}

		if(flying) {
			movementMotor.enabled = false;
			swimmingMotor.enabled = false;
			if(Cursor.visible || !GameState.IsPlaying) return;

			// 1.02: True look-direction flight. In flight mode the camera/view angle defines
			// the complete movement plane: looking up/down makes forward movement climb/dive,
			// and left/right strafing remains relative to the current view. No separate
			// Space/Shift altitude controls are required while flying.
			Transform view = Camera.main != null ? Camera.main.transform : transform;
			float horizontal = Input.GetAxis("Horizontal");
			float forward = Input.GetAxis("Vertical");
			Vector3 flyDirection = view.right * horizontal + view.forward * forward;
			flyDirection = Vector3.ClampMagnitude(flyDirection, 1f);
			characterCollider.Move(flyDirection * flySpeed * Time.deltaTime);
			return;
		}

		Vector3 direction = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
		direction = Vector3.ClampMagnitude(direction, 1);
		
		if(swimmingMotor.IsInWater()) {
			swimmingMotor.enabled = true;
			movementMotor.enabled = false;
			
			swimmingMotor.inputEmersion = Input.GetButton("Jump");
			swimmingMotor.inputMoveDirection = transform.TransformDirection(direction);
		} else {
			swimmingMotor.enabled = false;
			movementMotor.enabled = true;
			
			movementMotor.inputMoveDirection = transform.TransformDirection(direction);
			
			if(Input.GetButtonDown("Jump")) {
				jumpPressedTime = Time.time;
			}
			if( !Input.GetButton("Jump") ) {
				jumpPressedTime = -100;
			}
			
			movementMotor.inputJump = Time.time - jumpPressedTime <= 0.2f;
			movementMotor.holdingInputJump = Input.GetButton("Jump");
		}
	}
	
}
