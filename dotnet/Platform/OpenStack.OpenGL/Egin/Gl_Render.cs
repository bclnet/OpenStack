﻿using OpenStack.Gfx.Egin;
using OpenStack.Gfx.Egin.Particles;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static OpenStack.Gfx.Renderer;

namespace OpenStack.Gfx.OpenGL;

#region Camera

/// <summary>
/// GLCamera
/// </summary>
public abstract class GLCamera : Camera {
    public bool MouseOverRenderArea;

    public enum EventType {
        MouseEnter,
        MouseLeave,
        MouseMove,
        MouseDown,
        MouseUp,
        MouseWheel,
        KeyPress,
        KeyRelease
    }

    public void Event(EventType type, object e, object arg) {
        switch (type) {
            case EventType.MouseEnter: MouseOverRenderArea = true; break;
            case EventType.MouseLeave: MouseOverRenderArea = false; break;
        }
    }

    public abstract void HandleInput(MouseState mouseState, KeyboardState keyboardState);

    public override void GfxViewport(int x, int y, int width = 0, int height = 0) => GL.Viewport(x, y, width != 0 ? width : WindowSize.X, height != 0 ? height : WindowSize.Y);
}

/// <summary>
/// GLDebugCamera
/// </summary>
public class GLDebugCamera : GLCamera {
    bool MouseDragging;
    Vector2 MouseDelta;
    Vector2 MousePreviousPosition;
    KeyboardState KeyboardState;
    MouseState MouseState;
    int ScrollWheelDelta;

    public override void Tick(int deltaTime) {
        if (!MouseOverRenderArea) return;

        // use the keyboard state to update position
        HandleInputTick(deltaTime);

        // full width of the screen is a 1 PI (180deg)
        Yaw -= (float)Math.PI * MouseDelta.X / WindowSize.X;
        Pitch -= (float)Math.PI / AspectRatio * MouseDelta.Y / WindowSize.Y;
        ClampRotation();
        RecalculateMatrices();
    }

    public override void HandleInput(MouseState mouseState, KeyboardState keyboardState) {
        ScrollWheelDelta += mouseState.ScrollWheelValue - MouseState.ScrollWheelValue;
        MouseState = mouseState;
        KeyboardState = keyboardState;
        if (!MouseOverRenderArea || mouseState.LeftButton == ButtonState.Released) {
            MouseDragging = false;
            MouseDelta = default;
            if (!MouseOverRenderArea) return;
        }

        // drag
        if (mouseState.LeftButton == ButtonState.Pressed) {
            if (!MouseDragging) { MouseDragging = true; MousePreviousPosition = new Vector2(mouseState.X, mouseState.Y); }
            var mouseNewCoords = new Vector2(mouseState.X, mouseState.Y);
            MouseDelta.X = mouseNewCoords.X - MousePreviousPosition.X;
            MouseDelta.Y = mouseNewCoords.Y - MousePreviousPosition.Y;
            MousePreviousPosition = mouseNewCoords;
        }
    }

    public void HandleInputTick(float deltaTime) {
        var speed = CAMERASPEED * deltaTime;

        // double speed if shift is pressed
        if (KeyboardState.IsKeyDown(Key.ShiftLeft)) speed *= 2;
        else if (KeyboardState.IsKeyDown(Key.F)) speed *= 10;

        if (KeyboardState.IsKeyDown(Key.W)) Location += GetForwardVector() * speed;
        if (KeyboardState.IsKeyDown(Key.S)) Location -= GetForwardVector() * speed;
        if (KeyboardState.IsKeyDown(Key.D)) Location += GetRightVector() * speed;
        if (KeyboardState.IsKeyDown(Key.A)) Location -= GetRightVector() * speed;
        if (KeyboardState.IsKeyDown(Key.Z)) Location += new Vector3(0, 0, -speed);
        if (KeyboardState.IsKeyDown(Key.Q)) Location += new Vector3(0, 0, speed);

        // scroll
        if (ScrollWheelDelta != 0) { Location += GetForwardVector() * ScrollWheelDelta * speed; ScrollWheelDelta = 0; }
    }
}

#endregion

#region Model

/// <summary>
/// GLMeshBuffers
/// </summary>
public class GLMeshBuffers {
    public Buffer[] VertexBuffers;
    public Buffer[] IndexBuffers;

    public struct Buffer {
        public uint Handle;
        public long Size;
    }

    public GLMeshBuffers(IVBIB vbib) {
        VertexBuffers = new Buffer[vbib.VertexBuffers.Count];
        IndexBuffers = new Buffer[vbib.IndexBuffers.Count];
        for (var i = 0; i < vbib.VertexBuffers.Count; i++) {
            VertexBuffers[i].Handle = (uint)GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBuffers[i].Handle);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vbib.VertexBuffers[i].ElementCount * vbib.VertexBuffers[i].ElementSizeInBytes), vbib.VertexBuffers[i].Data, BufferUsageHint.StaticDraw);
            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out VertexBuffers[i].Size);
        }
        for (var i = 0; i < vbib.IndexBuffers.Count; i++) {
            IndexBuffers[i].Handle = (uint)GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, IndexBuffers[i].Handle);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(vbib.IndexBuffers[i].ElementCount * vbib.IndexBuffers[i].ElementSizeInBytes), vbib.IndexBuffers[i].Data, BufferUsageHint.StaticDraw);
            GL.GetBufferParameter(BufferTarget.ElementArrayBuffer, BufferParameterName.BufferSize, out IndexBuffers[i].Size);
        }
    }
}

