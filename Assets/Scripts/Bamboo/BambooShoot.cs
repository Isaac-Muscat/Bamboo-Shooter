using UnityEngine;

public class BambooShoot : MonoBehaviour
{
    public Vector2Int gridPos;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gridPos.x = Mathf.RoundToInt(transform.position.x);
        gridPos.y = Mathf.RoundToInt(transform.position.z);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
