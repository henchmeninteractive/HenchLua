using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using LuaSharp;

namespace Tests
{
	[TestClass]
	public class ValueTests
	{
		[TestMethod]
		public void Nils()
		{
			var val = new Value();
			Assert.IsTrue( val.IsNil );
			Assert.IsFalse( val.ToBool() );
			Assert.AreEqual( LuaSharp.ValueType.Nil, val.ValueType );

			val.Set( true );
			Assert.IsFalse( val.IsNil );

			val.Set( null );
			Assert.IsTrue( val.IsNil );

			val = new Value( null );
			Assert.IsTrue( val.IsNil );

			val = null;
			Assert.IsTrue( val.IsNil );

			val.Set( true );
			Assert.IsFalse( val.IsNil );

			val.SetNil();
			Assert.IsTrue( val.IsNil );
		}

		[TestMethod]
		public void Bools()
		{
			Value val = true;
			Assert.AreEqual( LuaSharp.ValueType.Bool, val.ValueType );
			Assert.IsTrue( val.ToBool() );
			Assert.IsTrue( (bool)val );

			val.Set( false );
			Assert.IsFalse( val.ToBool() );
			Assert.IsFalse( (bool)val );
		}

		[TestMethod]
		public void Numbers()
		{
			Value val = 4.5;
			Assert.AreEqual( LuaSharp.ValueType.Number, val.ValueType );
			Assert.AreEqual( 4.5, (double)val );
			Assert.AreEqual( 4.5, val.ToDouble() );

			Assert.AreEqual( 4, val.ToInt32() );

			val = uint.MaxValue;
			Assert.AreEqual( uint.MaxValue, (uint)val );
			Assert.AreEqual( uint.MaxValue, val.ToUInt32() );

			val = int.MaxValue;
			Assert.AreEqual( int.MaxValue, (int)val );
			Assert.AreEqual( int.MaxValue, val.ToInt32() );
			
			val = int.MinValue;
			Assert.AreEqual( int.MinValue, val.ToInt32() );

			val.Set( Math.PI );
			Assert.AreEqual( Math.PI, val.ToDouble() );
		}
	}
}
