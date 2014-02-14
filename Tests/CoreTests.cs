using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Henchmen.Lua.Tests
{
	[TestClass]
	public class CoreTests
	{
		[TestMethod]
		public void Math()
		{
			RunTestScript( "math.lua" );
		}

		[TestMethod]
		public void Goto()
		{
			RunTestScript( "goto.lua" );
		}

		[TestMethod]
		public void PatternMatching()
		{
			RunTestScript( "pm.lua" );
		}

		private static void RunTestScript( string script, params Value[] expectedResults )
		{
			RunTestScriptWithGlobals( script, new Table(), expectedResults );
		}

		private static void RunTestScriptWithGlobals( string script, Table globals, params Value[] expectedResults )
		{
			Libs.BaseLib.SetBaseMethods( globals );
			Libs.MathLib.SetMathMethods( globals );
			Libs.TableLib.SetTableMethods( globals );
			Libs.StringLib.SetStringMethods( globals );

			globals["print"] = (Callable)(l => 0);
			globals["assert"] = (Callable)(l =>
			{
				if( !l[1].ToBool() )
					Assert.Fail();
				return 0;
			});

			var thread = new Thread();

			var func = Helpers.LoadFunc( "lua-core/" + script, globals );
			Function.Optimize( func );

			thread.Call( func, 0, Thread.CallReturnAll );

			Assert.AreEqual( expectedResults.Length, thread.StackTop );
			for( int i = 0; i < expectedResults.Length; i++ )
				Assert.AreEqual( expectedResults[i], thread[i + 1] );
		}
	}
}
