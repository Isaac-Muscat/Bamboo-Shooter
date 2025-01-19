using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Debris : MonoBehaviour
{
    public int loot = 0;
    public bool flipped = false;
    public bool flippable = true;
    public float flipTimeout = 1;
    private float currentFlipTimeoutTime = 0;
    public float stringFlipStrength = 10;
    public float weakFlipStrength = 1;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Flip(Vector2 pos, Vector2 dir)
    {
        if (!flippable) return;
        
        float coef = flipped ? weakFlipStrength : stringFlipStrength;
        rb.AddForceAtPosition(
            new Vector3(dir.x, Random.Range(1, 3), dir.y) * coef,
            new Vector3(pos.x, 0f, pos.y), ForceMode.Impulse);
        
        flipped = true;
        flippable = false;
        currentFlipTimeoutTime = flipTimeout;
    }

    private void FixedUpdate()
    {
        currentFlipTimeoutTime -= Time.fixedDeltaTime;
        if (currentFlipTimeoutTime <= 0) flippable = true;
    }
}
