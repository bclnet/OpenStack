using System.Collections.Generic;

namespace OpenStack.Graphics
{
    /// <summary>
    /// IModelInfo
    /// </summary>
    public interface IModelInfo
    {
        IDictionary<string, object> Data { get; }
    }
}