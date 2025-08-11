using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class StaminaSlider : MonoBehaviour
{
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
            Debug.Log("player‚ªnull‚Å‚·");
        }
        
    }

    private void Update()
    {
        SetStmina();
    }

    private void SetStmina()
    {
        if(_player != null)
        {
            _slider.value = _player.stamina;
        }
    }
}