/// <summary>
/// GLMeshBufferCache
/// </summary>
public class GLMeshBufferCache {
    Dictionary<IVBIB, GLMeshBuffers> _gpuBuffers = [];
    Dictionary<VAOKey, uint> _vertexArrayObjects = [];

    struct VAOKey {
        public GLMeshBuffers VBIB;
        public Shader Shader;
        public uint VertexIndex;
        public uint IndexIndex;
        public uint BaseVertex;
    }

    public GLMeshBuffers GetVertexIndexBuffers(IVBIB vbib) {
        // cache
        if (_gpuBuffers.TryGetValue(vbib, out var z)) return z;
        // build
        var newGpuVbib = new GLMeshBuffers(vbib);
        _gpuBuffers.Add(vbib, newGpuVbib);
        return newGpuVbib;
    }

    public uint GetVertexArrayObject(IVBIB vbib, Shader shader, uint vtxIndex, uint idxIndex, uint baseVertex) {
        var gpuVbib = GetVertexIndexBuffers(vbib);
        var vaoKey = new VAOKey {
            VBIB = gpuVbib,
            Shader = shader,
            VertexIndex = vtxIndex,
            IndexIndex = idxIndex,
            BaseVertex = baseVertex,
        };

        // cache
        if (_vertexArrayObjects.TryGetValue(vaoKey, out var z)) return z;
        // build
        GL.GenVertexArrays(1, out uint newVaoHandle);
        GL.BindVertexArray(newVaoHandle);
        GL.BindBuffer(BufferTarget.ArrayBuffer, gpuVbib.VertexBuffers[vtxIndex].Handle);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, gpuVbib.IndexBuffers[idxIndex].Handle);
        var curVertexBuffer = vbib.VertexBuffers[(int)vtxIndex];
        var texCoordNum = 0;
        var colorNum = 0;
        foreach (var attribute in curVertexBuffer.Attributes) {
            var attributeName = $"v{attribute.SemanticName}";
            if (attribute.SemanticName == "TEXCOORD" && texCoordNum++ > 0) attributeName += texCoordNum;
            else if (attribute.SemanticName == "COLOR" && colorNum++ > 0) attributeName += colorNum;
            BindVertexAttrib(attribute, attributeName, shader.Program, (int)curVertexBuffer.ElementSizeInBytes, baseVertex);
        }
        GL.BindVertexArray(0);
        _vertexArrayObjects.Add(vaoKey, newVaoHandle);
        return newVaoHandle;
    }

    static void BindVertexAttrib(OnDiskBufferData.Attribute attribute, string attributeName, int shaderProgram, int stride, uint baseVertex) {
        var attributeLocation = GL.GetAttribLocation(shaderProgram, attributeName);
        if (attributeLocation == -1) return; // Ignore this attribute if it is not found in the shader
        GL.EnableVertexAttribArray(attributeLocation);
        switch (attribute.Format) {
            case DXGI_FORMAT.R32G32B32_FLOAT: GL.VertexAttribPointer(attributeLocation, 3, VertexAttribPointerType.Float, false, stride, (IntPtr)(baseVertex + attribute.Offset)); break;
            case DXGI_FORMAT.R8G8B8A8_UNORM: GL.VertexAttribPointer(attributeLocation, 4, VertexAttribPointerType.UnsignedByte, false, stride, (IntPtr)(baseVertex + attribute.Offset)); break;
            case DXGI_FORMAT.R32G32_FLOAT: GL.VertexAttribPointer(attributeLocation, 2, VertexAttribPointerType.Float, false, stride, (IntPtr)(baseVertex + attribute.Offset)); break;
            case DXGI_FORMAT.R16G16_FLOAT: GL.VertexAttribPointer(attributeLocation, 2, VertexAttribPointerType.HalfFloat, false, stride, (IntPtr)(baseVertex + attribute.Offset)); break;
            case DXGI_FORMAT.R32G32B32A32_FLOAT: GL.VertexAttribPointer(attributeLocation, 4, VertexAttribPointerType.Float, false, stride, (IntPtr)(baseVertex + attribute.Offset)); break;
            case DXGI_FORMAT.R8G8B8A8_UINT: GL.VertexAttribPointer(attributeLocation, 4, VertexAttribPointerType.UnsignedByte, false, stride, (IntPtr)(baseVertex + attribute.Offset)); break;
            case DXGI_FORMAT.R16G16_SINT: GL.VertexAttribIPointer(attributeLocation, 2, VertexAttribIntegerType.Short, stride, (IntPtr)(baseVertex + attribute.Offset)); break;
            case DXGI_FORMAT.R16G16B16A16_SINT: GL.VertexAttribIPointer(attributeLocation, 4, VertexAttribIntegerType.Short, stride, (IntPtr)(baseVertex + attribute.Offset)); break;
            case DXGI_FORMAT.R16G16_SNORM: GL.VertexAttribPointer(attributeLocation, 2, VertexAttribPointerType.Short, true, stride, (IntPtr)(baseVertex + attribute.Offset)); break;
            case DXGI_FORMAT.R16G16_UNORM: GL.VertexAttribPointer(attributeLocation, 2, VertexAttribPointerType.UnsignedShort, true, stride, (IntPtr)(baseVertex + attribute.Offset)); break;
            default: throw new FormatException($"Unknown attribute format {attribute.Format}");
        }
    }
}

