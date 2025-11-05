using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Vehicle Script
/// Handles main navigation (BFS) and local avoidance
/// TODO - make local avoidance and better pathfinding
/// </summary>
public class TrafficVehicle : MonoBehaviour
{
    [Header("Navigation")]
    [Tooltip("The lane the vehicle is currently on")]
    public TrafficLane currentLane;
    [Tooltip("The target lane")]
    public TrafficLane targetLane;

    [Header("Movement")]
    [Tooltip("The vehicles top speed")]
    public float maxSpeed = 15.0f;

    [Tooltip("Smooth turn amount")]
    public float rotationSpeed = 5.0f;

    [Header("Local Avoidance")]
    [Tooltip("Detect other cars distance")]
    public float detectionDistance = 10.0f;

    [Tooltip("Stopping distance")]
    public float stoppingDistance = 3.0f;

    [Tooltip("All Vehicle Layer")]
    public LayerMask vehicleLayerMask;

    [Header("Lane Changing")]
    [Tooltip("Enable disable lane changing")]
    public bool enableLaneChanging = true;

    [Tooltip("lane change maneuver speed")]
    public float laneChangeSpeed = 2.0f;

    [Tooltip("Lane change cooldown")]
    public float laneChangeCooldown = 1.0f;

    [Tooltip("Lane change distance check for other cars")]
    public float laneChangeCheckDistance = 15.0f;

    [Tooltip("proximity trigger for lane change (0-1)")]
    public float laneChangeTriggerProximity = 0.5f;

    /// <summary>
    /// Event broadcast when the vehicle reaches its target lane.
    /// The VehicleSpawner listens to this to assign a new target.
    /// </summary>
    public System.Action<TrafficVehicle> OnTargetReached;


    // Progress along the Bezier curve (0 -1).
    private float t = 0.0f;

    // The calculated path to the target.
    private List<TrafficLane> path = new List<TrafficLane>();
    private int pathIndex = 0; 
    public bool enableLocalAvoidance = false;

    //Lane change variables
    private bool isChangingLane = false;
    private TrafficLane targetChangeLane = null;
    private TrafficLane originatingLane = null;
    private float laneChangeProgress = 0.0f;
    private float timeSinceLastLaneChange = 0.0f;
    private bool recalculatePathOnLaneChange = true;



    /// <summary>
    /// Setup the vehicle
    /// </summary>
    void Start()
    {
        if (currentLane == null)
        {
            if (Time.frameCount > 2)
            {
                Debug.LogError("TrafficVehicle has no starting lane!", this);
                enabled = false;
            }
            return;
        }

        transform.position = currentLane.GetWorldPoint(0);
        transform.rotation = Quaternion.LookRotation(currentLane.GetWorldTangent(0));

        if (targetLane != null)
        {
            SetNewTarget(targetLane);
        }

        //Stagger cooldowns for lane changes
        timeSinceLastLaneChange = Random.Range(0f, laneChangeCooldown);
    }

    /// <summary>
    /// Sets a new target for the vehicle and calculates the path.
    /// </summary>
    public void SetNewTarget(TrafficLane newTarget)
    {
        targetLane = newTarget;
        if (currentLane == null || targetLane == null)
        {
            path.Clear();
            return;
        }

        //Finds path
        path = FindPath(currentLane, targetLane);
        pathIndex = 0;

        if (path == null || path.Count == 0)
        {
            Debug.LogError($"Vehicle {name} could not find a path from {currentLane.name} to {targetLane.name}!", this);
        }
    }

