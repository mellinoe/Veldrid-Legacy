using System;
using System.Diagnostics;
using Vulkan;
using static Veldrid.Graphics.Vulkan.VulkanUtil;
using static Vulkan.VulkanNative;

namespace Veldrid.Graphics.Vulkan
{
    public unsafe class VkRegularFramebuffer : VkFramebufferBase
    {
        protected readonly VkDevice _device;
        protected readonly VkPhysicalDevice _physicalDevice;

        private VkTexture2D _colorTexture;
        private VkTexture2D _depthTexture;
        private VkFramebuffer _framebuffer;
        private VkRenderPass _renderPassClear;
        private VkRenderPass _renderPassNoClear;

        public VkRegularFramebuffer(VkDevice device, VkPhysicalDevice physicalDevice)
        {
            _device = device;
            _physicalDevice = physicalDevice;
        }

        public override VkTexture2D ColorTexture { get => _colorTexture; set => AttachColorTexture(0, value); }
        public override VkTexture2D DepthTexture
        {
            get => _depthTexture;
            set
            {
                _depthTexture = value;
                VkImageViewCreateInfo imageViewCI = VkImageViewCreateInfo.New();
                imageViewCI.image = _depthTexture.DeviceImage;
                imageViewCI.viewType = VkImageViewType.Image2D;
                imageViewCI.format = _depthTexture.Format;
                imageViewCI.subresourceRange.aspectMask = VkImageAspectFlags.Depth;
                imageViewCI.subresourceRange.levelCount = 1;
                imageViewCI.subresourceRange.layerCount = 1;
                vkCreateImageView(_device, ref imageViewCI, null, out VkImageView depthView);
                DepthView = depthView;

                RecreateRenderPass();
            }
        }

        public VkImageView ColorView { get; private set; }
        public VkImageView DepthView { get; private set; }

        public override int Width => ColorTexture != null ? ColorTexture.Width : DepthTexture != null ? DepthTexture.Width : 0;
        public override int Height => ColorTexture != null ? ColorTexture.Height : DepthTexture != null ? DepthTexture.Height : 0;

        public override VkRenderPass RenderPassClearBuffer => _renderPassClear;
        public override VkRenderPass RenderPassNoClear => _renderPassNoClear;
        public override VkFramebuffer VkFramebuffer => _framebuffer;

        public override void AttachColorTexture(int index, VkTexture2D texture)
        {
            if (index != 0)
            {
                throw new NotImplementedException();
            }

            _colorTexture = (VkTexture2D)texture;
            VkImageViewCreateInfo imageViewCI = VkImageViewCreateInfo.New();
            imageViewCI.image = _colorTexture.DeviceImage;
            imageViewCI.viewType = VkImageViewType.Image2D;
            imageViewCI.format = _colorTexture.Format;
            imageViewCI.subresourceRange.aspectMask = VkImageAspectFlags.Color;
            imageViewCI.subresourceRange.levelCount = 1;
            imageViewCI.subresourceRange.layerCount = 1;
            vkCreateImageView(_device, ref imageViewCI, null, out VkImageView colorView);
            ColorView = colorView;

            RecreateRenderPass();
        }

