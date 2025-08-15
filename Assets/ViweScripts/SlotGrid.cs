using UnityEngine;
using UnityEngine.InputSystem;

public class SlotGrid : MonoBehaviour
{
    private Transform _transform;

    //--Slot�̐���--
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private int maxSlot;//�O���b�g��ɕ\��������ő�X���b�g��
    [SerializeField] private Item[] allItems;

    //--UI����n--


    private void Awake()
    {
        _transform = transform;
    }

    private void Start()
    {
        for (int i = 0; i < maxSlot; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, _transform);

            Slot slot = slotObj.GetComponent<Slot>();

            if (i < allItems.Length)
            {
                slot.SetItem(allItems[i]);
            }
            else
            {
                slot.SetItem(null);
            }
        }

        gameObject.SetActive(false);
    }
}    
