using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Vulkan;
using static Veldrid.Graphics.Vulkan.VulkanUtil;
using static Vulkan.VulkanNative;

namespace Veldrid.Graphics.Vulkan
{
    public unsafe class VkDeviceBuffer : DeviceBufferBase
    {
        private readonly VkDevice _device;
        private readonly bool _dynamic;

        private VkBuffer _buffer;
        private VkDeviceMemory _memory;
        private ulong _bufferCapacity;
        private ulong _bufferDataSize;
        private void* _mappedPtr;

        public VkBuffer DeviceBuffer => _buffer;

        public VkDeviceBuffer(
            VkDevice device,
            VkPhysicalDevice physicalDevice,
            ulong size,
            VkBufferUsageFlags usage,
            VkMemoryPropertyFlags memoryProperties,
            bool dynamic)
        {
            _device = device;
            _dynamic = dynamic;
            VkBufferCreateInfo bufferCI = VkBufferCreateInfo.New();
            bufferCI.size = size;
            bufferCI.usage = usage;
            VkResult result = vkCreateBuffer(device, ref bufferCI, null, out _buffer);
            CheckResult(result);

            vkGetBufferMemoryRequirements(device, _buffer, out VkMemoryRequirements memoryRequirements);
            _bufferCapacity = memoryRequirements.size;
            uint memoryType = FindMemoryType(physicalDevice, memoryRequirements.memoryTypeBits, memoryProperties);
            VkMemoryAllocateInfo memoryAI = VkMemoryAllocateInfo.New();
            memoryAI.allocationSize = memoryRequirements.size;
            memoryAI.memoryTypeIndex = memoryType;
            vkAllocateMemory(device, ref memoryAI, null, out _memory);
            vkBindBufferMemory(device, _buffer, _memory, 0);

            if (_dynamic)
            {
                MapBuffer((int)size, true);
            }
        }

        public override void GetData(IntPtr storageLocation, int storageSizeInBytes)
        {
            int copySize = Math.Min(storageSizeInBytes, (int)_bufferCapacity);
            IntPtr mappedPtr = MapBuffer(copySize);
            Unsafe.CopyBlock(storageLocation.ToPointer(), mappedPtr.ToPointer(), (uint)copySize);
            UnmapBuffer();
        }

        public override IntPtr MapBuffer(int numBytes) => MapBuffer(numBytes, false);

        public IntPtr MapBuffer(int numBytes, bool initDynamic)
        {
            if (!_dynamic || initDynamic)
            {
                void* mappedPtr;
                VkResult result = vkMapMemory(_device, _memory, 0, (ulong)numBytes, 0, &mappedPtr);
                CheckResult(result);
                _mappedPtr = mappedPtr;
                return (IntPtr)mappedPtr;
            }
            else
            {
                Debug.Assert(_mappedPtr != null);
                return (IntPtr)_mappedPtr;
            }
        }

        public override void SetData(IntPtr data, int dataSizeInBytes, int destinationOffsetInBytes)
        {
            EnsureBufferSize(dataSizeInBytes + destinationOffsetInBytes);
            _bufferDataSize = (ulong)dataSizeInBytes;
            IntPtr mappedPtr = MapBuffer(dataSizeInBytes);
            byte* destPtr = (byte*)mappedPtr + destinationOffsetInBytes;
            Unsafe.CopyBlock(destPtr, data.ToPointer(), (uint)dataSizeInBytes);
            UnmapBuffer();
        }

        public override void UnmapBuffer()
        {
            if (!_dynamic)
            {
                vkUnmapMemory(_device, _memory);
            }
        }

        public override void Dispose()
        {
            vkDestroyBuffer(_device, _buffer, null);
            vkFreeMemory(_device, _memory, null);
        }

        private void EnsureBufferSize(int dataSizeInBytes)
        {
            if (_bufferCapacity < (ulong)dataSizeInBytes)
            {
                throw new NotImplementedException();
            }
        }
    }
}
