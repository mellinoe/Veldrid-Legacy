using OpenTK.Graphics.ES30;
using System;

namespace Veldrid.Graphics.OpenGLES
{
    /// <summary>
    /// A utility class managing the relationships between textures, samplers, and their binding locations.
    /// </summary>
    internal class OpenGLESTextureSamplerManager
    {
        private readonly int _maxTextureUnits;
        private readonly OpenGLESTextureBinding[] _textureUnitTextures;
        private readonly BoundSamplerStateInfo[] _textureUnitSamplers;

        public OpenGLESTextureSamplerManager()
        {
            _maxTextureUnits = GL.GetInteger(GetPName.MaxCombinedTextureImageUnits);
            Utilities.CheckLastGLES3Error();
            _maxTextureUnits = Math.Max(_maxTextureUnits, 8); // OpenGL spec indicates that implementations must support at least 8.
            _textureUnitTextures = new OpenGLESTextureBinding[_maxTextureUnits];
            _textureUnitSamplers = new BoundSamplerStateInfo[_maxTextureUnits];
        }

        public void SetTexture(int textureUnit, OpenGLESTextureBinding texture)
        {
            if (_textureUnitTextures[textureUnit] != texture)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
                Utilities.CheckLastGLES3Error();
                GL.BindTexture(texture.BoundTexture.Target, texture.BoundTexture.ID);
                Utilities.CheckLastGLES3Error();

                EnsureSamplerMipmapState(textureUnit, texture.BoundTexture.MipLevels > 1);
                _textureUnitTextures[textureUnit] = texture;
            }
        }

        public void SetSampler(int textureUnit, OpenGLESSamplerState samplerState)
        {
            if (_textureUnitSamplers[textureUnit].SamplerState != samplerState)
            {
                bool mipmapped = false;
                OpenGLESTextureBinding texBinding = _textureUnitTextures[textureUnit];
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
            public OpenGLESSamplerState SamplerState;
            public bool Mipmapped;

            public BoundSamplerStateInfo(OpenGLESSamplerState samplerState, bool mipmapped)
            {
                SamplerState = samplerState;
                Mipmapped = mipmapped;
            }
        }
    }
}
