using System.Threading.Tasks;

namespace OpenStack;

/// <summary>
/// ISource
/// </summary>
public interface ISource {
    Task<T> GetAsset<T>(object path, object option = default, bool throwOnError = true);
    object FindPath<T>(object path);
}