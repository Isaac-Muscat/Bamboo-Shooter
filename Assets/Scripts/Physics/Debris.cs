using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class Debris : MonoBehaviour
{
    public int loot = 0;
    public bool flipped = false;
    public float stringFlipStrength = 10;
    public float weakFlipStrength = 1;
    public float health = 100;
    public GameObject deathPart;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Flip(Vector2 pos, Vector2 dir, float damage)
    {
        float coef = flipped ? weakFlipStrength : stringFlipStrength;
        rb.AddForceAtPosition(
            new Vector3(dir.x, Random.Range(1, 3), dir.y) * coef,
            new Vector3(pos.x, 0f, pos.y), ForceMode.Impulse);
        
        flipped = true;

        health -= damage;
        if (health < 0)
        {
            Instantiate(deathPart, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
