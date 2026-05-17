using OpenStack.Sfx;
using System;
using System.Threading.Tasks;

namespace OpenStack;

#region Platform

/// <summary>
/// SystemAudioBuilder
/// </summary>
public class SystemAudioBuilder : AudioBuilderBase<object> {
    public override object CreateAudio(ISource source, object path) => throw new NotImplementedException();
    public override void DeleteAudio(ISource source, object audio) => throw new NotImplementedException();
}

/// <summary>
/// SystemSfx
/// </summary>
public class SystemSfx : IOpenSfx<object> {
    readonly AudioManager<object> _audioManager = new(new SystemAudioBuilder());
    public AudioManager<object> AudioManager => _audioManager;
    public async Task<(object aud, object tag)> CreateAudio(ISource source, object path) => (_audioManager.CreateAudio(source, path), null);
}

#endregion
