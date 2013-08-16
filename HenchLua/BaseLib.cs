using System;
using System.Collections.Generic;

using Debug = System.Diagnostics.Debug;

namespace Henchmen.Lua
{
	public static class BaseLib
	{
		public static readonly String Name_BGetMetaTable = "getmetatable";
		public static readonly String Name_BSetMetaTable = "setmetatable";

		public static readonly String Name_BNext = "next";
		public static readonly String Name_BPairs = "pairs";

		public static readonly String Name_BType = "type";

		public static void SetBaseMethods( Table globals )
		{
			globals[Name_BNext] = Cb_BNext;
			globals[Name_BPairs] = (Callable)BPairs;

			globals[Name_BGetMetaTable] = (Callable)BGetMetatable;
			globals[Name_BSetMetaTable] = (Callable)BSetMetatable;

			globals[Name_BType] = (Callable)BType;
		}

		private static Callable Cb_BNext = (Callable)BNext;

		private static int BType( Thread l )
		{
			var stk = l.Stack;

			stk.Top = 1;

			int iType = (int)stk[1].ValueType;
			Debug.Assert( iType >= 0 && iType < Literals.TypeNames.Length );

			stk[1] = Literals.TypeNames[iType];

			return 1;
		}

		private static int BNext( Thread l )
		{
			var stk = l.Stack;

			stk.Top = 2;

			var tbl = (Table)stk[1];
			if( tbl == null )
				throw new InvalidCastException();

			Value key = stk[2];
			Value val;

			if( tbl.GetNext( ref key, out val ) )
			{
				stk.Top = 2;
				stk[1] = key;
				stk[2] = val;
				return 2;
			}
			else
			{
				stk.Top = 1;
				stk[1] = Value.Nil;
				return 1;
			}
		}

		private static int BPairs( Thread l )
		{
			var stk = l.Stack;

			var val = stk[1];
			var mt = GetMetatable( val );

			Value mmt;
			if( mt != null && mt.TryGetValue( Literals.TagMethod_Pairs, out mmt ) )
			{
				stk.Top = 1;
				l.Call( (Callable)mmt, 1, 3 );
			}
			else
			{
				stk.Top = 3;
				stk[1] = Cb_BNext;
				stk[2] = val;
				stk[3] = Value.Nil;
			}

			return 3;
		}

		private static int BGetMetatable( Thread l )
		{
			var stk = l.Stack;

			var mt = GetMetatable( stk[1] );
			
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

			stk.Top = 1;
			stk[1] = vmt;

			return 1;			
		}

		private static Table GetMetatable( Value value )
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
			var stk = l.Stack;

			var mt = GetMetatable( stk[1] );
			if( mt != null && mt.ContainsKey( Literals.TagInfo_Metatable ) )
				throw new ArgumentException( "Can't change a protected metatable." );

			mt = stk[2].ToTable();
			if( mt == null )
				throw new ArgumentException( "Expected a table." );

			SetMetatable( stk[1], mt );
			stk.Top = 1;

			return 1;
		}

		private static void SetMetatable( Value value, Table mt )
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
