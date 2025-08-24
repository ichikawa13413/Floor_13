using UnityEngine;

[CreateAssetMenu(fileName = "Items",menuName = "Item/item")]
public class Item : ScriptableObject
{
    [SerializeField] private string itemName;
    [SerializeField] private Sprite itemImage;
    [SerializeField] private GameObject itemObject;

    public string MyItemName { get => itemName; }
    public Sprite MyItemImage { get => itemImage; }
    public GameObject MyItemObject { get => itemObject; }
}
