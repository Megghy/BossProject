using System;
using System.Reflection;

namespace AlternativeCommandExecution.Extensions
{
	public static class TypeExtensions
	{
		public static TReturn CallPrivateStaticMethod<TReturn>(this Type type, string name, params object[] args)
		{
			var method = type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
			return (TReturn)method.Invoke(null, args);
		}
	}
}