/// <summary>
/// MeshBatchRenderer
/// </summary>
public static class MeshBatchRenderer {
    public static void Render(List<MeshBatchRequest> requests, Scene.RenderContext context) {
        // opaque: grouped by material
        if (context.Pass == Pass.Both || context.Pass == Pass.Opaque) DrawBatch(requests, context);
        // blended: in reverse order
        if (context.Pass == Pass.Both || context.Pass == Pass.Translucent) {
            var holder = new MeshBatchRequest[1]; // holds the one request we render at a time
            requests.Sort((a, b) => -a.DistanceFromCamera.CompareTo(b.DistanceFromCamera));
            foreach (var request in requests) { holder[0] = request; DrawBatch(holder, context); }
        }
    }

    static void DrawBatch(IEnumerable<MeshBatchRequest> drawCalls, Scene.RenderContext context) {
        GL.Enable(EnableCap.DepthTest);
        var viewProjectionMatrix = context.Camera.ViewProjectionMatrix.ToOpenTK();
        var cameraPosition = context.Camera.Location.ToOpenTK();
        var lightPosition = cameraPosition; // (context.LightPosition ?? context.Camera.Location).ToOpenTK();

        // groups
        var groupedDrawCalls = context.ReplacementShader == null
           ? drawCalls.GroupBy(a => a.Call.Shader)
           : drawCalls.GroupBy(a => context.ReplacementShader);
        foreach (var shaderGroup in groupedDrawCalls) {
            var shader = shaderGroup.Key;
            var uniformLocationAnimated = shader.GetUniformLocation("bAnimated");
            var uniformLocationAnimationTexture = shader.GetUniformLocation("animationTexture");
            var uniformLocationNumBones = shader.GetUniformLocation("fNumBones");
            var uniformLocationTransform = shader.GetUniformLocation("transform");
            var uniformLocationTint = shader.GetUniformLocation("m_vTintColorSceneObject");
            var uniformLocationTintDrawCall = shader.GetUniformLocation("m_vTintColorDrawCall");
            var uniformLocationTime = shader.GetUniformLocation("g_flTime");
            var uniformLocationObjectId = shader.GetUniformLocation("sceneObjectId");
            var uniformLocationMeshId = shader.GetUniformLocation("meshId");
            GL.UseProgram(shader.Program);
            GL.Uniform3(shader.GetUniformLocation("vLightPosition"), cameraPosition);
            GL.Uniform3(shader.GetUniformLocation("vEyePosition"), cameraPosition);
            GL.UniformMatrix4(shader.GetUniformLocation("uProjectionViewMatrix"), false, ref viewProjectionMatrix);

            // materials
            foreach (var materialGroup in shaderGroup.GroupBy(a => a.Call.Material)) {
                var material = materialGroup.Key;
                if (!context.ShowDebug && material.IsToolsMaterial) continue;
                material.Render(shader);
                foreach (var request in materialGroup) {
                    var transform = request.Transform.ToOpenTK();
                    GL.UniformMatrix4(uniformLocationTransform, false, ref transform);
                    if (uniformLocationObjectId != 1) GL.Uniform1(uniformLocationObjectId, request.NodeId);
                    if (uniformLocationMeshId != 1) GL.Uniform1(uniformLocationMeshId, request.MeshId);
                    if (uniformLocationTime != 1) GL.Uniform1(uniformLocationTime, request.Mesh.Time);
                    if (uniformLocationAnimated != -1) GL.Uniform1(uniformLocationAnimated, request.Mesh.AnimationTexture.HasValue ? 1.0f : 0.0f);

                    // push animation texture to the shader (if it supports it)
                    if (request.Mesh.AnimationTexture.HasValue) {
                        if (uniformLocationAnimationTexture != -1) { GL.ActiveTexture(TextureUnit.Texture0); GL.BindTexture(TextureTarget.Texture2D, request.Mesh.AnimationTexture.Value); GL.Uniform1(uniformLocationAnimationTexture, 0); }
                        if (uniformLocationNumBones != -1) { var v = (float)Math.Max(1, request.Mesh.AnimationTextureSize - 1); GL.Uniform1(uniformLocationNumBones, v); }
                    }

                    // draw
                    if (uniformLocationTint > -1) { var tint = request.Mesh.Tint.ToOpenTK(); GL.Uniform4(uniformLocationTint, tint); }
                    if (uniformLocationTintDrawCall > -1) GL.Uniform3(uniformLocationTintDrawCall, request.Call.TintColor.ToOpenTK());
                    GL.BindVertexArray(request.Call.VertexArrayObject);
                    GL.DrawElements((PrimitiveType)request.Call.PrimitiveType, request.Call.IndexCount, (DrawElementsType)request.Call.IndexType, (IntPtr)request.Call.StartIndex);
                }
                material.PostRender();
            }
        }
        GL.Disable(EnableCap.DepthTest);
    }
}

/// <summary>
/// QuadIndexBuffer
/// </summary>
public class QuadIndexBuffer {
    public int GLHandle;

