using UnityEngine;

public class BoostBrightness : MonoBehaviour
{
    [SerializeField] private Vector4 boostColor = new Vector4(2f, 2f, 2f, 1f);
    [ContextMenu("Boost Brightness")]
    public void Boost()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        // Multiplying white by 3 forces it past the 255 limits!
        renderer.color = new Color(boostColor.x, boostColor.y, boostColor.z, boostColor.w);
    }
}
