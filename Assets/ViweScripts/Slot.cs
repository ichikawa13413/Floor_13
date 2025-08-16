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

    //--VContainer関連--
    private Player _player;
    private Camera _camera;

    [Inject]
    public void Construct(Player player, Camera camera)
    {
        _player = player;
        _camera = camera;
    }

    private void Awake()
    {
        highLight = GetComponent<Image>();
        highLight.enabled = false;
        _transform = transform;
        _rectTransform = GetComponent<RectTransform>();
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
        CreateButton();
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

    private void CreateButton()
    {
        dropButton = Instantiate(dropButton, _transform);
        useButton = Instantiate(useButton, _transform);
        
        RectTransform dropButtonRect = dropButton.GetComponent<RectTransform>();
        RectTransform useButtonRect = useButton.GetComponent<RectTransform>();

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(_camera, _transform.position);
        
        dropButtonRect.position = screenPos;
        dropButtonRect.position += dropButtonPosition;
        useButtonRect.position = screenPos;
        useButtonRect.position += useButtonPosition;
    }
}
