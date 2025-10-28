using UnityEngine;
using UnityEngine.Splines;

public class EnemiesSystemDependenciesContainer : DependenciesContainerBase
{
    #region Properties
    [field: Header("Unity components.")]
    [field: SerializeField]
    public SplineContainer Path { get; private set; }
    
    [field: Header("Custom components.")]
    [field: SerializeField]
    public LifetimeManager LifetimeManager { get; private set; }
    [field: SerializeField]
    public EnemiesSpawner Spawner { get; private set; }
    [field: SerializeField]
    public SplineMover Mover { get; private set; }
    [field: SerializeField]
    public ObjectsPool Pool { get; private set; }
    
    public IEnd End { get; private set; }
    public IStart Start { get; private set; }
    public IMovementSystem MovementSystem { get; private set; }
    #endregion

    #region Methods
    private void Awake()
    {
        if(Spawner != null)
            Start = Spawner;

        if (Mover != null)
        {
            End = Mover;
            MovementSystem = Mover;
        }
        
        InjectDependencies();
    }

    public override void InjectDependencies() => Execute<EnemiesSystemDependenciesContainer>();
    #endregion
}
