using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HenchLua.Tests
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

		[TestMethod]
		public void RunOptimizer()
		{
			Function func;

			using( var script = Helpers.LoadByteCode( "test.luab" ) )
				func = Function.Load( script, null );

			Function.Optimize( func );

			Assert.AreNotEqual( null, func );
			Assert.AreEqual( "Closure", func.GetType().Name );
		}
	}
}
