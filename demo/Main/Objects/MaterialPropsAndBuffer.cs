using System.Runtime.CompilerServices;
using Veldrid.Graphics;

namespace Veldrid.NeoDemo.Objects
{
    public class MaterialPropsAndBuffer
    {
        public string Name { get; set; }
        private readonly DynamicDataProvider<MaterialProperties> _properties;
        public ConstantBuffer ConstantBuffer { get; private set; }

        public DynamicDataProvider<MaterialProperties> Properties => _properties;

        public MaterialPropsAndBuffer(MaterialProperties mp)
        {
            _properties = new DynamicDataProvider<MaterialProperties>(mp);
            _properties.DataChangedByRef += OnDataChanged;
        }

        private void OnDataChanged(ref MaterialProperties data)
        {
            ConstantBuffer.SetData(ref data);
        }

        public void CreateDeviceObjects(RenderContext rc)
        {
            ConstantBuffer = rc.ResourceFactory.CreateConstantBuffer(Unsafe.SizeOf<MaterialProperties>());
            Properties.SetData(ConstantBuffer);
        }

        public void DestroyDeviceObjects()
        {
            ConstantBuffer.Dispose();
        }
    }
}
