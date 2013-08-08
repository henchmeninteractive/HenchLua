using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuaSharp
{
	public static class BaseLib
	{
		public static readonly String Name_BGetMetaTable = "getmetatable";
		public static readonly String Name_BSetMetaTable = "setmetatable";

		public static void SetBaseMethods( Table globals )
		{
			globals[Name_BGetMetaTable] = (Callable)BGetMetaTable;
			globals[Name_BSetMetaTable] = (Callable)BSetMetaTable;
		}

		private static int BGetMetaTable( Thread l )
		{
			var stk = l.Stack;

			var mt = GetMetaTable( stk[1] );
			
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

		private static Table GetMetaTable( Value value )
		{
			var asTable = value.ToTable();
			if( asTable != null )
				return asTable.MetaTable;

			throw new ArgumentException( "Expected a table or user data." );
		}

		private static int BSetMetaTable( Thread l )
		{
			var stk = l.Stack;

			var mt = GetMetaTable( stk[1] );
			if( mt != null && mt.Contains( Literals.TagInfo_Metatable ) )
				throw new ArgumentException( "Can't change a protected metatable." );

			mt = stk[2].ToTable();
			if( mt == null )
				throw new ArgumentException( "Expected a table." );

			SetMetaTable( stk[1], mt );
			stk.Top = 1;

			return 1;
		}

		private static void SetMetaTable( Value value, Table mt )
		{
			var asTable = value.ToTable();
			if( asTable != null )
			{
				asTable.MetaTable = mt;
				return;
			}

			throw new ArgumentException( "Expected a table or user data." );
		}
	}
}
