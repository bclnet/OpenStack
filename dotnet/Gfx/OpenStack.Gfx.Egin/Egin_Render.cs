using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace OpenStack.Gfx.Egin;

#region Model

/// <summary>
/// IPickingTexture
/// </summary>
public interface IPickingTexture {
    bool IsActive { get; }
    bool Debug { get; }
    Shader Shader { get; }
    Shader DebugShader { get; }
    void Render();
    void Resize(int width, int height);
    void Finish();
}

/// <summary>
/// OnDiskBufferData
/// </summary>
public struct OnDiskBufferData {
    public uint ElementCount;
    public uint ElementSizeInBytes; // stride for vertices. Type for indices
    public Attribute[] Attributes; // Vertex attribs. Empty for index buffers
    public byte[] Data;

    public enum RenderSlotType {
        RENDER_SLOT_INVALID = -1,
        RENDER_SLOT_PER_VERTEX = 0,
        RENDER_SLOT_PER_INSTANCE = 1
    }

    public struct Attribute {
        public string SemanticName;
        public int SemanticIndex;
        public DXGI_FORMAT Format;
        public uint Offset;
        public int Slot;
        public RenderSlotType SlotType;
        public int InstanceStepRate;
    }
}

/// <summary>
/// IVBIB
/// </summary>
public interface IVBIB {
    List<OnDiskBufferData> VertexBuffers { get; }
    List<OnDiskBufferData> IndexBuffers { get; }
    IVBIB RemapBoneIndices(int[] remapTable);
}

/// <summary>
/// IEginModel
/// </summary>
public interface IEginModel : IModel {
    IVBIB RemapBoneIndices(IVBIB vbib, int meshIndex);
}

/// <summary>
/// AABB
/// </summary>
public struct AABB {
    public Vector3 Min;
    public Vector3 Max;
    public Vector3 Size => Max - Min;
    public Vector3 Center => (Min + Max) * 0.5f;

    public override string ToString() => $"AABB [({Min.X},{Min.Y},{Min.Z}) -> ({Max.X},{Max.Y},{Max.Z}))";

    public AABB(Vector3 min, Vector3 max) {
        Min = min;
        Max = max;
    }
    public AABB(float minX, float minY, float minZ, float maxX, float maxY, float maxZ) {
        Min = new Vector3(minX, minY, minZ);
        Max = new Vector3(maxX, maxY, maxZ);
    }

    public bool Contains(object source)
        => source switch {
            Vector3 point =>
                point.X >= Min.X && point.X < Max.X &&
                point.Y >= Min.Y && point.Y < Max.Y &&
                point.Z >= Min.Z && point.Z < Max.Z,
            AABB other =>
                other.Min.X >= Min.X && other.Max.X <= Max.X &&
                other.Min.Y >= Min.Y && other.Max.Y <= Max.Y &&
                other.Min.Z >= Min.Z && other.Max.Z <= Max.Z,
            _ => throw new ArgumentOutOfRangeException(nameof(source)),
        };

    public bool Intersects(AABB other)
        => other.Max.X >= Min.X && other.Min.X < Max.X &&
        other.Max.Y >= Min.Y && other.Min.Y < Max.Y &&
        other.Max.Z >= Min.Z && other.Min.Z < Max.Z;

    public AABB Union(AABB other)
        => new(Vector3.Min(Min, other.Min), Vector3.Max(Max, other.Max));

    public AABB Translate(Vector3 offset)
        => new(Min + offset, Max + offset);

