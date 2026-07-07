using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    private InputSystem_Actions inputSystem;
    private Rigidbody2D rb;
    private Vector2 moveInput;

    [Header("Player Movement Settings")]
    [Tooltip("The speed at which the player moves.")]
    [SerializeField] private float moveSpeed = 5f;
    [Tooltip("The animation component for the player.")]
    [SerializeField] private Animator animator; // Fixed: lowercase naming convention
    [Tooltip("The walkable ground/dock Tilemaps. A cell with no tile on any of these (e.g. water gaps) blocks movement.")]
    [SerializeField] private List<Tilemap> groundTilemaps;
    [Tooltip("Vertical offset applied to the ground-check point, so it samples higher than the player's pivot.")]
    [SerializeField] private float groundCheckOffsetY = 0.2f;

    void Awake()
    {
        animator = GetComponent<Animator>();
        inputSystem = new InputSystem_Actions();
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        inputSystem.Enable();
    }

    void OnDisable()
    {
        inputSystem.Disable();
    }

    void Update()
    {
        moveInput = inputSystem.Player.Move.ReadValue<Vector2>();

        if (moveInput != Vector2.zero)
        {
            if (moveInput.x > 0)
            {
                transform.localScale = new Vector3(1, 1, 1); // Face right
            }
            else if (moveInput.x < 0)
            {
                transform.localScale = new Vector3(-1, 1, 1); // Face left
            }

            animator.SetBool("IsRunning", true);
            animator.speed = 1f; // Fixed: Plays the run cycle at full, smooth speed
        }
        else
        {
            animator.SetBool("IsRunning", false);
            animator.speed = 1f;
        }
    }

    void FixedUpdate()
    {
        Vector2 delta = moveInput * moveSpeed * Time.fixedDeltaTime;
        Vector2 targetPos = rb.position + delta;

        if (groundTilemaps != null && groundTilemaps.Count > 0 && !IsWalkable(targetPos))
        {
            // Slide along the dock edge instead of freezing outright
            Vector2 xOnly = rb.position + new Vector2(delta.x, 0f);
            Vector2 yOnly = rb.position + new Vector2(0f, delta.y);

            if (IsWalkable(xOnly)) targetPos = xOnly;
            else if (IsWalkable(yOnly)) targetPos = yOnly;
            else targetPos = rb.position;
        }

        rb.MovePosition(targetPos);
    }

    bool IsWalkable(Vector2 point)
    {
       
        Vector2 checkPoint = point + Vector2.up * groundCheckOffsetY;
        foreach (Tilemap tilemap in groundTilemaps)
        {
            if (tilemap != null && tilemap.HasTile(tilemap.WorldToCell(checkPoint))) return true;
        }
        return false;
    }
    [ContextMenu("Print Ground Tilemaps")]
    private void PrintGroundTilemaps()
    {
         Debug.DrawRay(transform.position, Vector3.right * 2f, Color.red, 5f);
    }
}
