using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Player Economy")]
    public int currentGold = 500;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // This is the core logic function that processes purchases
    public bool TryBuyItem(ShopItemData item)
    {
        if (item == null) return false;

        if (currentGold >= item.price)
        {
            currentGold -= item.price;
            Debug.Log($"Successfully bought {item.itemName}! Remaining Gold: {currentGold}");

            // Trigger your custom inventory item unlocks right here
            UnlockPurchasedItem(item);

            return true; // Purchase completed successfully
        }
        else
        {
            Debug.LogWarning($"Not enough gold to buy {item.itemName}! Need {item.price} gold.");
            return false; // Purchase failed
        }
    }

    private void UnlockPurchasedItem(ShopItemData item)
    {
        // Example logic link: Add to inventory array or unlock a skin variable
    }
}
