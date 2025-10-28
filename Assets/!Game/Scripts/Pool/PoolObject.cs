using UnityEngine;
using UnityEngine.Events;

public class PoolObject : MonoBehaviour
{
    #region Properties
    public ObjectsPool Pool { get; set; }
    public bool IsInPool { get; set; } = false;
    #endregion
    
    #region Methods
    public void ReturnToPool()
    {
        if(Pool == null)
            return;
        
        Pool.ReturnObjectToPool(this);
    }
    #endregion
}
