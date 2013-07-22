using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LuaSharp.Tests
{
	[TestClass]
	public class StringTests
	{
		private static readonly dynamic StrInternals = typeof( String ).Expose();

		[TestMethod]
		public void TestEquals()
		{
			var a = new String( "A" );
			var b = new String( "B" );

			Assert.IsTrue( String.Empty.Equals( String.Empty ) );
			
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

			var a2 = new String( "A" );

			Assert.IsTrue( String.Equals( a, a2 ) );
			Assert.IsTrue( a == a2 );
			Assert.AreEqual( a, a2 );
		}

		[TestMethod]
		public void TestRoundTrip()
		{
			var testText = "A test! A test!";

			Assert.AreEqual( testText, new String( testText ).ToString() );
			Assert.AreEqual( testText, new String( testText, Encoding.Unicode ).ToString( Encoding.Unicode ) );
		}

		[TestMethod]
		public void TestHashCode()
		{
			var a = new String( "A" );
			var b = new String( "B" );

			Assert.AreNotEqual( a.GetHashCode(), b.GetHashCode() );
		}

		[TestMethod]
		public void ManyEquals()
		{
			var strs = new List<String>();
			
			for( int i = 0; i < 4096; i++ )
				strs.Add( new String( string.Format( "s:{0}", i ) ) );

			for( int i = 0; i < strs.Count; i++ )
			{
				var so = strs[i];
				var sn = new String( string.Format( "s:{0}", i ) );

				Assert.AreEqual( so, sn );
				Assert.AreEqual( so.GetHashCode(), sn.GetHashCode() );
			}
		}
	}
}
