using OpenStk.Sfx;
using System;
using System.Threading.Tasks;

namespace OpenStk;

#region Platform

/// <summary>
/// SystemAudioBuilder
/// </summary>
public class SystemAudioBuilder : AudioBuilder<object> {
    public override object Create(ISource source, object path) => throw new NotImplementedException();
    public override void Delete(ISource source, object audio) => throw new NotImplementedException();
}

/// <summary>
/// SystemSfx
/// </summary>
public class SystemSfx : IOpenSfx<object> {
    readonly AudioManager<object> _audioManager = new(new SystemAudioBuilder());
    public AudioManager<object> AudioManager => _audioManager;
    public async Task<(object aud, object tag)> CreateAudio(ISource source, object path) => (_audioManager.Create(source, path), null);
}

#endregion
