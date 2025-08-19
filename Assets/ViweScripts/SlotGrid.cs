using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using R3;
using System.Collections.Generic;
using System.Linq;

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

    //--Slot���R���g���[���[�ő��삷�邽�߂̕�--
    private List<Slot> slots;
    private GridLayoutGroup _gridLayoutGroup;
    private int constraint;
    private int currentSlectIndex;
    private Slot currentSelectSlot;
    [SerializeField] private PlayerInput playerInput;

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
        _gridLayoutGroup = GetComponent<GridLayoutGroup>();
        currentSlectIndex = 0;
    }

    private void Start()
    {
        CreateSlot();
        CreateButton();

        //�����ꂽ�X���b�g�̏����w�ǂ��āA�����ꂽ�X���b�g�̉��Ƀ{�^���ݒu
        slotSubject.Subscribe(slot => SetButton(slot));

        //�v���C���[���C���x���g���������{�^�����\��
        _player.observableUnit.Subscribe(_ => HideButton());

        slots = GetComponentsInChildren<Slot>().ToList();
        Debug.Log(slots.Count);
        CheckConstraint();
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
    /// �����ꂽ�X���b�g���Ɋ�Â��āA�{�^���̃N���b�N�����ƃ{�^���̈ʒu��ݒ�B
    /// </summary>
    /// <param name="slot">�N���b�N���ꂽ�X���b�g</param>
    private void SetButton(Slot slot)
    {
        dropButton.onClick.RemoveAllListeners();
        dropButton.onClick.RemoveAllListeners();

        dropButton.onClick.AddListener(() => slot.DropItem());
        useButton.onClick.AddListener(() => slot.UseItem());

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

    private void OnEnable()
    {
        playerInput.actions["ChoiceUp"].started += OnChoiceUp;
        playerInput.actions["ChoiceDown"].started += OnChoiceDown;
        playerInput.actions["ChoiceLeft"].started += OnChoiceLeft;
        playerInput.actions["ChoiceRight"].started += OnChoiceRight;
        //�ŏ��̃X���b�g��I����Ԃɂ��Ă���
        if (slots != null && slots.Count > 0)
        {
            //SelectSlot(0);
            currentSlectIndex = 0;
        }
    }

    private void OnDisable()
    {
        playerInput.actions["ChoiceUp"].started -= OnChoiceUp;
        playerInput.actions["ChoiceDown"].started -= OnChoiceDown;
        playerInput.actions["ChoiceLeft"].started -= OnChoiceLeft;
        playerInput.actions["ChoiceRight"].started -= OnChoiceRight;
    }

    public void OnChoiceUp(InputAction.CallbackContext context)
    {
        int index = currentSlectIndex;

        currentSlectIndex -= constraint;

        currentSlectIndex = Mathf.Clamp(currentSlectIndex, 0, slots.Count -1);
        if(index != currentSlectIndex)
        {
            SelectSlot(currentSlectIndex);
        }
        Debug.Log("Up" + currentSlectIndex);
    }
    public void OnChoiceDown(InputAction.CallbackContext context)
    {
        int index = currentSlectIndex;

        currentSlectIndex += constraint;

        currentSlectIndex = Mathf.Clamp(currentSlectIndex, 0, slots.Count - 1);
        if (index != currentSlectIndex)
        {
            SelectSlot(currentSlectIndex);
        }
        Debug.Log("Down" + currentSlectIndex);
    }
    public void OnChoiceLeft(InputAction.CallbackContext context)
    {
        int index = currentSlectIndex;

        currentSlectIndex--;

        currentSlectIndex = Mathf.Clamp(currentSlectIndex, 0, slots.Count - 1);
        if (index != currentSlectIndex)
        {
            SelectSlot(currentSlectIndex);
        }
        Debug.Log("left" + currentSlectIndex);
    }
    public void OnChoiceRight(InputAction.CallbackContext context)
    {
        int index = currentSlectIndex;

        currentSlectIndex++;

        currentSlectIndex = Mathf.Clamp(currentSlectIndex, 0, slots.Count - 1);
        if (index != currentSlectIndex)
        {
            SelectSlot(currentSlectIndex);
        }
        Debug.Log("Right" + currentSlectIndex);
    }

    private void SelectSlot(int index)
    {
        foreach(Slot slot in slots)
        {
            slot.HideHighLight();
        }
        slots[index].ShowHighLight();
    }

    private void CheckConstraint()
    {
        if (_gridLayoutGroup.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
        {
            constraint = _gridLayoutGroup.constraintCount;
        }
        else
        {
            Debug.LogError("GridLayoutGroup��Constraint��Fixed Column Count�ɐݒ肵�Ă��������B");
            //�Ƃ肠�����l��ݒ肵�Ă���
            constraint = 4;
        }
    }

    public void OnNavigate(InputAction.CallbackContext context)
    {
        switch (context.control.path)
        {
            case "dpad/up":
                SelectSlot(1);
                break;
            case "dpad/down":
                SelectSlot(2);
                break;
            case "dpad/left":
                SelectSlot(3);
                break;
            case "dpad/right":
                SelectSlot(4);
                break;
            default:
                Debug.Log("���s����Ă��܂���");
                break;
        }
        Debug.Log(context.control.path);
    }
}    
