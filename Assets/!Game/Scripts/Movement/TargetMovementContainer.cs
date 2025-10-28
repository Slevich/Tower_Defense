using UnityEngine;

public class TargetMovementContainer : MonoBehaviour
{
    #region Properties
    public Vector3 TargetPosition { get; set; } = Vector3.zero;
    public Vector3 LastDirection { get; set; } = Vector3.zero;
    public Vector3 StartPosition { get; set; } = Vector3.zero;
    public float StartTime { get; set; } = 0f;
    public float FlightTime { get; set; } = 0f;
    public float Distance { get; set; } = 0f;
    public bool IsMoving { get; set; } = false;
    public float Progress { get; set; } = 0f;
    #endregion

    #region Methods
    public void ResetValues()
    {
        IsMoving = false;
        Progress = 0f;
        StartTime = 0f;
        StartPosition = Vector3.zero;
        Distance = 0f;
        FlightTime = 0f;
        LastDirection = Vector3.zero;
        TargetPosition = Vector3.zero;
    }
    #endregion
}

public enum MovementType
{
    Linear,
    Parabolic
}