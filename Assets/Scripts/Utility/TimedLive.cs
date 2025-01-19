using System.Security.Cryptography;
using UnityEngine;

public class TimedLive : MonoBehaviour
{
    public float life = 2;

    // Update is called once per frame
    void FixedUpdate()
    {
        life -= Time.deltaTime;
        if (life < 0) Destroy(gameObject);
    }
}
