using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Wave
{
    public int wave { get; set; }
    public List<GameObject> EnemiesSequence { get; set; }
    public float SpawnInterval { get; set; } = 0;
}

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private List<GameObject> enemysequence;
    [SerializeField] private List<Vector3> Scale;
    [SerializeField] private List<float> EnemySpeed;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private GameObject boat;

    private Wave wave;
    private float timer = 0f;

    private void Awake()
    {
        wave = new Wave
        {
            wave = 1,
            EnemiesSequence = new List<GameObject>(enemysequence),
            SpawnInterval = spawnInterval
        };
    }

    void Update()
    {
        if (wave.EnemiesSequence.Count == 0) return;

        timer += Time.deltaTime;
        if (timer >= wave.SpawnInterval)
        {
            SpawnEnemy();
            timer = 0f;
        }
        
    }

    
    private void SpawnEnemy()
    {
        if (wave.EnemiesSequence.Count > 0 && !SceneManager.GetActiveScene().name.Equals("Level2"))
        {
            
            GameObject enemyPrefab = wave.EnemiesSequence[0];

            
            GameObject spawnedEnemy = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
            spawnedEnemy.GetComponent<SpriteRenderer>().sortingOrder = 5;
            spawnedEnemy.transform.localScale = Scale[0];   
            

            
            if (spawnedEnemy.TryGetComponent<SpriteRenderer>(out SpriteRenderer sr))
            {
                sr.sortingOrder = 2;
            }
            
            
            spawnedEnemy.AddComponent<Enemy>();
            spawnedEnemy.GetComponent<Enemy>().speed = EnemySpeed[0];
            wave.EnemiesSequence.RemoveAt(0);
            Scale.RemoveAt(0);
            EnemySpeed.RemoveAt(0); 
        }
        else if (SceneManager.GetActiveScene().name.Equals("Level2") && boat != null)
        {
            GameObject boatPrefab = boat;
            GameObject spawnedBoat = Instantiate(boatPrefab, transform.position, Quaternion.identity);
            spawnedBoat.AddComponent<Enemy>();
            spawnedBoat.GetComponent<Enemy>().speed = EnemySpeed[0];
            spawnedBoat.GetComponent<Enemy>().isBoat = true;
            GameObject enemyPrefab = wave.EnemiesSequence[0];

            
            GameObject spawnedEnemy = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
            spawnedEnemy.GetComponent<SpriteRenderer>().sortingOrder = 5;
            spawnedEnemy.transform.localScale = Scale[0];   
            

            
            if (spawnedEnemy.TryGetComponent<SpriteRenderer>(out SpriteRenderer sr))
            {
                sr.sortingOrder = 2;
            }
            
            spawnedBoat.layer = LayerMask.NameToLayer("Boat"); // Set the boat's layer to "Boat" so it can pass through walls
            
            spawnedEnemy.GetComponent<SpriteRenderer>().sortingOrder = 10;
            spawnedEnemy.AddComponent<Enemy>();
            spawnedEnemy.GetComponent<Enemy>().speed = EnemySpeed[0];
            wave.EnemiesSequence.RemoveAt(0);
            Scale.RemoveAt(0);
            EnemySpeed.RemoveAt(0); 
        }
    }
}
