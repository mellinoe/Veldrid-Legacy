using System.Numerics;
using Veldrid.Graphics;

namespace Veldrid.NeoDemo.Objects
{
    public static class CommonMaterials
    {
        public static MaterialPropsAndBuffer Brick { get; }

        static CommonMaterials()
        {
            Brick = new MaterialPropsAndBuffer(new MaterialProperties { SpecularIntensity = new Vector3(0.2f), SpecularPower = 42f }) { Name = "Brick" };
        }

        public static void CreateAllDeviceObjects(RenderContext rc)
        {
            Brick.CreateDeviceObjects(rc);
        }

        public static void DestroyAllDeviceObjects()
        {
            Brick.DestroyDeviceObjects();
        }
    }
}
