using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectsPool : MonoBehaviour
{
    #region Fields
    [Header("References.")] 
    [SerializeField]
    private PoolObject[] _prefabs = Array.Empty<PoolObject>();
    [SerializeField] 
    private Transform _outsidePoolObjectsParent;
    [Header("Settings.")]
    [SerializeField, Range(0, 100)]
    private int _maxPoolSize = 100;
    [SerializeField, Range(1, 100)]
    private int _preparingAmount =  10;
    
    [SerializeField]
    private List<PoolObject> _allInstances = new List<PoolObject>();
    [SerializeField]
    private List<PoolObject> _instancesInsidePool = new List<PoolObject>();
    [SerializeField]
    private List<PoolObject> _instancesOutsidePool = new List<PoolObject>();
    #endregion

    #region Methods
    private void OnValidate()
    {
        if(_preparingAmount < 0 )
            _preparingAmount = 0;
        else if(_preparingAmount > _maxPoolSize)
            _preparingAmount = _maxPoolSize;
    }
    
    private void Awake()
    {
        if (_preparingAmount > 0)
        {
            PrepareSomeObjects();
        }
    }

    private void PrepareSomeObjects()
    {
        if(_prefabs.Length == 0)
            return;

        int maxAmountPerPrefab = _maxPoolSize / _prefabs.Length;
        
        foreach (PoolObject prefab in _prefabs)
        {
            for(int i = 0; i < _preparingAmount && i < maxAmountPerPrefab; i++)
            {
                PoolObject newPoolObject = SpawnNewObject(prefab);
                ReturnObjectToPool(newPoolObject);
            }
        }
    }

    public PoolObject GetObjectFromPool()
    {
        PoolObject returnedObject = null;
        
        if (_instancesInsidePool.Count == 0)
        {
            if (_allInstances.Count == _maxPoolSize)
            {
                return null;
            }
            else
            {
                int randomPrefabIndex = UnityEngine.Random.Range(0, _prefabs.Length);
                returnedObject = SpawnNewObject(_prefabs[randomPrefabIndex]);
            }
        }
        else
        {
            int randomInstanceIndex = UnityEngine.Random.Range(0, _instancesInsidePool.Count);
            returnedObject  = _instancesInsidePool[randomInstanceIndex];
        }
        
        returnedObject.transform.SetParent(_outsidePoolObjectsParent);
        returnedObject.gameObject.SetActive(true);
        returnedObject.IsInPool = false;
        
        if(_instancesInsidePool.Contains(returnedObject))
            _instancesInsidePool.Remove(returnedObject);
        
        return returnedObject;
    }

    private PoolObject SpawnNewObject(PoolObject prefab)
    {
        if(prefab == null)
            return null;
        
        PoolObject poolObject = Instantiate(prefab, null);
        poolObject.Pool = this;
        _allInstances.Add(poolObject);
        return poolObject;
    }

    public void ReturnObjectToPool(PoolObject PoolObject)
    {
        if(PoolObject == null)
            return;
        
        if(PoolObject.Pool != this)
            return;
        
        PoolObject.IsInPool = true;
        PoolObject.transform.parent = transform;
        PoolObject.transform.localPosition = Vector3.zero;
        
        if(_instancesOutsidePool.Contains(PoolObject))
            _instancesOutsidePool.Remove(PoolObject);
        
        if(!_instancesInsidePool.Contains(PoolObject))
            _instancesInsidePool.Add(PoolObject);
        
        Component[] resetComponents = ComponentsSearcher.GetComponentsOfTypeFromObjectAndAllChildren(PoolObject.gameObject, typeof(IReset));

        if (resetComponents.Length > 0)
        {
            foreach (Component component in resetComponents)
            {
                ((IReset)component).Reset();
            }
        }
        
        PoolObject.gameObject.SetActive(false);
    }
    #endregion
}
