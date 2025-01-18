using UnityEngine;
using System.Collections.Generic; // Required for using List

public class BambooManager : MonoBehaviour
{
    public GameObject bambooSeedPrefab; // Assign your prefab in the Inspector
    public Vector3 minPosition = new Vector3(-10, 0, -10); // Minimum bounds
    public Vector3 maxPosition = new Vector3(10, 0, 10);   // Maximum bounds

    private List<GameObject> spawnedBambooSeeds = new List<GameObject>(); // List to store spawned prefabs

    // Update is called once per frame
    void Update()
    {
        // Trigger Grow method when Return (Enter) is pressed
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Grow();
        } else if (Input.GetKeyDown(KeyCode.Space)) {
            GrowBamboo();
        }
    }

    void Grow()
    {
        // Generate a random position within the bounds
        Vector3 randomPosition = new Vector3(
            Random.Range(minPosition.x, maxPosition.x),
            Random.Range(minPosition.y, maxPosition.y),
            Random.Range(minPosition.z, maxPosition.z)
        );

        // Instantiate the prefab
        GameObject newBambooSeed = Instantiate(bambooSeedPrefab, randomPosition, Quaternion.identity);

        // Add the spawned prefab to the list
        spawnedBambooSeeds.Add(newBambooSeed);
    }

    GameObject SelectSeed() {
        if (spawnedBambooSeeds.Count > 0)
        {
            int randomIndex = Random.Range(0, spawnedBambooSeeds.Count);
            GameObject randomBamboo = spawnedBambooSeeds[randomIndex];
            return randomBamboo;
        }

        return null;
    }

    void GrowBamboo() {
        GameObject seed = SelectSeed();
        //seed.Grow();
    }
}
