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

public class TestGfxApi(ISource source) : IOpenGfxApi<object, object> {
    public ISource Source => source;
    public Task<T> GetAsset<T>(object path) => throw new NotSupportedException();
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

public class TestGfxSprite(ISource source) : IOpenGfxSprite<object, object> {
    public ISource Source => source;
    public SpriteManager<object> SpriteManager => throw new NotImplementedException();
    public ObjectSpriteManager<object, object> ObjectManager => throw new NotImplementedException();
    public Task<T> GetAsset<T>(object path) => throw new NotSupportedException();
    public void PreloadSprite(object path) => throw new NotSupportedException();
    public object CreateObject(object path, object parent = null) => throw new NotImplementedException();
}

public class TestGfxModel(ISource source) : IOpenGfxModel<object, object, object, object> {
    public ISource Source => source;
    public MaterialManager<object, object> MaterialManager => throw new NotImplementedException();
    public ObjectModelManager<object, object, object> ObjectManager => throw new NotImplementedException();
    public ShaderManager<object> ShaderManager => throw new NotImplementedException();
    public TextureManager<object> TextureManager => throw new NotImplementedException();
    public Task<T> GetAsset<T>(object path) => throw new NotSupportedException();
    public void PreloadObject(object path) => throw new NotSupportedException();
    public void PreloadTexture(object path) => throw new NotSupportedException();
    public object CreateObject(object path, object parent = null) => throw new NotImplementedException();
    public object CreateShader(object path, IDictionary<string, bool> args = null) => throw new NotImplementedException();
    public object CreateTexture(object path, Range? level = null) => throw new NotImplementedException();
    public void PostObject(object src, Vector3 position, Vector3 eulerAngles, float? scale, object parent) => throw new NotImplementedException();
}

public class TestGfxLight(ISource source) : IOpenGfxLight<object> {
    public ISource Source => source;
    public Task<T> GetAsset<T>(object path) => throw new NotSupportedException();
    public object CreateLight(float radius, Color color, bool indoors) => throw new NotImplementedException();
}

public class TestGfxTerrain(ISource source) : IOpenGfxTerrain<object, object, object> {
    public ISource Source => source;
    public Task<T> GetAsset<T>(object path) => throw new NotSupportedException();
    public object CreateTerrainData(int offset, float[,] heights, float heightRange, float sampleDistance, GfxTerrainLayer<object>[] layers, float[,,] alphaMap) => throw new NotImplementedException();
    public object CreateTerrain(object data, Vector3 position, object parent = null) => throw new NotImplementedException();
}

public class TestSfx(ISource source) : IOpenSfx {
    public ISource Source => source;
}

/// <summary>
/// TestPlatform
/// </summary>
public class TestPlatform : Platform {
    public static readonly Platform This = new TestPlatform();
    TestPlatform() : base("TT", "Test") {
        GfxFactory = source => [new TestGfxApi(source), new TestGfxSprite(source), new TestGfxSprite(source), new TestGfxModel(source), new TestGfxTerrain(source)];
        SfxFactory = source => [new TestSfx(source)];
    }
}

#endregion

