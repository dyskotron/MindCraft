using MindCraft.Controller.Fsm;
using MindCraft.GameObjects;
using UnityEngine;
using UnityEngine.UI;

namespace MindCraft.View
{
    public class DebugText : MonoBehaviour
    {
        public Text Label;
        
        private void Update()
        {
            
            //$"Chunk Coords - X:{newCoords.X} Z:{newCoords.Y}\n" +
            //$"Player Position: {Player.position}\n" +
            
            
            Label.text = $"{Chunk.MAP_ELAPSED_TOTAL: 0.0000}\n" +
                         $"{Chunk.MESH_ELAPSED_TOTAL: 0.0000}\n" +
                         $"{Chunk.MAP_ELAPSED_TOTAL / Chunk.CHUNKS_TOTAL: 0.00000}\n" +
                         $"{Chunk.MESH_ELAPSED_TOTAL / Chunk.CHUNKS_TOTAL: 0.00000}\n" +
                         $"{GameAppState.GENERATION_TIME_TOTAL: 0.00000}\n";
        }
    }
}