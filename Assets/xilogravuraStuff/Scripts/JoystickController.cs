using UnityEngine;
using UnityEngine.UI;

public class JoystickController : MonoBehaviour
{
    [SerializeField] private AutoConnectJoystick joystick;
    [SerializeField] private Slider sliderJoystick;
    private float smoothSliderValue;
    private float factor = 0.1f;

    void Start()
    {
        
    }

    void Update(){
        float inputValue = joystick.getXAxis();
        if (Mathf.Abs(inputValue) < 0.05f) inputValue = 0;

        smoothSliderValue = Mathf.Lerp(smoothSliderValue, inputValue, factor);
        sliderJoystick.value = smoothSliderValue;
    }

    public float getValue() => smoothSliderValue;
}
