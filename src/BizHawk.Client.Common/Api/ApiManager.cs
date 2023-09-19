﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public static class ApiManager
	{
		private static IReadOnlyList<(Type ImplType, Type InterfaceType, ConstructorInfo Ctor, Type[] CtorTypes)> _apiTypes
			= Array.Empty<(Type ImplType, Type InterfaceType, ConstructorInfo Ctor, Type[] CtorTypes)>();

		public static void FindApis(IEnumerable<Type> typesToSearch)
		{
			var list = new List<(Type, Type, ConstructorInfo, Type[])>();
			foreach (var implType in typesToSearch
				.Where(t => /*t.IsClass &&*/t.IsSealed)) // small optimisation; api impl. types are all sealed classes
			{
				var interfaceType = implType.GetInterfaces().FirstOrDefault(t => typeof(IExternalApi).IsAssignableFrom(t) && t != typeof(IExternalApi));
				if (interfaceType == null) continue; // if we couldn't determine what it's implementing, then it's not an api impl. type
				var ctor = implType.GetConstructors().Single();
				list.Add((implType, interfaceType, ctor, ctor.GetParameters().Select(pi => pi.ParameterType).ToArray()));
			}
			_apiTypes = _apiTypes.Concat(list).ToArray();
		}

		private static ApiContainer? _container;

		private static ApiContainer? _luaContainer;

		private static ApiContainer Register(
			IEmulatorServiceProvider serviceProvider,
			Action<string> logCallback,
			IMainFormForApi mainForm,
			DisplayManagerBase displayManager,
			InputManager inputManager,
			IMovieSession movieSession,
			IToolManager toolManager,
			Config config,
			IEmulator emulator,
			IGameInfo game)
		{
			if (_apiTypes.Count == 0)
				throw new InvalidOperationException("Attempted to use ApiManager before calling ApiManager.FindApis.");

			var avail = new Dictionary<Type, object>
			{
				[typeof(Action<string>)] = logCallback,
				[typeof(IMainFormForApi)] = mainForm,
				[typeof(DisplayManagerBase)] = displayManager,
				[typeof(InputManager)] = inputManager,
				[typeof(IMovieSession)] = movieSession,
				[typeof(IToolManager)] = toolManager,
				[typeof(Config)] = config,
				[typeof(IEmulator)] = emulator,
				[typeof(IGameInfo)] = game,
			};
			return new ApiContainer(_apiTypes.Where(tuple => ServiceInjector.IsAvailable(serviceProvider, tuple.ImplType))
				.ToDictionary(
					tuple => tuple.InterfaceType,
					tuple =>
					{
						var instance = tuple.Ctor.Invoke(tuple.CtorTypes.Select(t => avail[t]).ToArray());
						if (!ServiceInjector.UpdateServices(serviceProvider, instance, mayCache: true)) throw new Exception("ApiHawk impl. has required service(s) that can't be fulfilled");
						return (IExternalApi) instance;
					}));
		}

		public static IExternalApiProvider Restart(
			IEmulatorServiceProvider serviceProvider,
			IMainFormForApi mainForm,
			DisplayManagerBase displayManager,
			InputManager inputManager,
			IMovieSession movieSession,
			IToolManager toolManager,
			Config config,
			IEmulator emulator,
			IGameInfo game)
		{
			_container?.Dispose();
			_container = Register(serviceProvider, Console.WriteLine, mainForm, displayManager, inputManager, movieSession, toolManager, config, emulator, game);
			return new BasicApiProvider(_container);
		}

		public static ApiContainer RestartLua(
			IEmulatorServiceProvider serviceProvider,
			Action<string> logCallback,
			IMainFormForApi mainForm,
			DisplayManagerBase displayManager,
			InputManager inputManager,
			IMovieSession movieSession,
			IToolManager toolManager,
			Config config,
			IEmulator emulator,
			IGameInfo game)
		{
			_luaContainer?.Dispose();
			_luaContainer = Register(serviceProvider, logCallback, mainForm, displayManager, inputManager, movieSession, toolManager, config, emulator, game);
			((GuiApi) _luaContainer.Gui).EnableLuaAutolockHack = true;
			return _luaContainer;
		}
	}
}