using UnityEngine;

namespace MindCraft.Physics
{
    public class VoxelRigidBody
    {
        public float Size;
        public float Height;
        public Transform Transform;

        public Vector3 Velocity;
        public Vector3 LostVelocity;
        public float VerticalMomentum;
        public bool Grounded;

        public VoxelRigidBody(float size, float height, Transform transform)
        {
            Size = size;
            Height = height;
            Transform = transform;
        }
    }
}