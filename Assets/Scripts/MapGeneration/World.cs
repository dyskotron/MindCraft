using System.Collections.Generic;
using MapGeneration.Defs;
using UnityEngine;

namespace MapGeneration
{
    public class World : MonoBehaviour
    {
        public Transform Player;
        public Material Material;
        public VoxelDef[] voxelDefs;
        public BiomeDef biomeDef;
        public int _seed;

        private Dictionary<ChunkCoord, Chunk> _chunks = new Dictionary<ChunkCoord, Chunk>();
        private Vector3 _spawnPosition;
        private ChunkCoord _lastPlayerCoords;
        private bool _initialized;

        private void Start()
        {
            Random.InitState(_seed);
            
            Locator.World = this;

            _spawnPosition = new Vector3(0f,biomeDef.TerrainMin + biomeDef.TerrainHeight + 1,0f);
            
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

        public float GetTerainHeight(Vector3 position)
        {
           return Mathf.FloorToInt(biomeDef.TerrainMin + biomeDef.TerrainHeight * Noise.Get2DPerlin(new Vector2(position.x, position.z) ,0, biomeDef.TerrainScale));
        }

        public byte GetVoxel(Vector3 position)
        {
            var posY = Mathf.FloorToInt(position.y);
            
            // GLOBAL PASS
            
            if (posY < 0 || posY >= VoxelLookups.CHUNK_HEIGHT)
                return VoxelTypeByte.AIR;
            
            if (posY == 0)
                return VoxelTypeByte.HARD_ROCK;
            
            // BASIC PASS

            var terrainHeight = GetTerainHeight(position);
                
            byte voxelValue = 0;
            //everything higher then terrainHeight is air
            if (posY >= terrainHeight)
                return VoxelTypeByte.AIR;
            
            //top voxels are grass
            if (posY == terrainHeight - 1)
                voxelValue = VoxelTypeByte.DIRT_WITH_GRASS;
            //3 voxels under grass are dirt
            else if (posY >= terrainHeight - 4)
                voxelValue = VoxelTypeByte.DIRT;
            //rest is rock
            else
                voxelValue = VoxelTypeByte.ROCK;
            
            
            //LODES PASS
            if (voxelValue == VoxelTypeByte.ROCK)
            {
                foreach (var lode  in biomeDef.Lodes)
                {
                    if (posY > lode.MinHeight && posY < lode.MaxHeight)
                    {
                        var treshold = lode.Treshold;
                        switch (lode.ScaleTresholdByHeight)
                        {
                            case ScaleTresholdByHeight.HighestTop:
                                treshold *= (posY - lode.MinHeight) / (float)lode.HeightRange;
                                break;
                            case ScaleTresholdByHeight.HighestBottom:
                                treshold *= (lode.MaxHeight - posY) / (float)lode.HeightRange;
                                break;
                        }
                        
                        if (Noise.Get3DPerlin(position, lode.Offset, lode.Scale, treshold))
                            voxelValue = lode.BlockId;
                    }
                }
            }
            

            return voxelValue;
        }

        public bool CheckVoxel(float x, float y, float z)
        {
            var posX = Mathf.FloorToInt(x);    
            var posY = Mathf.FloorToInt(y);    
            var posZ = Mathf.FloorToInt(z);

            int chunkX = Mathf.FloorToInt(posX / (float)VoxelLookups.CHUNK_SIZE);
            int chunkY = Mathf.FloorToInt(posZ / (float)VoxelLookups.CHUNK_SIZE);

            _chunks.TryGetValue(new ChunkCoord(chunkX, chunkY), out Chunk chunk);

            return chunk != null && chunk.CheckVoxel(posX - chunkX * VoxelLookups.CHUNK_SIZE, posY, posZ - chunkY * VoxelLookups.CHUNK_SIZE);
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
            _chunks.TryGetValue(new ChunkCoord(x, y), out Chunk chunk);
            
            if (chunk != null)
                chunk.IsActive = false;    
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
            //update player height
            /*
            var playerPosition = Player.position;
            playerPosition.y = GetTerainHeight(Player.position);
            Player.position = playerPosition;
            */
            //dont update chunks atm
            //return;
            
            //update chunks
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