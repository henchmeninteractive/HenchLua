using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IgorO.ExposedObjectProject;

namespace LuaSharp.Tests
{
	internal static class Helpers
	{
		public static dynamic Expose( this object obj )
		{
			var asType = obj as Type;
			if( asType != null )
				return ExposedClass.From( asType );

			return ExposedObject.From( obj );
		}

		public static Stream LoadScript( string name )
		{
			var baseDir = Path.GetDirectoryName( typeof( Helpers ).Assembly.Location );
			var path = Path.Combine( baseDir, "Scripts", name );

			return File.OpenRead( path );
		}
	}
}
