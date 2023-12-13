using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public GameObject tilePrefabe;
    public Vector2 mapSize; //public float mapSizeX, mapSizeY;
    public Transform mapHolder;
    [Range(0,1)]
    public float outlinePrecent;

    public GameObject obsPrefab;
    // public float obsCount;
    public List<Coord> allTilesCoord = new List<Coord>();
    private Queue<Coord> shuffleQueue;
    
    [Header("Paint Colorful")]
    public Color foregroundColor, backgroundColor;
    public float minObsHeight, maxObsHeight;

    [Header("Map Fully Accessible")] [Range(0, 1)]
    public float obsPrecent;

    private Coord mapCenter;//地图中心
    private bool[,] mapObstacles; //格子上是否有障碍物

    [Header("Nac Agent")]
    public Vector2 mapMaxSize;
    private void Start()
    {
        GenerateMap();
    }

    private void GenerateMap()
    {
        for (int i = 0; i < mapSize.x; i++)
        {
            for (int j = 0; j < mapSize.y; j++)
            {
                Vector3 newPos = new Vector3(-mapSize.x / 2 + 0.5f + i,0, -mapSize.y / 2 + 0.5f + j);
                GameObject spawnTile = Instantiate(tilePrefabe, newPos, Quaternion.Euler(90, 0, 0));
                spawnTile.transform.SetParent(mapHolder);
                spawnTile.transform.localScale *= (1 - outlinePrecent);
                allTilesCoord.Add(new Coord(i, j));
            }
            
        }

        shuffleQueue = new Queue<Coord>(Utilities.ShuffleCoords(allTilesCoord.ToArray()));
        int obsCount = (int)(mapSize.x * mapSize.y * obsPrecent);

        mapCenter = new Coord((int)mapSize.x / 2, (int)mapSize.y / 2);

        mapObstacles = new bool[(int) mapSize.x, (int) mapSize.y];
        AssetBundle ab = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "test_ab"));
        AssetBundle ab2 = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "test_ab_2"));
        obsPrefab = ab.LoadAsset<GameObject>("Sphere");
        int currentObsCount = 0;
        for (int i = 0; i < obsCount; i++)
        {
            
            Coord randomCoord = GetRandomCoord();
            mapObstacles[randomCoord.x, randomCoord.y] = true;
            currentObsCount++;
            
            if (randomCoord != mapCenter && MapIsFullyAccessible(mapObstacles, currentObsCount))
            {
                float obsHeight = Mathf.Lerp(minObsHeight, maxObsHeight, UnityEngine.Random.Range(0f, 1f));
                Vector3 newPos = new Vector3(-mapSize.x / 2 + 0.5f + randomCoord.x, obsHeight / 2,
                    -mapSize.y / 2 + 0.5f + randomCoord.y);
                GameObject spwanObs = Instantiate(obsPrefab, newPos, Quaternion.identity);
                spwanObs.transform.SetParent(mapHolder);
                spwanObs.transform.localScale = new Vector3(1 - outlinePrecent, obsHeight, 1 - outlinePrecent);

                #region 给障碍物随机添加颜色

                // MeshRenderer meshRenderer = spwanObs.GetComponent<MeshRenderer>();
                // Material material = meshRenderer.material;
                // float colorPrecent = randomCoord.y / mapSize.y;
                // material.color = Color.Lerp(foregroundColor, backgroundColor, colorPrecent);
                // meshRenderer.material = material;

                #endregion
            }
            else
            {
                mapObstacles[randomCoord.x, randomCoord.y] = false;
                currentObsCount--;
            }
        }
        
        ab.Unload(true);
        ab2.Unload(false);
    }

    private bool MapIsFullyAccessible(bool[,] _mapObstacles, int _currentObsCount)
    {
        bool[,] mapFlags = new bool[_mapObstacles.GetLength(0), _mapObstacles.GetLength(1)];
        mapFlags[mapCenter.x, mapCenter.y] = true;
        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(mapCenter);
        
        int accessibleCount = 1;

        while (queue.Count > 0)
        {
            Coord currentTile = queue.Dequeue();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    int neighborX = currentTile.x + x;
                    int neighborY = currentTile.y + y;
                    if (x == 0 || y == 0)
                    {
                        if (neighborX >= 0 && neighborX < _mapObstacles.GetLength(0)
                                           && neighborY >= 0 && neighborY < _mapObstacles.GetLength(1))
                        {
                            if (!mapFlags[neighborX, neighborY] && !_mapObstacles[neighborX, neighborY])
                            {
                                mapFlags[neighborX, neighborY] = true;
                                queue.Enqueue(new Coord(neighborX, neighborY));
                                accessibleCount++;
                            }
                        }
                    }
                }
            }
        }

        int obsTargetCount = (int)mapSize.x * (int)mapSize.y - _currentObsCount;
        return accessibleCount == obsTargetCount;
    }

    private Coord GetRandomCoord()
    {
        Coord randomCoord = shuffleQueue.Dequeue();
        shuffleQueue.Enqueue(randomCoord);
        return randomCoord;
    }

    private void FixedUpdate()
    {
    }
}
[System.Serializable]
public struct Coord
{
    public int x;
    public int y;

    public Coord(int _x, int _y)
    {
        x = _x;
        y = _y;
    }
    
    public static bool operator !=(Coord c1, Coord c2)
    {
        return !(c1 == c2);
    }

    public static bool operator ==(Coord c1, Coord c2)
    {
        return c1.x == c2.x && c1.y == c2.y;
    }
}