    // Note: Since we're dealing with AABBs here, the resulting AABB is likely to be bigger than the original if rotation
    // and whatnot is involved. This problem compounds with multiple transformations. Therefore, endeavour to premultiply matrices
    // and only use this at the last step.
    public AABB Transform(Matrix4x4 transform) {
        var points = new[]
        {
            Vector4.Transform(new Vector4(Min.X, Min.Y, Min.Z, 1.0f), transform),
            Vector4.Transform(new Vector4(Max.X, Min.Y, Min.Z, 1.0f), transform),
            Vector4.Transform(new Vector4(Max.X, Max.Y, Min.Z, 1.0f), transform),
            Vector4.Transform(new Vector4(Min.X, Max.Y, Min.Z, 1.0f), transform),
            Vector4.Transform(new Vector4(Min.X, Max.Y, Max.Z, 1.0f), transform),
            Vector4.Transform(new Vector4(Min.X, Min.Y, Max.Z, 1.0f), transform),
            Vector4.Transform(new Vector4(Max.X, Min.Y, Max.Z, 1.0f), transform),
            Vector4.Transform(new Vector4(Max.X, Max.Y, Max.Z, 1.0f), transform),
        };
        var min = points[0];
        var max = points[0];
        for (var i = 1; i < points.Length; ++i) {
            min = Vector4.Min(min, points[i]);
            max = Vector4.Max(max, points[i]);
        }
        return new AABB(new Vector3(min.X, min.Y, min.Z), new Vector3(max.X, max.Y, max.Z));
    }
}

/// <summary>
/// Frustum
/// </summary>
public class Frustum {
    Vector4[] Planes = new Vector4[6];

    public static Frustum CreateEmpty() => new() { Planes = [] };

    public void Update(Matrix4x4 viewProjectionMatrix) {
        Planes[0] = Vector4.Normalize(new Vector4(
            viewProjectionMatrix.M14 + viewProjectionMatrix.M11,
            viewProjectionMatrix.M24 + viewProjectionMatrix.M21,
            viewProjectionMatrix.M34 + viewProjectionMatrix.M31,
            viewProjectionMatrix.M44 + viewProjectionMatrix.M41));
        Planes[1] = Vector4.Normalize(new Vector4(
            viewProjectionMatrix.M14 - viewProjectionMatrix.M11,
            viewProjectionMatrix.M24 - viewProjectionMatrix.M21,
            viewProjectionMatrix.M34 - viewProjectionMatrix.M31,
            viewProjectionMatrix.M44 - viewProjectionMatrix.M41));
        Planes[2] = Vector4.Normalize(new Vector4(
            viewProjectionMatrix.M14 - viewProjectionMatrix.M12,
            viewProjectionMatrix.M24 - viewProjectionMatrix.M22,
            viewProjectionMatrix.M34 - viewProjectionMatrix.M32,
            viewProjectionMatrix.M44 - viewProjectionMatrix.M42));
        Planes[3] = Vector4.Normalize(new Vector4(
            viewProjectionMatrix.M14 + viewProjectionMatrix.M12,
            viewProjectionMatrix.M24 + viewProjectionMatrix.M22,
            viewProjectionMatrix.M34 + viewProjectionMatrix.M32,
            viewProjectionMatrix.M44 + viewProjectionMatrix.M42));
        Planes[4] = Vector4.Normalize(new Vector4(
            viewProjectionMatrix.M13,
            viewProjectionMatrix.M23,
            viewProjectionMatrix.M33,
            viewProjectionMatrix.M43));
        Planes[5] = Vector4.Normalize(new Vector4(
            viewProjectionMatrix.M14 - viewProjectionMatrix.M13,
            viewProjectionMatrix.M24 - viewProjectionMatrix.M23,
            viewProjectionMatrix.M34 - viewProjectionMatrix.M33,
            viewProjectionMatrix.M44 - viewProjectionMatrix.M43));
    }

    public Frustum Clone() {
        var r = new Frustum();
        Planes.CopyTo(r.Planes, 0);
        return r;
    }

    public bool Intersects(AABB box) {
        for (var i = 0; i < Planes.Length; ++i) {
            var closest = new Vector3(
                Planes[i].X < 0 ? box.Min.X : box.Max.X,
                Planes[i].Y < 0 ? box.Min.Y : box.Max.Y,
                Planes[i].Z < 0 ? box.Min.Z : box.Max.Z);
            if (Vector3.Dot(new Vector3(Planes[i].X, Planes[i].Y, Planes[i].Z), closest) + Planes[i].W < 0) return false;
        }
        return true;
    }
}

