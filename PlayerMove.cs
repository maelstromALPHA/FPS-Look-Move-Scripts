using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// KB/M based customisable FPS movement script, written by Martin Howard (@maelstromALPHA)
// Attach this script to the player object in your scene to work

public class PlayerMove : MonoBehaviour {

	// Boolean that allows you to save your custom values from changes in the inspector
	[SerializeField] private bool useCustomValues;

	// Serialized private & customisable variables, with values set in the inspector
	[SerializeField] private string horizontalInputName, verticalInputName;
	[SerializeField] private float walkSpeed, defaultSpeed, runSpeed, runBuildUpSpeed, jumpMultiplier, slopeForce, slopeForceRayLength;
	[SerializeField] private AnimationCurve jumpFallOff;
	[SerializeField] private KeyCode jumpKey, runKey, crouchKey, walkToggleKey;

	// Player states
	public bool isJumping { get; set; }
	public bool isWalking { get; set; }
	public bool isCrouching { get; set; }
	public bool isRunning { get; set; }
	public bool isStationary { get; set; }

	// Private variables
	private CharacterController cc;
	private float movementSpeed, verticalInput, horizontalInput;

    private void Awake(){
		cc = GetComponent<CharacterController> ();

		// If "use custom values" is not ticked in the inspector, set default values on awake
		if (!useCustomValues){	
			jumpFallOff.AddKey (0.0f, 1.0f);
			jumpFallOff.AddKey (0.65f, 0.0f);
			horizontalInputName = "Horizontal";
			verticalInputName = "Vertical";
			walkSpeed = 2.5f; 
			defaultSpeed = 6; 
			runSpeed = 12.5f;
			runBuildUpSpeed = 2;
			jumpMultiplier = 10;
			slopeForce = 25;
			slopeForceRayLength = 3;
			jumpKey = KeyCode.Space;
			runKey = KeyCode.LeftShift;
			crouchKey = KeyCode.LeftControl;
			walkToggleKey = KeyCode.Backslash;
		}
	}

	// Update is called once per frame
	void Update () {
		Move ();	
	}

	private void Move(){
		// Capture the user input on the horizontal and vertical axis
		horizontalInput = Input.GetAxis (horizontalInputName);
		verticalInput = Input.GetAxis (verticalInputName);

		// Take the user input on both axis and convert it to movement in the transform's forward and right vectors
		Vector3 forwardMovement = transform.forward * verticalInput;
		Vector3 sidewaysMovement = transform.right * horizontalInput;

		// If the player is not moving, set them to a stationary state
		if (horizontalInput == 0 && verticalInput == 0) {
			isStationary = true;
		} 
		else {
			isStationary = false;
		}

		// Move the character using these values multiplied by the player movement speed, clamping the magnitude of
		// this movement to ensure that diagonal movement is the same speed as horizontal/vertical movement
		cc.SimpleMove(Vector3.ClampMagnitude(forwardMovement + sidewaysMovement, 1.0f) * movementSpeed);

		// Apply a downward force if the player is on a slope and moving, in order to make movement smoother
		if ((verticalInput != 0 || horizontalInput != 0) && OnSlope ()) {
			cc.Move (Vector3.down * cc.height / 2 * slopeForce * Time.deltaTime);
		}

		// Set the player's movement speed and call the method for player jump if necessary
		SetMovementSpeed ();
		ToggleWalk ();
		Jump ();
		Crouch ();
	}

	// Function to set the player's movement speed based on key input
	private void SetMovementSpeed(){
		// Only let the player run if they are holding down the run key and are not crouching
		if (Input.GetKey (runKey) && !isCrouching) {
			// Make sure the player's walk status is reset when they start running
			isWalking = false;
			isRunning = true;
			movementSpeed = Mathf.Lerp (movementSpeed, runSpeed, Time.deltaTime * runBuildUpSpeed);
		} 
		// If the player is walking or crouching, set their speed to the "walkSpeed" variable
		else if (isWalking || isCrouching) {
			movementSpeed = Mathf.Lerp (movementSpeed, walkSpeed, Time.deltaTime * runBuildUpSpeed);
		} 
		// Otherwise, set the players speed back to the "defaultSpeed" variable
		else {
			isRunning = false;
			movementSpeed = Mathf.Lerp (movementSpeed, defaultSpeed, Time.deltaTime * runBuildUpSpeed);
		}
	}

	// Function that sets the player into a toggled walking state
	private void ToggleWalk(){
		if (Input.GetKeyDown (walkToggleKey) && !isWalking && !isCrouching) {
			isWalking = true;
		} 
		else if (Input.GetKeyDown(walkToggleKey) && isWalking){
			isWalking = false;			
		}
	}

	// Function checking if a player is jumping and then executing a jump
	private void Jump(){
		// When the "jumpKey" is pressed, call the JumpEvent co-routine and set "isJumping" to true
		if (Input.GetKeyDown (jumpKey) && !isJumping && !isCrouching) {
			isJumping = true;
			StartCoroutine ("JumpEvent");
		}
	}

	// Function that checks if the player is crouching or not and changes values accordingly
	private void Crouch(){
		// When the "crouchKey" is held down, put the player into a crouching state by setting
		// the "isCrouching" boolean to true and setting the character controller height to half
		if (Input.GetKey (crouchKey) && !isJumping && !isCrouching) {
			isWalking = false;
			isCrouching = true;
			cc.transform.localPosition = new Vector3 (cc.transform.position.x, cc.transform.position.y - 0.49f, cc.transform.position.z);
			cc.height = cc.height / 2;
		} 
		// When the user lets off the "crouchKey", set the "isCrouching" boolean back to false
		// and double the character controller height, back to it's original value
		else if (Input.GetKeyUp(crouchKey)) {
			isCrouching = false;
			cc.height = cc.height * 2;
		}
	}

	// Boolean function testing if the player is on a slope and return true or false
	private bool OnSlope(){
		// If the player is jumping, they are definetly not on a slope so return false
		if (isJumping) {
			return false;
		}

		// Create a raycast hit variable store raycast information later
		RaycastHit hit;

		// Check if a raycast from the player body shooting to the floor hits a sloped
		// surface or a gameobject tagged as "stairs, then return true
		if (Physics.Raycast (transform.position, Vector3.down, out hit, cc.height / 2 * slopeForceRayLength)) {
			if (hit.normal != Vector3.up || hit.transform.tag == "Stairs") {
				return true;
			}
		}

		// If none of the above conditions are met then return false
		return false;
	}

	// Coroutine that executes a jump on call
	private IEnumerator JumpEvent(){
		// Float to keep track of time spent in air during a jump
		cc.slopeLimit = 90.0f;
		float airTime = 0.0f;

		// While the character controller isn't grounded and aren't colliding with the ceiling, make the player jump based
		// on the "jumpFallOff" curve and jump multiplier, until the player hits the ground.. then set "isJumping" back to false
		do{
			float jumpForce = jumpFallOff.Evaluate(airTime);
			cc.Move(Vector3.up * jumpForce * jumpMultiplier * Time.deltaTime);
			airTime += Time.deltaTime;
			yield return null;
		} while(!cc.isGrounded && cc.collisionFlags != CollisionFlags.Above);

		cc.slopeLimit = 45.0f;
		isJumping = false;
	}

}