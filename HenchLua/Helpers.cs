using System;
using System.Collections.Generic;

using Debug = System.Diagnostics.Debug;

namespace Henchmen.Lua
{
	public static partial class Helpers
	{
		public static int CheckOpt( Value value, int? defaultRet, LString[] opts )
		{
			if( opts == null )
				throw new ArgumentNullException( "opts" );
			
			if( value.IsNil )
			{
				if( !defaultRet.HasValue )
					throw new ArgumentNullException( "A required argument was nil." );

				return defaultRet.GetValueOrDefault();
			}

			if( value.ValueType != LValueType.String )
				throw new ArgumentException( "A string argument was expected." );

			for( int i = 0; i < opts.Length; i++ )
			{
				if( value == opts[i] )
					return i;
			}

			throw new ArgumentException( "Invalid option." );
		}

		public static int CheckOpt( Value value, LString defaultArg, LString[] opts )
		{
			if( opts == null )
				throw new ArgumentNullException( "opts" );

			if( value.IsNil )
			{
				if( defaultArg.IsNil )
					throw new ArgumentNullException( "A required argument was nil." );

				value = defaultArg;
			}

			if( value.ValueType != LValueType.String )
				throw new ArgumentException( "A string argument was expected." );

			for( int i = 0; i < opts.Length; i++ )
			{
				if( value == opts[i] )
					return i;
			}

			throw new ArgumentException( "Invalid option." );
		}

		public static int CheckOpt( Value value, LString[] opts )
		{
			return CheckOpt( value, (int?)null, opts );
		}
	}
}
