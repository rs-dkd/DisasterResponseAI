using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A manager to create random, temporary accidents on the road network.
/// </summary>
public class AccidentManager : MonoBehaviour
{
    [Header("Accident Settings")]
    public GameObject accidentPrefab;
    public float accidentDuration = 15.0f;
    public float timeBetweenAccidents = 10.0f;
    [Range(0, 1)]
    public float accidentChance = 0.5f;

    private List<TrafficLane> allLanes;
    private HashSet<TrafficLane> closedLanes = new HashSet<TrafficLane>();
    public Transform mainTrafficSystemParent;

    /// <summary>
    /// Setup the manager and start the main sequence
    /// </summary>
    void Start()
    {
        allLanes = mainTrafficSystemParent.GetComponentsInChildren<TrafficLane>().ToList();

        if (allLanes == null || allLanes.Count == 0)
        {
            Debug.LogError("AccidentManager: No TrafficLanes found!", this);
            enabled = false;
            return;
        }
        if (accidentPrefab == null)
        {
            Debug.LogWarning("AccidentManager: No accident prefab assigned.", this);
        }
        StartCoroutine(AccidentCheckLoop());
    }
    /// <summary>
    /// Main sequence - to create accidents
    /// </summary>
    private IEnumerator AccidentCheckLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(timeBetweenAccidents);
            if (Random.value <= accidentChance)
            {
                StartRandomAccident();
            }
        }
    }
    /// <summary>
    /// Create random accident
    /// </summary>
    public void StartRandomAccident()
    {
        var availableLanes = allLanes.Where(lane => !closedLanes.Contains(lane)).ToList();
        if (availableLanes.Count == 0)
        {
            Debug.Log($"No lanes avialble for accident!");
            return;
        }
        TrafficLane lane = availableLanes[Random.Range(0, availableLanes.Count)];
        StartCoroutine(ManageAccident(lane));
    }
    /// <summary>
    /// Manage a single accident
    /// Start - and end
    /// </summary>
    private IEnumerator ManageAccident(TrafficLane lane)
    {
        float posOnLane = Random.Range(0.0f, 1.0f);
        AccidentData accident = new AccidentData(Time.timeSinceLevelLoad,AccidentType.VehicleAccident,lane, posOnLane);

        EmergencyResponseManager.Instance.allAccidents.Add(accident);


        Debug.Log($"Accident started on {lane.name}!");

        //Close the lane for the network
        lane.isClosed = true;
        closedLanes.Add(lane);

        //Create the physical obstacle
        GameObject obstacle = null;
        if (accidentPrefab != null)
        {
            Vector3 pos = lane.GetWorldPoint(posOnLane);
            Quaternion rot = Quaternion.LookRotation(lane.GetWorldTangent(posOnLane));
            obstacle = Instantiate(accidentPrefab, pos, rot);

            if (obstacle.layer != LayerMask.NameToLayer("Vehicles"))
            {
                Debug.LogWarning($"Accident prefab {obstacle.name} is not on the 'Vehicles' layer. Local avoidance may not work.", obstacle);
            }
        }

        //Wait
        yield return new WaitForSeconds(accidentDuration);

        //Clear accident for now - later add ambulances and stuff
        Debug.Log($"Accident cleared on {lane.name}.");
        lane.isClosed = false;
        closedLanes.Remove(lane);
        if (obstacle != null)
        {
            Destroy(obstacle);
        }
    }
}