using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Henchmen.Lua
{
	public static class MathLib
	{
		public static readonly LString Name_Math = "math";

		public static readonly LString Name_Pi = "pi";

		public static readonly LString Name_Sqrt = "sqrt";
		public static readonly Callable Sqrt = (Callable)MSqrt;

		public static readonly LString Name_Sin = "sin";
		public static readonly Callable Sin = (Callable)MSin;
		public static readonly LString Name_Cos = "cos";
		public static readonly Callable Cos = (Callable)MCos;
		public static readonly LString Name_Atan2 = "atan2";
		public static readonly Callable Atan2 = (Callable)MAtan2;

		public static void SetMathMethods( Table globals )
		{
			globals[Name_Math] = new Table()
			{
				{ Name_Pi, Math.PI },

				{ Name_Sqrt, Sqrt },
				{ Name_Sin, Sin },
				{ Name_Cos, Cos },
				{ Name_Atan2, Atan2 },
			};
		}

		private static int MSqrt( Thread l )
		{
			return l.SetStack( Math.Sqrt( (double)l[1] ) );
		}

		private static int MSin( Thread l )
		{
			return l.SetStack( Math.Sin( (double)l[1] ) );
		}

		private static int MCos( Thread l )
		{
			return l.SetStack( Math.Cos( (double)l[1] ) );
		}

		private static int MAtan2( Thread l )
		{
			return l.SetStack( Math.Atan2( (double)l[1], (double)l[2] ) );
		}
	}
}
