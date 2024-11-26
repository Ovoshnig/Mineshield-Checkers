using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    private PlayerInput _playerInput;
    private bool _isClicked = false;

    public async UniTask GetPlayerInput(List<int> playerIndexes)
    {
        Vector3 hitPoint = Vector3.zero;

        while (hitPoint == Vector3.zero)
        {
            if (_isClicked)
            {
                Vector3 mousePosition = Mouse.current.position.ReadValue();
                Ray ray = Camera.main.ScreenPointToRay(mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    hitPoint = hit.point;

                    (int i, int j) = CoordinateTranslator.Position2Indexes(hitPoint);
                    playerIndexes.Add(i);
                    playerIndexes.Add(j);
                }
            }

            await UniTask.Yield();
        }
    }

    private void Awake()
    {
        _playerInput = new PlayerInput();
        _playerInput.Player.ClickOnFigure.performed += _ => _isClicked = true;
        _playerInput.Player.ClickOnFigure.canceled += _ => _isClicked = false;
    }

    private void OnEnable() => _playerInput.Enable();

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDisable() => _playerInput.Disable();
}
