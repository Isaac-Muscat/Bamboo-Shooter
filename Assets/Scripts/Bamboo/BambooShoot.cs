using UnityEngine;

public class BambooShoot : MonoBehaviour
{
    public GameObject explodeParticleSystemPrefab;
    public PosStatePair posState;
    public BambooSeed seed;
    public BambooShoot child = null;
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
        Instantiate(explodeParticleSystemPrefab, transform.position, Quaternion.identity);
        seed.DeleteBamboo(posState);
    }
}
