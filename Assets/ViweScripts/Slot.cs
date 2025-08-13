using UnityEngine;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    private Item item;

    [SerializeField] private Image image;

    public Item MyItem { get => item; set => item = value; }
}
