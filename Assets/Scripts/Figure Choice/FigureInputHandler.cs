using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using System;

public class FigureInputHandler
{
    private readonly PlayerInput _playerInput;
    private readonly Func<UniTask> _onSwapLeft;
    private readonly Func<UniTask> _onSwapRight;
    private readonly Action<InputAction.CallbackContext> _onFigureChosen;

    public FigureInputHandler(Func<UniTask> onSwapLeft, Func<UniTask> onSwapRight, Action<InputAction.CallbackContext> onFigureChosen)
    {
        _playerInput = new PlayerInput();
        _onSwapLeft = onSwapLeft;
        _onSwapRight = onSwapRight;
        _onFigureChosen = onFigureChosen;

        _playerInput.FigureChoice.SwapLeft.performed += async ctx => await _onSwapLeft();
        _playerInput.FigureChoice.SwapRight.performed += async ctx => await _onSwapRight();
        _playerInput.FigureChoice.Choose.performed += _onFigureChosen;
    }

    public void Enable() => _playerInput.Enable();

    public void Disable() => _playerInput.Disable();
}
