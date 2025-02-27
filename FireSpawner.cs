using UnityEngine;

public class FireSpawner : MonoBehaviour
{
    public GameObject firePrefab; // Fire prefab to instantiate
    public float spawnInterval = 3f; // Interval to spawn fires
    public Transform[] spawnPoints; // Points where fires can spawn

    private void Start()
    {
        InvokeRepeating("SpawnFire", 0f, spawnInterval); // Start spawning fires
    }

    void SpawnFire()
    {
        int randomIndex = Random.Range(0, spawnPoints.Length);
        Instantiate(firePrefab, spawnPoints[randomIndex].position, Quaternion.identity);
    }
}
