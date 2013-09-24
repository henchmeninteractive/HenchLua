using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Henchmen.Lua.Tests
{
	[TestClass]
	public class MetaTableTests
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

		[TestMethod]
		public void NewIndex()
		{
			RunTestScript( "NewIndex.lua", 42 );
		}

		[TestMethod]
		public void NewIndex2()
		{
			RunTestScript( "NewIndex2.lua", 42 );
		}

		private static void RunTestScript( string script, params Value[] expectedResults )
		{
			var thread = new Thread();

			var globals = new Table();
			Libs.BaseLib.SetBaseMethods( globals );

			var func = Helpers.LoadFunc( "MetaTable/" + script, globals );
			Function.Optimize( func );

			thread.Call( func, 0, Thread.CallReturnAll );

			Assert.AreEqual( expectedResults.Length, thread.StackTop );
			for( int i = 0; i < expectedResults.Length; i++ )
				Assert.AreEqual( expectedResults[i], thread[i + 1] );
		}
	}
}