/// <summary>
/// IMesh
/// </summary>
public interface IMesh {
    IDictionary<string, object> Data { get; }
    IVBIB VBIB { get; }
    Vector3 MinBounds { get; }
    Vector3 MaxBounds { get; }
    void GetBounds();
}

/// <summary>
/// RenderMaterial
/// </summary>
public abstract class RenderMaterial {
    public MaterialShaderProp Material;
    public Dictionary<string, int> Textures = [];
    public bool IsBlended;
    public bool IsToolsMaterial;
    public float AlphaTestReference;
    public bool IsAdditiveBlend;
    public bool IsRenderBackfaces;

    public RenderMaterial(MaterialShaderProp material) {
        Material = material;
        switch (material) {
            //case MaterialPropShader p: break;
            case MaterialShaderVProp p:
                if (p.IntParams.ContainsKey("F_ALPHA_TEST") && p.IntParams["F_ALPHA_TEST"] == 1 && p.FloatParams.ContainsKey("g_flAlphaTestReference")) AlphaTestReference = p.FloatParams["g_flAlphaTestReference"];
                IsToolsMaterial = p.IntAttributes.ContainsKey("tools.toolsmaterial");
                IsBlended = (p.IntParams.ContainsKey("F_TRANSLUCENT") && p.IntParams["F_TRANSLUCENT"] == 1) || p.IntAttributes.ContainsKey("mapbuilder.water") || material.ShaderName == "vr_glass.vfx" || material.ShaderName == "tools_sprite.vfx";
                IsAdditiveBlend = p.IntParams.ContainsKey("F_ADDITIVE_BLEND") && p.IntParams["F_ADDITIVE_BLEND"] == 1;
                IsRenderBackfaces = p.IntParams.ContainsKey("F_RENDER_BACKFACES") && p.IntParams["F_RENDER_BACKFACES"] == 1;
                break;
            default: throw new ArgumentOutOfRangeException(nameof(material), $"{material}");
        }
    }

    public abstract void Render(Shader shader);

    public abstract void PostRender();
}

/// <summary>
/// DrawCall
/// </summary>
public class DrawCall {
    public int PrimitiveType;
    public Shader Shader;
    public uint BaseVertex;
    //public uint VertexCount;
    public uint StartIndex;
    public int IndexCount;
    //public uint InstanceIndex;
    //public uint InstanceCount;
    //public float UvDensity;
    //public string Flags;
    public Vector3 TintColor = Vector3.One;
    public RenderMaterial Material;
    public uint VertexArrayObject;
    public (uint Id, uint Offset) VertexBuffer;
    public int IndexType;
    public (uint Id, uint Offset) IndexBuffer;

    [Flags]
    public enum RenderMeshDrawPrimitiveFlags {
        None = 0x0,
        UseShadowFastPath = 0x1,
        UseCompressedNormalTangent = 0x2,
        IsOccluder = 0x4,
        InputLayoutIsNotMatchedToMaterial = 0x8,
        HasBakedLightingFromVertexStream = 0x10,
        HasBakedLightingFromLightmap = 0x20,
        CanBatchWithDynamicShaderConstants = 0x40,
        DrawLast = 0x80,
        HasPerInstanceBakedLightingData = 0x100,
    }

    public static bool IsCompressedNormalTangent(IDictionary<string, object> drawCall) {
        if (drawCall.ContainsKey("m_bUseCompressedNormalTangent")) return drawCall.Get<bool>("m_bUseCompressedNormalTangent");
        if (!drawCall.ContainsKey("m_nFlags")) return false;
        var flags = drawCall.Get<object>("m_nFlags");
        return flags switch {
            string s => s.Contains("MESH_DRAW_FLAGS_USE_COMPRESSED_NORMAL_TANGENT", StringComparison.InvariantCulture),
            long l => ((RenderMeshDrawPrimitiveFlags)l & RenderMeshDrawPrimitiveFlags.UseCompressedNormalTangent) != 0,
            int i => ((RenderMeshDrawPrimitiveFlags)i & RenderMeshDrawPrimitiveFlags.UseCompressedNormalTangent) != 0,
            byte b => ((RenderMeshDrawPrimitiveFlags)b & RenderMeshDrawPrimitiveFlags.UseCompressedNormalTangent) != 0,
            _ => false
        };
    }
}

