
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 1.5f;
    public bool isBoat = false;
    [Tooltip("The direction the enemy starts walking when it spawns.")]
    public Vector2 currentDirection = Vector2.right; // Starts moving Right (1, 0)
    private bool isCollidingWithWall = false; // Track if currently colliding with a wall
    private float currentspeed;
    

    private void Start()
    {
        currentspeed = speed;
        if (!this.TryGetComponent<CapsuleCollider2D>(out CapsuleCollider2D col))
        {
            col = this.gameObject.AddComponent<CapsuleCollider2D>();

            // 1. Make the hitbox tiny (like the shoes!)
            col.size = new Vector2(1f, 1f);

            // 2. ACTUALLY make it a trigger so it doesn't crash into your blue walls!
            col.isTrigger = false;
        }
        if (isBoat)
        {
            transform.localScale = new Vector3(0.5f,0.5f,0); // Flip the boat to face left
            transform.position += new Vector3(0, -0.1f, 0); // Adjust position so it doesn't spawn halfway in the wall
            this.GetComponent<CapsuleCollider2D>().isTrigger = true; // Make the boat's collider a trigger so it can pass through walls
            this.GetComponent<CapsuleCollider2D>().size = new Vector2(1.5f, 1.5f); // Make sure the boat is behind the enemies
        }
        // If it doesn't have a collider, add one and set it up correctly!
        
    }

    void Update()
    {
        if (isBoat&& Physics2D.OverlapCircle(transform.position, 0.1f, LayerMask.GetMask("Path"))) // If the boat is colliding with a wall
        {
            speed = 1f; // Slow down the boat when it hits a wall
            currentDirection = Vector2.left + new Vector2(0, Random.Range(-1.4f, 1.5f)); // Stop the boat from moving
            this.GetComponent<SpriteRenderer>().sortingOrder = -1; // Make sure the boat is behind the enemies
        }
        else if(Physics2D.OverlapCircle(transform.position, 0.1f, LayerMask.GetMask("Path")))
        {
            isCollidingWithWall = true; // Set the flag to true when colliding with a wall
            speed = currentspeed; // Reset speed to normal when colliding with a wall
            
        }
        else if (!isCollidingWithWall)
        {
            speed = 1f;
        }
        // Continuously move in the current Vector2 direction
        transform.Translate(currentDirection.normalized * speed * Time.deltaTime, Space.World);

        // Optional: Simple flip logic so they face left/right
        if (currentDirection.x > 0.01f) transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
        else if (currentDirection.x < -0.01f) transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
        if (Physics2D.OverlapCircle(transform.position + new Vector3(0, 0.1f, 0), 0.1f, LayerMask.GetMask("Boat")))
        {
            speed = 0.5f; // Slow down the enemy when colliding with the boat
        }
    }
    
    void OnDrawGizmos()
    {
        // Visualize the collision check radius in the editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
    }
}