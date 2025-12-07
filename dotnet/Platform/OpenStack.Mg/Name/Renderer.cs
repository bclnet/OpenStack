using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#pragma warning disable CS0649, CS0169, CS8500

namespace OpenStack.Mg;

#region Effects

class BasicXEffect : Effect {
    public EffectParameter MatrixTransform;
    public EffectParameter WorldMatrix;
    public EffectParameter Viewport;
    public EffectParameter Brighlight;
    public EffectPass Pass;
    public BasicXEffect(GraphicsDevice graphicsDevice) : base(graphicsDevice, GetShader()) {
        MatrixTransform = Parameters["MatrixTransform"];
        WorldMatrix = Parameters["WorldMatrix"];
        Viewport = Parameters["Viewport"];
        Brighlight = Parameters["Brightlight"];
        CurrentTechnique = Techniques["HueTechnique"];
        Pass = CurrentTechnique.Passes[0];
    }
    static byte[] GetShader() { using var ms = typeof(BasicXEffect).Assembly.GetManifestResourceStream("OpenStack.Mg.Name.shaders.IsometricWorld.fxc"); var b = new byte[ms.Length]; ms.ReadExactly(b); return b; }
}

public class XbrEffect : Effect {
    public EffectParameter MatrixTransform;
    public EffectParameter TextureSize;
    public XbrEffect(GraphicsDevice graphicsDevice) : base(graphicsDevice, GetShader()) {
        MatrixTransform = Parameters["MatrixTransform"];
        TextureSize = Parameters["textureSize"];
    }
    static byte[] GetShader() { using var ms = typeof(BasicXEffect).Assembly.GetManifestResourceStream("OpenStack.Mg.Name.shaders.xBR.fxc"); var b = new byte[ms.Length]; ms.ReadExactly(b); return b; }
}

#endregion

#region Batcher2D

