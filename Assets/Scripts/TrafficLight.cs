using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum LightColor
{
    Red,
    Yellow,
    Green
}
/// <summary>
/// Single Traffic Light
/// </summary>
public class TrafficLight : MonoBehaviour
{
    [Tooltip("Traffic Light off materila")]
    public Material offMaterial;
    [Tooltip("Traffic Light On materila")]
    public Material onMaterial;

    [Tooltip("Red light material")]
    public Renderer redLightRenderer;
    [Tooltip("Green light material")]
    public Renderer greenLightRenderer;
    [Tooltip("Yellow light material")]
    public Renderer yellowLightRenderer;
    /// <summary>
    /// Manages change the materials on a light - called from the traffic light group
    /// </summary>
    public void ChangeLight(LightColor color)
    {
        if(color == LightColor.Red)
        {
            redLightRenderer.material = onMaterial;
            greenLightRenderer.material = offMaterial;
            yellowLightRenderer.material = offMaterial;
        }
        else if(color == LightColor.Yellow)
        {
            redLightRenderer.material = offMaterial;
            greenLightRenderer.material = offMaterial;
            yellowLightRenderer.material = onMaterial;
        }
        else if(color == LightColor.Green)
        {
            redLightRenderer.material = offMaterial;
            greenLightRenderer.material = onMaterial;
            yellowLightRenderer.material = offMaterial;
        }
    }
}

