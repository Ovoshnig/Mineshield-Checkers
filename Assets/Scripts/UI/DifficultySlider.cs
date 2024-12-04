using System;
using UnityEngine;
using UnityEngine.UI;

public class DifficultySlider : MonoBehaviour
{
    private Slider _slider;

    public event Action<float> ValueChanged;

    private void Awake() => _slider = GetComponent<Slider>();

    private void OnEnable() => _slider.onValueChanged.AddListener(value => ValueChanged?.Invoke(value));

    private void OnDisable() => _slider.onValueChanged.RemoveListener(value => ValueChanged?.Invoke(value));
}
