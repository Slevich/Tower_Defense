using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;

public class SpawnerBase : MonoBehaviour, IStart, ISpawner
{
	#region Fields
    [Header("Settings.")]
    [SerializeField, Range(0f, 60f)] 
    private float _secondsBetweenSpawns = 1f;

    private CancellationTokenSource _cancellationTokenSource;
    protected ObjectsPool _pool;
    protected bool _inProgress;
    #endregion

    #region Properties
    public UnityEvent<GameObject> OnStartEvent { get; set; } = new UnityEvent<GameObject>();
    #endregion
    
    #region Methods
    public async void StartSpawn()
    {
        if(_inProgress)
            return;

        if (_pool == null)
            return;

        _inProgress = true;
        
        if(_cancellationTokenSource == null || (_cancellationTokenSource != null && _cancellationTokenSource.IsCancellationRequested))
            _cancellationTokenSource = new CancellationTokenSource();

        while (_inProgress && (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested))
        {
            PoolObject newInstance = _pool.GetObjectFromPool();
            OnStartEvent?.Invoke(newInstance.gameObject);
            
            try
            {
                await UniTask.WaitForSeconds(_secondsBetweenSpawns, cancellationToken: _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException exception)
            {
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }
                
                break;
            }
        }
    }
    
    public void StopSpawn()
    {
        if (!_inProgress)
            return;

        if(_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
        }

        _inProgress = false;
    }
    
    private void OnDisable() => StopSpawn();
    #endregion
}