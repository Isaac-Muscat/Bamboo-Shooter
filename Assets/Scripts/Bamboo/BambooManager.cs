using UnityEngine;
using System.Collections.Generic; // Required for using List
using static BambooSeed;
using System.Collections;



public class BambooManager : MonoBehaviour
{
    public GameObject bambooSeedPrefab; // Assign your prefab in the Inspector
    public RoomSpawn roomSpawn;

    public int numSeedsToSpawn = 5;
    public float bambooGrowSpeed = 0.1f;

    private List<BambooSeed> spawnedBambooSeeds = new List<BambooSeed>(); // List to store spawned prefabs

    public void GenerateSeeds()
    {
        // Instantiate the prefab
        Vector3 position = new Vector3(roomSpawn.buildingDimensions.x / 2, 0, roomSpawn.buildingDimensions.y / 2);
        BambooSeed newBambooSeed = Instantiate(bambooSeedPrefab, position, Quaternion.identity).GetComponent<BambooSeed>();
        newBambooSeed.gridPos = new Vector2Int(roomSpawn.buildingDimensions.x / 2, roomSpawn.buildingDimensions.y / 2);
        newBambooSeed.transform.position = new Vector3(newBambooSeed.gridPos.x, 0, newBambooSeed.gridPos.y);
        newBambooSeed.bambooState = new int[roomSpawn.buildingDimensions.x * roomSpawn.buildingDimensions.x];
        newBambooSeed.spawnPossibilities[new PosStatePair(newBambooSeed.gridPos, BambooState.X)] = 1;
        newBambooSeed.spawnPossibilities[new PosStatePair(newBambooSeed.gridPos, BambooState.Z)] = 1;
        newBambooSeed.roomSpawn = roomSpawn;
        newBambooSeed.tunnelVisionChance = 0.2f;
        newBambooSeed.tunnelVisionCountRangeMAX = 6;

        // Add the spawned prefab to the list
        spawnedBambooSeeds.Add(newBambooSeed);
    }

    public IEnumerator StartGrowing()
    {
        while (true)
        {
            GrowBamboo();
            yield return new WaitForSeconds(bambooGrowSpeed);
        }
    }

    void GrowBamboo() {
        foreach (BambooSeed seed in spawnedBambooSeeds)
        {
            seed.Grow();
        }
    }

    // Generic method to get random elements from a list
    List<T> GetRandomElements<T>(List<T> list, int count)
    {
        List<T> result = new List<T>();
        List<T> tempList = new List<T>(list); // Make a copy of the original list

        for (int i = 0; i < count; i++)
        {
            if (tempList.Count == 0) break; // Prevent errors if the list is empty
            int randomIndex = Random.Range(0, tempList.Count); // Get a random index
            result.Add(tempList[randomIndex]); // Add the random element to the result
            tempList.RemoveAt(randomIndex); // Remove the selected element to avoid duplicates
        }

        return result;
    }
}
