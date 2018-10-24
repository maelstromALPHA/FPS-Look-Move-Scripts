using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// KB/M based customisable FPS camera script, written by Martin Howard (@maelstromALPHA)
// Attach this script to a camera object that is a child of your player object to work

public class PlayerLook : MonoBehaviour {

	// Boolean that allows you to save your custom values from changes in the inspector
	[SerializeField] private bool useCustomValues;

	// Serialized private & customisable variables, with values set in the inspector
	[SerializeField] private string horizontalInputName, verticalInputName;
	[SerializeField] private string mouseXInput, mouseYInput;
	[SerializeField][Range(0,1)] private float cameraBobSpeed, cameraBobIntensity;
	[SerializeField] private Transform playerBody;

	// Values to be set via menu selection
	[Range(50,250)]public float mouseSensitivity;
	public bool bobbing;
	public bool invertY;

	// Private variables
	private CharacterController cc;
	private PlayerMove pm;
	private float xAxisClamp, midPoint, timer, waveslice, totalAxes, translateChange, adjustedIntensity, adjustedSpeed, horizontalInput, verticalInput;

	private void Awake(){
		cc = GetComponentInParent<CharacterController> ();
		pm = GetComponentInParent<PlayerMove> ();
		xAxisClamp = 0.0f;
		midPoint = cc.transform.position.y - 0.1f;
		LockCursor ();

		// If "use custom values" is not ticked in the inspector, set default values on awake
		if (!useCustomValues){	
			horizontalInputName = "Horizontal";
			verticalInputName = "Vertical";
			mouseXInput = "Mouse X";
			mouseYInput = "Mouse Y";
			mouseSensitivity = 100;
			bobbing = true;
			invertY = false;
			cameraBobSpeed = 0.125f;
			cameraBobIntensity = 0.125f;
			playerBody = GameObject.Find("Player").transform;
		}
	}

	// Update is called once per frame
	void Update () {
		CameraRotation ();

		// Only perform head bobbing if it has been set to on
		if (bobbing) {
			HeadBob ();
		}
	}

	// Lock the cursor to the center of the screen
	private void LockCursor(){		
		Cursor.lockState = CursorLockMode.Locked;
	}

	// Function that takes mouse input on X/Y axis and translates that into camera movement
	private void CameraRotation(){
		// Get the value of the mouse X and Y axis multiplied by the mouse sensitivity
		float mouseX = Input.GetAxis (mouseXInput) * mouseSensitivity * Time.deltaTime;
		float mouseY = Input.GetAxis (mouseYInput) * mouseSensitivity * Time.deltaTime;

        // Keep track of how much rotation is being applied to the camera and clamp it to a 75/75 degree range up/down,
        // making sure the view never loops around and the user can't look directly up or down for a more natural feeling.
        // Also, reverse the xAxisClamp value if the mouse settings are set to have an inverted Y-Axis
        if (invertY) {
            xAxisClamp += -mouseY;
        }
        else {
            xAxisClamp += mouseY;
        }		

		if (xAxisClamp > 75.0f) {
			xAxisClamp = 75.0f;
			mouseY = 0.0f;
			ClampXAxisRotationToValue (285.0f);
		}
		else if (xAxisClamp < -75.0f) {
			xAxisClamp = -75.0f;
			mouseY = 0.0f;
			ClampXAxisRotationToValue (75.0f);
		}

        // Rotate the camera and player object based on mouse input, also Check if the
        // player has inverted Y-Axis and then apply a negative to the vertical mouse input
        playerBody.Rotate(Vector3.up * mouseX);

        if (invertY) {
            transform.Rotate(-Vector3.left * mouseY);
        }
        else {
            transform.Rotate(Vector3.left * mouseY);
        }
    }

	// Function that clamps the camera X axis rotation to a specified value
	private void ClampXAxisRotationToValue(float value){
		Vector3 eulerRotation = transform.eulerAngles;
		eulerRotation.x = value;
		transform.eulerAngles = eulerRotation;
	}

	// Function that adds adjustable camera/head bobbing to the FPS camera script
	private void HeadBob(){
		// Capture the user input on the horizontal and vertical axis
		horizontalInput = Input.GetAxis (horizontalInputName);
		verticalInput = Input.GetAxis (verticalInputName);

		// Check the player state and adjust the camera bob intensity/speed accordingly, storing it for use later
		if (pm.isCrouching || pm.isWalking) {
			adjustedIntensity = cameraBobIntensity * 0.75f;
			adjustedSpeed = cameraBobSpeed * 0.75f;
		} 
		else if (pm.isRunning) {
			adjustedIntensity = cameraBobIntensity * 1.5f;
			adjustedSpeed = cameraBobSpeed * 1.5f;
		} 
		else if (pm.isJumping) {
			adjustedIntensity = 0;
			adjustedSpeed = 0;
		} 
		else {
			adjustedIntensity = cameraBobIntensity;
			adjustedSpeed = cameraBobSpeed;
		}
			
		// Make sure that there is no headbob effect if there is no horizontal/vertical input
		if (Mathf.Abs (horizontalInput) == 0 && Mathf.Abs (verticalInput) == 0) {
			timer = 0;
		}
		// Determine where the camera position should be during the "heads bounce" by using a sine wave
		else {
			waveslice = Mathf.Sin (timer);
			timer = timer + adjustedSpeed;

			if (timer > Mathf.PI * 2) {
				timer = timer - (Mathf.PI * 2);
			}
		}

		// When the current point in the sine wave is not zero, move the camera position to simulate the head bob effect
		if (waveslice != 0) {			
			translateChange = waveslice * adjustedIntensity;
			totalAxes = Mathf.Abs (horizontalInput) + Mathf.Abs (verticalInput);
			totalAxes = Mathf.Clamp (totalAxes, 0, 1);
			translateChange = totalAxes * translateChange;
			transform.localPosition =  new Vector3 (transform.localPosition.x, midPoint + translateChange, transform.localPosition.z);
		}
		// If the current point in the sine wave is zero, then set it back to the "midPoint"
		// which was the camera's original position before the head bob effect starts
		else {
			transform.localPosition =  new Vector3 (transform.localPosition.x, midPoint, transform.localPosition.z);
		}			
	}

}