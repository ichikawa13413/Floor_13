using TMPro;
using Unity.Mathematics;
using UnityEngine;
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
    [SerializeField] private Image ContinueButtonPrefab;
    [SerializeField] private Vector3 ContinueButtonPos;
    [SerializeField] private Image QuitButtonPredab;
    [SerializeField] private Vector3 QuitButtonPos;

    //--slodgridクラスを-

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
        var text = Instantiate(gameOverText, _transform);
    }

    //コンテニューボタンとクイットボタンを指定座標に生成
    public void CreateContinueButton()
    {
        var continueButton = Instantiate(ContinueButtonPrefab, _canvas.transform);
        var quitButton = Instantiate(QuitButtonPredab, _canvas.transform);

        Button button1 = continueButton.gameObject.GetComponentInChildren<Button>(false);
        Button button2 = quitButton.gameObject.GetComponentInChildren<Button>(false);

        button1.onClick.AddListener(_sceneLoadManager.ContinueFunction);
        button2.onClick.AddListener(_sceneLoadManager.QuitFunction);

        continueButton.GetComponent<RectTransform>().anchoredPosition = ContinueButtonPos;
        quitButton.GetComponent<RectTransform>().anchoredPosition = QuitButtonPos;
    }
}