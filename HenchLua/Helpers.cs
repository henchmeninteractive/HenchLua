using System;
using System.Collections.Generic;

using Debug = System.Diagnostics.Debug;

namespace Henchmen.Lua
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

		public static int NumToStr( byte[] buf, int index, double n, int precision = 6 )
		{
			Debug.Assert( precision >= 0 );

			if( double.IsNaN( n ) )
				return SetBufStr( buf, index, "NAN" );

			var len = 0;

			var neg = n < 0;
			if( neg )
			{
				buf[index + len++] = (byte)'-';
				n = -n;
			}

			if( double.IsInfinity( n ) )
				return len + SetBufStr( buf, index + len, "INF" );

			double mant; int exp;
			GetMantAndExp( n, out mant, out exp );

			var p = precision != 0 ? precision : 1;
			if( p > exp && exp >= -4 )
				len += NumToStrDec( buf, index + len, n, precision );
			else
				len += NumToStrSci( buf, index + len, n, mant, exp, precision );

			return len;
		}

		private static int NumToStrDec( byte[] buf, int index, double n, int precision )
		{
			var whole = (long)Math.Truncate( n );
			var fract = n - whole;

			int len = 0;
			do
			{
				int d = (int)(whole % 10);
				whole /= 10;

				buf[index + len++] = (byte)('0' + d);
			}
			while( whole > 0 );

			Array.Reverse( buf, index, len );

			if( fract == 0 || precision == 0 )
				return len;
				
			buf[index + len++] = (byte)'.';

			int p = 0;

			do
			{
				fract *= 10;
				int d = (int)Math.Truncate( fract );
				fract -= d;

				buf[index + len++] = (byte)('0' + d);
			}
			while( ++p < precision && fract > 0 );

			return len;
		}

		private static int NumToStrSci( byte[] buf, int index, double n, double mant, int exp, int precision )
		{
			var len = NumToStrDec( buf, index, mant, precision );
			buf[index + len++] = (byte)'e';

			if( exp >= 0 )
			{
				buf[index + len++] = (byte)'+';
			}
			else
			{
				buf[index + len++] = (byte)'-';
				exp = -exp;
			}

			var expLen = NumToStrDec( buf, index + len, exp, int.MaxValue );
			if( expLen == 1 )
			{
				buf[index + len + 1] = buf[index + len];
				buf[index + len] = (byte)'0';

				expLen++;
			}

			return len + expLen;
		}
		
		private static void GetMantAndExp( double n, out double mant, out int exp )
		{
			Debug.Assert( !double.IsInfinity( n ) && !double.IsNaN( n ) && n >= 0 );

			if( n > 10 )
			{
				for( n /= 10, exp = 1; n > 10; exp++ )
					n /= 10;
			}
			else if( n == 0 )
			{
				exp = 0;
			}
			else
			{
				for( n *= 10, exp = -1; n < 1; exp-- )
					n *= 10;
			}

			mant = n;
		}

		private static int SetBufStr( byte[] buf, int index, string s )
		{
			for( int i = 0; i < s.Length; i++ )
				buf[i] = (byte)s[i];
			return s.Length;
		}
	}
}
