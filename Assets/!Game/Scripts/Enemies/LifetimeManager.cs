using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class LifetimeManager : MonoBehaviour, IDependenciesInjection<EnemiesSystemDependenciesContainer>
{
    #region Fields
    private IStart _start;
    private IEnd _end;
    private IMovementSystem _movementSystem;
    private List<Health> _observableObjectsHealths = new List<Health>();
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private bool _inProgress = false;
    #endregion

    #region Properties
    public bool Initialized { get; set; } = false;
    #endregion
    
    #region Methods
    public void Inject(EnemiesSystemDependenciesContainer Container)
    {
        if(Container == null)
            return;
        
        _start = Container.Start;
        _end = Container.End;
        _movementSystem = Container.MovementSystem;
    }

    public void Initialize()
    {
        if(_start != null)
            _start.OnStartEvent.AddListener(OnLifetimeStart);
        
        if (_end != null)
            _end.OnEndEvent.AddListener(OnLifetimeEnd);
    }

    private void OnLifetimeStart(GameObject Instance)
    {
        Component healthComponent = ComponentsSearcher.GetSingleComponentOfTypeFromObjectAndChildren(Instance, typeof(Health));
        
        if(healthComponent == null)
            return;
        
        Health health = (Health)healthComponent;
        
        if(_observableObjectsHealths.Contains(health))
            return;
        
        _observableObjectsHealths.Add(health);
        
        if(_movementSystem != null)
            _movementSystem.AddTarget(Instance);
        
        if(!_inProgress && _observableObjectsHealths.Count > 0)
            StartDetectingLifetime();
    }

    private void OnLifetimeEnd(GameObject Instance)
    {
        Component healthComponent = ComponentsSearcher.GetSingleComponentOfTypeFromObjectAndChildren(Instance, typeof(Health));
        
        if(healthComponent == null)
            return;
        
        Health health = (Health)healthComponent;
        
        if(!_observableObjectsHealths.Contains(health))
            return;
        
        _observableObjectsHealths.Remove(health);
        
        if(_movementSystem != null)
            _movementSystem.RemoveTarget(Instance);
        
        PoolObject poolObject = Instance.GetComponent<PoolObject>();
        
        if(poolObject != null)
            poolObject.ReturnToPool();
        
        if(_inProgress && _observableObjectsHealths.Count == 0)
            StopDetectingLifetime();
    }

    private async void StartDetectingLifetime()
    {
        if(_inProgress)
            return;
        
        _inProgress = true;
        
        if(_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            _cancellationTokenSource = new CancellationTokenSource();

        while (_inProgress && !_cancellationTokenSource.IsCancellationRequested)
        {
            for (int i = _observableObjectsHealths.Count - 1; i >= 0; i--)
            {
                Health observableObjectHealth = _observableObjectsHealths[i];
                
                if(observableObjectHealth == null)
                    continue;

                if (observableObjectHealth.IsDead)
                {
                    OnLifetimeEnd(observableObjectHealth.gameObject);
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
        
        _inProgress = false;
    }

    private void StopDetectingLifetime()
    {
        if(_inProgress)
            _inProgress = false;
        
        if(_cancellationTokenSource != null &&  !_cancellationTokenSource.IsCancellationRequested)
            _cancellationTokenSource.Cancel();
    }
    
    private void OnDisable()
    {
        if(_start != null)
            _start.OnStartEvent.RemoveListener(OnLifetimeStart);
        
        if (_end != null)
            _end.OnEndEvent.RemoveListener(OnLifetimeEnd);
        
        StopDetectingLifetime();
    }
    #endregion
}
