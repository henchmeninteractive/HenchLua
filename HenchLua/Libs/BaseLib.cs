using System;
using System.Collections.Generic;

using Debug = System.Diagnostics.Debug;

namespace Henchmen.Lua.Libs
{
	public static class BaseLib
	{
		public static readonly LString Name__G = "_G";

		public static readonly LString Name_GetMetatable = "getmetatable";
		public static readonly Callable GetMetatable = (Callable)BGetMetatable;
		public static readonly LString Name_SetMetatable = "setmetatable";
		public static readonly Callable SetMetatable = (Callable)BSetMetatable;

		public static readonly LString Name_Pairs = "pairs";
		public static readonly Callable Pairs = (Callable)BPairs;
		public static readonly LString Name_Next = "next";
		public static readonly Callable Next = (Callable)BNext;

		public static readonly LString Name_IPairs = "ipairs";
		public static readonly Callable IPairs = (Callable)BIPairs;
		public static readonly Callable INext = (Callable)BINext;

		public static readonly LString Name_Type = "type";
		public static readonly Callable Type = (Callable)BType;

		public static readonly LString Name_ToNumber = "tonumber";
		public static readonly Callable ToNumber = (Callable)BToNumber;
		public static readonly LString Name_ToString = "tostring";
		public static new readonly Callable ToString = (Callable)BToString;

		public static readonly LString Name_Select = "select";
		public static readonly Callable Select = (Callable)BSelect;

		public static readonly LString Name_Print = "print";
		public static readonly Callable Print = (Callable)BPrint;

		public static readonly LString Name_CollectGarbage = "collectgarbage";
		public static readonly Callable CollectGarbage_Nop = (Callable)BCollectGarbage_Nop;
		public static readonly Callable CollectGarbage_Gc = (Callable)BCollectGarbage_Gc;

		public static readonly LString Name_RawGet = "rawget";
		public static readonly Callable RawGet = (Callable)BRawGet;
		public static readonly LString Name_RawSet = "rawset";
		public static readonly Callable RawSet = (Callable)BRawSet;

		public static void SetBaseMethods( Table globals )
		{
			globals[Name__G] = globals;

			globals[Name_Next] = Next;
			globals[Name_Pairs] = Pairs;
			globals[Name_IPairs] = IPairs;

			globals[Name_RawGet] = RawGet;
			globals[Name_RawSet] = RawSet;

			globals[Name_GetMetatable] = GetMetatable;
			globals[Name_SetMetatable] = SetMetatable;

			globals[Name_ToString] = ToString;
			globals[Name_ToNumber] = ToNumber;

			globals[Name_Print] = Print;

			globals[Name_Type] = Type;

			globals[Name_Select] = Select;

			globals[Name_CollectGarbage] = CollectGarbage_Nop;
		}

		/// <summary>
		/// Replaces the default (nop) GC handler with one that
		/// calls into the .NET GC to do partial or full collections.
		/// </summary>
		public static void SetRealCollectGarbageHandler( Table globals )
		{
			globals[Name_CollectGarbage] = CollectGarbage_Gc;
		}

		private static int BType( Thread l )
		{
			l.StackTop = 1;

			int iType = (int)l[1].ValueType;
			Debug.Assert( iType >= 0 && iType < Literals.TypeNames.Length );

			l[1] = Literals.TypeNames[iType];

			return 1;
		}

		private static int BNext( Thread l )
		{
			var tbl = (Table)l[1];
			if( tbl == null )
				throw new ArgumentNullException();

			Value key = l[2];
			Value val;

			if( tbl.GetNext( ref key, out val ) )
				return l.SetReturnValues( key, val );
			else
				return l.SetNilReturnValue();
		}

		private sealed class StableNext : UserFunction
		{
			private int loc;
			public override int Execute( Thread l )
			{
				var tbl = (Table)l[1];
				if( tbl == null )
					throw new ArgumentNullException();

				Value key, val;
				if( tbl.GetNext( ref loc, out key, out val ) )
					return l.SetReturnValues( key, val );
				else
					return l.SetNilReturnValue();
			}
		}

		private static int BPairs( Thread l )
		{
			var val = l[1];
			var mt = GetMetatableImp( val );

			Value mmt;
			if( mt != null && mt.TryGetValue( Literals.TagMethod_Pairs, out mmt ) )
			{
				l.StackTop = 1;
				l.Call( (Callable)mmt, 1, 3 );
			}
			else
			{
				l.SetStack( (Callable)new StableNext(),
					val, Value.Nil );
			}

			return 3;
		}

		private static int BINext( Thread l )
		{
			l.StackTop = 2;

			var tbl = (Table)l[1];
			if( tbl == null )
				throw new InvalidCastException();

			var key = (int)(double)l[2] + 1;
			Value val;

			if( tbl.TryGetValue( key, out val ) )
				return l.SetReturnValues( key, val );
			else
				return l.SetNilReturnValue();
		}

		private static int BIPairs( Thread l )
		{
			var val = l[1];
			var mt = GetMetatableImp( val );

			Value mmt;
			if( mt != null && mt.TryGetValue( Literals.TagMethod_IPairs, out mmt ) )
			{
				l.StackTop = 1;
				l.Call( (Callable)mmt, 1, 3 );
				
				return 3;
			}
			else
			{
				return l.SetReturnValues( INext, val, 0 );
			}
		}

