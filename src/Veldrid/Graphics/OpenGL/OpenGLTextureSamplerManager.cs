using OpenTK.Graphics.OpenGL;
using System;

namespace Veldrid.Graphics.OpenGL
{
    /// <summary>
    /// A utility class managing the relationships between textures, samplers, and their binding locations.
    /// </summary>
    internal class OpenGLTextureSamplerManager
    {
        private readonly bool _dsaAvailable;
        private readonly int _maxTextureUnits;
        private readonly OpenGLTextureBinding[] _textureUnitTextures;
        private readonly BoundSamplerStateInfo[] _textureUnitSamplers;

        public OpenGLTextureSamplerManager(OpenGLExtensions extensions)
        {
            _dsaAvailable = extensions.ARB_DirectStateAccess;
            _maxTextureUnits = GL.GetInteger(GetPName.MaxCombinedTextureImageUnits);
            _maxTextureUnits = Math.Max(_maxTextureUnits, 8); // OpenGL spec indicates that implementations must support at least 8.
            _textureUnitTextures = new OpenGLTextureBinding[_maxTextureUnits];
            _textureUnitSamplers = new BoundSamplerStateInfo[_maxTextureUnits];
        }

        public void SetTexture(int textureUnit, OpenGLTextureBinding texture)
        {
            if (_textureUnitTextures[textureUnit] != texture)
            {
                if (_dsaAvailable)
                {
                    GL.BindTextureUnit(textureUnit, texture.BoundTexture.ID);
                }
                else
                {
                    GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
                    GL.BindTexture(texture.BoundTexture.Target, texture.BoundTexture.ID);
                }

                EnsureSamplerMipmapState(textureUnit, texture.BoundTexture.MipLevels > 1);
                _textureUnitTextures[textureUnit] = texture;
            }
        }

        public void SetSampler(int textureUnit, OpenGLSamplerState samplerState)
        {
            if (_textureUnitSamplers[textureUnit].SamplerState != samplerState)
            {
                bool mipmapped = false;
                OpenGLTextureBinding texBinding = _textureUnitTextures[textureUnit];
                if (texBinding != null)
                {
                    mipmapped = texBinding.BoundTexture.MipLevels > 1;
                }

                samplerState.Apply(textureUnit, mipmapped);
                _textureUnitSamplers[textureUnit] = new BoundSamplerStateInfo(samplerState, mipmapped);
            }
            else if (_textureUnitTextures[textureUnit] != null)
            {
                EnsureSamplerMipmapState(textureUnit, _textureUnitTextures[textureUnit].BoundTexture.MipLevels > 1);
            }
        }

        private void EnsureSamplerMipmapState(int textureUnit, bool mipmapped)
        {
            if (_textureUnitSamplers[textureUnit].SamplerState != null && _textureUnitSamplers[textureUnit].Mipmapped != mipmapped)
            {
                _textureUnitSamplers[textureUnit].SamplerState.Apply(textureUnit, mipmapped);
                _textureUnitSamplers[textureUnit].Mipmapped = mipmapped;
            }
        }

        private struct BoundSamplerStateInfo
        {
            public OpenGLSamplerState SamplerState;
            public bool Mipmapped;

            public BoundSamplerStateInfo(OpenGLSamplerState samplerState, bool mipmapped)
            {
                SamplerState = samplerState;
                Mipmapped = mipmapped;
            }
        }
    }
}
