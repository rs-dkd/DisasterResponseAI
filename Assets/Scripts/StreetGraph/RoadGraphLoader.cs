using UnityEngine;

public class RoadGraphLoader : MonoBehaviour
{
    public TextAsset roadJson;

    public RoadGraphData RawData { get; private set; }
    public RoadGraph Graph { get; private set; }

    private void Awake()
    {
        if (roadJson == null)
        {
            Debug.LogError("RoadGraphLoader: roadJson not assigned.");
            return;
        }

        RawData = JsonUtility.FromJson<RoadGraphData>(roadJson.text);
        Graph = new RoadGraph(RawData);

        Debug.Log($"RoadGraphLoader: Loaded {Graph.Nodes.Count} nodes, {RawData.edges.Length} edges.");
    }
}
