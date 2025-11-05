using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class RoadSystemEditor : EditorWindow
{
    // --- Preferences ---
    private TrafficLane lanePrefab;
    private float laneWidth = 3.5f;
    private float roadLength = 50.0f;
    private float intersectionSize = 15.0f; 
    private float stubLength = 10.0f; 

    private bool showExtraGizmos;
    private const string GIZMO_PREF_KEY = "RoadSystem_ShowExtraGizmos";

    [MenuItem("Tools/Road System Editor")]
    public static void ShowWindow()
    {
        GetWindow<RoadSystemEditor>("Road System");
    }

    private void OnEnable()
    {
        showExtraGizmos = EditorPrefs.GetBool(GIZMO_PREF_KEY, true);
    }

    private void OnGUI()
    {

        GUILayout.Label("Road Generation Settings", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox("You must have a prefab with the TrafficLane.cs script attached.", MessageType.Info);
        lanePrefab = (TrafficLane)EditorGUILayout.ObjectField("Lane Prefab", lanePrefab, typeof(TrafficLane), false);

        EditorGUILayout.Space(10);

        GUILayout.Label("Shared Parameters", EditorStyles.boldLabel);
        laneWidth = EditorGUILayout.FloatField("Lane Width", laneWidth);

        EditorGUILayout.Space(5);
        GUILayout.Label("Road Parameters", EditorStyles.boldLabel);
        roadLength = EditorGUILayout.FloatField("Road Length", roadLength);

        EditorGUILayout.Space(5);
        GUILayout.Label("Intersection Parameters", EditorStyles.boldLabel);
        intersectionSize = EditorGUILayout.FloatField("Intersection Size (Inner)", intersectionSize);
        stubLength = EditorGUILayout.FloatField("Connection Stub Length", stubLength);

        EditorGUILayout.Space(10);

        GUILayout.Label("Editor Display", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        showExtraGizmos = EditorGUILayout.Toggle("Show Lane Extras", showExtraGizmos);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetBool(GIZMO_PREF_KEY, showExtraGizmos);
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space(10);

        if (lanePrefab == null)
        {
            EditorGUILayout.HelpBox("Please assign a Lane Prefab to enable generation.", MessageType.Warning);
            GUI.enabled = false;
        }

        GUILayout.Label("Generators", EditorStyles.boldLabel);

        if (GUILayout.Button("Create 2-Way 2 Lane Street")) CreateTwoWayStreet();
        //if (GUILayout.Button("Create 2-Way 4 Lane Street")) CreateTwoWayStreet();
        //if (GUILayout.Button("Create T-Intersection")) CreateTIntersection();
        //if (GUILayout.Button("Create 4-Way 2 lane Intersection")) CreateFourWayIntersection();
        //if (GUILayout.Button("Create 4-Way 4 lane Intersection")) CreateFourWayIntersection();

        GUI.enabled = true;

        EditorGUILayout.Space(10);
        GUILayout.Label("Utilities", EditorStyles.boldLabel);

        if (lanePrefab == null)
        {
            EditorGUILayout.HelpBox("Assign a Lane Prefab to use utilities.", MessageType.Warning);
            GUI.enabled = false;
        }

        EditorGUILayout.HelpBox("Select two lanes in the scene. The tool will auto-detect direction and create a new lane to bridge them.", MessageType.Info);
        if (GUILayout.Button("Bridge 2 Selected Lanes"))
        {
            BridgeSelectedLanes();
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox("Rebuilds all previousLane AND nextLane connections", MessageType.Info);
        if (GUILayout.Button("Fix All Lane Connections"))
        {
            FixAllConnections();
        }

        GUI.enabled = true;

        EditorGUILayout.Space(10);
        GUILayout.Label("Analysis", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Finds broken connections or disconnected road islands", MessageType.Info);
        if (GUILayout.Button("Open Road Network Analyzer"))
        {
            RoadSystemAnalyzer.ShowWindow();
        }
    }
    /// <summary>
    /// Spawn point for the new road profile
    /// </summary>
    private Vector3 GetSpawnPoint()
    {
        Vector3 spawnPoint = Vector3.zero;
        if (SceneView.lastActiveSceneView != null)
        {
            Camera sceneCam = SceneView.lastActiveSceneView.camera;
            Ray ray = new Ray(sceneCam.transform.position, sceneCam.transform.forward);

            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float enter))
            {
                spawnPoint = ray.GetPoint(enter);
            }
            else
            {
                Vector3 forwardPoint = ray.GetPoint(20);
                spawnPoint = new Vector3(forwardPoint.x, 0, forwardPoint.z);
            }
        }

        spawnPoint.y = 0;
        return spawnPoint;
    }
    /// <summary>
    /// Create a new lane
    /// </summary>
    private TrafficLane CreateLane(Transform parent, string name, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        if (lanePrefab == null) return null;

        TrafficLane lane = (TrafficLane)PrefabUtility.InstantiatePrefab(lanePrefab, parent);
        lane.gameObject.name = name;
        lane.transform.localPosition = Vector3.zero;
        lane.transform.localRotation = Quaternion.identity;
        lane.p0 = p0; lane.p1 = p1; lane.p2 = p2; lane.p3 = p3;

        lane.nextLanes = new List<TrafficLane>();
        lane.previousLanes = new List<TrafficLane>();

        Undo.RegisterCreatedObjectUndo(lane.gameObject, "Create Lane");
        return lane;
    }

    /// <summary>
    /// Connect 2 lanes and set the prev and next lanes varialbes
    /// </summary>
    private void ConnectLanes(TrafficLane from, TrafficLane to)
    {
        if (from == null || to == null) return;

        Undo.RecordObject(from, "Connect Lanes");
        Undo.RecordObject(to, "Connect Lanes");

        if (from.nextLanes == null) from.nextLanes = new List<TrafficLane>();
        if (to.previousLanes == null) to.previousLanes = new List<TrafficLane>();

        if (!from.nextLanes.Contains(to))
        {
            from.nextLanes.Add(to);
        }
        if (!to.previousLanes.Contains(from))
        {
            to.previousLanes.Add(from);
        }

        EditorUtility.SetDirty(from);
        EditorUtility.SetDirty(to);
    }
    /// <summary>
    /// Bridge lanes by creating a new lane 
    /// </summary>
    
    private void BridgeSelectedLanes()
    {
        List<TrafficLane> selectedLanes = Selection.gameObjects
            .Select(go => go.GetComponent<TrafficLane>())
            .Where(lane => lane != null)
            .ToList();

        if (selectedLanes.Count != 2)
        {
            EditorUtility.DisplayDialog("Connection Error", "Please select 2 TrafficLane objects in the scene.", "OK");
            return;
        }

        TrafficLane laneA = selectedLanes[0];
        TrafficLane laneB = selectedLanes[1];

        Vector3 endA = laneA.GetWorldPoint(1); Vector3 startA = laneA.GetWorldPoint(0);
        Vector3 endB = laneB.GetWorldPoint(1); Vector3 startB = laneB.GetWorldPoint(0);

        float distA_to_B = Vector3.Distance(endA, startB);
        float distB_to_A = Vector3.Distance(endB, startA);

        TrafficLane fromLane, toLane;
        if (distA_to_B < distB_to_A) { fromLane = laneA; toLane = laneB; }
        else { fromLane = laneB; toLane = laneA; }

        Vector3 new_p0 = fromLane.GetWorldPoint(1);
        Vector3 new_p3 = toLane.GetWorldPoint(0);
        Vector3 tangent_p0 = fromLane.GetWorldTangent(1).normalized;
        Vector3 tangent_p3 = toLane.GetWorldTangent(0).normalized;

        //Flatten lanes to Y = 0
        new_p0.y = 0;
        new_p3.y = 0;
        tangent_p0.y = 0;
        tangent_p3.y = 0;
        tangent_p0.Normalize(); 
        tangent_p3.Normalize(); 

        float controlPointLength = Vector3.Distance(new_p0, new_p3) / 3.0f;
        Vector3 new_p1 = new_p0 + tangent_p0 * controlPointLength;
        Vector3 new_p2 = new_p3 - tangent_p3 * controlPointLength;

        Transform parent = fromLane.transform.parent;
        Vector3 local_p0 = (parent != null) ? parent.InverseTransformPoint(new_p0) : new_p0;
        Vector3 local_p1 = (parent != null) ? parent.InverseTransformPoint(new_p1) : new_p1;
        Vector3 local_p2 = (parent != null) ? parent.InverseTransformPoint(new_p2) : new_p2;
        Vector3 local_p3 = (parent != null) ? parent.InverseTransformPoint(new_p3) : new_p3;

        TrafficLane newLane = CreateLane(parent, $"Bridge_{fromLane.name}_to_{toLane.name}",
            local_p0, local_p1, local_p2, local_p3);

        if (newLane != null)
        {
            ConnectLanes(fromLane, newLane);
            ConnectLanes(newLane, toLane);   

            Selection.activeGameObject = newLane.gameObject;
            Debug.Log($"Successfully bridged {fromLane.name} to {toLane.name} with new lane {newLane.name}.", newLane);
        }
    }

    /// <summary>
    /// Fixes all connections in a road network - sets the prev and next lanes for each lane if missing
    /// </summary>
    private void FixAllConnections()
    {
        TrafficLane[] allLanes = FindObjectsOfType<TrafficLane>(true);

        if (allLanes.Length == 0)
        {
            Debug.Log("No traffic lanes found in scene.");
            return;
        }

        var nextConnections = new Dictionary<TrafficLane, HashSet<TrafficLane>>();
        var prevConnections = new Dictionary<TrafficLane, HashSet<TrafficLane>>();

        foreach (var lane in allLanes)
        {
            nextConnections[lane] = new HashSet<TrafficLane>();
            prevConnections[lane] = new HashSet<TrafficLane>();
        }

        foreach (var lane in allLanes)
        {
            if (lane.nextLanes != null)
            {
                foreach (var nextLane in lane.nextLanes)
                {
                    if (nextLane == null) continue;
                    nextConnections[lane].Add(nextLane);
                    prevConnections[nextLane].Add(lane);
                }
            }

            if (lane.previousLanes != null)
            {
                foreach (var prevLane in lane.previousLanes)
                {
                    if (prevLane == null) continue;
                    nextConnections[prevLane].Add(lane);
                    prevConnections[lane].Add(prevLane);
                }
            }
        }

        int nextLinksFixed = 0;
        int prevLinksFixed = 0;
        int lanesModified = 0;

        foreach (var lane in allLanes)
        {
            bool laneChanged = false;
            Undo.RecordObject(lane, "Fix Lane Connections");

            var trueNext = nextConnections[lane];
            var truePrev = prevConnections[lane];

            if (lane.nextLanes == null) lane.nextLanes = new List<TrafficLane>();

            //Add missing next lanes
            foreach (var next in trueNext)
            {
                if (!lane.nextLanes.Contains(next))
                {
                    lane.nextLanes.Add(next);
                    nextLinksFixed++;
                    laneChanged = true;
                }
            }
            //remove invalid next lanes
            for (int i = lane.nextLanes.Count - 1; i >= 0; i--)
            {
                if (lane.nextLanes[i] == null || !trueNext.Contains(lane.nextLanes[i]))
                {
                    lane.nextLanes.RemoveAt(i);
                    nextLinksFixed++;
                    laneChanged = true;
                }
            }


            //fix previous lanes
            if (lane.previousLanes == null) lane.previousLanes = new List<TrafficLane>();

            //add msising links
            foreach (var prev in truePrev)
            {
                if (!lane.previousLanes.Contains(prev))
                {
                    lane.previousLanes.Add(prev);
                    prevLinksFixed++;
                    laneChanged = true;
                }
            }
            //remove invalid links
            for (int i = lane.previousLanes.Count - 1; i >= 0; i--)
            {
                if (lane.previousLanes[i] == null || !truePrev.Contains(lane.previousLanes[i]))
                {
                    lane.previousLanes.RemoveAt(i);
                    prevLinksFixed++; 
                    laneChanged = true;
                }
            }

            if (laneChanged)
            {
                lanesModified++;
                EditorUtility.SetDirty(lane);
            }
        }

        string summary = $"Fix All Connections: Complete.\nFound {allLanes.Length} lanes.\n" +
            $"Modified {lanesModified} lanes.\n" +
            $"Synced/fixed {nextLinksFixed} 'next lane' links.\n" +
            $"Synced/fixed {prevLinksFixed} 'previous lane' links.";

        EditorUtility.DisplayDialog("Connections Fixed", summary, "OK");
    }


    /// <summary>
    /// Generate a 2 way street
    /// </summary>
    private void CreateTwoWayStreet()
    {
        Vector3 spawnPoint = GetSpawnPoint();
        GameObject roadRoot = new GameObject("2-Way Street");
        roadRoot.transform.position = spawnPoint;
        Undo.RegisterCreatedObjectUndo(roadRoot, "Create 2-Way Street");
        float halfWidth = laneWidth / 2.0f;
        CreateLane(roadRoot.transform, "Lane_A", new Vector3(-halfWidth, 0, 0), new Vector3(-halfWidth, 0, roadLength * 0.33f), new Vector3(-halfWidth, 0, roadLength * 0.66f), new Vector3(-halfWidth, 0, roadLength));
        CreateLane(roadRoot.transform, "Lane_B", new Vector3(halfWidth, 0, roadLength), new Vector3(halfWidth, 0, roadLength * 0.66f), new Vector3(halfWidth, 0, roadLength * 0.33f), new Vector3(halfWidth, 0, 0));
        Selection.activeGameObject = roadRoot;
    }

}
