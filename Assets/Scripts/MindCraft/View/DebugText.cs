using MindCraft.Controller.Fsm;
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

            Label.text = $"START TOTAL:{GameAppState.GENERATION_TIME_TOTAL: 0.00000}\n" +
                         $"UPDATE DATA{0  : 0.00000}\n";
        }
    }
}