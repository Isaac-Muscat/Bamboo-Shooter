using System.Collections.Generic;
using Unity.Mathematics.Geometry;
using UnityEngine;

public class RoomSpawn : MonoBehaviour
{

    [Header("Prefabs")]
    // TILES: UNINIT, FLOOR, WALL, DOOR
    public GameObject[] tiles;
    public GameObject roomSeedPrefab;
    
    [Header("Settings")]
    public bool regenerate = false;
    public Vector2Int buildingDimensions = new Vector2Int(20, 20);
    public Vector2Int centerRoomDimensions = new Vector2Int(5, 5);
    public int numRooms = 6;
    public Vector2Int minRoomSize = new Vector2Int(5, 5);
    public Vector2Int maxRoomSize = new Vector2Int(20, 20);
    
    public int[] numDebris;
    public GameObject[] debrisPrefabs;
    

    public int[] floorState;

    [Header("References")]
    public List<RoomSeed> seeds;

    [Header("Game State")]
    public int spawnSeed = 0;
    public List<int> unexploredSeeds;
    public List<int> exploredTiles;

    public int XYToIDX(int x, int y)
    {
        if (x < 0 || y < 0) return -1;
        if (x >= buildingDimensions.x || y >= buildingDimensions.y) return -1;
        return x + y * buildingDimensions.x;
    }

    private Vector2Int IDXToXY(int idx)
    {
        return new Vector2Int(idx % buildingDimensions.x, idx / buildingDimensions.y);
    }
    
    public void SetTile(int x, int y, int state)
    {
        int idx = XYToIDX(x, y);
        if (idx < 0) return;
        floorState[idx] = state;
    }
    
    public int GetTile(int x, int y)
    {
        int idx = XYToIDX(x, y);
        if (idx < 0) return -1;
        return floorState[idx];
    }
    
