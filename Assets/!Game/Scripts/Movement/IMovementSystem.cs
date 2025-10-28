using UnityEngine;

public interface IMovementSystem
{
    public void AddTarget(GameObject Target);
    public void RemoveTarget(GameObject Target);
    public Transform ReturnOrigin();
    public float ReturnSpeed();
}
