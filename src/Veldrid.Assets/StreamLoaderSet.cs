using System;
using System.Collections;
using System.Collections.Generic;

namespace Veldrid.Assets
{
    public class StreamLoaderSet : IEnumerable<KeyValuePair<Type, AssetLoader>>
    {
        private readonly Dictionary<Type, AssetLoader> _loaders = new Dictionary<Type, AssetLoader>();

        public AssetLoader Get(Type t, AssetLoader defaultLoader)
        {
            AssetLoader loader;
            if (!_loaders.TryGetValue(t, out loader))
            {
                loader = defaultLoader;
            }

            return loader;
        }

        public AssetLoader<T> Get<T>(Type t, AssetLoader<T> defaultLoader)
        {
            AssetLoader loader;
            if (!_loaders.TryGetValue(t, out loader))
            {
                loader = defaultLoader;
            }

            return (AssetLoader<T>)loader;
        }


        public bool TryGetLoader(Type t, out AssetLoader loader)
        {
            return _loaders.TryGetValue(t, out loader);
        }

        public bool TryGetLoader<T>(out AssetLoader<T> loader)
        {
            AssetLoader untypedLoader;
            if (_loaders.TryGetValue(typeof(T), out untypedLoader))
            {
                loader = (AssetLoader<T>)untypedLoader;
                return true;
            }

            loader = null;
            return false;
        }

        public void Add(Type t, AssetLoader loader)
        {
            if (_loaders.ContainsKey(t))
            {
                throw new InvalidOperationException("A loader for type " + t.Name + " is already registered.");
            }

            _loaders.Add(t, loader);
        }

        public IEnumerator<KeyValuePair<Type, AssetLoader>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<Type, AssetLoader>>)_loaders).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<Type, AssetLoader>>)_loaders).GetEnumerator();
        }
    }
}
