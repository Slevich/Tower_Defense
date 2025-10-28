using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class EnemySystemExecutionOrder : MonoBehaviour, IDependenciesInjection<EnemiesSystemDependenciesContainer>
{
    #region Fields
    [Header("Settings.")]
    [SerializeField, Range(0.1f, 60f)]
    private float _startDelayInSeconds = 0.5f;
    
    private LifetimeManager _lifetimeManager;
    private EnemiesSpawner _spawner;
    private SplineMover _mover;
    private CancellationTokenSource _cancellationTokenSource;
    #endregion

    #region Properties
    public bool Initialized { get; set; } = false;
    #endregion

    #region Methods
    public async void Inject(EnemiesSystemDependenciesContainer Container)
    {
        if(Container == null)
            return;

        _lifetimeManager = Container.LifetimeManager;
        _spawner = Container.Spawner;
        _mover = Container.Mover;
        
        if(_cancellationTokenSource == null || (_cancellationTokenSource != null && _cancellationTokenSource.IsCancellationRequested))
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
        
        if(_lifetimeManager != null)
            _lifetimeManager.Initialize();
        
        if(_mover != null)
            _mover.Initialize();
        
        if(_spawner != null)
            _spawner.Initialize();
    }
    #endregion
}
