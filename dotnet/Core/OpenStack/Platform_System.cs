using OpenStack.Sfx;
using System;

namespace OpenStack;

/// <summary>
/// SystemAudioBuilder
/// </summary>
public class SystemAudioBuilder : AudioBuilderBase<object> {
    public override object CreateAudio(object path) => throw new NotImplementedException();
    public override void DeleteAudio(object audio) => throw new NotImplementedException();
}

#region OpenSfx

/// <summary>
/// SystemSfx
/// </summary>
public class SystemSfx(ISource source) : IOpenSfx<object> {
    readonly ISource _source = source;
    readonly AudioManager<object> _audioManager = new(source, new SystemAudioBuilder());

    public ISource Source => _source;
    public AudioManager<object> AudioManager => _audioManager;
    public object CreateAudio(object path) => _audioManager.CreateAudio(path).aud;
}

#endregion