        private void RecreateRenderPass()
        {
            VkRenderPassCreateInfo renderPassCI = VkRenderPassCreateInfo.New();

            VkAttachmentDescription colorAttachmentDesc = new VkAttachmentDescription();
            colorAttachmentDesc.format = ColorTexture?.Format ?? 0;
            colorAttachmentDesc.samples = VkSampleCountFlags.Count1;
            colorAttachmentDesc.loadOp = VkAttachmentLoadOp.Clear;
            colorAttachmentDesc.storeOp = VkAttachmentStoreOp.Store;
            colorAttachmentDesc.stencilLoadOp = VkAttachmentLoadOp.DontCare;
            colorAttachmentDesc.stencilStoreOp = VkAttachmentStoreOp.DontCare;
            colorAttachmentDesc.initialLayout = VkImageLayout.Undefined;
            colorAttachmentDesc.finalLayout = VkImageLayout.PresentSrc;

            VkAttachmentReference colorAttachmentRef = new VkAttachmentReference();
            colorAttachmentRef.attachment = 0;
            colorAttachmentRef.layout = VkImageLayout.ColorAttachmentOptimal;

            VkAttachmentDescription depthAttachmentDesc = new VkAttachmentDescription();
            VkAttachmentReference depthAttachmentRef = new VkAttachmentReference();
            if (DepthTexture != null)
            {
                depthAttachmentDesc.format = DepthTexture.Format;
                depthAttachmentDesc.samples = VkSampleCountFlags.Count1;
                depthAttachmentDesc.loadOp = VkAttachmentLoadOp.Clear;
                depthAttachmentDesc.storeOp = VkAttachmentStoreOp.Store;
                depthAttachmentDesc.stencilLoadOp = VkAttachmentLoadOp.DontCare;
                depthAttachmentDesc.stencilStoreOp = VkAttachmentStoreOp.DontCare;
                depthAttachmentDesc.initialLayout = VkImageLayout.Undefined;
                depthAttachmentDesc.finalLayout = VkImageLayout.DepthStencilAttachmentOptimal;

                depthAttachmentRef.attachment = ColorTexture == null ? 0u : 1u;
                depthAttachmentRef.layout = VkImageLayout.DepthStencilAttachmentOptimal;
            }

            VkSubpassDescription subpass = new VkSubpassDescription();
            StackList<VkAttachmentDescription, Size512Bytes> attachments = new StackList<VkAttachmentDescription, Size512Bytes>();
            subpass.pipelineBindPoint = VkPipelineBindPoint.Graphics;
            if (ColorTexture != null)
            {
                subpass.colorAttachmentCount = 1;
                subpass.pColorAttachments = &colorAttachmentRef;
                attachments.Add(colorAttachmentDesc);
            }

            if (DepthTexture != null)
            {
                subpass.pDepthStencilAttachment = &depthAttachmentRef;
                attachments.Add(depthAttachmentDesc);
            }

            VkSubpassDependency subpassDependency = new VkSubpassDependency();
            subpassDependency.srcSubpass = SubpassExternal;
            subpassDependency.srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput;
            subpassDependency.dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput;
            subpassDependency.dstAccessMask = VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite;
            if (DepthTexture != null)
            {
                subpassDependency.dstAccessMask |= VkAccessFlags.DepthStencilAttachmentRead | VkAccessFlags.DepthStencilAttachmentWrite;
            }

            renderPassCI.attachmentCount = attachments.Count;
            renderPassCI.pAttachments = (VkAttachmentDescription*)attachments.Data;
            renderPassCI.subpassCount = 1;
            renderPassCI.pSubpasses = &subpass;
            renderPassCI.dependencyCount = 1;
            renderPassCI.pDependencies = &subpassDependency;

            VkResult result = vkCreateRenderPass(_device, ref renderPassCI, null, out _renderPassClear);
            CheckResult(result);

            for (int i = 0; i < attachments.Count; i++)
            {
                attachments[i].loadOp = VkAttachmentLoadOp.Load;
            }

            result = vkCreateRenderPass(_device, ref renderPassCI, null, out _renderPassNoClear);
            CheckResult(result);

            StackList<VkImageView, Size2IntPtr> fbAttachments = new StackList<VkImageView, Size2IntPtr>();

            if (ColorView != VkImageView.Null)
            {
                fbAttachments.Add(ColorView);
            }
            if (DepthView != VkImageView.Null)
            {
                fbAttachments.Add(DepthView);
            }

            uint width, height;
            if (ColorTexture != null)
            {
                width = (uint)ColorTexture.Width;
                height = (uint)ColorTexture.Height;
            }
            else
            {
                Debug.Assert(DepthTexture != null);
                width = (uint)DepthTexture.Width;
                height = (uint)DepthTexture.Height;
            }

            VkFramebufferCreateInfo framebufferCI = VkFramebufferCreateInfo.New();
            framebufferCI.renderPass = _renderPassClear;
            framebufferCI.attachmentCount = fbAttachments.Count;
            framebufferCI.pAttachments = (VkImageView*)fbAttachments.Data;
            framebufferCI.width = width;
            framebufferCI.height = height;
            framebufferCI.layers = 1;
            vkCreateFramebuffer(_device, ref framebufferCI, null, out VkFramebuffer newFramebuffer);

            _framebuffer = newFramebuffer;
        }

        public override VkTexture2D GetColorTexture(int index)
        {
            if (index != 0)
            {
                throw new NotImplementedException();
            }

            return ColorTexture;
        }

        public override void Dispose()
        {
            if (RenderPassClearBuffer != VkRenderPass.Null)
            {
                vkDestroyRenderPass(_device, RenderPassClearBuffer, null);
            }
            if (VkFramebuffer != VkFramebuffer.Null)
            {
                vkDestroyFramebuffer(_device, VkFramebuffer, null);
            }
        }
    }
}