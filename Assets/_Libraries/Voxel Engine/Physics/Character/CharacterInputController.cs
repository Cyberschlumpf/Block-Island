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
		if(RelativityGravityController.Active) flying=true;
		if(flying) { movementMotor.enabled = false; swimmingMotor.enabled = false; }
	}
	
	// Update is called once per frame
	void Update () {
		// Relativity is a walking/gravity level: never inherit Creative flight from scene initialization.
		if(RelativityGravityController.Active && !flying) { flying=true; movementMotor.enabled=false; swimmingMotor.enabled=false; }
		if(Input.GetKeyDown(KeyCode.F) && GameState.IsPlaying && !Cursor.visible && !RelativityGravityController.Active) {
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

			// 1.0.34: Restore the accepted free-flight controls from the pre-sphere build.
			// There is deliberately NO fixed world-up in the Sphere-64 level:
			// - WASD flies in the complete camera/view direction (including straight up/down).
			// - Space / Shift additionally move along the camera's own up/down axis.
			// Together with MouseLook's unlimited 6DOF pitch this allows the player to
			// fly and orient on the ceiling, floor or any side of the sphere.
			if(RelativityGravityController.Active) {
				if(Input.GetButton("Jump")) flyDirection += view.up;
				if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) flyDirection -= view.up;
			}

			flyDirection = Vector3.ClampMagnitude(flyDirection, 1f);
			characterCollider.Move(flyDirection * flySpeed * Time.deltaTime);
			return;
		}

		// At the exact hall centre gravity vanishes: WASD follows the view and Space/Shift move vertically.
		if(RelativityGravityController.ZeroGravity) {
			Transform view=Camera.main!=null?Camera.main.transform:transform;
			Vector3 free=view.right*Input.GetAxis("Horizontal")+view.forward*Input.GetAxis("Vertical");
			if(Input.GetButton("Jump")) free+=view.up;
			if(Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift)) free-=view.up;
			movementMotor.inputMoveDirection=Vector3.ClampMagnitude(free,1f);
			movementMotor.inputJump=false; movementMotor.holdingInputJump=false; return;
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
			
			movementMotor.inputMoveDirection = RelativityGravityController.Active ? RelativityGravityController.Planar(transform.TransformDirection(direction)).normalized : transform.TransformDirection(direction);
			
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
