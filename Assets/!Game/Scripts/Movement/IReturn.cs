using UnityEngine;
using UnityEngine.Events;

public interface IEnd
{
    public UnityEvent<GameObject> OnEndEvent { get; set; }
}
