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
        private readonly VkRenderContext _rc;
        private readonly VkBufferUsageFlags _usage;
        private readonly VkMemoryPropertyFlags _memoryProperties;
        private readonly bool _isDynamic;

        private VkBuffer _buffer;
        private VkMemoryBlock _memory;
        private ulong _bufferCapacity;
        private ulong _bufferDataSize;
        private void* _mappedPtr;
        private readonly VkMemoryRequirements _bufferMemoryRequirements;

        public VkBuffer DeviceBuffer => _buffer;

        public VkDeviceBuffer(
            VkRenderContext rc,
            ulong size,
            VkBufferUsageFlags usage,
            VkMemoryPropertyFlags memoryProperties,
            bool dynamic)
        {
            _rc = rc;
            usage |= VkBufferUsageFlags.TransferSrc;
            _usage = usage;
            _memoryProperties = memoryProperties;
            _isDynamic = dynamic;

            VkBufferCreateInfo bufferCI = VkBufferCreateInfo.New();
            bufferCI.size = size;
            bufferCI.usage = _usage;
            VkResult result = vkCreateBuffer(rc.Device, ref bufferCI, null, out _buffer);
            CheckResult(result);

            vkGetBufferMemoryRequirements(rc.Device, _buffer, out _bufferMemoryRequirements);
            _bufferCapacity = _bufferMemoryRequirements.size;
            uint memoryType = FindMemoryType(rc.PhysicalDevice, _bufferMemoryRequirements.memoryTypeBits, memoryProperties);
            VkMemoryBlock memoryToken = rc.MemoryManager.Allocate(
                memoryType,
                _bufferMemoryRequirements.size,
                _bufferMemoryRequirements.alignment);
            _memory = memoryToken;
            vkBindBufferMemory(rc.Device, _buffer, _memory.DeviceMemory, _memory.Offset);
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
            void* mappedPtr;
            VkResult result = vkMapMemory(_rc.Device, _memory.DeviceMemory, _memory.Offset, (ulong)numBytes, 0, &mappedPtr);
            CheckResult(result);
            _mappedPtr = mappedPtr;
            return (IntPtr)mappedPtr;
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
            vkUnmapMemory(_rc.Device, _memory.DeviceMemory);
        }

        public override void Dispose()
        {
            vkDestroyBuffer(_rc.Device, _buffer, null);
            _rc.MemoryManager.Free(_memory);
        }

        private void EnsureBufferSize(int dataSizeInBytes)
        {
            if (_bufferCapacity < (ulong)dataSizeInBytes)
            {
                VkBufferCreateInfo newBufferCI = VkBufferCreateInfo.New();
                newBufferCI.size = (ulong)dataSizeInBytes;
                newBufferCI.usage = _usage | VkBufferUsageFlags.TransferDst;
                VkResult result = vkCreateBuffer(_rc.Device, ref newBufferCI, null, out VkBuffer newBuffer);
                CheckResult(result);
                vkGetBufferMemoryRequirements(_rc.Device, newBuffer, out VkMemoryRequirements newMemoryRequirements);

                uint memoryType = FindMemoryType(_rc.PhysicalDevice, newMemoryRequirements.memoryTypeBits, _memoryProperties);
                VkMemoryBlock newMemory = _rc.MemoryManager.Allocate(
                    memoryType,
                    newMemoryRequirements.size,
                    newMemoryRequirements.alignment);

                result = vkBindBufferMemory(_rc.Device, newBuffer, newMemory.DeviceMemory, newMemory.Offset);
                CheckResult(result);

                if (_bufferDataSize > 0)
                {
                    VkCommandBuffer copyCmd = _rc.BeginOneTimeCommands();
                    VkBufferCopy region = new VkBufferCopy();
                    region.size = _bufferDataSize;
                    vkCmdCopyBuffer(copyCmd, _buffer, newBuffer, 1, ref region);
                    _rc.EndOneTimeCommands(copyCmd, VkFence.Null);
                }

                _rc.MemoryManager.Free(_memory);
                vkDestroyBuffer(_rc.Device, _buffer, null);

                _buffer = newBuffer;
                _memory = newMemory;
                _bufferCapacity = (ulong)dataSizeInBytes;
            }
        }
    }
}
