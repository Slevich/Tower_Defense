using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class ObjectRotator : MonoBehaviour, IRotator
{
    #region Fields
    [Header("References.")]
    [SerializeField]
    private Transform _rotationObject;
    [Header("Settings.")]
    [SerializeField, Range(0f, 100f)] 
    private float _rotationSpeed = 5f;
    
    private bool _isRotating = false;
    private CancellationTokenSource _cancellationTokenSource;
    private float _rotationDifference = 2f;
    private Transform _target;
    const int requiredStableFrames = 3;
    #endregion

    #region Properties
    public UnityEvent<bool> TargetRotationReachedEvent { get; set; } = new UnityEvent<bool>();
    public bool IsRotating => _isRotating;
    #endregion

    #region Methods
    public void SetTarget(Transform Target)
    {
        if(Target == null && _target != null)
            StopRotation();
        
        _target = Target;
    }
    
    public async void StartRotation()
    {
        if(_target == null)
            return;

        if (_rotationObject == null)
            _rotationObject = transform;

        if (_isRotating)
            return;

        _isRotating = true;
        
        if(_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            _cancellationTokenSource = new CancellationTokenSource();

        int framesStable = 0;
        
        while (_isRotating)
        {
            if (_target == null)
            {
                TargetRotationReachedEvent?.Invoke(false);
                break;
            }

            Vector3 targetPosition = new Vector3(_target.position.x, _rotationObject.position.y, _target.position.z);
            Vector3 direction = targetPosition - _rotationObject.position;

            if (direction.sqrMagnitude < 0.0001f)
            {
                TargetRotationReachedEvent?.Invoke(true);
                break;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Quaternion currentRotation = _rotationObject.rotation;

            _rotationObject.rotation = Quaternion.RotateTowards(
                currentRotation,
                targetRotation,
                _rotationSpeed * Time.deltaTime
            );

            float angle = Quaternion.Angle(_rotationObject.rotation, targetRotation);

            if (angle <= _rotationDifference)
            {
                framesStable++;

                if (framesStable >= requiredStableFrames)
                    TargetRotationReachedEvent?.Invoke(true);
            }
            else
            {
                framesStable = 0;
                TargetRotationReachedEvent?.Invoke(false);
            }

            try
            {
                await UniTask.WaitForEndOfFrame(cancellationToken: _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException exception)
            {
                break;
            }
        }
        
        _isRotating = false;
    }

    public void StopRotation()
    {
        if (!_isRotating)
            return;

        if(_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            _cancellationTokenSource.Cancel();

        TargetRotationReachedEvent?.Invoke(false);
        _isRotating = false;
    }

    private void OnDisable() => StopRotation();
    #endregion
}