    // Spawn the rooms
    public void GenerateRooms()
    {
        floorState = new int[buildingDimensions.x * buildingDimensions.y];

        // Set the center room
        for (int x = 0; x < centerRoomDimensions.x; x++)
        {
            for (int y = 0; y < centerRoomDimensions.y; y++)
            {
                SetTile(
                    x + buildingDimensions.x/2 - centerRoomDimensions.x/2, 
                    y + buildingDimensions.y/2 - centerRoomDimensions.y/2, 1);
            }
        }
        
        // Set the walls around the center room
        for (int x = 0; x < centerRoomDimensions.x + 2; x++)
        {
            SetTile(x - centerRoomDimensions.x/2 + buildingDimensions.x/2 - 1, centerRoomDimensions.y/2 + buildingDimensions.y/2, 2);
            SetTile(x - centerRoomDimensions.x/2 + buildingDimensions.x/2 - 1, -centerRoomDimensions.y/2 + buildingDimensions.y/2 - 1, 2);
        }

        for (int y = 0; y < centerRoomDimensions.y; y++)
        {
            SetTile(centerRoomDimensions.x/2 + buildingDimensions.x/2, y - centerRoomDimensions.y/2 + buildingDimensions.y/2, 2);
            SetTile(-centerRoomDimensions.x/2 + buildingDimensions.x/2 - 1, y - centerRoomDimensions.y/2 + buildingDimensions.y/2, 2);
        }
        
        // Set the walls around the building
        for (int x = 0; x < buildingDimensions.x; x++)
        {
            SetTile(x, 0, 2);
            SetTile(x, buildingDimensions.y-1, 2);
        }
        for (int y = 0; y < buildingDimensions.y; y++)
        {
            SetTile(0, y, 2);
            SetTile(buildingDimensions.x-1, y, 2);
        }

        // spawn a bunch of rooms
        for (int i = 0; i < numRooms; i++)
        {
            // spawn the room
            Vector2Int roomSeed = Vector2Int.zero;
            // move the room seed outside of existing rooms, limited to 100 iters
            bool seedValid = false;
            for (int z = 0; z < 100; z++)
            {
                roomSeed = new Vector2Int(Random.Range(0, buildingDimensions.x), Random.Range(0, buildingDimensions.y));
                if (GetTile(roomSeed.x, roomSeed.y) == 0)
                {
                    seedValid = true;
                    break;
                }
            }

            if (!seedValid) continue;

            // generate the desired size
            Vector2Int desiredDims = new Vector2Int(
                Random.Range(minRoomSize.x, maxRoomSize.x), 
                Random.Range(minRoomSize.y, maxRoomSize.y));

            RoomSeed seed = Instantiate(roomSeedPrefab, transform).GetComponent<RoomSeed>();
            seed.transform.position = new Vector3(roomSeed.x, -5, roomSeed.y);
            seed.pos = roomSeed;
            seeds.Add(seed);
            
            // Flood fill from the room seed
            FloodFillRoom(roomSeed, Vector2Int.zero, desiredDims, seeds.Count - 1);
        }
        
        // Generate the doors
        unexploredSeeds = new List<int>();
        for (int i = 0; i < seeds.Count; i++) unexploredSeeds.Add(i);
        exploredTiles = new List<int>();
        
        spawnSeed = Random.Range(0, seeds.Count - 1);

        // Connect doors sensibly
        RecurseDoors(spawnSeed);
        
        // Connect remaining doors randomly
        foreach (int unexplored in unexploredSeeds)
        {
            FindDoorConnection(unexplored);
        }
        
        // TODO: Generate the door to the center room
        
        // Spawn debris

        List<int> invalidSpots = new List<int>();
        for (int d = 0; d < debrisPrefabs.Length; d++)
        {
            
            for (int i = 0; i < numDebris[d]; i++)
            {
                int spawnX = Random.Range(1, buildingDimensions.x - 2);
                int spawnY = Random.Range(1, buildingDimensions.y - 2);

                int debrisIdx = XYToIDX(spawnX, spawnY);
                if (GetTile(spawnX, spawnY) == 1 && !invalidSpots.Contains(debrisIdx))
                {
                    // SPAWN THE DEBRIS
                    Instantiate(debrisPrefabs[d], new Vector3(spawnX, 0.5f, spawnY), Quaternion.identity);
                    invalidSpots.Add(debrisIdx);
                }
            }
        }

        // Draw the room
        for (int i = 0; i < floorState.Length; i++)
        {
            if (floorState[i] == 0 || floorState[i] == 1) continue;
            GameObject tile = Instantiate(tiles[floorState[i]], transform);
            Vector2Int pos = IDXToXY(i);
            tile.transform.position = new Vector3(pos.x, 0, pos.y);
        }
    }

