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
			Assert.AreEqual( 0, thread.StackTop );
		}

		[TestMethod]
		public void MathChunk()
		{
			RunTestScript( "MathChunk.lua", 42 );
		}

		[TestMethod]
		public void MathChunk2()
		{
			RunTestScript( "MathChunk2.lua", 42 );
		}

		[TestMethod]
		public void MathChunk3()
		{
			RunTestScript( "MathChunk3.lua", 42 );
		}

		[TestMethod]
		public void Concat()
		{
			RunTestScript( "Concat.lua", new LString( "abcabbcab-LOMG-bc" ) );
		}

		[TestMethod]
		public void Concat2()
		{
			RunTestScript( "Concat2.lua", new LString( "abc42" ) );
		}

		[TestMethod]
		public void NumFormat()
		{
			RunTestScript( "NumFormat.lua", 42 );
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

			Assert.AreEqual( 1, thread.StackTop );
			Assert.AreEqual( 42, thread[1] );

			thread.Pop();
			Assert.AreEqual( 0, thread.StackTop );

			thread.Call( func, 0, 1 );

			Assert.AreEqual( 1, thread.StackTop );
			Assert.AreEqual( 42, thread[1] );
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
		public void ClosedUpValue4()
		{
			RunTestScript( "ClosedUpValue4.lua", 42 );
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
			Libs.BaseLib.SetBaseMethods( globals );

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
		public void If5()
		{
			RunTestScript( "If5.lua", 42 );
		}

		[TestMethod]
		public void If6()
		{
			RunTestScript( "If6.lua", 42 );
		}

		[TestMethod]
		public void Eq()
		{
			RunTestScript( "Eq.lua", 42 );
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

			Assert.IsTrue( tableObj.ValueType == LValueType.Table );
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
			globals[new LString( "callback" )] = (Callable)(l =>
			{
				Assert.AreEqual( 1, l.StackTop );
				Assert.AreEqual( 42, l[1] );

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
			globals[new LString( "callback" )] = (Callable)fn;

			thread.Call( func, 0, 0 );

			Assert.AreEqual( 1, fn.RunCount );
		}

		private class CallbackFunc : UserFunction
		{
			public int RunCount = 0;

			public override int Execute( Thread l )
			{
				Assert.AreEqual( 1, l.StackTop );
				Assert.AreEqual( 42, l[1] );

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
			globals[new LString( "callback" )] = (Callable)(l =>
			{
				Assert.AreEqual( 3, l.StackTop );

				for( int i = 1; i <= 3; i++ )
					l.Push( l[i].ToDouble() + i );

				numCallbacks++;
				return 3;
			});

			thread.Call( func, 0, 3 );

			Assert.AreEqual( 2, numCallbacks );

			Assert.AreEqual( 42, thread[1] );
			Assert.AreEqual( 54, thread[2] );
			Assert.AreEqual( 66, thread[3] );
		}

		[TestMethod]
		public void DeepOpSelf()
		{
			var globals = new Table();
			Libs.BaseLib.SetBaseMethods( globals );

			globals["g__index"] = (Callable)(l =>
			{
				var t = (Table)l[1];
				var k = (LString)l[2];

				var kstr = k.ToString();
				kstr += "_";

				k = new LString( kstr );

				return l.SetStack( t[k] );
			});

			RunTestScriptWithGlobals( "DeepOpSelf.lua", globals, 42 );
		}

		private static void RunTestScript( string script, params Value[] expectedResults )
		{
			RunTestScriptWithGlobals( script, new Table(), expectedResults );
		}

		private static void RunTestScriptWithGlobals( string script, Table globals, params Value[] expectedResults )
		{
			globals["assertEqual"] = (Callable)(l => { Assert.AreEqual( l[1], l[2] ); return 0; });

			var thread = new Thread();

			var func = Helpers.LoadFunc( "Thread/" + script, globals );
			Function.Optimize( func );

			thread.Call( func, 0, Thread.CallReturnAll );

			Assert.AreEqual( expectedResults.Length, thread.StackTop );
			for( int i = 0; i < expectedResults.Length; i++ )
				Assert.AreEqual( expectedResults[i], thread[i + 1] );
		}
	}
}
