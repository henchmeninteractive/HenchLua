using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Henchmen.Lua.Tests
{
	[TestClass]
	public class TableLibTests
	{
		[TestMethod]
		public void Insert()
		{
			RunTestScript( "TableInsert.lua", 42 );
		}

		[TestMethod]
		public void Insert2()
		{
			RunTestScript( "TableInsert2.lua", 42 );
		}

		private static void RunTestScript( string script, params Value[] expectedResults )
		{
			RunTestScriptWithGlobals( script, new Table(), expectedResults );
		}

		private static void RunTestScriptWithGlobals( string script, Table globals, params Value[] expectedResults )
		{
			var thread = new Thread();

			Libs.BaseLib.SetBaseMethods( globals );
			Libs.TableLib.SetTableMethods( globals );

			var func = Helpers.LoadFunc( "Libs/" + script, globals );
			Function.Optimize( func );

			thread.Call( func, 0, Thread.CallReturnAll );

			Assert.AreEqual( expectedResults.Length, thread.StackTop );
			for( int i = 0; i < expectedResults.Length; i++ )
				Assert.AreEqual( expectedResults[i], thread[i + 1] );
		}
	}
}
