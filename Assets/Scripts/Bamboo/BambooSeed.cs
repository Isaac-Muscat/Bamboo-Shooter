using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;
public enum BambooState
{
    EMPTY = 0,
    X = 1 << 0,
    Z = 1 << 1
}
public struct PosStatePair
{
    public Vector2Int pos;
    public BambooState state;
    public PosStatePair(Vector2Int pos, BambooState state) { this.pos = pos; this.state = state; }
};

public class BambooSeed : MonoBehaviour
{
    public RoomSpawn roomSpawn;
    public GameObject bambooPrefab1; // Assign your prefab in the Inspector

    public const int branchingMagScaleFactor = 1;
    public const float branchingCountRangeFactor = 10;

    private List<BambooShoot> spawnedBamboo = new List<BambooShoot>(); // List to store spawned prefabs
    public Dictionary<PosStatePair, int> spawnPossibilities = new Dictionary<PosStatePair, int>();

    // Grid
    public int[] bambooState;
    public Vector2Int gridPos = new Vector2Int(0, 0);

    public PosStatePair tunnelVisionBamboo;
    public bool tunnelVisionMode = false;
    public int tunnelVisionCount = 0;
    public float tunnelVisionChance = -0.1f;
    public int tunnelVisionCountRangeMAX = 3;
    public Vector2Int tunnelVisionDirection = new Vector2Int(0, 0); // 0 - x, 1 

    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PosStatePair selected = spawnedBamboo[Random.Range(0, spawnedBamboo.Count)].posState;
            DeleteBamboo(selected);
        }
    }

    public int XYToIDX(int x, int y)
    {
        if (x < 0 || y < 0) return -1;
        if (x >= roomSpawn.buildingDimensions.x || y >= roomSpawn.buildingDimensions.x) return -1;
        return x + y * roomSpawn.buildingDimensions.x;
    }

    private Vector2Int IDXToXY(int idx)
    {
        return new Vector2Int(idx % roomSpawn.buildingDimensions.x, idx / roomSpawn.buildingDimensions.y);
    }
    void BambooIncr(Dictionary<PosStatePair, int> dict, PosStatePair key, int val)
    {
        if (dict.ContainsKey(key))
        {
            dict[key] += val;
        }
        else
        {
            dict[key] = 1;
        }
    }

    void AddGrowthPossibilities(PosStatePair bambooShoot, int val)
    {
        int x = bambooShoot.pos.x;
        int y = bambooShoot.pos.y;

        int same = GetTile(x, y);
        if (same != -1)
        {
            if ((same & (int)BambooState.X) == 0) BambooIncr(spawnPossibilities, new PosStatePair(new Vector2Int(x, y), BambooState.X), val);
            if ((same & (int)BambooState.Z) == 0) BambooIncr(spawnPossibilities, new PosStatePair(new Vector2Int(x, y), BambooState.Z), val);
        }

        if (bambooShoot.state == BambooState.X)
        {
            int pos_x = GetTile(x + 1, y);
            int neg_x = GetTile(x - 1, y);
            if (pos_x != -1 && ((pos_x & (int)BambooState.X) == 0)) BambooIncr(spawnPossibilities, new PosStatePair(new Vector2Int(x + 1, y), BambooState.X), val);
            if (neg_x != -1 && ((neg_x & (int)BambooState.X) == 0)) BambooIncr(spawnPossibilities, new PosStatePair(new Vector2Int(x - 1, y), BambooState.X), val);
        } else if (bambooShoot.state == BambooState.Z)
        {
            int pos_y = GetTile(x, y + 1);
            int neg_y = GetTile(x, y - 1);
            if (pos_y != -1 && ((pos_y & (int)BambooState.Z) == 0)) BambooIncr(spawnPossibilities, new PosStatePair(new Vector2Int(x, y + 1), BambooState.Z), val);
            if (neg_y != -1 && ((neg_y & (int)BambooState.Z) == 0)) BambooIncr(spawnPossibilities, new PosStatePair(new Vector2Int(x, y - 1), BambooState.Z), val);
        }
    }

    public BambooShoot SetTile(int x, int y, BambooState state)
    {
        int idx = XYToIDX(x, y);
        if (idx < 0) return null;
        if (state == BambooState.EMPTY)
        {
            bambooState[idx] = 0;
            DeleteBamboo(idx);
            return null;
        } else
        {
            bambooState[idx] |= (int)state;
        }

        Quaternion rotation = Quaternion.Euler(0, 0, 0);
        Vector3 position = new Vector3(x, 0, y);
        if (state == BambooState.Z)
        {
            rotation = Quaternion.Euler(0, 90f, 0);
        }
        PosStatePair cur_possibility = new PosStatePair(new Vector2Int(x, y), state);
        AddGrowthPossibilities(cur_possibility, 1);
        spawnPossibilities.Remove(cur_possibility);
        BambooShoot newBamboo = Instantiate(bambooPrefab1, position, rotation).GetComponent<BambooShoot>();
        newBamboo.posState = cur_possibility;
        newBamboo.seed = this;
        spawnedBamboo.Add(newBamboo);
        return newBamboo;
    }

    public int GetTile(int x, int y)
    {
        int idx = XYToIDX(x, y);
        if (idx < 0) return -1;
        return bambooState[idx];
    }
    BambooState RandBambooState()
    {
        return (BambooState)Random.Range(1, 3);
    }
    public void DeleteBamboo(int idx)
    {
        foreach (BambooShoot bambmooShoot in spawnedBamboo)
        {
            if (XYToIDX(bambmooShoot.posState.pos.x, bambmooShoot.posState.pos.y) == idx)
            {
                Destroy(bambmooShoot.gameObject);
                spawnedBamboo.Remove(bambmooShoot);
                // FIXME Need to decrement neigbours from list of possible growth options
            }
        }
    }
    public void DeleteBamboo(PosStatePair posState)
    {
        BambooShoot to_delete = null;
        foreach (BambooShoot bambooShoot in spawnedBamboo)
        {
            if (bambooShoot.posState.pos.x == posState.pos.x && bambooShoot.posState.pos.y == posState.pos.y &&
                bambooShoot.posState.state == posState.state)
            {
                to_delete = bambooShoot;
                break;
            }
        }
        if (to_delete != null)
        {
            Debug.Log("IN DELETE");
            Destroy(to_delete.gameObject);
            spawnedBamboo.Remove(to_delete);
            // FIXME Need to remove neigbours from list of possible growth options
            AddGrowthPossibilities(posState, -1);
        }
    }


    PosStatePair SelectTunnelVisionBamboo()
    {
        PosStatePair selected = new List<PosStatePair>(spawnPossibilities.Keys)[Random.Range(0, spawnPossibilities.Count)];
        while (spawnPossibilities[selected] == 0)
        {
            spawnPossibilities.Remove(selected);
            selected = new List<PosStatePair>(spawnPossibilities.Keys)[Random.Range(0, spawnPossibilities.Count)];
        }
        return selected;
    }

    

    Vector2Int SelectTunnelVisionDirection(PosStatePair selected)
    {
        Vector2Int relPosToSeed = selected.pos - gridPos;
        if (selected.state == BambooState.X) {
            if (relPosToSeed.x > 0) return new Vector2Int(1, 0);
            else if (relPosToSeed.x < 0) return new Vector2Int(-1, 0);
            else return new Vector2Int(Random.value < 0.5f ? -1 : 1, 0);
        } else if (selected.state == BambooState.Z) {
            if (relPosToSeed.y > 0) return new Vector2Int(0, 1);
            else if (relPosToSeed.y < 0) new Vector2Int(0, -1);
            else return new Vector2Int(0, Random.value < 0.5f ? -1 : 1);
        }
        return new Vector2Int(0, 0);
    }

    public void Grow() {
        if (tunnelVisionMode)
        {
            PosStatePair selected = new PosStatePair(new Vector2Int(tunnelVisionBamboo.pos.x, tunnelVisionBamboo.pos.y), tunnelVisionBamboo.state);
            if (spawnPossibilities.ContainsKey(selected))
            {
                SetTile(selected.pos.x, selected.pos.y, selected.state);
            } else
            {
                tunnelVisionMode = false;
                Grow();
            }
            selected.pos += tunnelVisionDirection;
            tunnelVisionBamboo = selected;
            tunnelVisionCount--;
            if (tunnelVisionCount <= 0)
            {
                tunnelVisionMode = false;
            }
        } else
        {
            PosStatePair selected = new List<PosStatePair>(spawnPossibilities.Keys)[Random.Range(0, spawnPossibilities.Count)];
            SetTile(selected.pos.x, selected.pos.y, selected.state);

            if (Random.Range(0.0f, 1.0f) < tunnelVisionChance)
            {
                tunnelVisionMode = true;
                tunnelVisionCount = tunnelVisionCountRangeMAX;
                tunnelVisionBamboo = SelectTunnelVisionBamboo();
                tunnelVisionDirection = SelectTunnelVisionDirection(tunnelVisionBamboo);
            }
        }
    }

}
