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

    public PlayerController player;
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

        if (!flipped)
        {
            if (loot == 1)
            {
                loot = 0;
                if (player.weapon >= player.lootObjects.Length - 1)
                    StartCoroutine(SpawnMoney(25));
                else
                {
                    Loot spLoot = Instantiate(player.lootObjects[player.weapon + 1], transform.position, Quaternion.identity).GetComponent<Loot>();
                    spLoot.Initialize(rb.linearVelocity, player);
                }
            } else if (loot == 2)
            {
                loot = 0;
                if (player.hasKeycard)
                    StartCoroutine(SpawnMoney(50));
                else
                {
                    Loot spLoot = Instantiate(player.keyCard, transform.position, Quaternion.identity).GetComponent<Loot>();
                    spLoot.Initialize(rb.linearVelocity, player);
                }
            }
            else
            {
                StartCoroutine(SpawnMoney(Random.Range(1, 3)));
            }
        }
        
        flipped = true;

        health -= damage;
        if (health < 0)
        {
            Instantiate(deathPart, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }

    IEnumerator SpawnMoney(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Loot spLoot = Instantiate(player.coin, transform.position, Quaternion.identity).GetComponent<Loot>();
            spLoot.Initialize(rb.linearVelocity, player);
            yield return new WaitForFixedUpdate();
        }
    }
}
