using OpenStack.Client;
using OpenStack.Gfx;
using OpenStack.Sfx;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Threading.Tasks;

namespace OpenStack;

#region Client

public class TestClientHost : IClientHost {
    public void Dispose() => throw new NotImplementedException();
    public void Run() => throw new NotImplementedException();
}

#endregion

#region Platform

public class TestGfxApi : IOpenGfxApi<object, object> {
    public void AddMeshCollider(object src, object mesh, bool isKinematic, bool isStatic) => throw new NotImplementedException();
    public void AddMeshRenderer(object src, object mesh, object material, bool enabled, bool isStatic) => throw new NotImplementedException();
    public void AddMissingMeshCollidersRecursively(object src, bool isStatic) => throw new NotImplementedException();
    public void Attach(GfxAttach method, object src, params object[] args) => throw new NotImplementedException();
    public object CreateMesh(object mesh) => throw new NotImplementedException();
    public object CreateObject(string name, string tag = null, object parent = null) => throw new NotImplementedException();
    public void SetLayerRecursively(object src, int layer) => throw new NotImplementedException();
    public void Parent(object src, object parent) => throw new NotImplementedException();
    public void Transform(object src, Vector3 position, Quaternion rotation, Vector3 localScale) => throw new NotImplementedException();
    public void Transform(object src, Vector3 position, Matrix4x4 rotation, Vector3 localScale) => throw new NotImplementedException();
    public void SetVisible(object src, bool visible) => throw new NotImplementedException();
    public void Destroy(object src) => throw new NotImplementedException();
}

public class TestGfxSprite : IOpenGfxSprite<object, object> {
    public SpriteManager<object> SpriteManager => throw new NotImplementedException();
    public void PreloadSprite(ISource source, object path) => throw new NotSupportedException();
    public Task<(object spr, object tag)> CreateSprite(ISource source, object path, object parent = null) => throw new NotImplementedException();
}

public class TestGfxModel : IOpenGfxModel<object, object, object, object> {
    public MaterialManager<object, object> MaterialManager => throw new NotImplementedException();
    public ObjectModelManager<object, object, object> ObjectManager => throw new NotImplementedException();
    public ShaderManager<object> ShaderManager => throw new NotImplementedException();
    public TextureManager<object> TextureManager => throw new NotImplementedException();
    public void PreloadObject(ISource source, object path) => throw new NotSupportedException();
    public void PreloadTexture(ISource source, object path) => throw new NotSupportedException();
    public Task<(object obj, object tag)> CreateObject(ISource source, object path, bool isStatic, object parent = null) => throw new NotImplementedException();
    public Task<(object sha, object tag)> CreateShader(ISource source, object path, IDictionary<string, bool> args = null) => throw new NotImplementedException();
    public Task<(object tex, object tag)> CreateTexture(ISource source, object path, Range? level = null) => throw new NotImplementedException();
    public void PostObject(object src, Vector3 position, Vector3 eulerAngles, float? scale, object parent = default) => throw new NotImplementedException();
}

public class TestGfxLight : IOpenGfxLight<object> {
    public object CreateLight(string name, Vector3? position, float radius, Color color, bool indoors, object parent = default) => throw new NotImplementedException();
    public object CreateReflectionProbe(string name, Vector3? position, object parent = null) => throw new NotImplementedException();
}

public class TestGfxTerrain : IOpenGfxTerrain<object, object, object> {
    public object CreateTerrainData(int offset, float[,] heights, float heightRange, float sampleDistance, GfxTerrainLayer<object>[] layers, float[,,] alphaMap) => throw new NotImplementedException();
    public object CreateTerrain(string name, Vector3? position, object data, object parent = null) => throw new NotImplementedException();
}

public class TestSfx : IOpenSfx {
}

/// <summary>
/// TestPlatform
/// </summary>
public class TestPlatform : Platform {
    public static Dictionary<Type, Func<object, bool, object, object>> BuildersByType = [];
    public static readonly Platform This = new TestPlatform();
    TestPlatform() : base("TT", "Test") {
        GfxFactory = () => [new TestGfxApi(), new TestGfxSprite(), new TestGfxSprite(), new TestGfxModel(), new TestGfxTerrain()];
        SfxFactory = () => [new TestSfx()];
    }
}

#endregion
