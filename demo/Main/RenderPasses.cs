namespace Veldrid.NeoDemo
{
    public enum RenderPasses : byte
    {
        Standard = 1 << 0,
        AlphaBlend = 1 << 1,
        Overlay = 1 << 2,
    }
}
