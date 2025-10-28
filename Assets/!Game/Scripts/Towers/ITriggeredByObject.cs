using UnityEngine;
using UnityEngine.Events;

public interface ITriggeredByObject
{
    public UnityEvent<GameObject> OnTriggeredEvent { get; set; }
}
