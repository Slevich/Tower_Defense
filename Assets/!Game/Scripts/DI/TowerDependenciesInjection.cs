using UnityEngine;

public class TowerDependenciesInjection : DependenciesContainerBase
{
    #region Properties
    [field: Header("Custom components.")]
    [field: SerializeField]
    public SphereArea DetectionSphere { get; private set; }
    [field: SerializeField]
    public ObjectsDetection DetectionSystem { get;  private set; }
    [field: SerializeField]
    public ShootingManager ShootingManager { get; private set; }
    [field: SerializeField]
    public ObjectsPool BulletsPool { get; private set; }
    [field: SerializeField]
    public BulletsSpawner BulletsSpawner { get; private set; }
    [field: SerializeField]
    public TargetMover TargetMovementSystem { get; private set; }
    [field: SerializeField]
    public ObjectRotator RotationSystem { get; private set; }
    
    public IEnd End { get; private set; }
    public IStart Start { get; private set; }
    public ITriggeredByObject TriggeredByObject { get; set; }
    public IMovementSystem MovementSystem { get; private set; }
    public ISpawner Spawner { get; private set; }
    public IRotator Rotator { get; private set; }
    #endregion

    #region Methods
    private void Awake()
    {
        Start = BulletsSpawner;
        End = TargetMovementSystem;
        TriggeredByObject = DetectionSystem;
        MovementSystem = TargetMovementSystem;
        Spawner = BulletsSpawner;
        Rotator = RotationSystem;
        
        InjectDependencies();
    }

    public override void InjectDependencies() => Execute<TowerDependenciesInjection>();
    #endregion
}
