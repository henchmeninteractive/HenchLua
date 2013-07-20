using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using LuaSharp;

namespace Tests
{
	[TestClass]
	public class TableTests
	{
		private static readonly dynamic TableInternals = typeof( Table ).Expose();

		[TestMethod]
		public void TestCtor()
		{
			var ta = new Table();
			var taEx = ta.Expose();

			Assert.AreEqual( TableInternals.EmptyNodes, taEx.nodes );
			Assert.AreEqual( null, taEx.array );

			var tb = new Table( 8, 10 );
			var tbEx = tb.Expose();

			Assert.IsNotNull( tbEx.array );
			Assert.IsTrue( tbEx.array.Length == 8 );

			Assert.IsNotNull( tbEx.nodes );
			Assert.IsTrue( tbEx.nodes.Length >= 10 );
		}

		[TestMethod]
		public void AddAndGetBoolKey()
		{
			var ta = new Table();

			Assert.AreEqual( 0, ta.Count() );

			var acc = new TableAccessor();
			acc.Key.Set( true );
			acc.Value.Set( 213 );

			acc.RawSet( ta );

			Assert.AreEqual( 1, ta.Count() );

			acc.Value.SetNil();

			acc.RawGet( ta );

			Assert.AreEqual( 213, acc.Value.ToInt32() );

			acc.Clear();

			acc.Key.Set( false );
			acc.RawGet( ta );

			Assert.IsTrue( acc.Value.IsNil );

			acc.Key.Set( true );
			acc.RawGet( ta );

			Assert.AreEqual( 213, acc.Value.ToInt32() );

			acc.Value.SetNil();

			acc.RawSet( ta );

			Assert.AreEqual( 0, ta.Count() );

			acc.RawGet( ta );

			Assert.IsTrue( acc.Value.IsNil );
		}

		[TestMethod]
		public void AddAndGetNumKey()
		{
			var ta = new Table();

			Assert.AreEqual( 0, ta.Count() );

			var acc = new TableAccessor();
			acc.Key.Set( 0.525 );
			acc.Value.Set( 213 );

			acc.RawSet( ta );

			Assert.AreEqual( 1, ta.Count() );

			acc.Value.SetNil();

			acc.RawGet( ta );

			Assert.AreEqual( 213, acc.Value.ToInt32() );

			acc.Clear();

			acc.Key.Set( false );
			acc.RawGet( ta );

			Assert.IsTrue( acc.Value.IsNil );

			acc.Key.Set( 0.525 );
			acc.RawGet( ta );

			Assert.AreEqual( 213, acc.Value.ToInt32() );

			acc.Value.SetNil();

			acc.RawSet( ta );

			Assert.AreEqual( 0, ta.Count() );

			acc.RawGet( ta );

			Assert.IsTrue( acc.Value.IsNil );
		}

		[TestMethod]
		public void AddAndGetStringKey()
		{
			var ta = new Table();
			var str = new LuaSharp.String( "Testing" );

			Assert.AreEqual( 0, ta.Count() );

			var acc = new TableAccessor();
			acc.Key.Set( str );
			acc.Value.Set( 213 );

			acc.RawSet( ta );

			Assert.AreEqual( 1, ta.Count() );

			acc.Value.SetNil();

			acc.RawGet( ta );

			Assert.AreEqual( 213, acc.Value.ToInt32() );

			acc.Clear();

			acc.Key.Set( 58 );
			acc.RawGet( ta );

			Assert.IsTrue( acc.Value.IsNil );

			acc.Key.Set( str );
			acc.RawGet( ta );

			Assert.AreEqual( 213, acc.Value.ToInt32() );

			acc.Value.SetNil();

			acc.RawSet( ta );

			Assert.AreEqual( 0, ta.Count() );

			acc.RawGet( ta );

			Assert.IsTrue( acc.Value.IsNil );
		}

		[TestMethod]
		public void TestArrayAccess()
		{
			var ta = new Table();

			Assert.AreEqual( 0, ta.Capacity );

			var acc = new TableAccessor();

			for( int i = 1; i <= 31; i++ )
			{
				acc.Key.Set( i );
				acc.Value.Set( i * 1.255 );
				acc.RawSet( ta );
			}

			Assert.AreEqual( 31, ta.Count() );
			Assert.AreEqual( 0, ta.NodeCapacity );
			Assert.IsTrue( ta.ArrayCapacity > 31 );

			for( int i = 1; i <= 31; i++ )
			{
				acc.Key.Set( i );
				acc.RawGet( ta );

				Assert.AreEqual( i * 1.255, acc.Value.ToDouble() );
			}
		}

