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

		public static bool StrToNum( byte[] str, int index, int count, out double num )
		{
			int endIndex = index + count;

			SkipSpace( str, ref index, endIndex );

			int sign = ReadSign( str, ref index, endIndex );
			if( sign == 0 )
			{
				num = 0;
				return false;
			}

			int radix;
			byte expChar;

			if( endIndex - index > 2 && str[index] == '0' && Lower( str[index + 1] ) == 'x' )
			{
				index += 2;
				radix = 16;

				expChar = (byte)'p';
			}
			else
			{
				radix = 10;
				expChar = (byte)'e';
			}

			var val = StrToNum( str, ref index, endIndex, radix, true, expChar );
			if( !val.HasValue )
			{
				num = 0;
				return false;
			}

			SkipSpace( str, ref index, endIndex );

			if( index != endIndex )
			{
				num = 0;
				return false;
			}

			num = val.GetValueOrDefault() * sign;
			return true;
		}

		public static bool StrToInt( byte[] str, int index, int count, out double num, int radix = 10 )
		{
			int endIndex = index + count;

			SkipSpace( str, ref index, endIndex );

			int sign = ReadSign( str, ref index, endIndex );
			if( sign == 0 )
			{
				num = 0;
				return false;
			}
			
			var val = StrToNum( str, ref index, endIndex, radix, false, null );
			if( !val.HasValue )
			{
				num = 0;
				return false;
			}

			SkipSpace( str, ref index, endIndex );

			if( index != endIndex )
			{
				num = 0;
				return false;
			}

			num = val.GetValueOrDefault() * sign;
			return true;
		}

		private static double? StrToNum( byte[] str, ref int index, int endIndex,
			int radix, bool allowDecimal, byte? expCharLwr )
		{
			int wholeLen;
			var wholePart = ReadDigits( str, ref index, endIndex, radix, out wholeLen );
			if( wholePart < 0 )
				return null;

			int decLen = 0;
			double decPart = -1;

			if( allowDecimal && index < endIndex && str[index] == '.' )
			{
				index++;

				decPart = ReadDigits( str, ref index, endIndex, radix, out decLen );

				if( decPart < 0 )
					return null;
			}

			if( wholeLen == 0 && decLen == 0 )
				//must have at least one of the two
				return null;

			double expPart = 0;

			if( expCharLwr.HasValue && index < endIndex &&
				Lower( str[index] ) == expCharLwr )
			{
				if( decPart != -1 && decLen == 0 )
					//if we have a decimal, we need digits before the exp
					return null;

				index++;

				var expSign = ReadSign( str, ref index, endIndex );

				int expLen;
				expPart = ReadDigits( str, ref index, endIndex, 10, out expLen );
				
				if( expPart < 0 || expLen == 0 )
					//have to have exp digits
					return null;

				expPart *= expSign;
			}

			double num = wholePart;

			if( decLen > 0 )
			{
				for( int c = decLen; c-- != 0; )
					num *= radix;
				num += decPart;

				if( radix == 16 )
					//foolishness, we have a base-2 exponent
					decLen *= 4;

				expPart -= decLen;
			}

			if( expPart != 0 )
			{
				var expBase = radix != 16 ? radix : 2;
				double exp = Math.Pow( expBase, Math.Abs( expPart ) );

				if( expPart > 0 )
					num *= exp;
				else
					num /= exp;
			}

			return num;
		}

		private static void SkipSpace( byte[] str, ref int index, int endIndex )
		{
			while( index < endIndex && IsSpace( str[index] ) )
				index++;
		}

		private static int ReadSign( byte[] str, ref int index, int endIndex, bool allowPlus = true )
		{
			if( index == endIndex )
				return 0;

			if( str[index] == '-' )
			{
				index++;
				return -1;
			}

			if( allowPlus && str[index] == '+' )
				index++;

			return 1;
		}

		private static double ReadDigits( byte[] str, ref int index, int endIndex, int radix, out int length )
		{
			double ret = 0;

			for( length = 0; index < endIndex; index++, length++ )
			{
				int digit = ParseDigit( str[index], radix );
				if( digit == -1 )
					break;

				ret = (ret * radix) + digit;
			}

			return ret;
		}

		private static int ParseDigit( byte ch, int radix )
		{
			int ret;

			if( ch >= '0' && ch <= '9' )
				ret = ch - '0';
			else if( ch >= 'a' && ch <= 'z' )
				ret = 10 + ch - 'a';
			else if( ch >= 'A' && ch <= 'Z' )
				ret = 10 + ch - 'A';
			else
				ret = -1;

			return ret < radix ? ret : -1;
		}

		private static bool IsSpace( byte ch )
		{
			switch( (char)ch )
			{
			case ' ':
			case '\f':
			case '\n':
			case '\r':
			case '\t':
			case '\v':
				return true;

			default:
				return false;
			}
		}

		private static byte Lower( byte ch )
		{
			if( ch >= 'A' && ch <= 'Z' )
				ch = (byte)(ch - 'A' + 'a');

			return ch;
		}
	}
}
