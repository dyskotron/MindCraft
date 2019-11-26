using System.Collections.Generic;
using Framewerk;
using MindCraft.Data;
using MindCraft.Model;
using UnityEngine;

namespace MindCraft.Physics
{
    public interface IVoxelPhysicsWorld
    {
        void AddRigidBody(VoxelRigidBody body);
        bool CheckBodyOnGlobalXyz(VoxelRigidBody body, float x, float y, float z);
        void SetEnabled(bool value);
        void Destroy();
    }

    /// <summary>
    /// Super simple voxel "physics"
    /// </summary>
    public class VoxelPhysicsWorld : IVoxelPhysicsWorld
    {
        private const float AUTOJUMP_FORCE = 5;
        private const float AUTOJUMP_TRESHOLD = 0.05f;
        private const float ATOJUMP_TRESHOLD_POW2 = AUTOJUMP_TRESHOLD * AUTOJUMP_TRESHOLD;
        
        [Inject] public IUpdater Updater { get; set; }
        [Inject] public IWorldSettings WorldSettings { get; set; }
        [Inject] public IWorldModel WorldModel { get; set; }
        
        public bool Enabled => _enabled;

        private bool _enabled;

        private float _gravity;

        private List<VoxelRigidBody> _bodies = new List<VoxelRigidBody>();

        [PostConstruct]
        public void PostConstruct()
        {
            _gravity = WorldSettings.Gravity;
            SetEnabled(true);
        }

        public void AddRigidBody(VoxelRigidBody body)
        {
            _bodies.Add(body);
        }

        public void SetEnabled(bool value)
        {
            if (_enabled == value)
                return;

            _enabled = value;

            if (_enabled)
                Updater.EveryStep(UpdateBodies);
            else
                Updater.RemoveStepAction(UpdateBodies);
        }

        public void Destroy()
        {
            SetEnabled(false);
        }

        private void UpdateBodies()
        {
            foreach (var body in _bodies)
            {
                UpdateBody(body);
            }
        }

        private void UpdateBody(VoxelRigidBody body)
        {
            body.Grounded = false;

            //apply gravity
            if (body.VerticalMomentum > _gravity)
                body.VerticalMomentum += Time.fixedDeltaTime * _gravity;

            //apply vertical momentum
            body.Velocity += body.VerticalMomentum * Time.fixedDeltaTime * Vector3.up;

            //collision with world
            var oldPosition = body.Transform.position;
            var targetPosition = oldPosition + body.Velocity;
            body.LostVelocity = Vector3.zero;

            if (CheckBodyOnGlobalXyz(body, targetPosition.x, targetPosition.y, targetPosition.z))
            {
                if (CheckBodyOnGlobalXyz(body, targetPosition.x, oldPosition.y, oldPosition.z))
                {
                    body.LostVelocity.x = body.Velocity.x;
                    body.Velocity.x = 0;
                }

                if (CheckBodyOnGlobalXyz(body, oldPosition.x, targetPosition.y, oldPosition.z))
                {
                    if (body.Velocity.y < 0)
                        body.Grounded = true;

                    body.LostVelocity.y = body.Velocity.y;
                    body.Velocity.y = 0;
                }

                if (CheckBodyOnGlobalXyz(body, oldPosition.x, oldPosition.y, targetPosition.z))
                {
                    body.LostVelocity.z = body.Velocity.z;
                    body.Velocity.z = 0;
                }
            }
            
            var newPosition = oldPosition + body.Velocity;
            
            // == AUTOJUMP == //TODO: move to player controller
            //jump if we're prevented to move on horizontal plane and there is space to move there one voxel up
            //jumping shoul also check
            if (body.Grounded)
            {
                var preventedHorizontalMagnitude = new Vector2(body.LostVelocity.x, body.LostVelocity.z).magnitude;
                
                if (preventedHorizontalMagnitude > AUTOJUMP_TRESHOLD && CheckAutojumpPositionAvailable(body, targetPosition, oldPosition))
                    //!CheckBodyOnGlobalXyz(body , targetPosition.x, oldPosition.y + 1, targetPosition.z))
                {
                    body.VerticalMomentum = 5;
                }
            }

            body.Transform.position = newPosition;
        }

        //this brute force works for now as its cheap physics anyway, but definitely not feasible when more bodies than just player will be present in a game
        //TODO: check only in direction player is actually moving to
        public bool CheckBodyOnGlobalXyz(VoxelRigidBody body, float x, float y , float z)
        {
                //bottom
                return WorldModel.CheckVoxelOnGlobalXyz(x + body.Size, y, z) ||
                       WorldModel.CheckVoxelOnGlobalXyz(x + body.Size, y, z + body.Size) ||
                       WorldModel.CheckVoxelOnGlobalXyz(x, y, z + body.Size) ||
                       WorldModel.CheckVoxelOnGlobalXyz(x - body.Size, y, z + body.Size) ||
                       WorldModel.CheckVoxelOnGlobalXyz(x - body.Size, y , z) ||
                       WorldModel.CheckVoxelOnGlobalXyz(x - body.Size, y, z - body.Size) ||
                       WorldModel.CheckVoxelOnGlobalXyz(x, y, z - body.Size) ||
                       WorldModel.CheckVoxelOnGlobalXyz(x + body.Size, y, z - body.Size) ||

                       //top
                       WorldModel.CheckVoxelOnGlobalXyz(x + body.Size, y + body.Height, z) ||
                       WorldModel.CheckVoxelOnGlobalXyz(x + body.Size, y + body.Height, z + body.Size) ||
                       WorldModel.CheckVoxelOnGlobalXyz(x, y + body.Height, z + body.Size) ||
                       WorldModel.CheckVoxelOnGlobalXyz(x - body.Size, y + body.Height, z + body.Size) ||
                       WorldModel.CheckVoxelOnGlobalXyz(x - body.Size, y + body.Height, z) ||
                       WorldModel.CheckVoxelOnGlobalXyz(x - body.Size, y + body.Height, z - body.Size) ||
                       WorldModel.CheckVoxelOnGlobalXyz(x, y + body.Height, z - body.Size) ||
                       WorldModel.CheckVoxelOnGlobalXyz(x + body.Size, y + body.Height, z - body.Size);
        }

        private bool CheckAutojumpPositionAvailable(VoxelRigidBody body, Vector3 targetPosition, Vector3 oldPosition)
        {
            return !CheckBodyOnGlobalXyz(body, targetPosition.x, oldPosition.y + 1, targetPosition.z) ||
                !CheckBodyOnGlobalXyz(body, targetPosition.x, oldPosition.y + 1, targetPosition.z) ||
                !CheckBodyOnGlobalXyz(body, targetPosition.x, oldPosition.y + 1, targetPosition.z);
        }
    }
}