    public QuadIndexBuffer(int size) {
        GLHandle = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, GLHandle);
        var indices = new ushort[size];
        for (var i = 0; i < size / 6; ++i) {
            indices[(i * 6) + 0] = (ushort)((i * 4) + 0);
            indices[(i * 6) + 1] = (ushort)((i * 4) + 1);
            indices[(i * 6) + 2] = (ushort)((i * 4) + 2);
            indices[(i * 6) + 3] = (ushort)((i * 4) + 0);
            indices[(i * 6) + 4] = (ushort)((i * 4) + 2);
            indices[(i * 6) + 5] = (ushort)((i * 4) + 3);
        }
        GL.BufferData(BufferTarget.ElementArrayBuffer, size * sizeof(ushort), indices, BufferUsageHint.StaticDraw);
    }
}

/// <summary>
/// GLPickingTexture
/// </summary>
public class GLPickingTexture : IDisposable, IPickingTexture {
    public struct PixelInfo {
        public uint ObjectId;
        public uint MeshId;
        public uint Unused2;
    }

    public enum PickingIntent {
        Select,
        Open
    }

    public class PickingRequest {
        public bool ActiveNextFrame;
        public int CursorPositionX;
        public int CursorPositionY;
        public PickingIntent Intent;
        public void NextFrame(int x, int y, PickingIntent intent) {
            ActiveNextFrame = true;
            CursorPositionX = x;
            CursorPositionY = y;
            Intent = intent;
        }
    }

    public struct PickingResponse {
        public PickingIntent Intent;
        public PixelInfo PixelInfo;
    }

    public event EventHandler<PickingResponse> OnPicked;
    public readonly PickingRequest Request = new();
    public Shader Shader { get; }
    public Shader DebugShader { get; }
    public bool IsActive => Request.ActiveNextFrame;
    public bool Debug { get; }
    int width = 4;
    int height = 4;
    int fboHandle;
    int colorHandle;
    int depthHandle;

    public GLPickingTexture(OpenGLGfxModel gfx, EventHandler<PickingResponse> onPicked) {
        (Shader, _) = gfx.ShaderManager.CreateShader("vrf.picking", new Dictionary<string, bool>());
        (DebugShader, _) = gfx.ShaderManager.CreateShader("vrf.picking", new Dictionary<string, bool>() { { "F_DEBUG_PICKER", true } });
        OnPicked += onPicked;
        Setup();
    }

    public void Dispose() {
        OnPicked = null;
        GL.DeleteTexture(colorHandle);
        GL.DeleteTexture(depthHandle);
        GL.DeleteFramebuffer(fboHandle);
    }

    public void Setup() {
        fboHandle = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboHandle);

        // color
        colorHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, colorHandle);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32ui, width, height, 0, PixelFormat.RgbaInteger, PixelType.UnsignedInt, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, colorHandle, 0);

        // depth
        depthHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, depthHandle);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, width, height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthHandle, 0);

        // bind
        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete) throw new InvalidOperationException($"Framebuffer failed to bind with error: {status}");
        GL.BindTexture(TextureTarget.Texture2D, 0);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Render() {
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, fboHandle);
        GL.ClearColor(0, 0, 0, 0);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public void Finish() {
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        if (Request.ActiveNextFrame) {
            Request.ActiveNextFrame = false;
            var pixelInfo = ReadPixelInfo(Request.CursorPositionX, Request.CursorPositionY);
            OnPicked?.Invoke(this, new PickingResponse {
                Intent = Request.Intent,
                PixelInfo = pixelInfo,
            });
        }
    }

    public void Resize(int width, int height) {
        this.width = width;
        this.height = height;
        GL.BindTexture(TextureTarget.Texture2D, colorHandle);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32ui, width, height, 0, PixelFormat.RgbaInteger, PixelType.UnsignedInt, IntPtr.Zero);
        GL.BindTexture(TextureTarget.Texture2D, depthHandle);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, width, height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
    }

    public PixelInfo ReadPixelInfo(int width, int height) {
        GL.Flush();
        GL.Finish();
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fboHandle);
        GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
        var pixelInfo = new PixelInfo();
        GL.ReadPixels(width, this.height - height, 1, 1, PixelFormat.RgbaInteger, PixelType.UnsignedInt, ref pixelInfo);
        GL.ReadBuffer(ReadBufferMode.None);
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
        return pixelInfo;
    }
}

