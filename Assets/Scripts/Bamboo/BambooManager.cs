using UnityEngine;
using System.Collections.Generic; // Required for using List

public class BambooManager : MonoBehaviour
{
    public GameObject bambooSeedPrefab; // Assign your prefab in the Inspector
    public Vector3 minPosition = new Vector3(-10, 0, -10); // Minimum bounds
    public Vector3 maxPosition = new Vector3(10, 0, 10);   // Maximum bounds

    private List<BambooSeed> spawnedBambooSeeds = new List<BambooSeed>(); // List to store spawned prefabs

    // Update is called once per frame
    void Update()
    {
        // Trigger Grow method when Return (Enter) is pressed
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Grow();
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
        BambooSeed newBambooSeed = Instantiate(bambooSeedPrefab, randomPosition, Quaternion.identity).GetComponent<BambooSeed>();

        // Add the spawned prefab to the list
        spawnedBambooSeeds.Add(newBambooSeed);
    }

    BambooSeed SelectSeed() {
        if (spawnedBambooSeeds.Count > 0)
        {
            int randomIndex = Random.Range(0, spawnedBambooSeeds.Count);
            BambooSeed randomBamboo = spawnedBambooSeeds[randomIndex];
            return randomBamboo;
        }
        return null;
    }

    void GrowBamboo() {
        BambooSeed seed = SelectSeed();
        BambooSeed bambooScript = seed.GetComponent<BambooSeed>();
        bambooScript.Grow();
    }
}
