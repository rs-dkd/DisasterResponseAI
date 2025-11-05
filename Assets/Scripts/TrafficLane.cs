using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A single lane in the traffic system.
/// </summary>
public class TrafficLane : MonoBehaviour
{
    [Header("Lane Settings")]
    [Tooltip("Speed limit")]
    public float speedLimit = 10.0f;

    [Header("Path - Bezier points")]
    public Vector3 p0;
    public Vector3 p1 = Vector3.forward * 5;
    public Vector3 p2 = Vector3.forward * 10;
    public Vector3 p3 = Vector3.forward * 15;

    [Header("Connections")]
    public List<TrafficLane> nextLanes = new List<TrafficLane>();

    [Tooltip("Lane to the left. Used for lane switching logic.")]
    public TrafficLane neighborLaneLeft = null;

    [Tooltip("Lane to the right. Used for lane switching logic.")]
    public TrafficLane neighborLaneRight = null;

    [Tooltip("Managed by editor tools")]
    public List<TrafficLane> previousLanes = new List<TrafficLane>();




    [Header("State")]
    [Tooltip("Is a stop signal active")]
    public bool isStopSignalActive = false;

    [Tooltip("Road closed (accident)")]
    public bool isClosed = false;

    private const string GIZMO_PREF_KEY = "RoadSystem_ShowExtraGizmos";


    /// <summary>
    /// Get a point on the lane (0 - 1)
    /// </summary>
    private Vector3 GetPoint(float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return
            oneMinusT * oneMinusT * oneMinusT * p0 + 3f * oneMinusT * oneMinusT * t * p1 + 3f * oneMinusT * t * t * p2 + t * t * t * p3;
    }
    /// <summary>
    /// Get a tangent point on the lane (0 - 1)
    /// </summary>
    private Vector3 GetTangent(float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return
            3f * oneMinusT * oneMinusT * (p1 - p0) + 6f * oneMinusT * t * (p2 - p1) + 3f * t * t * (p3 - p2);
    }
    /// <summary>
    /// Get a world position on the lane (0 - 1)
    /// </summary>
    public Vector3 GetWorldPoint(float t)
    {
        return transform.TransformPoint(GetPoint(t));
    }
    /// <summary>
    /// Get a world tangent on the lane (0 - 1)
    /// </summary>
    public Vector3 GetWorldTangent(float t)
    {
        return transform.TransformDirection(GetTangent(t));
    }


#if UNITY_EDITOR
    /// <summary>
    /// Draw the lane gizmos in the editor
    /// Lane line, speed limit, color coded if blocked, connecting lanes, neighbor lanes
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector3 worldP3_self = transform.TransformPoint(p3);

        //Draw line to next lanes
        if (nextLanes != null)
        {
            foreach (var nextLane in nextLanes)
            {
                if (nextLane != null)
                {
                    Vector3 nextWorldP0 = nextLane.transform.TransformPoint(nextLane.p0);
                    Gizmos.DrawLine(worldP3_self, nextWorldP0);
                    Vector3 dir = (nextWorldP0 - worldP3_self).normalized;
                    if (dir != Vector3.zero)
                    {
                        Vector3 right = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 30, 0) * Vector3.back;
                        Vector3 left = Quaternion.LookRotation(dir) * Quaternion.Euler(0, -30, 0) * Vector3.back;
                        Gizmos.DrawLine(nextWorldP0, nextWorldP0 + right * 0.5f);
                        Gizmos.DrawLine(nextWorldP0, nextWorldP0 + left * 0.5f);
                    }
                }
            }
        }

        //Draw lines to neighbor lanes
        Handles.color = Color.cyan;
        Vector3 worldCenter_self = GetWorldPoint(0.5f);

        if (neighborLaneLeft != null)
        {
            Vector3 worldCenter_left = neighborLaneLeft.GetWorldPoint(0.5f);
            Handles.DrawDottedLine(worldCenter_self, worldCenter_left, 5.0f); 
        }

        if (neighborLaneRight != null)
        {
            Vector3 worldCenter_right = neighborLaneRight.GetWorldPoint(0.5f);
            Handles.DrawDottedLine(worldCenter_self, worldCenter_right, 5.0f);
        }
        

        Vector3 worldP0 = GetWorldPoint(0);
        Vector3 worldP1 = transform.TransformPoint(p1);
        Vector3 worldP2 = transform.TransformPoint(p2);
        Vector3 worldP3 = GetWorldPoint(1);

        if (Selection.activeGameObject != this.gameObject)
        {
            Color pathColor = new Color(1, 1, 1, 0.5f);
            if (isClosed)
            {
                pathColor = new Color(1, 0, 0, 0.5f); 
            }
            else if (isStopSignalActive)
            {
                pathColor = new Color(1, 0.8f, 0, 0.5f); 
            }
            Handles.color = pathColor;
            Handles.DrawBezier(worldP0, worldP3, worldP1, worldP2, Handles.color, null, 2f);
        }

        if (isStopSignalActive || isClosed)
        {
            Gizmos.color = new Color(1, 0, 0, 1f); 
        }
        else
        {
            Gizmos.color = new Color(0, 1, 0, 0.7f);
        }

        Gizmos.DrawSphere(worldP0, 0.25f);

        Gizmos.color = new Color(1, 0, 0, 0.7f);
        Gizmos.DrawSphere(worldP3, 0.25f);

        bool showExtras = EditorPrefs.GetBool(GIZMO_PREF_KEY, true);
        if (!showExtras)
        {
            return;
        }

        Handles.color = new Color(1, 1, 1, 0.7f);
        int numArrows = 3;
        float arrowSize = 0.75f;
        for (int i = 1; i <= numArrows; i++)
        {
            float t = (float)i / (numArrows + 1);
            Vector3 point = GetWorldPoint(t);
            Vector3 tangent = GetWorldTangent(t).normalized;
            if (tangent != Vector3.zero)
            {
                Handles.ArrowHandleCap(0, point, Quaternion.LookRotation(tangent), arrowSize, EventType.Repaint);
            }
        }

        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;
        style.fontStyle = FontStyle.Bold;
        Vector3 textPos = worldP0 + Vector3.up * 0.5f;
        string text = $"Speed: {speedLimit}";
        Handles.Label(textPos, text, style);
    }
#endif
}