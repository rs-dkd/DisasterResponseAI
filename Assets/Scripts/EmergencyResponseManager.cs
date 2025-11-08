using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

/// <summary>
/// Controls dispatching emergency vehicles and prepositioning units to minimize response times
/// </summary>
public class EmergencyResponseManager : MonoBehaviour
{
    public static EmergencyResponseManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate EmergencyResponseManager found! Destroying this new one.");
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public int vehicleCount = 3;
    public List<AccidentData> allAccidents;
    [Tooltip("Entire road network.")]
    public List<TrafficLane> allLanes;

    [Header("K-Means Parameters")]
    [Tooltip("Max number of iterations")]
    public int maxIterations = 100;

    [Tooltip("Stop algo if hotspots not moving threshold")]
    public float convergenceThreshold = 0.01f;
    public Transform mainTrafficSystemParent;

    public GameObject[] emergencyVics;

    private void Start()
    {
        allLanes = mainTrafficSystemParent.GetComponentsInChildren<TrafficLane>().ToList();

    }

    public float checkerTime = 10;
    float timer = 10;
    private void Update()
    {
        timer -= Time.deltaTime;
        if(timer < 0)
        {
            List<TrafficLane>  lanes = CalculateHotspots();
            timer = checkerTime;

            for (int i = 0; i < emergencyVics.Length; i++)
            {
                emergencyVics[i].transform.position = lanes[i].GetWorldPoint(0.5f);
            }
        }
    }

    /// <summary>
    /// Get evenly spaced roads based on number of emergency vehicles
    /// </summary>
    private List<TrafficLane> GetEvenlySpacedRoadPositions(int count)
    {
        List<TrafficLane> spacedLanes = new List<TrafficLane>();


        float step = (float)allLanes.Count / count;

        for (int i = 0; i < count; i++)
        {
            int index = Mathf.RoundToInt(i * step);

            index = Mathf.Clamp(index, 0, allLanes.Count - 1);

            spacedLanes.Add(allLanes[index]);
        }


        return spacedLanes.Distinct().ToList();
    }
    /// <summary>
    /// Calculates the best roads to preposition vehicles.
    /// </summary>
    public List<TrafficLane> CalculateHotspots()
    {
        if (allAccidents == null || allAccidents.Count == 0 || vehicleCount <= 0)
        {
            return GetEvenlySpacedRoadPositions(vehicleCount);
        }


        List<Vector3> accidentPositions = new List<Vector3>();
        foreach (AccidentData accident in allAccidents)
        {
            // We get the position from the 'Road' object
            accidentPositions.Add(accident.laneLocation.GetWorldPoint(accident.positionOnLane));
        }

        if (accidentPositions.Count < vehicleCount)
        {
            return GetEvenlySpacedRoadPositions(vehicleCount);
        }

        //Run the K-Means algorithm 
        List<Vector3> idealPositions = RunKMeans(accidentPositions, vehicleCount);

        //Get closest roads from the positions
        List<TrafficLane> prepositionRoads = new List<TrafficLane>();
        foreach (Vector3 pos in idealPositions)
        {
            TrafficLane closestRoad = FindClosestRoad(pos);
            if (closestRoad != null)
            {
                prepositionRoads.Add(closestRoad);
            }
        }

        return prepositionRoads;
    }

    /// <summary>
    /// K-Means clustering algorithm
    /// </summary>
    private List<Vector3> RunKMeans(List<Vector3> dataPoints, int k)
    {
        List<Vector3> centroids = new List<Vector3>();
        List<int> usedIndices = new List<int>();
        for (int i = 0; i < k; i++)
        {
            int index = -1;
            do
            {
                index = Random.Range(0, dataPoints.Count);
            } while (usedIndices.Contains(index));

            usedIndices.Add(index);
            centroids.Add(dataPoints[index]);
        }

        List<Vector3> oldCentroids = new List<Vector3>();

        // Main algorithm loop
        for (int iter = 0; iter < maxIterations; iter++)
        {
            // 2. Assignment Step: Assign each point to the nearest cluster/centroid
            List<Vector3>[] clusters = new List<Vector3>[k];
            for (int i = 0; i < k; i++)
            {
                clusters[i] = new List<Vector3>();
            }

            foreach (Vector3 point in dataPoints)
            {
                int closestCentroidIndex = GetClosestCentroidIndex(point, centroids);
                clusters[closestCentroidIndex].Add(point);
            }

            // 3. Update Step: Recalculate centroids as the mean of their cluster
            oldCentroids = new List<Vector3>(centroids); // Store old positions
            for (int i = 0; i < k; i++)
            {
                if (clusters[i].Count > 0)
                {
                    centroids[i] = CalculateMeanPosition(clusters[i]);
                }
            }

            // 4. Convergence Check: Stop if centroids have barely moved
            if (HasConverged(oldCentroids, centroids))
            {
                Debug.Log($"K-Means converged in {iter + 1} iterations.");
                break;
            }
            if (iter == maxIterations - 1)
            {
                Debug.LogWarning("K-Means reached max iterations without converging.");
            }
        }

        return centroids;
    }

    private int GetClosestCentroidIndex(Vector3 point, List<Vector3> centroids)
    {
        float minDistance = float.MaxValue;
        int closestIndex = 0;
        for (int i = 0; i < centroids.Count; i++)
        {
            float distance = Vector3.Distance(point, centroids[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    private Vector3 CalculateMeanPosition(List<Vector3> points)
    {
        if (points.Count == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero;
        foreach (Vector3 point in points)
        {
            sum += point;
        }
        return sum / points.Count;
    }

    private bool HasConverged(List<Vector3> oldCentroids, List<Vector3> newCentroids)
    {
        for (int i = 0; i < oldCentroids.Count; i++)
        {
            if (Vector3.Distance(oldCentroids[i], newCentroids[i]) > convergenceThreshold)
            {
                return false;
            }
        }
        return true;
    }



    /// <summary>
    /// Finds the closest Road (node) in your network to an ideal mathematical point.
    /// </summary>
    private TrafficLane FindClosestRoad(Vector3 position)
    {
        if (allLanes == null || allLanes.Count == 0)
        {
            Debug.LogError("The 'allRoads' list is not set up in the HotspotManager!");
            return null;
        }

        TrafficLane closestRoad = null;
        float minDistance = float.MaxValue;

        foreach (TrafficLane road in allLanes)
        {
            // Compare the ideal point to the 'Road' (node) position
            float distance = Vector3.Distance(position, road.GetWorldPoint(0.5f));
            if (distance < minDistance)
            {
                minDistance = distance;
                closestRoad = road;
            }
        }
        return closestRoad;
    }

}