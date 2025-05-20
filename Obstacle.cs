using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Prefabs and Camera")]
    public GameObject obstacleTopPrefab;
    public GameObject obstacleBottomPrefab;
    public GameObject scoreTriggerPrefab;
    public Transform cameraTransform;

    [Header("Spawn Settings")]
    public float spawnXInterval = 5f;
    public float minY = -2f;
    public float maxY = 2f;
    public float gapY = 8f;

    [Header("Y Difference Check")]
    public float minYDifference = 1f;
    public int maxSpawnAttempts = 5;

    private float lastSpawnX;
    private float lastBottomY;

    private List<GameObject> topObstacles = new List<GameObject>();
    private List<GameObject> bottomObstacles = new List<GameObject>();
    private List<GameObject> scoreTriggers = new List<GameObject>();

    void Start()
    {
        lastSpawnX = cameraTransform.position.x;
        lastBottomY = Random.Range(minY, maxY);
    }

    void Update()
    {
        if (cameraTransform.position.x - lastSpawnX >= spawnXInterval)
        {
            SpawnObstaclePair();
            lastSpawnX = cameraTransform.position.x;
            RemoveOldObject(topObstacles);
            RemoveOldObject(bottomObstacles);
            RemoveOldObject(scoreTriggers);
        }
    }

    void SpawnObstaclePair()
    {
        float obstacleX = cameraTransform.position.x + spawnXInterval;
        float bottomY = GenerateBottomY();
        float topY = bottomY + gapY;

        Vector2 bottomPos = new Vector2(obstacleX, bottomY);
        Vector2 topPos = new Vector2(obstacleX, topY);

        GameObject trigger = Instantiate(scoreTriggerPrefab);
        BoxCollider2D collider = trigger.GetComponent<BoxCollider2D>();

        float triggerY = bottomY + gapY / 2f;

        if (collider != null)
        {
            triggerY -= collider.offset.y;
        }

        trigger.transform.position = new Vector2(obstacleX+2.17f, triggerY);

        GameObject bottom = Instantiate(obstacleBottomPrefab, bottomPos, Quaternion.identity);
        GameObject top = Instantiate(obstacleTopPrefab, topPos, Quaternion.identity);

        bottomObstacles.Add(bottom);
        topObstacles.Add(top);
        scoreTriggers.Add(trigger);
    }


    float GenerateBottomY()
    {
        float y = Random.Range(minY, maxY);
        int attempts = 0;
        while (Mathf.Abs(y - lastBottomY) < minYDifference && attempts < maxSpawnAttempts)
        {
            y = Random.Range(minY, maxY);
            attempts++;
        }
        lastBottomY = y;
        return y;
    }

    void RemoveOldObject(List<GameObject> list)
    {
        if (list.Count > 3)
        {
            Destroy(list[0]);
            list.RemoveAt(0);
        }
    }
}
