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
    [SerializeField] private Vector3 ChoiceArrowOffset;
    [SerializeField] private GameObject ChoiceArrowPrefab;
    private GameObject ChoiceArrow;
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
    private int slotsIndex;
    private bool isOpenKeyboard;
    private bool isLockChoice;
    private Subject<Unit> decisionSubject;//�R���g���[���[�ŃX���b�g��I���������ɒʒm
    private enum State//�ǂ̃{�^����I�𒆂��Ǘ�
    {
        dropState,
        useState,
        nullState
    }
    private State currentState;

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
        slotSubject = new Subject<Slot>();
        _gridLayoutGroup = GetComponent<GridLayoutGroup>();
        currentSlectIndex = 0;
        isOpenKeyboard = false;
        isLockChoice = false;
        decisionSubject = new Subject<Unit>();
        currentState = State.nullState;
    }

    private void Start()
    {
        CreateSlot();
        CreateButton();

        //�����ꂽ�X���b�g�̏����w�ǂ��āA�����ꂽ�X���b�g�̉��Ƀ{�^���ݒu
        slotSubject.Subscribe(slot => SetButton(slot));

        //�v���C���[���C���x���g���������{�^�����\��
        _player.closeObservable.Subscribe(_ => HideButton());

        //�v���C���[���L�[�{�[�h�ŊJ������true
        _player.keyboardObservable.Subscribe(_ => isOpenKeyboard = true);

        slots = GetComponentsInChildren<Slot>().ToList();
        slotsIndex = slots.Count - 1;

        CheckConstraint();

        decisionSubject.Subscribe(_ =>
        {
            isLockChoice = true;
            currentState = State.useState;
        });
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

        SetChoiceArrow(useButton.transform.position);
    }

    private void HideButton()
    {
        dropButton.gameObject.SetActive(false);
        useButton.gameObject.SetActive(false);
        ChoiceArrow.gameObject.SetActive(false);

        //���łɃC���x���g�����I�t�ɂ�����bool�����Z�b�g
        isOpenKeyboard = false;
        isLockChoice = false;
        currentState = State.nullState;
    }

    private void OnEnable()
    {
        //�ŏ��̃X���b�g��I����Ԃɂ��Ă���
        if (slots != null && slots.Count > 0 && !isOpenKeyboard)
        {
            SelectSlot(0);
            currentSlectIndex = 0;
        }
    }

    public void OnChoiceUp(InputAction.CallbackContext context)
    {
        //�C���x���g�����L�[�{�[�h�ŊJ���āA�R���g���[���[�̑���ɐ؂�ւ������index0����X�^�[�g����
        if (isOpenKeyboard)
        {
            ResetIndex();
            return;
        }

        //�h���b�v�{�^���I�𒆂ɏ\���L�[��������ꂽ�烆�[�Y�{�^���Ɉړ�����
        if (currentState == State.dropState && isLockChoice)
        {
            SetChoiceArrow(useButton.transform.position);
            currentState = State.useState;
            return;
        }
        else if (currentState == State.useState)//���[�Y�{�^���I�𒆂ɏ�������ꂽ�珈�����I��
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
        if (currentState == State.useState && isLockChoice)
        {
            SetChoiceArrow(dropButton.transform.position);
            currentState = State.dropState;
            return;
        }
        else if (currentState == State.dropState)//�h���b�v�{�^���I�𒆂ɏ�������ꂽ�珈�����I��
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

    private void ResetIndex()
    {
        if (isOpenKeyboard)
        {
            SelectSlot(0);
            currentSlectIndex = 0;
        }
        isOpenKeyboard = false;
    }

    public void OnDecisionButton(InputAction.CallbackContext context)
    {
        if (!this.gameObject.activeSelf) return;
      
        Slot currentSlot = slots[currentSlectIndex];

        SetButton(currentSlot);

        decisionSubject.OnNext(Unit.Default);
    }

    private void SetChoiceArrow(Vector3 ButtonPos)
    {
        ChoiceArrow.SetActive(true);
        ChoiceArrow.transform.position = ButtonPos;
        ChoiceArrow.transform.position += ChoiceArrowOffset;
        currentState = State.useState;
    }

    public void OnQuitChoice(InputAction.CallbackContext context)
    {
        HideButton();
    }
}    