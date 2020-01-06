using UnityEditor;
using UnityEngine;

public class GeometrySettingsWindow : EditorWindow
{
    private int _chunkSize = GeometryConsts.CHUNK_SIZE;
    private int _chunkHeight = GeometryConsts.CHUNK_HEIGHT;
    private int _viewDistance = GeometryConsts.VIEW_DISTANCE;

    [MenuItem("MindCraft/Geometry Settings")]
    static void Init()
    {
        GeometrySettingsWindow window = (GeometrySettingsWindow) GetWindow(typeof(GeometrySettingsWindow));
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Geometry settings", EditorStyles.boldLabel);

        _chunkSize = int.Parse(EditorGUILayout.TextField("Chunk width", _chunkSize.ToString()));
        _chunkHeight = int.Parse(EditorGUILayout.TextField("Chunk height", _chunkHeight.ToString()));
        _viewDistance = int.Parse(EditorGUILayout.TextField("View distance", _viewDistance.ToString()));

        if (GUILayout.Button("Update"))
        {
            GeometrySettings.RegenerateGeometryConsts(_chunkSize, _chunkHeight, _viewDistance);
        }
    }
}