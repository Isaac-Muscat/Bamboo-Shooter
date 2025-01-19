using UnityEngine;

public class BambooShoot : MonoBehaviour
{
    public PosStatePair posState;
    public BambooSeed seed;
    public float health = 100;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        posState.pos.x = Mathf.RoundToInt(transform.position.x);
        posState.pos.y = Mathf.RoundToInt(transform.position.z);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Damage(float damage)
    {
        health -= damage;
        if (health < 0)
        {
            Kill();
        }
    }

    void Kill()
    {
        seed.DeleteBamboo(posState);
    }
}
