using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("OpenStack.SfxTests")]

namespace OpenStack.Sfx
{
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
    /// IOpenSfx
    /// </summary>
    public interface IOpenSfx
    {
    }

    /// <summary>
    /// IOpenGfxAny
    /// </summary>
    public interface IOpenSfxAny<Audio> : IOpenSfx
    {
        IAudioManager<Audio> AudioManager { get; }
        Audio CreateAudio(object path);
    }
}