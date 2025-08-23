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
        if(fill.color.a > 0 || backGround.color.a > 0)
        {
            Color backGroundColor = fill.color;
            Color fillColor = backGround.color;
            Color handleColor = handle.color;

            backGroundColor.a -= minusRate * Time.deltaTime;
            fillColor.a -= minusRate * Time.deltaTime;
            handleColor.a -= minusRate * Time.deltaTime;

            backGroundColor.a = Mathf.Clamp(backGroundColor.a, 0, 1);
            fillColor.a = Mathf.Clamp(fillColor.a, 0, 1);
            handleColor.a = Mathf.Clamp(handleColor.a, 0, 1);

            fill.color = backGroundColor;
            backGround.color = fillColor;
            handle.color = handleColor;
        }
    }
}
