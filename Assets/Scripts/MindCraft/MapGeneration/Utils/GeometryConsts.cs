using UnityEngine;
using System.Collections;

public class GeometryConsts
{
    public const int FACES_PER_VOXEL = 6;
    public const int TRIANGLE_INDICES_PER_FACE = 6;
    public const int VERTICES_PER_FACE = 4;
    
    public const int MAX_OCTAVES = 10;
    public const int CHUNK_SIZE = 8; 
    public const int CHUNK_SIZE_POW2 = 64; 
    public const int CHUNK_HEIGHT = 128; 
    public const int VOXELS_PER_CHUNK = 8192; 
    public const int VOXELS_PER_CLUSTER = 73728; 
    public const int VIEW_DISTANCE = 30; 
    public const int VIEW_DISTANCE_IN_CHUNKS = 4; 
    
    public const float LIGHT_FALL_OFF = 0.2f;
    public const float MIN_LIGHT = 0.15f;
    
    public const int DIFFUSE_LIGHTS_MARGIN = 5;
    public const int LIGHTS_CLUSTER_MIN = -5;
    public const int LIGHTS_CLUSTER_MAX = 12;
    
    public const int MULTIMAP_CENTER_OFFSET = 32768; 
    
    // Helper consts for bitwise operations
    public const int CHUNK_SIZE_LOG2 = 3; 
    public const int VOXELS_PER_CHUNK_LOG2 = 13; 
    public const int MODULO_BY_CHUNK_SIZE = 7; 
    public const int MODULO_BY_SIZE_TIMES_HEIGHT = 1023; 
    
}
