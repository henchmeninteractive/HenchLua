using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LuaSharp.Tests
{
	[TestClass]
	public class ProtoTests
	{
		[TestMethod,
		ExpectedException( typeof( NotImplementedException ) )]
		public void LoadBinary()
		{
			Function func;

			using( var script = Helpers.LoadScript( "test.luab" ) )
				func = Function.Load( script );
		}
	}
}