/// <summary>
/// GLRenderMaterial
/// </summary>
public class GLRenderMaterial(MaterialShaderProp material) : RenderMaterial(material) {
    public override void Render(Shader shader) {
        // start at 1, texture unit 0 is reserved for the animation texture
        var textureUnit = 1;
        int location;
        foreach (var texture in Textures) {
            location = shader.GetUniformLocation(texture.Key);
            if (location > -1) {
                GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
                GL.BindTexture(TextureTarget.Texture2D, texture.Value);
                GL.Uniform1(location, textureUnit);
                textureUnit++;
            }
        }
        switch (Material) {
            case MaterialShaderVProp p:
                foreach (var param in p.IntParams) {
                    location = shader.GetUniformLocation(param.Key);
                    if (location > -1) GL.Uniform1(location, param.Value);
                }
                foreach (var param in p.FloatParams) {
                    location = shader.GetUniformLocation(param.Key);
                    if (location > -1) GL.Uniform1(location, param.Value);
                }
                foreach (var param in p.VectorParams) {
                    location = shader.GetUniformLocation(param.Key);
                    if (location > -1) GL.Uniform4(location, param.Value.X, param.Value.Y, param.Value.Z, param.Value.W);
                }
                break;
        }
        var alphaReference = shader.GetUniformLocation("g_flAlphaTestReference");
        if (alphaReference > -1) GL.Uniform1(alphaReference, AlphaTestReference);
        if (IsBlended) {
            GL.DepthMask(false);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, IsAdditiveBlend ? BlendingFactor.One : BlendingFactor.OneMinusSrcAlpha);
        }
        if (IsRenderBackfaces) GL.Disable(EnableCap.CullFace);
    }

    public override void PostRender() {
        if (IsBlended) { GL.DepthMask(true); GL.Disable(EnableCap.Blend); }
        if (IsRenderBackfaces) GL.Enable(EnableCap.CullFace);
    }
}

/// <summary>
/// GLRenderableMesh
/// </summary>
public class GLRenderableMesh(OpenGLGfxModel gfx, IMesh mesh, int meshIndex, IDictionary<string, string> skinMaterials = null, IEginModel model = null) : RenderableMesh(t => ((GLRenderableMesh)t).Gfx = gfx, mesh, meshIndex, skinMaterials, model) {
    OpenGLGfxModel Gfx;

    public override void SetRenderMode(string renderMode) {
        foreach (var call in DrawCallsOpaque.Union(DrawCallsBlended)) {
            // recycle old shader parameters that are not render modes since we are scrapping those anyway
            var parameters = call.Shader.Parameters.Where(kvp => !kvp.Key.StartsWith("renderMode")).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            if (renderMode != null && call.Shader.RenderModes.Contains(renderMode)) parameters.Add($"renderMode_{renderMode}", true);
            (call.Shader, _) = Gfx.ShaderManager.CreateShader(call.Shader.Name, parameters);
            call.VertexArrayObject = Gfx.MeshBufferCache.GetVertexArrayObject(Mesh.VBIB, call.Shader, call.VertexBuffer.Id, call.IndexBuffer.Id, call.BaseVertex);
        }
    }

    protected override void ConfigureDrawCalls(IDictionary<string, string> skinMaterials, bool firstSetup) {
        var data = Mesh.Data;
        if (firstSetup) Gfx.MeshBufferCache.GetVertexIndexBuffers(VBIB); // this call has side effects because it uploads to gpu

        // prepare drawcalls
        var i = 0;
        foreach (var sceneObject in data.GetArray("m_sceneObjects"))
            foreach (var objectDrawCall in sceneObject.GetArray("m_drawCalls")) {
                var materialName = objectDrawCall.Get<string>("m_material") ?? objectDrawCall.Get<string>("m_pMaterial");
                if (skinMaterials != null && skinMaterials.ContainsKey(materialName)) materialName = skinMaterials[materialName];
                var (material, _) = Gfx.MaterialManager.CreateMaterial($"{materialName}_c");
                var isOverlay = material.Material is MaterialShaderVProp z && z.IntParams.ContainsKey("F_OVERLAY");
                if (isOverlay) continue; // ignore overlays for now
                var shaderArgs = new Dictionary<string, bool>();
                if (DrawCall.IsCompressedNormalTangent(objectDrawCall)) shaderArgs.Add("fulltangent", false);
                // first
                if (firstSetup) {
                    var drawCall = CreateDrawCall(objectDrawCall, shaderArgs, material);
                    DrawCallsAll.Add(drawCall);
                    if (drawCall.Material.IsBlended) DrawCallsBlended.Add(drawCall);
                    else DrawCallsOpaque.Add(drawCall);
                    continue;
                }
                // next
                SetupDrawCallMaterial(DrawCallsAll[i++], shaderArgs, material);
            }
    }

