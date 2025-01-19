using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Projectile : MonoBehaviour
{
    [Header("Modifiers")]
    public float speed = 10;
    public float drag = 0;
    public int livingFrames = 1;
    public float variance = 0.2f;
    public float damage = 0.5f;
    public GameObject deathPart;
    [Header("State")]
    public Vector2 velocity;
    public bool fired = false;
    public RoomSpawn roomMan;
    public GameObject grassDie;

    public void Fire(Vector2 pos, Vector2 dir)
    {
        fired = true;
        transform.position = new Vector3(pos.x, 0.5f, pos.y);
        velocity = dir * speed;
        Vector2 cross = Vector2.Perpendicular(dir);
        float varianceRand = Random.Range(-variance, variance);
        velocity += cross * (speed * varianceRand);
        GetComponent<Rigidbody>().AddForce(new Vector3(velocity.x, 0, velocity.y), ForceMode.Impulse);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!fired) return;
        if (livingFrames <= 0)
        {
            Instantiate(deathPart, transform.position - new Vector3(velocity.x, 0, velocity.y)*Time.fixedDeltaTime, Quaternion.identity);
            Destroy(gameObject);
            return;
        }
        
        livingFrames--;
        //velocity *= (drag * Time.fixedDeltaTime) + 1;
        //transform.position += new Vector3(velocity.x, 0, velocity.y) * Time.fixedDeltaTime;
        // stepped collision check
        Vector2 startBullet = new Vector2(transform.position.x, transform.position.z) - velocity * Time.fixedDeltaTime;
        Vector2 step = velocity * Time.fixedDeltaTime / 10.0f;
        for (int i = 0; i < 10; i++)
        {
            Vector2 pos = startBullet + step * i;
            if (roomMan.CollideBamboo(pos))
            {
                Instantiate(grassDie, transform.position - new Vector3(velocity.x, 0, velocity.y)*Time.fixedDeltaTime, Quaternion.identity);
                Instantiate(deathPart, transform.position - new Vector3(velocity.x, 0, velocity.y)*Time.fixedDeltaTime, Quaternion.identity);
                Destroy(gameObject);
                break;
            }
        }
        
        // TODO: Raycast
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if it's a bamboo instance
        BambooDamage bamboo = other.GetComponent<BambooDamage>();
        Debris debris = other.GetComponent<Debris>();
        if (bamboo != null)
        {
            bamboo.Damage(damage);
        } else if (debris != null)
        {
            debris.Flip(transform.position, velocity/5, damage);
        }
        else if (!other.CompareTag("NoBullet"))
        {
            Instantiate(deathPart, transform.position - new Vector3(velocity.x, 0, velocity.y)*Time.fixedDeltaTime, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
