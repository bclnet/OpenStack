using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("OpenStack.SfxTests")]

namespace OpenStack.Sfx;

/// <summary>
/// ISystemSfx
/// </summary>
public interface ISystemSfx : IOpenSfxAny<object> { }