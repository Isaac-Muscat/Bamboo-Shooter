using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    public PosStatePair(Vector2Int pos, BambooState state) { 
        this.pos = pos;
        this.state = state;
    }
};


public class BambooSeed : MonoBehaviour
{
    public RoomSpawn roomSpawn;
    public GameObject bambooPrefab1; // Assign your prefab in the Inspector
    private GameObject CAMERA_ASSEMBLY;

    public const int branchingMagScaleFactor = 1;
    public const float branchingCountRangeFactor = 10;
    private float warningDistanceThreshold = 10;

    public Dictionary<PosStatePair, int> spawnPossibilities = new Dictionary<PosStatePair, int>();

    // Grid
    public int[] bambooState;
    public BambooShoot[] spawnedBambooX;
    public BambooShoot[] spawnedBambooZ;
    public Vector2Int gridPos = new Vector2Int(0, 0);

    public PosStatePair tunnelVisionBamboo;
    public bool tunnelVisionMode = false;
    public int tunnelVisionCount = 0;
    public float tunnelVisionChance = -0.1f;
    public int tunnelVisionCountRangeMAX = 3;
    public Vector2Int tunnelVisionDirection = new Vector2Int(0, 0); // 0 - x, 1 

    Vector3 loseBambooPos;

    void Start()
    {
        CAMERA_ASSEMBLY = GameObject.Find("CAMERA_ASSEMBLY");
    }

    // Update is called once per frame
    void Update()
    {
    }

    void AddBambooToSpawned(BambooShoot bamboo) {
        if (bamboo.posState.state == BambooState.X)
        {
            spawnedBambooX[XYToIDX(bamboo.posState)] = bamboo;
        } else if (bamboo.posState.state == BambooState.Z)
        {
            spawnedBambooZ[XYToIDX(bamboo.posState)] = bamboo;
        }
    }
    public int XYToIDX(PosStatePair p)
    {
        int x = p.pos.x;
        int y = p.pos.y;
        if (x < 0 || y < 0) return -1;
        if (x >= roomSpawn.buildingDimensions.x || y >= roomSpawn.buildingDimensions.x) return -1;
        return x + y * roomSpawn.buildingDimensions.x;
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

    public IEnumerator PanCamera()
    {
        while ((CAMERA_ASSEMBLY.transform.position - loseBambooPos).magnitude > 0.5)
        {
            Vector3 dir = (loseBambooPos - CAMERA_ASSEMBLY.transform.position).normalized;
            CAMERA_ASSEMBLY.transform.position += dir * 0.1f;
            yield return new WaitForEndOfFrame();
        }
    }

    void Lose(BambooShoot newBamboo)
    {
        loseBambooPos = new Vector3(newBamboo.posState.pos.x, 0, newBamboo.posState.pos.y);
        Debug.Log("Lose");
        Time.timeScale = 0;
        StartCoroutine(PanCamera());
    }

    void IssueWarning()
    {
        Debug.Log("Warning ");
    }

    public BambooShoot SetTile(int x, int y, BambooState state)
    {
        int idx = XYToIDX(x, y);
        if (idx < 0) return null;
        if (state == BambooState.EMPTY)
        {
            Debug.LogError("WARNING NOT IMPLEMENTED");
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
        AddBambooToSpawned(newBamboo);
        if ((gridPos - newBamboo.posState.pos).magnitude > warningDistanceThreshold)
        {
            IssueWarning();
        }
        if (newBamboo.posState.pos.x >= roomSpawn.buildingDimensions.x - 1 || newBamboo.posState.pos.y >= roomSpawn.buildingDimensions.y - 1 ||
            newBamboo.posState.pos.y <= 0 || newBamboo.posState.pos.x <= 0)
        {
            Lose(newBamboo);
        }
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
        BambooShoot destroyx = spawnedBambooX[idx];
        BambooShoot destroyz = spawnedBambooZ[idx];

        if (destroyx != null)
        {
            Destroy(destroyx.gameObject);
            spawnedBambooX[idx] = null;
            // FIXME Need to decrement neigbours from list of possible growth options
        }
        if (destroyz != null)
        {
            Destroy(destroyz.gameObject);
            spawnedBambooZ[idx] = null;
            // FIXME Need to decrement neigbours from list of possible growth options
        }

    }
    public void DeleteBamboo(PosStatePair posState)
    {
        int idx = XYToIDX(posState);
        if (posState.state == BambooState.X)
        {
            BambooShoot to_delete = spawnedBambooX[idx];
            Destroy(to_delete.gameObject);
            spawnedBambooX[idx] = null;
            AddGrowthPossibilities(posState, -1);
        } else if (posState.state == BambooState.Z)
        {
            BambooShoot to_delete = spawnedBambooZ[idx];
            Destroy(to_delete.gameObject);
            spawnedBambooZ[idx] = null;
            AddGrowthPossibilities(posState, -1);
        }
    }

    PosStatePair FarthestBambooShoot()
    {
        float max_dist2 = -1;
        PosStatePair max_posState = new List<PosStatePair>(spawnPossibilities.Keys)[Random.Range(0, spawnPossibilities.Count)];
        foreach (PosStatePair posStatePair in spawnPossibilities.Keys)
        {
            float dist2 = (gridPos - posStatePair.pos).magnitude;
            if (dist2 > max_dist2)
            {
                max_dist2 = dist2;
                max_posState = posStatePair;
            }
        }
        return max_posState;
    }

    PosStatePair ClosestBambooShoot()
    {
        float min_dist2 = roomSpawn.buildingDimensions.magnitude;
        PosStatePair min_posState = new List<PosStatePair>(spawnPossibilities.Keys)[Random.Range(0, spawnPossibilities.Count)];
        foreach (PosStatePair posStatePair in spawnPossibilities.Keys)
        {
            float dist2 = (gridPos - posStatePair.pos).magnitude;
            if (dist2 < min_dist2)
            {
                min_dist2 = dist2;
                min_posState = posStatePair;
            }
        }
        return min_posState;
    }

    PosStatePair SelectTunnelVisionBamboo()
    {
        PosStatePair selected = new List<PosStatePair>(spawnPossibilities.Keys)[Random.Range(0, spawnPossibilities.Count)];
        if (spawnPossibilities.Keys.Count < 20)
        {
            selected = ClosestBambooShoot();
        }

        while (spawnPossibilities[selected] <= 0)
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
            while (spawnPossibilities[selected] <= 0)
            {
                spawnPossibilities.Remove(selected);
                selected = new List<PosStatePair>(spawnPossibilities.Keys)[Random.Range(0, spawnPossibilities.Count)];
            }
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
