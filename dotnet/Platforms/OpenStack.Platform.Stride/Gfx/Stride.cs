using Stride.Engine;
using System;
using System.Collections.Generic;

namespace OpenStack.Gfx.Stride;

#region Extensions

/*
	<ItemGroup>
		<PackageReference Include="Stride.Core" Version="4.2.0.2381" />
		<PackageReference Include="Stride.Engine" Version="4.2.0.2381" />
		<PackageReference Include="Stride.Particles" Version="4.2.0.2381" />
		<PackageReference Include="Stride.UI" Version="4.2.0.2381" />
	</ItemGroup>
*/

// StrideX
public static class StrideX {
    public static Dictionary<Type, Func<object, bool, object, Entity>> BuildersByType = [];

}

#endregion
