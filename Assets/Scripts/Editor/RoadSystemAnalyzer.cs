using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
/// <summary>
/// A tool to analyze the road network and notify of any problems
/// </summary>
public class RoadSystemAnalyzer : EditorWindow
{
    private Vector2 scrollPos;
    // Updated lists to store the lane along with the message
    private List<KeyValuePair<string, TrafficLane>> errors = new List<KeyValuePair<string, TrafficLane>>();
    private List<KeyValuePair<string, TrafficLane>> warnings = new List<KeyValuePair<string, TrafficLane>>();
    private List<string> graphInfo = new List<string>();

    [MenuItem("Tools/Road System Analyzer")]

    /// <summary>
    /// Show the window
    /// </summary>
    public static void ShowWindow()
    {
        GetWindow<RoadSystemAnalyzer>("Road Analyzer");
    }
    /// <summary>
    /// Setup and display the UI in Editor
    /// </summary>
    private void OnGUI()
    {
        GUILayout.Label("Road Network Analysis Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This tool will scan all TrafficLanes in your scene for errors, warnings, and connectivity problems.", MessageType.Info);

        //Analyze network button
        if (GUILayout.Button("Analyze Entire Road Network", GUILayout.Height(40)))
        {
            AnalyzeNetwork();
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        if (graphInfo.Count > 0)
        {
            GUILayout.Label("Connectivity Analysis", EditorStyles.boldLabel);
            foreach (var info in graphInfo)
            {
                EditorGUILayout.HelpBox(info, MessageType.Info);
            }
        }

        //Errors section
        if (errors.Count > 0)
        {
            GUILayout.Label("Errors (Must Fix)", EditorStyles.boldLabel);
            foreach (var entry in errors)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(entry.Key, MessageType.Error, true);

                //Allows for user to select problem lane
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    if (entry.Value != null)
                    {
                        Selection.activeGameObject = entry.Value.gameObject;
                        EditorGUIUtility.PingObject(entry.Value.gameObject);
                        if (SceneView.lastActiveSceneView != null)
                        {

                            Bounds bounds = new Bounds(entry.Value.transform.position, Vector3.one * 10f);
                            SceneView.lastActiveSceneView.Frame(bounds, false);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        //Warnings section
        if (warnings.Count > 0)
        {
            GUILayout.Label("Warnings (Review)", EditorStyles.boldLabel);
            foreach (var entry in warnings)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(entry.Key, MessageType.Warning, true);

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    if (entry.Value != null)
                    {
                        Selection.activeGameObject = entry.Value.gameObject;
                        EditorGUIUtility.PingObject(entry.Value.gameObject);
                        if (SceneView.lastActiveSceneView != null)
                        {
                            Bounds bounds = new Bounds(entry.Value.transform.position, Vector3.one * 10f);
                            SceneView.lastActiveSceneView.Frame(bounds, false);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();
    }
    /// <summary>
    /// Analyze the entire network - called from the editor button
    /// </summary>
    private void AnalyzeNetwork()
    {
        //Clear messages
        errors.Clear();
        warnings.Clear();
        graphInfo.Clear();
        Debug.Log("Starting Analysis");

        // Find all lanes in the scene
        TrafficLane[] allLanes = FindObjectsOfType<TrafficLane>();
        if (allLanes.Length == 0)
        {
            warnings.Add(new KeyValuePair<string, TrafficLane>("No TrafficLanes found in the scene.", null));
            Debug.LogWarning("Analysis complete: No TrafficLanes found.");
            return;
        }

        HashSet<TrafficLane> allLanesSet = new HashSet<TrafficLane>(allLanes);
        HashSet<TrafficLane> visitedLanes = new HashSet<TrafficLane>();
        List<HashSet<TrafficLane>> graphs = new List<HashSet<TrafficLane>>();

        //Check each lane for issues
        foreach (TrafficLane lane in allLanes)
        {
            CheckLane(lane);
        }

        //Check for disconnected graphs
        foreach (TrafficLane lane in allLanes)
        {
            if (!visitedLanes.Contains(lane))
            {
                //Start a new graph traversal.
                HashSet<TrafficLane> newGraph = new HashSet<TrafficLane>();
                Stack<TrafficLane> stack = new Stack<TrafficLane>();

                stack.Push(lane);
                visitedLanes.Add(lane);
                newGraph.Add(lane);

                //Perform a DFS to find all reachable lanes
                while (stack.Count > 0)
                {
                    TrafficLane current = stack.Pop();

                    //Check next lanes
                    if (current.nextLanes != null)
                    {
                        foreach (var next in current.nextLanes)
                        {
                            if (next != null && !visitedLanes.Contains(next))
                            {
                                visitedLanes.Add(next);
                                newGraph.Add(next);
                                stack.Push(next);
                            }
                        }
                    }

                    //Checkprevious lanes
                    if (current.previousLanes != null)
                    {
                        foreach (var prev in current.previousLanes)
                        {
                            if (prev != null && !visitedLanes.Contains(prev))
                            {
                                visitedLanes.Add(prev);
                                newGraph.Add(prev);
                                stack.Push(prev);
                            }
                        }
                    }
                }
                graphs.Add(newGraph);
            }
        }

        //Report graph analysis
        graphInfo.Add($"Found {allLanes.Length} total lanes.");
        graphInfo.Add($"Network is divided into {graphs.Count} disconnected graph(s).");

        if (graphs.Count > 1)
        {
            graphInfo.Add("This is likely the cause of your 'Could not find path' error!");
            for (int i = 0; i < graphs.Count; i++)
            {
                graphInfo.Add($"  Graph {i + 1} contains {graphs[i].Count} lanes.");
                if (graphs[i].Count > 0)
                {
                    Debug.LogWarning($"Graph {i + 1} (example lane: {graphs[i].First().name}). Your vehicle cannot route from this graph to another.", graphs[i].First());
                }
            }
        }
        else
        {
            graphInfo.Add("Your road network is fully connected. Good job!");
        }

        //Summary
        string summary = $"Analysis Complete: Found {errors.Count} Errors, {warnings.Count} Warnings, and {graphs.Count} disconnected graphs.";
        Debug.Log(summary);

        //Refresh window
        Repaint();
    }
    /// <summary>
    /// Checks a lane for errors or warnings
    /// </summary>
    private void CheckLane(TrafficLane lane)
    {
        if (lane == null) return;

        //Check for dead end lanes - all lanes should have a next lane or a neighbor lane
        if (lane.nextLanes != null)
        {
            for (int i = 0; i < lane.nextLanes.Count; i++)
            {
                if (lane.nextLanes[i] == null)
                {
                    string error = $"Lane '{lane.name}' has a NULL entry in its 'Next Lanes' list at index {i}. This will break pathfinding!";
                    // Add to new list structure
                    errors.Add(new KeyValuePair<string, TrafficLane>(error, lane));
                    Debug.LogError(error, lane);
                }
            }
        }

        //Dead end lane
        if (lane.nextLanes == null || lane.nextLanes.Count == 0)
        {
            string warning = $"Lane '{lane.name}' is a DEAD END (no 'Next Lanes'). Is this intentional?";
            warnings.Add(new KeyValuePair<string, TrafficLane>(warning, lane));
            Debug.LogWarning(warning, lane);
        }

        //No previous lanes connected
        if (lane.previousLanes == null || lane.previousLanes.Count == 0)
        {
            string warning = $"Lane '{lane.name}' is an ORPHANED lane (no 'Previous Lanes'). Is this a starting lane?";
            warnings.Add(new KeyValuePair<string, TrafficLane>(warning, lane));
            Debug.LogWarning(warning, lane);
        }
    }
}

