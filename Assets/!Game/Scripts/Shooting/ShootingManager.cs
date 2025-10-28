using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

public class ShootingManager : MonoBehaviour, IDependenciesInjection<TowerDependenciesInjection>
{
    #region Fields
    [Header("Settings.")]
    [SerializeField]
    private bool _rotateBeforeShoot = false;
    [SerializeField] 
    private LayerMask _enemyLayer = 0;
    
    private IStart _start;
    private IEnd _end;
    private ITriggeredByObject _triggeredByObject;
    private IMovementSystem _movementSystem;
    private ISpawner _spawner;
    private IRotator _rotator;
    private GameObject _currentTarget = null;
    private bool _readyToShoot = false;
    private bool _inProgress = false;

    #endregion

    #region Properties
    public bool Initialized { get; set; } = false;
    #endregion
    
    #region Methods
    public void Inject(TowerDependenciesInjection Container)
    {
        if(Container == null)
            return;

        _start = Container.Start;
        _end = Container.End;
        _triggeredByObject = Container.TriggeredByObject;
        _movementSystem = Container.MovementSystem;
        _spawner = Container.Spawner;
        
        if(_rotateBeforeShoot)
            _rotator = Container.Rotator;
    }

    public void Initialize()
    {
        Initialized = true;
        
        if(_triggeredByObject != null)
            _triggeredByObject.OnTriggeredEvent.AddListener(ShootPreparation);
        
        if(_start != null)
            _start.OnStartEvent.AddListener(OnStart);
        
        if(_end != null)
            _end.OnEndEvent.AddListener(OnEnd);

        if (_rotator != null && _rotateBeforeShoot)
            _rotator.TargetRotationReachedEvent.AddListener(SetPrepareState);
        else
            _readyToShoot = true;
    }

    private void ShootPreparation(GameObject Target)
    {
        if (Target == null)
        {
            StopShooting();
            return;
        }
        
        if (Target == _currentTarget)
        {
            if (!_readyToShoot)
            {
                if (_rotateBeforeShoot && _rotator != null)
                {
                    _rotator.SetTarget(_currentTarget.transform);
                    if (!_rotator.IsRotating)
                        _rotator.StartRotation();
                }
            }
            return;
        }
        
        StopShooting();

        _currentTarget = Target;
        _readyToShoot = !_rotateBeforeShoot;

        if (_rotateBeforeShoot && _rotator != null)
        {
            _rotator.SetTarget(_currentTarget.transform);
            _rotator.StartRotation();
        }

        if (_readyToShoot)
            StartShooting();
    }
    
    private void StartShooting()
    {
        if (_inProgress || _spawner == null)
            return;

        _inProgress = true;
        _spawner.StartSpawn();
    }

    private void StopShooting()
    {
        if (_inProgress)
            _spawner?.StopSpawn();

        _inProgress = false;
        _readyToShoot = false;

        if (_rotateBeforeShoot && _rotator != null)
        {
            _rotator.StopRotation();
            _rotator.SetTarget(null);
        }

        _readyToShoot = false;
        _currentTarget = null;
    }

    private void SetPrepareState(bool state)
    {
        _readyToShoot = state;
        
        if (_readyToShoot && !_inProgress && _currentTarget != null)
            StartShooting();
    }

    private void OnStart(GameObject Target)
    {
        if(Target == null)
            return;
        
        if(_currentTarget == null)
            return;

        Component movementTargetComponent = ComponentsSearcher.GetSingleComponentOfTypeFromObjectAndChildren(Target, typeof(TargetMovementContainer));
        
        if(movementTargetComponent == null)
            return;
        
        TargetMovementContainer movementContainer = (TargetMovementContainer)movementTargetComponent;
        Vector3 enemyPredictedPosition = CalculateEnemyPredictedPosition();
        movementContainer.TargetPosition = enemyPredictedPosition;
        
        if(_movementSystem != null)
            _movementSystem.AddTarget(Target);
    }

