using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class _playerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 4.3f;
    public float gravity = -19f;
    public float jumpHeight = 1.25f;
    public float airControl = 3f; // how hard to change direction mid air( smalleer -> harder)

    [Header("Camera Settings")]
    public Transform playerCamera;
    public float mouseSensitivity = 2f;
    private float cameraPitch = 0f; //to rotate on x axis

    private CharacterController _controller;

    private Vector3 velocity;
    private Vector3 currentMoveVelocity;

    private void Start() {
        _controller = GetComponent<CharacterController>();

        //lock cursor in middle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update() {
        HandleMouseLook();
        HandleMovement();
    }

    //get mouse input, rotate pitch/yaw
    private void HandleMouseLook() {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        //rotate camera on X axis (up and down for camera)
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f); // to not go around our head

        playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

        //rotate left right player body - y axis
        transform.Rotate(Vector3.up * mouseX);
    }
    
    //get axis movements/space and move player controller
    private void HandleMovement() {
        //get raw input
        float x = Input.GetAxisRaw("Horizontal"); //A,D
        float z = Input.GetAxisRaw("Vertical"); //W,S
        Vector3 targetMove = (transform.right * x + transform.forward * z).normalized * walkSpeed;

        if (_controller.isGrounded) { // on ground keep raw speed
            currentMoveVelocity = targetMove;
        } else { // on air lerp to have some inertia in changing direction
            currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetMove, airControl * Time.deltaTime);
        }

        //apply horizontal movement
        _controller.Move(currentMoveVelocity * Time.deltaTime);

        //ground check
        if(_controller.isGrounded && velocity.y < 0) {
            velocity.y = -2f; //pull down to be connected 100% to the ground
        }

        //jump
        if(Input.GetButtonDown("Jump") && _controller.isGrounded) {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        //apply gravity
        velocity.y += gravity * Time.deltaTime;

        //move controller on vertical
        _controller.Move(velocity * Time.deltaTime);
    }
}
