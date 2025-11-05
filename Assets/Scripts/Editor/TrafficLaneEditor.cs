using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for TrafficLane.
/// This script provides interactive handles in the Scene View to edit the Bezier curve.
/// </summary>
[CustomEditor(typeof(TrafficLane))]
public class TrafficLaneEditor : Editor
{
    private TrafficLane lane;
    private Transform handleTransform;
    private Quaternion handleRotation;

    private const float handleSize = 0.2f;
    private const float pickSize = 0.3f;

    /// <summary>
    /// Renders the scene view
    /// </summary>
    private void OnSceneGUI()
    {
        lane = target as TrafficLane;
        if (lane == null)
            return;

        handleTransform = lane.transform;
        //Use the lane's rotation for the handles orientation
        handleRotation = handleTransform.rotation;

        //Get world space points
        Vector3 p0 = handleTransform.TransformPoint(lane.p0);
        Vector3 p1 = handleTransform.TransformPoint(lane.p1);
        Vector3 p2 = handleTransform.TransformPoint(lane.p2);
        Vector3 p3 = handleTransform.TransformPoint(lane.p3);

        //Draw the Bezier Curve
        Handles.color = Color.white;
        Handles.DrawBezier(p0, p3, p1, p2, Color.white, null, 3f);

        //Draw Control Point Lines
        Handles.color = Color.gray;
        Handles.DrawLine(p0, p1);
        Handles.DrawLine(p2, p3);

        //Draw and Manage Handles
        //P0 Handle
        Handles.color = Color.green;
        EditorGUI.BeginChangeCheck();
        Vector3 newWorldP0 = DrawHandle(p0);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(lane, "Move Start Point");
            lane.p0 = handleTransform.InverseTransformPoint(newWorldP0);

            //Update previous lanes too
            if (lane.previousLanes != null)
            {
                foreach (var prevLane in lane.previousLanes)
                {
                    if (prevLane == null) continue;
                    Undo.RecordObject(prevLane, "Move Connected End Point");
                    prevLane.p3 = prevLane.transform.InverseTransformPoint(newWorldP0);
                    EditorUtility.SetDirty(prevLane); 
                }
            }
        }

        //P3 Handle
        Handles.color = Color.red;
        EditorGUI.BeginChangeCheck();
        Vector3 newWorldP3 = DrawHandle(p3);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(lane, "Move End Point");
            lane.p3 = handleTransform.InverseTransformPoint(newWorldP3);

            //Update connected lanes
            if (lane.nextLanes != null)
            {
                foreach (var nextLane in lane.nextLanes)
                {
                    if (nextLane == null) continue;
                    Undo.RecordObject(nextLane, "Move Connected Start Point");
                    nextLane.p0 = nextLane.transform.InverseTransformPoint(newWorldP3);
                    EditorUtility.SetDirty(nextLane); 
                }
            }
        }

        //P1 Handle
        Handles.color = Color.cyan;
        EditorGUI.BeginChangeCheck();
        Vector3 newWorldP1 = DrawHandle(p1);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(lane, "Move Start Control Point");
            lane.p1 = handleTransform.InverseTransformPoint(newWorldP1);
        }

        //P2 Handle
        Handles.color = Color.cyan;
        EditorGUI.BeginChangeCheck();
        Vector3 newWorldP2 = DrawHandle(p2);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(lane, "Move End Control Point");
            lane.p2 = handleTransform.InverseTransformPoint(newWorldP2);
        }
    }

    /// <summary>
    /// Helper function to draw a standard handle.
    /// </summary>
    private Vector3 DrawHandle(Vector3 worldPoint)
    {
        float size = HandleUtility.GetHandleSize(worldPoint) * handleSize;
        return Handles.PositionHandle(worldPoint, handleRotation);
    }
}

