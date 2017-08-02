using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

namespace Veldrid.Graphics.Vulkan.Tests
{
    public class VulkanDeviceBufferTests
    {
        [Fact]
        public void SetData_GetData()
        {
            VkRenderContext rc = TestData.CreateVulkanContext();
            ConstantBuffer cb = rc.ResourceFactory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
            Matrix4x4 mat = Matrix4x4.Identity;

            cb.SetData(ref mat, Unsafe.SizeOf<Matrix4x4>());

            Matrix4x4 ret = new Matrix4x4();
            cb.GetData(ref ret, Unsafe.SizeOf<Matrix4x4>());
            Assert.Equal(mat, ret);
        }
    }
}
