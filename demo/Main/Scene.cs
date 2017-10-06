using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Veldrid.Graphics;

namespace Veldrid.NeoDemo
{
    public class Scene
    {
        private readonly Octree<CullRenderable> _octree
            = new Octree<CullRenderable>(new BoundingBox(Vector3.One * -50, Vector3.One * 50), 2);

        private readonly List<Renderable> _freeRenderables = new List<Renderable>();
        private readonly List<IUpdateable> _updateables = new List<IUpdateable>();

        private readonly Dictionary<RenderPasses, Func<CullRenderable, bool>> _filters
            = new Dictionary<RenderPasses, Func<CullRenderable, bool>>();

        private readonly Camera _camera;

        public Camera Camera => _camera;

        public Scene(int viewWidth, int viewHeight)
        {
            _camera = new Camera(viewWidth, viewHeight);
            _updateables.Add(_camera);
        }

        public void AddRenderable(Renderable r)
        {
            if (r is CullRenderable cr)
            {
                _octree.AddItem(cr.BoundingBox, cr);
            }
            else
            {
                _freeRenderables.Add(r);
            }
        }

        public void AddUpdateable(IUpdateable updateable)
        {
            Debug.Assert(updateable != null);
            _updateables.Add(updateable);
        }

        public void Update(float deltaSeconds)
        {
            foreach (IUpdateable updateable in _updateables)
            {
                updateable.Update(deltaSeconds);
            }
        }

        public void RenderAllStages(RenderContext rc, SceneContext sc)
        {
            // Total guesses.
            float nearCascadeLimit = 15;
            float midCascadeLimit = 30;
            float farCascadeLimit = 45;

            sc.DepthLimitsBuffer.SetData(new DepthCascadeLimits
            {
                NearLimit = nearCascadeLimit,
                MidLimit = midCascadeLimit,
                FarLimit = farCascadeLimit
            });

            sc.LightInfoBuffer.SetData(sc.DirectionalLight.GetInfo());

            // Near
            Matrix4x4 viewProj0 = UpdateDirectionalLightMatrices(
                sc,
                Camera.NearDistance,
                nearCascadeLimit,
                sc.NearShadowMapTexture.Width,
                out BoundingFrustum lightFrustum);
            sc.LightViewProjectionBuffer0.SetData(ref viewProj0);
            sc.CurrentLightViewProjectionBuffer = sc.LightViewProjectionBuffer0;
            rc.SetFramebuffer(sc.NearShadowMapFramebuffer);
            rc.SetViewport(0, 0, sc.NearShadowMapTexture.Width, sc.NearShadowMapTexture.Height);
            rc.ClearBuffer();
            Render(rc, sc, RenderPasses.ShadowMap, lightFrustum, null);
            rc.SetDefaultFramebuffer();
            rc.SetViewport(0, 0, rc.CurrentFramebuffer.Width, rc.CurrentFramebuffer.Height);

            // Mid
            Matrix4x4 viewProj1 = UpdateDirectionalLightMatrices(
                sc,
                nearCascadeLimit,
                midCascadeLimit,
                sc.MidShadowMapTexture.Width,
                out lightFrustum);
            sc.LightViewProjectionBuffer1.SetData(ref viewProj1);
            sc.CurrentLightViewProjectionBuffer = sc.LightViewProjectionBuffer1;
            rc.SetFramebuffer(sc.MidShadowMapFramebuffer);
            rc.SetViewport(0, 0, sc.MidShadowMapTexture.Width, sc.MidShadowMapTexture.Height);
            rc.ClearBuffer();
            Render(rc, sc, RenderPasses.ShadowMap, lightFrustum, null);
            rc.SetDefaultFramebuffer();
            rc.SetViewport(0, 0, rc.CurrentFramebuffer.Width, rc.CurrentFramebuffer.Height);

            // Far
            Matrix4x4 viewProj2 = UpdateDirectionalLightMatrices(
                sc,
                midCascadeLimit,
                farCascadeLimit,
                sc.FarShadowMapTexture.Width,
                out lightFrustum);
            sc.LightViewProjectionBuffer2.SetData(ref viewProj2);
            sc.CurrentLightViewProjectionBuffer = sc.LightViewProjectionBuffer2;
            rc.SetFramebuffer(sc.FarShadowMapFramebuffer);
            rc.SetViewport(0, 0, sc.FarShadowMapTexture.Width, sc.FarShadowMapTexture.Height);
            rc.ClearBuffer();
            Render(rc, sc, RenderPasses.ShadowMap, lightFrustum, null);
            rc.SetDefaultFramebuffer();
            rc.SetViewport(0, 0, rc.CurrentFramebuffer.Width, rc.CurrentFramebuffer.Height);

            BoundingFrustum cameraFrustum = new BoundingFrustum(_camera.ViewMatrix * _camera.ProjectionMatrix);
            Render(rc, sc, RenderPasses.Standard, cameraFrustum, null);
            Render(rc, sc, RenderPasses.AlphaBlend, cameraFrustum, null);
            Render(rc, sc, RenderPasses.Overlay, cameraFrustum, null);
        }

