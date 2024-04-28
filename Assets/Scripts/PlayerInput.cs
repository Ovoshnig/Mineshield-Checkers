using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    private RaycastHit hit;

    private const float _cellSize = 2.5f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private (int i, int j) Position2Indexes(Vector3 position)
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

    public async UniTask GetPlayerInput(List<int> playerIndexes)
    {
        Vector3 hitPoint = Vector3.zero;

        while (hitPoint == Vector3.zero)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mousePosition = Input.mousePosition;
                Ray ray = Camera.main.ScreenPointToRay(mousePosition);

                if (Physics.Raycast(ray, out hit))
                {
                    hitPoint = hit.point;

                    var (i, j) = Position2Indexes(hitPoint);
                    playerIndexes.Add(i);
                    playerIndexes.Add(j);
                }
            }

            await UniTask.Yield();
        }
    }
}
