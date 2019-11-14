using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MapGeneration
{
    [ExecuteInEditMode]
    public class Chunk : MonoBehaviour
    {
        public Vector3 Size;

        public MeshRenderer MeshRenderer;
        public MeshFilter MeshFilter;

        public AnimationCurve HorizonCurve;

        private const int FACES_PER_VERTEX = 6;
        private const int TRIANGLE_VERTICES_PER_FACE = 6;
        private const int VERTICES_PER_FACE = 4;

        //Chunk Generation
        private List<Vector3> vertices;
        private List<int> triangles;
        private List<Vector2> uvs;
        private bool[,,] voxelMap;

        private int currentVertexIndex;


        private void Start()
        {
            voxelMap = new bool[(int) Size.x, (int) Size.y, (int) Size.z];

            CreateMap();
            MeshFilter.mesh = CreateMesh();
        }

        private bool GetVoxelData(Vector3 position)
        {
            var x = Mathf.FloorToInt(position.x);
            var y = Mathf.FloorToInt(position.y);
            var z = Mathf.FloorToInt(position.z);

            return GetVoxelData(x, y, z);
        }


        private bool GetVoxelData(int x, int y, int z)
        {
            //TODO: specific checks for each direction, so we save useless checks
            if (x < 0 || y < 0 || z < 0 || x >= Size.x || y >= Size.y || z >= Size.z)
                return false;

            return voxelMap[x, y, z];
        }

        private void CreateMap()
        {
            for (var iX = 0; iX < Size.x; iX++)
            {
                for (var iZ = 0; iZ < Size.z; iZ++)
                {
                    //curve is just affecting distribution & range of values at this point
                    //will be much more usefull with perlin noise
                    var rand = HorizonCurve.Evaluate(Random.Range(0f, 1f));
                    var yMax = Mathf.Max(rand * Size.y, 1);
                    for (var iY = 0; iY < yMax; iY++)
                    {
#if DEV_BUILD
                    try
                    {
                        voxelMap[iX, iY, iZ] = true;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"<color=\"aqua\">Chunk.CreateMap() : Invalid Voxel Index => rand:{rand} iX:{iX} iY:{iY} iZ:{iZ}</color>");
                    }
#else
                        voxelMap[iX, iY, iZ] = true;
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

            for (var iX = 0; iX < Size.x; iX++)
            {
                for (var iZ = 0; iZ < Size.z; iZ++)
                {
                    for (var iY = 0; iY < Size.y; iY++)
                    {
                        if (GetVoxelData(iX, iY, iZ))
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
            //iterate faces
            for (int iF = 0; iF < FACES_PER_VERTEX; iF++)
            {
                //check neighbours
                if (GetVoxelData(position + VoxelDef.Neighbours[iF]))
                    continue;

                //iterate triangles
                for (int iV = 0; iV < TRIANGLE_VERTICES_PER_FACE; iV++)
                {
                    var vertexIndex = VoxelDef.indexToVertex[iV];

                    // each face needs just 4 vertices & UVs
                    if (iV < VERTICES_PER_FACE)
                    {
                        vertices.Add(position + VoxelDef.Vertices[VoxelDef.Triangles[iF, iV]]);
                        uvs.Add(VoxelDef.Uvs[iV]);
                    }

                    //we still need 6 triangle vertices tho
                    triangles.Add(currentVertexIndex + vertexIndex);
                }

                currentVertexIndex += VERTICES_PER_FACE;
            }
        }
    }
}