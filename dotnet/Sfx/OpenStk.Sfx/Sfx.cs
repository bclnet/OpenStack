using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("OpenStack.SfxTests")]

namespace OpenStk.Sfx;

#region Audio

/// <summary>
///  AudioBuilder
/// </summary>
/// <typeparam name="B_Audio"></typeparam>
public abstract class AudioBuilder<B_Audio> {
    public abstract B_Audio Create(ISource source, object path);
    public abstract void Delete(ISource source, B_Audio audio);
}

/// <summary>
/// AudioManager
/// </summary>
/// <typeparam name="Audio"></typeparam>
/// <param name="source"></param>
/// <param name="builder"></param>
public class AudioManager<Audio>(AudioBuilder<Audio> builder) {
    readonly AudioBuilder<Audio> Builder = builder;
    readonly Dictionary<object, (Audio aud, object tag)> Cached = [];
    readonly Dictionary<object, Task<object>> Tasks = [];

    public (Audio aud, object tag) Create(ISource source, object path) {
        var key = (source, path);
        if (Cached.TryGetValue(key, out var c)) return c;
        // load & cache the audio.
        var tag = Load(source, path).Result;
        var obj = tag != null ? Builder.Create(source, tag) : default;
        return Cached[key] = (obj, tag);
    }

    public void Preload(ISource source, object path) {
        var key = (source, path);
        if (Cached.ContainsKey(key)) return;
        // start loading the texture file asynchronously if we haven't already started.
        if (!Tasks.ContainsKey(key)) Tasks[key] = source.GetAsset<object>(path);
    }

    public void Delete(ISource source, object path) {
        var key = (source, path);
        if (!Cached.TryGetValue(key, out var c)) return;
        Builder.Delete(source, c.aud);
        Cached.Remove(key);
    }

    async Task<object> Load(ISource source, object path) {
        var key = (source, path);
        Debug.Assert(!Cached.ContainsKey(key));
        Preload(source, path);
        var obj = await Tasks[key];
        Tasks.Remove(key);
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