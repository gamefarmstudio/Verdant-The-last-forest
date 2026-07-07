using UnityEngine;

[CreateAssetMenu(fileName = "NewShopItem", menuName = "Shop/Item")]
public class ShopItemData : ScriptableObject
{
    public string itemName;
    public int price;
    public Sprite icon;
    [TextArea] public string description;
}
