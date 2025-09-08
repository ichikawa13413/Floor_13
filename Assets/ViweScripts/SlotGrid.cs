using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Windows;
using VContainer;
using VContainer.Unity;

public class SlotGrid : MonoBehaviour
{
    private Transform _transform;
    private IObjectResolver _container;

    //--Slot�̐���--
    [SerializeField] private GameObject slotPrefab;
    private int maxSlot;//�O���b�g��ɕ\��������ő�X���b�g��
    [SerializeField] private Item[] gettedItems;

    //--�{�^���n--
    [SerializeField] private Button dropButtonPrefab;
    [SerializeField] private Button useButtonPrefab;
    [SerializeField] private Vector3 dropButtonOffset;
    [SerializeField] private Vector3 useButtonOffset;
    [SerializeField] private Vector3 ChoiceArrowOffset;
    [SerializeField] private GameObject ChoiceArrowPrefab;
    private GameObject ChoiceArrow;
    private Button dropButton;
    private Button useButton;
    private Canvas _canvas;
    private Subject<Slot> slotMouseClickSubject;
    public ISubject<Slot> slotOnNext => slotMouseClickSubject;
    private bool isMouseClick;
    private Player _player;

    //--Slot���R���g���[���[�ő��삷�邽�߂̕�--
    private List<Slot> slots;
    private GridLayoutGroup _gridLayoutGroup;
    private int constraint;
    private int currentSlectIndex;
    private int slotsIndex;
    private bool isOpenKeyboard;
    private PlayerInput playerInput;

    //--�R���g���[���[�Ń{�^���𑀍삷��n--
    private bool isLockChoice;
    private Subject<Unit> decisionSubject;//�R���g���[���[�ŃX���b�g��I���������ɒʒm
    private enum arrowState//�ǂ̃{�^����I�𒆂��Ǘ�
    {
        dropState,
        useState,
        nullArrowState
    }
    private arrowState currentArrowState;
    private enum buttonState
    {
        dropDecisionState,
        useDecisionState,
        nullButtonState
    }
    private buttonState currentButtonState;

    //--�A�C�e�����E�������Ɏg������--
    private const int FIRST_INDEX = 0;
    private const int EMPTY_SLOT_COUNT = 0;

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

        //--�X���b�g�n--
        slotMouseClickSubject = new Subject<Slot>();
        _gridLayoutGroup = GetComponent<GridLayoutGroup>();
        isMouseClick = false;

        //--�R���g���[���[�Ń{�^���𑀍삷��n--
        currentSlectIndex = 0;
        isOpenKeyboard = false;

