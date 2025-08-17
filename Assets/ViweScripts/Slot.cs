using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;
using R3;

public class Slot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Transform _transform;
    private RectTransform _rectTransform;

    //--スロット関連--
    [SerializeField] private Image itemImage;
    private Item item;
    private Image highLight;
    public Item MyItem { get => item; private set => item = value; }

    //--ボタン関連--
    [SerializeField] private Button dropButton;
    [SerializeField] private Button useButton;
    [SerializeField] private Vector3 dropButtonPosition;
    [SerializeField] private Vector3 useButtonPosition;
    private Subject<Slot> slotSubject;
    public Observable<Slot> slotObservable => slotSubject;

    //--VContainer関連--
    private Player _player;
    private SlotGrid _slotGrid;

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
        _rectTransform = GetComponent<RectTransform>();
        slotSubject = new Subject<Slot>();
    }

    private void Start()
    {
        //プレイヤーがインベントリをクローズした時にハイライトを消す
        _player.observableUnit.Subscribe(_ => HideHighLight());
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
        _slotGrid.slotSubject.OnNext(this);
    }

    public void SetItem(Item item)
    {
        MyItem = item;

        if (MyItem != null)
        {
            itemImage.sprite = item.MyItemImage;
        }
        else
        {
            itemImage.color = new Color(0, 0, 0, 0);
        }
    }

    private void ShowHighLight()
    {
        highLight.enabled = true;
    }

    private void HideHighLight()
    {
        highLight.enabled = false;
    }
}
