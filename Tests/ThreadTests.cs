using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LuaSharp.Tests
{
	[TestClass]
	public class ThreadTests
	{
		[TestMethod]
		public void Ctor()
		{
			var thread = new Thread();
			Assert.AreEqual( 0, thread.Stack.Top );
		}

		[TestMethod]
		public void MathChunk()
		{
			var thread = new Thread();
			var func = Helpers.LoadFunc( "MathChunk.lua", new Table() );

			thread.Call( func, 0, 1 );

			var stk = thread.Stack;

			Assert.AreEqual( 1, stk.Top );
			Assert.AreEqual( 42, stk[-1] );
		}

		[TestMethod]
		public void SimpleCall()
		{
			var thread = new Thread();
			var func = Helpers.LoadFunc( "Call.lua", new Table() );

			thread.Call( func, 0, 1 );

			var stk = thread.Stack;

			Assert.AreEqual( 1, stk.Top );
			Assert.AreEqual( 42, stk[-1] );
		}
	}
}