    DrawCall CreateDrawCall(IDictionary<string, object> objectDrawCall, IDictionary<string, bool> shaderArgs, GLRenderMaterial material) {
        var drawCall = new DrawCall();
        var primitiveType = objectDrawCall.Get<object>("m_nPrimitiveType");
        switch (primitiveType) {
            case byte primitiveTypeByte:
                if ((RenderPrimitiveType)primitiveTypeByte == RenderPrimitiveType.RENDER_PRIM_TRIANGLES) drawCall.PrimitiveType = (int)PrimitiveType.Triangles;
                break;
            case string primitiveTypeString:
                if (primitiveTypeString == "RENDER_PRIM_TRIANGLES") drawCall.PrimitiveType = (int)PrimitiveType.Triangles;
                break;
        }
        if (drawCall.PrimitiveType != (int)PrimitiveType.Triangles) throw new NotImplementedException($"Unknown PrimitiveType in drawCall! {primitiveType})");
        // material
        SetupDrawCallMaterial(drawCall, shaderArgs, material);
        // index-buffer
        var indexBufferObject = objectDrawCall.GetSub("m_indexBuffer");
        drawCall.IndexBuffer = (indexBufferObject.GetUInt32("m_hBuffer"), indexBufferObject.GetUInt32("m_nBindOffsetBytes"));
        // vertex
        var vertexElementSize = VBIB.VertexBuffers[(int)drawCall.VertexBuffer.Id].ElementSizeInBytes;
        drawCall.BaseVertex = objectDrawCall.GetUInt32("m_nBaseVertex") * vertexElementSize;
        //drawCall.VertexCount = objectDrawCall.GetUInt32("m_nVertexCount");
        // index
        var indexElementSize = VBIB.IndexBuffers[(int)drawCall.IndexBuffer.Id].ElementSizeInBytes;
        drawCall.StartIndex = objectDrawCall.GetUInt32("m_nStartIndex") * indexElementSize;
        drawCall.IndexCount = objectDrawCall.GetInt32("m_nIndexCount");
        // tint
        if (objectDrawCall.ContainsKey("m_vTintColor")) drawCall.TintColor = objectDrawCall.GetVector3("m_vTintColor");
        // index-type
        if (indexElementSize == 2) drawCall.IndexType = (int)DrawElementsType.UnsignedShort; // shopkeeper_vr
        else if (indexElementSize == 4) drawCall.IndexType = (int)DrawElementsType.UnsignedInt; // glados
        else throw new ArgumentOutOfRangeException(nameof(indexElementSize), $"Unsupported index type {indexElementSize}");
        // vbo
        var vertexBuffer = objectDrawCall.GetArray("m_vertexBuffers")[0];
        drawCall.VertexBuffer = (vertexBuffer.GetUInt32("m_hBuffer"), vertexBuffer.GetUInt32("m_nBindOffsetBytes"));
        drawCall.VertexArrayObject = Gfx.MeshBufferCache.GetVertexArrayObject(VBIB, drawCall.Shader, drawCall.VertexBuffer.Id, drawCall.IndexBuffer.Id, drawCall.BaseVertex);
        return drawCall;
    }

    void SetupDrawCallMaterial(DrawCall drawCall, IDictionary<string, bool> shaderArgs, RenderMaterial material) {
        drawCall.Material = material;
        // add shader parameters from material to the shader parameters from the draw call
        var combinedShaderArgs = shaderArgs.Concat(material.Material.ShaderArgs).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        // load shader
        (drawCall.Shader, _) = Gfx.ShaderManager.CreateShader(drawCall.Material.Material.ShaderName, combinedShaderArgs);
        // bind and validate shader
        GL.UseProgram(drawCall.Shader.Program);
        // tint and normal
        if (!drawCall.Material.Textures.ContainsKey("g_tTintMask")) drawCall.Material.Textures.Add("g_tTintMask", Gfx.TextureManager.CreateSolidTexture(1, 1, 1f, 1f, 1f, 1f));
        if (!drawCall.Material.Textures.ContainsKey("g_tNormal")) drawCall.Material.Textures.Add("g_tNormal", Gfx.TextureManager.CreateSolidTexture(1, 1, 0.5f, 1f, 0.5f, 1f));
    }
}

#endregion

#region Scene

/// <summary>
/// OctreeDebugRenderer
/// </summary>
public class OctreeDebugRenderer<T> where T : class {
    const int STRIDE = sizeof(float) * 7;

    readonly Shader Shader;
    readonly object ShaderTag;
    readonly Octree<T> Octree;
    readonly int VaoHandle;
    readonly int VboHandle;
    readonly bool Dynamic;
    int VertexCount;

    public OctreeDebugRenderer(Octree<T> octree, OpenGLGfxModel graphic, bool dynamic) {
        Octree = octree;
        Dynamic = dynamic;
        (Shader, ShaderTag) = graphic.ShaderManager.CreateShader("vrf.grid");
        GL.UseProgram(Shader.Program);
        VboHandle = GL.GenBuffer();
        if (!dynamic) Rebuild();
        VaoHandle = GL.GenVertexArray();
        GL.BindVertexArray(VaoHandle);
        GL.BindBuffer(BufferTarget.ArrayBuffer, VboHandle);
        var location = Shader.GetAttribLocation("aVertexPosition");
        GL.EnableVertexAttribArray(location);
        GL.VertexAttribPointer(location, 3, VertexAttribPointerType.Float, false, STRIDE, 0);
        location = Shader.GetAttribLocation("aVertexColor");
        GL.EnableVertexAttribArray(location);
        GL.VertexAttribPointer(location, 4, VertexAttribPointerType.Float, false, STRIDE, sizeof(float) * 3);
        GL.BindVertexArray(0);
    }

    static void AddLine(List<float> vertices, Vector3 from, Vector3 to, float r, float g, float b, float a) {
        vertices.Add(from.X); vertices.Add(from.Y); vertices.Add(from.Z);
        vertices.Add(r); vertices.Add(g); vertices.Add(b); vertices.Add(a);
        vertices.Add(to.X); vertices.Add(to.Y); vertices.Add(to.Z);
        vertices.Add(r); vertices.Add(g); vertices.Add(b); vertices.Add(a);
    }