/// <summary>
/// RenderableMesh
/// </summary>
public abstract class RenderableMesh {
    public AABB BoundingBox;
    public Vector4 Tint = Vector4.One;
    public List<DrawCall> DrawCallsAll = [];
    public List<DrawCall> DrawCallsOpaque = [];
    public List<DrawCall> DrawCallsBlended = [];
    public int? AnimationTexture;
    public int AnimationTextureSize;
    public float Time = 0f;
    public int MeshIndex;
    protected IMesh Mesh;
    protected IVBIB VBIB;

    public RenderableMesh(Action<RenderableMesh> action, IMesh mesh, int meshIndex, IDictionary<string, string> skinMaterials = null, IEginModel model = null) {
        action(this);
        Mesh = mesh;
        VBIB = model != null ? model.RemapBoneIndices(mesh.VBIB, meshIndex) : mesh.VBIB;
        Mesh.GetBounds();
        BoundingBox = new AABB(Mesh.MinBounds, Mesh.MaxBounds);
        MeshIndex = meshIndex;
        ConfigureDrawCalls(skinMaterials, true);
    }

    public IEnumerable<string> GetSupportedRenderModes() => DrawCallsAll.SelectMany(s => s.Shader.RenderModes).Distinct();

    public abstract void SetRenderMode(string renderMode);

    public void SetAnimationTexture(int? texture, int animationTextureSize) {
        AnimationTexture = texture;
        AnimationTextureSize = animationTextureSize;
    }

    public void Update(float timeStep) => Time += timeStep;

    public void SetSkin(IDictionary<string, string> skinMaterials) => ConfigureDrawCalls(skinMaterials, false);

    protected abstract void ConfigureDrawCalls(IDictionary<string, string> skinMaterials, bool firstSetup);
}

/// <summary>
/// MeshBatchRequest
/// </summary>
public class MeshBatchRequest {
    public Matrix4x4 Transform;
    public RenderableMesh Mesh;
    public DrawCall Call;
    public float DistanceFromCamera;
    public int NodeId;
    public int MeshId;
}

/// <summary>
/// IMeshCollection
/// </summary>
public interface IMeshCollection {
    IEnumerable<RenderableMesh> RenderableMeshes { get; }
}

#endregion

#region Camera

/// <summary>
/// Camera
/// </summary>
public abstract class Camera {
    protected const float CAMERASPEED = 300f; // Per second
    protected const float FOV = MathX.PiOver4;

    public Vector3 Location = new(1);
    public float Pitch;
    public float Yaw;
    public float Scale = 1.0f;
    public Matrix4x4 ProjectionMatrix;
    public Matrix4x4 CameraViewMatrix;
    public Matrix4x4 ViewProjectionMatrix;
    public Frustum ViewFrustum = new();
    public IPickingTexture Picker;
    public Vector2<int> WindowSize;
    public float AspectRatio;

    public Camera() => LookAt(new Vector3(0));

    protected void RecalculateMatrices() {
        CameraViewMatrix = Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateLookAt(Location, Location + GetForwardVector(), Vector3.UnitZ);
        ViewProjectionMatrix = CameraViewMatrix * ProjectionMatrix;
        ViewFrustum.Update(ViewProjectionMatrix);
    }

    // Calculate forward vector from pitch and yaw
    protected Vector3 GetForwardVector() => new((float)(Math.Cos(Yaw) * Math.Cos(Pitch)), (float)(Math.Sin(Yaw) * Math.Cos(Pitch)), (float)Math.Sin(Pitch));

    protected Vector3 GetRightVector() => new((float)Math.Cos(Yaw - MathX.PiOver2), (float)Math.Sin(Yaw - MathX.PiOver2), 0f);

