using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using R3;

public class SlotGrid : MonoBehaviour
{
    private Transform _transform;
    private IObjectResolver _container;

    //--Slot�̐���--
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private int maxSlot;//�O���b�g��ɕ\��������ő�X���b�g��
    [SerializeField] private Item[] allItems;

    //--�{�^���n--
    [SerializeField] private Button dropButtonPrefab;
    [SerializeField] private Button useButtonPrefab;
    [SerializeField] private Vector3 dropButtonOffset;
    [SerializeField] private Vector3 useButtonOffset;
    private Button dropButton;
    private Button useButton;
    private Canvas _canvas;
    public Subject<Slot> slotSubject;
    private Player _player;
    

    [Inject]
    public void Construct(IObjectResolver container,Canvas canvas, Player player)
    {
        _container = container;
        _canvas = canvas;
        _player = player;
    }

    private void Awake()
    {
        _transform = transform;
        slotSubject = new Subject<Slot>();
    }

    private void Start()
    {
        CreateSlot();
        CreateButton();

        //�����ꂽ�X���b�g�̏����w�ǂ��āA�����ꂽ�X���b�g�̉��Ƀ{�^���ݒu
        slotSubject.Subscribe(slot => SetButton(slot));

        //�v���C���[���C���x���g���������{�^�����\��
        _player.observableUnit.Subscribe(_ => HideButton());
    }

    //�X���b�g��maxSlot������
    private void CreateSlot()
    {
        for (int i = 0; i < maxSlot; i++)
        {
            GameObject slotObj = _container.Instantiate(slotPrefab, _transform);

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

    private void CreateButton()
    {
        dropButton = Instantiate(dropButtonPrefab, _canvas.transform);
        useButton = Instantiate(useButtonPrefab, _canvas.transform);

        dropButton.gameObject.SetActive(false);
        useButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// �����ꂽ�X���b�g�̉��Ƀ{�^���ݒu
    /// </summary>
    /// <param name="slot">�����ꂽ�X���b�g</param>
    private void SetButton(Slot slot)
    {
        RectTransform dropButtonRect = dropButton.GetComponent<RectTransform>();
        RectTransform useButtonRect = useButton.GetComponent<RectTransform>();

        dropButtonRect.position = slot.transform.position;
        useButtonRect.position = slot.transform.position;

        dropButtonRect.position += dropButtonOffset;
        useButtonRect.position += useButtonOffset;

        dropButton.gameObject.SetActive(true);
        useButton.gameObject.SetActive(true);
    }

    private void HideButton()
    {
        dropButton.gameObject.SetActive(false);
        useButton.gameObject.SetActive(false);
    }
}    
