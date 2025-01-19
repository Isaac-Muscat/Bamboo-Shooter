using System;
using UnityEngine;

public class Loot : MonoBehaviour
{
    public int lootID = 0;
    private Transform vis;

    private bool init = false;
    private float anim = 0;

    private Vector2 velocity;
    private float drag = 1f;

    public PlayerController pc;
    
    public void Initialize(Vector3 vel, PlayerController player)
    {
        pc = player;
        velocity = new Vector2(vel.x, vel.z);

        vis = transform.GetChild(0);
        init = true;
    }

    private void Update()
    {
        if (!init) return;

        anim += Time.deltaTime;
        vis.localPosition = new Vector3(0, Mathf.Sin(anim) / 2f + 0.6f, 0);
        vis.Rotate(Vector3.up, anim/10f);
    }

    private void FixedUpdate()
    {
        if (lootID < 0)
        {
            Vector2 pos = new Vector2(transform.position.x, transform.position.z);
            velocity -= (pos - pc.position).normalized * (Time.fixedDeltaTime * 10);
        }
        transform.position += new Vector3(velocity.x, 0, velocity.y) * Time.fixedDeltaTime;
        transform.position = new Vector3(transform.position.x, 0, transform.position.z);
        velocity /= 1 + (drag * Time.fixedDeltaTime);
        
        // TODO: check collisions
    }
}
