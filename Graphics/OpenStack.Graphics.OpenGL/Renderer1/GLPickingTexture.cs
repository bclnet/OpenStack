using OpenStack.Graphics.Renderer1;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace OpenStack.Graphics.OpenGL.Renderer1
{
    public class GLPickingTexture : IDisposable, IPickingTexture
    {
        public class PickingRequest
        {
            public bool ActiveNextFrame;
            public int CursorPositionX;
            public int CursorPositionY;
            public PickingIntent Intent;

            public void NextFrame(int x, int y, PickingIntent intent)
            {
                ActiveNextFrame = true;
                CursorPositionX = x;
                CursorPositionY = y;
                Intent = intent;
            }
        }

        public enum PickingIntent
        {
            Select,
            Open
        }

        public struct PickingResponse
        {
            public PickingIntent Intent;
            public PixelInfo PixelInfo;
        }

        public struct PixelInfo
        {
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
            public uint ObjectId;
            public uint MeshId;
            public uint Unused2;
#pragma warning restore CS0649  // Field is never assigned to, and will always have its default value
        }

        public event EventHandler<PickingResponse> OnPicked;
        public readonly PickingRequest Request = new PickingRequest();

        public Shader Shader { get; }
        public Shader DebugShader { get; }

        public bool IsActive => Request.ActiveNextFrame;
        public bool Debug { get; set; }

        int width = 4;
        int height = 4;
        int fboHandle;
        int colorHandle;
        int depthHandle;

        public GLPickingTexture(IOpenGLGraphic graphic, EventHandler<PickingResponse> onPicked)
        {
            Shader = graphic.LoadShader("vrf.picking", new Dictionary<string, bool>());
            DebugShader = graphic.LoadShader("vrf.picking", new Dictionary<string, bool>() { { "F_DEBUG_PICKER", true } });
            OnPicked += onPicked;
            Setup();
        }

        public void Setup()
        {
            fboHandle = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboHandle);

            colorHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, colorHandle);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32ui, width, height, 0, PixelFormat.RgbaInteger, PixelType.UnsignedInt, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, colorHandle, 0);

            depthHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, depthHandle);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, width, height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthHandle, 0);

            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete) throw new InvalidOperationException($"Framebuffer failed to bind with error: {status}");

            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Render()
        {
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, fboHandle);
            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        public void Finish()
        {
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);

            if (Request.ActiveNextFrame)
            {
                Request.ActiveNextFrame = false;
                var pixelInfo = ReadPixelInfo(Request.CursorPositionX, Request.CursorPositionY);
                OnPicked?.Invoke(this, new PickingResponse
                {
                    Intent = Request.Intent,
                    PixelInfo = pixelInfo,
                });
            }
        }

        public void Resize(int width, int height)
        {
            this.width = width;
            this.height = height;
            GL.BindTexture(TextureTarget.Texture2D, colorHandle);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32ui, width, height, 0, PixelFormat.RgbaInteger, PixelType.UnsignedInt, IntPtr.Zero);
            GL.BindTexture(TextureTarget.Texture2D, depthHandle);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, width, height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
        }

        public PixelInfo ReadPixelInfo(int width, int height)
        {
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

        public void Dispose()
        {
            OnPicked = null;
            GL.DeleteTexture(colorHandle);
            GL.DeleteTexture(depthHandle);
            GL.DeleteFramebuffer(fboHandle);
        }
    }
}
