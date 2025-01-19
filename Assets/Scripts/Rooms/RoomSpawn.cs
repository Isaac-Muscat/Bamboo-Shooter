using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics.Geometry;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Random = UnityEngine.Random;

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

    public int numWeapons = 3;
    public int numKeycards = 2;

    public int[] floorState;

    [Header("References")]
    public List<RoomSeed> seeds;

    [Header("Game State")]
    public int spawnSeed = 0;
    public List<int> unexploredSeeds;
    public List<int> exploredTiles;
    public List<GameObject> centerWalls;
    public List<int> centerWallTiles;

    [Header("Bamboo Settings")]
    public int frameInterval = 10;
    private int currentFrameInterval = 0;

    public float bambooScale = 2.5f;
    public MeshRenderer bambooPlane;
    public ComputeShader bambooCS;
    public RenderTexture bambooTexture;
    private RenderTexture bambooSimTex_Out;
    public Texture2D bambooSimCPU;
    private int bambooAnimFrame = 0;

    public int XYToIDX_ROOM(int x, int y)
    {
        if (x < 0 || y < 0) return -1;
        if (x >= buildingDimensions.x || y >= buildingDimensions.y) return -1;
        return x + y * buildingDimensions.x;
    }

    private Vector2Int IDXToXY_ROOM(int idx)
    {
        return new Vector2Int(idx % buildingDimensions.x, idx / buildingDimensions.y);
    }
    
    public void SetTile_ROOM(int x, int y, int state)
    {
        int idx = XYToIDX_ROOM(x, y);
        if (idx < 0) return;
        floorState[idx] = state;
    }
    
    public int GetTile_ROOM(int x, int y)
    {
        int idx = XYToIDX_ROOM(x, y);
        if (idx < 0) return -1;
        return floorState[idx];
    }
    
    // ROOM INITIALIZATION
    
    public void GenerateRooms(PlayerController pc)
    {
        floorState = new int[buildingDimensions.x * buildingDimensions.y];

        // Set the center room
        for (int x = 0; x < centerRoomDimensions.x; x++)
        {
            for (int y = 0; y < centerRoomDimensions.y; y++)
            {
                SetTile_ROOM(
                    x + buildingDimensions.x/2 - centerRoomDimensions.x/2, 
                    y + buildingDimensions.y/2 - centerRoomDimensions.y/2, 1);
            }
        }
        
        // Set the walls around the center room
        for (int x = 0; x < centerRoomDimensions.x + 2; x++)
        {
            SetTile_ROOM(x - centerRoomDimensions.x/2 + buildingDimensions.x/2 - 1, centerRoomDimensions.y/2 + buildingDimensions.y/2, 6);
            SetTile_ROOM(x - centerRoomDimensions.x/2 + buildingDimensions.x/2 - 1, -centerRoomDimensions.y/2 + buildingDimensions.y/2 - 1, 6);
            centerWallTiles.Add(XYToIDX_ROOM(x - centerRoomDimensions.x/2 + buildingDimensions.x/2 - 1, centerRoomDimensions.y/2 + buildingDimensions.y/2));
            centerWallTiles.Add(XYToIDX_ROOM(x - centerRoomDimensions.x/2 + buildingDimensions.x/2 - 1, -centerRoomDimensions.y/2 + buildingDimensions.y/2 - 1));

        }

        for (int y = 0; y < centerRoomDimensions.y; y++)
        {
            SetTile_ROOM(centerRoomDimensions.x/2 + buildingDimensions.x/2, y - centerRoomDimensions.y/2 + buildingDimensions.y/2, 6);
            SetTile_ROOM(-centerRoomDimensions.x/2 + buildingDimensions.x/2 - 1, y - centerRoomDimensions.y/2 + buildingDimensions.y/2, 6);
            centerWallTiles.Add(XYToIDX_ROOM(centerRoomDimensions.x/2 + buildingDimensions.x/2, y - centerRoomDimensions.y/2 + buildingDimensions.y/2));
            centerWallTiles.Add(XYToIDX_ROOM(-centerRoomDimensions.x/2 + buildingDimensions.x/2 - 1, y - centerRoomDimensions.y/2 + buildingDimensions.y/2));
        }
        
        // Set the walls around the building
        for (int x = 0; x < buildingDimensions.x; x++)
        {
            int wallType = 2;
            if ((x + 1) % 4 < 3) wallType = 4;
            SetTile_ROOM(x, 0, wallType);
            SetTile_ROOM(x, buildingDimensions.y-1, wallType);
        }
        for (int y = 0; y < buildingDimensions.y; y++)
        {
            int wallType = 2;
            if ((y + 1) % 4 < 3) wallType = 4;
            SetTile_ROOM(0, y, wallType);
            SetTile_ROOM(buildingDimensions.x-1, y, wallType);
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
                if (GetTile_ROOM(roomSeed.x, roomSeed.y) == 0)
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
        List<Debris> spawnedDebris = new List<Debris>();
        for (int d = 0; d < debrisPrefabs.Length; d++)
        {
            
            for (int i = 0; i < numDebris[d]; i++)
            {
                int spawnX = Random.Range(1, buildingDimensions.x - 2);
                int spawnY = Random.Range(1, buildingDimensions.y - 2);

                int debrisIdx = XYToIDX_ROOM(spawnX, spawnY);
                if (GetTile_ROOM(spawnX, spawnY) == 1 && !invalidSpots.Contains(debrisIdx))
                {
                    // SPAWN THE DEBRIS
                    Debris spD = Instantiate(debrisPrefabs[d], new Vector3(spawnX, 0.5f, spawnY), Quaternion.identity)
                        .GetComponent<Debris>();
                    spawnedDebris.Add(spD);
                    spD.player = pc;
                    spD.loot = 0;
                    invalidSpots.Add(debrisIdx);
                }
            }
        }

        // Give debris loot
        List<int> invalidDebris = new List<int>();
        for (int i = 0; i < numWeapons; i++)
        {
            for (int z = 0; z < 100; z++)
            {
                int chosenDebris = Random.Range(0, spawnedDebris.Count-1);
                if (invalidDebris.Contains(chosenDebris)) continue;
                
                invalidDebris.Add(chosenDebris);
                spawnedDebris[chosenDebris].loot = 1;
                break;
            }
        }
        
        for (int i = 0; i < numKeycards; i++)
        {
            for (int z = 0; z < 100; z++)
            {
                int chosenDebris = Random.Range(0, spawnedDebris.Count-1);
                if (invalidDebris.Contains(chosenDebris)) continue;
                
                invalidDebris.Add(chosenDebris);
                spawnedDebris[chosenDebris].loot = 2;
                break;
            }
        }

        // Draw the room
        for (int i = 0; i < floorState.Length; i++)
        {
            if (floorState[i] == 0 || floorState[i] == 1) continue;
            GameObject tile = Instantiate(tiles[floorState[i]], transform);
            Vector2Int pos = IDXToXY_ROOM(i);
            tile.transform.position = new Vector3(pos.x, 0, pos.y);
            
            if (floorState[i] == 6) centerWalls.Add(tile);
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
            if (GetTile_ROOM(doorIDX, seedPos.y) == 2)
            {
                if (GetTile_ROOM(doorIDX + 1, seedPos.y) == 1)
                {
                    int newPathIdx = XYToIDX_ROOM(doorIDX + 1, seedPos.y);
                    if (exploredTiles.Contains(newPathIdx)) break;
                    
                    // find which seed the new path belongs to
                    foreach (int unexplored in unexploredSeeds)
                    {
                        if (seeds[unexplored].tiles.Contains(XYToIDX_ROOM(doorIDX + 1, seedPos.y)))
                        {
                            //Debug.Log("Found door from "+currentSeedIDX+" to " + unexplored);
                            // GENERATE THE DOOR
                            SetTile_ROOM(doorIDX, seedPos.y, 3);
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
            if (GetTile_ROOM(seedPos.x, doorIDX) == 2)
            {
                if (GetTile_ROOM(seedPos.x, doorIDX + 1) == 1)
                {
                    int newPathIdx = XYToIDX_ROOM(seedPos.x, doorIDX + 1);
                    if (exploredTiles.Contains(newPathIdx)) break;

                    // find which seed the new path belongs to
                    foreach (int unexplored in unexploredSeeds)
                    {
                        if (seeds[unexplored].tiles.Contains(XYToIDX_ROOM(seedPos.x, doorIDX + 1)))
                        {
                            //Debug.Log("Found door from "+currentSeedIDX+" to " + unexplored);
                            // GENERATE THE DOOR
                            SetTile_ROOM(seedPos.x, doorIDX, 3);
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
            if (GetTile_ROOM(doorIDX, seedPos.y) == 2)
            {
                if (GetTile_ROOM(doorIDX - 1, seedPos.y) == 1)
                {
                    int newPathIdx = XYToIDX_ROOM(doorIDX - 1, seedPos.y);
                    if (exploredTiles.Contains(newPathIdx)) break;

                    // find which seed the new path belongs to
                    foreach (int unexplored in unexploredSeeds)
                    {
                        if (seeds[unexplored].tiles.Contains(XYToIDX_ROOM(doorIDX - 1, seedPos.y)))
                        {
                            //Debug.Log("Found door from "+currentSeedIDX+" to " + unexplored);
                            // GENERATE THE DOOR
                            SetTile_ROOM(doorIDX, seedPos.y, 3);
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
            if (GetTile_ROOM(seedPos.x, doorIDX) == 2)
            {
                if (GetTile_ROOM(seedPos.x, doorIDX - 1) == 1)
                {
                    int newPathIdx = XYToIDX_ROOM(seedPos.x, doorIDX - 1);
                    if (exploredTiles.Contains(newPathIdx)) break;

                    // find which seed the new path belongs to
                    foreach (int unexplored in unexploredSeeds)
                    {
                        if (seeds[unexplored].tiles.Contains(XYToIDX_ROOM(seedPos.x, doorIDX - 1)))
                        {
                            //Debug.Log("Found door from "+currentSeedIDX+" to " + unexplored);
                            // GENERATE THE DOOR
                            SetTile_ROOM(seedPos.x, doorIDX, 3);
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
            if (GetTile_ROOM(doorIDX, seedPos.y) == 2)
            {
                if (GetTile_ROOM(doorIDX + 1, seedPos.y) == 1)
                {
                    int newPathIdx = XYToIDX_ROOM(doorIDX + 1, seedPos.y);
                    if (!exploredTiles.Contains(newPathIdx)) break;

                    //Debug.Log("Found nonsense door to " + currentSeedIDX);
                    // GENERATE THE DOOR
                    SetTile_ROOM(doorIDX, seedPos.y, 3);
                    return;
                }
                break;
            }
        }
        
        for (int y = 0; y <= buildingDimensions.y; y++)
        {
            int doorIDX = seedPos.y + y;
            if (GetTile_ROOM(seedPos.x, doorIDX) == 2)
            {
                if (GetTile_ROOM(seedPos.x, doorIDX + 1) == 1)
                {
                    int newPathIdx = XYToIDX_ROOM(seedPos.x, doorIDX + 1);
                    if (!exploredTiles.Contains(newPathIdx)) break;

                    //Debug.Log("Found nonsense door to " + currentSeedIDX);
                    // GENERATE THE DOOR
                    SetTile_ROOM(seedPos.x, doorIDX, 3);
                    return;
                }
                break;
            }
        }
        
        for (int x = 0; x <= buildingDimensions.x; x++)
        {
            int doorIDX = seedPos.x - x;
            if (GetTile_ROOM(doorIDX, seedPos.y) == 2)
            {
                if (GetTile_ROOM(doorIDX - 1, seedPos.y) == 1)
                {
                    int newPathIdx = XYToIDX_ROOM(doorIDX - 1, seedPos.y);
                    if (!exploredTiles.Contains(newPathIdx)) break;

                    //Debug.Log("Found nonsense door to " + currentSeedIDX);
                    // GENERATE THE DOOR
                    SetTile_ROOM(doorIDX, seedPos.y, 3);
                    return;
                }
                break;
            }
        }
        
        for (int y = 0; y <= buildingDimensions.y; y++)
        {
            int doorIDX = seedPos.y - y;
            if (GetTile_ROOM(seedPos.x, doorIDX) == 2)
            {
                if (GetTile_ROOM(seedPos.x, doorIDX - 1) == 1)
                {
                    int newPathIdx = XYToIDX_ROOM(seedPos.x, doorIDX - 1);
                    if (!exploredTiles.Contains(newPathIdx)) break;

                    //Debug.Log("Found nonsense door to " + currentSeedIDX);
                    // GENERATE THE DOOR
                    SetTile_ROOM(seedPos.x, doorIDX, 3);
                    return;
                }
                break;
            }
        }
    }

    Vector2Int FloodFillRoom(Vector2Int seed, Vector2Int currentDims, Vector2Int maxDims, int seedId)
    {
        seeds[seedId].tiles.Add(XYToIDX_ROOM(seed.x, seed.y));
        
        Vector2Int propogation = Vector2Int.one;
        if (GetTile_ROOM(seed.x, seed.y + 1) == 0)
        {
            if (currentDims.y == maxDims.y)
            {
                SetTile_ROOM(seed.x, seed.y + 1, 2);
            }
            else
            {
                SetTile_ROOM(seed.x, seed.y + 1, 1);
                FloodFillRoom(
                    new Vector2Int(seed.x, seed.y + 1),
                    new Vector2Int(currentDims.x, currentDims.y + 1), maxDims, seedId);
            }
        }
        
        if (GetTile_ROOM(seed.x, seed.y - 1) == 0)
        {
            if (currentDims.y == -maxDims.y)
            {
                SetTile_ROOM(seed.x, seed.y - 1, 2);
            }
            else
            {
                SetTile_ROOM(seed.x, seed.y - 1, 1);
                FloodFillRoom(
                    new Vector2Int(seed.x, seed.y - 1),
                    new Vector2Int(currentDims.x, currentDims.y - 1), maxDims, seedId);
            }
        }
        
        if (GetTile_ROOM(seed.x + 1, seed.y) == 0)
        {
            if (currentDims.x == maxDims.x)
            {
                SetTile_ROOM(seed.x + 1, seed.y, 2);
                if (currentDims.y == maxDims.y)
                    SetTile_ROOM(seed.x + 1, seed.y+1, 2);
                if (currentDims.y == -maxDims.y)
                    SetTile_ROOM(seed.x + 1, seed.y-1, 2);
            }
            else
            {
                SetTile_ROOM(seed.x + 1, seed.y, 1);
                FloodFillRoom(
                    new Vector2Int(seed.x + 1, seed.y),
                    new Vector2Int(currentDims.x + 1, currentDims.y), maxDims, seedId);
            }
        }
        
        if (GetTile_ROOM(seed.x - 1, seed.y) == 0)
        {
            if (currentDims.x == -maxDims.x)
            {
                SetTile_ROOM(seed.x - 1, seed.y, 2);
                if (currentDims.y == maxDims.y)
                    SetTile_ROOM(seed.x - 1, seed.y+1, 2);
                if (currentDims.y == -maxDims.y)
                    SetTile_ROOM(seed.x - 1, seed.y-1, 2);
            }
            else
            {
                SetTile_ROOM(seed.x - 1, seed.y, 1);
                FloodFillRoom(
                    new Vector2Int(seed.x - 1, seed.y),
                    new Vector2Int(currentDims.x - 1, currentDims.y), maxDims, seedId);
            }
        }

        return propogation;
    }
    
    // BAMBOO INITIALIZATION

    public void GenerateBamboo()
    {
        int texDim = Mathf.RoundToInt(bambooScale * buildingDimensions.x);
        bambooTexture = new RenderTexture(texDim, texDim, 24);
        bambooTexture.enableRandomWrite = true;
        bambooTexture.filterMode = FilterMode.Point;
        bambooTexture.Create();
        
        bambooSimTex_Out = new RenderTexture(texDim, texDim, GraphicsFormat.R32_SFloat, GraphicsFormat.None);
        bambooSimTex_Out.enableRandomWrite = true;
        bambooSimTex_Out.filterMode = FilterMode.Point;
        bambooSimTex_Out.Create();
        
        // Set up the CPU Blit
        bambooSimCPU = new Texture2D(texDim, texDim, TextureFormat.RFloat, false);
        bambooSimCPU.filterMode = FilterMode.Point;
        
        bambooPlane.material.mainTexture = bambooTexture;
    }
    
    private void FixedUpdate()
    {
        if (bambooSimCPU == null) return;
        
        if (currentFrameInterval > 0)
        {
            currentFrameInterval--;
            return;
        }

        currentFrameInterval = frameInterval;
        bambooAnimFrame++;
        
        // Apply any CPU side changes
        
        // SIMULATE - CPU copied prev image
        bambooCS.SetTexture(0, "_Sim_IN", bambooSimCPU);
        bambooCS.SetTexture(0, "_Sim_OUT", bambooSimTex_Out);
        bambooCS.SetTexture(0, "_Vis_OUT", bambooTexture);
        bambooCS.SetFloat("_rngOffset", Random.Range(0, 400));
        bambooCS.SetFloat("_animFrame", bambooAnimFrame);
        bambooCS.Dispatch(0, bambooTexture.width / 8, bambooTexture.height / 8, 1);
        
        // COPY TO CPU -- Check and edit for collisions
        RenderTexture.active = bambooSimTex_Out;
        bambooSimCPU.ReadPixels (new Rect (0, 0, bambooSimTex_Out.width, bambooSimTex_Out.height), 0, 0);
        //bambooSimCPU.SetPixel((at +i),j, new Color(1,0,0) );
        bambooSimCPU.Apply();
        //RenderTexture.active = Camera.main.targetTexture; 
    }

    public bool CollideBamboo(Vector2 pos)
    {
        Vector2Int bambooSpace = new Vector2Int(Mathf.FloorToInt(bambooSimTex_Out.width - pos.x * bambooScale), Mathf.RoundToInt(bambooSimTex_Out.height - pos.y * bambooScale));
        //Debug.Log(bambooSpace);
        if (bambooSimCPU.GetPixel(bambooSpace.x, bambooSpace.y).r > 0)
        {
            // HIT
            bambooSimCPU.SetPixel(bambooSpace.x, bambooSpace.y, new Color(0, 0, 0, 0));
            bambooSimCPU.Apply();
            return true;
        }
        return false;
    }

    public IEnumerator LowerWalls()
    {
        for (int i = 0; i < 100; i++)
        {
            for (int z = 0; z < centerWalls.Count; z++)
            {
                floorState[centerWallTiles[z]] = 1;
                centerWalls[z].transform.position -= Vector3.up * 0.02f;
            }
            yield return new WaitForEndOfFrame();
        }
        
        Time.timeScale = 1;
    }
}
