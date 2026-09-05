using System.Threading.Tasks;

namespace OpenStk;

/// <summary>
/// ISource
/// </summary>
public interface ISource {
    Task<T> GetAsset<T>(object path, object option = default, bool throwOnError = true);
    //object FindPath<T>(object path);
}

/// <summary>
/// IHaveSource
/// </summary>
public interface IHaveSource {
    ISource Source { get; }
}