using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using VContainer;

public class GameUIManager : MonoBehaviour
{
    private Transform _transform;
    private Canvas _canvas;
    private SceneLoadManager _sceneLoadManager;
    private Player _player;
    private enum gameOverUIState
    {
        mainSelection,    //Continue�EQuitButton��I��
        Caution,   //�x���C���[�W��\����
        hideGameOverUI    //�Q�[���I�[�o�[UI���\�����i�������j  
    }
    gameOverUIState currentGameOverUI;

    //--�Q�[���I�[�o�[�e�L�X�g--
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Vector3 gameOverTextPos;

    //--�Q�[���I�[�o�[���ɕ\�������{�^��--
    [SerializeField] private Button ContinueButtonPrefab;
    [SerializeField] private Vector3 ContinueButtonPos;
    [SerializeField] private Button QuitButtonPredab;
    [SerializeField] private Vector3 QuitButtonPos;
    private Button ContinueButton;
    private Button QuitButton;

    //--�Q�[���I�����ɂł�x�����--
    [SerializeField] private Image cautionPrefab;
    [SerializeField] private Button yesButtonPrefab;
    [SerializeField] private Button noButtonPrefab;
    [SerializeField] private Vector3 yesButtonPos;
    [SerializeField] private Vector3 noButtonPos;
    private Image cautionImage;
    private Button yesButton;
    private Button noButton;

    //--�R���g���[���[�Ń{�^���𑀍삷�鎞�Ɏg���n--
    [SerializeField] private Vector2 choiceArrowOffset;
    [SerializeField] private GameObject ChoiceArrowPrefab;
    private readonly Vector3 CHOICE_ARROW_ROTATION = new Vector3(0, 0, 180);
    private GameObject ChoiceArrow;
    private GameObject lastButton;

    [Inject]
    public void Construct(Canvas canvas, SceneLoadManager sceneLoadManager, Player player)
    {
        _canvas = canvas;
        _sceneLoadManager = sceneLoadManager;
        _player = player;
    }

    //���̃N���X�ŃQ�[���I�[�o�[���ɕ\�������UI�̊Ǘ����s��
    private void Awake()
    {
        _transform = transform;
        currentGameOverUI = gameOverUIState.hideGameOverUI;
    }

    private void Update()
    {
        Debug.Log("<color=red>" + currentGameOverUI + "</color>");
        Debug.Log(EventSystem.current.currentSelectedGameObject);

        if (EventSystem.current.currentSelectedGameObject == null)
        {
            //todo
            //����̖��̓}�E�X�Ń{�^���ȊO���N���b�N��������lastButton����O�̂��̋L�����Ă���i�R���e�j���[�{�^����I�����Ă���ꍇ�A�{�^���ȊO���N���b�N����ƃN�C�b�g�{�^���ɖ߂�
            EventSystem.current.SetSelectedGameObject(lastButton);
        }

        SetChoiceArrow();
    }

    public void CreateGameOverText()
    {
        TextMeshProUGUI text = Instantiate(gameOverText, _canvas.transform);
        text.GetComponent<RectTransform>().anchoredPosition = gameOverTextPos;
        ChangeState(gameOverUIState.mainSelection);
    }

    /// <summary>
    /// �R���e�j���[�{�^���ƃN�C�b�g�{�^�����w����W�ɐ����A�e�{�^���ɋ@�\��ǉ�
    /// ���̌�AChoiceArrow�𐶐��A�����ʒu��QuitButton
    /// </summary>
    public void CreateContinueButton()
    {
        ChangeState(gameOverUIState.mainSelection);

        ContinueButton = Instantiate(ContinueButtonPrefab, _canvas.transform);
        QuitButton = Instantiate(QuitButtonPredab, _canvas.transform);

        ContinueButton.onClick.AddListener(_sceneLoadManager.ContinueFunction);
        QuitButton.onClick.AddListener(CreateCaution);

        ContinueButton.GetComponent<RectTransform>().anchoredPosition = ContinueButtonPos;
        QuitButton.GetComponent<RectTransform>().anchoredPosition = QuitButtonPos;

        CreateChoiceArrow(QuitButton.gameObject);
        Debug.Log("�R���e�j���[�{�^����\��");
    }

    //�x���|�b�v�A�b�v��\������
    private void CreateCaution()
    {
        ChangeState(gameOverUIState.Caution);

        cautionImage = Instantiate(cautionPrefab, _canvas.transform);
        yesButton = Instantiate(yesButtonPrefab, cautionImage.transform);
        noButton = Instantiate(noButtonPrefab, cautionImage.transform);

        yesButton.GetComponent<RectTransform>().anchoredPosition = yesButtonPos;
        noButton.GetComponent<RectTransform>().anchoredPosition= noButtonPos;

        yesButton.onClick.AddListener(_sceneLoadManager.QuitFunction);
        noButton.onClick.AddListener(() => 
        {
            //�x���C���[�W��j�󂵂�EventSystem.current��QuitButton�ɂ���
            Destroy(cautionImage.gameObject); 
            ChangeCurrentButton(QuitButton.gameObject);
        });

        //�x����ʂ�yesButton�������I���ɂ���
        ChangeCurrentButton(yesButton.gameObject);
    }

    private void ChangeState(gameOverUIState wantState)
    {
        currentGameOverUI = wantState;
    }

    private void ChangeCurrentButton(GameObject wantButton)
    {
        EventSystem.current.SetSelectedGameObject(wantButton);
        lastButton = wantButton;
    }

    /// <summary>
    /// ���ݑI�𒆂̃{�^���̉���ChoiceArrow��u��(�܂��������Ă��Ȃ��ꍇ�͐�������j
    /// EventSystem��target��I�𒆂ɂ���A�Ō�ɑI�������{�^���ilastButton�j��target����
    /// </summary>
    /// <param name="target">���ݑI�𒆂̃{�^��</param>
    private void CreateChoiceArrow(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if(ChoiceArrow == null)
        {
            ChoiceArrow = Instantiate(ChoiceArrowPrefab, _canvas.transform);
            ChoiceArrow.GetComponent<RectTransform>().anchoredPosition = target.transform.position;
            ChoiceArrow.GetComponent<RectTransform>().anchoredPosition += choiceArrowOffset;
            ChoiceArrow.transform.localEulerAngles = CHOICE_ARROW_ROTATION;
        }

        ChoiceArrow.GetComponent<RectTransform>().anchoredPosition = target.GetComponent<RectTransform>().anchoredPosition;
        ChoiceArrow.GetComponent<RectTransform>().anchoredPosition += choiceArrowOffset;
        ChangeCurrentButton(target);
    }

    /// <summary>
    /// ���ݑI�𒆂̃{�^���ɃA���[���Z�b�g����
    /// </summary>
    private void SetChoiceArrow()
    {
        //���ݑI�𒆂̃{�^���̉��ɃA���[��ݒu
        if (currentGameOverUI == gameOverUIState.mainSelection || currentGameOverUI == gameOverUIState.Caution)
        {
            GameObject currentGameObject = EventSystem.current.currentSelectedGameObject;
            ChoiceArrow.GetComponent<RectTransform>().anchoredPosition =
                currentGameObject.GetComponent<RectTransform>().anchoredPosition + choiceArrowOffset;
        }
    }
}