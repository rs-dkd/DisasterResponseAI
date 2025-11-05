using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Spawns a number of vehicles onto the road network and gives them
/// random destinations. When a vehicle reaches its destination, it
/// is assigned a new one.
/// </summary>
public class VehicleSpawner : MonoBehaviour
{
    [Header("Spawning Config")]
    [Tooltip("The vehicle prefab to spawn. Must have a TrafficVehicle component.")]
    [SerializeField] private GameObject vehiclePrefab;
    [Tooltip("The total number of vehicles to spawn.")]
    [SerializeField] private int numberOfVehicles = 20;
    [Tooltip("Traffic System Parent Transform")]
    [SerializeField] private Transform mainTrafficSystemParent;
    //All lanes in the scene
    private List<TrafficLane> allLanes;

    /// <summary>
    /// Setup - Get all lanes and spawn the vehicles
    /// </summary>
    void Start()
    {
        allLanes = mainTrafficSystemParent.GetComponentsInChildren<TrafficLane>().ToList();

        if (allLanes == null || allLanes.Count == 0)
        {
            Debug.LogError("VehicleSpawner: No TrafficLanes found in scene! Cannot spawn vehicles.");
            return;
        }
        if (vehiclePrefab == null)
        {
            Debug.LogError("VehicleSpawner: Vehicle Prefab is not set!");
            return;
        }

        //Spawn the vehicles
        SpawnVehicles();
    }
    /// <summary>
    /// Spawn Vehicles
    /// </summary>
    private void SpawnVehicles()
    {
        for (int i = 0; i < numberOfVehicles; i++)
        {
            //Get random lane
            TrafficLane startLane = GetRandomLane();

            //Create the vehicle on a random point on the lane
            Vector3 spawnPos = startLane.GetWorldPoint(UnityEngine.Random.Range(0.0f,1.0f));
            Quaternion spawnRot = Quaternion.LookRotation(startLane.GetWorldTangent(0).normalized);
            GameObject newVehicleObj = Instantiate(vehiclePrefab, spawnPos, spawnRot);
            newVehicleObj.name = $"Vehicle_{i}";

            //Add vehicle script
            TrafficVehicle vehicle = newVehicleObj.GetComponent<TrafficVehicle>();


            //Assign lane to vehicle
            vehicle.currentLane = startLane;

            //Assign random target lane
            vehicle.targetLane = GetRandomLane(startLane); 

            //Create function to call on target reached
            vehicle.OnTargetReached += HandleVehicleTargetReached;
        }
    }

    /// <summary>
    /// This method is called by a vehicle when it reaches its target.
    /// </summary>
    private void HandleVehicleTargetReached(TrafficVehicle vehicle)
    {
        //Get a new target
        TrafficLane newTarget = GetRandomLane(vehicle.currentLane);
        vehicle.SetNewTarget(newTarget);
    }

    /// <summary>
    /// Helper function to get a random lane
    /// </summary>
    private TrafficLane GetRandomLane(TrafficLane excludeLane = null)
    {
        if (allLanes.Count <= 1)
        {
            return allLanes[0];
        }

        TrafficLane newLane;
        do
        {
            newLane = allLanes[UnityEngine.Random.Range(0, allLanes.Count)];
        }
        while (newLane == excludeLane);

        return newLane;
    }
}