		private static int BGetMetatable( Thread l )
		{
			var mt = GetMetatableImp( l[1] );

			Value vmt;

			if( mt != null )
			{
				int loc = mt.FindValue( Literals.TagInfo_Metatable );
				if( loc != 0 )
					mt.ReadValue( loc, out vmt );
				else
					vmt = mt;
			}
			else
			{
				vmt = new Value();
			}

			return l.SetReturnValues( vmt );
		}

		private static Table GetMetatableImp( Value value )
		{
			var asTable = value.ToTable();
			if( asTable != null )
				return asTable.Metatable;

			var asHasMt = value.RefVal as IHasMetatable;
			if( asHasMt != null )
				return asHasMt.Metatable;

			throw new ArgumentException( "Expected a table or user data." );
		}

		private static int BSetMetatable( Thread l )
		{
			var mt = GetMetatableImp( l[1] );
			if( mt != null && mt.ContainsKey( Literals.TagInfo_Metatable ) )
				throw new ArgumentException( "Can't change a protected metatable." );

			mt = l[2].ToTable();
			if( mt == null )
				throw new ArgumentException( "Expected a table." );

			SetMetatableImp( l[1], mt );
			l.StackTop = 1;

			return 1;
		}

		private static void SetMetatableImp( Value value, Table mt )
		{
			var asTable = value.ToTable();
			if( asTable != null )
			{
				asTable.metatable = mt;
				return;
			}

			throw new ArgumentException( "Expected a table value." );
		}

		private static int BRawGet( Thread l )
		{
			var ta = (Table)l[1];
			return l.SetReturnValues( ta[l[2]] );
		}

		private static int BRawSet( Thread l )
		{
			var ta = (Table)l[1];
			ta[l[2]] = l[3];
			return 0;
		}

		private static int BToNumber( Thread l )
		{
			var nval = l[1];
			
			if( nval.ValueType == LValueType.Number )
				return l.SetReturnValues( nval );

			var nstr = nval.ToLString();
			if( nstr.IsNil )
				return l.SetNilReturnValue();
			
			byte[] nbuf;
			int nIndex, nCount;

			nstr.UnsafeGetDataBuffer( out nbuf, out nIndex, out nCount );

			double num;

			if( l.StackTop < 2 )
			{
				if( !Helpers.StrToNum( nbuf, nIndex, nCount, out num ) )
					return l.SetNilReturnValue();
			}
			else
			{
				int radix = (int)l[2];
				if( radix < 2 || radix > 36 )
					throw new ArgumentOutOfRangeException( "base out of range" );

				if( !Helpers.StrToInt( nbuf, nIndex, nCount, out num, radix ) )
					return l.SetNilReturnValue();
			}

			return l.SetReturnValues( num );
		}

		private static int BToString( Thread l )
		{
			var ret = ToStringCore( l[1], l );
			return l.SetReturnValues( ret );
		}

		private static LString ToStringCore( Value v, Thread l )
		{
			LString ret;

			switch( v.ValueType )
			{
			case LValueType.Nil:
				ret = Literals.Nil;
				break;

			case LValueType.Bool:
				ret = v.IsTrue ? Literals.True : Literals.False;
				break;

			case LValueType.Number:
				l.ConvertToString( ref v );
				goto case LValueType.String;

			case LValueType.String:
				ret = (LString)v;
				break;

			default:
				ret = new LString( v.ToString() );
				break;
			}

			return ret;
		}

		private static int BPrint( Thread l )
		{
			for( int i = 1; i <= l.StackTop; i++ )
			{
				var str = ToStringCore( l[i], l );

				if( i > 1 )
					Console.Write( "\t" );

				Console.Write( str.ToString() );
			}

			Console.WriteLine();

			return 0;
		}

		private static int BSelect( Thread l )
		{
			var selector = l[1];

			if( selector == Literals.Symbol_Hash )
				return l.SetReturnValues( l.StackTop - 1 );

			var sel = (int)selector;
			var top = l.StackTop;

			if( sel < 0 )
				sel = top + sel;
			else if( sel > top )
				sel = top;

			if( sel <= 1 )
				throw new ArgumentOutOfRangeException( "index", "index out of range" );

			return top - sel;						
		}

		private enum GcOpt
		{
			Stop,
			Restart,
			Collect,
			Count,
			Step,
			SetPause,
			SetStepMul,
			SetMajorInc,
			IsRunning,
			Generational,
			Incremental,
		}

		private static readonly LString[] GcOptNames = new LString[]
		{
			"stop",
			"restart",
			"collect",
			"count",
			"step",
			"setpause",
			"setstepmul",
			"setmajorinc",
			"isrunning",
			"generational",
			"incremental"
		};

		private static int BCollectGarbage_Nop( Thread l )
		{
			switch( (GcOpt)Helpers.CheckOpt( l[1], (int)GcOpt.Collect, GcOptNames ) )
			{
			case GcOpt.Count:
				return l.SetReturnValues( 0, 0 );

			default:
				break;
			}

			return l.SetReturnValues( 0 );
		}

		private static int BCollectGarbage_Gc( Thread l )
		{
			switch( (GcOpt)Helpers.CheckOpt( l[1], (int)GcOpt.Collect, GcOptNames ) )
			{
			case GcOpt.Collect:
				GC.Collect();
				break;

			case GcOpt.Step:
				GC.Collect( 0 );
				break;

			case GcOpt.Count:
				return l.SetReturnValues( 0, 0 );

			default:
				break;				
			}

			return l.SetReturnValues( 0 );
		}
	}
}
