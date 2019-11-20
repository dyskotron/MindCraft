using System.Collections.Generic;
using System.Diagnostics;
using MindCraft;
using MindCraft.Data.Defs;
using MindCraft.GameObjects;
using MindCraft.MapGeneration.Lookup;
using MindCraft.Model;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace MindCraft.MapGeneration
{
    public class World : MonoBehaviour
    {
        public WorldModel WorldModel;
        
        public Transform Player;
        public Material Material;
        [FormerlySerializedAs("PlaceVoxelMaterial")] public Material PlaceBlockMaterial;
        [FormerlySerializedAs("UtilMaterial")] public Material MineMaterial;
        [FormerlySerializedAs("VoxelDefs")] public BlockDef[] blockDefs;
        public BiomeDef BiomeDef;
        public int Seed;

        [Header("Debug Params")] public bool DebugChunksMaterisl;
        public Material DebugMaterial;
        public Text DebugText;

        private Dictionary<ChunkCoord, Chunk> _chunks = new Dictionary<ChunkCoord, Chunk>();
        private Vector3 _spawnPosition;
        private ChunkCoord _lastPlayerCoords;
        private bool _initialized;
        
        
        private Stopwatch _watch;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Random.InitState(Seed);
            
            Camera.main.nearClipPlane = 0.01f;
            Camera.main.farClipPlane = VoxelLookups.VIEW_DISTANCE;
            
            Locator.World = this;
            Locator.WorldModel = WorldModel = new WorldModel();
            Locator.TextureLookup = new TextureLookup();
            Locator.TextureLookup.PostConstruct();

            _watch = new Stopwatch();
            _watch.Start();
            GenerateWorld();
            _watch.Stop();

            //place player few blocks above terrain
            _spawnPosition = new Vector3(0f, WorldModel.GetTerrainHeight(0,0) + 5, 0f);

            Player.position = _spawnPosition;
            _lastPlayerCoords = WorldModelHelper.GetChunkCoordsFromWorldPosition(_spawnPosition);

            _initialized = true;

            //var playerScript = Player.GetComponent<Player>();
            //playerScript.Init();
        }

        private void Update()
        {
            if (_initialized)
                UpdateView();
        }

        public Chunk GetChunk(ChunkCoord coords)
        {
            return _chunks[coords];
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
            if (!_chunks.ContainsKey(coords))
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
            var map = WorldModel.CreateChunkMap(coords);
            _chunks[coords] = new Chunk();
            _chunks[coords].Init(coords);
            _chunks[coords].UpdateChunkMesh(map);
        }

        private void UpdateView()
        {
            //update chunks
            var newCoords = WorldModelHelper.GetChunkCoordsFromWorldPosition(Player.position);

            //debug text
            if (Time.time > 1)
            {
                DebugText.text = $"Chunk Coords - X:{newCoords.X} Z:{newCoords.Y}\n" +
                                 $"Player Position: {Player.position}\n" +
                                 $"{Chunk.MAP_ELAPSED_TOTAL: 0.0000}\n" +
                                 $"{Chunk.MESH_ELAPSED_TOTAL: 0.0000}\n" +
                                 $"{Chunk.MAP_ELAPSED_TOTAL / Chunk.CHUNKS_TOTAL: 0.00000}\n" +
                                 $"{Chunk.MESH_ELAPSED_TOTAL / Chunk.CHUNKS_TOTAL: 0.00000}\n" +
                                 $"{_watch.Elapsed.TotalSeconds: 0.00000}\n";
            }

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
                    if (x >= lastMinX && x < lastMaxX && y >= lastMinY && y < lastMaxY)
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
                    if (x >= newMinX && x < newMaxX && y >= newMinY && y < newMaxY)
                        continue;

                    HideChunk(x, y);
                }
            }

            _lastPlayerCoords = newCoords;
        }

        public Material GetMaterial(ChunkCoord coords)
        {
            if (!DebugChunksMaterisl)
                return Material;

            return (coords.X + coords.Y) % 2 == 0 ? Material : DebugMaterial;
        }
    }
}