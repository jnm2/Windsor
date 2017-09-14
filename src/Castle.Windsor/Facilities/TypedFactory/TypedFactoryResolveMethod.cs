namespace Castle.Facilities.TypedFactory
{
	using System;
	using System.Diagnostics;
	using System.Reflection;
	using System.Text;

	using Castle.Core;

	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class TypedFactoryResolveMethod
	{
		public MethodInfo Method { get; }

		public TypedFactoryResolveMethod(MethodInfo method)
		{
			Method = method ?? throw new ArgumentNullException(nameof(method));
		}

		public Type ComponentType => Method.ReturnType; // Coupled to DefaultDelegateComponentSelector.GetComponentType

		private DependencyModelCollection dependencies;
		public DependencyModelCollection Dependencies
		{
			get
			{
				if (dependencies == null)
				{
					dependencies = new DependencyModelCollection();

					foreach (var methodParameter in Method.GetParameters())
					{
						// Coupled; TODO: actually build it the way the interceptor would?
						dependencies.Add(new DependencyModel(
							methodParameter.Name,
							methodParameter.ParameterType,
							methodParameter.IsOptional,
							methodParameter.HasDefaultValue,
							methodParameter.DefaultValue));
					}
				}
				return dependencies;
			}
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.Append('(');

			var isFirst = true;
			foreach (var dependency in Dependencies)
			{
				if (isFirst) isFirst = false; else sb.Append(", ");
				sb.Append(dependency.TargetItemType.Name);
				if (dependency.DependencyKey != null)
					sb.Append(' ').Append(dependency.DependencyKey);
			}

			sb.Append(") -> ").Append(ComponentType.Name);
			sb.Append(" via ").Append(Method.DeclaringType.Name).Append('.').Append(Method.Name);

			return sb.ToString();
		}
	}
}