// Copyright 2004-2017 Castle Project - http://www.castleproject.org/
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace CastleTests.Diagnostics
{
	using System;
	using System.Linq;
	using System.Reflection;

	using Castle.Facilities.TypedFactory;
	using Castle.MicroKernel.Registration;
	using Castle.Windsor;
	using Castle.Windsor.Diagnostics;

	using NUnit.Framework;
	using NUnit.Framework.Constraints;

	[TestFixture]
	public class ConfigurationValidationExtensionsTestCase
	{
		public sealed class Dependency_on_service_with_unknown_arg : ContainerFixture
		{
			private sealed class ServiceWithUnknownArg
			{
				public ServiceWithUnknownArg(int arg)
				{
				}
			}

			[Test]
			public void Should_be_invalid()
			{
				Assert.That(Container, HasInvalidComponent<ServiceWithUnknownArg>());
			}
		}

		public sealed class Dependency_on_service_with_unknown_arg_with_explicit_factory : ContainerFixture
		{
			private sealed class ServiceWithUnknownArg
			{
				public ServiceWithUnknownArg(int arg)
				{
				}
			}

			public Dependency_on_service_with_unknown_arg_with_explicit_factory()
			{
				Container.Register(Component.For<Func<int, ServiceWithUnknownArg>>().AsFactory());
			}

			[Test]
			public void Should_be_valid()
			{
				Assert.That(Container, HasValidComponent<ServiceWithUnknownArg>());
			}
		}

		public sealed class Dependency_on_factory_of_service_with_unknown_arg_implicit : ContainerFixture
		{
			private sealed class ServiceWithUnknownArg
			{
				public ServiceWithUnknownArg(int arg)
				{
				}
			}

			private sealed class ConsumingService
			{
				public ConsumingService(Func<int, ServiceWithUnknownArg> factory) { }
			}

			[Test]
			public void Service_with_unknown_arg_should_be_valid()
			{
				Assert.That(Container, HasValidComponent<ServiceWithUnknownArg>());
			}

			[Test]
			public void Consuming_service_should_be_valid()
			{
				Assert.That(Container, HasValidComponent<ConsumingService>());
			}
		}

		public sealed class Dependency_on_factory_of_service_with_unknown_arg_explicit : ContainerFixture
		{
			private sealed class ServiceWithUnknownArg
			{
				public ServiceWithUnknownArg(int arg)
				{
				}
			}

			private sealed class ConsumingService
			{
				public ConsumingService(Func<int, ServiceWithUnknownArg> factory)
				{
				}
			}

			public Dependency_on_factory_of_service_with_unknown_arg_explicit()
			{
				Container.Register(Component.For<Func<int, ServiceWithUnknownArg>>().AsFactory());
			}

			[Test]
			public void Service_with_unknown_arg_should_be_valid()
			{
				Assert.That(Container, HasValidComponent<ServiceWithUnknownArg>());
			}

			[Test]
			public void Consuming_service_should_be_valid()
			{
				Assert.That(Container, HasValidComponent<ConsumingService>());
			}
		}

		public sealed class Dependency_on_factory_of_service_missing_unknown_arg_with_implicit_factory : ContainerFixture
		{
			private sealed class ServiceWithUnknownArg
			{
				public ServiceWithUnknownArg(int arg)
				{
				}
			}

			private sealed class ConsumingService
			{
				public ConsumingService(Func<ServiceWithUnknownArg> factory) { }
			}

			[Test]
			public void Service_with_unknown_arg_should_be_invalid()
			{
				Assert.That(Container, HasInvalidComponent<ServiceWithUnknownArg>());
			}

			[Test]
			public void Consuming_service_with_improper_factory_should_be_invalid()
			{
				Assert.That(Container, HasInvalidComponent<ConsumingService>());
			}
		}

		public sealed class Dependency_on_factory_of_service_missing_unknown_arg_with_explicit_factory : ContainerFixture
		{
			private sealed class ServiceWithUnknownArg
			{
				public ServiceWithUnknownArg(int arg)
				{
				}
			}

			private sealed class ConsumingService
			{
				public ConsumingService(Func<ServiceWithUnknownArg> factory)
				{
				}
			}

			public Dependency_on_factory_of_service_missing_unknown_arg_with_explicit_factory()
			{
				Container.Register(Component.For<Func<int, ServiceWithUnknownArg>>().AsFactory());
			}

			[Test]
			public void Service_with_unknown_arg_should_be_valid()
			{
				Assert.That(Container, HasValidComponent<ServiceWithUnknownArg>());
			}

			[Test]
			public void Consuming_service_with_improper_factory_should_be_invalid()
			{
				Assert.That(Container, HasInvalidComponent<ConsumingService>());
			}
		}

		public abstract class ContainerFixture : IDisposable
		{
			private readonly IWindsorContainer container;

			protected IWindsorContainer Container => container;

			protected ContainerFixture()
			{
				container = new WindsorContainer();
				container.AddFacility<TypedFactoryFacility>();

				foreach (var nestedType in GetType().GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
					container.Register(Component.For(nestedType));
			}

			protected Constraint HasInvalidComponent<TComponent>() => new ComponentValidityConstraint(typeof(TComponent), shouldBeAbleToResolve: false);
			protected Constraint HasValidComponent<TComponent>() => new ComponentValidityConstraint(typeof(TComponent), shouldBeAbleToResolve: true);

			private sealed class ComponentValidityConstraint : Constraint
			{
				private readonly Type componentType;
				private readonly bool shouldBeAbleToResolve;

				public ComponentValidityConstraint(Type componentType, bool shouldBeAbleToResolve)
				{
					this.componentType = componentType;
					this.shouldBeAbleToResolve = shouldBeAbleToResolve;
				}

				public override ConstraintResult ApplyTo<TActual>(TActual actual)
				{
					if (!(actual is IWindsorContainer container))
						throw new ArgumentException("Expected an IWindsorContainer", nameof(actual));

					var unresolvables = container.GetUnresolvableDependencies();

					var canActuallyResolve = unresolvables.Any(_ =>
					{
						var handlerSupportsComponent = _.handler.Supports(componentType);
						var handlerHasMissingDependencies = _.handler.MissingDependencies.Any();
						return handlerSupportsComponent && !handlerHasMissingDependencies;
					});

					var isSuccess = shouldBeAbleToResolve == canActuallyResolve;

					return new ConstraintResult(this, actual, isSuccess);
				}
			}

			public void Dispose()
			{
				container.Dispose();
			}
		}
	}
}