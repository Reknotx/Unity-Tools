using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoadCreator))]
public class RoadEditor : Editor
{
    private RoadCreator creator;

    private void OnSceneGUI()
    {
        if (creator.autoUpdate && Event.current.type == EventType.Repaint)
        {
            creator.UpdateRoad();
        }
    }

    private void OnEnable()
    {
        creator = (RoadCreator) target;
    }
}
