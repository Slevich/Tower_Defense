using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class ObjectsDetection : MonoBehaviour, IDependenciesInjection<TowerDependenciesInjection>, ITriggeredByObject
{
    #region Fields
    [Header("Settings.")] 
    [SerializeField] 
    private LayerMask _enemyLayer = 0;
    
    private SphereArea _sphere;
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private GameObject _closestEnemy = null;
    private bool _inProgress = false;
    #endregion

    #region Properties
    public bool Initialized { get; set; } = false;
    public UnityEvent<GameObject> OnTriggeredEvent { get; set; } = new UnityEvent<GameObject>();
    #endregion
    
    #region Methods
    public void Inject(TowerDependenciesInjection Container)
    {
        if(Container == null)
            return;

        _sphere = Container.DetectionSphere;
    }

    public void Initialize()
    {
        StartDetection();
    }

    public async void StartDetection()
    {
        if(_inProgress)
            return;
        
        if(_sphere == null)
            return;
        
        _inProgress = true;
        
        if(_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            _cancellationTokenSource = new CancellationTokenSource();

        while (_inProgress && !_cancellationTokenSource.IsCancellationRequested)
        {
            Collider[] overlappingEnemies = Physics.OverlapSphere(_sphere.Center, _sphere.Radius, _enemyLayer);
            GameObject newClosestEnemy = null;
            
            if (overlappingEnemies.Length > 0)
            {
                overlappingEnemies = overlappingEnemies.OrderBy(collider => Vector3.Distance(_sphere.Center, collider.transform.position)).ToArray();
                newClosestEnemy = overlappingEnemies.First().gameObject;
            }
            
            if (newClosestEnemy != _closestEnemy)
            {
                OnTriggeredEvent?.Invoke(newClosestEnemy);
            }

            _closestEnemy = newClosestEnemy;
            
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

    public void StopDetection()
    {
        if(!_inProgress)
            return;

        if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
        }
        
        _inProgress = false;
    }

    private void OnDisable() => StopDetection();
    #endregion
}
