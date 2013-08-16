using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Henchmen.Lua.Tests
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
			RunTestScript( "MathChunk.lua", 42 );
		}

		[TestMethod]
		public void Concat()
		{
			RunTestScript( "Concat.lua", new String( "abcabbcab-LOMG-bc" ) );
		}

		[TestMethod]
		public void SimpleCall()
		{
			RunTestScript( "Call.lua", 42 );
		}

		[TestMethod]
		public void TwoSimpleCalls()
		{
			var thread = new Thread();
			var func = Helpers.LoadFunc( "Thread/Call.lua", new Table() );

			thread.Call( func, 0, 1 );

			var stk = thread.Stack;

			Assert.AreEqual( 1, stk.Top );
			Assert.AreEqual( 42, stk[1] );

			stk.Pop();
			Assert.AreEqual( 0, stk.Top );

			thread.Call( func, 0, 1 );

			Assert.AreEqual( 1, stk.Top );
			Assert.AreEqual( 42, stk[1] );
		}

		[TestMethod]
		public void TailCall()
		{
			RunTestScript( "TailCall.lua", 42 );
		}

		[TestMethod]
		public void Vararg()
		{
			RunTestScript( "Vararg.lua", 42, true, false );
		}

		[TestMethod]
		public void SimpleUpValue()
		{
			RunTestScript( "SimpleUpValue.lua", 42 );
		}

		[TestMethod]
		public void ClosedUpValue()
		{
			RunTestScript( "ClosedUpValue.lua", 42 );
		}

		[TestMethod]
		public void ClosedUpValue2()
		{
			RunTestScript( "ClosedUpValue2.lua", 42 );
		}

		[TestMethod]
		public void ClosedUpValue3()
		{
			RunTestScript( "ClosedUpValue3.lua", 42 );
		}

		[TestMethod]
		public void SimpleLoop()
		{
			RunTestScript( "SimpleLoop.lua", 42 );
		}

		[TestMethod]
		public void SimpleLoop2()
		{
			RunTestScript( "SimpleLoop2.lua", 6 );
		}

		[TestMethod]
		public void SimpleLoop3()
		{
			RunTestScript( "SimpleLoop3.lua", 4 );
		}

		[TestMethod]
		public void SimpleLoop4()
		{
			RunTestScript( "SimpleLoop4.lua", 15 );
		}

		[TestMethod]
		public void ForPairs()
		{
			var globals = new Table();
			BaseLib.SetBaseMethods( globals );

			RunTestScriptWithGlobals( "ForPairs.lua", globals, true );
		}

		[TestMethod]
		public void If()
		{
			RunTestScript( "If.lua", 42 );
		}

		[TestMethod]
		public void If2()
		{
			RunTestScript( "If2.lua", 42 );
		}

		[TestMethod]
		public void If3()
		{
			RunTestScript( "If3.lua", 42 );
		}

		[TestMethod]
		public void If4()
		{
			RunTestScript( "If4.lua", 42 );
		}

		[TestMethod]
		public void StrLen()
		{
			RunTestScript( "StrLen.lua", 42 );
		}

		[TestMethod]
		public void TableLen()
		{
			var globals = new Table();
			RunTestScriptWithGlobals( "TableLen.lua", globals, 42 );

			var tableObj = globals["t"];

			Assert.IsTrue( tableObj.ValueType == ValueType.Table );
			var table = (Table)tableObj;

			Assert.AreEqual( 5, table.GetLen() );

			Assert.AreEqual( true, table[1] );
			Assert.AreEqual( false, table[2] );
			Assert.AreEqual( true, table[3] );
			Assert.AreEqual( false, table[4] );
			Assert.AreEqual( true, table[5] );

			Assert.IsFalse( table.ContainsKey( 0 ) );
			Assert.IsFalse( table.ContainsKey( 6 ) );
		}

		[TestMethod]
		public void Callback1()
		{
			var thread = new Thread();

			var globals = new Table();
			var func = Helpers.LoadFunc( "Thread/Callback.lua", globals );

			int numCallbacks = 0;
			globals[new String( "callback" )] = (Callable)(l =>
			{
				Assert.AreEqual( 1, l.Stack.Top );
				Assert.AreEqual( 42, l.Stack[1] );

				numCallbacks++;
				return 0;
			});

			thread.Call( func, 0, 0 );

			Assert.AreEqual( 1, numCallbacks );
		}

		[TestMethod]
		public void Callback2()
		{
			var thread = new Thread();

			var globals = new Table();
			var func = Helpers.LoadFunc( "Thread/Callback.lua", globals );

			var fn = new CallbackFunc();
			globals[new String( "callback" )] = (Callable)fn;

			thread.Call( func, 0, 0 );

			Assert.AreEqual( 1, fn.RunCount );
		}

		private class CallbackFunc : UserFunction
		{
			public int RunCount = 0;

			public override int Execute( Thread l )
			{
				Assert.AreEqual( 1, l.Stack.Top );
				Assert.AreEqual( 42, l.Stack[1] );

				RunCount++;
				return 0;
			}
		}

		[TestMethod]
		public void CallbackReturn()
		{
			var thread = new Thread();

			var globals = new Table();
			var func = Helpers.LoadFunc( "Thread/CallbackReturn.lua", globals );

			int numCallbacks = 0;
			globals[new String( "callback" )] = (Callable)(l =>
			{
				Assert.AreEqual( 3, l.Stack.Top );

				for( int i = 1; i <= 3; i++ )
					l.Stack.Push( l.Stack[i].ToDouble() + i );

				numCallbacks++;
				return 3;
			});

			thread.Call( func, 0, 3 );

			Assert.AreEqual( 2, numCallbacks );

			Assert.AreEqual( 42, thread.Stack[1] );
			Assert.AreEqual( 54, thread.Stack[2] );
			Assert.AreEqual( 66, thread.Stack[3] );
		}

		private static void RunTestScript( string script, params Value[] expectedResults )
		{
			RunTestScriptWithGlobals( script, new Table(), expectedResults );
		}

		private static void RunTestScriptWithGlobals( string script, Table globals, params Value[] expectedResults )
		{
			var thread = new Thread();

			var func = Helpers.LoadFunc( "Thread/" + script, globals );
			Function.Optimize( func );

			thread.Call( func, 0, Thread.CallReturnAll );

			var stk = thread.Stack;

			Assert.AreEqual( expectedResults.Length, stk.Top );
			for( int i = 0; i < expectedResults.Length; i++ )
				Assert.AreEqual( expectedResults[i], stk[i + 1] );
		}
	}
}
