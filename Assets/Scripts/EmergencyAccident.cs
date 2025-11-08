using UnityEngine;


public enum AccidentType
{
   VehicleAccident, BuildingFire, VehicleStuck
}

[System.Serializable]
public class AccidentData
{
    public float timeHappened;
    public AccidentType accidentType;
    public TrafficLane laneLocation;
    public float positionOnLane;

    public AccidentData(float time, AccidentType type, TrafficLane lane, float pos)
    {
        this.timeHappened = time;
        this.accidentType = type;
        this.laneLocation = lane;
        this.positionOnLane = pos;
    }
}




/// <summary>
/// Base class for any type of emergency
/// </summary>

public class EmergencyAccident : MonoBehaviour
{

}
