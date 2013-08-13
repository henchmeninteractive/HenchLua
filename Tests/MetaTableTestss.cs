using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Henchmen.Lua.Tests
{
	[TestClass]
	public class MetaTableTestss
	{
		[TestMethod]
		public void SetMetaTable()
		{
			RunTestScript( "SetMetaTable.lua", true );
		}

		[TestMethod]
		public void Index()
		{
			RunTestScript( "Index.lua", 42 );
		}

		[TestMethod]
		public void Index2()
		{
			RunTestScript( "Index2.lua", 42 );
		}

		private static void RunTestScript( string script, params Value[] expectedResults )
		{
			var thread = new Thread();

			var globals = new Table();
			BaseLib.SetBaseMethods( globals );

			var func = Helpers.LoadFunc( "MetaTable/" + script, globals );
			Function.Optimize( func );

			thread.Call( func, 0, Thread.CallReturnAll );

			var stk = thread.Stack;

			Assert.AreEqual( expectedResults.Length, stk.Top );
			for( int i = 0; i < expectedResults.Length; i++ )
				Assert.AreEqual( expectedResults[i], stk[i + 1] );
		}
	}
}
