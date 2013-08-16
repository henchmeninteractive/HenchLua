using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Henchmen.Lua.Tests
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
			var x = ta[Value.Nil];
		}

		[TestMethod,
		ExpectedException( typeof( ArgumentNullException ) )]
		public void ReadNilKeyTest2()
		{
			var ta = new Table();
			var x = ta[new LString()];
		}

		[TestMethod,
		ExpectedException( typeof( ArgumentNullException ) )]
		public void WriteNilKeyTest1()
		{
			var ta = new Table();
			ta[Value.Nil] = 4;
		}

		[TestMethod,
		ExpectedException( typeof( ArgumentNullException ) )]
		public void WriteNilKeyTest2()
		{
			var ta = new Table();
			ta[new LString()] = 4;
		}

		[TestMethod]
		public void AddAndGetBoolKey()
		{
			var ta = new Table();

			Assert.AreEqual( 0, ta.Count() );

			ta[true] = 213;
			Assert.AreEqual( 213, ta[true] );
			
			Assert.AreEqual( 1, ta.Count() );

			Assert.AreEqual( Value.Nil, ta[false] );

			Assert.AreEqual( 213, ta[true] );

			ta[true] = Value.Nil;

			Assert.AreEqual( 0, ta.Count() );

			Assert.AreEqual( Value.Nil, ta[true] );
		}

		[TestMethod]
		public void AddAndGetNumKey()
		{
			var ta = new Table();

			Assert.AreEqual( 0, ta.Count() );

			ta[1] = 213;

			Assert.AreEqual( 213, ta[1] );
			Assert.AreEqual( 1, ta.Count() );
			Assert.AreEqual( Value.Nil, ta[Math.PI] );
			Assert.AreEqual( 213, ta[1] );

			ta[1] = Value.Nil;

			Assert.AreEqual( 0, ta.Count() );
			Assert.AreEqual( Value.Nil, ta[1] );

			ta[Math.PI] = 213;

			Assert.AreEqual( 213, ta[Math.PI] );
			Assert.AreEqual( 1, ta.Count() );
			Assert.AreEqual( Value.Nil, ta[1] );
			Assert.AreEqual( 213, ta[Math.PI] );

			ta[Math.PI] = Value.Nil;

			Assert.AreEqual( 0, ta.Count() );
			Assert.AreEqual( Value.Nil, ta[Math.PI] );
		}

		[TestMethod]
		public void AddAndGetStringKey()
		{
			var sa1 = new LString( "A" );
			var sa2 = new LString( "A" );
			var sb = new LString( "B" );

			var ta = new Table();

			Assert.AreEqual( 0, ta.Count() );

			ta[sa1] = 213;

			Assert.AreEqual( 213, ta[sa1] );
			Assert.AreEqual( 213, ta[sa2] );
			Assert.AreEqual( 1, ta.Count() );
			Assert.AreEqual( Value.Nil, ta[sb] );
			Assert.AreEqual( 213, ta[sa1] );

			ta[sa1] = Value.Nil;

			Assert.AreEqual( 0, ta.Count() );
			Assert.AreEqual( Value.Nil, ta[sa1] );

			ta[sb] = 2354;

			Assert.AreEqual( 2354, ta[sb] );
			Assert.AreEqual( 1, ta.Count() );
			Assert.AreEqual( Value.Nil, ta[sa2] );
			Assert.AreEqual( 2354, ta[sb] );

			ta[sb] = Value.Nil;

			Assert.AreEqual( 0, ta.Count() );
			Assert.AreEqual( Value.Nil, ta[sb] );
		}

		[TestMethod]
		public void ManyStringsTest()
		{
			var ta = new Table();

			const int max = 2048;

			for( int i = 0; i < max; i++ )
				ta[new LString( string.Format( "str:{0}", i ) )] = i;

			for( int i = 0; i < max; i++ )
				Assert.AreEqual( i, ta[new LString( string.Format( "str:{0}", i ) )] );
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
				ta[i] = ta[i].IsNil ? (Value)n : Value.Nil;
			}

			Assert.AreEqual( oldArrCap, ta.ArrayCapacity );
			Assert.AreEqual( 0, ta.NodeCapacity );

			for( int i = 1; i <= 31; i++ )
				ta[i] = Value.Nil;

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
				ta[i * Math.PI] = ta[i * Math.PI].IsNil ? (Value)n : Value.Nil;
			}

			Assert.AreEqual( 0, ta.ArrayCapacity );

			for( int i = 1; i <= 31; i++ )
				ta[i * Math.PI] = Value.Nil;

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
			var strs = new LString[256];
			for( int i = 0; i < strs.Length; i++ )
				strs[i] = new LString( string.Format( "str:{0}", i ) );

			var ta = new Table();

			ta[Math.E] = Math.PI;

			for( int n = 0; n < 4096 * 4; n++ )
			{
				int i = 1 + ((n * 17) % 13) * ((n * 13) % 17);
				ta[i] = ta[i].IsNil ? (Value)n : Value.Nil;

				int j = 1 + (((n - 5) * 17) % 13) * (((n + 3) * 13) % 17);
				
				var jKey = j * Math.PI;
				ta[jKey] = ta[jKey].IsNil ? (Value)n : Value.Nil;
				
				var sjKey = new LString( string.Format( "jKey:{0}", j ) );
				ta[sjKey] = ta[sjKey].IsNil ? (Value)n : Value.Nil;

				if( n % 10 == 0 )
				{
					var s = strs[n % strs.Length];
					ta[s] = ta[s].IsNil ? (Value)n : Value.Nil;
				}
			}

			Assert.AreEqual( Math.PI, ta[Math.E] );
		}

		[TestMethod]
		public void ReuseNumBoxes()
		{
			var ta = new Table( 1, 1 );
			var taEx = ta.Expose();

			ta[1] = 1;
			ta[Math.PI] = 1;

			var arrBox = Helpers.Expose( taEx.array.GetValue( 0 ) ).Val;
			var nodBox = Helpers.Expose( Helpers.Expose( taEx.nodes.GetValue( 0 ) ).Value ).Val;

			Assert.AreEqual( 1, Helpers.Expose( arrBox ).Value );
			Assert.AreEqual( 1, Helpers.Expose( nodBox ).Value );

			for( int i = 2; i < 20; i++ )
			{
				ta[1] = i;
				ta[Math.PI] = i;

				var arrBox2 = Helpers.Expose( taEx.array.GetValue( 0 ) ).Val;
				var nodBox2 = Helpers.Expose( Helpers.Expose( taEx.nodes.GetValue( 0 ) ).Value ).Val;

				Assert.IsTrue( object.ReferenceEquals( arrBox, arrBox2 ) );
				Assert.IsTrue( object.ReferenceEquals( nodBox, nodBox2 ) );

				Assert.AreEqual( i, Helpers.Expose( arrBox ).Value );
				Assert.AreEqual( i, Helpers.Expose( nodBox ).Value );
			}
		}

		[TestMethod]
		public void NextTest_AllIntKeys()
		{
			var t = new Table( 110, 0 );

			for( int i = 0; i < 100; i++ )
			{
				double x = i;
				t[x] = -x;
			}

			int n = 0;
			for( Value v, k = new Value(); t.GetNext( ref k, out v ); )
			{
				Assert.AreEqual( LValueType.Number, k.ValueType );
				Assert.AreEqual( LValueType.Number, v.ValueType );

				Assert.AreEqual( -k.ToDouble(), v.ToDouble() );

				n++;
			}

			Assert.AreEqual( 100, n );

			n = 0;
			foreach( var p in t )
			{
				Assert.AreEqual( LValueType.Number, p.Key.ValueType );
				Assert.AreEqual( LValueType.Number, p.Value.ValueType );

				Assert.AreEqual( -p.Key.ToDouble(), p.Value.ToDouble() );

				n++;
			}

			Assert.AreEqual( 100, n );
		}

		[TestMethod]
		public void NextTest_AllNodeKeys()
		{
			var t = new Table( 0, 130 );

			for( int i = 0; i < 100; i++ )
			{
				double x = i * Math.PI;
				t[x] = -x;
			}

			int n = 0;
			for( Value v, k = new Value(); t.GetNext( ref k, out v ); )
			{
				Assert.AreEqual( LValueType.Number, k.ValueType );
				Assert.AreEqual( LValueType.Number, v.ValueType );

				Assert.AreEqual( -k.ToDouble(), v.ToDouble() );

				n++;
			}

			Assert.AreEqual( 100, n );

			n = 0;
			foreach( var p in t )
			{
				Assert.AreEqual( LValueType.Number, p.Key.ValueType );
				Assert.AreEqual( LValueType.Number, p.Value.ValueType );

				Assert.AreEqual( -p.Key.ToDouble(), p.Value.ToDouble() );

				n++;
			}

			Assert.AreEqual( 100, n );
		}

		[TestMethod]
		public void NextTest_MixedKeys()
		{
			var t = new Table( 100, 64 );

			for( int i = 0; i < 100; i++ )
			{
				double x = i;

				if( i % 2 == 0 )
					x *= Math.PI;

				t[x] = -x;
			}

			int n = 0;
			for( Value v, k = new Value(); t.GetNext( ref k, out v ); )
			{
				Assert.AreEqual( LValueType.Number, k.ValueType );
				Assert.AreEqual( LValueType.Number, v.ValueType );

				Assert.AreEqual( -k.ToDouble(), v.ToDouble() );

				n++;
			}

			Assert.AreEqual( 100, n );

			n = 0;
			foreach( var p in t )
			{
				Assert.AreEqual( LValueType.Number, p.Key.ValueType );
				Assert.AreEqual( LValueType.Number, p.Value.ValueType );

				Assert.AreEqual( -p.Key.ToDouble(), p.Value.ToDouble() );

				n++;
			}

			Assert.AreEqual( 100, n );
		}
	}
}
