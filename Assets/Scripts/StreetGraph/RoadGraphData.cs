using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NodeData
{
    public int id;
    public float[] pos;

    public Vector3 Position
    {
        get
        {
            if (pos != null && pos.Length >= 3)
                return new Vector3(pos[0], pos[1], pos[2]);
            return Vector3.zero;
        }
    }
}

[Serializable]
public class EdgeData
{
    public int source;
    public int target;
    public float length;
    public float[][] geometry;
    public float maxspeed_kph;
    public string highway;
    public string name;
    public string oneway;
}

[Serializable]
public class BuildingData
{
    public int id;
    public float[][] footprint;
}

[Serializable]
public class RoadGraphData
{
    public NodeData[] nodes;
    public EdgeData[] edges;
    public BuildingData[] buildings;
}

public class RoadGraph
{
    public Dictionary<int, NodeData> Nodes = new();
    public Dictionary<int, List<EdgeData>> Adjacency = new();

    public RoadGraph(RoadGraphData data)
    {
        if (data.nodes != null)
        {
            foreach (var n in data.nodes)
            {
                Nodes[n.id] = n;
                Adjacency[n.id] = new List<EdgeData>();
            }
        }

        if (data.edges != null)
        {
            foreach (var e in data.edges)
            {
                if (!Adjacency.ContainsKey(e.source))
                    Adjacency[e.source] = new List<EdgeData>();

                Adjacency[e.source].Add(e);

                if (!Adjacency.ContainsKey(e.target))
                    Adjacency[e.target] = new List<EdgeData>();

                var reverse = new EdgeData
                {
                    source = e.target,
                    target = e.source,
                    length = e.length
                };
                Adjacency[e.target].Add(reverse);
            }
        }
    }
}
