using UnityEngine;
using UnityEngine.Splines;

public class SystemsDependenciesContainer : DependenciesContainerBase
{
    #region Fields
    [field: Header("Unity components.")]
    [field: SerializeField]
    public SplineContainer Path { get; private set; }
    
    // [field: Header("Custom components.")]
    // [field: SerializeField]
    // public GridCell _gridCell;
    #endregion

    #region Methods
    public override void InjectDependencies() => Execute<SystemsDependenciesContainer>();
    #endregion
}
