using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class _playerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 4.3f;
    public float gravity = -19f;
    public float jumpHeight = 1.25f;
    public float airControl = 3f; // how hard to change direction mid air( smalleer -> harder)

    [Header("Sprint & Sneak")]
    public float sprintSpeed = 6.5f;
    public float sneakspeed = 1.5f;
    public float sprintJumpBoost = 1.5f;

    [Header("Camera Settings")]
    public Transform playerCamera;
    private Camera _cameraComponent;
    public float mouseSensitivity = 2f;
    private float cameraPitch = 0f; //to rotate on x axis

    public float normalCameraY;
    public float sneakCameraY;
    public float cameraTransitionSpeed = 10f;

    public float normalFOV;
    //public float sprintFOV = 85; //not used anymore, add 15 to normal fov
    public float fovTransitionSpeed = 10f;

    [Header("Interaction Setttings")]
    public float reach = 4f;
    public Transform highlightBlock; // selected block highlighter
    public float interactionDelay = 0.15f; //delay when holding click to break/place
    private float interactionTimer = 0f;

    [Header("Liquids")]
    public float swimSpeedMultiplier = 0.5f;
    public float swimUpSpeed = 3f;
    public float waterGravityMultiplier = 0.2f;
    public float waterTerminalVelocity = -2.5f; // maximum sinking speed
    public float waterExitJumpForce = 6.5f;

    public bool inLiquid = false; //trigger swimming
    public bool inLava = false; //kill the player

    private _worldManager _world;
    private CharacterController _controller;
    private _gameManager _manager;
    private _playerInventory _inventoryScript;
    private _viewmodelController _viewModel;

    private Vector3 velocity;
    private Vector3 currentMoveVelocity;

    private bool isSneaking;
    private bool isSprinting;
    private float lastWPressTime = 0f;
    private float doubleTapThreshold = 0.3f;

    private void Start() {
        _controller = GetComponent<CharacterController>();
        _world = FindObjectOfType<_worldManager>();
        _manager = FindObjectOfType<_gameManager>();
        _inventoryScript = GetComponent<_playerInventory>();

        if(_inventoryScript != null) {
            _viewModel = _inventoryScript._viewmodelController; //grab viewmodel controller from inventory
        }

        //lock cursor in middle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        //save Y camera values
        if (playerCamera != null) {
            normalCameraY = playerCamera.localPosition.y;
            sneakCameraY = normalCameraY - 0.4f;

            //get component
            _cameraComponent = playerCamera.GetComponent<Camera>();
            if (_cameraComponent != null) {
                //normalFOV = _cameraComponent.fieldOfView; no longer overwrite it
            }
        } else { Debug.Log("player camera not assigned!"); }
    }

    private void Update() {
        //if game is paused, player should not update anything
        if (_manager != null) {
            if (_manager.isPaused) {
                return;
            }
        }

        //check state if you are in liquids or not
        CheckLiquids();

        //not paused, update everything
        HandleMouseLook();
        HandleMovement();
        HandleInteraction();

        //kill player if touching lava
        if (inLava) {
            if (_manager != null) _manager.GameOver();
        }
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

        isSneaking = Input.GetKey(KeyCode.LeftShift);

        //sprinting
        if (Input.GetKeyDown(KeyCode.W)) {
            if(Time.time - lastWPressTime < doubleTapThreshold) {
                isSprinting = true;
            }
            lastWPressTime = Time.time;
        }

        //or by moving with CTRL holding
        if(Input.GetKeyDown(KeyCode.LeftControl) && z > 0) {
            isSprinting = true;
        }

        if(z <= 0 || isSneaking) {
            isSprinting = false;
        }

        //determine curernt speed
        float currentTargetSpeed = walkSpeed;
        if (inLiquid) currentTargetSpeed *= swimSpeedMultiplier;// SWIMMING
        else {
            //dont let sneak/sprint change speed in water
            if (isSneaking) currentTargetSpeed = sneakspeed;
            if (isSprinting) currentTargetSpeed = sprintSpeed;
        }
        
        

        //canera nivenebt for sneak
        float targetCamY = isSneaking ? sneakCameraY : normalCameraY;
        Vector3 camPos = playerCamera.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, targetCamY, cameraTransitionSpeed * Time.deltaTime);
        playerCamera.localPosition = camPos;

        //fov change for sprint
        if(_cameraComponent != null) {
            float targetFOV = isSprinting ? (normalFOV + 15) : normalFOV;
            _cameraComponent.fieldOfView = Mathf.Lerp(_cameraComponent.fieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime); // lerp through fovs
        }

        //actual movement
        Vector3 targetMove = (transform.right * x + transform.forward * z).normalized * currentTargetSpeed;

        if (_controller.isGrounded && !inLiquid) { // on ground keep raw speed
            currentMoveVelocity = targetMove;
        } else { // on air / water lerp to have some inertia in changing direction
            currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetMove, airControl * Time.deltaTime);
        }

        if (inLiquid) {
            //SWIM
            if (Input.GetButton("Jump")){
                //check if player is at surface
                Vector3 headPos = transform.position + new Vector3(0, 1.5f, 0);
                Vector3Int headVoxelPos = new Vector3Int(Mathf.FloorToInt(headPos.x), Mathf.FloorToInt(headPos.y), Mathf.FloorToInt(headPos.z) );

                byte headBlock = _world.GetVoxelGlobal(headVoxelPos);

                //only boost player when next to ledge
                if ((headBlock == (byte)BlockType.Air || VoxelData.IsCrossModel(headBlock) ) && IsNearSolidBlock()) {
                    velocity.y = waterExitJumpForce; //get a boost to climb back to surface
                } else {

                    //swim regularly
                    velocity.y = Mathf.Lerp(velocity.y, swimUpSpeed, 2f * Time.deltaTime);
                }
            } else {
                velocity.y += (gravity * waterGravityMultiplier) * Time.deltaTime; //slowly sink
            }

            //velocity dampening
            velocity.y = Mathf.Lerp(velocity.y, 0f, 4f * Time.deltaTime);

            if (velocity.y < waterTerminalVelocity) {
                velocity.y = waterTerminalVelocity; //don't sink too fast
            }
        } else {
            //not in liquid

            //ground check
            if (_controller.isGrounded && velocity.y < 0) {
                velocity.y = -2f; //pull down to be connected 100% to the ground
            }

            //jump
            if (Input.GetButton("Jump") && _controller.isGrounded) {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

                //increase speed if sprinting
                if (isSprinting) {
                    currentMoveVelocity += transform.forward * sprintJumpBoost;
                }
            }

            //apply gravity
            velocity.y += gravity * Time.deltaTime;
        }

        //create vector with both horizontal and vertical movement

        Vector3 finalMove = new Vector3(currentMoveVelocity.x, velocity.y, currentMoveVelocity.z);

        //sneak edge detection
        if(isSneaking && _controller.isGrounded && !inLiquid) {
            //check next frame position
            Vector3 currentPos = transform.position;

            Vector3 predictedX = currentPos + new Vector3(finalMove.x * Time.deltaTime, 0, 0);
            if (!IsSafeToStep(predictedX)) {
                finalMove.x = 0f;
                currentMoveVelocity.x = 0f;
            }

            Vector3 predictedZ = currentPos + new Vector3(0, 0, finalMove.z * Time.deltaTime);
            if (!IsSafeToStep(predictedZ)) {
                finalMove.z = 0f;
                currentMoveVelocity.z = 0f;
            }

            //also check edge
            if(finalMove.x != 0 && finalMove.z != 0) {
                Vector3 predictedDiag = currentPos + new Vector3(finalMove.x * Time.deltaTime, 0, finalMove.z * Time.deltaTime);

                if (!IsSafeToStep(predictedDiag)) { //stop movement
                    finalMove.x = 0f;
                    finalMove.z = 0f;
                    currentMoveVelocity.x = 0f;
                    currentMoveVelocity.z = 0f;
                }
            }

        }

        //move controller, we are multiplying with time so it's not dependant on FPS, but rather on time
        _controller.Move(finalMove * Time.deltaTime);
    }

    //helper function to raycast 
    private bool IsSafeToStep(Vector3 pos) {
        float y = _controller.bounds.min.y + 0.1f;
        return Physics.Raycast(new Vector3(pos.x, pos.y, pos.z), Vector3.down, 0.3f);
    }

    //check if near a solid block (used in water to get out of it to help player climb the ledge
    private bool IsNearSolidBlock() {
        int x = Mathf.FloorToInt(transform.position.x);
        int y = Mathf.FloorToInt(transform.position.y ); //where player would want to step
        int z = Mathf.FloorToInt(transform.position.z);

        //define coords of neightbouring blocks
        Vector3Int[] neighbors = new Vector3Int[] {
            new Vector3Int (x+1, y, z),
            new Vector3Int (x-1, y, z),
            new Vector3Int (x, y, z+1),
            new Vector3Int (x, y, z-1)
        };

        foreach(Vector3Int checkPos in neighbors) {
            byte blockID = _world.GetVoxelGlobal(checkPos);

            if (VoxelData.HasCollision(blockID))
                return true; //next to ledge
        }
        return false;//we are in middle of water
    }

    //check if we are inside liquid
    private void CheckLiquids() {
        //pivot of player is at feet
        Vector3 checkPos = transform.position + new Vector3(0, 0.8f, 0);
        Vector3Int voxelPos = new Vector3Int(Mathf.FloorToInt(checkPos.x), Mathf.FloorToInt(checkPos.y), Mathf.FloorToInt(checkPos.z));

        byte currentBlock = _world.GetVoxelGlobal(voxelPos);

        //check what block id it is
        if(currentBlock == (byte)BlockType.Water || currentBlock == (byte)BlockType.Lava) {
            inLiquid = true;
        } else {
            inLiquid = false;
        }

        if(currentBlock == (byte)BlockType.Lava) {
            inLava = true;
        } else {
            inLava = false;
        }
    }

    private void HandleInteraction() {
        //update interaction timer global
        if (interactionTimer > 0) {
            interactionTimer -= Time.deltaTime;
        }

        //raycast
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        bool isLookingAtBlock = Physics.Raycast(ray, out hit, reach); // if ray hit something

        Vector3Int breakCoord = Vector3Int.zero;
        Vector3Int placeCoord = Vector3Int.zero;

        //outline and calculate coordinates logic
        if (isLookingAtBlock) {
            //we are right on block edge
            //if we go a little bit in direction of ray direction we are inside the block we can break
            //and if we go a little bit in the opposite direction of ray direction we are outside where we need to place
            //and rounding to int we get the exact block coordinates we need to operate on

            Vector3 pointInSolidBlock = hit.point + (ray.direction * 0.01f);
            breakCoord = new Vector3Int(Mathf.FloorToInt(pointInSolidBlock.x), Mathf.FloorToInt(pointInSolidBlock.y), Mathf.FloorToInt(pointInSolidBlock.z));

            Vector3 pointInEmptyAir = hit.point - (ray.direction * 0.01f);
            placeCoord = new Vector3Int(Mathf.FloorToInt(pointInEmptyAir.x), Mathf.FloorToInt(pointInEmptyAir.y), Mathf.FloorToInt(pointInEmptyAir.z));

            //outline
            if (highlightBlock != null) {
                highlightBlock.gameObject.SetActive(true);
                highlightBlock.position = new Vector3(breakCoord.x + 0.5f, breakCoord.y + 0.5f, breakCoord.z + 0.5f);
            }

        } else {
            //no raycasthit , hide block outline
            if (highlightBlock != null) {
                highlightBlock.gameObject.SetActive(false);
            }
        }

        //left click swing / place block logic
        bool isFirstClick = Input.GetMouseButtonDown(0);
        bool isHoldingClick = Input.GetMouseButton(0) && interactionTimer <= 0f;
        if (isFirstClick || (isHoldingClick && isLookingAtBlock)) {
            //swing animation
            if (_viewModel != null) {
                _viewModel.TriggerSwingAnimation(false);
            }
            interactionTimer = interactionDelay; //reset timer

            //check to break block

            if (isLookingAtBlock) {
                //only break if it is not bedrock or air
                if (_world.GetVoxelGlobal(breakCoord) != (byte)BlockType.Bedrock && _world.GetVoxelGlobal(breakCoord) != (byte)BlockType.Air) {
                    byte blockToBreak = _world.GetVoxelGlobal(breakCoord);
                    _world.SetVoxelGlobal(breakCoord, (byte)BlockType.Air); // we replace block with air
                    _world.SpawnBlockParticles(breakCoord, blockToBreak); //spawn destruction particles

                    //check what block it was if it gives score
                    if (VoxelData.ScoreValue(blockToBreak) > 0) {
                        if(_manager != null) {
                            _manager.IncreaseScore(VoxelData.ScoreValue(blockToBreak)); //updatea player score
                        }
                    }

                    //if it's mushroom add time
                    if(blockToBreak == (byte)BlockType.BrownMushroom) {
                        if(_manager != null) {
                            _manager.AddTime(false);
                        }
                    }
                    if (blockToBreak == (byte)BlockType.RedMushroom) {
                        if (_manager != null) {
                            _manager.AddTime(true);
                        }
                    }

                    //check if foilage above break it also
                    Vector3Int blockAboveCoord = new Vector3Int(breakCoord.x, breakCoord.y + 1, breakCoord.z);

                    byte blockAboveID = _world.GetVoxelGlobal(blockAboveCoord);

                    //if its cross model break it
                    if (VoxelData.IsCrossModel(blockAboveID)) {
                        _world.SetVoxelGlobal(blockAboveCoord, (byte)BlockType.Air);
                        _world.SpawnBlockParticles(blockAboveCoord, blockAboveID); //spawn destruction particles

                        //if it's mushroom add time
                        if (blockAboveID == (byte)BlockType.BrownMushroom) {
                            if (_manager != null) {
                                _manager.AddTime(false);
                            }
                        }
                        if (blockAboveID == (byte)BlockType.RedMushroom) {
                            if (_manager != null) {
                                _manager.AddTime(true);
                            }
                        }
                    }
                }
            }
        }

        //place block + animation logic
        if (Input.GetMouseButtonDown(1) || (Input.GetMouseButton(1) && interactionTimer <= 0f)) {

            if (isLookingAtBlock) {
                //get selected block
                byte selectedBlock = (byte)BlockType.Air;
                if(_inventoryScript != null) {
                    selectedBlock = _inventoryScript.hotbar[_inventoryScript.currentSlotIndex];
                }

                if (selectedBlock != (byte)BlockType.Air) {
                    //logic to prevent placing block inside player using BOUNDS
                    //each object has BOUNDS = "collision box" enclosing itself, we will create bounds for a block and check if it overlaps with character controller's bounds box

                    Vector3 blockCenter = new Vector3(placeCoord.x + 0.5f, placeCoord.y + 0.5f, placeCoord.z + 0.5f);
                    Bounds blockBonds = new Bounds(blockCenter, Vector3.one);
                    Bounds playerBounds = _controller.bounds;

                    //check if they overlap
                    if (!playerBounds.Intersects(blockBonds)) {
                        //place block if they dont

                        if (selectedBlock == (byte)BlockType.Torch) {
                            if (hit.normal.y == -1) return; // cant place torces on roof
                        }

                        _world.SetVoxelGlobal(placeCoord, selectedBlock); // place block

                        //swing animation
                        if (_viewModel != null) {
                            _viewModel.TriggerSwingAnimation(true);
                        }

                        interactionTimer = interactionDelay; // init the timer for hold to place

                    } else {
                        Debug.Log("overlap player");
                    }
                }
            }
        }
    }
}
