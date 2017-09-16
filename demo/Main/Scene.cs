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

        public void Render(
            RenderContext rc,
            SceneContext sc,
            RenderPasses pass,
            Comparer<RenderItemIndex> comparer = null)
        {
            _renderQueue.Clear();
            _cullableStage.Clear();
            BoundingFrustum frustum = new BoundingFrustum(_camera.ViewMatrix * _camera.ProjectionMatrix);
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
        private readonly List<CullRenderable> _cullableStage = new List<CullRenderable>();
        private readonly List<Renderable> _renderableStage = new List<Renderable>();

        private void CollectVisibleObjects(
            ref BoundingFrustum frustum,
            RenderPasses renderPass,
            List<CullRenderable> renderables)
        {
            _octree.GetContainedObjects(frustum, renderables, GetFilter(renderPass));
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
