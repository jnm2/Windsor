namespace Castle.Windsor.Diagnostics
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;

	using Castle.Core;
	using Castle.Facilities.TypedFactory;

	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class ServiceValidationInfo
	{
		public Type ServiceType { get; }

		public IReadOnlyCollection<DependencyModel> RuntimeParameters { get; }
		public IReadOnlyCollection<TypedFactoryInfo> ReturnedByTypedFactories { get; }
		public IReadOnlyCollection<TypedFactoryInfo> TypedFactoriesLackingParameters { get; }
		public IReadOnlyCollection<ServiceValidationInfo> DirectDependenciesLackingRuntimeParameters { get; }

		public ServiceValidationInfo(
			Type serviceType,
			IReadOnlyCollection<DependencyModel> runtimeParameters,
			IReadOnlyCollection<TypedFactoryInfo> returnedByTypedFactories,
			IReadOnlyCollection<TypedFactoryInfo> typedFactoriesLackingParameters,
			IReadOnlyCollection<ServiceValidationInfo> directDependenciesLackingRuntimeParameters)
		{
			ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
			RuntimeParameters = runtimeParameters ?? throw new ArgumentNullException(nameof(runtimeParameters));
			ReturnedByTypedFactories = returnedByTypedFactories ?? throw new ArgumentNullException(nameof(returnedByTypedFactories));
			TypedFactoriesLackingParameters = typedFactoriesLackingParameters ?? throw new ArgumentNullException(nameof(typedFactoriesLackingParameters));
			DirectDependenciesLackingRuntimeParameters = directDependenciesLackingRuntimeParameters ?? throw new ArgumentNullException(nameof(directDependenciesLackingRuntimeParameters));
		}

		private string runtimeParameterList;

		private string GetRuntimeParameterList()
		{
			if (runtimeParameterList == null)
			{
				var sb = new StringBuilder();

				foreach (var runtimeParameter in RuntimeParameters)
				{
					sb.AppendLine().Append(" - ").Append(runtimeParameter.TargetItemType.Name);
					if (!string.IsNullOrEmpty(runtimeParameter.DependencyKey)) sb.Append(' ').Append(runtimeParameter.DependencyKey);
				}

				runtimeParameterList = sb.ToString();
			}
			return runtimeParameterList;
		}

		public IEnumerable<string> GetErrorMessages()
		{
			if (ReturnedByTypedFactories.Count == 0 && RuntimeParameters.Count != 0)
			{
				yield return new StringBuilder("In order for ")
					.Append(ServiceType.Name)
					.Append(" to depend on these parameters, either these dependencies must be registered" +
					        " in the container, or a typed factory must be explicitly registered or implicitly used" +
					        " which requires these runtime parameters in order to resolve each instance:")
					.AppendLine(GetRuntimeParameterList())
					.ToString();
			}


			foreach (var typedFactory in TypedFactoriesLackingParameters)
			{
				var sb = new StringBuilder("In order for typed factory ")
					.Append(typedFactory.FactoryType.Name)
					.Append(" to return ")
					.Append(ServiceType.Name)
					.Append(", either these dependencies must be registered in the container, or the typed factory" +
					        " must require these runtime parameters in order to resolve each instance:")
					.AppendLine(GetRuntimeParameterList())
					.AppendLine()
					.Append("The typed factory is ");

				if (typedFactory.IsExplicitlyRegistered)
					sb.Append(typedFactory.Dependents.Count != 0
						? "explicitly registered and also "
						: "explicitly registered.");

				if (typedFactory.Dependents.Count != 0)
				{
					sb.Append("implicitly used as a dependency of:");

					foreach (var dependent in typedFactory.Dependents)
					foreach (var dependentService in dependent.Services)
					{
						sb.AppendLine().Append(" - ").Append(dependentService.Name);
					}
				}

				yield return sb.ToString();
			}


			foreach (var directDependency in DirectDependenciesLackingRuntimeParameters)
			{
				yield return new StringBuilder("In order for ")
					.Append(ServiceType.Name)
					.Append(" to depend on ")
					.Append(directDependency.ServiceType.Name)
					.Append(", either these dependencies must be registered in the container, or ")
					.Append(ServiceType.Name)
					.Append(" must replace the direct dependency with a dependency on a typed factory returning ")
					.Append(directDependency.ServiceType.Name)
					.Append(" which requires these runtime parameters in order to resolve each instance:")
					.Append(GetRuntimeParameterList())
					.ToString();
			}
		}

		public override string ToString()
		{
			return $"{ServiceType.Name}: {GetErrorMessages().Count()} errors";
		}
	}
}