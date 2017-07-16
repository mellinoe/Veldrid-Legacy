using System;

namespace Veldrid.Graphics
{
    /// <summary>
    /// A bitmask enum specifying a shader stage, or a set of shader stages.
    /// </summary>
    [Flags]
    public enum ShaderStages : byte
    {
        None = 0,
        /// <summary>
        /// The first shader stage, responsible for transforming vertices from the input assembler into input for
        /// further shader stages.
        /// </summary>
        Vertex = 1 << 0,
        /// <summary>
        /// An optional shader stage, which performs additional mutation, manipulation, and generation of primitive data.
        /// </summary>
        Geometry = 1 << 1,
        /// <summary>
        /// The final shader stage, responsible for outputting final image data to the framebuffer.
        /// </summary>
        Fragment = 1 << 2,

        /// <summary>
        /// All shader stages.
        /// </summary>
        All = Vertex | Geometry | Fragment
    }
}
