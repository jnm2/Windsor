namespace Castle.Facilities.TypedFactory
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;

	using Castle.Core;

	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class TypedFactoryInfo
	{
		public Type FactoryType { get; }
		public bool IsExplicitlyRegistered { get; }
		public IReadOnlyCollection<ComponentModel> Dependents { get; }
		public IReadOnlyCollection<TypedFactoryResolveMethod> ResolveMethods { get; }

		public TypedFactoryInfo(Type factoryType, bool isExplicitlyRegistered, IReadOnlyCollection<ComponentModel> dependents, IReadOnlyCollection<TypedFactoryResolveMethod> resolveMethods)
		{
			FactoryType = factoryType ?? throw new ArgumentNullException(nameof(factoryType));
			IsExplicitlyRegistered = isExplicitlyRegistered;
			Dependents = dependents ?? throw new ArgumentNullException(nameof(dependents));
			ResolveMethods = resolveMethods ?? throw new ArgumentNullException(nameof(resolveMethods));
		}

		public override string ToString()
		{
			return FactoryType.Name + (IsExplicitlyRegistered ? " (explicit)" : " (implicit)");
		}
	}
}