		[TestMethod]
		public void TestNodeAccess()
		{
			var ta = new Table();

			Assert.AreEqual( 0, ta.Capacity );

			var acc = new TableAccessor();

			for( int i = 1; i <= 31; i++ )
			{
				acc.Key.Set( i * Math.PI );
				acc.Value.Set( i );
				acc.RawSet( ta );
			}

			Assert.AreEqual( 31, ta.Count() );
			Assert.AreEqual( 0, ta.ArrayCapacity );
			Assert.IsTrue( ta.NodeCapacity > 31 );

			for( int i = 1; i <= 31; i++ )
			{
				acc.Key.Set( i * Math.PI );
				acc.RawGet( ta );

				Assert.AreEqual( (double)i, acc.Value.ToDouble() );
			}
		}

		[TestMethod]
		public void TestArrayRemovals()
		{
			var ta = new Table();

			Assert.AreEqual( 0, ta.Capacity );

			var acc = new TableAccessor();

			for( int i = 1; i <= 31; i++ )
			{
				acc.Key.Set( i );
				acc.Value.Set( i * 1.255 );
				acc.RawSet( ta );
			}

			Assert.AreEqual( 31, ta.Count() );
			Assert.AreEqual( 0, ta.NodeCapacity );
			Assert.IsTrue( ta.ArrayCapacity > 31 );

			int oldArrCap = ta.ArrayCapacity;

			for( int n = 0; n < 1000; n++ )
			{
				int i = (n * 23 + n) % 31 + 1;
				
				acc.Key.Set( i );
				acc.RawGet( ta );

				if( acc.Value.IsNil )
					acc.Value.Set( n );
				else
					acc.Value.SetNil();

				acc.RawSet( ta );
			}

			Assert.AreEqual( oldArrCap, ta.ArrayCapacity );
			Assert.AreEqual( 0, ta.NodeCapacity );

			for( int i = 1; i <= 31; i++ )
			{
				acc.Key.Set( i );
				acc.Value.SetNil();
				acc.RawSet( ta );
			}

			Assert.AreEqual( 0, ta.Count() );
			Assert.AreEqual( oldArrCap, ta.ArrayCapacity );
			Assert.AreEqual( 0, ta.NodeCapacity );

			for( int i = 1; i <= 12; i++ )
			{
				acc.Key.Set( i );
				acc.Value.Set( i );
				acc.RawSet( ta );
			}

			Assert.AreEqual( 12, ta.Count() );
			Assert.AreEqual( oldArrCap, ta.ArrayCapacity );
			Assert.AreEqual( 0, ta.NodeCapacity );
		}

		[TestMethod]
		public void TestNodeRemovals()
		{
			var ta = new Table();

			Assert.AreEqual( 0, ta.Capacity );

			var acc = new TableAccessor();

			for( int i = 1; i <= 31; i++ )
			{
				acc.Key.Set( i * Math.PI );
				acc.Value.Set( i * 1.255 );
				acc.RawSet( ta );
			}

			Assert.AreEqual( 31, ta.Count() );
			Assert.AreEqual( 0, ta.ArrayCapacity );
			Assert.IsTrue( ta.NodeCapacity > 31 );

			int oldNodeCap = ta.NodeCapacity;

			for( int n = 0; n < 1000; n++ )
			{
				int i = (n * 23 + n) % 31 + 1;

				acc.Key.Set( i * Math.PI );
				acc.RawGet( ta );

				if( acc.Value.IsNil )
					acc.Value.Set( n );
				else
					acc.Value.SetNil();

				acc.RawSet( ta );
			}

			//Assert.AreEqual( oldNodeCap, ta.NodeCapacity );
			Assert.AreEqual( 0, ta.ArrayCapacity );

			for( int i = 1; i <= 31; i++ )
			{
				acc.Key.Set( i * Math.PI );
				acc.Value.SetNil();
				acc.RawSet( ta );
			}

			Assert.AreEqual( 0, ta.Count() );
			//Assert.AreEqual( oldNodeCap, ta.NodeCapacity );
			Assert.AreEqual( 0, ta.ArrayCapacity );

			for( int i = 1; i <= 12; i++ )
			{
				acc.Key.Set( i * Math.PI );
				acc.Value.Set( i );
				acc.RawSet( ta );
			}

			Assert.AreEqual( 12, ta.Count() );
			//Assert.AreEqual( oldNodeCap, ta.NodeCapacity );
			Assert.IsTrue( ta.NodeCapacity <= oldNodeCap * 2 );
			Assert.AreEqual( 0, ta.ArrayCapacity );
		}
	}
}
