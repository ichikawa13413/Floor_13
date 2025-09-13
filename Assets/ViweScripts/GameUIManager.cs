using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using VContainer;

public class GameUIManager : MonoBehaviour
{
    private Transform _transform;
    private Canvas _canvas;
    private SceneLoadManager _sceneLoadManager;

    //--ゲームオーバーテキスト--
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Vector3 gameOverTextPos;

    //--ゲームオーバー時に表示されるボタン--
    [SerializeField] private Button ContinueButtonPrefab;
    [SerializeField] private Vector3 ContinueButtonPos;
    [SerializeField] private Button QuitButtonPredab;
    [SerializeField] private Vector3 QuitButtonPos;

    //--ゲーム終了時にでる警告画面--
    [SerializeField] private Image cautionPrefab;
    [SerializeField] private Button yesButtonPrefab;
    [SerializeField] private Button noButtonPrefab;
    [SerializeField] private Vector3 yesButtonPos;
    [SerializeField] private Vector3 noButtonPos;
    private Image cautionImage;

    [Inject]
    public void Construct(Canvas canvas, SceneLoadManager sceneLoadManager)
    {
        _canvas = canvas;
        _sceneLoadManager = sceneLoadManager;
    }

    //このクラスでゲームオーバー時に表示されるUIの管理を行う
    private void Awake()
    {
        _transform = transform;
    }

    public void CreateGameOverText()
    {
        TextMeshProUGUI text = Instantiate(gameOverText, _canvas.transform);
        text.GetComponent<RectTransform>().anchoredPosition = gameOverTextPos;
    }

    //コンテニューボタンとクイットボタンを指定座標に生成
    public void CreateContinueButton()
    {
        Button continueButton = Instantiate(ContinueButtonPrefab, _canvas.transform);
        Button quitButton = Instantiate(QuitButtonPredab, _canvas.transform);

        continueButton.onClick.AddListener(_sceneLoadManager.ContinueFunction);
        quitButton.onClick.AddListener(CreateCaution);

        continueButton.GetComponent<RectTransform>().anchoredPosition = ContinueButtonPos;
        quitButton.GetComponent<RectTransform>().anchoredPosition = QuitButtonPos;
    }

    //警告ポップアップを表示する
    private void CreateCaution()
    {
        cautionImage = Instantiate(cautionPrefab, _canvas.transform);
        Button yesButton = Instantiate(yesButtonPrefab, cautionImage.transform);
        Button noButton = Instantiate(noButtonPrefab, cautionImage.transform);

        yesButton.GetComponent<RectTransform>().anchoredPosition = yesButtonPos;
        noButton.GetComponent<RectTransform>().anchoredPosition= noButtonPos;

        yesButton.onClick.AddListener(_sceneLoadManager.QuitFunction);
        noButton.onClick.AddListener(() => Destroy(cautionImage.gameObject));
    }
}