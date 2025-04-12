using System;

namespace OpenStack.Gfx.Model;

#region IModel

/// <summary>
/// IModel
/// </summary>
public interface IModel
{
    T Create<T>(string platform, Func<object, T> func);
}

#endregion