    private Vector3 CalculateEnemyPredictedPosition(int iterations = 5)
    {
        if (_currentTarget == null)
            return Vector3.zero;

        Component enemyMovementComponent = ComponentsSearcher.GetSingleComponentOfTypeFromObjectAndChildren(_currentTarget, typeof(SplineMovementContainer));
        if (enemyMovementComponent == null)
            return _currentTarget.transform.position;

        SplineMovementContainer splineMovement = (SplineMovementContainer)enemyMovementComponent;
        SplineAnimate animate = splineMovement.Animate;
        if (animate == null)
            return _currentTarget.transform.position;

        Transform origin = _movementSystem.ReturnOrigin();
        if (origin == null)
            return _currentTarget.transform.position;

        float projectileSpeed = _movementSystem.ReturnSpeed();
        if (projectileSpeed <= 0f)
            return _currentTarget.transform.position;

        float splineLength = animate.Container.Spline.GetLength();
        float predictedT = animate.NormalizedTime;
        
        for (int i = 0; i < iterations; i++)
        {
            Vector3 predictedPos = animate.Container.Spline.EvaluatePosition(predictedT);
            float distance = Vector3.Distance(origin.position, predictedPos);
            float flightTime = distance / projectileSpeed;

            float enemySpeed = animate.MaxSpeed;
            float deltaT = (enemySpeed * flightTime) / splineLength;

            predictedT = Mathf.Clamp01(animate.NormalizedTime + deltaT);
        }

        return animate.Container.Spline.EvaluatePosition(predictedT);
    }

    private void OnEnd(GameObject Target)
    {
        if(Target == null)
            return;

        if(_movementSystem != null)
            _movementSystem.RemoveTarget(Target);
        
        CastDamageSphere(Target);
        Component poolObjectComponent = ComponentsSearcher.GetSingleComponentOfTypeFromObjectAndChildren(Target, typeof(PoolObject));
        
        if(poolObjectComponent == null)
            return;
        
        ((PoolObject)poolObjectComponent).ReturnToPool();
    }

    private void CastDamageSphere(GameObject Target)
    {
        Component damageContainerComponent = ComponentsSearcher.GetSingleComponentOfTypeFromObjectAndChildren(Target, typeof(DamageContainer));
        
        if(damageContainerComponent == null)
            return;
        
        DamageContainer damageContainer = (DamageContainer)damageContainerComponent;
        Health enemyHealth = null;
        
        Collider[] overlappedEnemies = Physics.OverlapSphere(damageContainer.DamageArea.Center, damageContainer.DamageArea.Radius, _enemyLayer);
        
        if(overlappedEnemies.Length == 0)
            return;
        
        overlappedEnemies = overlappedEnemies.OrderBy(enemy => Vector3.Distance(enemy.transform.position, damageContainer.DamageArea.Center)).ToArray();
        
        Component enemyHealthComponent = ComponentsSearcher.GetSingleComponentOfTypeFromObjectAndChildren(overlappedEnemies.First().gameObject, typeof(Health));
        
        if(enemyHealthComponent == null)
            return;

        enemyHealth = (Health)enemyHealthComponent;
        
        if(enemyHealth == null)
            return;
        
        enemyHealth.CauseDamage((uint)damageContainer.DamageAmount);
    }
    
    private void OnDisable()
    {
        if(_triggeredByObject != null)
            _triggeredByObject.OnTriggeredEvent.RemoveListener(ShootPreparation);
        
        if(_start != null)
            _start.OnStartEvent.RemoveListener(OnStart);
        
        if(_end != null)
            _end.OnEndEvent.RemoveListener(OnEnd);
        
        if (_rotator != null && _rotateBeforeShoot)
            _rotator.TargetRotationReachedEvent.RemoveListener(SetPrepareState);
    }

    #endregion
}
