using UnityEngine;

namespace Bezier_Curve.Scripts
{
    public class BezierCurve : MonoBehaviour
    {
        public Vector3[] points;

        private void Reset()
        {
            points = new Vector3[]
            {
                new Vector3(1f, 0f, 0f),
                new Vector3(2f, 0f, 0f),
                new Vector3(3f, 0f, 0f),
                new Vector3(4f, 0f, 0f)
            };

        }

        public Vector3 GetPoint(float t) =>
            transform.TransformPoint(Bezier.GetPoint(points[0], points[1], points[2], points[3], t));
    
        public Vector3 GetVelocity (float t) =>
            transform.TransformPoint(Bezier.GetFirstDerivative(points[0], points[1], points[2], points[3], t)) -
            transform.position;

        public Vector3 GetDirection (float t) => GetVelocity(t).normalized;
    }
}
