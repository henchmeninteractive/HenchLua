using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IgorO.ExposedObjectProject;

namespace Henchmen.Lua.Tests
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

		public static Stream LoadByteCode( string name )
		{
			var baseDir = Path.GetDirectoryName( typeof( Helpers ).Assembly.Location );
			var path = Path.Combine( baseDir, "Scripts", name );

			if( string.Equals( Path.GetExtension( path ), ".luab", StringComparison.OrdinalIgnoreCase ) )
				//binary chunk, we're done
				return File.OpenRead( path );

			var state = Native.NewState();
			try
			{
				//silly workaround for encoding issues
				var sourceBytes = File.ReadAllBytes( path );
				
				int idx = 0;
				if( sourceBytes.Length >= 3 && sourceBytes[0] == 0xEF && sourceBytes[1] == 0xBB && sourceBytes[2] == 0xBF )
					idx = 3;

				var stat = Native.Load( state, new MemoryStream( sourceBytes, idx, sourceBytes.Length - idx, false ), Path.GetFileName( path ), "t" );

				if( stat != Native.Result.OK )
					throw new InvalidDataException( Native.ToString( state, 1 ) ?? "Failed loading chunk for test." );

				var ret = new MemoryStream();
				Native.Dump( state, ret );
				ret.Position = 0;

				return ret;
			}
			finally
			{
				Native.Close( state );
			}
		}

		public static Function LoadFunc( string name, Table globals )
		{
			using( var byteCode = LoadByteCode( name ) )
				return Function.Load( byteCode, globals );
		}
	}
}
