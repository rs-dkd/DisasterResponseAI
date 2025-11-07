using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityEngineLaneBuilder : MonoBehaviour
{
    [Header("Inputs")]
    [Tooltip("Road graph loader that has already loaded street_graph.json")]
    public RoadGraphLoader roadGraphLoader;

    [Tooltip("Prefab with TrafficLane component (same one used by RoadSystemEditor).")]
    public TrafficLane lanePrefab;

    [Tooltip("Parent transform under which all generated lanes will be created.")]
    public Transform lanesParent;

    [Header("Generation Settings")]
    [Tooltip("Scale factor to apply to positions if needed (1 = unchanged).")]
    public float positionScale = 1.0f;

    [Tooltip("Use edge.geometry if available, otherwise straight line between nodes.")]
    public bool useEdgeGeometry = true;

    private Dictionary<EdgeData, TrafficLane> edgeToLane = new();

    private bool _generated = false;

    private IEnumerator Start()
    {
        yield return null;

        if (roadGraphLoader == null || roadGraphLoader.Graph == null)
        {
            Debug.LogError("CityEngineLaneBuilder: RoadGraphLoader or Graph is null. Check inspector wiring.", this);
            yield break;
        }

        if (!_generated)
        {
            GenerateLanes();
            _generated = true;
        }
    }

    [ContextMenu("Generate Lanes From Road Graph")]
    public void GenerateLanes()
    {
        if (roadGraphLoader == null || roadGraphLoader.Graph == null)
        {
            Debug.LogError("CityEngineLaneBuilder: RoadGraphLoader or graph not set/loaded.", this);
            return;
        }
        if (lanePrefab == null)
        {
            Debug.LogError("CityEngineLaneBuilder: lanePrefab is not assigned.", this);
            return;
        }
        if (lanesParent == null)
        {
            lanesParent = this.transform;
        }

        for (int i = lanesParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(lanesParent.GetChild(i).gameObject);
        }

        edgeToLane.Clear();

        var graph = roadGraphLoader.Graph;
        var data = roadGraphLoader.RawData;

        foreach (var edge in data.edges)
        {
            if (!graph.Nodes.TryGetValue(edge.source, out var srcNode)) continue;
            if (!graph.Nodes.TryGetValue(edge.target, out var dstNode)) continue;

            Vector3 p0, p1, p2, p3;

            if (useEdgeGeometry && edge.geometry != null && edge.geometry.Length >= 2)
            {
                var verts = edge.geometry;
                Vector3 start = ToWorld(verts[0]);
                Vector3 end = ToWorld(verts[verts.Length - 1]);

                if (verts.Length >= 4)
                {
                    p0 = start;
                    p3 = end;
                    p1 = ToWorld(verts[verts.Length / 3]);
                    p2 = ToWorld(verts[2 * verts.Length / 3]);
                }
                else
                {
                    p0 = start;
                    p3 = end;
                    Vector3 dir = (p3 - p0).normalized;
                    float segLen = Vector3.Distance(p0, p3);
                    p1 = p0 + dir * (segLen / 3f);
                    p2 = p0 + dir * (2f * segLen / 3f);
                }
            }
            else
            {
                Vector3 start = ToWorld(srcNode.Position);
                Vector3 end = ToWorld(dstNode.Position);
                p0 = start;
                p3 = end;
                Vector3 dir = (p3 - p0).normalized;
                float segLen = Vector3.Distance(p0, p3);
                p1 = p0 + dir * (segLen / 3f);
                p2 = p0 + dir * (2f * segLen / 3f);
            }

            TrafficLane lane = Instantiate(lanePrefab, lanesParent);
            lane.name = $"Lane_{edge.source}_{edge.target}";

            lane.transform.position = Vector3.zero;
            lane.transform.rotation = Quaternion.identity;

            lane.p0 = lanesParent.InverseTransformPoint(p0);
            lane.p1 = lanesParent.InverseTransformPoint(p1);
            lane.p2 = lanesParent.InverseTransformPoint(p2);
            lane.p3 = lanesParent.InverseTransformPoint(p3);

            if (edge.maxspeed_kph > 0)
            {
                lane.speedLimit = edge.maxspeed_kph / 3.6f;
            }

            lane.nextLanes = new List<TrafficLane>();
            lane.previousLanes = new List<TrafficLane>();

            edgeToLane[edge] = lane;
        }

        var nodeToIncoming = new Dictionary<int, List<EdgeData>>();
        var nodeToOutgoing = new Dictionary<int, List<EdgeData>>();

        foreach (var edge in data.edges)
        {
            if (!nodeToIncoming.ContainsKey(edge.target))
                nodeToIncoming[edge.target] = new List<EdgeData>();
            nodeToIncoming[edge.target].Add(edge);

            if (!nodeToOutgoing.ContainsKey(edge.source))
                nodeToOutgoing[edge.source] = new List<EdgeData>();
            nodeToOutgoing[edge.source].Add(edge);
        }

        foreach (var kvp in nodeToIncoming)
        {
            int nodeId = kvp.Key;
            List<EdgeData> incomingEdges = kvp.Value;

            nodeToOutgoing.TryGetValue(nodeId, out var outgoingEdges);
            if (outgoingEdges == null) continue;

            foreach (var inEdge in incomingEdges)
            {
                if (!edgeToLane.TryGetValue(inEdge, out var inLane)) continue;

                foreach (var outEdge in outgoingEdges)
                {
                    if (!edgeToLane.TryGetValue(outEdge, out var outLane)) continue;

                    inLane.nextLanes.Add(outLane);
                    outLane.previousLanes.Add(inLane);
                }
            }
        }

        Debug.Log($"CityEngineLaneBuilder: Generated {edgeToLane.Count} lanes from road graph.", this);
    }

    private Vector3 ToWorld(float[] coords)
    {
        if (coords == null || coords.Length < 3)
            return Vector3.zero;

        Vector3 local = new Vector3(-coords[0], coords[1], coords[2]) * positionScale;

        Transform t = lanesParent != null ? lanesParent : transform;
        return t.TransformPoint(local);
    }

    private Vector3 ToWorld(Vector3 v)
    {
        Vector3 local = new Vector3(-v.x, v.y, v.z) * positionScale;
        Transform t = lanesParent != null ? lanesParent : transform;
        return t.TransformPoint(local);
    }
}