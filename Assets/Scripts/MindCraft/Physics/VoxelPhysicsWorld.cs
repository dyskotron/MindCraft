using System.Collections.Generic;
using Framewerk;
using MindCraft.Model;
using UnityEngine;

namespace MindCraft.Physics
{
    public class VoxelRigidBody
    {
        public float Size;
        public float Height;
        public Transform Transform;
        
        public Vector3 Velocity;
        public float VerticalMomentum;
        public bool Grounded;

        public VoxelRigidBody(float size, float height, Transform transform)
        {
            Size = size;
            Height = height;
            Transform = transform;
        }
    }

    public interface IVoxelPhysicsWorld
    {
        void AddRigidBody(VoxelRigidBody body);
        bool Enabled { get;}
        void SetEnabled(bool enabled);
        void Destroy();
    }

    public class VoxelPhysicsWorld : IVoxelPhysicsWorld
    {
        [Inject] public IUpdater Updater { get; set; }
        [Inject] public IWorldModel WorldModel { get; set; }

        public bool Enabled => _enabled;
        
        private float Gravity = -9.8f;
        private List<VoxelRigidBody> _bodies = new List<VoxelRigidBody>();

        private bool _enabled;

        [PostConstruct]
        public void PostConstruct()
        {
            SetEnabled(true);
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

        public void AddRigidBody(VoxelRigidBody body)
        {
            _bodies.Add(body);
        }

        private void UpdateBodies()
        {
            foreach (var body in _bodies)
            {
                //apply gravity
                if (body.VerticalMomentum > Gravity)
                    body.VerticalMomentum += Time.fixedDeltaTime * Gravity;

                /* TODO: MOVE TO CHARACTER CONTROLLER
                //walk / sprint
                _body._velocity = ((_body.transform.forward * _vertical) + (_body.transform.right * _horizontal)) * Time.deltaTime * _moveSpeed;
                */

                //apply vertical momentum
                body.Velocity += body.VerticalMomentum * Time.fixedDeltaTime * Vector3.up;

                if (body.Velocity.z > 0 && CheckFront(body) || (body.Velocity.z < 0 && CheckBack(body)))
                    body.Velocity.z = 0;
                if (body.Velocity.x > 0 && CheckRight(body) || (body.Velocity.x < 0 && CheckLeft(body)))
                    body.Velocity.x = 0;

                if (body.Velocity.y < 0)
                    body.Velocity.y = CheckDownSpeed(body);
                if (body.Velocity.y > 0)
                    body.Velocity.y = CheckUpSpeed(body);
                
                body.Transform.Translate(body.Velocity, Space.World);
            }
        }

        private float CheckDownSpeed(VoxelRigidBody body)
        {
            if (WorldModel.CheckVoxelOnGlobalXyz(body.Transform.position.x - body.Size, body.Transform.position.y + body.Velocity.y, body.Transform.position.z - body.Size) ||
                WorldModel.CheckVoxelOnGlobalXyz(body.Transform.position.x - body.Size, body.Transform.position.y + body.Velocity.y, body.Transform.position.z + body.Size) ||
                WorldModel.CheckVoxelOnGlobalXyz(body.Transform.position.x + body.Size, body.Transform.position.y + body.Velocity.y, body.Transform.position.z - body.Size) ||
                WorldModel.CheckVoxelOnGlobalXyz(body.Transform.position.x + body.Size, body.Transform.position.y + body.Velocity.y, body.Transform.position.z + body.Size))
            {
                body.Grounded = true;

                return 0;
            }

            body.Grounded = false;

            return  + body.Velocity.y;
        }

        private float CheckUpSpeed(VoxelRigidBody body)
        {
            if (
                WorldModel.CheckVoxelOnGlobalXyz(body.Transform.position.x - body.Size, body.Transform.position.y + body.Height + body.Velocity.y, body.Transform.position.z - body.Size) ||
                WorldModel.CheckVoxelOnGlobalXyz(body.Transform.position.x - body.Size, body.Transform.position.y + body.Height + body.Velocity.y, body.Transform.position.z + body.Size) ||
                WorldModel.CheckVoxelOnGlobalXyz(body.Transform.position.x + body.Size, body.Transform.position.y + body.Height + body.Velocity.y, body.Transform.position.z - body.Size) ||
                WorldModel.CheckVoxelOnGlobalXyz(body.Transform.position.x + body.Size, body.Transform.position.y + body.Height + body.Velocity.y, body.Transform.position.z + body.Size)
            )
                return 0;

            return body.Velocity.y;
        }

        private bool CheckFront(VoxelRigidBody body)
        {
            return WorldModel.CheckVoxelOnGlobalXyz(body.Transform.position.x, body.Transform.position.y, body.Transform.position.z + body.Size) ||
                   WorldModel.CheckVoxelOnGlobalXyz(body.Transform.position.x, body.Transform.position.y, body.Transform.position.z + body.Size);
        }

        private bool CheckBack(VoxelRigidBody body)
        {
            return WorldModel.CheckVoxelOnGlobalXyz(body.Transform.position.x, body.Transform.position.y, body.Transform.position.z - body.Size) ||
                   WorldModel.CheckVoxelOnGlobalXyz(body.Transform.position.x, body.Transform.position.y, body.Transform.position.z - body.Size);
        }

        private bool CheckLeft(VoxelRigidBody body)
        {
            return WorldModel.CheckVoxelOnGlobalXyz(body.Transform.position.x - body.Size, body.Transform.position.y, body.Transform.position.z) ||
                   WorldModel.CheckVoxelOnGlobalXyz(body.Transform.position.x - body.Size, body.Transform.position.y, body.Transform.position.z);
        }

        private bool CheckRight(VoxelRigidBody body)
        {
            return WorldModel.CheckVoxelOnGlobalXyz(body.Transform.position.x + body.Size, body.Transform.position.y, body.Transform.position.z) ||
                   WorldModel.CheckVoxelOnGlobalXyz(body.Transform.position.x + body.Size, body.Transform.position.y, body.Transform.position.z);
        }
    }
}