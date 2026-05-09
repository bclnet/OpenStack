using System;

namespace OpenStack.Gfx.Stride;

#region TestTriRenderer

/// <summary>
/// TestTriRenderer
/// </summary>
public class TestTriRenderer : Renderer {
    readonly StrideGfxModel GfxModel;

    public TestTriRenderer(IOpenGfx[] gfx, object obj) {
        GfxModel = (StrideGfxModel)gfx[GfX.XModel];
    }
}

#endregion

#region TextureRenderer

/// <summary>
/// TextureRenderer
/// </summary>
public class TextureRenderer : Renderer {
    readonly StrideGfxModel GfxModel;
    readonly object Obj;
    readonly Range Level;
    readonly object Texture;
    int FrameDelay;

    public TextureRenderer(IOpenGfx[] gfx, object obj, Range level) {
        GfxModel = (StrideGfxModel)gfx[GfX.XModel];
        Obj = obj;
        Level = level;
        GfxModel.TextureManager.DeleteTexture(obj);
        Texture = GfxModel.TextureManager.CreateTexture(obj, level).tex;
    }

    public override void Start() {
        Log.Info($"MakeTexture");
        Log.Info($"Done");
    }

    public override void Update(float deltaTime) {
        if (Obj is not ITextureFrames obj || GfxModel == null || !obj.HasFrames) return;
        FrameDelay += (int)deltaTime;
        if (FrameDelay <= obj.Fps || !obj.DecodeFrame()) return;
        FrameDelay = 0; // reset delay between frames
        GfxModel.TextureManager.ReloadTexture(obj, Level);
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