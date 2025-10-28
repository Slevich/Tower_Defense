using UnityEngine;

public class EnemiesSpawner : SpawnerBase, IDependenciesInjection<EnemiesSystemDependenciesContainer>
{
    #region Properties
    public bool Initialized { get; set; } = false;
    #endregion
    
    #region Methods
    public void Inject(EnemiesSystemDependenciesContainer Container)
    {
        if(Container == null)
            return;
        
        _pool = Container.Pool;
    }

    public void Initialize()
    {
        Initialized = true;
        StartSpawn();
    }
    #endregion
}
