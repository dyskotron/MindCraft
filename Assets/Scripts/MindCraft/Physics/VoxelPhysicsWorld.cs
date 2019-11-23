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
        void SetEnabled(bool value);
        void Destroy();
    }

    /// <summary>
    /// Super simple voxel "physics"
    /// </summary>
    public class VoxelPhysicsWorld : IVoxelPhysicsWorld
    {
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
            if(_enabled == value)
                return;
            
            _enabled = value;  

            if(_enabled)
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
            foreach (var voxelRigidBody in _bodies)
            {
                UpdateBody(voxelRigidBody); 
            } 
        }

        private void UpdateBody(VoxelRigidBody voxelRigidBody)
        {
            voxelRigidBody.Grounded = false;
                
            //apply gravity
            if (voxelRigidBody.VerticalMomentum > _gravity)
                voxelRigidBody.VerticalMomentum += Time.fixedDeltaTime * _gravity;

            //apply vertical momentum
            voxelRigidBody.Velocity += voxelRigidBody.VerticalMomentum * Time.fixedDeltaTime * Vector3.up;
            
            //collision with world
            var oldPosition = voxelRigidBody.Transform.position;
            var newPosition = oldPosition + voxelRigidBody.Velocity;
            voxelRigidBody.LostVelocity = Vector3.zero;
            
            if (WorldModel.CheckVoxelOnGlobalXyz(newPosition.x, newPosition.y, newPosition.z))
            {
                if (WorldModel.CheckVoxelOnGlobalXyz(newPosition.x, oldPosition.y, oldPosition.z))
                {
                    voxelRigidBody.LostVelocity.x = voxelRigidBody.Velocity.x;
                    voxelRigidBody.Velocity.x = 0;
                }

                if (WorldModel.CheckVoxelOnGlobalXyz(oldPosition.x, newPosition.y, oldPosition.z))
                {
                    if (voxelRigidBody.Velocity.y < 0)
                        voxelRigidBody.Grounded = true;
                    
                    voxelRigidBody.LostVelocity.y = voxelRigidBody.Velocity.y;
                    voxelRigidBody.Velocity.y = 0;
                }

                if (WorldModel.CheckVoxelOnGlobalXyz(oldPosition.x, oldPosition.y, newPosition.z))
                {
                    voxelRigidBody.LostVelocity.z = voxelRigidBody.Velocity.z;
                    voxelRigidBody.Velocity.z = 0;
                }

                newPosition = oldPosition + voxelRigidBody.Velocity;
            }
            
            //if we are prevented to move on horizontal plane and it can be solved by jumping one square then jump
            //should work only one block above ground
            //TODO: check neighbour blocks as now it can autojump in weird cornercases
            if (voxelRigidBody.Grounded)
            {
                var preventedHoriontalMagnitude = new Vector2(voxelRigidBody.LostVelocity.x, voxelRigidBody.LostVelocity.z).magnitude;
                voxelRigidBody.LostVelocity.z = preventedHoriontalMagnitude;
                var lostVelocityNormalised = voxelRigidBody.LostVelocity.normalized;
                
                if (preventedHoriontalMagnitude > 0.05f && !WorldModel.CheckVoxelOnGlobalXyz(newPosition.x + lostVelocityNormalised.x, 
                                                                                             newPosition.y + 1, 
                                                                                             newPosition.z + lostVelocityNormalised.z))
                {
                    voxelRigidBody.VerticalMomentum = 5;
                }
            }

            voxelRigidBody.Transform.position = newPosition;
        }
    }
}