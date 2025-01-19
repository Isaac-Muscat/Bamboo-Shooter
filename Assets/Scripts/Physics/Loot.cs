using System;
using UnityEngine;

public class Loot : MonoBehaviour
{
    public int lootID = 0;
    private Transform vis;

    private bool init = false;
    private float anim = 0;
    
    public void Initialize(Vector3 velocity)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.AddForce(new Vector3(velocity.x, 0, velocity.z), ForceMode.Impulse);

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
}
