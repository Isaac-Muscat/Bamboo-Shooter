using System;
using UnityEngine;

public class BambooDamage : MonoBehaviour
{
    public float health = 400;
    public PlayerController pc;

    private MeshRenderer rend;
    private Color desiredCol;

    private void Start()
    {
        rend = GetComponent<MeshRenderer>();
        desiredCol = rend.material.color;
    }

    private void Update()
    {
        rend.material.color = Color.Lerp(rend.material.color, desiredCol, Time.deltaTime * 10);
    }

    public void Damage(float dmg)
    {
        health -= dmg;
        rend.material.color = Color.red;
        if (health < 0)
            pc.Win();
    }
}
