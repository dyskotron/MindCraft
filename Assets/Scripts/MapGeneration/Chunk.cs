using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MapGeneration
{
    [ExecuteInEditMode]
    public class Chunk : MonoBehaviour
    {
        public MeshRenderer MeshRenderer;
        public MeshFilter MeshFilter;

        public AnimationCurve HorizonCurve;

        private const int EMPTY_VOXEL = 0;
        private const int FACES_PER_VERTEX = 6;
        private const int TRIANGLE_VERTICES_PER_FACE = 6;
        private const int VERTICES_PER_FACE = 4;
        
        //texture consts
        private const int BLOCKS_PER_TEXTURE = 4;
        private const float NORMALIZED_BLOCK_SIZE = 1f / BLOCKS_PER_TEXTURE;
        
        private const float TEXTURE_CORDS_FIX = 0.001f;
        private static readonly Vector2 CORDS_FIX_OFFSET = new Vector2(TEXTURE_CORDS_FIX, TEXTURE_CORDS_FIX);
        private const float FIXED_BLOCK_SIZE = NORMALIZED_BLOCK_SIZE - 2 * TEXTURE_CORDS_FIX;

        //Chunk Generation
        private List<Vector3> vertices;
        private List<int> triangles;
        private List<Vector2> uvs;
        private byte[,,] voxelMap;

        private int currentVertexIndex;


        private void Start()
        {
            voxelMap = new byte[ VoxelLookups.CHUNK_SIZE, VoxelLookups.CHUNK_HEIGHT, VoxelLookups.CHUNK_SIZE];

            CreateMap();
            MeshFilter.mesh = CreateMesh();
        }

        private void CreateMap()
        {
            for (var iX = 0; iX < VoxelLookups.CHUNK_SIZE; iX++)
            {
                for (var iZ = 0; iZ < VoxelLookups.CHUNK_SIZE; iZ++)
                {
                    //curve is just affecting distribution & range of values at this point
                    //will be much more usefull with perlin noise
                    var rand = HorizonCurve.Evaluate(Random.Range(0f, 1f));
                    var yMax = (int)Math.Max(rand * VoxelLookups.CHUNK_HEIGHT, 1);
                    for (var iY = 0; iY <= yMax; iY++)
                    {
#if DEV_BUILD
                        try
                        {
#endif

                        if(iY == 0)
                            voxelMap[iX, iY, iZ] = 1;
                        else if(iY == yMax)
                            voxelMap[iX, iY, iZ] = 3;
                        else
                            voxelMap[iX, iY, iZ] = 2;
                        
#if DEV_BUILD
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"<color=\"aqua\">Chunk.CreateMap() : Invalid Voxel Index => rand:{rand} iX:{iX} iY:{iY} iZ:{iZ}</color>");
                        }
#endif
                    }
                }
            }
        }

        private Mesh CreateMesh()
        {
            vertices = new List<Vector3>();
            triangles = new List<int>();
            uvs = new List<Vector2>();

            currentVertexIndex = 0;

            for (var iX = 0; iX < VoxelLookups.CHUNK_SIZE; iX++)
            {
                for (var iZ = 0; iZ < VoxelLookups.CHUNK_SIZE; iZ++)
                {
                    for (var iY = 0; iY < VoxelLookups.CHUNK_HEIGHT; iY++)
                    {
                        if (GetVoxelData(iX, iY, iZ) != EMPTY_VOXEL)
                            AddVoxel(new Vector3(iX, iY, iZ));
                    }
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();

            return mesh;
        }

        private void AddVoxel(Vector3 position)
        {
            var voxelId = GetVoxelData(position);
            var voxelType = Locator.World.voxelDefs[voxelId];
            
            //iterate faces
            for (int iF = 0; iF < FACES_PER_VERTEX; iF++)
            {
                //check neighbours
                if (GetVoxelData(position + VoxelLookups.Neighbours[iF]) != EMPTY_VOXEL)
                    continue;
                
                //iterate triangles
                for (int iV = 0; iV < TRIANGLE_VERTICES_PER_FACE; iV++)
                {
                    var vertexIndex = VoxelLookups.indexToVertex[iV];

                    // each face needs just 4 vertices & UVs
                    if (iV < VERTICES_PER_FACE)
                    {
                        vertices.Add(position + VoxelLookups.Vertices[VoxelLookups.Triangles[iF, iV]]);
                    }

                    //we still need 6 triangle vertices tho
                    triangles.Add(currentVertexIndex + vertexIndex);
                }

                AddTexture(voxelType.FaceTextures[iF]);
                currentVertexIndex += VERTICES_PER_FACE;
            }
        }

        private void AddTexture(int textureId)
        {
            float x = (textureId % BLOCKS_PER_TEXTURE) * NORMALIZED_BLOCK_SIZE;
            float y = (int)(textureId / BLOCKS_PER_TEXTURE) * NORMALIZED_BLOCK_SIZE;
            
            uvs.Add(CORDS_FIX_OFFSET + new Vector2(x, y));
            uvs.Add(CORDS_FIX_OFFSET + new Vector2(x, y + FIXED_BLOCK_SIZE));
            uvs.Add(CORDS_FIX_OFFSET + new Vector2(x + FIXED_BLOCK_SIZE, y));
            uvs.Add(CORDS_FIX_OFFSET + new Vector2(x + FIXED_BLOCK_SIZE, y + FIXED_BLOCK_SIZE));
        }
        
        
        private byte GetVoxelData(Vector3 position)
        {
            var x = Mathf.FloorToInt(position.x);
            var y = Mathf.FloorToInt(position.y);
            var z = Mathf.FloorToInt(position.z);

            return GetVoxelData(x, y, z);
        }


        private byte GetVoxelData(int x, int y, int z)
        {
            //TODO: specific checks for each direction, so we save useless checks
            if (x < 0 || y < 0 || z < 0 || x >= VoxelLookups.CHUNK_SIZE || y >= VoxelLookups.CHUNK_HEIGHT || z >= VoxelLookups.CHUNK_SIZE)
                return 0;

            return voxelMap[x, y, z];
        }
    }
}