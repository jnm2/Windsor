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
	using System.Collections.Generic;
	using System.Linq;

	using Castle.MicroKernel.Registration;
	using Castle.Windsor;
	using Castle.Windsor.Diagnostics;

	using NUnit.Framework;

	[TestFixture]
	public static class ConfigurationValidationExtensionsTestCase
	{
		private sealed class ServiceWithUnknownArg
		{
			public ServiceWithUnknownArg(int arg)
			{
			}
		}

		private sealed class ServiceConsumingViaValidFactory
		{
			public ServiceConsumingViaValidFactory(Func<int, ServiceWithUnknownArg> factory)
			{
			}
		}

		private sealed class ServiceConsumingViaInvalidFactory
		{
			public ServiceConsumingViaInvalidFactory(Func<ServiceWithUnknownArg> factory)
			{
			}
		}

		private sealed class ServiceConsumingDirectly
		{
			public ServiceConsumingDirectly(ServiceWithUnknownArg service)
			{
			}
		}

		public static IEnumerable<TestCaseData> TestCases()
		{
			var bools = new[] { false, true };

			foreach (var withValidExplicitFactory in bools)
				foreach (var withInvalidExplicitFactory in bools)
					foreach (var withValidImplicitFactory in bools)
						foreach (var withInvalidImplicitFactory in bools)
							foreach (var withDirectDependent in bools)
							{
								var argumentNames = new List<string>();
								if (withValidExplicitFactory) argumentNames.Add("valid explicit factory");
								if (withInvalidExplicitFactory) argumentNames.Add("invalid explicit factory");
								if (withValidImplicitFactory) argumentNames.Add("valid implicit factory");
								if (withInvalidImplicitFactory) argumentNames.Add("invalid implicit factory");
								if (withDirectDependent) argumentNames.Add("direct dependent");

								yield return new TestCaseData(
									withValidExplicitFactory,
									withInvalidExplicitFactory,
									withValidImplicitFactory,
									withInvalidImplicitFactory,
									withDirectDependent).SetName("{m}(with " + string.Join(", ", argumentNames) + ")");
							}
		}

		[TestCaseSource(nameof(TestCases))]
		public static void Test_serivce_with_runtime_arg(bool withValidExplicitFactory, bool withInvalidExplicitFactory, bool withValidImplicitFactory, bool withInvalidImplicitFactory, bool withDirectDependent)
		{
			using (var container = new WindsorContainer())
			{
				container.Register(Component.For<ServiceWithUnknownArg>());

				if (withValidExplicitFactory)
					container.Register(Component.For<Func<int, ServiceWithUnknownArg>>());

				if (withInvalidExplicitFactory)
					container.Register(Component.For<Func<ServiceWithUnknownArg>>());

				if (withValidImplicitFactory)
					container.Register(Component.For<ServiceConsumingViaValidFactory>());

				if (withInvalidImplicitFactory)
					container.Register(Component.For<ServiceConsumingViaInvalidFactory>());

				if (withDirectDependent)
					container.Register(Component.For<ServiceConsumingDirectly>());


				var validationInfo = container.GetValidationInfo().ToDictionary(_ => _.ServiceType);

				Assert.Multiple(() =>
				{
					var serviceInfo = validationInfo[typeof(ServiceWithUnknownArg)];

					Assert.That(serviceInfo, Has.Property("RuntimeParameters").With.Exactly(1).Items);

					Assert.That(serviceInfo, Has.Property("ReturnedByTypedFactories").With.Exactly(
						(withValidExplicitFactory | withValidImplicitFactory ? 1 : 0)
						+ (withInvalidExplicitFactory | withInvalidImplicitFactory ? 1 : 0)).Items);

					Assert.That(serviceInfo, Has.Property("TypedFactoriesLackingParameters").With.Exactly(
						withInvalidExplicitFactory | withInvalidImplicitFactory ? 1 : 0).Items);

					if (withDirectDependent)
						Assert.That(validationInfo[typeof(ServiceConsumingDirectly)],
							Has.Property("DirectDependenciesLackingRuntimeParameters").With.Exactly(1).Items);
				});
			}
		}
	}
}
