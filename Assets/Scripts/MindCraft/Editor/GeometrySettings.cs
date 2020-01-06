//Author: APMIX
//Put this in Assets/Editor Folder

using System.IO;
using Unity.Mathematics;
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
        GeometrySettingsWindow window = (GeometrySettingsWindow)GetWindow(typeof(GeometrySettingsWindow));
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

public class GeometrySettings
{
    public static void RegenerateGeometryConsts(int chunkSize, int chunkHeight, int viewDistance)
    {
        string copyPath = "Assets/Scripts/MindCraft/MapGeneration/Utils/GeometryConsts.cs";
        Debug.Log("Creating Classfile: " + copyPath);

        var chunkSizePow2 = chunkSize * chunkSize;
        var voxelsPerChunk = chunkSizePow2 * chunkHeight;
        var viewDistanceInChunks = Mathf.CeilToInt(viewDistance / (float)chunkSize);

        //CAN'T BE BIGGER THAN CHUNK_SIZE! -
        //TODO: calculate automatically from light params and chunk size
        var diffuseLightsMargin = 5;
        var lightClusterMin = -diffuseLightsMargin;
        var lightClusterMax = chunkSize + diffuseLightsMargin - 1;
        
        //Bitwise stuff
        var chunkSizeLog2 = (int)math.log2(chunkSize);
        var voxelsPerChunkLog2 = (int)math.log2(voxelsPerChunk);
        var moduloByChunkSize = chunkSize - 1;
        var moduloBySizeTimesHeight = chunkSize * chunkHeight - 1;
        
    //index of chunk in the center of concenated arrays we send to jobs that needs to know about neighbours
    //center chunk is what we care about, rest is just to get surrounding data
    var multimapCenterOffset = 4 * voxelsPerChunk;
        

        using (StreamWriter outfile = new StreamWriter(copyPath))
        {
            outfile.WriteLine("using UnityEngine;");
            outfile.WriteLine("using System.Collections;");
            outfile.WriteLine("");
            outfile.WriteLine("public class GeometryConsts");
            outfile.WriteLine("{");
            outfile.WriteLine("    public const int FACES_PER_VOXEL = 6;");
            outfile.WriteLine("    public const int TRIANGLE_INDICES_PER_FACE = 6;");
            outfile.WriteLine("    public const int VERTICES_PER_FACE = 4;");
            outfile.WriteLine("    ");
            outfile.WriteLine("    public const int MAX_OCTAVES = 10;");
            outfile.WriteLine($"    public const int CHUNK_SIZE = {chunkSize}; ");
            outfile.WriteLine($"    public const int CHUNK_SIZE_POW2 = {chunkSizePow2}; ");
            outfile.WriteLine($"    public const int CHUNK_HEIGHT = {chunkHeight}; ");
            outfile.WriteLine($"    public const int VOXELS_PER_CHUNK = {voxelsPerChunk}; ");
            outfile.WriteLine($"    public const int VOXELS_PER_CLUSTER = {voxelsPerChunk * 9}; ");
            outfile.WriteLine($"    public const int VIEW_DISTANCE = {viewDistance}; ");
            outfile.WriteLine($"    public const int VIEW_DISTANCE_IN_CHUNKS = {viewDistanceInChunks}; ");
            outfile.WriteLine("    ");
            outfile.WriteLine("    public const float LIGHT_FALL_OFF = 0.2f;");
            outfile.WriteLine("    public const float MIN_LIGHT = 0.15f;");
            outfile.WriteLine("    ");
            outfile.WriteLine($"    public const int DIFFUSE_LIGHTS_MARGIN = {diffuseLightsMargin};");
            outfile.WriteLine($"    public const int LIGHTS_CLUSTER_MIN = {lightClusterMin};");
            outfile.WriteLine($"    public const int LIGHTS_CLUSTER_MAX = {lightClusterMax};");
            outfile.WriteLine("    ");
            outfile.WriteLine($"    public const int MULTIMAP_CENTER_OFFSET = {multimapCenterOffset}; ");
            outfile.WriteLine("    ");
            outfile.WriteLine("    // Helper consts for bitwise operations");
            outfile.WriteLine($"    public const int CHUNK_SIZE_LOG2 = {chunkSizeLog2}; ");
            outfile.WriteLine($"    public const int VOXELS_PER_CHUNK_LOG2 = {voxelsPerChunkLog2}; ");
            outfile.WriteLine($"    public const int MODULO_BY_CHUNK_SIZE = {moduloByChunkSize}; ");
            outfile.WriteLine($"    public const int MODULO_BY_SIZE_TIMES_HEIGHT = {moduloBySizeTimesHeight}; ");
            outfile.WriteLine("    ");
            outfile.WriteLine("}");
        } //File written

        AssetDatabase.Refresh();
    }
}

