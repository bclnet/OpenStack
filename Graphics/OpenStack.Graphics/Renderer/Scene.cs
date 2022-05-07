using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenStack.Graphics.Renderer
{
    public class Scene
    {
        public class UpdateContext
        {
            public float Timestep { get; }
            public UpdateContext(float timestep) => Timestep = timestep;
        }

        public class RenderContext
        {
            public Camera Camera { get; }
            public RenderPass RenderPass { get; }

            public RenderContext(Camera camera, RenderPass renderPass)
            {
                Camera = camera;
                RenderPass = renderPass;
            }
        }

        public Camera MainCamera { get; set; }
        public IOpenGraphic Graphic { get; }
        public Octree<SceneNode> StaticOctree { get; }
        public Octree<SceneNode> DynamicOctree { get; }

        public IEnumerable<SceneNode> AllNodes
            => _staticNodes.Concat(_dynamicNodes);

        readonly List<SceneNode> _staticNodes = new List<SceneNode>();
        readonly List<SceneNode> _dynamicNodes = new List<SceneNode>();

        readonly Action<List<MeshBatchRequest>, RenderContext> _meshBatchRenderer;

        public Scene(IOpenGraphic graphic, Action<List<MeshBatchRequest>, RenderContext> meshBatchRenderer, float sizeHint = 32768)
        {
            Graphic = graphic ?? throw new ArgumentNullException(nameof(graphic));
            _meshBatchRenderer = meshBatchRenderer ?? throw new ArgumentNullException(nameof(meshBatchRenderer));
            StaticOctree = new Octree<SceneNode>(sizeHint);
            DynamicOctree = new Octree<SceneNode>(sizeHint);
        }

        public void Add(SceneNode node, bool dynamic)
        {
            if (dynamic)
            {
                _dynamicNodes.Add(node);
                DynamicOctree.Insert(node, node.BoundingBox);
            }
            else
            {
                _staticNodes.Add(node);
                StaticOctree.Insert(node, node.BoundingBox);
            }
        }

        public void Update(float timestep)
        {
            var updateContext = new UpdateContext(timestep);
            foreach (var node in _staticNodes) node.Update(updateContext);
            foreach (var node in _dynamicNodes)
            {
                var oldBox = node.BoundingBox;
                node.Update(updateContext);
                DynamicOctree.Update(node, oldBox, node.BoundingBox);
            }
        }

        public void RenderWithCamera(Camera camera, Frustum cullFrustum = null)
        {
            var allNodes = StaticOctree.Query(cullFrustum ?? camera.ViewFrustum);
            allNodes.AddRange(DynamicOctree.Query(cullFrustum ?? camera.ViewFrustum));

            // Collect mesh calls
            var opaqueDrawCalls = new List<MeshBatchRequest>();
            var blendedDrawCalls = new List<MeshBatchRequest>();
            var looseNodes = new List<SceneNode>();
            foreach (var node in allNodes)
            {
                if (node is IMeshCollection meshCollection)
                    foreach (var mesh in meshCollection.Meshes)
                    {
                        foreach (var call in mesh.DrawCallsOpaque)
                            opaqueDrawCalls.Add(new MeshBatchRequest
                            {
                                Transform = node.Transform,
                                Mesh = mesh,
                                Call = call,
                                DistanceFromCamera = (node.BoundingBox.Center - camera.Location).LengthSquared(),
                            });

                        foreach (var call in mesh.DrawCallsBlended)
                            blendedDrawCalls.Add(new MeshBatchRequest
                            {
                                Transform = node.Transform,
                                Mesh = mesh,
                                Call = call,
                                DistanceFromCamera = (node.BoundingBox.Center - camera.Location).LengthSquared(),
                            });
                    }
                else looseNodes.Add(node);
            }

            // Sort loose nodes by distance from camera
            looseNodes.Sort((a, b) =>
            {
                var aLength = (a.BoundingBox.Center - camera.Location).LengthSquared();
                var bLength = (b.BoundingBox.Center - camera.Location).LengthSquared();
                return bLength.CompareTo(aLength);
            });

            // Opaque render pass
            var opaqueRenderContext = new RenderContext(camera, RenderPass.Opaque);
            _meshBatchRenderer(opaqueDrawCalls, opaqueRenderContext);
            foreach (var node in looseNodes) node.Render(opaqueRenderContext);

            // Blended render pass, back to front for loose nodes
            var blendedRenderContext = new RenderContext(camera, RenderPass.Translucent);
            _meshBatchRenderer(blendedDrawCalls, blendedRenderContext);
            foreach (var node in Enumerable.Reverse(looseNodes)) node.Render(blendedRenderContext);
        }

        public void SetEnabledLayers(HashSet<string> layers)
        {
            foreach (var renderer in AllNodes) renderer.LayerEnabled = layers.Contains(renderer.LayerName);

            StaticOctree.Clear();
            DynamicOctree.Clear();

            foreach (var node in _staticNodes) if (node.LayerEnabled) StaticOctree.Insert(node, node.BoundingBox);
            foreach (var node in _dynamicNodes) if (node.LayerEnabled) DynamicOctree.Insert(node, node.BoundingBox);
        }
    }
}
