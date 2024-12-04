using TMPro;
using UnityEngine;

public class DifficultyDisplay : MonoBehaviour
{
    [SerializeField] private DifficultySlider _difficultySlider;

    private TMP_Text _text;

    private void Awake() => _text = GetComponent<TMP_Text>();

    private void OnEnable() => _difficultySlider.ValueChanged += value => _text.text = value.ToString();

    private void OnDisable() => _difficultySlider.ValueChanged -= value => _text.text = value.ToString();
}
