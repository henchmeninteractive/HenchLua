using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuaSharp
{
	[Serializable]
	public class LuaException : Exception
	{
		public LuaException() { }
		public LuaException( string message ) : base( message ) { }
		public LuaException( string message, Exception inner ) : base( message, inner ) { }
		protected LuaException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context )
			: base( info, context ) { }
	}

	[Serializable]
	public class InvalidBytecodeException : LuaException
	{
		public InvalidBytecodeException() { }
		public InvalidBytecodeException( string message ) : base( message ) { }
		public InvalidBytecodeException( string message, Exception inner ) : base( message, inner ) { }
		protected InvalidBytecodeException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context )
			: base( info, context ) { }
	}
}
