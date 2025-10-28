using UnityEngine;
using UnityEngine.Events;

public interface IRotator
{
    public UnityEvent<bool> TargetRotationReachedEvent { get; set; }
    
    public void SetTarget(Transform Target);
    public void StartRotation();
    public void StopRotation();
    
    public bool IsRotating { get; }
}
