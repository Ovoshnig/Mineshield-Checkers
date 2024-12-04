using UnityEngine;

public class FigureSelectionModel
{
    private readonly GameObject[] _figurePrefabs;

    public int CurrentIndex { get; set; }

    public static GameObject ChosenFigure { get; set; } = null;
    public static int ChosenDifficulty { get; set; } = 4;

    public FigureSelectionModel(GameObject[] figurePrefabs) => _figurePrefabs = figurePrefabs;

    public bool CanSwap(int direction)
    {
        return (direction < 0 && CurrentIndex > 0) ||
               (direction > 0 && CurrentIndex < _figurePrefabs.Length - 1);
    }

    public GameObject GetCurrentPrefab() => _figurePrefabs[CurrentIndex];
}