        private Matrix4x4 UpdateDirectionalLightMatrices(
            SceneContext sc,
            float near,
            float far,
            int shadowMapWidth,
            out BoundingFrustum lightFrustum)
        {
            Vector3 lightDir = sc.DirectionalLight.Direction;
            Vector3 viewDir = sc.Camera.LookDirection;
            Vector3 viewPos = sc.Camera.Position;
            Vector3 unitY = Vector3.UnitY;
            FrustumHelpers.ComputePerspectiveFrustumCorners(
                ref viewPos,
                ref viewDir,
                ref unitY,
                sc.Camera.FieldOfView,
                near,
                far,
                sc.Camera.AspectRatio,
                out FrustumCorners cameraCorners);

            // Approach used: http://alextardif.com/ShadowMapping.html

            Vector3 frustumCenter = Vector3.Zero;
            frustumCenter += cameraCorners.NearTopLeft;
            frustumCenter += cameraCorners.NearTopRight;
            frustumCenter += cameraCorners.NearBottomLeft;
            frustumCenter += cameraCorners.NearBottomRight;
            frustumCenter += cameraCorners.FarTopLeft;
            frustumCenter += cameraCorners.FarTopRight;
            frustumCenter += cameraCorners.FarBottomLeft;
            frustumCenter += cameraCorners.FarBottomRight;
            frustumCenter /= 8f;

            float radius = (cameraCorners.NearTopLeft - cameraCorners.FarBottomRight).Length() / 2.0f;
            float texelsPerUnit = shadowMapWidth / (radius * 2.0f);

            Matrix4x4 scalar = Matrix4x4.CreateScale(texelsPerUnit, texelsPerUnit, texelsPerUnit);

            Vector3 baseLookAt = -lightDir;

            Matrix4x4 lookat = Matrix4x4.CreateLookAt(Vector3.Zero, baseLookAt, Vector3.UnitY);
            lookat = scalar * lookat;
            Matrix4x4.Invert(lookat, out Matrix4x4 lookatInv);

            frustumCenter = Vector3.Transform(frustumCenter, lookat);
            frustumCenter.X = (int)frustumCenter.X;
            frustumCenter.Y = (int)frustumCenter.Y;
            frustumCenter = Vector3.Transform(frustumCenter, lookatInv);

            Vector3 lightPos = frustumCenter - (lightDir * radius * 2f);

            Matrix4x4 lightView = Matrix4x4.CreateLookAt(lightPos, frustumCenter, Vector3.UnitY);

            Matrix4x4 lightProjection = Matrix4x4.CreateOrthographicOffCenter(
                -radius,
                radius,
                -radius,
                radius,
                -radius * 6f,
                radius * 6f);
            Matrix4x4 viewProjectionMatrix = lightView * lightProjection;

            lightFrustum = new BoundingFrustum(lightProjection);
            return viewProjectionMatrix;
        }

        public void Render(
            RenderContext rc,
            SceneContext sc,
            RenderPasses pass,
            BoundingFrustum frustum,
            Comparer<RenderItemIndex> comparer = null)
        {
            _renderQueue.Clear();

            _cullableStage.Clear();
            CollectVisibleObjects(ref frustum, pass, _cullableStage);
            _renderQueue.AddRange(_cullableStage, _camera.Position);

            _renderableStage.Clear();
            CollectFreeObjects(pass, _renderableStage);
            _renderQueue.AddRange(_renderableStage, _camera.Position);

            if (comparer == null)
            {
                _renderQueue.Sort();
            }
            else
            {
                _renderQueue.Sort(comparer);
            }

            foreach (Renderable renderable in _renderQueue)
            {
                renderable.Render(rc, sc, pass);
            }
        }

        private readonly RenderQueue _renderQueue = new RenderQueue();
        private readonly List<CullRenderable> _shadowmapStage = new List<CullRenderable>();
        private readonly List<CullRenderable> _cullableStage = new List<CullRenderable>();
        private readonly List<Renderable> _renderableStage = new List<Renderable>();

        private void CollectVisibleObjects(
            ref BoundingFrustum frustum,
            RenderPasses renderPass,
            List<CullRenderable> renderables)
        {
            _octree.GetAllContainedObjects(renderables, GetFilter(renderPass));
            //_octree.GetContainedObjects(frustum, renderables, GetFilter(renderPass));
        }

        private void CollectFreeObjects(RenderPasses renderPass, List<Renderable> renderables)
        {
            foreach (Renderable r in _freeRenderables)
            {
                if ((r.RenderPasses & renderPass) != 0)
                {
                    renderables.Add(r);
                }
            }
        }

        private Func<CullRenderable, bool> GetFilter(RenderPasses renderPass)
        {
            if (!_filters.TryGetValue(renderPass, out Func<CullRenderable, bool> filter))
            {
                filter = cr => (cr.RenderPasses & renderPass) != 0;
                _filters.Add(renderPass, filter);
            }

            return filter;
        }

        internal void DestroyAllDeviceObjects()
        {
            _cullableStage.Clear();
            _octree.GetAllContainedObjects(_cullableStage);
            foreach (CullRenderable cr in _cullableStage)
            {
                cr.DestroyDeviceObjects();
            }
            foreach (Renderable r in _freeRenderables)
            {
                r.DestroyDeviceObjects();
            }
        }

        internal void CreateAllDeviceObjects(RenderContext rc)
        {
            _cullableStage.Clear();
            _octree.GetAllContainedObjects(_cullableStage);
            foreach (CullRenderable cr in _cullableStage)
            {
                cr.CreateDeviceObjects(rc);
            }
            foreach (Renderable r in _freeRenderables)
            {
                r.CreateDeviceObjects(rc);
            }
        }
    }
}
