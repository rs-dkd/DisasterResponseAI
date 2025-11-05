using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages a traffic light system by cycling through groups of lanes.
/// </summary>
public class TrafficLightGroup : MonoBehaviour
{
    [System.Serializable]
    public class LaneGroup
    {
        public string groupName;
        public List<TrafficLane> lanes = new List<TrafficLane>();
        public TrafficLight[] onTrafficLights;
    }

    [Tooltip("The groups of lanes to control. Each group will be activated in order.")]
    public List<LaneGroup> lightGroups = new List<LaneGroup>();

    [Tooltip("Green time")]
    public float greenLightDuration = 10.0f;

    [Tooltip("Yellow Time")]
    public float yellowLightDuration = 2.0f;

    //Current group
    private int currentGroupIndex = 0;

    void Start()
    {
        if (lightGroups.Count == 0)
        {
            return;
        }

        //Initialize all lights to red
        foreach (var group in lightGroups)
        {
            SetGroupState(group, LightColor.Red);
        }

        //Start the cycle
        StartCoroutine(LightCycle());
    }
    /// <summary>
    /// Main function for cycling the light groups.
    /// </summary>
    private IEnumerator LightCycle()
    {
        while (true)
        {
            LaneGroup currentGroup = lightGroups[currentGroupIndex];

            //Turn current group to green
            SetGroupState(currentGroup, LightColor.Green);
            yield return new WaitForSeconds(greenLightDuration);
            //Turn current group to yellow
            SetGroupState(currentGroup, LightColor.Yellow);
            yield return new WaitForSeconds(yellowLightDuration);
            //Turn current group to red
            SetGroupState(currentGroup, LightColor.Red);

            //Next group
            currentGroupIndex = (currentGroupIndex + 1) % lightGroups.Count;
        }
    }

    /// <summary>
    /// Sets the state for all lanes in a group.
    /// </summary>
    private void SetGroupState(LaneGroup group, LightColor color)
    {
        foreach (var lane in group.lanes)
        {
            if (lane != null)
            {
                //Yellow and Green are both go
                if(color == LightColor.Red)
                    lane.isStopSignalActive = true;
                else
                    lane.isStopSignalActive = false;
            }
        }
        foreach (var light in group.onTrafficLights)
        {
            if (light != null)
            {
                light.ChangeLight(color);
            }
        }
    }
}

