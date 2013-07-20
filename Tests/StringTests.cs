using System;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using LuaSharp;

using LuaStr = LuaSharp.String;

namespace Tests
{
	[TestClass]
	public class StringTests
	{
		private static readonly dynamic StrInternals = typeof( LuaStr ).Expose();

		[TestMethod]
		public void TestEquals()
		{
			var a = new LuaStr( "A" );
			var b = new LuaStr( "B" );

			Assert.IsTrue( LuaStr.Empty.Equals( LuaStr.Empty ) );
			
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
		}

		[TestMethod]
		public void TestRoundTrip()
		{
			var testText = "A test! A test!";

			Assert.AreEqual( testText, new LuaStr( testText ).ToString() );
			Assert.AreEqual( testText, new LuaStr( testText, Encoding.Unicode ).ToString( Encoding.Unicode ) );
		}

		[TestMethod]
		public void TestHashCode()
		{
			var a = new LuaStr( "A" );
			var b = new LuaStr( "B" );

			Assert.AreNotEqual( a.GetHashCode(), b.GetHashCode() );
		}
	}
}
