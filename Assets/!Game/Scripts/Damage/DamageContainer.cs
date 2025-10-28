using UnityEngine;

public class DamageContainer : MonoBehaviour
{
    #region Properties
    [field: Header("References.")]
    [field: SerializeField]
    public SphereArea DamageArea;

    [field: Header("Settings.")] 
    [field: SerializeField, Range(0, 100)]
    public int DamageAmount = 10;
    #endregion
}
