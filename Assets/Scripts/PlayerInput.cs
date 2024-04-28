using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    hitPoint = hit.point;

                    var (i, j) = CoordinateTranslator.Position2Indexes(hitPoint);
                    playerIndexes.Add(i);
                    playerIndexes.Add(j);
                }
            }

            await UniTask.Yield();
        }
    }
}
