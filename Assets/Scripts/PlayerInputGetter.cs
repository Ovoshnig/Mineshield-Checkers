using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputGetter : MonoBehaviour
{
    private bool _isClicked;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnMouseClick() => _isClicked = true;
    
    public async UniTask GetPlayerInput(List<int> playerIndexes)
    {
        Vector3 hitPoint = Vector3.zero;

        while (hitPoint == Vector3.zero)
        {
            if (_isClicked)
            {
                _isClicked = false;

                Vector3 mousePosition = Mouse.current.position.ReadValue();
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
