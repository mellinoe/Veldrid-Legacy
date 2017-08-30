using System.Numerics;
using Veldrid.Platform;

namespace Veldrid.NeoDemo
{
    public class Camera : IUpdateable
    {
        private readonly SceneContext _sc;

        private float _fov = 1f;
        private float _near = 0.5f;
        private float _far = 1000f;

        private Vector3 _position = new Vector3(0, 3, 0);
        private Vector3 _lookDirection = new Vector3(0, -.3f, -1f);
        private float _moveSpeed = 1f;

        public Camera(SceneContext sc, float width, float height)
        {
            _sc = sc;
            UpdatePerspectiveMatrix(width, height);
            UpdateViewMatrix();
        }

        public void Update(float deltaSeconds)
        {
            Vector3 motionDir = Vector3.Zero;
            if (InputTracker.GetKey(Key.A))
            {
                motionDir += -Vector3.UnitX;
            }
            if (InputTracker.GetKey(Key.D))
            {
                motionDir += Vector3.UnitX;
            }
            if (InputTracker.GetKey(Key.W))
            {
                motionDir += -Vector3.UnitZ;
            }
            if (InputTracker.GetKey(Key.S))
            {
                motionDir += Vector3.UnitZ;
            }
            if (InputTracker.GetKey(Key.Q))
            {
                motionDir += -Vector3.UnitY;
            }
            if (InputTracker.GetKey(Key.E))
            {
                motionDir += Vector3.UnitY;
            }

            if (motionDir != Vector3.Zero)
            {
                _position += motionDir * _moveSpeed * deltaSeconds;
                UpdateViewMatrix();
            }
        }

        public void WindowResized(float width, float height)
        {
            UpdatePerspectiveMatrix(width, height);
        }

        private void UpdatePerspectiveMatrix(float width, float height)
        {
            Matrix4x4 perspective = Matrix4x4.CreatePerspectiveFieldOfView(_fov, width / height, _near, _far);
            _sc.ProjectionMatrixBuffer.SetData(ref perspective);
        }

        private void UpdateViewMatrix()
        {
            Matrix4x4 view = Matrix4x4.CreateLookAt(_position, _position + _lookDirection, Vector3.UnitY);
            _sc.ViewMatrixBuffer.SetData(ref view);
        }
    }
}
