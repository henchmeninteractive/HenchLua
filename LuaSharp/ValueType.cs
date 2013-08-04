using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuaSharp
{
	/// <summary>
	/// Nicely wraps up both closures and callbacks.
	/// </summary>
	public struct Callable
	{
		public static readonly Callable Nil;

		public Callable( Function value )
		{
			Val = value;
		}

		public static implicit operator Callable( Function value )
		{
			return new Callable() { Val = value };
		}

		internal object Val;

		internal static bool IsCallable( object obj )
		{
			return false;
		}
	}
}
