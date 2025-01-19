using UnityEngine;

public class SceneState : MonoBehaviour
{
    public int maxGrowthRate = 6;
    public int growthRate = 6;
    private static SceneState instance;

    private void Awake()
    {
        // Ensure there's only one instance
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Keep this object alive across scenes
        }
        else
        {
            Destroy(gameObject); // Destroy duplicates
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void MakeHarder()
    {
        growthRate--;
    }

    public void Reset()
    {
        growthRate = maxGrowthRate;
    }
}
