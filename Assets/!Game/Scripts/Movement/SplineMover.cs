using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;

public class SplineMover : MonoBehaviour, IDependenciesInjection<EnemiesSystemDependenciesContainer>, IEnd, IMovementSystem
{
    #region Fields
    [Header("Settings.")] 
    [SerializeField, Range(0f, 60f)] 
    private float _movementDurationInSeconds = 5f;
    
    private SplineContainer _path;
    private List<SplineAnimate> _movementObjects = new List<SplineAnimate>();
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private bool _inProgress = false;
    private Vector3 _targetPosition = Vector3.zero;
    #endregion
    
    #region Properties
    public bool Initialized { get; set; } = false;
    public UnityEvent<GameObject> OnEndEvent { get; set; } = new UnityEvent<GameObject>();
    #endregion
    
    #region Methods
    public Transform ReturnOrigin() => null;
    public float ReturnSpeed() => 0f;
    
    public void Inject(EnemiesSystemDependenciesContainer Container)
    {
        if (Container == null)
            return;
        
        _path = Container.Path;

        if (_path != null)
            _targetPosition = _path.transform.TransformPoint(_path.Spline.Knots.Last().Position);
    }

    public void Initialize()
    {
        Initialized = true;
    }

    public void AddTarget(GameObject Target)
    {
        if(Target == null)
            return;
        
        if(_movementObjects.Select(obj => obj.gameObject).Contains(Target))
            return;

        SplineAnimate movement = null;

        if (Target.TryGetComponent(out SplineAnimate animation))
        {
            movement = animation;
        }
        else
        {
            movement = Target.AddComponent<SplineAnimate>();
        }
        
        _movementObjects.Add(movement);

        if (_path != null)
        {
            movement.Container = _path;
            movement.Duration = _movementDurationInSeconds;
            movement.Loop = SplineAnimate.LoopMode.Loop;
            movement.PlayOnAwake = true;
            movement.Restart(true);

            Component splineMovementContainerComponent =
                ComponentsSearcher.GetSingleComponentOfTypeFromObjectAndChildren(Target, typeof(SplineMovementContainer));
            
            if(splineMovementContainerComponent != null)
                ((SplineMovementContainer)splineMovementContainerComponent).Animate = movement;
        }
        
        if(!_inProgress)
            StartObservingTargets();
    }

    private async void StartObservingTargets()
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
                SplineAnimate movementAnimation = _movementObjects[i];

                if (Vector3.Distance(movementAnimation.transform.position, _targetPosition) < 0.1f)
                {
                    OnEndEvent?.Invoke(movementAnimation.gameObject);
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

    public void RemoveTarget(GameObject Target)
    {
        if(Target == null)
            return;
        
        if(!TryGetComponent<SplineAnimate>(out SplineAnimate animation))
            return;
        
        int removedIndex = _movementObjects.IndexOf(animation);
        
        if(removedIndex < 0)
            return;
        
        SplineAnimate movementAnimation = _movementObjects[removedIndex];
        movementAnimation.Pause();
        _movementObjects.RemoveAt(removedIndex);

        if(_movementObjects.Count == 0)
            StopObservingTargets();
    }
    
    private void StopObservingTargets()
    {
        if(!_inProgress)
            return;

        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
        }
        
        _inProgress = false;
    }
    
    private void OnDisable() => StopObservingTargets();
    #endregion
}
