namespace Castle.Facilities.TypedFactory
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	using Castle.Core;
	using Castle.Facilities.TypedFactory.Internal;
	using Castle.MicroKernel;
	using Castle.Windsor;

	// The implementation is coupled in a brittle way to the typed factory resolve logic.
	// Common logic should be refactored into a common class to localize the connascence.
	public static class TypedFactoryAnalysis
	{
		private sealed class TypedFactoryInfoBuilder
		{
			public bool IsExplicitlyRegistered { get; set; }
			public List<ComponentModel> Dependents { get; } = new List<ComponentModel>();
			public List<TypedFactoryResolveMethod> ResolveMethods { get; } = new List<TypedFactoryResolveMethod>();
		}

		public static IReadOnlyCollection<TypedFactoryInfo> GetAllTypedFactories(this IWindsorContainer container)
		{
			var kernel = container.Kernel;
			var buildersByFactoryType = new Dictionary<Type, TypedFactoryInfoBuilder>();

			var registeredHandlers = container.Kernel.GetAssignableHandlers(typeof(object)).ToList();
			foreach (var handler in registeredHandlers)
				VisitModelHierarchy(handler.ComponentModel, fromDependentModel: null);

			void VisitModelHierarchy(ComponentModel model, ComponentModel fromDependentModel)
			{
				var factoryMap = (Dictionary<MethodInfo, FactoryMethod>)model.ExtendedProperties[TypedFactoryFacility.FactoryMapCacheKey];
				if (factoryMap != null)
				{
					var factoryType = model.Services.Single();

					if (!buildersByFactoryType.TryGetValue(factoryType, out var builder))
						buildersByFactoryType.Add(factoryType, builder = new TypedFactoryInfoBuilder());

					if (fromDependentModel == null)
						builder.IsExplicitlyRegistered = true;
					else
						builder.Dependents.Add(fromDependentModel);

					foreach (var (method, kind) in factoryMap)
						if (kind == FactoryMethod.Resolve)
							builder.ResolveMethods.Add(new TypedFactoryResolveMethod(method));
				}

				// Look for implicit factories
				foreach (var dependency in model.Dependencies)
				{
					if (kernel.GetHandlers(dependency.TargetItemType).Length != 0) continue;

					// This is normally lazily registered at resolve time
					// TODO: build it the way the intercepter would?
					var dependencyHandler = ((IKernelInternal)container.Kernel).LoadHandlerByType(dependency.DependencyKey, dependency.TargetItemType, new Arguments());
					if (dependencyHandler != null)
					{
						VisitModelHierarchy(
							kernel.ComponentModelBuilder.BuildModel(
								dependencyHandler.ComponentModel.ComponentName,
								new[] { dependency.TargetItemType },
								dependency.TargetItemType,
								dependencyHandler.ComponentModel.ExtendedProperties),
							fromDependentModel: model);
					}
				}
			}

			var r = new List<TypedFactoryInfo>(buildersByFactoryType.Count);

			foreach (var (factoryType, builder) in buildersByFactoryType)
				r.Add(new TypedFactoryInfo(factoryType, builder.IsExplicitlyRegistered, builder.Dependents, builder.ResolveMethods));

			return r;
		}

		private static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> keyValuePair, out TKey key, out TValue value)
		{
			key = keyValuePair.Key;
			value = keyValuePair.Value;
		}
	}
}
