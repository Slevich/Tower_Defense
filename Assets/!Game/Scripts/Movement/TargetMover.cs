using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;

public class TargetMover : MonoBehaviour, IEnd, IMovementSystem
{
    #region Fields
    [Header("References.")]
    [SerializeField]
    private Transform _origin;
    
    [Header("Settings.")]
    [SerializeField, Range(0f, 100f)]
    private float _speed = 10f;
    [SerializeField] 
    private MovementType _movementType = MovementType.Linear;
    [SerializeField, Range(0f, 10f)] 
    private float _porabolaHeight = 3f;
    
    private bool _inProgress = false;
    private CancellationTokenSource _cancellationTokenSource;
    private List<TargetMovementContainer> _movementObjects = new List<TargetMovementContainer>();
    #endregion
    
    #region Properties
    public UnityEvent<GameObject> OnEndEvent { get; set; } = new UnityEvent<GameObject>();
    #endregion

    #region Methods
    public Transform ReturnOrigin() => _origin;
    public float ReturnSpeed() => _speed;
    
    public void AddTarget(GameObject Target)
    {
        if(Target == null)
            return;
        
        if(_movementObjects.Select(obj => obj.gameObject).Contains(Target))
            return;
        
        Component targetMovementComponent = ComponentsSearcher.GetSingleComponentOfTypeFromObjectAndChildren(Target, typeof(TargetMovementContainer));
        
        if(targetMovementComponent == null)
            return;

        TargetMovementContainer movementContainer = (TargetMovementContainer)targetMovementComponent;
        movementContainer.IsMoving = false;
        movementContainer.transform.position = _origin.position;
        _movementObjects.Add(movementContainer);
        Debug.Log("Add target!");
        
        if(!_inProgress)
            StartMovingTargets();
    }

    private async void StartMovingTargets()
    {
        if(_movementObjects.Count == 0)
            return;
        
        if(_cancellationTokenSource == null || (_cancellationTokenSource != null && _cancellationTokenSource.IsCancellationRequested))
            _cancellationTokenSource = new CancellationTokenSource();
        
        _inProgress = true;

        while (_inProgress && (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested))
        {
            for (int i = _movementObjects.Count - 1; i >= 0; i--)
            {
                TargetMovementContainer movementContainer = _movementObjects[i];

                switch (_movementType)
                {
                    case MovementType.Linear:
                        MoveLinear(movementContainer);
                        break;
                    
                    case MovementType.Parabolic:
                        MoveParabolic(movementContainer);
                        break;
                }

                if (Vector3.Distance(movementContainer.transform.position, movementContainer.TargetPosition) < 0.1f)
                {
                    OnEndEvent?.Invoke(movementContainer.gameObject);
                }
            }
            
            try
            {
                await UniTask.WaitForEndOfFrame(_cancellationTokenSource.Token);
            }
            catch (OperationCanceledException exception)
            {
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }
                
                return;
            }
        }
    }

    private void MoveLinear(TargetMovementContainer movementContainer)
    {
        if (!movementContainer.IsMoving)
        {
            movementContainer.IsMoving = true;
            movementContainer.Distance = Vector3.Distance(_origin.position, movementContainer.TargetPosition);
        }
        
        movementContainer.Progress += (_speed * Time.deltaTime) / movementContainer.Distance;
        movementContainer.transform.position = Vector3.Lerp(_origin.position, movementContainer.TargetPosition, movementContainer.Progress);
                        
        if (Vector3.Distance(movementContainer.transform.position, movementContainer.TargetPosition) < 0.05f)
        {
            movementContainer.transform.position = movementContainer.TargetPosition;
            OnEndEvent?.Invoke(movementContainer.gameObject);
        }
    }

    private void MoveParabolic(TargetMovementContainer movementContainer)
    {
        Vector3 start = _origin.position;
        Vector3 end = movementContainer.TargetPosition;
        
        if (!movementContainer.IsMoving)
        {
            movementContainer.IsMoving = true;
            movementContainer.StartPosition = start;
        }
        
        Vector3 horizontalStart = new Vector3(movementContainer.StartPosition.x, 0f, movementContainer.StartPosition.z);
        Vector3 horizontalEnd = new Vector3(end.x, 0f, end.z);
        Vector3 horizontalCurrent = new Vector3(movementContainer.transform.position.x, 0f, movementContainer.transform.position.z);

        float totalHorizontalDistance = Vector3.Distance(horizontalStart, horizontalEnd);
        
        if (totalHorizontalDistance <= Mathf.Epsilon)
        {
            movementContainer.transform.position = end;
            movementContainer.LastDirection = (end - movementContainer.transform.position).normalized;
            OnEndEvent?.Invoke(movementContainer.gameObject);
            return;
        }
        
        float remaining = Vector3.Distance(horizontalCurrent, horizontalEnd);
        float progress = Mathf.Clamp01(1f - (remaining / totalHorizontalDistance));
        
        Vector3 horizontalDir = (horizontalEnd - horizontalCurrent).normalized;
        float step = _speed * Time.deltaTime;
        float moveAmount = Mathf.Min(step, remaining);
        Vector3 newHorizontal = horizontalCurrent + horizontalDir * moveAmount;
        
        float baseY = Mathf.Lerp(movementContainer.StartPosition.y, end.y, progress);
        
        float heightOffset = 4f * _porabolaHeight * progress * (1f - progress);
        float finalY = baseY + heightOffset;
        
        Vector3 newPosition = new Vector3(newHorizontal.x, finalY, newHorizontal.z);
        
        Vector3 lastDir = (newPosition - movementContainer.transform.position).normalized;
        movementContainer.LastDirection = lastDir.sqrMagnitude > 0f ? lastDir : movementContainer.LastDirection;
        movementContainer.transform.position = newPosition;
        
        float finishedThreshold = 0.1f;
        if (Vector3.Distance(new Vector3(newPosition.x, 0f, newPosition.z), horizontalEnd) <= finishedThreshold
            && Mathf.Abs(newPosition.y - end.y) <= 0.5f)
        {
            movementContainer.transform.position = end;
            OnEndEvent?.Invoke(movementContainer.gameObject);
        }
    }

    public void RemoveTarget(GameObject Target)
    {
        if(Target == null)
            return;
        
        Component targetMovementComponent = ComponentsSearcher.GetSingleComponentOfTypeFromObjectAndChildren(Target, typeof(TargetMovementContainer));
        
        if(targetMovementComponent == null)
            return;
        
        TargetMovementContainer movementContainer = (TargetMovementContainer)targetMovementComponent;
        int removedIndex = _movementObjects.IndexOf(movementContainer);
        
        if(removedIndex < 0)
            return;
        
        movementContainer.ResetValues();
        _movementObjects.RemoveAt(removedIndex);
        Debug.Log("Remove target!");

        if(_movementObjects.Count == 0)
            StopMovingTargets();
    }
    
    private void StopMovingTargets()
    {
        if(!_inProgress)
            return;

        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
        }
        
        _inProgress = false;
    }
    
    private void OnDisable() => StopMovingTargets();
    #endregion
}
