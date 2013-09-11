using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Henchmen.Lua.Libs
{
	public static class MathLib
	{
		public static readonly LString Name_Math = "math";

		public static readonly LString Name_Pi = "pi";
		public static readonly LString Name_Huge = "huge";

		public static readonly LString Name_Abs = "abs";
		public static readonly Callable Abs = (Callable)(l => l.SetReturnValues( Math.Abs( (double)l[1] ) ));
		public static readonly LString Name_Ceil = "ceil";
		public static readonly Callable Ceil = (Callable)(l => l.SetReturnValues( Math.Ceiling( (double)l[1] ) ));
		public static readonly LString Name_Floor = "floor";
		public static readonly Callable Floor = (Callable)(l => l.SetReturnValues( Math.Floor( (double)l[1] ) ));
		public static readonly LString Name_Min = "min";
		public static readonly Callable Min = (Callable)MMin;
		public static readonly LString Name_Max = "max";
		public static readonly Callable Max = (Callable)MMax;

		public static readonly LString Name_Exp = "exp";
		public static readonly Callable Exp = (Callable)(l => l.SetReturnValues( Math.Exp( (double)l[1] ) ));
		public static readonly LString Name_Pow = "pow";
		public static readonly Callable Pow = (Callable)(l => l.SetReturnValues( Math.Pow( (double)l[1], (double)l[2] ) ));
		public static readonly LString Name_Log = "log";
		public static readonly Callable Log = (Callable)MLog;
		public static readonly LString Name_Sqrt = "sqrt";
		public static readonly Callable Sqrt = (Callable)(l => l.SetReturnValues( Math.Sqrt( (double)l[1] ) ));

		public static readonly LString Name_Fmod = "fmod";
		public static readonly Callable Fmod = (Callable)(l => l.SetReturnValues( (double)l[1] % (double)l[2] ));
		public static readonly LString Name_Modf = "modf";
		public static readonly Callable Modf = (Callable)MModf;
		public static readonly LString Name_Frexp = "frexp";
		public static readonly Callable Frexp = (Callable)MFrexp;
		public static readonly LString Name_Ldexp = "ldexp";
		public static readonly Callable Ldexp = (Callable)(l => l.SetReturnValues( (double)l[1] * Math.Pow( 2, (double)l[2] ) ));

		public static readonly LString Name_Deg = "deg";
		public static readonly Callable Deg = (Callable)(l => l.SetReturnValues( (double)l[1] * 180.0 / Math.PI ));
		public static readonly LString Name_Rad = "rad";
		public static readonly Callable Rad = (Callable)(l => l.SetReturnValues( (double)l[1] * Math.PI / 180.0 ));

		public static readonly LString Name_Sin = "sin";
		public static readonly Callable Sin = (Callable)(l => l.SetReturnValues( Math.Sin( (double)l[1] ) ));
		public static readonly LString Name_Asin = "asin";
		public static readonly Callable Asin = (Callable)(l => l.SetReturnValues( Math.Asin( (double)l[1] ) ));
		public static readonly LString Name_Sinh = "sinh";
		public static readonly Callable Sinh = (Callable)(l => l.SetReturnValues( Math.Sinh( (double)l[1] ) ));

		public static readonly LString Name_Cos = "cos";
		public static readonly Callable Cos = (Callable)(l => l.SetReturnValues( Math.Cos( (double)l[1] ) ));
		public static readonly LString Name_Acos = "acos";
		public static readonly Callable Acos = (Callable)(l => l.SetReturnValues( Math.Acos( (double)l[1] ) ));
		public static readonly LString Name_Cosh = "cosh";
		public static readonly Callable Cosh = (Callable)(l => l.SetReturnValues( Math.Cosh( (double)l[1] ) ));

		public static readonly LString Name_Tan = "tan";
		public static readonly Callable Tan = (Callable)(l => l.SetReturnValues( Math.Tan( (double)l[1] ) ));
		public static readonly LString Name_Atan = "atan";
		public static readonly Callable Atan = (Callable)(l => l.SetReturnValues( Math.Atan( (double)l[1] ) ));
		public static readonly LString Name_Atan2 = "atan2";
		public static readonly Callable Atan2 = (Callable)(l => l.SetReturnValues( Math.Atan2( (double)l[1], (double)l[2] ) ));
		public static readonly LString Name_Tanh = "tanh";
		public static readonly Callable Tanh = (Callable)(l => l.SetReturnValues( Math.Tanh( (double)l[1] ) ));

		public static readonly LString Name_Random = "random";
		public static readonly LString Name_RandSeed = "randomseed";

		public static void SetMathMethods( Table globals )
		{
			var rng = new MRng();

			globals[Name_Math] = new Table()
			{
				{ Name_Abs, Abs },
				{ Name_Acos, Acos },
				{ Name_Asin, Asin },
				{ Name_Atan, Atan },
				{ Name_Atan2, Atan2 },
				{ Name_Ceil, Ceil },
				{ Name_Cos, Cos },
				{ Name_Cosh, Cosh },
				{ Name_Deg, Deg },
				{ Name_Exp, Exp },
				{ Name_Floor, Floor },
				{ Name_Fmod, Fmod },
				{ Name_Frexp, Frexp },
				{ Name_Huge, double.MaxValue },
				{ Name_Ldexp, Ldexp },
				{ Name_Log, Log },
				{ Name_Max, Max },
				{ Name_Min, Min },
				{ Name_Modf, Modf },
				{ Name_Pi, Math.PI },
				{ Name_Pow, Pow },
				{ Name_Rad, Rad },
				{ Name_Random, (Callable)rng.MRandom },
				{ Name_RandSeed, (Callable)rng.MSeed },
				{ Name_Sin, Sin },
				{ Name_Sinh, Sinh },
				{ Name_Sqrt, Sqrt },
				{ Name_Tan, Tan },
				{ Name_Tanh, Tanh },
			};
		}

		private static int MLog( Thread l )
		{
			var n = (double)l[1];
			if( l.StackTop == 1 )
				return l.SetReturnValues( Math.Log( n ) );

			var b = (double)l[2];
			if( b == 10 )
				return l.SetReturnValues( Math.Log10( n ) );

			return l.SetReturnValues( Math.Log( n, b ) );
		}

		private static int MMax( Thread l )
		{
			var ret = (double)l[1];
			
			for( int i = 2; i <= l.StackTop; i++ )
			{
				var n = (double)l[i];
				if( n > ret )
					ret = n;
			}

			return l.SetReturnValues( ret );
		}

		private static int MMin( Thread l )
		{
			var ret = (double)l[1];

			for( int i = 2; i <= l.StackTop; i++ )
			{
				var n = (double)l[i];
				if( n < ret )
					ret = n;
			}

			return l.SetReturnValues( ret );
		}

		private static int MModf( Thread l )
		{
			var n = (double)l[1];

			var i = Math.Truncate( n );
			var f = n - i;

			return l.SetReturnValues( i, f );
		}

		private static int MFrexp( Thread l )
		{
			var n = (double)l[1];

			//extract the actual bits
			
			long bits = BitConverter.DoubleToInt64Bits( n );

			var isNeg = (bits < 0);
			var exp = (int)((bits >> 52) & 0x7FFL);
			var mant = bits & 0xFFFFFFFFFFFFFL;
							  
			if( exp == 0 )
				//denormal, exponent is actually 1
				exp++;
			else
				//normal number, add the leading 1 to the mantissa
				mant = mant | (1L << 52);

			//deal with the exponent's bias (1023)

			//also compensate for the fact that the mantissa is coming
			//through as a whole number rather (therefore the extra 52)

			exp -= 1023 + 52;

			if( mant == 0 )
				l.SetReturnValues( 0, 0 );

			//partially normalize the mantissa (that is, move trailing 0s
			//into the exponent so that the mantissa is as small as can be)

			while( (mant & 0x1) == 0 )
			{
				mant >>= 1;
				exp++;
			}

			//can't do much more in bits alone, the rest of the normalization
			//with the doubles we'll return (this is exactly the same loop
			//as above, only with fractions (because odd numbers)

			var nMant = (double)mant;
			var nExp = (double)exp;

			while( nMant >= 1 )
			{
				nMant *= 0.5;
				nExp++;
			}

			if( isNeg )
				nMant = -nMant;

			return l.SetReturnValues( nMant, nExp );
		}

		private sealed class MRng
		{
			private Random rng;

			public int MRandom( Thread l )
			{
				if( rng == null )
					rng = new Random();

				double max, min, n = rng.NextDouble();

				switch( l.StackTop )
				{
				case 0:
					break;

				case 1:
					max = (double)l[1];
					if( max < 1 )
						throw new ArgumentException( "interval is empty" );

					n = Math.Floor( n * max ) + 1;
					break;

				case 2:
					min = (double)l[1];
					max = (double)l[2];

					if( min > max )
						throw new ArgumentException( "interval is empty" );

					n = Math.Floor( n * (max - min + 1) ) + min;
					break;

				default:
					throw new ArgumentException( "wrong number of arguments" );
				}

				return l.SetReturnValues( n );
			}

			public int MSeed( Thread l )
			{
				rng = new Random( (int)l[1] );
				return 0;
			}
		}
	}
}
