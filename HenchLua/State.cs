using System;

namespace Henchmen.Lua
{
	/// <summary>
	/// A simple extension of Thread, which pairs it with a table of globals.
	/// The globals don't have any special role to play in execution, this is
	/// just a convenient way to keep everything bundled together.
	/// </summary>
    public class State : Thread
    {
		public State()
		{
			globals = new Table();
		}

		public State( Table globals )
		{
			if( globals == null )
				throw new ArgumentNullException( "globals" );

			this.globals = globals;
		}

		private Table globals;
		public Table Globals { get { return globals; } }

		public Function LoadFunction( System.IO.Stream byteCode )
		{
			return Function.Load( byteCode, globals );
		}
    }
}