        //--�R���g���[���[�Ń{�^���𑀍삷��n--
        isLockChoice = false;
        decisionSubject = new Subject<Unit>();
        currentArrowState = arrowState.nullArrowState;
        currentButtonState = buttonState.nullButtonState;
    }

    private void Start()
    {
        if (_player == null) return;

        maxSlot = _player.playerLimitItem;

        CreateSlot();
        CreateButton();

        //�}�E�X�ŃN���b�N���ꂽ�X���b�g�̏����w�ǂ��āA�����ꂽ�X���b�g�̉��Ƀ{�^���ݒu
        slotMouseClickSubject.Subscribe(slot =>
        {
            isMouseClick = true;
            SetButton(slot);
        });

        //�v���C���[���C���x���g���������{�^�����\��
        _player.closeObservable.Subscribe(_ => HideButton());

        //�v���C���[���L�[�{�[�h�ŊJ������true
        _player.keyboardObservable.Subscribe(_ => isOpenKeyboard = true);

        slots = GetComponentsInChildren<Slot>().ToList();
        slotsIndex = slots.Count - 1;

        CheckConstraint();

        //����{�^���������ꂽ�瑀������b�N���āA�A���[��useButton�ɐݒ�
        decisionSubject.Subscribe(_ =>
        {
            isLockChoice = true;
        });

        playerInput = _player.MyInput;

        //�v���C���[���A�C�e�����E������SetItem�N��
        _player.getItemObservable.Subscribe(item => SetItem(item));
    }

    private void Update()
    {
        Debug.Log(currentButtonState);
        Debug.Log(currentSlectIndex);
    }

    private void OnEnable()
    {
        //�ŏ��̃X���b�g��I����Ԃɂ��Ă���
        if (slots != null && slots.Count > 0 && !isOpenKeyboard)
        {
            SelectSlot(0);
            currentSlectIndex = 0;
        }

        playerInput.actions["ChoiceUp"].started += OnChoiceUp;
        playerInput.actions["ChoiceDown"].started += OnChoiceDown;
        playerInput.actions["ChoiceLeft"].started += OnChoiceLeft;
        playerInput.actions["ChoiceRight"].started += OnChoiceRight;
        playerInput.actions["DecisionButton"].started += OnDecisionButton;
        playerInput.actions["QuitButton"].started += OnQuitChoice;
    }

    private void OnDisable()
    {
        playerInput.actions["ChoiceUp"].started -= OnChoiceUp;
        playerInput.actions["ChoiceDown"].started -= OnChoiceDown;
        playerInput.actions["ChoiceLeft"].started -= OnChoiceLeft;
        playerInput.actions["ChoiceRight"].started -= OnChoiceRight;
        playerInput.actions["DecisionButton"].started -= OnDecisionButton;
        playerInput.actions["QuitButton"].started -= OnQuitChoice;
    }

    ///-----<�X���b�g�n���\�b�h>-----

    //�X���b�g��maxSlot������
    private void CreateSlot()
    {
        for (int i = 0; i < maxSlot; i++)
        {
            GameObject slotObj = _container.Instantiate(slotPrefab, _transform);

            Slot slot = slotObj.GetComponent<Slot>();

            if (i < gettedItems.Length)
            {
                slot.SetItem(gettedItems[i]);
            }
            else
            {
                slot.SetItem(null);
            }
        }

        gameObject.SetActive(false);
    }

    //gridLayoutGroup��Constraint��FixedColumnCount�ɐݒ肳��Ă��邩�m�F�A���̌�constraint�̒l����
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
    ///------------------------------

    ///-----<�{�^���n���\�b�h>-----
    private void CreateButton()
    {
        dropButton = Instantiate(dropButtonPrefab, _canvas.transform);
        useButton = Instantiate(useButtonPrefab, _canvas.transform);
        ChoiceArrow = Instantiate(ChoiceArrowPrefab, _canvas.transform);

        dropButton.gameObject.SetActive(false);
        useButton.gameObject.SetActive(false);
        ChoiceArrow.gameObject.SetActive(false);
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

        if (!isMouseClick)
        {
            SetChoiceArrow(useButton.transform.position);
        }
    }

    private void HideButton()
    {
        dropButton.gameObject.SetActive(false);
        useButton.gameObject.SetActive(false);
        ChoiceArrow.gameObject.SetActive(false);

        //���łɃC���x���g�����I�t�ɂ�����bool�����Z�b�g
        isOpenKeyboard = false;
        isLockChoice = false;
        isMouseClick = false;
        currentArrowState = arrowState.nullArrowState;
        currentButtonState = buttonState.nullButtonState;
    }


    ///-----<�R���g���[���[�ŃC���x���g���𑀍삷��n>-----
    public void OnChoiceUp(InputAction.CallbackContext context)
    {
        //�C���x���g�����L�[�{�[�h�ŊJ���āA�R���g���[���[�̑���ɐ؂�ւ������index0����X�^�[�g����
        if (isOpenKeyboard)
        {
            ResetIndex();
            return;
        }

        //�h���b�v�{�^���I�𒆂ɏ\���L�[��������ꂽ�烆�[�Y�{�^���Ɉړ�����
        if (currentArrowState == arrowState.dropState && isLockChoice)
        {
            SetChoiceArrow(useButton.transform.position);
            currentArrowState = arrowState.useState;
            currentButtonState = buttonState.useDecisionState;
            return;
        }
        else if (currentArrowState == arrowState.useState)//���[�Y�{�^���I�𒆂ɏ�������ꂽ�珈�����I��
        {
            return;
        }

        int index = currentSlectIndex;

        //��ԏ�̒i�̃X���b�g��������ړ������Ȃ�
        if (index >= constraint)
        {
            index -= constraint;
        }

        //gridLayoutGroup�̘g�O����o�Ă��Ȃ����m�F
        index = Mathf.Clamp(index, 0, slotsIndex);

        if (index != currentSlectIndex)
        {
            currentSlectIndex = index;
            SelectSlot(currentSlectIndex);
        }
    }

    public void OnChoiceDown(InputAction.CallbackContext context)
    {
        //�C���x���g�����L�[�{�[�h�ŊJ���āA�R���g���[���[�̑���ɐ؂�ւ������index�����Z�b�g����
        if (isOpenKeyboard)
        {
            ResetIndex();
            return;
        }

        //���[�Y�{�^���I�𒆂ɏ\���L�[��������ꂽ��h���b�v�{�^���Ɉړ�����
        if (currentArrowState == arrowState.useState && isLockChoice)
        {
            SetChoiceArrow(dropButton.transform.position);
            currentArrowState = arrowState.dropState;
            currentButtonState = buttonState.dropDecisionState;
            return;
        }
        else if (currentArrowState == arrowState.dropState)//�h���b�v�{�^���I�𒆂ɏ�������ꂽ�珈�����I��
        {
            return;
        }

        int index = currentSlectIndex;

        //�X���b�g�̈�ԉ��̒i�̈ړ������p
        int underLimit = slotsIndex - constraint;

        //��ԉ��̒i�̃X���b�g��������ړ������Ȃ�
        if (index <= underLimit)
        {
            index += constraint;
        }

        //gridLayoutGroup�̘g�O����o�Ă��Ȃ����m�F
        index = Mathf.Clamp(index, 0, slots.Count - 1);

        if (index != currentSlectIndex)
        {
            currentSlectIndex = index;
            SelectSlot(currentSlectIndex);
        }
    }

    public void OnChoiceLeft(InputAction.CallbackContext context)
    {
        if (isLockChoice) return;

        //�C���x���g�����L�[�{�[�h�ŊJ���āA�R���g���[���[�̑���ɐ؂�ւ������index�����Z�b�g����
        if (isOpenKeyboard)
        {
            ResetIndex();
            return;
        }

        int index = currentSlectIndex;
        index--;

        //gridLayoutGroup�̘g�O����o�Ă��Ȃ����m�F
        index = Mathf.Clamp(index, 0, slots.Count - 1);

        if (index != currentSlectIndex)
        {
            currentSlectIndex = index;
            SelectSlot(currentSlectIndex);
        }
    }

    public void OnChoiceRight(InputAction.CallbackContext context)
    {
        if (isLockChoice) return;

        //�C���x���g�����L�[�{�[�h�ŊJ���āA�R���g���[���[�̑���ɐ؂�ւ������index�����Z�b�g����
        if (isOpenKeyboard)
        {
            ResetIndex();
            return;
        }

        int index = currentSlectIndex;
        index++;

        //gridLayoutGroup�̘g�O����o�Ă��Ȃ����m�F
        index = Mathf.Clamp(index, 0, slots.Count - 1);

        if (index != currentSlectIndex)
        {
            currentSlectIndex = index;
            SelectSlot(currentSlectIndex);
        }
    }

    private void SelectSlot(int index)
    {
        foreach(Slot slot in slots)
        {
            slot.HideHighLight();
        }
        slots[index].ShowHighLight();
    }

    private void ResetIndex()
    {
        if (isOpenKeyboard)
        {
            SelectSlot(0);
            currentSlectIndex = 0;
        }
        isOpenKeyboard = false;
    }

    ///------------------------------

    ///-----<�R���g���[���[�Ń{�^���𑀍삷��n>-----

    public void OnDecisionButton(InputAction.CallbackContext context)
    {
        if (!this.gameObject.activeSelf) return;
      
        Slot currentSlot = slots[currentSlectIndex];
        SetButton(currentSlot);

        //�I�������X���b�g�Ŏg���{�^���������A�{�^�����������̂�
        if (isLockChoice && currentButtonState != buttonState.nullButtonState)
        {
            switch (currentButtonState)
            {
                case buttonState.useDecisionState:
                    currentSlot.UseItem();
                    Debug.Log("UseItem");
                    break;
                case buttonState.dropDecisionState:
                    currentSlot.DropItem();
                    Debug.Log("DropItem");
                    break;
            }
            currentButtonState = buttonState.nullButtonState;
            HideButton();
            return;
        }

        decisionSubject.OnNext(Unit.Default);

        currentArrowState = arrowState.useState;
        currentButtonState = buttonState.useDecisionState;
    }

    /// <summary>
    /// �I�𒆂̃{�^���̉E�ɃA�C�R����u��
    /// </summary>
    /// <param name="ButtonPos">�I�𒆂̃{�^���̍��W</param>
    private void SetChoiceArrow(Vector3 ButtonPos)
    {
        ChoiceArrow.SetActive(true);
        ChoiceArrow.transform.position = ButtonPos;
        ChoiceArrow.transform.position += ChoiceArrowOffset;
        currentArrowState = arrowState.useState;
    }

    public void OnQuitChoice(InputAction.CallbackContext context)
    {
        HideButton();
    }

    ///------------------------------

    ///-----<�A�C�e�����E�������Ȃ�>-----
    
    public void SetItem(Item item)
    {
        if (item == null) return;

        //slots��MyItem��null�̃C���f�b�N�X���܂Ƃ߂����X�g���쐬
        List<int> nullIndex = slots
             .Select((slot, index) => new { Slot = slot, Index = index })
             .Where(x => x.Slot.MyItem == null)
             .Select(x => x.Index)
             .OrderBy(x => x)
             .ToList();

        //���ォ�珇��item�����Ă���
        int currentIndex = nullIndex[FIRST_INDEX];
        Slot currentSlot = slots[currentIndex];
        currentSlot.SetItem(item);
    }

    public bool CanGetItem()
    {
        int nullSlotCount = slots.Where(slot => slot.MyItem == null).ToList().Count;
        return nullSlotCount > EMPTY_SLOT_COUNT;
    }
}