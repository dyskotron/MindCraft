using System.Collections.Generic;
using MindCraft.Data;
using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Utils;
using MindCraft.Model;
using strange.framework.api;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace MindCraft.View
{
    public class ChunksRenderer
    {
        [Inject] public IInstanceProvider InstanceProvider { get; set; }
        [Inject] public IWorldModel WorldModel { get; set; }
        
        private Dictionary<ChunkCoord, Chunk> _chunks = new Dictionary<ChunkCoord, Chunk>();
        private List<Chunk> _chunkPool = new List<Chunk>();
        
        public void UpdateChunkMesh(ChunkCoord coords, NativeArray<byte> chunkMap)
        {
            _chunks[coords].UpdateChunkMesh(chunkMap);
        }
        
        public void GenerateChunksAroundPlayer(ChunkCoord coords)
        {
            //render all chunks within view distance
            foreach (var pos in MapBoundsLookup.ChunkGeneration)
            {
                CreateChunk(new ChunkCoord(pos)); 
            } 
        }

        public void UpdateChunksAroundPlayer(ChunkCoord newCoords)
        {
            //TODO ONLY SetActive(false); for chunks within MapBoundsLookup.REMOVE_RING_OFFSET
            //current HideChunk method moving chunk to the pool should be calle only for those out of remove ring.
            
            //hide chunks out of sight
            foreach (var position in MapBoundsLookup.ChunkRemove)
            {
                HideChunk(newCoords.X + position.x, newCoords.Y + position.y);    
            }

            //show chunks coming into view distance
            foreach (var position in MapBoundsLookup.ChunkAdd)
            {
                ShowChunk(newCoords.X + position.x, newCoords.Y + position.y);
            }
        }
        
        public void CreateChunk(ChunkCoord coords)
        {
            var map = WorldModel.GetMapByChunkCoords(coords);

            Chunk chunk;
            if (_chunkPool.Count > 0)
            {
                chunk = _chunkPool[0];
                _chunkPool.RemoveAt(0);
            }
            else
            {
                chunk = InstanceProvider.GetInstance<Chunk>();    
            }
            
            chunk.Init(coords);
            chunk.UpdateChunkMesh(map);

            _chunks[coords] = chunk;
        }
        
        private void ShowChunk(int x, int y)
        {
            var coords = new ChunkCoord(x, y);
            
            if (!_chunks.ContainsKey(coords))
                CreateChunk(coords);
            else if (!_chunks[coords].IsActive)
                _chunks[coords].IsActive = true;
        }
        
        private void HideChunk(int x, int y)
        {
            var coords = new ChunkCoord(x, y);
            _chunks.TryGetValue(coords, out Chunk chunk);

            if (chunk != null)
            {
                _chunkPool.Add(_chunks[coords]);
                _chunks.Remove(coords);
            }
        }
        
        
        
        public void UpdateChunkMesh(byte[,,] map)
        {
            var _map = map;

            /*
            currentVertexIndex = 0;
            vertices.Clear();
            triangles.Clear();
            uvs.Clear();
            */
            
            for (var iX = 0; iX < VoxelLookups.CHUNK_SIZE; iX++)
            {
                for (var iZ = 0; iZ < VoxelLookups.CHUNK_SIZE; iZ++)
                {
                    for (var iY = 0; iY < VoxelLookups.CHUNK_HEIGHT; iY++)
                    {
                        var type = _map[iX, iY, iZ];
                        //if (type != BlockTypeByte.AIR)
                            //TODO: AddVoxel(type, iX, iY, iZ);
                    }
                }
            }
            
            /*
            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();

            _meshFilter.mesh = mesh;
            */
        }
        /*
        private void AddVoxel(byte voxelId, int x, int y, int z)
        {
            //ArrayHelper.To3D(index, out int x, out int y, out int z);
            
            
            var position = new Vector3(x, y, z);
            //iterate faces
            for (int iF = 0; iF < Chunk.FACES_PER_VOXEL; iF++)
            {
                var neighbour = VoxelLookups.Neighbours[iF];

                //check neighbours
                / *
                var blockDef = BlockDefs.GetDefinitionById((BlockTypeId) GetVoxelData(x + neighbour.x, y + neighbour.y, z + neighbour.z));
                if (!blockDef.IsTransparent)
                    continue;
                    * /

                //iterate triangles
                for (int iV = 0; iV < TRIANGLE_INDICES_PER_FACE; iV++)
                {
                    var vertexIndex = VoxelLookups.indexToVertex[iV];

                    // each face needs just 4 vertices & UVs
                    if (iV < VERTICES_PER_FACE)
                    {
                        vertices.Add(position + VoxelLookups.Vertices[VoxelLookups.Triangles[iF, iV]]);
                        uvs.Add(TextureLookup.WorldUvLookup[voxelId, iF, iV]);
                    }

                    //we still need 6 triangle vertices tho
                    triangles.Add(currentVertexIndex + vertexIndex);
                }

                currentVertexIndex += VERTICES_PER_FACE;
            }
        }*/
    }
}