    void RecurseDoors(int currentSeedIDX)
    {
        if (!unexploredSeeds.Remove(currentSeedIDX))
        {
            //Debug.Log("Failed to remove " + currentSeedIDX);
        }
        else
        {
            //Debug.Log("REMOVED " + currentSeedIDX);
        }
        foreach(int tile in seeds[currentSeedIDX].tiles) exploredTiles.Add(tile);
        Vector2Int seedPos = seeds[currentSeedIDX].pos;
        
        for (int x = 0; x <= buildingDimensions.x; x++)
        {
            int doorIDX = seedPos.x + x;
            if (GetTile(doorIDX, seedPos.y) == 2)
            {
                if (GetTile(doorIDX + 1, seedPos.y) == 1)
                {
                    int newPathIdx = XYToIDX(doorIDX + 1, seedPos.y);
                    if (exploredTiles.Contains(newPathIdx)) break;
                    
                    // find which seed the new path belongs to
                    foreach (int unexplored in unexploredSeeds)
                    {
                        if (seeds[unexplored].tiles.Contains(XYToIDX(doorIDX + 1, seedPos.y)))
                        {
                            //Debug.Log("Found door from "+currentSeedIDX+" to " + unexplored);
                            // GENERATE THE DOOR
                            SetTile(doorIDX, seedPos.y, 3);
                            RecurseDoors(unexplored);
                            break;
                        }
                    }
                }
                break;
            }
        }
        
        for (int y = 0; y <= buildingDimensions.y; y++)
        {
            int doorIDX = seedPos.y + y;
            if (GetTile(seedPos.x, doorIDX) == 2)
            {
                if (GetTile(seedPos.x, doorIDX + 1) == 1)
                {
                    int newPathIdx = XYToIDX(seedPos.x, doorIDX + 1);
                    if (exploredTiles.Contains(newPathIdx)) break;

                    // find which seed the new path belongs to
                    foreach (int unexplored in unexploredSeeds)
                    {
                        if (seeds[unexplored].tiles.Contains(XYToIDX(seedPos.x, doorIDX + 1)))
                        {
                            //Debug.Log("Found door from "+currentSeedIDX+" to " + unexplored);
                            // GENERATE THE DOOR
                            SetTile(seedPos.x, doorIDX, 3);
                            RecurseDoors(unexplored);
                            break;
                        }
                    }
                }
                break;
            }
        }
        
        for (int x = 0; x <= buildingDimensions.x; x++)
        {
            int doorIDX = seedPos.x - x;
            if (GetTile(doorIDX, seedPos.y) == 2)
            {
                if (GetTile(doorIDX - 1, seedPos.y) == 1)
                {
                    int newPathIdx = XYToIDX(doorIDX - 1, seedPos.y);
                    if (exploredTiles.Contains(newPathIdx)) break;

                    // find which seed the new path belongs to
                    foreach (int unexplored in unexploredSeeds)
                    {
                        if (seeds[unexplored].tiles.Contains(XYToIDX(doorIDX - 1, seedPos.y)))
                        {
                            //Debug.Log("Found door from "+currentSeedIDX+" to " + unexplored);
                            // GENERATE THE DOOR
                            SetTile(doorIDX, seedPos.y, 3);
                            RecurseDoors(unexplored);
                            break;
                        }
                    }
                }
                break;
            }
        }
        
        for (int y = 0; y <= buildingDimensions.y; y++)
        {
            int doorIDX = seedPos.y - y;
            if (GetTile(seedPos.x, doorIDX) == 2)
            {
                if (GetTile(seedPos.x, doorIDX - 1) == 1)
                {
                    int newPathIdx = XYToIDX(seedPos.x, doorIDX - 1);
                    if (exploredTiles.Contains(newPathIdx)) break;

                    // find which seed the new path belongs to
                    foreach (int unexplored in unexploredSeeds)
                    {
                        if (seeds[unexplored].tiles.Contains(XYToIDX(seedPos.x, doorIDX - 1)))
                        {
                            //Debug.Log("Found door from "+currentSeedIDX+" to " + unexplored);
                            // GENERATE THE DOOR
                            SetTile(seedPos.x, doorIDX, 3);
                            RecurseDoors(unexplored);
                            break;
                        }
                    }
                }
                break;
            }
        }
    }

