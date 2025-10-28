using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TowerExecutionOrder : MonoBehaviour, IDependenciesInjection<TowerDependenciesInjection>
{
    #region Fields
    [Header("Settings.")]
    [SerializeField, Range(0.1f, 60f)]
    private float _startDelayInSeconds = 0.5f;

    private ObjectsDetection _detectionSystem;
    private ShootingManager _shootingManager;
    private CancellationTokenSource _cancellationTokenSource;
    #endregion

    #region Properties
    public bool Initialized { get; set; } = false;
    #endregion

    #region Methods
    public async void Inject(TowerDependenciesInjection Container)
    {
        if(Container == null)
            return;

        _detectionSystem = Container.DetectionSystem;
        _shootingManager = Container.ShootingManager;
        
        if(_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            await UniTask.WaitForSeconds(_startDelayInSeconds,  cancellationToken: _cancellationTokenSource.Token);
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
        
        Initialize();
    }

    private void Initialize()
    {
        Initialized = true;
        
        if(_detectionSystem != null)
            _detectionSystem.Initialize();
        
        if(_shootingManager != null)
            _shootingManager.Initialize();
    }
    #endregion
}
