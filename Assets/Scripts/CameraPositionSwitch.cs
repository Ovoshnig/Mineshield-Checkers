using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraPositionSwitch : MonoBehaviour
{
    [SerializeField] private Transform _cameraPointsParent;
    [SerializeField] private List<Transform> _cameraPoints;
    [SerializeField, Min(0f)] private int _currentIndex = 0;

    private PlayerInput _playerInput;
    private Vector2 _direction;

    private void OnValidate()
    {
        if (_currentIndex > _cameraPoints.Count - 1)
            _currentIndex = 0;

        UpdateCameraPoints();
    }

    private void Awake()
    {
        _playerInput = new();
        _playerInput.CameraSwitch.SwitchCamera.performed += OnSwitchCameraPerformed;
    }

    private void OnEnable() => _playerInput.Enable();

    private void Start()
    {
        UpdateCameraPoints();
        MoveToCurrentPoint();
    }

    private void OnDisable() => _playerInput.Disable();

    [ContextMenu("Update camera points")]
    private void UpdateCameraPoints()
    {
        _cameraPoints.Clear();

        foreach (Transform point in _cameraPointsParent)
            _cameraPoints.Add(point);
    }

    [ContextMenu("Go to current point")]
    private void MoveToCurrentPoint()
    {
        Transform pointTransform = _cameraPoints[_currentIndex];

        if (transform.parent != pointTransform)
        {
            transform.SetParent(pointTransform);
            transform.SetPositionAndRotation(pointTransform.position, pointTransform.rotation);
        }
    }

    [ContextMenu("Go to next point")]
    private void GoToNextPoint()
    {
        if (_currentIndex < _cameraPoints.Count - 1)
            _currentIndex++;
        else
            _currentIndex = 0;

        MoveToCurrentPoint();
    }

    [ContextMenu("Go to previous point")]
    private void GoToPreviousPoint()
    {
        if (_currentIndex > 0)
            _currentIndex--;
        else
            _currentIndex = _cameraPoints.Count - 1;

        MoveToCurrentPoint();
    }

    private void OnSwitchCameraPerformed(InputAction.CallbackContext context)
    {
        _direction = context.action.ReadValue<Vector2>();

        if (_direction.x > 0)
            GoToNextPoint();
        else if (_direction.x < 0)
            GoToPreviousPoint();
    }
}
