using UnityEngine;
using UnityEngine.Events;

public class EnemyHealth : MonoBehaviour
{
    public EnemyBase enemyBase; // Reference to the EnemyBase script

    void Awake()
    {
        if (enemyBase == null)
        {
            enemyBase = GetComponent<EnemyBase>();
        }
    }

    void Start()
    {
        if (enemyBase != null)
        {
            enemyBase.OnHPChanged.AddListener(UpdateHealthUI);
        }
    }

    void UpdateHealthUI(int currentHealth)
    {
        // Update the health UI here
        Debug.Log($"Enemy Health: {currentHealth}");
    }
}