using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Henchmen.Lua.Tests
{
	[TestClass]
	public class StringTests
	{
		private static readonly dynamic StrInternals = typeof( LString ).Expose();

		[TestMethod]
		public void TestEquals()
		{
			var a = new LString( "A" );
			var b = new LString( "B" );

			Assert.IsTrue( LString.Empty.Equals( LString.Empty ) );
			
			Assert.IsTrue( a == a );
			Assert.IsFalse( a == b );

			Assert.IsFalse( a != a );
			Assert.IsTrue( a != b );

			var exA = a.Expose();
			var exB = b.Expose();

			byte[] intA = exA.InternalData;
			byte[] intB = exB.InternalData;

			Assert.IsTrue( StrInternals.InternalEquals( intA, intA ) );
			Assert.IsTrue( StrInternals.InternalEquals( intB, intB ) );
			Assert.IsFalse( StrInternals.InternalEquals( intA, intB ) );

			var a2 = new LString( "A" );

			Assert.IsTrue( LString.Equals( a, a2 ) );
			Assert.IsTrue( a == a2 );
			Assert.AreEqual( a, a2 );
		}

		[TestMethod]
		public void TestRoundTrip()
		{
			var testText = "A test! A test!";

			Assert.AreEqual( testText, new LString( testText ).ToString() );
			Assert.AreEqual( testText, new LString( testText, Encoding.Unicode ).ToString( Encoding.Unicode ) );
		}

		[TestMethod]
		public void TestHashCode()
		{
			var a = new LString( "A" );
			var b = new LString( "B" );

			Assert.AreNotEqual( a.GetHashCode(), b.GetHashCode() );
		}

		[TestMethod]
		public void ManyEquals()
		{
			var strs = new List<LString>();
			
			for( int i = 0; i < 4096; i++ )
				strs.Add( new LString( string.Format( "s:{0}", i ) ) );

			for( int i = 0; i < strs.Count; i++ )
			{
				var so = strs[i];
				var sn = new LString( string.Format( "s:{0}", i ) );

				Assert.AreEqual( so, sn );
				Assert.AreEqual( so.GetHashCode(), sn.GetHashCode() );
			}
		}
	}
}
