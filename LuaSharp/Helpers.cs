using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Debug = System.Diagnostics.Debug;

namespace LuaSharp
{
	internal static class Helpers
	{
		public static int CeilLog2( int x )
		{
			Debug.Assert( x > 0 );

			int ret = 0;
			x--;

			while( x >= 256 )
			{
				ret += 8;
				x >>= 8;
			}

			if( x >= 128 )
				ret += 8;
			else if( x >= 64 )
				ret += 7;
			else if( x >= 32 )
				ret += 6;
			else if( x >= 16 )
				ret += 5;
			else if( x >= 8 )
				ret += 4;
			else if( x >= 4 )
				ret += 3;
			else if( x >= 2 )
				ret += 2;
			else if( x == 1 )
				ret += 1;

			return ret;
		}

		public static int FbToInt( int x )
		{
			int e = (x >> 3) & 0x1f;
			return e == 0 ? x : ((x & 7) + 8) << (e - 1);
		}
	}
}
