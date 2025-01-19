using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Modifiers")]
    public float speed = 10;
    public float drag = 0;
    public int livingFrames = 1;
    public float variance = 0.2f;
    [Header("State")]
    public Vector2 velocity;
    public bool fired = false;

    public void Fire(Vector2 pos, Vector2 dir)
    {
        fired = true;
        transform.position = new Vector3(pos.x, 0.5f, pos.y);
        velocity = dir * speed;
        Vector2 cross = Vector2.Perpendicular(dir);
        float varianceRand = Random.Range(-variance, variance);
        velocity += cross * (speed * varianceRand);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!fired) return;
        if (livingFrames <= 0)
        {
            Destroy(gameObject);
            return;
        }
        
        livingFrames--;
        velocity *= (drag * Time.fixedDeltaTime) + 1;
        transform.position += new Vector3(velocity.x, 0, velocity.y) * Time.fixedDeltaTime;
        
        // TODO: Raycast
    }
}
