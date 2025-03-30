using OpenStack.Gfx;
using OpenStack.Gfx.Texture;
using System;
using static OpenStack.Debug;

namespace OpenStack.Stride.Renderers;

#region TestTriRenderer

/// <summary>
/// TestTriRenderer
/// </summary>
public class TestTriRenderer : Renderer
{
    readonly IStrideGfx3d Gfx;

    public TestTriRenderer(IStrideGfx3d gfx, object obj)
    {
        Gfx = gfx;
    }
}

#endregion

#region TextureRenderer

/// <summary>
/// TextureRenderer
/// </summary>
public class TextureRenderer : Renderer
{
    readonly IStrideGfx3d Gfx;
    readonly object Obj;
    readonly Range Level;
    readonly object Texture;
    int FrameDelay;

    public TextureRenderer(IStrideGfx3d gfx, object obj, Range level)
    {
        Gfx = gfx;
        Obj = obj;
        Level = level;
        Gfx.TextureManager.DeleteTexture(obj);
        Texture = Gfx.TextureManager.CreateTexture(obj, level).tex;
    }

    public override void Start()
    {
        Log($"MakeTexture");
        Log($"Done");
    }

    public override void Update(float deltaTime)
    {
        if (Obj is not ITextureFrames obj || Gfx == null || !obj.HasFrames) return;
        FrameDelay += (int)deltaTime;
        if (FrameDelay <= obj.Fps || !obj.DecodeFrame()) return;
        FrameDelay = 0; // reset delay between frames
        Gfx.TextureManager.ReloadTexture(obj, Level);
    }
}

#endregion

//if (!string.IsNullOrEmpty(View.Param1)) MakeTexture(View.Param1);
//Entity MakeTexture(string path)
//{
//    //var obj = GeometricPrimitive.Plane.New(Game.GraphicsDevice).ToMeshDraw();
//    var obj = new Entity("Name", rotation: Quaternion.CreateFromYawPitchRoll(-90f, 180f, -180f))
//    {
//        new ModelComponent(new PlaneProceduralModel().Generate(Game.Services))
//    };
//    //var obj = Content.CreatePrimitive(PrimitiveType.Plane);
//    //obj.transform.rotation = ;
//    //var meshRenderer = obj.GetComponent<MeshRenderer>();
//    //(meshRenderer.material, _) = Gfx.MaterialManager.CreateMaterial(new FixedMaterialInfo { MainFilePath = path });
//    return obj;
//}