    public void SetViewport(int x, int y, int width, int height) {
        // store window size and aspect ratio
        AspectRatio = width / (float)height;
        WindowSize = new Vector2<int>(width, height);
        // calculate projection matrix
        ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(FOV, AspectRatio, 1.0f, 40000.0f);
        RecalculateMatrices();
        // setup viewport
        GfxViewport(x, y, width, height);
        Picker?.Resize(width, height);
    }

    public abstract void GfxViewport(int x, int y, int width = 0, int height = 0);

    public void CopyFrom(Camera fromOther) {
        AspectRatio = fromOther.AspectRatio;
        WindowSize = fromOther.WindowSize;
        Location = fromOther.Location;
        Pitch = fromOther.Pitch;
        Yaw = fromOther.Yaw;
        ProjectionMatrix = fromOther.ProjectionMatrix;
        CameraViewMatrix = fromOther.CameraViewMatrix;
        ViewProjectionMatrix = fromOther.ViewProjectionMatrix;
        ViewFrustum.Update(ViewProjectionMatrix);
    }

    public void SetLocation(Vector3 location) {
        Location = location;
        RecalculateMatrices();
    }

    public void SetLocationPitchYaw(Vector3 location, float pitch, float yaw) {
        Location = location;
        Pitch = pitch;
        Yaw = yaw;
        RecalculateMatrices();
    }

    public void LookAt(Vector3 target) {
        var dir = Vector3.Normalize(target - Location);
        Yaw = (float)Math.Atan2(dir.Y, dir.X);
        Pitch = (float)Math.Asin(dir.Z);
        ClampRotation();
        RecalculateMatrices();
    }

    public void SetFromTransformMatrix(Matrix4x4 matrix) {
        Location = matrix.Translation;
        // extract view direction from view matrix and use it to calculate pitch and yaw
        var dir = new Vector3(matrix.M11, matrix.M12, matrix.M13);
        Yaw = (float)Math.Atan2(dir.Y, dir.X);
        Pitch = (float)Math.Asin(dir.Z);
        RecalculateMatrices();
    }

    public void SetScale(float scale) {
        Scale = scale;
        RecalculateMatrices();
    }

    public virtual void Tick(int deltaTime) { }

    // Prevent camera from going upside-down
    protected void ClampRotation() {
        if (Pitch >= MathX.PiOver2) Pitch = MathX.PiOver2 - 0.001f;
        else if (Pitch <= -MathX.PiOver2) Pitch = -MathX.PiOver2 + 0.001f;
    }
}

#endregion

#region Render

/// <summary>
/// EginRenderer
/// </summary>
public abstract class EginRenderer : Renderer {
    //public virtual AABB BoundingBox { get; }
    public virtual (int, int)? GetViewport((int, int) size) => default;
    public abstract void Render(Camera camera, Pass pass);
}

#endregion

#region Scene

/// <summary>
/// Octree
/// </summary>
public class Octree<T> where T : class {
    const int MaximumElementsBeforeSubdivide = 4;
    const float MinimumNodeSize = 64.0f;
    public Node Root;

    public struct Element {
        public T ClientObject;
        public AABB BoundingBox;
    }

    public class Node(Node parent, Vector3 regionMin, Vector3 regionSize) {
        public Node Parent = parent;
        public AABB Region = new(regionMin, regionMin + regionSize);
        public List<Element> Elements;
        public Node[] Children;

