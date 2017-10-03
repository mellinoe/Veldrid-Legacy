using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid.Graphics;

namespace Veldrid.NeoDemo
{
    public class DirectionalLight
    {
        private RgbaFloat _color;
        public Transform Transform { get; } = new Transform();

        public Vector3 Direction => Transform.Forward;

        public event Action<RgbaFloat> ColorChanged;

        public RgbaFloat Color { get => _color; set { _color = value; ColorChanged?.Invoke(value); } }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DirectionalLightInfo
    {
        public Vector3 Direction;
        private float _padding;
        public Vector4 Color;
    }
}
