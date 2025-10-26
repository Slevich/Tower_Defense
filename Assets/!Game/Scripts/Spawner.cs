using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Splines;
using Cysharp.Threading.Tasks;

public class Spawner : MonoBehaviour, IDependenciesInjection<SystemsDependenciesContainer>
{
	#region Fields
    [Header("References.")] 
    [SerializeField]
    private GameObject _enemyPrefab;
    private SplineContainer _path;
    
    [Header("Settings.")]
    [SerializeField, Range(0f, 60f)] 
    private float _secondsBetweenSpawns = 1f;

    private bool _inProgress;
    private CancellationTokenSource _cancellationTokenSource;
    private List<GameObject> _instances = new List<GameObject>();
    #endregion

    #region Properties
    public bool Initialized { get; set; } = false;
    #endregion
    
    #region Methods

    public void Inject(SystemsDependenciesContainer Container)
    {
        if(Container == null)
            return;
        
        _path = Container.Path;
    }

    public void Initialize()
    {
        StartSpawn();
    }
    
    public async void StartSpawn()
    {
        if(!Initialized)
            return;
        
        if(_inProgress)
            return;

        if (_enemyPrefab == null)
            return;

        _inProgress = true;
        
        if(_cancellationTokenSource == null || (_cancellationTokenSource != null && _cancellationTokenSource.IsCancellationRequested))
            _cancellationTokenSource = new CancellationTokenSource();

        while (_inProgress && (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested))
        {
            GameObject newInstance = Instantiate(_enemyPrefab, transform);
            
            if (_path != null)
            {
                SplineAnimate splineAnimation = newInstance.AddComponent<SplineAnimate>();
                splineAnimation.Container = _path;
                splineAnimation.Play();
            }
            
            _instances.Add(newInstance);

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

    private void OnEnable () => StartSpawn();

    private void OnDisable() => StopSpawn();
    #endregion
}