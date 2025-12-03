using OpenStack.Gfx;
using OpenStack.Sfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace OpenStack;

#region Platform

/// <summary>
/// Gets the platform.
/// </summary>
public abstract class Platform(string id, string name) {
    /// <summary>
    /// Gets the active.
    /// </summary>
    public bool Enabled = true;

    /// <summary>
    /// Gets the platform id.
    /// </summary>
    public readonly string Id = id;

    /// <summary>
    /// Gets the platform name.
    /// </summary>
    public readonly string Name = name;

    /// <summary>
    /// Gets the platform name.
    /// </summary>
    public string DisplayName => Name;

    /// <summary>
    /// Gets the platform tag.
    /// </summary>
    public string Tag;

    /// <summary>
    /// Gets the platforms gfx factory.
    /// </summary>
    public Func<ISource, IOpenGfx[]> GfxFactory = source => null; // throw new Exception("No GfxFactory");

    /// <summary>
    /// Gets the platforms sfx factory.
    /// </summary>
    public Func<ISource, IOpenSfx[]> SfxFactory = source => null; // throw new Exception("No SfxFactory");

    /// <summary>
    /// Gets the platforms assert func.
    /// </summary>
    public Action<bool> AssertFunc = x => System.Diagnostics.Debug.Assert(x);

    /// <summary>
    /// Gets the platforms log func.
    /// </summary>
    public Action<string> LogFunc = a => System.Diagnostics.Debug.Print(a);

    /// <summary>
    /// Activates the platform.
    /// </summary>
    public virtual void Activate() {
        Log.AssertFunc = AssertFunc;
        Log.Func = LogFunc;
    }

    /// <summary>
    /// Deactivates the platform.
    /// </summary>
    public virtual void Deactivate() { }
}

/// <summary>
/// UnknownPlatform
/// </summary>
public class UnknownPlatform : Platform {
    public static readonly Platform This = new UnknownPlatform();
    UnknownPlatform() : base("UK", "Unknown") { }
}

/// <summary>
/// PlatformX
/// </summary>
public static class PlatformX {
    //public static Action Hook;

    /// <summary>
    /// The platform OS.
    /// </summary>
    public enum OS { Unknown, Windows, OSX, Linux, Android }

    /// <summary>
    /// Gets the platform os.
    /// </summary>
    public static readonly OS PlatformOS = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? OS.Windows
        : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? OS.OSX
        : RuntimeInformation.OSDescription.StartsWith("android-") ? OS.Android
        : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? OS.Linux
        : OS.Unknown;
    //: throw new ArgumentOutOfRangeException(nameof(RuntimeInformation.IsOSPlatform), RuntimeInformation.OSDescription);

    /// <summary>
    /// Gets the platform startups.
    /// </summary>
    public static readonly HashSet<Platform> Platforms = [UnknownPlatform.This];

    /// <summary>
    /// Determines if in a test host.
    /// </summary>
    public static readonly bool InTestHost = AppDomain.CurrentDomain.GetAssemblies().Any(x => x.FullName.StartsWith("testhost,"));

    /// <summary>
    /// Gets the application Path
    /// </summary>
    public static readonly string ApplicationPath = AppDomain.CurrentDomain.BaseDirectory;

    /// <summary>
    /// Gets the platform startups.
    /// </summary>
    public static YamlDict Options = new("~/.gamex.yaml");

    /// <summary>
    /// Gets or sets the current platform.
    /// </summary>
    public static Platform Current = Activate(InTestHost ? TestPlatform.This : UnknownPlatform.This);

    /// <summary>
    /// Activates a platform.
    /// </summary>
    public static Platform Activate(Platform platform) {
        //Hook?.Invoke(); Hook = null;
        if (platform == null || !platform.Enabled) platform = UnknownPlatform.This;
        Platforms.Add(platform);
        var current = Current;
        if (current != platform) {
            current?.Deactivate();
            platform?.Activate();
            Current = platform;
        }
        return platform;
    }

    ///// <summary>
    ///// Creates the matcher.
    ///// </summary>
    ///// <param name="searchPattern">The searchPattern.</param>
    ///// <returns></returns>
    //public static Func<string, bool> CreateMatcher(string searchPattern) {
    //    if (string.IsNullOrEmpty(searchPattern)) return x => true;
    //    var wildcardCount = searchPattern.Count(x => x.Equals('*'));
    //    if (wildcardCount <= 0) return x => x.Equals(searchPattern, StringComparison.CurrentCultureIgnoreCase);
    //    else if (wildcardCount == 1) {
    //        var newPattern = searchPattern.Replace("*", "");
    //        if (searchPattern.StartsWith("*")) return x => x.EndsWith(newPattern, StringComparison.CurrentCultureIgnoreCase);
    //        else if (searchPattern.EndsWith("*")) return x => x.StartsWith(newPattern, StringComparison.CurrentCultureIgnoreCase);
    //    }
    //    var regexPattern = $"^{Regex.Escape(searchPattern).Replace("\\*", ".*")}$";
    //    return x => {
    //        try { return Regex.IsMatch(x, regexPattern); }
    //        catch { return false; }
    //    };
    //}

    public static string DecodePath(string path, string rootPath = null) => Util.DecodePath(ApplicationPath, path, rootPath);
}

#endregion

#region Test Platform

public class TestGfxSprite(ISource source) : IOpenGfxSprite {
    readonly ISource _source = source;
    public object Source => _source;
    public Task<T> GetAsset<T>(object path) => throw new NotSupportedException();
    public void PreloadSprite(object path) => throw new NotSupportedException();
    public void PreloadObject(object path) => throw new NotSupportedException();
}

public class TestGfxModel(ISource source) : IOpenGfxModel {
    readonly ISource _source = source;
    public object Source => _source;
    public Task<T> GetAsset<T>(object path) => throw new NotSupportedException();
    public void PreloadTexture(object path) => throw new NotSupportedException();
    public void PreloadObject(object path) => throw new NotSupportedException();
}

public class TestSfx(ISource source) : IOpenSfx {
    readonly ISource _source = source;
    public object Source => _source;
}

/// <summary>
/// TestPlatform
/// </summary>
public class TestPlatform : Platform {
    public static readonly Platform This = new TestPlatform();
    TestPlatform() : base("TT", "Test") {
        GfxFactory = source => [new TestGfxSprite(source), new TestGfxSprite(source), new TestGfxModel(source)];
        SfxFactory = source => [new TestSfx(source)];
    }
}

#endregion
