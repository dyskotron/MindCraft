using MindCraft.Model;
using UnityEngine;

namespace MindCraft.View
{
    public interface IWorldRaycaster
    {
        bool IsHit { get; }
        Vector3Int HitPosition { get; }
        Vector3Int LastPosition { get; }
        bool Raycast(Vector3 origin, Vector3 direction);
    }

    public class WorldRaycaster : IWorldRaycaster
    {
        [Inject] public IWorldModel WorldModel { get; set; }

        public bool IsHit { get; private set; }
        public Vector3Int HitPosition { get; private set; }
        public Vector3Int LastPosition { get; private set; }

        private float CheckIncrement =  0.01f;
        private float Reach =  10;

        public bool Raycast(Vector3 origin, Vector3 direction)
        {
            float step = CheckIncrement;
            Vector3Int lastPos = new Vector3Int();

            while (step < Reach)
            {
                Vector3 position = origin + direction * step;
                var newPosition = new Vector3Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y), Mathf.FloorToInt(position.z));

                if (WorldModel.CheckVoxelOnGlobalXyz(position.x, position.y, position.z))
                {
                    HitPosition = newPosition;
                    LastPosition = lastPos;
                    
                    return IsHit = true;
                }

                lastPos = newPosition;
                step += CheckIncrement;
            }

            return IsHit = false;
        }
    }
}