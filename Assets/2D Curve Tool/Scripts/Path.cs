using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Path
{
    [SerializeField, HideInInspector]
    private List<Vector2> points;

    public Path(Vector2 center)
    {
        points = new List<Vector2>()
        {
            center + Vector2.left,
            center + (Vector2.left + Vector2.up) * 0.5f,
            center + (Vector2.right + Vector2.down) * .5f,
            center + Vector2.right
        };
    }

    public Vector2 this[int i] => points[i];

    public int NumPoints => points.Count;

    public int NumSegments => (points.Count - 4) / 3 + 1;

    public void AddSegment(Vector2 anchorPos)
    {
        points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
        points.Add((points[points.Count - 1] + anchorPos) * 0.5f);
        points.Add(anchorPos);
    }

    public Vector2[] GetPointsInSegment(int i)
    {
        return new Vector2[]
        {
            points[i * 3],
            points[i * 3 + 1],
            points[i * 3 + 2],
            points[i * 3 + 3]
        };
    }

    public void MovePoint(int i, Vector2 pos)
    {
        points[i] = pos;
    }
}
