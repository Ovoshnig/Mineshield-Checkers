using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderValueDisplay : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private TMP_Text _text;

    private void OnEnable() => _slider.onValueChanged.AddListener(value => _text.text = value.ToString());

    private void OnDisable() => _slider.onValueChanged.RemoveListener(value => _text.text = value.ToString());
}
