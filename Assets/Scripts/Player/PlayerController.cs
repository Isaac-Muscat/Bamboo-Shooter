using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [Header("Global References")]
    public RoomSpawn roomManager;
    public BambooDamage bambooDamage;

    [Header("Player References")]
    public Transform playerBody;
    public Transform cameraAssembly;
    public Camera mainCam;
    public Transform crosshair;
    public MeshFilter playerMesh;

    [Header("Attacking")]
    public int weapon = 0;
    public GameObject[] weaponAttackPrefabs;
    public float[] weaponDelays;
    private float currentWeaponDelay = 0;
    public GameObject[] lootObjects;
    public bool hasKeycard = false;
    public int numCoins;
    public GameObject keyCard;
    public GameObject coin;

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
    private Vector2 lastLookDir = Vector2.up;
    private Vector2 lastPointInput = Vector2.zero;
    private Vector2 lastMouseInput = Vector2.zero;
    private bool firing = false;

    [Header("Audio")] 
    public AudioSource source1;
    public AudioSource source2;
    public AudioSource keycardSFX;

    void Start()
    {
        roomManager.frameInterval = PlayerPrefs.GetInt("GrowthFactor");
        // Init the level
        roomManager.GenerateRooms(this);
        roomManager.GenerateBamboo();
        bambooDamage.pc = this;
        //return;
        position = roomManager.seeds[roomManager.spawnSeed].pos;
        playerBody.position = new Vector3(position.x, 0, position.y);
        cameraAssembly.position = playerBody.position;

        //mainCam = Camera.main;
        source1.Play();
        source2.Play();
        source1.volume = 1;
        source2.volume = 0;
    }
    
    // PHYSICS
    private void FixedUpdate()
    {
        //return;
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
        if (velocity.magnitude > 0.1)
            distanceSinceLastStep += Vector2.Distance(prevPos, position);
        else if (stepState)
        {
            stepState = false;
            playerMesh.mesh = playerAnimateMeshes[0];
        }

        if (distanceSinceLastStep > distanceBetweenSteps)
        {
            distanceSinceLastStep = 0;
            playerMesh.mesh = stepState ? playerAnimateMeshes[1] : playerAnimateMeshes[0];
            playerMesh.transform.localPosition = stepState ? new Vector3(0, 0.1f, 0.1f) : new Vector3(0, 0, 0);
            playerMesh.transform.localScale = stepSide ? new Vector3(1, 1, 1) : new Vector3(-1, 1, 1);
            
            if (stepState) stepSide = !stepSide;
            stepState = !stepState;
            
        }
        
        playerBody.position = new Vector3(position.x, 0, position.y);
        // LERP
        cameraAssembly.position = Vector3.Lerp(cameraAssembly.position, playerBody.position, Time.fixedDeltaTime*4);
        
        // UPDATE THE LOOK DIR / CORSSHAIR
        Vector3 crosshairPos = mainCam.ViewportToWorldPoint(new Vector3(lastPointInput.x * 0.2f + 0.5f, lastPointInput.y * 0.2f + 0.5f, 4));
        crosshair.position = crosshairPos;

        if (lastPointInput == Vector2.zero)
        {
            if (lastInput != Vector2.zero) lastLookDir = lastInput.normalized;
        }
        else lastLookDir = lastPointInput;
        playerMesh.transform.LookAt(playerMesh.transform.position - new Vector3(lastLookDir.x, 0, lastLookDir.y), Vector3.up);
        
        // COMPUTE SHOOTING
        currentWeaponDelay -= Time.fixedDeltaTime;
        if (firing && currentWeaponDelay < 0)
        {
            currentWeaponDelay = weaponDelays[weapon];
            Projectile fired = Instantiate(weaponAttackPrefabs[weapon], transform).GetComponent<Projectile>();
            fired.roomMan = roomManager;
            fired.Fire(position + lastLookDir*0.5f, lastLookDir);
        }
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
        if (roomManager.GetTile_ROOM(roundedPosTR.x, roundedPosTR.y) % 2 == 0)
        {
            // COLLISION ON TR
            collisionCode |= 0b0001;
        }
        if (roomManager.GetTile_ROOM(roundedPosBL.x, roundedPosTR.y) % 2 == 0)
        {
            // COLLISION ON TL
            collisionCode |= 0b0010;
        }
        if (roomManager.GetTile_ROOM(roundedPosTR.x, roundedPosBL.y) % 2 == 0)
        {
            // COLLISION ON BR
            collisionCode |= 0b0100;
        }
        if (roomManager.GetTile_ROOM(roundedPosBL.x, roundedPosBL.y) % 2 == 0)
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
    
    // UNITY EVENT FUNCS

    public void DebrisCollision(GameObject collider, Vector2 collisionPoint)
    {
        Debris debris = collider.GetComponent<Debris>();
        Loot loot = collider.GetComponent<Loot>();
        if (debris != null)
        {
            Vector2 collisionDir = collisionPoint - position;
            debris.Flip(collisionPoint, collisionDir + velocity, 25);
            // move the player out of the collision
            float dotFac = Vector2.Dot(velocity, collisionDir);
            velocity -= collisionDir * dotFac * 5;
            position -= collisionDir * dotFac * Time.fixedDeltaTime;
        } else if (loot != null)
        {
            if (loot.lootID == -1) numCoins++;
            if (loot.lootID == -2 && !hasKeycard)
            {
                hasKeycard = true;
                GetKey();
            }
            Destroy(collider);
        }
    }
    
    // INPUT HANDLING

    public void Move(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            lastInput = context.ReadValue<Vector2>();
            if (lastInput.magnitude < 0.1)
                lastInput = Vector2.zero;
        } else if (context.canceled)
        {
            lastInput = Vector2.zero;
        }
    }
    
    public void Look(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            lastPointInput = context.ReadValue<Vector2>().normalized;
        } else if (context.canceled)
        {
            lastPointInput = Vector2.zero;
        }
    }
    
    public void Shoot(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            firing = true;
        } else if (context.canceled)
        {
            firing = false;
        }
    }

    public void GetKey()
    {
        keycardSFX.Play();
        Time.timeScale = 0;
        StartCoroutine(Pan());
        //TODO: Music
    }

    IEnumerator Pan()
    {
        Vector3 delta = (new Vector3(20, 0, 20) - cameraAssembly.position) / 100.0f;
        Vector3 initialPos = cameraAssembly.position;
        for (int i = 0; i < 100; i++)
        {
            cameraAssembly.transform.position = initialPos + delta*i;
            float fac = i / 100.0f;
            source1.volume = 1 - fac;
            source2.volume = fac;
            yield return new WaitForEndOfFrame();
        }

        source1.volume = 0;
        source2.volume = 1;
        StartCoroutine(roomManager.LowerWalls());
    }
    
    public void Win()
    {
        int growthFactor = PlayerPrefs.GetInt("GrowthFactor");
        if (growthFactor <= 1)
        {
            SceneManager.LoadScene("MenuScene");
        }
        PlayerPrefs.SetInt("GrowthFactor", growthFactor - 1);
        SceneManager.LoadScene("MainScene");
    }
}