    static void AddBox(List<float> vertices, AABB box, float r, float g, float b, float a) {
        AddLine(vertices, new Vector3(box.Min.X, box.Min.Y, box.Min.Z), new Vector3(box.Max.X, box.Min.Y, box.Min.Z), r, g, b, a);
        AddLine(vertices, new Vector3(box.Max.X, box.Min.Y, box.Min.Z), new Vector3(box.Max.X, box.Max.Y, box.Min.Z), r, g, b, a);
        AddLine(vertices, new Vector3(box.Max.X, box.Max.Y, box.Min.Z), new Vector3(box.Min.X, box.Max.Y, box.Min.Z), r, g, b, a);
        AddLine(vertices, new Vector3(box.Min.X, box.Max.Y, box.Min.Z), new Vector3(box.Min.X, box.Min.Y, box.Min.Z), r, g, b, a);
        //
        AddLine(vertices, new Vector3(box.Min.X, box.Min.Y, box.Max.Z), new Vector3(box.Max.X, box.Min.Y, box.Max.Z), r, g, b, a);
        AddLine(vertices, new Vector3(box.Max.X, box.Min.Y, box.Max.Z), new Vector3(box.Max.X, box.Max.Y, box.Max.Z), r, g, b, a);
        AddLine(vertices, new Vector3(box.Max.X, box.Max.Y, box.Max.Z), new Vector3(box.Min.X, box.Max.Y, box.Max.Z), r, g, b, a);
        AddLine(vertices, new Vector3(box.Min.X, box.Max.Y, box.Max.Z), new Vector3(box.Min.X, box.Min.Y, box.Max.Z), r, g, b, a);
        //
        AddLine(vertices, new Vector3(box.Min.X, box.Min.Y, box.Min.Z), new Vector3(box.Min.X, box.Min.Y, box.Max.Z), r, g, b, a);
        AddLine(vertices, new Vector3(box.Max.X, box.Min.Y, box.Min.Z), new Vector3(box.Max.X, box.Min.Y, box.Max.Z), r, g, b, a);
        AddLine(vertices, new Vector3(box.Max.X, box.Max.Y, box.Min.Z), new Vector3(box.Max.X, box.Max.Y, box.Max.Z), r, g, b, a);
        AddLine(vertices, new Vector3(box.Min.X, box.Max.Y, box.Min.Z), new Vector3(box.Min.X, box.Max.Y, box.Max.Z), r, g, b, a);
    }

    void AddOctreeNode(List<float> vertices, Octree<T>.Node node, int depth) {
        AddBox(vertices, node.Region, 1.0f, 1.0f, 1.0f, node.HasElements ? 1.0f : 0.1f);
        if (node.HasElements)
            foreach (var element in node.Elements) {
                var shading = Math.Min(1.0f, depth * 0.1f);
                AddBox(vertices, element.BoundingBox, 1.0f, shading, 0.0f, 1.0f);
                // AddLine(vertices, element.BoundingBox.Min, node.Region.Min, 1.0f, shading, 0.0f, 0.5f);
                // AddLine(vertices, element.BoundingBox.Max, node.Region.Max, 1.0f, shading, 0.0f, 0.5f);
            }
        if (node.HasChildren)
            foreach (var child in node.Children)
                AddOctreeNode(vertices, child, depth + 1);
    }

    void Rebuild() {
        var vertices = new List<float>();
        AddOctreeNode(vertices, Octree.Root, 0);
        VertexCount = vertices.Count / 7;
        GL.BindBuffer(BufferTarget.ArrayBuffer, VboHandle);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), Dynamic ? BufferUsageHint.DynamicDraw : BufferUsageHint.StaticDraw);
    }

    public void Render(Camera camera, Pass pass) {
        if (pass == Pass.Translucent || pass == Pass.Both) {
            if (Dynamic) Rebuild();
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthMask(false);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.UseProgram(Shader.Program);
            var projectionViewMatrix = camera.ViewProjectionMatrix.ToOpenTK();
            GL.UniformMatrix4(Shader.GetUniformLocation("uProjectionViewMatrix"), false, ref projectionViewMatrix);
            GL.BindVertexArray(VaoHandle);
            GL.DrawArrays(PrimitiveType.Lines, 0, VertexCount);
            GL.BindVertexArray(0);
            GL.UseProgram(0);
            GL.DepthMask(true);
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
        }
    }
}

/// <summary>
/// MeshSceneNode
/// </summary>
public class MeshSceneNode : SceneNode, IMeshCollection {
    GLRenderableMesh Mesh;

    public MeshSceneNode(Scene scene, IMesh mesh, int meshIndex, IDictionary<string, string> skinMaterials = null) : base(scene) {
        Mesh = new GLRenderableMesh(Scene.Gfx as OpenGLGfxModel, mesh, meshIndex, skinMaterials);
        LocalBoundingBox = Mesh.BoundingBox;
    }

    public Vector4 Tint {
        get => Mesh.Tint;
        set => Mesh.Tint = value;
    }

    public IEnumerable<RenderableMesh> RenderableMeshes {
        get { yield return Mesh; }
    }

    public override IEnumerable<string> GetSupportedRenderModes() => Mesh.GetSupportedRenderModes();
    public override void SetRenderMode(string renderMode) => Mesh.SetRenderMode(renderMode);
    public override void Update(Scene.UpdateContext context) => Mesh.Update(context.Timestep);
    public override void Render(Scene.RenderContext context) { } // This node does not render itself; it uses the batching system via IRenderableMeshCollection
}

#endregion

#region Particle

