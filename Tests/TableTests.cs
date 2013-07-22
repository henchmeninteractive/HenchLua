using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using LuaSharp;
using LuaStr = LuaSharp.String;

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

		[TestMethod,
		ExpectedException( typeof( ArgumentNullException ) )]
		public void ReadNilKeyTest1()
		{
			var ta = new Table();
			var x = ta[null];
		}

		[TestMethod,
		ExpectedException( typeof( ArgumentNullException ) )]
		public void ReadNilKeyTest2()
		{
			var ta = new Table();
			var x = ta[new LuaStr()];
		}

		[TestMethod,
		ExpectedException( typeof( ArgumentNullException ) )]
		public void WriteNilKeyTest1()
		{
			var ta = new Table();
			ta[null] = 4;
		}

		[TestMethod,
		ExpectedException( typeof( ArgumentNullException ) )]
		public void WriteNilKeyTest2()
		{
			var ta = new Table();
			ta[new LuaStr()] = 4;
		}

		[TestMethod]
		public void AddAndGetBoolKey()
		{
			var ta = new Table();

			Assert.AreEqual( 0, ta.Count() );

			ta[true] = 213;
			Assert.AreEqual( 213, ta[true] );
			
			Assert.AreEqual( 1, ta.Count() );

			Assert.AreEqual( null, ta[false] );

			Assert.AreEqual( 213, ta[true] );

			ta[true] = null;

			Assert.AreEqual( 0, ta.Count() );

			Assert.AreEqual( null, ta[true] );
		}

		[TestMethod]
		public void AddAndGetNumKey()
		{
			var ta = new Table();

			Assert.AreEqual( 0, ta.Count() );

			ta[1] = 213;

			Assert.AreEqual( 213, ta[1] );
			Assert.AreEqual( 1, ta.Count() );
			Assert.AreEqual( null, ta[Math.PI] );
			Assert.AreEqual( 213, ta[1] );

			ta[1] = null;

			Assert.AreEqual( 0, ta.Count() );
			Assert.AreEqual( null, ta[1] );

			ta[Math.PI] = 213;

			Assert.AreEqual( 213, ta[Math.PI] );
			Assert.AreEqual( 1, ta.Count() );
			Assert.AreEqual( null, ta[1] );
			Assert.AreEqual( 213, ta[Math.PI] );

			ta[Math.PI] = null;

			Assert.AreEqual( 0, ta.Count() );
			Assert.AreEqual( null, ta[Math.PI] );
		}

		[TestMethod]
		public void AddAndGetStringKey()
		{
			var sa1 = new LuaStr( "A" );
			var sa2 = new LuaStr( "A" );
			var sb = new LuaStr( "B" );

			var ta = new Table();

			Assert.AreEqual( 0, ta.Count() );

			ta[sa1] = 213;

			Assert.AreEqual( 213, ta[sa1] );
			Assert.AreEqual( 213, ta[sa2] );
			Assert.AreEqual( 1, ta.Count() );
			Assert.AreEqual( null, ta[sb] );
			Assert.AreEqual( 213, ta[sa1] );

			ta[sa1] = null;

			Assert.AreEqual( 0, ta.Count() );
			Assert.AreEqual( null, ta[sa1] );

			ta[sb] = 2354;

			Assert.AreEqual( 2354, ta[sb] );
			Assert.AreEqual( 1, ta.Count() );
			Assert.AreEqual( null, ta[sa2] );
			Assert.AreEqual( 2354, ta[sb] );

			ta[sb] = null;

			Assert.AreEqual( 0, ta.Count() );
			Assert.AreEqual( null, ta[sb] );
		}

		[TestMethod]
		public void ManyStringsTest()
		{
			var ta = new Table();

			const int max = 2048;

			for( int i = 0; i < max; i++ )
				ta[new LuaStr( string.Format( "str:{0}", i ) )] = i;

			for( int i = 0; i < max; i++ )
				Assert.AreEqual( i, ta[new LuaStr( string.Format( "str:{0}", i ) )] );
		}

		[TestMethod]
		public void ArrayAccess()
		{
			var ta = new Table();

			Assert.AreEqual( 0, ta.Capacity );

			for( int i = 1; i <= 31; i++ )
				ta[i] = i * 1.255;

			Assert.AreEqual( 31, ta.Count() );
			Assert.AreEqual( 0, ta.NodeCapacity );
			Assert.IsTrue( ta.ArrayCapacity > 31 );

			for( int i = 1; i <= 31; i++ )
				Assert.AreEqual( i * 1.255, ta[i] );
		}

		[TestMethod]
		public void NodeAccess()
		{
			var ta = new Table();

			Assert.AreEqual( 0, ta.Capacity );

			for( int i = 1; i <= 31; i++ )
				ta[i * Math.PI] = i;

			Assert.AreEqual( 31, ta.Count() );
			Assert.AreEqual( 0, ta.ArrayCapacity );
			Assert.IsTrue( ta.NodeCapacity > 31 );

			for( int i = 1; i <= 31; i++ )
				Assert.AreEqual( (double)i, ta[i * Math.PI] );
		}

		[TestMethod]
		public void ArrayRemovals()
		{
			var ta = new Table();

			Assert.AreEqual( 0, ta.Capacity );

			for( int i = 1; i <= 31; i++ )
				ta[i] = i * 1.255;

			Assert.AreEqual( 31, ta.Count() );
			Assert.AreEqual( 0, ta.NodeCapacity );
			Assert.IsTrue( ta.ArrayCapacity > 31 );

			int oldArrCap = ta.ArrayCapacity;

			for( int n = 0; n < 1000; n++ )
			{
				int i = (n * 23 + n) % 31 + 1;
				ta[i] = ta[i] == null ? (Value)n : null;
			}

			Assert.AreEqual( oldArrCap, ta.ArrayCapacity );
			Assert.AreEqual( 0, ta.NodeCapacity );

			for( int i = 1; i <= 31; i++ )
				ta[i] = null;

			Assert.AreEqual( 0, ta.Count() );
			Assert.AreEqual( oldArrCap, ta.ArrayCapacity );
			Assert.AreEqual( 0, ta.NodeCapacity );

			for( int i = 1; i <= 12; i++ )
				ta[i] = i;

			Assert.AreEqual( 12, ta.Count() );
			Assert.AreEqual( oldArrCap, ta.ArrayCapacity );
			Assert.AreEqual( 0, ta.NodeCapacity );
		}

		[TestMethod]
		public void NodeRemovals()
		{
			var ta = new Table();

			Assert.AreEqual( 0, ta.Capacity );

			for( int i = 1; i <= 31; i++ )
				ta[i * Math.PI] = i * 1.255;

			Assert.AreEqual( 31, ta.Count() );
			Assert.AreEqual( 0, ta.ArrayCapacity );
			Assert.IsTrue( ta.NodeCapacity > 31 );

			int oldNodeCap = ta.NodeCapacity;

			for( int n = 0; n < 1000; n++ )
			{
				int i = (n * 23 + n) % 31 + 1;
				ta[i * Math.PI] = ta[i * Math.PI] == null ? (Value)n : null;
			}

			Assert.AreEqual( 0, ta.ArrayCapacity );

			for( int i = 1; i <= 31; i++ )
				ta[i * Math.PI] = null;

			Assert.AreEqual( 0, ta.Count() );
			Assert.AreEqual( 0, ta.ArrayCapacity );

			for( int i = 1; i <= 12; i++ )
				ta[i * Math.PI] = i;

			Assert.AreEqual( 12, ta.Count() );
			Assert.IsTrue( ta.NodeCapacity <= oldNodeCap * 2 );
			Assert.AreEqual( 0, ta.ArrayCapacity );
		}

		[TestMethod]
		public void ManyKeys()
		{
			var strs = new LuaStr[256];
			for( int i = 0; i < strs.Length; i++ )
				strs[i] = new LuaStr( string.Format( "str:{0}", i ) );

			var ta = new Table();

			ta[Math.E] = Math.PI;

			for( int n = 0; n < 4096 * 4; n++ )
			{
				int i = 1 + ((n * 17) % 13) * ((n * 13) % 17);
				ta[i] = ta[i] == null ? (Value)n : null;

				int j = 1 + (((n - 5) * 17) % 13) * (((n + 3) * 13) % 17);
				
				var jKey = j * Math.PI;
				ta[jKey] = ta[jKey] == null ? (Value)n : null;
				
				var sjKey = new LuaStr( string.Format( "jKey:{0}", j ) );
				ta[sjKey] = ta[sjKey] == null ? (Value)n : null;

				if( n % 10 == 0 )
				{
					var s = strs[n % strs.Length];
					ta[s] = ta[s] == null ? (Value)n : null;
				}
			}

			Assert.AreEqual( Math.PI, ta[Math.E] );
		}
	}
}
