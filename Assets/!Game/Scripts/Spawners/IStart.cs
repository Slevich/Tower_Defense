using UnityEngine;
using UnityEngine.Events;

public interface IStart
{
    public UnityEvent<GameObject> OnStartEvent { get; set; }
}
