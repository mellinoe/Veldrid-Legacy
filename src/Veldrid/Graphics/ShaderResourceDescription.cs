namespace Veldrid.Graphics
{
    public struct ShaderResourceDescription
    {
        public readonly string Name;
        public readonly ShaderResourceType Type;
        public readonly int DataSizeInBytes;
        public readonly ShaderStages Stages;

        public ShaderResourceDescription(string name, ShaderResourceType type, ShaderStages stages = ShaderStages.All)
        {
            if (type == ShaderResourceType.ConstantBuffer)
            {
                throw new VeldridException(
                    "If ShaderResourceType.ConstantBuffer is specified, dataSizeInBytes must also be provided.");
            }

            Name = name;
            Type = type;
            DataSizeInBytes = -1;
            Stages = stages;
        }

        public ShaderResourceDescription(string name, int constantBufferDataSizeInBytes)
            : this(name, ShaderResourceType.ConstantBuffer, constantBufferDataSizeInBytes)
        { }

        public ShaderResourceDescription(string name, ShaderResourceType type, int dataSizeInBytes, ShaderStages stages = ShaderStages.All)
        {
            Name = name;
            Type = type;
            DataSizeInBytes = dataSizeInBytes;
            Stages = stages;
        }

        public ShaderResourceDescription(string name, ShaderConstantType constantType, ShaderStages stages = ShaderStages.All)
        {
            Name = name;
            Type = ShaderResourceType.ConstantBuffer;
            if (!FormatHelpers.GetShaderConstantTypeByteSize(constantType, out int dataSizeInBytes))
            {
                throw new VeldridException("Invalid shader constant type: " + constantType);
            }

            DataSizeInBytes = dataSizeInBytes;
            Stages = stages;
        }

        public static ShaderResourceDescription ConstantBuffer(string name, ShaderConstantType type) => new ShaderResourceDescription(name, type);
        public static ShaderResourceDescription Texture(string name) => new ShaderResourceDescription(name, ShaderResourceType.Texture, -1);
        public static ShaderResourceDescription Sampler(string name) => new ShaderResourceDescription(name, ShaderResourceType.Sampler, -1);
    }
}
