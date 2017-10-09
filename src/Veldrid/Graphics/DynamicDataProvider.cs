using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Veldrid.Graphics
{
    /// <summary>
    /// A data provider with dynamically and manually controllable data.
    /// </summary>
    /// <typeparam name="T">The type of data to provide.</typeparam>
    public class DynamicDataProvider<T> : ConstantBufferDataProvider<T> where T : struct
    {
        private static readonly Func<T, T, bool> s_equalityFunc = GetEqualityFunc();

        private readonly int _dataSizeInBytes;
        private T _data;

        /// <summary>
        /// Provides a notification when this provider's data changes.
        /// </summary>
        public event Action DataChanged;

        /// <summary>
        /// Gets or sets the data provided by this <see cref="DynamicDataProvider{T}"/>.
        /// </summary>
        public T Data
        {
            get
            {
                return _data;
            }
            set
            {
                if (!s_equalityFunc(_data, value))
                {
                    if (!_data.Equals(value))
                    {
                        _data = value;
                        DataChanged?.Invoke();
                    }
                }
            }
        }

        /// <summary>
        /// Constructs a new <see cref="DynamicDataProvider{T}"/> with the given initial data.
        /// </summary>
        /// <param name="data">The initial data to provide.</param>
        public DynamicDataProvider(T data)
        {
            Data = data;
            _dataSizeInBytes = Marshal.SizeOf<T>();
        }

        /// <summary>
        /// Constructs a new <see cref="DynamicDataProvider{T}"/> with default data.
        /// </summary>
        public DynamicDataProvider()
        {
            _dataSizeInBytes = Marshal.SizeOf<T>();
        }

        /// <summary>
        /// Gets the size of data contained in this <see cref="DynamicDataProvider{T}"/>.
        /// </summary>
        public int DataSizeInBytes => _dataSizeInBytes;

        /// <summary>
        /// Propogates data from this provider into the given GPU buffer.
        /// </summary>
        /// <param name="buffer">The GPU buffer to transfer data to.</param>
        public void SetData(ConstantBuffer buffer)
        {
            buffer.SetData(ref _data, _dataSizeInBytes);
        }

        private static Func<T, T, bool> GetEqualityFunc()
        {
            if (typeof(IEquatable<T>).IsAssignableFrom(typeof(T)))
            {
                return EqualityComparer<T>.Default.Equals;
            }
            else
            {
                return (a, b) =>
                {
                    unsafe
                    {
                        byte* aPtr = (byte*)Unsafe.AsPointer(ref a);
                        byte* bPtr = (byte*)Unsafe.AsPointer(ref b);

                        int length = Unsafe.SizeOf<T>();
                        for (int i = 0; i < length; i++)
                        {
                            if (aPtr[i] != bPtr[i])
                            {
                                return false;
                            }
                        }

                        return true;
                    }
                };
            }
        }
    }
}
