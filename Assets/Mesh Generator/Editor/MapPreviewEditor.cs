using UnityEditor;
using UnityEngine;

namespace Mesh_Generator.Scripts.Editor
{
    [CustomEditor(typeof(MapPreview))]
    public class MapPreviewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            MapPreview mapPreview = (MapPreview) target;
            if (DrawDefaultInspector() && mapPreview.autoUpdate) mapPreview.DrawMapInEditor();

            if (GUILayout.Button("Generate")) mapPreview.DrawMapInEditor();
        }
    }
}
