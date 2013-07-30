using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LuaSharp.Tests
{
	[TestClass]
	public class ProtoTests
	{
		[TestMethod]
		public void LoadBinary()
		{
			Function func;

			using( var script = Helpers.LoadByteCode( "test.luab" ) )
				func = Function.Load( script, null );

			Assert.AreNotEqual( null, func );
			Assert.AreEqual( "Closure", func.GetType().Name );
		}
	}
}
