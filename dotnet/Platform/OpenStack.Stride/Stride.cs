using OpenStack.Gfx;
using OpenStack.Gfx.Texture;
using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("OpenStack.GfxTests")]

namespace OpenStack.Stride;

/*
	<ItemGroup>
		<PackageReference Include="Stride.Core" Version="4.2.0.2381" />
		<PackageReference Include="Stride.Engine" Version="4.2.0.2381" />
		<PackageReference Include="Stride.Particles" Version="4.2.0.2381" />
		<PackageReference Include="Stride.UI" Version="4.2.0.2381" />
	</ItemGroup>
*/

// StrideExtensions
public static class StrideExtensions { }

// StrideObjectBuilder : MISSING

// StrideShaderBuilder : MISSING

// StrideTextureBuilder
public class StrideTextureBuilder : TextureBuilderBase<Texture>
{
    Texture _defaultTexture;
    public override Texture DefaultTexture => _defaultTexture ??= CreateDefaultTexture();

    public void Release()
    {
        if (_defaultTexture != null) { /*DeleteTexture(_defaultTexture);*/ _defaultTexture = null; }
    }

    Texture CreateDefaultTexture() => CreateSolidTexture(4, 4, [
        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,

        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,

        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,

        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,
        0f, 0.9f, 0f, 1f,
        0.9f, 0.2f, 0.8f, 1f,
    ]);

    public override Texture CreateTexture(Texture reuse, ITexture source, Range? level = null)
    {
        var (bytes, format, _) = source.Begin("ST");
        try
        {
            return null;
        }
        finally { source.End(); }
    }

    public override Texture CreateSolidTexture(int width, int height, float[] pixels)
    {
        return null;
    }

    public override Texture CreateNormalMap(Texture texture, float strength)
    {
        throw new NotImplementedException();
    }

    public override void DeleteTexture(Texture texture) { }
}

// StrideMaterialBuilder : MISSING

// IStrideGfx3d
public interface IStrideGfx3d : IOpenGfx3dAny<Entity, Material, Texture, int> { }

// StrideGfx3d
public class StrideGfx3d : IStrideGfx3d
{
    readonly ISource _source;
    readonly ITextureManager<Texture> _textureManager;
    readonly MaterialManager<Material, Texture> _materialManager = default;
    readonly Object3dManager<Entity, Material, Texture> _objectManager = default;
    readonly ShaderManager<int> _shaderManager = default;

    public StrideGfx3d(ISource source)
    {
        _source = source;
        _textureManager = new TextureManager<Texture>(source, new StrideTextureBuilder());
        //_materialManager = new MaterialManager<Material, int>(source, _textureManager, new StrideMaterialBuilder(_textureManager));
        //_objectManager = new ObjectManager<Model, Material, int>(source, _materialManager, new StrideObjectBuilder());
        //_shaderManager = new ShaderManager<int>(source, new StrideShaderBuilder());
    }

    public ISource Source => _source;
    public ITextureManager<Texture> TextureManager => _textureManager;
    public IMaterialManager<Material, Texture> MaterialManager => _materialManager;
    public IObject3dManager<Entity, Material, Texture> ObjectManager => _objectManager;
    public IShaderManager<int> ShaderManager => _shaderManager;
    public Texture CreateTexture(object path, Range? level = null) => _textureManager.CreateTexture(path, level).tex;
    public void PreloadTexture(object path) => throw new NotImplementedException();
    public Entity CreateObject(object path) => throw new NotImplementedException();
    public void PreloadObject(object path) => throw new NotImplementedException();
    public int CreateShader(object path, IDictionary<string, bool> args = null) => throw new NotImplementedException();

    public Task<T> LoadFileObject<T>(object path) => _source.LoadFileObject<T>(path);
}

// StrideSfx
public class StrideSfx(ISource source) : SystemSfx(source) { }

// StridePlatform
public class StridePlatform : Platform
{
    public static readonly Platform This = new StridePlatform();
    static Logger Log;
    StridePlatform() : base("ST", "Stride")
    {
        Log = GlobalLogger.GetLogger(typeof(StridePlatform).FullName);
        Log.Debug("Start loading MyTexture");
        GfxFactory = source => false ? null : new StrideGfx3d(source);
        SfxFactory = source => new StrideSfx(source);
        LogFunc = a => Log.Info(a);
        LogFormatFunc = (a, b) => Log.Info(string.Format(a, b));
    }
}