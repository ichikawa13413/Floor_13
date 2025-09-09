using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;
using R3;

public class Slot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Transform _transform;

    //--�X���b�g�֘A--
    [SerializeField] private Image itemImage;
    private Item item;
    private Image highLight;
    public Item MyItem { get => item; private set => item = value; }

    //--VContainer�֘A--
    private Player _player;
    private SlotGrid _slotGrid;

    //--�A�C�e���h���b�v�֘A--
    [SerializeField] private int yOffset;
    [SerializeField] private int zOffset;

    [Inject]
    public void Construct(Player player,SlotGrid slotGrid)
    {
        _player = player;
        _slotGrid = slotGrid;
    }

    private void Awake()
    {
        highLight = GetComponent<Image>();
        highLight.enabled = false;
        _transform = transform;
    }

    private void Start()
    {
        //�v���C���[���C���x���g�����N���[�Y�������Ƀn�C���C�g������
        _player.closeObservable.Subscribe(_ => HideHighLight());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowHighLight();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideHighLight();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _slotGrid.slotOnNext.OnNext(this);
    }

    public void SetItem(Item item)
    {
        MyItem = item;

        if (MyItem != null)
        {
            itemImage.sprite = item.MyItemImage;
            itemImage.color = Color.white;//���̂��A�b�v������or�X���b�g0�Ԃ����ǂݍ��܂Ȃ��H
        }
        else
        {
            itemImage.color = Color.clear;
        }
    }

    public void ShowHighLight()
    {
        highLight.enabled = true;
    }

    public void HideHighLight()
    {
        highLight.enabled = false;
    }

    public void DropItem()
    {
        //�v���C���[�̑O�ɊY������A�C�e���𐶐�
        if (MyItem != null)
        {
            Vector3 playerPos = _player.transform.position;
            Vector3 PlayerForward = _player.transform.forward;
            Vector3 spawnPos = playerPos + (PlayerForward * zOffset) + (Vector3.up * yOffset);
            Quaternion playerRotation = _player.transform.rotation;
            GameObject itemObject = Instantiate(MyItem.MyItemObject, spawnPos, playerRotation);

            _player.DropItem(MyItem);//�h���b�v����A�C�e�����v���C���[�ɒʒm
            MyItem = null;
            itemImage.color = Color.clear;
        }
        else
        {
            Debug.Log("�A�C�e�����h���b�v�o���܂���");
        }
    }

    public void UseItem()
    {
        if (MyItem != null)
        {
            MyItem = null;
            itemImage.color = Color.clear;
        }
        else
        {
            Debug.Log("�A�C�e�����g�p�o���܂���");
        }
    }
}
