using UnityEngine;
using UnityEngine.UI;
using VContainer;
using R3;

public class StaminaSlider : MonoBehaviour
{
    [SerializeField] private Image backGround;
    [SerializeField] private Image fill;
    [SerializeField] private Image handle;
    [SerializeField] private float minusRate;
    private Slider _slider;
    private const int MAXIMUM_ALPHA = 1;
    private const int MINIMUM_ALPHA = 0;

    private Player _player;

    [Inject]
    public void Construct(Player player)
    {
        _player = player;
    }

    private void Awake()
    {
        _slider = GetComponent<Slider>();
    }

    private void Start()
    {
        if(_player != null)
        {
            _slider.maxValue = _player.maxStamina;
        }
        else
        {
            Debug.Log("playerがnullです");
        }
        
        _player.maxStaminaObservable.Subscribe(_ => HideStaminaSlider());
        _player.consumeObservable.Subscribe(_ => ShowStaminaSlider());
    }

    private void Update()
    {
        SetStamina();
    }

    private void SetStamina()
    {
        if(_player != null)
        {
            _slider.value = _player.stamina;
        }
    }

    //スタミナが満タンになったらスタミナスライダーゆっくりと消す
    private void HideStaminaSlider()
    {
        if(fill.color.a > MINIMUM_ALPHA || backGround.color.a > MINIMUM_ALPHA || handle.color.a > MINIMUM_ALPHA)
        {
            Color backGroundColor = fill.color;
            Color fillColor = backGround.color;
            Color handleColor = handle.color;

            backGroundColor.a -= minusRate * Time.deltaTime;
            fillColor.a -= minusRate * Time.deltaTime;
            handleColor.a -= minusRate * Time.deltaTime;

            backGroundColor.a = Mathf.Clamp(backGroundColor.a, MINIMUM_ALPHA, MAXIMUM_ALPHA);
            fillColor.a = Mathf.Clamp(fillColor.a, MINIMUM_ALPHA, MAXIMUM_ALPHA);
            handleColor.a = Mathf.Clamp(handleColor.a, MINIMUM_ALPHA, MAXIMUM_ALPHA);

            fill.color = backGroundColor;
            backGround.color = fillColor;
            handle.color = handleColor;
        }
    }

    //スタミナ消費中はスライダーを表示する
    private void ShowStaminaSlider()
    {
        if (fill.color.a < MAXIMUM_ALPHA || backGround.color.a < MAXIMUM_ALPHA || handle.color.a < MAXIMUM_ALPHA)
        {
            Color backGroundColor = fill.color;
            Color fillColor = backGround.color;
            Color handleColor = handle.color;

            backGroundColor.a = MAXIMUM_ALPHA;
            fillColor.a = MAXIMUM_ALPHA;
            handleColor.a = MAXIMUM_ALPHA;

            fill.color = backGroundColor;
            backGround.color = fillColor;
            handle.color = handleColor;
        }
    }
}