        public void Subdivide() {
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
            foreach (var element in Elements) {
                var movedDown = false;
                foreach (var child in Children)
                    if (child.Region.Contains(element.BoundingBox)) {
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

        public void Insert(Element element) {
            if (!HasChildren && HasElements && Region.Size.X > MinimumNodeSize && Elements.Count >= MaximumElementsBeforeSubdivide) Subdivide();
            var inserted = false;
            if (HasChildren) {
                var elementBB = element.BoundingBox;
                foreach (var child in Children)
                    if (child.Region.Contains(elementBB)) {
                        inserted = true;
                        child.Insert(element);
                        break;
                    }
            }
            if (!inserted) {
                Elements ??= [];
                Elements.Add(element);
            }
        }

        public (Node Node, int Index) Find(T clientObject, AABB bounds) {
            if (HasElements)
                for (var i = 0; i < Elements.Count; i++)
                    if (Elements[i].ClientObject == clientObject) return (this, i);
            if (HasChildren)
                foreach (var child in Children)
                    if (child.Region.Contains(bounds)) return child.Find(clientObject, bounds);
            return (null, -1);
        }

        public void Clear() {
            Elements = null;
            Children = null;
        }

        public void Query(object source, List<T> results) {
            switch (source) {
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

    public Octree(float size) {
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        Root = new Node(null, new Vector3(-size * 0.5f), new Vector3(size));
    }

    public void Insert(T obj, AABB bounds) {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        Root.Insert(new Element { ClientObject = obj, BoundingBox = bounds });
    }

    public void Remove(T obj, AABB bounds) {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        var (node, index) = Root.Find(obj, bounds);
        node?.Elements.RemoveAt(index);
    }

    public void Update(T obj, AABB oldBounds, AABB newBounds) {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        var (node, index) = Root.Find(obj, oldBounds);
        if (node != null) {
            // Locate the closest ancestor that the new bounds fit inside
            var ancestor = node;
            while (ancestor.Parent != null && !ancestor.Region.Contains(newBounds)) ancestor = ancestor.Parent;

            // Still fits in same node?
            if (ancestor == node) {
                // Still check for pushdown
                if (node.HasChildren)
                    foreach (var child in node.Children)
                        if (child.Region.Contains(newBounds)) {
                            node.Elements.RemoveAt(index);
                            child.Insert(new Element { ClientObject = obj, BoundingBox = newBounds });
                            return;
                        }

                // Not pushed down into any children
                node.Elements[index] = new Element { ClientObject = obj, BoundingBox = newBounds };
            }
            else {
                node.Elements.RemoveAt(index);
                ancestor.Insert(new Element { ClientObject = obj, BoundingBox = newBounds });
            }
        }
    }

    public void Clear() => Root.Clear();

    public List<T> Query(object source) {
        var results = new List<T>();
        Root.Query(source, results);
        return results;
    }
}

/// <summary>
/// Scene
/// </summary>
public class Scene(IOpenGfxModel gfx, Action<List<MeshBatchRequest>, Scene.RenderContext> meshBatchRenderer, float sizeHint = 32768) {
    public readonly Camera MainCamera;
    public readonly Vector3? LightPosition;
    public readonly IOpenGfxModel Gfx = gfx ?? throw new ArgumentNullException(nameof(gfx));
    public readonly Octree<SceneNode> StaticOctree = new(sizeHint);
    public readonly Octree<SceneNode> DynamicOctree = new(sizeHint);
    public bool ShowDebug;
    public IEnumerable<SceneNode> AllNodes => StaticNodes.Concat(DynamicNodes);
    readonly List<SceneNode> StaticNodes = [];
    readonly List<SceneNode> DynamicNodes = [];
    readonly Action<List<MeshBatchRequest>, RenderContext> MeshBatchRenderer = meshBatchRenderer ?? throw new ArgumentNullException(nameof(meshBatchRenderer));

    public class UpdateContext(float timestep) {
        public float Timestep = timestep;
    }

    public class RenderContext {
        public Camera Camera;
        public Vector3? LightPosition;
        public Renderer.Pass RenderPass;
        public Shader ReplacementShader;
        public bool ShowDebug;
    }

    public void Add(SceneNode node, bool dynamic) {
        if (dynamic) {
            DynamicNodes.Add(node);
            DynamicOctree.Insert(node, node.BoundingBox);
            node.Id = DynamicNodes.Count * 2 - 1;
        }
        else {
            StaticNodes.Add(node);
            StaticOctree.Insert(node, node.BoundingBox);
            node.Id = StaticNodes.Count * 2;
        }
    }

    public SceneNode Find(int id) {
        if (id == 0) return null;
        else if (id % 2 == 1) {
            var index = (id + 1) / 2 - 1;
            return index >= DynamicNodes.Count ? null : DynamicNodes[index];
        }
        else {
            var index = id / 2 - 1;
            return index >= StaticNodes.Count ? null : StaticNodes[index];
        }
    }

    public void Update(float timestep) {
        var updateContext = new UpdateContext(timestep);
        foreach (var node in StaticNodes) node.Update(updateContext);
        foreach (var node in DynamicNodes) { var oldBox = node.BoundingBox; node.Update(updateContext); DynamicOctree.Update(node, oldBox, node.BoundingBox); }
    }

    public void RenderWithCamera(Camera camera, Frustum cullFrustum = null) {
        var allNodes = StaticOctree.Query(cullFrustum ?? camera.ViewFrustum);
        allNodes.AddRange(DynamicOctree.Query(cullFrustum ?? camera.ViewFrustum));

        // Collect mesh calls
        var opaqueDrawCalls = new List<MeshBatchRequest>();
        var blendedDrawCalls = new List<MeshBatchRequest>();
        var looseNodes = new List<SceneNode>();
        foreach (var node in allNodes)
            if (node is IMeshCollection s)
                foreach (var mesh in s.RenderableMeshes) {
                    foreach (var call in mesh.DrawCallsOpaque)
                        opaqueDrawCalls.Add(new MeshBatchRequest {
                            Transform = node.Transform,
                            Mesh = mesh,
                            Call = call,
                            DistanceFromCamera = (node.BoundingBox.Center - camera.Location).LengthSquared(),
                            NodeId = node.Id,
                            MeshId = mesh.MeshIndex,
                        });
                    foreach (var call in mesh.DrawCallsBlended)
                        blendedDrawCalls.Add(new MeshBatchRequest {
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
        looseNodes.Sort((a, b) => {
            var aLength = (a.BoundingBox.Center - camera.Location).LengthSquared();
            var bLength = (b.BoundingBox.Center - camera.Location).LengthSquared();
            return bLength.CompareTo(aLength);
        });

        // Opaque render pass
        var renderContext = new RenderContext {
            Camera = camera,
            LightPosition = LightPosition,
            RenderPass = Renderer.Pass.Opaque,
            ShowDebug = ShowDebug,
        };

        // Blended render pass, back to front for loose nodes
        if (camera.Picker != null)
            if (camera.Picker.IsActive) { camera.Picker.Render(); renderContext.ReplacementShader = camera.Picker.Shader; }
            else if (camera.Picker.Debug) renderContext.ReplacementShader = camera.Picker.DebugShader;
        MeshBatchRenderer(opaqueDrawCalls, renderContext);
        foreach (var node in looseNodes) node.Render(renderContext);
        if (camera.Picker != null && camera.Picker.IsActive) {
            camera.Picker.Finish();
            RenderWithCamera(camera, cullFrustum);
        }
    }

    public void SetEnabledLayers(HashSet<string> layers) {
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
public abstract class SceneNode(Scene scene) {
    Matrix4x4 _transform = Matrix4x4.Identity;
    AABB _localBoundingBox;
    public Matrix4x4 Transform {
        get => _transform;
        set { _transform = value; BoundingBox = _localBoundingBox.Transform(_transform); }
    }
    public string LayerName;
    public bool LayerEnabled = true;
    public AABB BoundingBox;
    public AABB LocalBoundingBox {
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

#endregion

#region Particle

/// <summary>
/// IParticleSystem
/// </summary>
public interface IParticleSystem {
    IDictionary<string, object> Data { get; }
    IEnumerable<IDictionary<string, object>> Renderers { get; }
    IEnumerable<IDictionary<string, object>> Operators { get; }
    IEnumerable<IDictionary<string, object>> Initializers { get; }
    IEnumerable<IDictionary<string, object>> Emitters { get; }
    IEnumerable<string> GetChildParticleNames(bool enabledOnly = false);
}

#endregion
