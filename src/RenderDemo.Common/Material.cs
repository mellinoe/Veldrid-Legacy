using System;

namespace Veldrid.Graphics
{
    // TODO REMOVE EVERYTHING HERE.
    public class Material : IDisposable
    {
        public Material(
            ShaderSet shaderSet,
            ShaderResourceBindingSlots resourceBindings)
        {
            ShaderSet = shaderSet;
            ResourceBindings = resourceBindings;
        }

        public ShaderSet ShaderSet { get; }
        public ShaderResourceBindingSlots ResourceBindings { get; }

        public void Dispose()
        {
            ShaderSet.Dispose();
        }

        internal void Apply(RenderContext rc)
        {
            rc.ShaderSet = ShaderSet;
            rc.ShaderResourceBindingSlots = ResourceBindings;
        }
    }
}
