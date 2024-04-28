using UnityEngine;

public static class CoordinateTranslator
{
    private const float _cellSize = 2.5f;

    public static (int i, int j) Position2Indexes(Vector3 position)
    {
        var (iFloat, jFloat) = (position[0], position[2]);

        iFloat /= _cellSize;
        jFloat /= _cellSize;

        iFloat += 4f;
        jFloat += 4f;

        int i = Mathf.RoundToInt(iFloat - 0.5f);
        int j = Mathf.RoundToInt(jFloat - 0.5f);

        return (i, j);
    }

    public static Vector3 Indexes2Position(int i, int j)
    {
        float iFloat = i + 0.5f;
        float jFloat = j + 0.5f;

        iFloat -= 4f;
        jFloat -= 4f;

        iFloat *= _cellSize;
        jFloat *= _cellSize;

        Vector3 position = new(iFloat, 0f, jFloat);
        return position;
    }
}