public static class ParticleControllerFactory {
    // Register particle emitters
    static readonly IDictionary<string, Func<IDictionary<string, object>, IDictionary<string, object>, IParticleEmitter>> EmitterDictionary
       = new Dictionary<string, Func<IDictionary<string, object>, IDictionary<string, object>, IParticleEmitter>> {
           ["C_OP_InstantaneousEmitter"] = (baseProperties, emitterInfo) => new InstantaneousEmitter(baseProperties, emitterInfo),
           ["C_OP_ContinuousEmitter"] = (baseProperties, emitterInfo) => new ContinuousEmitter(baseProperties, emitterInfo),
       };

    // Register particle initializers
    static readonly IDictionary<string, Func<IDictionary<string, object>, IParticleInitializer>> InitializerDictionary
       = new Dictionary<string, Func<IDictionary<string, object>, IParticleInitializer>> {
           ["C_INIT_CreateWithinSphere"] = initializerInfo => new CreateWithinSphere(initializerInfo),
           ["C_INIT_InitialVelocityNoise"] = initializerInfo => new InitialVelocityNoise(initializerInfo),
           ["C_INIT_OffsetVectorToVector"] = initializerInfo => new OffsetVectorToVector(initializerInfo),
           ["C_INIT_PositionOffset"] = initializerInfo => new PositionOffset(initializerInfo),
           ["C_INIT_RandomAlpha"] = initializerInfo => new RandomAlpha(initializerInfo),
           ["C_INIT_RandomColor"] = initializerInfo => new RandomColor(initializerInfo),
           ["C_INIT_RandomLifeTime"] = initializerInfo => new RandomLifeTime(initializerInfo),
           ["C_INIT_RandomRadius"] = initializerInfo => new RandomRadius(initializerInfo),
           ["C_INIT_RandomRotation"] = initializerInfo => new RandomRotation(initializerInfo),
           ["C_INIT_RandomRotationSpeed"] = initializerInfo => new RandomRotationSpeed(initializerInfo),
           ["C_INIT_RandomSequence"] = initializerInfo => new RandomSequence(initializerInfo),
           ["C_INIT_RandomTrailLength"] = initializerInfo => new RandomTrailLength(initializerInfo),
           ["C_INIT_RemapParticleCountToScalar"] = initializerInfo => new RemapParticleCountToScalar(initializerInfo),
           ["C_INIT_RingWave"] = initializerInfo => new RingWave(initializerInfo),
       };

    // Register particle operators
    static readonly IDictionary<string, Func<IDictionary<string, object>, IParticleOperator>> OperatorDictionary
       = new Dictionary<string, Func<IDictionary<string, object>, IParticleOperator>> {
           ["C_OP_Decay"] = operatorInfo => new Decay(operatorInfo),
           ["C_OP_BasicMovement"] = operatorInfo => new BasicMovement(operatorInfo),
           ["C_OP_ColorInterpolate"] = operatorInfo => new ColorInterpolate(operatorInfo),
           ["C_OP_InterpolateRadius"] = operatorInfo => new InterpolateRadius(operatorInfo),
           ["C_OP_FadeAndKill"] = operatorInfo => new FadeAndKill(operatorInfo),
           ["C_OP_FadeInSimple"] = operatorInfo => new FadeInSimple(operatorInfo),
           ["C_OP_FadeOutSimple"] = operatorInfo => new FadeOutSimple(operatorInfo),
           ["C_OP_OscillateScalar"] = operatorInfo => new OscillateScalar(operatorInfo),
           ["C_OP_SpinUpdate"] = operatorInfo => new SpinUpdate(operatorInfo),
       };

    // Register particle renderers
    static readonly IDictionary<string, Func<IDictionary<string, object>, OpenGLGfxModel, IParticleRenderer>> RendererDictionary
       = new Dictionary<string, Func<IDictionary<string, object>, OpenGLGfxModel, IParticleRenderer>> {
           ["C_OP_RenderSprites"] = (rendererInfo, gfx) => new OpenGLParticleRenderer.SpritesParticleRenderer(rendererInfo, gfx),
           ["C_OP_RenderTrails"] = (rendererInfo, gfx) => new OpenGLParticleRenderer.TrailsParticleRenderer(rendererInfo, gfx),
       };

    public static bool TryCreateEmitter(string name, IDictionary<string, object> baseProperties, IDictionary<string, object> emitterInfo, out IParticleEmitter emitter) {
        if (EmitterDictionary.TryGetValue(name, out var factory)) {
            emitter = factory(baseProperties, emitterInfo);
            return true;
        }
        emitter = default;
        return false;
    }

    public static bool TryCreateInitializer(string name, IDictionary<string, object> initializerInfo, out IParticleInitializer initializer) {
        if (InitializerDictionary.TryGetValue(name, out var factory)) { initializer = factory(initializerInfo); return true; }
        initializer = default;
        return false;
    }

    public static bool TryCreateOperator(string name, IDictionary<string, object> operatorInfo, out IParticleOperator @operator) {
        if (OperatorDictionary.TryGetValue(name, out var factory)) { @operator = factory(operatorInfo); return true; }
        @operator = default;
        return false;
    }

    public static bool TryCreateRender(string name, IDictionary<string, object> rendererInfo, OpenGLGfxModel gfx, out IParticleRenderer renderer) {
        if (RendererDictionary.TryGetValue(name, out var factory)) { renderer = factory(rendererInfo, gfx); return true; }
        renderer = default;
        return false;
    }
}

#endregion