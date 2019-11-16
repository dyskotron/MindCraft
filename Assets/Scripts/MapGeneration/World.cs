using System.Collections.Generic;
using UnityEngine;

namespace MapGeneration
{
    public class World : MonoBehaviour
    {
        public Transform Player;
        public Material Material;
        public VoxelDef[] voxelDefs;

        private Dictionary<ChunkCoord, Chunk> _chunks = new Dictionary<ChunkCoord, Chunk>();
        private Vector3 _spawnPosition;
        private ChunkCoord _lastPlayerCoords;
        private bool _initialized;

        private void Start()
        {
            Locator.World = this;
            
            _spawnPosition = new Vector3(0f,VoxelLookups.CHUNK_HEIGHT + 1,0f);

            GenerateWorld();
            
            Player.position = _spawnPosition;
            _lastPlayerCoords = GetChunkCoordsByPosition(_spawnPosition);

            _initialized = true;
        }

        private void Update()
        {
            if(_initialized)
                UpdateView();
        }

        public byte GetVoxel(Vector3 position)
        {
            if (position.y < 0 || position.y >= VoxelLookups.CHUNK_HEIGHT)
                return Chunk.EMPTY_VOXEL;
            
            if (position.y == 0)
                return 1;
            
            if (position.y == VoxelLookups.CHUNK_HEIGHT - 1)
                return 3;
            
            return 2;
        }
        
        private void GenerateWorld()
        {
            //create chunks
            for (var x = -VoxelLookups.VIEW_DISTANCE_IN_CHUNKS; x < VoxelLookups.VIEW_DISTANCE_IN_CHUNKS; x++)
            {
                for (var y = -VoxelLookups.VIEW_DISTANCE_IN_CHUNKS; y < VoxelLookups.VIEW_DISTANCE_IN_CHUNKS; y++)
                {   
                    CreateChunk(new ChunkCoord(x, y));
                }   
            }    
        }

        private void ShowChunk(int x, int y)
        {
            var coords = new ChunkCoord(x, y);
            if(!_chunks.ContainsKey(coords))
                CreateChunk(coords);
            else if (!_chunks[coords].IsActive)
            {
                _chunks[coords].IsActive = true;
            } 
        }

        private void HideChunk(int x, int y)
        {
            _chunks[new ChunkCoord(x, y)].IsActive = false;    
        }

        private void CreateChunk(ChunkCoord coords)
        {
            _chunks[coords] = new Chunk(coords);    
        }
        
        private ChunkCoord GetChunkCoordsByPosition(Vector3 position)
        {
            return new ChunkCoord(Mathf.FloorToInt(position.x / VoxelLookups.CHUNK_SIZE), 
                                  Mathf.FloorToInt(position.z / VoxelLookups.CHUNK_SIZE));
        }

        private void UpdateView()
        {
            var newCoords = GetChunkCoordsByPosition(Player.position);
            
            if (newCoords == _lastPlayerCoords)
                return;
            
            var lastMinX = _lastPlayerCoords.X - VoxelLookups.VIEW_DISTANCE_IN_CHUNKS;
            var lastMinY = _lastPlayerCoords.Y - VoxelLookups.VIEW_DISTANCE_IN_CHUNKS;
            var lastMaxX = _lastPlayerCoords.X + VoxelLookups.VIEW_DISTANCE_IN_CHUNKS;
            var lastMaxY = _lastPlayerCoords.Y + VoxelLookups.VIEW_DISTANCE_IN_CHUNKS;
            
            var newMinX = newCoords.X - VoxelLookups.VIEW_DISTANCE_IN_CHUNKS;
            var newMinY = newCoords.Y - VoxelLookups.VIEW_DISTANCE_IN_CHUNKS;
            var newMaxX = newCoords.X + VoxelLookups.VIEW_DISTANCE_IN_CHUNKS;
            var newMaxY = newCoords.Y + VoxelLookups.VIEW_DISTANCE_IN_CHUNKS;
            
            //TODO: merged loops so everything is checked in one iteration
            
            //show new chunks
            for (var x = newMinX; x < newMaxX; x++)
            {
                for (var y = newMinY; y < newMaxY; y++)
                {
                    //except old cords
                    if(x >= lastMinX && x < lastMaxX && y >= lastMinY && y < lastMaxY)
                        continue;
                    
                    ShowChunk(x, y);
                }   
            }   
            
            //hide all old chunks
            for (var x = lastMinX; x < lastMaxX; x++)
            {
                for (var y = lastMinY; y < lastMaxY; y++)
                {
                    //except new ones
                    if(x >= newMinX && x < newMaxX && y >= newMinY && y < newMaxY)
                        continue;
                    
                    HideChunk(x, y);
                }   
            } 

            _lastPlayerCoords = newCoords;
        }
    }
}