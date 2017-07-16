namespace Veldrid.Graphics
{
    /// <summary>
    /// A device-specific representation of the resources available to a <see cref="ShaderSet"/>.
    /// </summary>
    public interface ShaderResourceBindingSlots
    {
        /// <summary>
        /// A device-agnostic description of the constant buffers.
        /// </summary>
        ShaderResourceDescription[] Resources { get; }
    }
}
