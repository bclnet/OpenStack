using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static OpenStack.Debug;

[assembly: InternalsVisibleTo("OpenStack.SfxTests")]

namespace OpenStack.Sfx;

#region Audio

/// <summary>
///  AudioBuilderBase
/// </summary>
/// <typeparam name="Audio"></typeparam>
public abstract class AudioBuilderBase<Audio>
{
    public abstract Audio CreateAudio(object path);
    public abstract void DeleteAudio(Audio audio);
}

/// <summary>
/// IAudioManager
/// </summary>
public interface IAudioManager<Audio>
{
    (Audio aud, object tag) CreateAudio(object path);
    void PreloadAudio(object path);
    void DeleteAudio(object path);
}

/// <summary>
/// AudioManager
/// </summary>
/// <typeparam name="Audio"></typeparam>
/// <param name="source"></param>
/// <param name="builder"></param>
public class AudioManager<Audio>(ISource source, AudioBuilderBase<Audio> builder) : IAudioManager<Audio>
{
    readonly ISource Source = source;
    readonly AudioBuilderBase<Audio> Builder = builder;
    readonly Dictionary<object, (Audio aud, object tag)> CachedAudios = [];
    readonly Dictionary<object, Task<object>> PreloadTasks = [];

    public (Audio aud, object tag) CreateAudio(object path)
    {
        if (CachedAudios.TryGetValue(path, out var c)) return c;
        // load & cache the audio.
        var tag = LoadAudio(path).Result;
        var obj = tag != null ? Builder.CreateAudio(tag) : default;
        CachedAudios[path] = (obj, tag);
        return (obj, tag);
    }

    public void PreloadAudio(object path)
    {
        if (CachedAudios.ContainsKey(path)) return;
        // start loading the texture file asynchronously if we haven't already started.
        if (!PreloadTasks.ContainsKey(path)) PreloadTasks[path] = Source.LoadFileObject<object>(path);
    }

    public void DeleteAudio(object path)
    {
        if (!CachedAudios.TryGetValue(path, out var c)) return;
        Builder.DeleteAudio(c.aud);
        CachedAudios.Remove(path);
    }

    async Task<object> LoadAudio(object path)
    {
        Assert(!CachedAudios.ContainsKey(path));
        PreloadAudio(path);
        var obj = await PreloadTasks[path];
        PreloadTasks.Remove(path);
        return obj;
    }
}

#endregion

#region OpenSfx

/// <summary>
/// IOpenSfx
/// </summary>
public interface IOpenSfx
{
}

/// <summary>
/// IOpenSfx
/// </summary>
public interface IOpenSfx<Audio> : IOpenSfx
{
    IAudioManager<Audio> AudioManager { get; }
    Audio CreateAudio(object path);
}

#endregion