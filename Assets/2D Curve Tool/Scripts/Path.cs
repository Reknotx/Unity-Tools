using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Path
{
    [SerializeField, HideInInspector]
    private List<Vector2> points;

    [SerializeField, HideInInspector]
    private bool _isClosed;

    [SerializeField, HideInInspector]
    private bool _autoSetControlPoints;

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

    public bool IsClosed
    {
        get => _isClosed;

        set
        {
            if (_isClosed == value) return;
            
            _isClosed = value;
            if (_isClosed)
            {
                points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
                points.Add(points[0] * 2 - points[1]);
                if (AutoSetControlPoints)
                {
                    AutoSetAnchorCP(0);
                    AutoSetAnchorCP(points.Count - 3);
                }
            }
            else
            {
                points.RemoveRange(points.Count - 2, 2);
                if (AutoSetControlPoints) AutoSetStartAndEndControls();

            }
        }
    }

    public bool AutoSetControlPoints
    {
        get => _autoSetControlPoints;

        set
        {
            if (_autoSetControlPoints == value) return;
            
            _autoSetControlPoints = value;
            
            if (_autoSetControlPoints) AutoSetAllCP();
        }
    }

    public int NumPoints => points.Count;

    public int NumSegments => points.Count / 3;

    public void AddSegment(Vector2 anchorPos)
    {
        points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
        points.Add((points[points.Count - 1] + anchorPos) * 0.5f);
        points.Add(anchorPos);
        
        if (AutoSetControlPoints) AutoSetAllAffectedControlPoints(points.Count - 1);
    }

    public void SplitSegment(Vector2 anchorPos, int segmentIndex)
    {
        points.InsertRange(segmentIndex * 3 + 2, new[] {Vector2.zero, anchorPos, Vector2.zero});

        if (AutoSetControlPoints) AutoSetAllAffectedControlPoints(segmentIndex * 3 + 3);
        else AutoSetAnchorCP(segmentIndex * 3 + 3);
    }

    public void DeleteSegment(int anchorIndex)
    {
        if (NumSegments <= 2 && (_isClosed || NumSegments <= 1)) return;
        if (anchorIndex == 0)
        {
            if (_isClosed)
            {
                points[points.Count - 1] = points[2];
            }

            points.RemoveRange(0, 3);
        }
        else if (anchorIndex == points.Count - 1 && _isClosed)
        {
            points.RemoveRange(anchorIndex - 2, 3);
        }
        else
        {
            points.RemoveRange(anchorIndex - 1, 3);
        }
    }

    public Vector2[] GetPointsInSegment(int i)
    {
        return new[]
        {
            points[i * 3],
            points[i * 3 + 1],
            points[i * 3 + 2],
            points[LoopIndex(i * 3 + 3)]
        };
    }

    public void MovePoint(int i, Vector2 pos)
    {
        Vector2 deltaMove = pos - points[i];

        if (i % 3 == 0 || !AutoSetControlPoints)
        {
            points[i] = pos;

            if (AutoSetControlPoints)
            {
                AutoSetAllAffectedControlPoints(i);
            }
            else
            {
                if (i % 3 == 0)
                {
                    if (i + 1 < points.Count || _isClosed) points[LoopIndex(i + 1)] += deltaMove;

                    if (i - 1 >= 0 || _isClosed) points[LoopIndex(i - 1)] += deltaMove;
                }
                else
                {
                    bool nextPointIsAnchor = (i + 1) % 3 == 0;
                    int correspondingControlIndex = nextPointIsAnchor ? i + 2 : i - 2;
                    int anchorIndex = nextPointIsAnchor ? i + 1 : i - 1;

                    if (correspondingControlIndex >= 0 && correspondingControlIndex < points.Count || _isClosed)
                    {
                        int loopedAnchorIndex = LoopIndex(anchorIndex);
                        int loopedCorrespondingCI = LoopIndex(correspondingControlIndex);

                        float dist = (points[loopedAnchorIndex] - points[loopedCorrespondingCI]).magnitude;
                        Vector2 dir = (points[loopedAnchorIndex] - pos).normalized;
                        points[loopedCorrespondingCI] = points[loopedAnchorIndex] + dir * dist;
                    }
                }
            }
        }
    }

    public Vector2[] CalculateEvenlySpacedPoints(float spacing, float resolution = 1f)
    {
        List<Vector2> evenlySpacedPoints = new List<Vector2>();
        evenlySpacedPoints.Add(points[0]);
        Vector2 previousPoint = points[0];
        float distSinceLastEvenPoint = 0f;

        for (int segmentIndex = 0; segmentIndex < NumSegments; segmentIndex++)
        {
            Vector2[] p = GetPointsInSegment(segmentIndex);
            
            float controlNetLength = Vector2.Distance(p[0], p[1]) + Vector2.Distance(p[1], p[2]) +
                                     Vector2.Distance(p[2], p[3]);
            float estimatedCurveLength = Vector2.Distance(p[0], p[3]) + controlNetLength / 2f;
            int divisions = Mathf.CeilToInt(estimatedCurveLength * resolution * 10); 
            
            float t = 0;
            while (t <= 1)
            {
                t += 1f / divisions;
                Vector2 pointOnCurve = Bezier.EvaluateCubic(p[0], p[1], p[2], p[3], t);
                distSinceLastEvenPoint += Vector2.Distance(previousPoint, pointOnCurve);

                while (distSinceLastEvenPoint >= spacing)
                {
                    float overshootDist = distSinceLastEvenPoint - spacing;
                    Vector2 newEvenlySpacedPoint = pointOnCurve + (previousPoint - pointOnCurve).normalized * overshootDist;
                    evenlySpacedPoints.Add(newEvenlySpacedPoint);
                    distSinceLastEvenPoint = overshootDist;
                    previousPoint = newEvenlySpacedPoint;
                }
                previousPoint = pointOnCurve;
            }
        }
        
        return evenlySpacedPoints.ToArray();
    }

    void AutoSetAllAffectedControlPoints(int updatedAnchorIndex)
    {
        for (int i = updatedAnchorIndex - 3; i < updatedAnchorIndex + 3; i += 3)
        {
            if (i >= 0 && i < points.Count || _isClosed) AutoSetAnchorCP(LoopIndex(i));
        }
        
        AutoSetStartAndEndControls();
    }
    
    
    void AutoSetAllCP()
    {
        for (int i = 0; i < points.Count; i += 3) AutoSetAnchorCP(i);
        
        AutoSetStartAndEndControls();
    }

    void AutoSetAnchorCP(int anchorIndex)
    {
        Vector2 anchorPos = points[anchorIndex];
        Vector2 dir = Vector2.zero;
        float[] neighborDistances = new float[2];

        if (anchorIndex - 3 >= 0 || _isClosed)
        {
            Vector2 offset = points[LoopIndex(anchorIndex - 3)] - anchorPos;
            dir += offset.normalized;
            neighborDistances[0] = offset.magnitude;
        }
        if (anchorIndex + 3 >= 0 || _isClosed)
        {
            Vector2 offset = points[LoopIndex(anchorIndex + 3)] - anchorPos;
            dir -= offset.normalized;
            neighborDistances[1] = -offset.magnitude;
        }

        dir.Normalize();

        for (int i = 0; i < 2; i++)
        {
            int controlIndex = anchorIndex + i * 2 - 1;
            if (controlIndex >= 0 && controlIndex < points.Count || _isClosed)
            {
                points[LoopIndex(controlIndex)] = anchorPos + dir * neighborDistances[i] * .5f;
            }
        }
    }

    void AutoSetStartAndEndControls()
    {
        if (_isClosed) return;

        points[1] = (points[0] + points[2]) * .5f;
        points[points.Count - 2] = (points[points.Count - 1] + points[points.Count - 3]) * .5f;
    }
    
    
    int LoopIndex(int i) => (i + points.Count) % points.Count;
}
