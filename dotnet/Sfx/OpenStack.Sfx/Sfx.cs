using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("OpenStack.SfxTests")]

namespace OpenStack.Sfx;

#region Audio

/// <summary>
///  AudioBuilderBase
/// </summary>
/// <typeparam name="Audio"></typeparam>
public abstract class AudioBuilderBase<Audio> {
    public abstract Audio CreateAudio(ISource source, object path);
    public abstract void DeleteAudio(ISource source, Audio audio);
}

/// <summary>
/// AudioManager
/// </summary>
/// <typeparam name="Audio"></typeparam>
/// <param name="source"></param>
/// <param name="builder"></param>
public class AudioManager<Audio>(AudioBuilderBase<Audio> builder) {
    readonly AudioBuilderBase<Audio> Builder = builder;
    readonly Dictionary<object, (Audio aud, object tag)> CachedAudios = [];
    readonly Dictionary<object, Task<object>> PreloadTasks = [];

    public (Audio aud, object tag) CreateAudio(ISource source, object path) {
        var key = (source, path);
        if (CachedAudios.TryGetValue(key, out var c)) return c;
        // load & cache the audio.
        var tag = LoadAudio(source, path).Result;
        var obj = tag != null ? Builder.CreateAudio(source, tag) : default;
        return CachedAudios[key] = (obj, tag);
    }

    public void PreloadAudio(ISource source, object path) {
        var key = (source, path);
        if (CachedAudios.ContainsKey(key)) return;
        // start loading the texture file asynchronously if we haven't already started.
        if (!PreloadTasks.ContainsKey(key)) PreloadTasks[key] = source.GetAsset<object>(path);
    }

    public void DeleteAudio(ISource source, object path) {
        var key = (source, path);
        if (!CachedAudios.TryGetValue(key, out var c)) return;
        Builder.DeleteAudio(source, c.aud);
        CachedAudios.Remove(key);
    }

    async Task<object> LoadAudio(ISource source, object path) {
        var key = (source, path);
        Log.Assert(!CachedAudios.ContainsKey(key));
        PreloadAudio(source, path);
        var obj = await PreloadTasks[key];
        PreloadTasks.Remove(key);
        return obj;
    }
}

#endregion

#region OpenSfx

/// <summary>
/// IOpenSfx
/// </summary>
public interface IOpenSfx {
}

/// <summary>
/// IOpenSfx
/// </summary>
public interface IOpenSfx<Audio> : IOpenSfx {
    AudioManager<Audio> AudioManager { get; }
    Task<(Audio aud, object tag)> CreateAudio(ISource source, object path);
}

#endregion