public unsafe class Batcher2D(GraphicsDevice device) : IDisposable {
    [Conditional("DEBUG")] void AssertStarted() { if (!_started) throw new InvalidOperationException(); }
    [Conditional("DEBUG")] void AssertNotStarted() { if (_started) throw new InvalidOperationException(); }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct PositionNormalTextureColor4 : IVertexType {
        static readonly VertexDeclaration VertexDeclaration = new(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),                          // position
            new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),            // normal
            new VertexElement(sizeof(float) * 6, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0), // tex coord
            new VertexElement(sizeof(float) * 9, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 1)  // hue
        );

        public const int SizeOf = sizeof(float) * 12 * 4;

        public Vector3 Position0;
        public Vector3 Normal0;
        public Vector3 TextureCoordinate0;
        public Vector3 Hue0;

        public Vector3 Position1;
        public Vector3 Normal1;
        public Vector3 TextureCoordinate1;
        public Vector3 Hue1;

        public Vector3 Position2;
        public Vector3 Normal2;
        public Vector3 TextureCoordinate2;
        public Vector3 Hue2;

        public Vector3 Position3;
        public Vector3 Normal3;
        public Vector3 TextureCoordinate3;
        public Vector3 Hue3;

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    }

    static readonly float[] _cornerOffsetX = [0.0f, 1.0f, 0.0f, 1.0f];
    static readonly float[] _cornerOffsetY = [0.0f, 0.0f, 1.0f, 1.0f];
    static readonly DepthStencilState DefaultStencil = new() {
        StencilEnable = false,
        DepthBufferEnable = false,
        StencilFunction = CompareFunction.NotEqual,
        ReferenceStencil = -1,
        StencilMask = -1,
        StencilFail = StencilOperation.Keep,
        StencilDepthBufferFail = StencilOperation.Keep,
        StencilPass = StencilOperation.Keep
    };

    const int MAX_SPRITES = 0x800;
    const int MAX_VERTICES = MAX_SPRITES * 4;
    const int MAX_INDICES = MAX_SPRITES * 6;
    BlendState _blendState = BlendState.AlphaBlend;
    int _currentBufferPosition;
    Effect _customEffect;
    readonly IndexBuffer _indexBuffer = IndexBufferSetData(new(device, IndexElementSize.SixteenBits, MAX_INDICES, BufferUsage.WriteOnly));
    int _numSprites;
    Matrix _projectionMatrix = new(
        0f, 0f, 0f, 0f,
        0f, 0f, 0f, 0f,
        0f, 0f, 1f, 0f,
        -1f, 1f, 0f, 1f);
    readonly RasterizerState _rasterizerState = new() {
        CullMode = CullMode.CullCounterClockwiseFace,
        FillMode = FillMode.Solid,
        DepthBias = 0,
        MultiSampleAntiAlias = true,
        ScissorTestEnable = true,
        SlopeScaleDepthBias = 0,
    };
    SamplerState _sampler = SamplerState.PointClamp;
    bool _started;
    DepthStencilState _stencil = DefaultStencil;
    Matrix TransformMatrix;
    readonly DynamicVertexBuffer _vertexBuffer = new(device, typeof(PositionNormalTextureColor4), MAX_VERTICES, BufferUsage.WriteOnly);
    readonly BasicXEffect _basicEffect = new(device);
    Texture2D[] _textureInfo = new Texture2D[MAX_SPRITES];
    PositionNormalTextureColor4[] _vertexInfo = new PositionNormalTextureColor4[MAX_SPRITES];
    public GraphicsDevice GraphicsDevice = device;
    public int TextureSwitches, FlushesDone;
    static IndexBuffer IndexBufferSetData(IndexBuffer buffer) {
        var indexs = new short[MAX_INDICES];
        for (int i = 0, j = 0; i < MAX_INDICES; i += 6, j += 4) {
            indexs[i] = (short)j;
            indexs[i + 1] = (short)(j + 1);
            indexs[i + 2] = (short)(j + 2);
            indexs[i + 3] = (short)(j + 1);
            indexs[i + 4] = (short)(j + 3);
            indexs[i + 5] = (short)(j + 2);
        }
        buffer.SetData(indexs);
        return buffer;
    }

    public void Dispose() {
        _vertexInfo = null;
        _basicEffect?.Dispose();
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
    }

    public void SetBrightlight(float f) => _basicEffect.Brighlight.SetValue(f);

    public void DrawString(SpriteFont spriteFont, ReadOnlySpan<char> text, int x, int y, Vector3 color) => DrawString(spriteFont, text, new Vector2(x, y), color);
    public void DrawString(SpriteFont spriteFont, ReadOnlySpan<char> text, Vector2 position, Vector3 color) {
        if (text.IsEmpty) return;
        Resize();
        var texture = spriteFont.Texture;
        var glyphs = spriteFont.Glyphs;
        var characters = spriteFont.Characters;
        var offset = Vector2.Zero;
        var firstInLine = true;
        var baseOffset = Vector2.Zero;
        float axisDirX = 1f, axisDirY = 1f;
        int index, defaultIndex = characters.IndexOf(spriteFont.DefaultCharacter ?? '?');
        foreach (var c in text) {
            // special characters
            if (c == '\r') continue;
            else if (c == '\n') { offset.X = 0.0f; offset.Y += spriteFont.LineSpacing; firstInLine = true; continue; }
            // get the List index from the character map, defaulting to the DefaultCharacter if it's set.
            ref SpriteFont.Glyph glyph = ref glyphs[(index = characters.IndexOf(c)) != -1 ? index : defaultIndex];
            // for the first character in a line, always push the width rightward, even if the kerning pushes the character to the left.
            if (firstInLine) { offset.X += Math.Abs(glyph.LeftSideBearing); firstInLine = false; }
            else offset.X += spriteFont.Spacing + glyph.LeftSideBearing;
            // calculate the character origin
            var pos = new Vector2(baseOffset.X + (offset.X + glyph.Cropping.X) * axisDirX, baseOffset.Y + (offset.Y + glyph.Cropping.Y) * axisDirY);
            //ref Microsoft.Xna.Framework.Rectangle bit = ref glyph.BoundsInTexture;
            var rect = glyph.BoundsInTexture; // new Rectangle(bit.X, bit.Y, bit.Width, bit.Height);
            Draw(texture, position + pos, rect, color);
            offset.X += glyph.Width + glyph.RightSideBearing;
        }
    }

    public struct YOffsets { public int Top; public int Right; public int Left; public int Bottom; }

    public void DrawStretchedLand(Texture2D texture, Vector2 position, Rectangle sourceRect, ref YOffsets yOffsets, ref Vector3 normalTop, ref Vector3 normalRight, ref Vector3 normalLeft, ref Vector3 normalBottom, Vector3 hue, float depth) {
        Resize();
        ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];

        // we need to apply an offset to the texture
        float sourceX = (sourceRect.X + 0.5f) / texture.Width, sourceY = (sourceRect.Y + 0.5f) / texture.Height, sourceW = (sourceRect.Width - 1f) / texture.Width, sourceH = (sourceRect.Height - 1f) / texture.Height;

        vertex.TextureCoordinate0.X = (_cornerOffsetX[0] * sourceW) + sourceX; vertex.TextureCoordinate0.Y = (_cornerOffsetY[0] * sourceH) + sourceY;
        vertex.TextureCoordinate1.X = (_cornerOffsetX[1] * sourceW) + sourceX; vertex.TextureCoordinate1.Y = (_cornerOffsetY[1] * sourceH) + sourceY;
        vertex.TextureCoordinate2.X = (_cornerOffsetX[2] * sourceW) + sourceX; vertex.TextureCoordinate2.Y = (_cornerOffsetY[2] * sourceH) + sourceY;
        vertex.TextureCoordinate3.X = (_cornerOffsetX[3] * sourceW) + sourceX; vertex.TextureCoordinate3.Y = (_cornerOffsetY[3] * sourceH) + sourceY;
        vertex.TextureCoordinate0.Z = vertex.TextureCoordinate1.Z = vertex.TextureCoordinate2.Z = vertex.TextureCoordinate3.Z = 0;

        vertex.Normal0 = normalTop; vertex.Normal1 = normalRight; vertex.Normal2 = normalLeft; vertex.Normal3 = normalBottom;

        vertex.Position0.X = position.X + 22; vertex.Position0.Y = position.Y - yOffsets.Top; // Top
        vertex.Position1.X = position.X + 44; vertex.Position1.Y = position.Y + (22 - yOffsets.Right); // Right
        vertex.Position2.X = position.X; vertex.Position2.Y = position.Y + (22 - yOffsets.Left); // Left
        vertex.Position3.X = position.X + 22; vertex.Position3.Y = position.Y + (44 - yOffsets.Bottom); // Bottom
        vertex.Position0.Z = vertex.Position1.Z = vertex.Position2.Z = vertex.Position3.Z = depth;
        vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;
        PushSprite(texture);
    }

    public const byte SHADER_SHADOW = 8;
    public void DrawShadow(Texture2D texture, Vector2 position, Rectangle sourceRect, bool flip, float depth) {
        float width = sourceRect.Width, height = sourceRect.Height * 0.5f, translatedY = position.Y + height - 10, ratio = height / width;
        Resize();

        ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];

        vertex.Position0.X = position.X + width * ratio; vertex.Position0.Y = translatedY;
        vertex.Position1.X = position.X + width * (ratio + 1f); vertex.Position1.Y = translatedY;
        vertex.Position2.X = position.X; vertex.Position2.Y = translatedY + height;
        vertex.Position3.X = position.X + width; vertex.Position3.Y = translatedY + height;
        vertex.Position0.Z = vertex.Position1.Z = vertex.Position2.Z = vertex.Position3.Z = depth;

        float sourceX = (sourceRect.X + 0.5f) / texture.Width, sourceY = (sourceRect.Y + 0.5f) / texture.Height, sourceW = (sourceRect.Width - 1f) / texture.Width, sourceH = (sourceRect.Height - 1f) / texture.Height;
        byte effects = (byte)((flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None) & (SpriteEffects)0x03);

        vertex.TextureCoordinate0.X = (_cornerOffsetX[0 ^ effects] * sourceW) + sourceX; vertex.TextureCoordinate0.Y = (_cornerOffsetY[0 ^ effects] * sourceH) + sourceY;
        vertex.TextureCoordinate1.X = (_cornerOffsetX[1 ^ effects] * sourceW) + sourceX; vertex.TextureCoordinate1.Y = (_cornerOffsetY[1 ^ effects] * sourceH) + sourceY;
        vertex.TextureCoordinate2.X = (_cornerOffsetX[2 ^ effects] * sourceW) + sourceX; vertex.TextureCoordinate2.Y = (_cornerOffsetY[2 ^ effects] * sourceH) + sourceY;
        vertex.TextureCoordinate3.X = (_cornerOffsetX[3 ^ effects] * sourceW) + sourceX; vertex.TextureCoordinate3.Y = (_cornerOffsetY[3 ^ effects] * sourceH) + sourceY;
        vertex.TextureCoordinate0.Z = vertex.TextureCoordinate1.Z = vertex.TextureCoordinate2.Z = vertex.TextureCoordinate3.Z = 0;

        vertex.Normal0.X = 0; vertex.Normal0.Y = 0; vertex.Normal0.Z = 1;
        vertex.Normal1.X = 0; vertex.Normal1.Y = 0; vertex.Normal1.Z = 1;
        vertex.Normal2.X = 0; vertex.Normal2.Y = 0; vertex.Normal2.Z = 1;
        vertex.Normal3.X = 0; vertex.Normal3.Y = 0; vertex.Normal3.Z = 1;
        vertex.Hue0.Z = vertex.Hue1.Z = vertex.Hue2.Z = vertex.Hue3.Z = vertex.Hue0.X = vertex.Hue1.X = vertex.Hue2.X = vertex.Hue3.X = 0;
        vertex.Hue0.Y = vertex.Hue1.Y = vertex.Hue2.Y = vertex.Hue3.Y = SHADER_SHADOW;
        PushSprite(texture);
    }

    public void DrawCharacterSitted(Texture2D texture, Vector2 position, Rectangle sourceRect, Vector3 mod, Vector3 hue, bool flip, float depth) {
        Resize();

        float h03 = sourceRect.Height * mod.X, h06 = sourceRect.Height * mod.Y, h09 = sourceRect.Height * mod.Z;
        float sittingOffset = flip ? -8.0f : 8.0f;
        float width = sourceRect.Width, widthOffset = sourceRect.Width + sittingOffset;

        if (mod.X != 0.0f) {
            Resize();
            ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];

            vertex.Position0.X = position.X + sittingOffset; vertex.Position0.Y = position.Y;
            vertex.Position1.X = position.X + widthOffset; vertex.Position1.Y = position.Y;
            vertex.Position2.X = position.X + sittingOffset; vertex.Position2.Y = position.Y + h03;
            vertex.Position3.X = position.X + widthOffset; vertex.Position3.Y = position.Y + h03;
            vertex.Position0.Z = vertex.Position1.Z = vertex.Position2.Z = vertex.Position3.Z = depth;

            float sourceX = (sourceRect.X + 0.5f) / texture.Width, sourceY = (sourceRect.Y + 0.5f) / texture.Height, sourceW = (sourceRect.Width - 1f) / texture.Width, sourceH = (sourceRect.Height - 1f) / texture.Height;
            byte effects = (byte)((flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None) & (SpriteEffects)0x03);

            vertex.TextureCoordinate0.X = (_cornerOffsetX[0 ^ effects] * sourceW) + sourceX; vertex.TextureCoordinate0.Y = (_cornerOffsetY[0 ^ effects] * sourceH) + sourceY;
            vertex.TextureCoordinate1.X = (_cornerOffsetX[1 ^ effects] * sourceW) + sourceX; vertex.TextureCoordinate1.Y = (_cornerOffsetY[1 ^ effects] * sourceH) + sourceY;
            vertex.TextureCoordinate2.X = (_cornerOffsetX[2 ^ effects] * sourceW) + sourceX; vertex.TextureCoordinate2.Y = (_cornerOffsetY[2 ^ effects] * sourceH * mod.X) + sourceY;
            vertex.TextureCoordinate3.X = (_cornerOffsetX[3 ^ effects] * sourceW) + sourceX; vertex.TextureCoordinate3.Y = (_cornerOffsetY[3 ^ effects] * sourceH * mod.X) + sourceY;
            vertex.TextureCoordinate0.Z = vertex.TextureCoordinate1.Z = vertex.TextureCoordinate2.Z = vertex.TextureCoordinate3.Z = 0;

            vertex.Normal0.X = 0; vertex.Normal0.Y = 0; vertex.Normal0.Z = 1;
            vertex.Normal1.X = 0; vertex.Normal1.Y = 0; vertex.Normal1.Z = 1;
            vertex.Normal2.X = 0; vertex.Normal2.Y = 0; vertex.Normal2.Z = 1;
            vertex.Normal3.X = 0; vertex.Normal3.Y = 0; vertex.Normal3.Z = 1;
            vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;
            PushSprite(texture);
        }

        if (mod.Y != 0.0f) {
            Resize();

            ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];

            vertex.Position0.X = position.X + sittingOffset; vertex.Position0.Y = position.Y + h03;
            vertex.Position1.X = position.X + widthOffset; vertex.Position1.Y = position.Y + h03;
            vertex.Position2.X = position.X; vertex.Position2.Y = position.Y + h06;
            vertex.Position3.X = position.X + width; vertex.Position3.Y = position.Y + h06;
            vertex.Position0.Z = vertex.Position1.Z = vertex.Position2.Z = vertex.Position3.Z = depth;

            float sourceX = (sourceRect.X + 0.5f) / texture.Width, sourceY = (sourceRect.Y + 0.5f + h03) / texture.Height, sourceW = (sourceRect.Width - 1f) / texture.Width, sourceH = (sourceRect.Height - 1f - h03) / texture.Height;
            byte effects = (byte)((flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None) & (SpriteEffects)0x03);

            vertex.TextureCoordinate0.X = (_cornerOffsetX[0 ^ effects] * sourceW) + sourceX; vertex.TextureCoordinate0.Y = (_cornerOffsetY[0 ^ effects] * sourceH) + sourceY;
            vertex.TextureCoordinate1.X = (_cornerOffsetX[1 ^ effects] * sourceW) + sourceX; vertex.TextureCoordinate1.Y = (_cornerOffsetY[1 ^ effects] * sourceH) + sourceY;
            vertex.TextureCoordinate2.X = (_cornerOffsetX[2 ^ effects] * sourceW) + sourceX; vertex.TextureCoordinate2.Y = (_cornerOffsetY[2 ^ effects] * sourceH * mod.Y) + sourceY;
            vertex.TextureCoordinate3.X = (_cornerOffsetX[3 ^ effects] * sourceW) + sourceX; vertex.TextureCoordinate3.Y = (_cornerOffsetY[3 ^ effects] * sourceH * mod.Y) + sourceY;
            vertex.TextureCoordinate0.Z = vertex.TextureCoordinate1.Z = vertex.TextureCoordinate2.Z = vertex.TextureCoordinate3.Z = 0;

            vertex.Normal0.X = 0; vertex.Normal0.Y = 0; vertex.Normal0.Z = 1;
            vertex.Normal1.X = 0; vertex.Normal1.Y = 0; vertex.Normal1.Z = 1;
            vertex.Normal2.X = 0; vertex.Normal2.Y = 0; vertex.Normal2.Z = 1;
            vertex.Normal3.X = 0; vertex.Normal3.Y = 0; vertex.Normal3.Z = 1;
            vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;
            PushSprite(texture);
        }

        if (mod.Z != 0.0f) {
            Resize();

            ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];

            vertex.Position0.X = position.X; vertex.Position0.Y = position.Y + h06;
            vertex.Position1.X = position.X + width; vertex.Position1.Y = position.Y + h06;
            vertex.Position2.X = position.X; vertex.Position2.Y = position.Y + h09;
            vertex.Position3.X = position.X + width; vertex.Position3.Y = position.Y + h09;
            vertex.Position0.Z = vertex.Position1.Z = vertex.Position2.Z = vertex.Position3.Z = depth;

            float sourceX = (sourceRect.X + 0.5f) / texture.Width, sourceY = (sourceRect.Y + 0.5f + h06) / texture.Height, sourceW = (sourceRect.Width - 1f) / texture.Width, sourceH = (sourceRect.Height - 1f - h06) / texture.Height;
            byte effects = (byte)((flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None) & (SpriteEffects)0x03);

            vertex.TextureCoordinate0.X = (_cornerOffsetX[0 ^ effects] * sourceW) + sourceX; vertex.TextureCoordinate0.Y = (_cornerOffsetY[0 ^ effects] * sourceH) + sourceY;
            vertex.TextureCoordinate1.X = (_cornerOffsetX[1 ^ effects] * sourceW) + sourceX; vertex.TextureCoordinate1.Y = (_cornerOffsetY[1 ^ effects] * sourceH) + sourceY;
            vertex.TextureCoordinate2.X = (_cornerOffsetX[2 ^ effects] * sourceW) + sourceX; vertex.TextureCoordinate2.Y = (_cornerOffsetY[2 ^ effects] * sourceH * mod.Z) + sourceY;
            vertex.TextureCoordinate3.X = (_cornerOffsetX[3 ^ effects] * sourceW) + sourceX; vertex.TextureCoordinate3.Y = (_cornerOffsetY[3 ^ effects] * sourceH * mod.Z) + sourceY;
            vertex.TextureCoordinate0.Z = vertex.TextureCoordinate1.Z = vertex.TextureCoordinate2.Z = vertex.TextureCoordinate3.Z = 0;

            vertex.Normal0.X = 0; vertex.Normal0.Y = 0; vertex.Normal0.Z = 1;
            vertex.Normal1.X = 0; vertex.Normal1.Y = 0; vertex.Normal1.Z = 1;
            vertex.Normal2.X = 0; vertex.Normal2.Y = 0; vertex.Normal2.Z = 1;
            vertex.Normal3.X = 0; vertex.Normal3.Y = 0; vertex.Normal3.Z = 1;
            vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;
            PushSprite(texture);
        }
    }

    public void DrawTiled(Texture2D texture, Rectangle destinationRectangle, Rectangle sourceRectangle, Vector3 hue) {
        var h = destinationRectangle.Height;
        var rect = sourceRectangle;
        var pos = new Vector2(destinationRectangle.X, destinationRectangle.Y);
        while (h > 0) {
            pos.X = destinationRectangle.X;
            var w = destinationRectangle.Width;
            rect.Height = Math.Min(h, sourceRectangle.Height);
            while (w > 0) {
                rect.Width = Math.Min(w, sourceRectangle.Width);
                Draw(texture, pos, rect, hue);
                w -= sourceRectangle.Width;
                pos.X += sourceRectangle.Width;
            }
            h -= sourceRectangle.Height;
            pos.Y += sourceRectangle.Height;
        }
    }

    public bool DrawRectangle(Texture2D texture, int x, int y, int width, int height, Vector3 hue, float depth = 0f) {
        var rect = new Rectangle(x, y, width, 1);
        Draw(texture, rect, null, hue, 0f, Vector2.Zero, SpriteEffects.None, depth);
        rect.X += width; rect.Width = 1; rect.Height += height;
        Draw(texture, rect, null, hue, 0f, Vector2.Zero, SpriteEffects.None, depth);
        rect.X = x; rect.Y = y + height; rect.Width = width; rect.Height = 1;
        Draw(texture, rect, null, hue, 0f, Vector2.Zero, SpriteEffects.None, depth);
        rect.X = x; rect.Y = y; rect.Width = 1;
        rect.Height = height; Draw(texture, rect, null, hue, 0f, Vector2.Zero, SpriteEffects.None, depth);
        return true;
    }

    public void DrawLine(Texture2D texture, Vector2 start, Vector2 end, Vector3 color, float stroke) {
        var radians = start.AngleBetween(end);
        Vector2.Distance(ref start, ref end, out var length);
        Draw(texture, start, texture.Bounds, color, radians, Vector2.Zero, new Vector2(length, stroke), SpriteEffects.None, 0);
    }

    public void Draw(Texture2D texture, Vector2 position, Vector3 color)
        => AddSprite(texture, 0f, 0f, 1f, 1f, position.X, position.Y, texture.Width, texture.Height, color, 0f, 0f, 0f, 1f, 0f, 0);

    public void Draw(Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Vector3 color) {
        float sourceX, sourceY, sourceW, sourceH, destW, destH;
        if (sourceRectangle.HasValue) { sourceX = sourceRectangle.Value.X / (float)texture.Width; sourceY = sourceRectangle.Value.Y / (float)texture.Height; sourceW = sourceRectangle.Value.Width / (float)texture.Width; sourceH = sourceRectangle.Value.Height / (float)texture.Height; destW = sourceRectangle.Value.Width; destH = sourceRectangle.Value.Height; }
        else { sourceX = 0.0f; sourceY = 0.0f; sourceW = 1.0f; sourceH = 1.0f; destW = texture.Width; destH = texture.Height; }
        AddSprite(texture, sourceX, sourceY, sourceW, sourceH, position.X, position.Y, destW, destH, color, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0);
    }

    public void Draw(Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Vector3 color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth) {
        float sourceX, sourceY, sourceW, sourceH, destW = scale, destH = scale;
        if (sourceRectangle.HasValue) { sourceX = sourceRectangle.Value.X / (float)texture.Width; sourceY = sourceRectangle.Value.Y / (float)texture.Height; sourceW = Math.Sign(sourceRectangle.Value.Width) * Math.Max(Math.Abs(sourceRectangle.Value.Width), PlatformX.Epsilon) / texture.Width; sourceH = Math.Sign(sourceRectangle.Value.Height) * Math.Max(Math.Abs(sourceRectangle.Value.Height), PlatformX.Epsilon) / texture.Height; destW *= sourceRectangle.Value.Width; destH *= sourceRectangle.Value.Height; }
        else { sourceX = 0.0f; sourceY = 0.0f; sourceW = 1.0f; sourceH = 1.0f; destW *= texture.Width; destH *= texture.Height; }
        AddSprite(texture, sourceX, sourceY, sourceW, sourceH, position.X, position.Y, destW, destH, color, origin.X / sourceW / texture.Width, origin.Y / sourceH / texture.Height, (float)Math.Sin(rotation), (float)Math.Cos(rotation), layerDepth, (byte)(effects & (SpriteEffects)0x03));
    }

    public void Draw(Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Vector3 color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth) {
        float sourceX, sourceY, sourceW, sourceH;
        if (sourceRectangle.HasValue) { sourceX = sourceRectangle.Value.X / (float)texture.Width; sourceY = sourceRectangle.Value.Y / (float)texture.Height; sourceW = Math.Sign(sourceRectangle.Value.Width) * Math.Max(Math.Abs(sourceRectangle.Value.Width), PlatformX.Epsilon) / texture.Width; sourceH = Math.Sign(sourceRectangle.Value.Height) * Math.Max(Math.Abs(sourceRectangle.Value.Height), PlatformX.Epsilon) / texture.Height; scale.X *= sourceRectangle.Value.Width; scale.Y *= sourceRectangle.Value.Height; }
        else { sourceX = 0.0f; sourceY = 0.0f; sourceW = 1.0f; sourceH = 1.0f; scale.X *= texture.Width; scale.Y *= texture.Height; }
        AddSprite(texture, sourceX, sourceY, sourceW, sourceH, position.X, position.Y, scale.X, scale.Y, color, origin.X / sourceW / texture.Width, origin.Y / sourceH / texture.Height, (float)Math.Sin(rotation), (float)Math.Cos(rotation), layerDepth, (byte)(effects & (SpriteEffects)0x03));
    }

    public void Draw(Texture2D texture, Rectangle destinationRectangle, Vector3 color)
        => AddSprite(texture, 0.0f, 0.0f, 1.0f, 1.0f, destinationRectangle.X, destinationRectangle.Y, destinationRectangle.Width, destinationRectangle.Height, color, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0);

    public void Draw(Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Vector3 color) {
        float sourceX, sourceY, sourceW, sourceH;
        if (sourceRectangle.HasValue) { sourceX = sourceRectangle.Value.X / (float)texture.Width; sourceY = sourceRectangle.Value.Y / (float)texture.Height; sourceW = sourceRectangle.Value.Width / (float)texture.Width; sourceH = sourceRectangle.Value.Height / (float)texture.Height; }
        else { sourceX = 0.0f; sourceY = 0.0f; sourceW = 1.0f; sourceH = 1.0f; }
        AddSprite(texture, sourceX, sourceY, sourceW, sourceH, destinationRectangle.X, destinationRectangle.Y, destinationRectangle.Width, destinationRectangle.Height, color, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0);
    }

    public void Draw(Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Vector3 color, float rotation, Vector2 origin, SpriteEffects effects, float layerDepth) {
        float sourceX, sourceY, sourceW, sourceH;
        if (sourceRectangle.HasValue) { sourceX = sourceRectangle.Value.X / (float)texture.Width; sourceY = sourceRectangle.Value.Y / (float)texture.Height; sourceW = Math.Sign(sourceRectangle.Value.Width) * Math.Max(Math.Abs(sourceRectangle.Value.Width), PlatformX.Epsilon) / texture.Width; sourceH = Math.Sign(sourceRectangle.Value.Height) * Math.Max(Math.Abs(sourceRectangle.Value.Height), PlatformX.Epsilon) / texture.Height; }
        else { sourceX = 0.0f; sourceY = 0.0f; sourceW = 1.0f; sourceH = 1.0f; }
        AddSprite(texture, sourceX, sourceY, sourceW, sourceH, destinationRectangle.X, destinationRectangle.Y, destinationRectangle.Width, destinationRectangle.Height, color, origin.X / sourceW / texture.Width, origin.Y / sourceH / texture.Height, (float)Math.Sin(rotation), (float)Math.Cos(rotation), layerDepth, (byte)(effects & (SpriteEffects)0x03));
    }

    void AddSprite(Texture2D texture, float sourceX, float sourceY, float sourceW, float sourceH, float destinationX, float destinationY, float destinationW, float destinationH, Vector3 color, float originX, float originY, float rotationSin, float rotationCos, float depth, byte effects) {
        Resize();
        SetVertex(ref _vertexInfo[_numSprites], sourceX, sourceY, sourceW, sourceH, destinationX, destinationY, destinationW, destinationH, color, originX, originY, rotationSin, rotationCos, depth, effects);
        _textureInfo[_numSprites] = texture;
        ++_numSprites;
    }

    public void Begin() => Begin(null, Matrix.Identity);
    public void Begin(Effect effect) => Begin(effect, Matrix.Identity);
    public void Begin(Effect customEffect, Matrix transformMatrix) { AssertNotStarted(); _started = true; TextureSwitches = 0; FlushesDone = 0; _customEffect = customEffect; TransformMatrix = transformMatrix; }
    public void End() { AssertStarted(); Flush(); _started = false; _customEffect = null; }

    void SetVertex(ref PositionNormalTextureColor4 sprite, float sourceX, float sourceY, float sourceW, float sourceH, float destinationX, float destinationY, float destinationW, float destinationH, Vector3 color, float originX, float originY, float rotationSin, float rotationCos, float depth, byte effects) {
        float cornerX, cornerY;
        cornerX = -originX * destinationW; cornerY = -originY * destinationH; sprite.Position0.X = (-rotationSin * cornerY) + (rotationCos * cornerX) + destinationX; sprite.Position0.Y = (rotationCos * cornerY) + (rotationSin * cornerX) + destinationY;
        cornerX = (1.0f - originX) * destinationW; cornerY = -originY * destinationH; sprite.Position1.X = (-rotationSin * cornerY) + (rotationCos * cornerX) + destinationX; sprite.Position1.Y = (rotationCos * cornerY) + (rotationSin * cornerX) + destinationY;
        cornerX = -originX * destinationW; cornerY = (1.0f - originY) * destinationH; sprite.Position2.X = (-rotationSin * cornerY) + (rotationCos * cornerX) + destinationX; sprite.Position2.Y = (rotationCos * cornerY) + (rotationSin * cornerX) + destinationY;
        cornerX = (1.0f - originX) * destinationW; cornerY = (1.0f - originY) * destinationH; sprite.Position3.X = (-rotationSin * cornerY) + (rotationCos * cornerX) + destinationX; sprite.Position3.Y = (rotationCos * cornerY) + (rotationSin * cornerX) + destinationY;

        sprite.TextureCoordinate0.X = (_cornerOffsetX[0 ^ effects] * sourceW) + sourceX; sprite.TextureCoordinate0.Y = (_cornerOffsetY[0 ^ effects] * sourceH) + sourceY;
        sprite.TextureCoordinate1.X = (_cornerOffsetX[1 ^ effects] * sourceW) + sourceX; sprite.TextureCoordinate1.Y = (_cornerOffsetY[1 ^ effects] * sourceH) + sourceY;
        sprite.TextureCoordinate2.X = (_cornerOffsetX[2 ^ effects] * sourceW) + sourceX; sprite.TextureCoordinate2.Y = (_cornerOffsetY[2 ^ effects] * sourceH) + sourceY;
        sprite.TextureCoordinate3.X = (_cornerOffsetX[3 ^ effects] * sourceW) + sourceX; sprite.TextureCoordinate3.Y = (_cornerOffsetY[3 ^ effects] * sourceH) + sourceY;
        sprite.TextureCoordinate0.Z = sprite.TextureCoordinate1.Z = sprite.TextureCoordinate2.Z = sprite.TextureCoordinate3.Z = 0;

        sprite.Position0.Z = sprite.Position1.Z = sprite.Position2.Z = sprite.Position3.Z = depth;

        sprite.Hue0 = sprite.Hue1 = sprite.Hue2 = sprite.Hue3 = color;

        sprite.Normal0.X = 0; sprite.Normal0.Y = 0; sprite.Normal0.Z = 1;
        sprite.Normal1.X = 0; sprite.Normal1.Y = 0; sprite.Normal1.Z = 1;
        sprite.Normal2.X = 0; sprite.Normal2.Y = 0; sprite.Normal2.Z = 1;
        sprite.Normal3.X = 0; sprite.Normal3.Y = 0; sprite.Normal3.Z = 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Resize() {
        AssertStarted();
        if (_numSprites >= _vertexInfo.Length) { var newMax = _vertexInfo.Length + MAX_SPRITES; Array.Resize(ref _vertexInfo, newMax); Array.Resize(ref _textureInfo, newMax); }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool PushSprite(Texture2D texture) {
        if (texture == null || texture.IsDisposed) return false;
        Resize();
        _textureInfo[_numSprites++] = texture;
        return true;
    }

    void ApplyStates() {
        GraphicsDevice.BlendState = _blendState;
        GraphicsDevice.DepthStencilState = _stencil;
        GraphicsDevice.RasterizerState = _rasterizerState;
        GraphicsDevice.SamplerStates[0] = _sampler; GraphicsDevice.SamplerStates[1] = GraphicsDevice.SamplerStates[2] = GraphicsDevice.SamplerStates[3] = SamplerState.PointClamp;
        GraphicsDevice.Indices = _indexBuffer;
        GraphicsDevice.SetVertexBuffer(_vertexBuffer);

        _projectionMatrix.M11 = (float)(2.0 / GraphicsDevice.Viewport.Width); _projectionMatrix.M22 = (float)(-2.0 / GraphicsDevice.Viewport.Height);

        var matrix = _projectionMatrix;
        Matrix.CreateOrthographicOffCenter(0f, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, short.MinValue, short.MaxValue, out matrix);
        Matrix.Multiply(ref TransformMatrix, ref matrix, out matrix);

        //Matrix halfPixelOffset = Matrix.CreateTranslation(-0.5f, -0.5f, 0);
        //Matrix.Multiply(ref halfPixelOffset, ref matrix, out matrix);

        _basicEffect.WorldMatrix.SetValue(Matrix.Identity);
        _basicEffect.Viewport.SetValue(new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));
        _basicEffect.MatrixTransform.SetValue(matrix);
        _basicEffect.Pass.Apply();
    }

    void Flush() {
        if (_numSprites == 0) return;
        ApplyStates();
        var arrayOffset = 0;
    nextbatch:
        ++FlushesDone;
        int batchSize = Math.Min(_numSprites, MAX_SPRITES), baseOff = UpdateVertexBuffer(arrayOffset, batchSize), offset = 0;
        var texture = _textureInfo[arrayOffset];
        for (var i = 1; i < batchSize; ++i) {
            var tex = _textureInfo[arrayOffset + i];
            if (tex != texture) { ++TextureSwitches; InternalDraw(texture, baseOff + offset, i - offset); texture = tex; offset = i; }
        }
        InternalDraw(texture, baseOff + offset, batchSize - offset);
        if (_numSprites > MAX_SPRITES) { _numSprites -= MAX_SPRITES; arrayOffset += MAX_SPRITES; goto nextbatch; }
        _numSprites = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void InternalDraw(Texture texture, int baseSprite, int batchSize) {
        GraphicsDevice.Textures[0] = texture;
        if (_customEffect != null)
            foreach (var pass in _customEffect.CurrentTechnique.Passes) { pass.Apply(); GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseSprite << 2, 0, batchSize << 1); }
        else GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseSprite << 2, 0, batchSize << 1);
    }

    public bool ClipBegin(int x, int y, int width, int height) {
        if (width <= 0 || height <= 0) return false;
        Rectangle scissor = ScissorStack.CalculateScissors(TransformMatrix, x, y, width, height);
        Flush();
        if (ScissorStack.PushScissors(GraphicsDevice, scissor)) { EnableScissorTest(true); return true; }
        return false;
    }

    public void ClipEnd() {
        EnableScissorTest(false);
        ScissorStack.PopScissors(GraphicsDevice);
        Flush();
    }

    public void EnableScissorTest(bool enable) {
        bool rasterize = GraphicsDevice.RasterizerState.ScissorTestEnable;
        if (ScissorStack.HasScissors) enable = true;
        if (enable == rasterize) return;
        Flush();
        GraphicsDevice.RasterizerState.ScissorTestEnable = enable;
    }

    public void SetBlendState(BlendState blend) { Flush(); _blendState = blend ?? BlendState.AlphaBlend; }

    public void SetStencil(DepthStencilState stencil) { Flush(); _stencil = stencil ?? DefaultStencil; }

    public void SetSampler(SamplerState sampler) { Flush(); _sampler = sampler ?? SamplerState.PointClamp; }

    unsafe int UpdateVertexBuffer(int start, int count) {
        int offset; SetDataOptions hint;
        if (_currentBufferPosition + count > MAX_SPRITES) { offset = 0; hint = SetDataOptions.Discard; }
        else { offset = _currentBufferPosition; hint = SetDataOptions.NoOverwrite; }
        _vertexBuffer.SetData(_vertexInfo, offset, count, hint);
        _currentBufferPosition = offset + count;
        return offset;
    }
}

#endregion