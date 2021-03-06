﻿using System;
using System.Diagnostics;
using Vulkan;
using static Vulkan.VulkanNative;

namespace Veldrid.Graphics.Vulkan
{
    internal unsafe static class VulkanUtil
    {
        [Conditional("DEBUG")]
        public static void CheckResult(VkResult result)
        {
            if (result != VkResult.Success)
            {
                throw new VeldridException("Unsuccessful VkResult: " + result);
            }
        }

        public static uint FindMemoryType(VkPhysicalDevice physicalDevice, uint typeFilter, VkMemoryPropertyFlags properties)
        {
            vkGetPhysicalDeviceMemoryProperties(physicalDevice, out VkPhysicalDeviceMemoryProperties memProperties);
            for (int i = 0; i < memProperties.memoryTypeCount; i++)
            {
                if (((typeFilter & (1 << i)) != 0)
                    && (memProperties.GetMemoryType((uint)i).propertyFlags & properties) == properties)
                {
                    return (uint)i;
                }
            }

            throw new VeldridException("No suitable memory type.");
        }

        public static void CreateImage(
            VkDevice device,
            VkPhysicalDevice physicalDevice,
            VkDeviceMemoryManager memoryManager,
            uint width,
            uint height,
            uint arrayLayers,
            VkFormat format,
            VkImageTiling tiling,
            VkImageUsageFlags usage,
            VkMemoryPropertyFlags properties,
            out VkImage image,
            out VkMemoryBlock memory)
        {
            VkImageCreateInfo imageCI = VkImageCreateInfo.New();
            imageCI.imageType = VkImageType.Image2D;
            imageCI.extent.width = width;
            imageCI.extent.height = height;
            imageCI.extent.depth = 1;
            imageCI.mipLevels = 1;
            imageCI.arrayLayers = arrayLayers;
            imageCI.format = format;
            imageCI.tiling = tiling;
            imageCI.initialLayout = VkImageLayout.Preinitialized;
            imageCI.usage = usage;
            imageCI.sharingMode = VkSharingMode.Exclusive;
            imageCI.samples = VkSampleCountFlags.Count1;

            VkResult result = vkCreateImage(device, ref imageCI, null, out image);
            CheckResult(result);

            vkGetImageMemoryRequirements(device, image, out VkMemoryRequirements memRequirements);
            VkMemoryBlock memoryToken = memoryManager.Allocate(
                FindMemoryType(physicalDevice, memRequirements.memoryTypeBits, properties),
                memRequirements.size,
                memRequirements.alignment);
            memory = memoryToken;
            result = vkBindImageMemory(device, image, memory.DeviceMemory, memory.Offset);
            CheckResult(result);
        }

        public static string[] EnumerateInstanceLayers()
        {
            uint propCount = 0;
            VkResult result = vkEnumerateInstanceLayerProperties(ref propCount, null);
            CheckResult(result);
            if (propCount == 0)
            {
                return Array.Empty<string>();
            }

            VkLayerProperties[] props = new VkLayerProperties[propCount];
            vkEnumerateInstanceLayerProperties(ref propCount, ref props[0]);

            string[] ret = new string[propCount];
            for (int i = 0; i < propCount; i++)
            {
                fixed (byte* layerNamePtr = props[i].layerName)
                {
                    ret[i] = Utilities.GetString(layerNamePtr);
                }
            }

            return ret;
        }

        public static string[] EnumerateInstanceExtensions()
        {
            uint propCount = 0;
            VkResult result = vkEnumerateInstanceExtensionProperties((byte*)null, ref propCount, null);
            CheckResult(result);
            if (propCount == 0)
            {
                return Array.Empty<string>();
            }

            VkExtensionProperties[] props = new VkExtensionProperties[propCount];
            vkEnumerateInstanceExtensionProperties((byte*)null, ref propCount, ref props[0]);

            string[] ret = new string[propCount];
            for (int i = 0; i < propCount; i++)
            {
                fixed (byte* extensionNamePtr = props[i].extensionName)
                {
                    ret[i] = Utilities.GetString(extensionNamePtr);
                }
            }

            return ret;
        }

        public static bool ArrayEquals<T>(T[] left, T[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (!ReferenceEquals(left[i], right[i]))
                {
                    return false;
                }
            }

            return true;
        }

    }

    internal unsafe static class VkPhysicalDeviceMemoryPropertiesEx
    {
        public static VkMemoryType GetMemoryType(this VkPhysicalDeviceMemoryProperties memoryProperties, uint index)
        {
            return (&memoryProperties.memoryTypes_0)[index];
        }
    }
}
