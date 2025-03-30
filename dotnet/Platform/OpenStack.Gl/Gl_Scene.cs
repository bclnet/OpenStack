using OpenStack.Gfx;
using OpenStack.Gfx.Render;
using OpenStack.Gfx.Scene;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;
using static OpenStack.Gfx.Renderer;

namespace OpenStack.Gl.Scene;

/// <summary>
/// OctreeDebugRenderer
/// </summary>
public class OctreeDebugRenderer<T> where T : class
{
    const int STRIDE = sizeof(float) * 7;

    readonly Shader Shader;
    readonly object ShaderTag;
    readonly Octree<T> Octree;
    readonly int VaoHandle;
    readonly int VboHandle;
    readonly bool Dynamic;
    int VertexCount;

    public OctreeDebugRenderer(Octree<T> octree, IOpenGLGfx3d graphic, bool dynamic)
    {
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

    static void AddLine(List<float> vertices, Vector3 from, Vector3 to, float r, float g, float b, float a)
    {
        vertices.Add(from.X); vertices.Add(from.Y); vertices.Add(from.Z);
        vertices.Add(r); vertices.Add(g); vertices.Add(b); vertices.Add(a);
        vertices.Add(to.X); vertices.Add(to.Y); vertices.Add(to.Z);
        vertices.Add(r); vertices.Add(g); vertices.Add(b); vertices.Add(a);
    }

    static void AddBox(List<float> vertices, AABB box, float r, float g, float b, float a)
    {
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

    void AddOctreeNode(List<float> vertices, Octree<T>.Node node, int depth)
    {
        AddBox(vertices, node.Region, 1.0f, 1.0f, 1.0f, node.HasElements ? 1.0f : 0.1f);
        if (node.HasElements)
            foreach (var element in node.Elements)
            {
                var shading = Math.Min(1.0f, depth * 0.1f);
                AddBox(vertices, element.BoundingBox, 1.0f, shading, 0.0f, 1.0f);
                // AddLine(vertices, element.BoundingBox.Min, node.Region.Min, 1.0f, shading, 0.0f, 0.5f);
                // AddLine(vertices, element.BoundingBox.Max, node.Region.Max, 1.0f, shading, 0.0f, 0.5f);
            }
        if (node.HasChildren)
            foreach (var child in node.Children)
                AddOctreeNode(vertices, child, depth + 1);
    }

    void Rebuild()
    {
        var vertices = new List<float>();
        AddOctreeNode(vertices, Octree.Root, 0);
        VertexCount = vertices.Count / 7;
        GL.BindBuffer(BufferTarget.ArrayBuffer, VboHandle);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), Dynamic ? BufferUsageHint.DynamicDraw : BufferUsageHint.StaticDraw);
    }

    public void Render(Camera camera, Pass pass)
    {
        if (pass == Pass.Translucent || pass == Pass.Both)
        {
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