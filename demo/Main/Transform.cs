using System.Numerics;

namespace Veldrid.NeoDemo
{
    public class Transform
    {
        private Vector3 _position;
        private Quaternion _rotation = Quaternion.Identity;
        private Vector3 _scale = Vector3.One;

        public Vector3 Position { get => _position; set => _position = value; }
        public Quaternion Rotation { get => _rotation; set => _rotation = value; }
        public Vector3 Scale { get => _scale; set => _scale = value; }

        public Matrix4x4 GetTransformMatrix()
        {
            return Matrix4x4.CreateScale(_scale)
                * Matrix4x4.CreateFromQuaternion(_rotation)
                * Matrix4x4.CreateTranslation(Position);
        }
    }
}
