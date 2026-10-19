using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Floating health bar above an enemy.
/// Clones a world-space Canvas prefab (containing a Slider) for this enemy, keeps it above the
/// enemy's head without rotating or flipping with it, and updates it from EnemyBase.OnHPChanged.
/// Also adds a CapsuleCollider2D if the enemy has none, so towers can detect it.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Tooltip("Reference to the EnemyBase script")]
    public EnemyBase enemyBase;

    [Tooltip("Drag in the Canvas prefab that contains a Slider — a separate copy is cloned for each enemy")]
    [SerializeField] private Canvas healthCanvasPrefab;

    [Tooltip("Local position above the enemy's head where the health bar is placed")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, 0f);

    [Header("Scene view preview")]
    [Tooltip("Size of the preview box drawn at the offset position (Editor only, no effect in game)")]
    [SerializeField] private Vector3 gizmoPreviewSize = new Vector3(0.6f, 0.15f, 0.05f);

    private Canvas healthCanvas;
    private Slider healthSlider;

    void Awake()
    {
        if (enemyBase == null)
            enemyBase = GetComponent<EnemyBase>();

        if (enemyBase == null)
        {
            Debug.LogError($"[EnemyHealth] '{name}' has no EnemyBase — can't set up health bar.", this);
            return;
        }

        if (healthCanvasPrefab == null)
        {
            Debug.LogError($"[EnemyHealth] '{name}' no Canvas prefab has been assigned to the Health Canvas Prefab field", this);
            return;
        }
        if (!this.gameObject.TryGetComponent<CapsuleCollider2D>(out var collider))
        {
            this.gameObject.AddComponent<CapsuleCollider2D>();
        }

        // Clone a Canvas just for this enemy, so several enemies don't fight over the same Slider.
        healthCanvas = Instantiate(healthCanvasPrefab);
        healthCanvas.transform.SetParent(transform, worldPositionStays: false);
        healthCanvas.transform.localPosition = offset;
        healthCanvas.transform.localRotation = Quaternion.identity; // don't tilt/flip with the enemy

        // A World Space Canvas needs a worldCamera, otherwise Unity warns about UI events
        // (not strictly needed since this Slider isn't interactable, but set it properly anyway).
        if (healthCanvas.renderMode == RenderMode.WorldSpace && healthCanvas.worldCamera == null)
        {
            healthCanvas.worldCamera = Camera.main;
            if (healthCanvas.worldCamera == null)
            {
                Debug.LogWarning($"[EnemyHealth] '{name}' could not find Camera.main — " +
                                 "check that the main camera in the scene has the 'MainCamera' tag", this);
            }
        }

        healthSlider = healthCanvas.GetComponentInChildren<Slider>(true); // true = find it even if disabled
        if (healthSlider == null)
        {
            Debug.LogError($"[EnemyHealth] '{name}' cloned the Canvas but found no Slider inside it", this);
            return;
        }

        // Tint the fill image red (fillColor doesn't exist on Slider itself)
        var fillImage = healthSlider.fillRect != null ? healthSlider.fillRect.GetComponent<Image>() : null;
        if (fillImage != null) fillImage.color = Color.red;

        healthSlider.interactable = false;
    }

    void Start()
    {
        if (enemyBase == null || healthSlider == null) return;

        // Every Awake in the scene has finished by now — MaxHP/CurrentHP should be ready.
        healthSlider.maxValue = enemyBase.MaxHP;
        healthSlider.value = enemyBase.CurrentHP;
        Debug.Log($"[EnemyHealth] '{name}' health bar set up with MaxHP={enemyBase.MaxHP}, CurrentHP={enemyBase.CurrentHP}", this);

        enemyBase.OnHPChanged.AddListener(UpdateHealthUI);
    }

    void UpdateHealthUI(int currentHealth)
    {
        if (healthSlider != null)
            healthSlider.value = currentHealth;
    }

    void LateUpdate()
    {
        // Set the position ourselves every frame instead of relying only on parenting:
        // - stops the Canvas tilting/turning when the enemy turns or flips its sprite
        // - LateUpdate runs after the enemy moved in Update, so the bar doesn't jitter or lag a frame
        if (healthCanvas == null) return;

        healthCanvas.transform.position = transform.position + offset;
        healthCanvas.transform.rotation = Quaternion.identity;
    }

    // Draws a preview box at the offset position in the Scene view — helps position the health bar
    // before pressing Play, without actually instantiating anything.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f); // translucent red so it clearly reads as a preview
        Vector3 previewPos = transform.position + offset;
        Gizmos.DrawCube(previewPos, gizmoPreviewSize);

        // Solid outline makes the bounds easier to see when the box is small.
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(previewPos, gizmoPreviewSize);
    }
}