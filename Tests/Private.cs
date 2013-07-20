using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IgorO.ExposedObjectProject;

namespace Tests
{
	internal static class Private
	{
		public static dynamic Expose( this object obj )
		{
			var asType = obj as Type;
			if( asType != null )
				return ExposedClass.From( asType );

			return ExposedObject.From( obj );
		}
	}
}
