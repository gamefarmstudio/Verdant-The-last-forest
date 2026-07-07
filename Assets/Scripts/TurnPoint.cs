using UnityEngine;
using System.Collections.Generic;

public class TurnPoint : MonoBehaviour
{
    [Header("Turn Settings")]
    [Tooltip("Add all the possible directions the enemy could take at this intersection!")]
    public Vector2[] possibleDirections;

    [Tooltip("Keep this tiny (e.g. 0.05) so they turn exactly on the dot!")]
    public float turnRadius = 0.05f;

    // Memory list to prevent turning the same enemy twice
    private HashSet<Enemy> alreadyTurnedEnemies = new HashSet<Enemy>();

    void Update()
    {
        alreadyTurnedEnemies.RemoveWhere(e => e == null);
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemy in allEnemies)
        {
            if (alreadyTurnedEnemies.Contains(enemy)) continue;

            float distance = Vector2.Distance(enemy.transform.position, transform.position);

            if (distance <= turnRadius)
            {
                // Make sure we actually gave this script some directions in the Inspector
                if (possibleDirections != null && possibleDirections.Length > 0)
                {
                    // Roll the dice and pick a random direction from the list!
                    int randomIndex = Random.Range(0, possibleDirections.Length);
                    enemy.currentDirection = possibleDirections[randomIndex];
                }

                alreadyTurnedEnemies.Add(enemy);
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, turnRadius);
    }
}