    void Update()
    {
        if (currentLane == null) return;

        timeSinceLastLaneChange += Time.deltaTime;

        float baseSpeed = Mathf.Min(maxSpeed, currentLane.speedLimit);
        float avoidanceSpeed = baseSpeed;
        RaycastHit hit;

        //Local Avoidance logic
        if (enableLocalAvoidance && Physics.Raycast(transform.position, transform.forward, out hit, detectionDistance, vehicleLayerMask))
        {
            //TODO - add local avoidance for the vehicle in front and if changing lanes

        }

        //Changing lanes
        if (enableLaneChanging && !isChangingLane && timeSinceLastLaneChange >= laneChangeCooldown)
        {
            if (path != null && path.Count > 0 && pathIndex < path.Count - 1)
            {

                //Is the next target a neighbor than change lanes
                TrafficLane nextLaneInPath = path[pathIndex + 1];
                bool isPlannedMerge = (currentLane.neighborLaneLeft == nextLaneInPath || currentLane.neighborLaneRight == nextLaneInPath);

                if (isPlannedMerge && t > 0.2f) 
                {
                    StartLaneChange(nextLaneInPath, false);
                }
            }
        }

        float effectiveSpeed = Mathf.Min(baseSpeed, avoidanceSpeed);



        float tangentLength = currentLane.GetWorldTangent(t).magnitude;
        if (tangentLength < 0.1f) tangentLength = 0.1f;

        if (!isChangingLane)
        {
            t += (effectiveSpeed * Time.deltaTime) / tangentLength;
        }


        if (t >= 1.0f && !isChangingLane)
        {
            float overflowT = t - 1.0f; 

            if (path == null || path.Count == 0)
            {
                t = 1.0f;
                OnTargetReached?.Invoke(this);
            }
            else
            {
                pathIndex++;

                if (pathIndex < path.Count)
                {
                    TrafficLane nextLane = path[pathIndex];

                    // Red light
                    if (nextLane.isStopSignalActive)
                    {
                        t = 1.0f; 
                        pathIndex--; 
                        transform.position = currentLane.GetWorldPoint(1.0f);
                        transform.rotation = Quaternion.LookRotation(currentLane.GetWorldTangent(1.0f).normalized);
                        return; 
                    }

                    //Accident or closed lane
                    if (nextLane.isClosed)
                    {
                        t = 1.0f; 
                        pathIndex--; 
                        SetNewTarget(targetLane); 
                        transform.position = currentLane.GetWorldPoint(1.0f);
                        transform.rotation = Quaternion.LookRotation(currentLane.GetWorldTangent(1.0f).normalized);
                        return; 
                    }

                    //Planned merge
                    bool isNeighborChange = (currentLane.neighborLaneLeft == nextLane ||
                                             currentLane.neighborLaneRight == nextLane);
                    if (isNeighborChange)
                    {
                        t = 1.0f;
                        pathIndex--; 
                        StartLaneChange(nextLane, false); 
                    }
                    else
                    {
                        currentLane = nextLane;
                        t = overflowT; 
                    }
                }
                else
                {
                    //Reached end of path
                    currentLane = path.Last();
                    t = 1.0f;
                    path.Clear();

                    OnTargetReached?.Invoke(this);
                }
            }
        }

        //Lane changing for moving and rotating the vehicle
        Vector3 targetPoint;
        Vector3 tangent;

        if (isChangingLane)
        {
            laneChangeProgress += Time.deltaTime * laneChangeSpeed;

            Vector3 originPoint = originatingLane.GetWorldPoint(t);
            Vector3 targetLanePoint = targetChangeLane.GetWorldPoint(t);

            targetPoint = Vector3.Lerp(originPoint, targetLanePoint, laneChangeProgress);

            Vector3 originTangent = originatingLane.GetWorldTangent(t).normalized;
            Vector3 targetLaneTangent = targetChangeLane.GetWorldTangent(t).normalized;
            tangent = Vector3.Lerp(originTangent, targetLaneTangent, laneChangeProgress).normalized;

            if (laneChangeProgress >= 1.0f)
            {
                currentLane = targetChangeLane;
                originatingLane = null;
                targetChangeLane = null;
                isChangingLane = false;
                timeSinceLastLaneChange = 0f;

                if (recalculatePathOnLaneChange)
                {
                    SetNewTarget(this.targetLane);
                }
                else
                {
                    pathIndex++;
                }
            }
        }
        else
        {
            //Follow the current lane
            targetPoint = currentLane.GetWorldPoint(t);
            tangent = currentLane.GetWorldTangent(t).normalized;
        }

        transform.position = targetPoint;
        if (tangent != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(tangent);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }




    /// <summary>
    /// Begins the lane change maneuver.
    /// </summary>
    private void StartLaneChange(TrafficLane target, bool recalculatePath = true)
    {
        isChangingLane = true;
        targetChangeLane = target;
        originatingLane = currentLane; 
        laneChangeProgress = 0.0f;
        recalculatePathOnLaneChange = recalculatePath; 
    }


    /// <summary>
    /// Finds the shortest path from start to end using BFS 
    /// TODO - Make a more advanced pathfinding algo
    /// </summary>
    private List<TrafficLane> FindPath(TrafficLane start, TrafficLane end)
    {
        Queue<TrafficLane> queue = new Queue<TrafficLane>();
        Dictionary<TrafficLane, TrafficLane> parentMap = new Dictionary<TrafficLane, TrafficLane>();

        queue.Enqueue(start);
        parentMap[start] = null;

        while (queue.Count > 0)
        {
            TrafficLane current = queue.Dequeue();

            if (current == end)
            {
                List<TrafficLane> path = new List<TrafficLane>();
                TrafficLane pathNode = end;
                while (pathNode != null)
                {
                    path.Add(pathNode);
                    pathNode = parentMap[pathNode];
                }
                path.Reverse();
                return path;
            }

            //Adds next lanes and neighbors
            List<TrafficLane> allConnections = new List<TrafficLane>();
            if (current.nextLanes != null)
            {
                allConnections.AddRange(current.nextLanes);
            }
            if (current.neighborLaneLeft != null)
            {
                allConnections.Add(current.neighborLaneLeft);
            }
            if (current.neighborLaneRight != null)
            {
                allConnections.Add(current.neighborLaneRight);
            }

            foreach (TrafficLane next in allConnections)
            {
                //Dont pathfind through closed lanes
                if (next != null && !parentMap.ContainsKey(next) && !next.isClosed)
                {
                    parentMap[next] = current;
                    queue.Enqueue(next);
                }
            }
        }

        return new List<TrafficLane>(); 
    }
    /// <summary>
    /// Draw gizmos in editor
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(Vector3.zero, Vector3.forward * detectionDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(Vector3.zero, Vector3.forward * stoppingDistance);

        if (currentLane != null)
        {
            Vector3 tangent = currentLane.GetWorldTangent(t).normalized;
            if (tangent == Vector3.zero) tangent = transform.forward;

            Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.LookRotation(tangent), Vector3.one);
            Gizmos.color = new Color(0, 1, 1, 0.3f); 
            Gizmos.DrawCube(Vector3.zero, new Vector3(2.0f, 2.0f, laneChangeCheckDistance));
        }
    }
}
