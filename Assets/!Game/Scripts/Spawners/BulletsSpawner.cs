using UnityEngine;

public class BulletsSpawner : SpawnerBase, IDependenciesInjection<TowerDependenciesInjection>
{
    #region Properties
    public bool Initialized { get; set; } = false;
    #endregion
    
    #region Methods
    public void Inject(TowerDependenciesInjection Container)
    {
        if(Container == null)
            return;
        
        _pool = Container.BulletsPool;
    }

    public void Initialize()
    {
        Initialized = true;
        StartSpawn();
    }
    #endregion
}