    void FindDoorConnection(int currentSeedIDX)
    {
        foreach(int tile in seeds[currentSeedIDX].tiles) exploredTiles.Add(tile);
        Vector2Int seedPos = seeds[currentSeedIDX].pos;
        
        for (int x = 0; x <= buildingDimensions.x; x++)
        {
            int doorIDX = seedPos.x + x;
            if (GetTile(doorIDX, seedPos.y) == 2)
            {
                if (GetTile(doorIDX + 1, seedPos.y) == 1)
                {
                    int newPathIdx = XYToIDX(doorIDX + 1, seedPos.y);
                    if (!exploredTiles.Contains(newPathIdx)) break;

                    //Debug.Log("Found nonsense door to " + currentSeedIDX);
                    // GENERATE THE DOOR
                    SetTile(doorIDX, seedPos.y, 3);
                    return;
                }
                break;
            }
        }
        
        for (int y = 0; y <= buildingDimensions.y; y++)
        {
            int doorIDX = seedPos.y + y;
            if (GetTile(seedPos.x, doorIDX) == 2)
            {
                if (GetTile(seedPos.x, doorIDX + 1) == 1)
                {
                    int newPathIdx = XYToIDX(seedPos.x, doorIDX + 1);
                    if (!exploredTiles.Contains(newPathIdx)) break;

                    //Debug.Log("Found nonsense door to " + currentSeedIDX);
                    // GENERATE THE DOOR
                    SetTile(seedPos.x, doorIDX, 3);
                    return;
                }
                break;
            }
        }
        
        for (int x = 0; x <= buildingDimensions.x; x++)
        {
            int doorIDX = seedPos.x - x;
            if (GetTile(doorIDX, seedPos.y) == 2)
            {
                if (GetTile(doorIDX - 1, seedPos.y) == 1)
                {
                    int newPathIdx = XYToIDX(doorIDX - 1, seedPos.y);
                    if (!exploredTiles.Contains(newPathIdx)) break;

                    //Debug.Log("Found nonsense door to " + currentSeedIDX);
                    // GENERATE THE DOOR
                    SetTile(doorIDX, seedPos.y, 3);
                    return;
                }
                break;
            }
        }
        
        for (int y = 0; y <= buildingDimensions.y; y++)
        {
            int doorIDX = seedPos.y - y;
            if (GetTile(seedPos.x, doorIDX) == 2)
            {
                if (GetTile(seedPos.x, doorIDX - 1) == 1)
                {
                    int newPathIdx = XYToIDX(seedPos.x, doorIDX - 1);
                    if (!exploredTiles.Contains(newPathIdx)) break;

                    //Debug.Log("Found nonsense door to " + currentSeedIDX);
                    // GENERATE THE DOOR
                    SetTile(seedPos.x, doorIDX, 3);
                    return;
                }
                break;
            }
        }
    }

    Vector2Int FloodFillRoom(Vector2Int seed, Vector2Int currentDims, Vector2Int maxDims, int seedId)
    {
        seeds[seedId].tiles.Add(XYToIDX(seed.x, seed.y));
        
        Vector2Int propogation = Vector2Int.one;
        if (GetTile(seed.x, seed.y + 1) == 0)
        {
            if (currentDims.y == maxDims.y)
            {
                SetTile(seed.x, seed.y + 1, 2);
            }
            else
            {
                SetTile(seed.x, seed.y + 1, 1);
                FloodFillRoom(
                    new Vector2Int(seed.x, seed.y + 1),
                    new Vector2Int(currentDims.x, currentDims.y + 1), maxDims, seedId);
            }
        }
        
        if (GetTile(seed.x, seed.y - 1) == 0)
        {
            if (currentDims.y == -maxDims.y)
            {
                SetTile(seed.x, seed.y - 1, 2);
            }
            else
            {
                SetTile(seed.x, seed.y - 1, 1);
                FloodFillRoom(
                    new Vector2Int(seed.x, seed.y - 1),
                    new Vector2Int(currentDims.x, currentDims.y - 1), maxDims, seedId);
            }
        }
        
        if (GetTile(seed.x + 1, seed.y) == 0)
        {
            if (currentDims.x == maxDims.x)
            {
                SetTile(seed.x + 1, seed.y, 2);
                if (currentDims.y == maxDims.y)
                    SetTile(seed.x + 1, seed.y+1, 2);
                if (currentDims.y == -maxDims.y)
                    SetTile(seed.x + 1, seed.y-1, 2);
            }
            else
            {
                SetTile(seed.x + 1, seed.y, 1);
                FloodFillRoom(
                    new Vector2Int(seed.x + 1, seed.y),
                    new Vector2Int(currentDims.x + 1, currentDims.y), maxDims, seedId);
            }
        }
        
        if (GetTile(seed.x - 1, seed.y) == 0)
        {
            if (currentDims.x == -maxDims.x)
            {
                SetTile(seed.x - 1, seed.y, 2);
                if (currentDims.y == maxDims.y)
                    SetTile(seed.x - 1, seed.y+1, 2);
                if (currentDims.y == -maxDims.y)
                    SetTile(seed.x - 1, seed.y-1, 2);
            }
            else
            {
                SetTile(seed.x - 1, seed.y, 1);
                FloodFillRoom(
                    new Vector2Int(seed.x - 1, seed.y),
                    new Vector2Int(currentDims.x - 1, currentDims.y), maxDims, seedId);
            }
        }

        return propogation;
    }
}
