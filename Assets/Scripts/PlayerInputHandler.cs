using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    private PlayerInput _playerInput;
    private Vector2 _clickPosition;

    public event Action<Vector2> ClickPerformed;

    private void Awake()
    {
        _playerInput = new PlayerInput();
        _playerInput.Player.ClickOnFigure.performed += OnClickPerformed;
    }

    private void OnEnable() => _playerInput.Enable();

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDisable() => _playerInput.Disable();

    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        _clickPosition = Mouse.current.position.ReadValue();
        ClickPerformed?.Invoke(_clickPosition);
    }
}
