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
        private readonly VkDeviceMemoryManager _memoryManager;
        private readonly bool _dynamic;

        private VkBuffer _buffer;
        private VkMemoryBlock _memory;
        private ulong _bufferCapacity;
        private ulong _bufferDataSize;
        private void* _mappedPtr;

        public VkBuffer DeviceBuffer => _buffer;

        public VkDeviceBuffer(
            VkDevice device,
            VkPhysicalDevice physicalDevice,
            VkDeviceMemoryManager memoryManager,
            ulong size,
            VkBufferUsageFlags usage,
            VkMemoryPropertyFlags memoryProperties,
            bool dynamic)
        {
            _device = device;
            _memoryManager = memoryManager;
            _dynamic = dynamic;
            VkBufferCreateInfo bufferCI = VkBufferCreateInfo.New();
            bufferCI.size = size;
            bufferCI.usage = usage;
            VkResult result = vkCreateBuffer(device, ref bufferCI, null, out _buffer);
            CheckResult(result);

            vkGetBufferMemoryRequirements(device, _buffer, out VkMemoryRequirements memoryRequirements);
            _bufferCapacity = memoryRequirements.size;
            uint memoryType = FindMemoryType(physicalDevice, memoryRequirements.memoryTypeBits, memoryProperties);
            VkMemoryBlock memoryToken = memoryManager.Allocate(memoryType, memoryRequirements.size, memoryRequirements.alignment);
            _memory = memoryToken;
            vkBindBufferMemory(device, _buffer, _memory.DeviceMemory, _memory.Offset);

            _dynamic = false;
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
                VkResult result = vkMapMemory(_device, _memory.DeviceMemory, _memory.Offset, (ulong)numBytes, 0, &mappedPtr);
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
                vkUnmapMemory(_device, _memory.DeviceMemory);
            }
        }

        public override void Dispose()
        {
            vkDestroyBuffer(_device, _buffer, null);
            _memoryManager.Free(_memory);
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
