using System;
using System.Collections.Generic;

using Debug = System.Diagnostics.Debug;

namespace Henchmen.Lua
{
	public static class BaseLib
	{
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

		public static readonly LString Name_BType = "type";
		public static readonly Callable Type = (Callable)BType;

		public static void SetBaseMethods( Table globals )
		{
			globals[Name_Next] = Next;
			globals[Name_Pairs] = Pairs;
			globals[Name_IPairs] = IPairs;

			globals[Name_GetMetatable] = GetMetatable;
			globals[Name_SetMetatable] = SetMetatable;

			globals[Name_BType] = Type;
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
			l.StackTop = 2;

			var tbl = (Table)l[1];
			if( tbl == null )
				throw new InvalidCastException();

			Value key = l[2];
			Value val;

			if( tbl.GetNext( ref key, out val ) )
				return l.SetStack( key, val );
			else
				return l.SetStack( Value.Nil );
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
				l.SetStack( Next, val, Value.Nil );
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
				return l.SetStack( key, val );
			else
				return l.SetStack( Value.Nil );
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
			}
			else
			{
				l.SetStack( INext, val, 0 );
			}

			return 3;
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
				vmt = Value.Nil;
			}

			return l.SetStack( vmt );
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
	}
}
