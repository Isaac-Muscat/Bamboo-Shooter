using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Global References")]
    public RoomSpawn roomManager;

    [Header("Player References")]
    public Transform playerBody;
    public Transform cameraAssembly;
    public MeshFilter playerMesh;
    

    [Header("Physics")]
    public float playerRadius = 0.4f;
    public float drag = 10f;
    public float accelMultiplier = 10;
    public Vector2 position;
    public Vector2 velocity;
    
    [Header("Visuals")]
    public Mesh[] playerAnimateMeshes;
    public float distanceBetweenSteps = 0.4f;
    public float distanceSinceLastStep = 0;
    private bool stepState = false;
    private bool stepSide = false;
    
    [Header("Input")]
    private Vector2 lastInput = Vector2.zero;

    void Start()
    {
        // Init the level
        roomManager.GenerateRooms();
        position = roomManager.seeds[roomManager.spawnSeed].pos;
        playerBody.position = new Vector3(position.x, 0, position.y);
        cameraAssembly.position = playerBody.position;
    }
    
    // PHYSICS
    private void FixedUpdate()
    {
        // update accel based on input
        Vector2 accelVec = lastInput * (accelMultiplier * Time.fixedDeltaTime);
        Vector2 dragVec = -velocity * (drag * Time.fixedDeltaTime);

        Vector2 prevPos = position;
        
        velocity += accelVec + dragVec;
        
        // Check the walls
        for (int i = 0; i < 4; i++)
        {
            Vector2 qStep = (velocity / 4.0f) * Time.fixedDeltaTime;
            position += qStep;
            CollisionCheck();
        }

        // Animate the player
        if (lastInput != Vector2.zero)
            distanceSinceLastStep += Vector2.Distance(prevPos, position);
        else
            stepState = false;
            
        if (distanceSinceLastStep > distanceBetweenSteps)
        {
            distanceSinceLastStep = 0;
            playerMesh.mesh = stepState ? playerAnimateMeshes[1] : playerAnimateMeshes[0];
            playerMesh.transform.localScale = stepSide ? new Vector3(1, 1, 1) : new Vector3(-1, 1, 1);
            
            if (stepState) stepSide = !stepSide;
            stepState = !stepState;
            
        }
        
        playerBody.position = new Vector3(position.x, 0, position.y);
        cameraAssembly.position = playerBody.position;
    }

    private void CollisionCheck()
    {
        Vector2Int roundedPosTR = new Vector2Int(
            Mathf.RoundToInt(position.x + playerRadius), 
            Mathf.RoundToInt(position.y + playerRadius));
        Vector2Int roundedPosBL = new Vector2Int(
            Mathf.RoundToInt(position.x - playerRadius), 
            Mathf.RoundToInt(position.y - playerRadius));

        // GENERATE A COLLISION CODE
        int collisionCode = 0;
        if (roomManager.GetTile(roundedPosTR.x, roundedPosTR.y) == 2)
        {
            // COLLISION ON TR
            collisionCode |= 0b0001;
        }
        if (roomManager.GetTile(roundedPosBL.x, roundedPosTR.y) == 2)
        {
            // COLLISION ON TL
            collisionCode |= 0b0010;
        }
        if (roomManager.GetTile(roundedPosTR.x, roundedPosBL.y) == 2)
        {
            // COLLISION ON BR
            collisionCode |= 0b0100;
        }
        if (roomManager.GetTile(roundedPosBL.x, roundedPosBL.y) == 2)
        {
            // COLLISION ON BL
            collisionCode |= 0b1000;
        }
        
        // PARSE THE CODE
        if ((collisionCode & 0b0001) > 0 && (collisionCode & 0b0010) > 0)
        {
            // top collision
            velocity.y = 0;
            position.y = roundedPosTR.y - 0.5f - playerRadius;
        }
        
        if ((collisionCode & 0b0100) > 0 && (collisionCode & 0b1000) > 0)
        {
            // bottom collision
            velocity.y = 0;
            position.y = roundedPosBL.y + 0.5f + playerRadius;
        }
        if ((collisionCode & 0b0001) > 0 && (collisionCode & 0b0100) > 0)
        {
            // right collision
            velocity.x = 0;
            position.x = roundedPosTR.x - 0.5f - playerRadius;
        }
        
        if ((collisionCode & 0b0010) > 0 && (collisionCode & 0b1000) > 0)
        {
            // left collision
            velocity.x = 0;
            position.x = roundedPosBL.x + 0.5f + playerRadius;
        }
    }
    
    // INPUT HANDLING

    public void Move(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            lastInput = context.ReadValue<Vector2>();
        } else if (context.canceled)
        {
            lastInput = Vector2.zero;
        }
    }
}
