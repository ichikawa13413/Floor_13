using NUnit.Framework.Constraints;
using UnityEngine;

public class SlotGrid : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private int slotNumber;//�O���b�g��ɕ\��������X���b�g��

    private Transform _transform;

    

    private void Awake()
    {
        _transform = transform;
    }

    private void Start()
    {
        for (int i = 0; i < slotNumber; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, _transform);

            Slot slot = slotObj.GetComponent<Slot>();

            /*
            if (i < allItems.Length)
            {
                slot.SetItem(allItems[i]);
            }
            else
            {
                slot.SetItem(null);
            }*/
        }
    }
}    
