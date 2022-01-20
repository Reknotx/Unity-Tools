using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PathCreator))]
public class PathEditor : Editor
{
    private PathCreator creator;

    private Path path => creator.path;

    private const float segmentSelectDistThreshold = .1f;
    private int selectedSegmentIndex = -1;

    private static bool registeringInput;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUI.BeginChangeCheck();
        
        if (GUILayout.Button("Create new"))
        {
            Undo.RecordObject(creator, "Create new");
            creator.CreatePath();
        }

        bool isClosed = GUILayout.Toggle(path.IsClosed, "Closed loop");
        
        if (isClosed != path.IsClosed)
        {
            Undo.RecordObject(creator, "Toggle closed");
            path.IsClosed = isClosed;
        }

        bool autoSetControlPoints = GUILayout.Toggle(path.AutoSetControlPoints, "Auto set control points");
        if (autoSetControlPoints != path.AutoSetControlPoints)
        {
            Undo.RecordObject(creator, "Toggle auto set controls");
            path.AutoSetControlPoints = autoSetControlPoints;
        }

        if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();
    }
    
    private void OnSceneGUI()
    {
        Input();
        Draw();
    }

    void Input()
    {
        Event guiEvent = Event.current;
        Vector2 mousePos = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition).origin;
        
        //Added this to avoid having input fire off multiple times.
        if (registeringInput) registeringInput = false;
        else
        {
            //TODO - Apparently this code fires off twice
            if (guiEvent.shift && guiEvent.type == EventType.MouseDown && guiEvent.button == 0)
            {
                registeringInput = true;

                if (selectedSegmentIndex != -1)
                {
                    Undo.RecordObject(creator, "Split segment");
                    path.SplitSegment(mousePos, selectedSegmentIndex);
                }
                else if (!path.IsClosed)
                {
                    Undo.RecordObject(creator, "Add Segment");
                    path.AddSegment(mousePos);
                }
            }
            else if(guiEvent.type == EventType.MouseDown && guiEvent.button == 1)
            {
                registeringInput = true;
                float minDistToAnchor = creator.anchorDiameter * .5f;
                int closestAnchorIndex = -1;

                for (int i = 0; i < path.NumPoints; i += 3)
                {
                    float dist = Vector2.Distance(mousePos, path[i]);
                    if (dist < minDistToAnchor)
                    {
                        minDistToAnchor = dist;
                        closestAnchorIndex = i;
                    }
                }

                if (closestAnchorIndex != -1)
                {
                    Undo.RecordObject(creator, "Delete segment");
                    path.DeleteSegment(closestAnchorIndex);
                }
            }

            if (guiEvent.type == EventType.MouseMove)
            {
                float minDistToSegment = segmentSelectDistThreshold;
                int newSelectedSegmentIndex = -1;

                for (int i = 0; i < path.NumSegments; i++)
                {
                    Vector2[] points = path.GetPointsInSegment(i);
                    float dist =
                        HandleUtility.DistancePointBezier(mousePos, points[0], points[3], points[1], points[2]);
                    if (dist < minDistToSegment)
                    {
                        minDistToSegment = dist;
                        newSelectedSegmentIndex = i;
                    }
                }

                if (newSelectedSegmentIndex != selectedSegmentIndex)
                {
                    selectedSegmentIndex = newSelectedSegmentIndex;
                    HandleUtility.Repaint();
                }
            }
        }

        HandleUtility.AddDefaultControl(0);
    }

    void Draw()
    {
        for (int i = 0; i < path.NumSegments; i++)
        {
            Vector2[] points = path.GetPointsInSegment(i);
            if (creator.displayControlPoints)
            {
                Handles.color = Color.black;
                Handles.DrawLine(points[1], points[0]);
                Handles.DrawLine(points[2], points[3]);
            }

            Color segmentColor = i == selectedSegmentIndex && Event.current.shift ? creator.selectedSegmentColor : creator.segmentColor;
            
            Handles.DrawBezier(points[0], points[3], points[1], points[2], segmentColor, null, 2);
        }

        for (int i = 0; i < path.NumPoints; i++)
        {
            if (i % 3 != 0 && !creator.displayControlPoints) continue;
            
            Handles.color = i % 3 == 0 ? creator.anchorColor : creator.controlColor;

            float handleSize = i % 3 == 0 ? creator.anchorDiameter : creator.controlDiameter;
            Vector2 newPos = Handles.FreeMoveHandle(path[i], Quaternion.identity, handleSize, Vector2.zero,
                Handles.CylinderHandleCap);
            if (path[i] != newPos)
            {
                Undo.RecordObject(creator, "Move Point");
                path.MovePoint(i, newPos);
            }
        }
    }
    
    private void OnEnable()
    {
        creator = (PathCreator) target;
        
        if (creator.path == null) creator.CreatePath();
    }
}
