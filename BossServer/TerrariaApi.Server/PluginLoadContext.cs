using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace TerrariaApi.Server
{
	public class PluginLoadContext : AssemblyLoadContext
	{
		private AssemblyDependencyResolver _resolver;

		public PluginLoadContext(string pluginPath)
		{
			_resolver = new AssemblyDependencyResolver(pluginPath);
		}

		protected override Assembly Load(AssemblyName assemblyName)
		{
			Assembly assembly;
			try
			{
				assembly = AppDomain.CurrentDomain.Load(assemblyName);
				return assembly;
			}
			catch (Exception _)
			{
				// ignored
			}

			if ((assembly = ServerApi.Plugins.FirstOrDefault(p =>
				p.Plugin.GetType().Assembly.GetName().Name == assemblyName.Name)
				?.Plugin.GetType().Assembly) != null)
			{
				return assembly;
			}

			string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
			if (assemblyPath != null)
			{
				return LoadFromAssemblyPath(assemblyPath);
			}

			return null;
		}

		protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
		{
			string libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
			if (libraryPath != null)
			{
				return LoadUnmanagedDllFromPath(libraryPath);
			}

			return IntPtr.Zero;
		}
	}
}
