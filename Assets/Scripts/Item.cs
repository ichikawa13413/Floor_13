using UnityEngine;

[CreateAssetMenu(fileName = "Items",menuName = "Item/item")]
public class Item : ScriptableObject
{
    [SerializeField] private string itemName;
    [SerializeField] private Sprite itemImage;

    public string MyItemName { get => itemName; }
    public Sprite MyItemImage { get => itemImage; }
}
