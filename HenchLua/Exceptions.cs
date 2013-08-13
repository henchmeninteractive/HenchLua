using System;

namespace Henchmen.Lua
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

	/// <summary>
	/// Attempting to apply an operation to an invalid value.
	/// </summary>
	[Serializable]
	public class InvalidOperandException : LuaException
	{
		public InvalidOperandException() { }
		public InvalidOperandException( string message ) : base( message ) { }
		public InvalidOperandException( string message, Exception inner ) : base( message, inner ) { }
		protected InvalidOperandException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context )
			: base( info, context ) { }
	}
}
