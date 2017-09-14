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

namespace Castle.Windsor.Diagnostics
{
	using System.Collections.Generic;
	using System.Linq;

	using Castle.Core;
	using Castle.MicroKernel;

	public static class ConfigurationValidationExtensions
	{
		public static IReadOnlyCollection<(DependencyModel dependency, IHandler handler)> GetUnresolvableDependencies(this IWindsorContainer container)
		{
			var unresolvables = new List<(DependencyModel dependency, IHandler handler)>();
			var allHandlers = container.Kernel.GetAssignableHandlers(typeof(object)).ToList();
			var waitingHandlers = allHandlers.FindAll(handler => handler.CurrentState == HandlerState.WaitingDependency);

			foreach (var waitingHandler in waitingHandlers)
			{
				foreach (var dependency in waitingHandler.MissingDependencies)
				{
					unresolvables.Add((dependency, waitingHandler));
				}
			}

			return unresolvables.AsReadOnly();
		}

		internal static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> keyValuePair, out TKey key, out TValue value)
		{
			key = keyValuePair.Key;
			value = keyValuePair.Value;
		}

	}
}