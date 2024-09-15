using OpenStack.Gfx.Renders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace OpenStack.Gfx.Scenes
{
    /// <summary>
    /// Octree
    /// </summary>
    public class Octree<T> where T : class
    {
        const int MaximumElementsBeforeSubdivide = 4;
        const float MinimumNodeSize = 64.0f;
        public Node Root;

        public struct Element
        {
            public T ClientObject;
            public AABB BoundingBox;
        }

        public class Node(Node parent, Vector3 regionMin, Vector3 regionSize)
        {
            public Node Parent = parent;
            public AABB Region = new(regionMin, regionMin + regionSize);
            public List<Element> Elements;
            public Node[] Children;

            public void Subdivide()
            {
                if (Children != null) return; // Already subdivided
                var subregionSize = Region.Size * 0.5f;
                var center = Region.Min + subregionSize;
                Children = [
                    new Node(this, Region.Min, subregionSize),
                    new Node(this, new Vector3(center.X, Region.Min.Y, Region.Min.Z), subregionSize),
                    new Node(this, new Vector3(Region.Min.X, center.Y, Region.Min.Z), subregionSize),
                    new Node(this, new Vector3(center.X, center.Y, Region.Min.Z), subregionSize),
                    new Node(this, new Vector3(Region.Min.X, Region.Min.Y, center.Z), subregionSize),
                    new Node(this, new Vector3(center.X, Region.Min.Y, center.Z), subregionSize),
                    new Node(this, new Vector3(Region.Min.X, center.Y, center.Z), subregionSize),
                    new Node(this, new Vector3(center.X, center.Y, center.Z), subregionSize)];
                var remainingElements = new List<Element>();
                foreach (var element in Elements)
                {
                    var movedDown = false;
                    foreach (var child in Children)
                        if (child.Region.Contains(element.BoundingBox))
                        {
                            child.Insert(element);
                            movedDown = true;
                            break;
                        }
                    if (!movedDown)
                        remainingElements.Add(element);
                }
                Elements = remainingElements;
            }

            public bool HasChildren => Children != null;

            public bool HasElements => Elements != null && Elements.Count > 0;

            public void Insert(Element element)
            {
                if (!HasChildren && HasElements && Region.Size.X > MinimumNodeSize && Elements.Count >= MaximumElementsBeforeSubdivide) Subdivide();
                var inserted = false;
                if (HasChildren)
                {
                    var elementBB = element.BoundingBox;
                    foreach (var child in Children)
                        if (child.Region.Contains(elementBB))
                        {
                            inserted = true;
                            child.Insert(element);
                            break;
                        }
                }
                if (!inserted)
                {
                    Elements ??= [];
                    Elements.Add(element);
                }
            }

            public (Node Node, int Index) Find(T clientObject, AABB bounds)
            {
                if (HasElements)
                    for (var i = 0; i < Elements.Count; i++)
                        if (Elements[i].ClientObject == clientObject) return (this, i);
                if (HasChildren)
                    foreach (var child in Children)
                        if (child.Region.Contains(bounds)) return child.Find(clientObject, bounds);
                return (null, -1);
            }

            public void Clear()
            {
                Elements = null;
                Children = null;
            }

            public void Query(object source, List<T> results)
            {
                switch (source)
                {
                    case AABB boundingBox:
                        if (HasElements)
                            foreach (var element in Elements)
                                if (element.BoundingBox.Intersects(boundingBox)) results.Add(element.ClientObject);
                        if (HasChildren)
                            foreach (var child in Children)
                                if (child.Region.Intersects(boundingBox)) child.Query(boundingBox, results);
                        break;
                    case Frustum frustum:
                        if (HasElements)
                            foreach (var element in Elements)
                                if (frustum.Intersects(element.BoundingBox)) results.Add(element.ClientObject);
                        if (HasChildren)
                            foreach (var child in Children)
                                if (frustum.Intersects(child.Region)) child.Query(frustum, results);
                        break;
                }
            }
        }

        public Octree(float size)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            Root = new Node(null, new Vector3(-size * 0.5f), new Vector3(size));
        }

        public void Insert(T obj, AABB bounds)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            Root.Insert(new Element { ClientObject = obj, BoundingBox = bounds });
        }

        public void Remove(T obj, AABB bounds)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var (node, index) = Root.Find(obj, bounds);
            node?.Elements.RemoveAt(index);
        }

        public void Update(T obj, AABB oldBounds, AABB newBounds)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var (node, index) = Root.Find(obj, oldBounds);
            if (node != null)
            {
                // Locate the closest ancestor that the new bounds fit inside
                var ancestor = node;
                while (ancestor.Parent != null && !ancestor.Region.Contains(newBounds)) ancestor = ancestor.Parent;

                // Still fits in same node?
                if (ancestor == node)
                {
                    // Still check for pushdown
                    if (node.HasChildren)
                        foreach (var child in node.Children)
                            if (child.Region.Contains(newBounds))
                            {
                                node.Elements.RemoveAt(index);
                                child.Insert(new Element { ClientObject = obj, BoundingBox = newBounds });
                                return;
                            }

                    // Not pushed down into any children
                    node.Elements[index] = new Element { ClientObject = obj, BoundingBox = newBounds };
                }
                else
                {
                    node.Elements.RemoveAt(index);
                    ancestor.Insert(new Element { ClientObject = obj, BoundingBox = newBounds });
                }
            }
        }

        public void Clear() => Root.Clear();

        public List<T> Query(object source)
        {
            var results = new List<T>();
            Root.Query(source, results);
            return results;
        }
    }

    /// <summary>
    /// Scene
    /// </summary>
    public class Scene(IOpenGfx gfx, Action<List<MeshBatchRequest>, Scene.RenderContext> meshBatchRenderer, float sizeHint = 32768)
    {
        public Camera MainCamera;
        public Vector3? LightPosition;
        public IOpenGfx Gfx = gfx ?? throw new ArgumentNullException(nameof(gfx));
        public Octree<SceneNode> StaticOctree = new Octree<SceneNode>(sizeHint);
        public Octree<SceneNode> DynamicOctree = new Octree<SceneNode>(sizeHint);
        public bool ShowDebug;
        public IEnumerable<SceneNode> AllNodes => StaticNodes.Concat(DynamicNodes);
        List<SceneNode> StaticNodes = [];
        List<SceneNode> DynamicNodes = [];
        Action<List<MeshBatchRequest>, RenderContext> MeshBatchRenderer = meshBatchRenderer ?? throw new ArgumentNullException(nameof(meshBatchRenderer));

        public class UpdateContext(float timestep)
        {
            public float Timestep = timestep;
        }

        public class RenderContext
        {
            public Camera Camera;
            public Vector3? LightPosition;
            public RenderPass RenderPass;
            public Shader ReplacementShader;
            public bool ShowDebug;
        }

        public void Add(SceneNode node, bool dynamic)
        {
            if (dynamic)
            {
                DynamicNodes.Add(node);
                DynamicOctree.Insert(node, node.BoundingBox);
                node.Id = DynamicNodes.Count * 2 - 1;
            }
            else
            {
                StaticNodes.Add(node);
                StaticOctree.Insert(node, node.BoundingBox);
                node.Id = StaticNodes.Count * 2;
            }
        }

        public SceneNode Find(int id)
        {
            if (id == 0) return null;
            else if (id % 2 == 1)
            {
                var index = (id + 1) / 2 - 1;
                return index >= DynamicNodes.Count ? null : DynamicNodes[index];
            }
            else
            {
                var index = id / 2 - 1;
                return index >= StaticNodes.Count ? null : StaticNodes[index];
            }
        }

        public void Update(float timestep)
        {
            var updateContext = new UpdateContext(timestep);
            foreach (var node in StaticNodes) node.Update(updateContext);
            foreach (var node in DynamicNodes) { var oldBox = node.BoundingBox; node.Update(updateContext); DynamicOctree.Update(node, oldBox, node.BoundingBox); }
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
                if (node is IMeshCollection s)
                    foreach (var mesh in s.RenderableMeshes)
                    {
                        foreach (var call in mesh.DrawCallsOpaque)
                            opaqueDrawCalls.Add(new MeshBatchRequest
                            {
                                Transform = node.Transform,
                                Mesh = mesh,
                                Call = call,
                                DistanceFromCamera = (node.BoundingBox.Center - camera.Location).LengthSquared(),
                                NodeId = node.Id,
                                MeshId = mesh.MeshIndex,
                            });
                        foreach (var call in mesh.DrawCallsBlended)
                            blendedDrawCalls.Add(new MeshBatchRequest
                            {
                                Transform = node.Transform,
                                Mesh = mesh,
                                Call = call,
                                DistanceFromCamera = (node.BoundingBox.Center - camera.Location).LengthSquared(),
                                NodeId = node.Id,
                                MeshId = mesh.MeshIndex,
                            });
                    }
                else looseNodes.Add(node);

            // Sort loose nodes by distance from camera
            looseNodes.Sort((a, b) =>
            {
                var aLength = (a.BoundingBox.Center - camera.Location).LengthSquared();
                var bLength = (b.BoundingBox.Center - camera.Location).LengthSquared();
                return bLength.CompareTo(aLength);
            });

            // Opaque render pass
            var renderContext = new RenderContext
            {
                Camera = camera,
                LightPosition = LightPosition,
                RenderPass = RenderPass.Opaque,
                ShowDebug = ShowDebug,
            };

            // Blended render pass, back to front for loose nodes
            if (camera.Picker != null)
                if (camera.Picker.IsActive) { camera.Picker.Render(); renderContext.ReplacementShader = camera.Picker.Shader; }
                else if (camera.Picker.Debug) renderContext.ReplacementShader = camera.Picker.DebugShader;
            MeshBatchRenderer(opaqueDrawCalls, renderContext);
            foreach (var node in looseNodes) node.Render(renderContext);
            if (camera.Picker != null && camera.Picker.IsActive)
            {
                camera.Picker.Finish();
                RenderWithCamera(camera, cullFrustum);
            }
        }

        public void SetEnabledLayers(HashSet<string> layers)
        {
            foreach (var renderer in AllNodes) renderer.LayerEnabled = layers.Contains(renderer.LayerName);
            StaticOctree.Clear();
            DynamicOctree.Clear();
            foreach (var node in StaticNodes)
                if (node.LayerEnabled) StaticOctree.Insert(node, node.BoundingBox);
            foreach (var node in DynamicNodes)
                if (node.LayerEnabled) DynamicOctree.Insert(node, node.BoundingBox);
        }
    }

    /// <summary>
    /// SceneNode
    /// </summary>
    public abstract class SceneNode(Scene scene)
    {
        Matrix4x4 _transform = Matrix4x4.Identity;
        AABB _localBoundingBox;
        public Matrix4x4 Transform
        {
            get => _transform;
            set { _transform = value; BoundingBox = _localBoundingBox.Transform(_transform); }
        }
        public string LayerName;
        public bool LayerEnabled = true;
        public AABB BoundingBox;
        public AABB LocalBoundingBox
        {
            get => _localBoundingBox;
            protected set { _localBoundingBox = value; BoundingBox = _localBoundingBox.Transform(_transform); }
        }
        public string Name;
        public int Id;
        public Scene Scene = scene;

        public abstract void Update(Scene.UpdateContext context);
        public abstract void Render(Scene.RenderContext context);
        public virtual IEnumerable<string> GetSupportedRenderModes() => [];
        public virtual void SetRenderMode(string mode) { }
    }
}
