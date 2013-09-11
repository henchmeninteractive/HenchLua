﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Henchmen.Lua.Libs
{
	public static class TableLib
	{
		public static readonly LString Name_Table = "table";

		public static readonly LString Name_Insert = "insert";
		public static readonly Callable Insert = (Callable)TInsert;

		public static readonly LString Name_Remove = "remove";
		public static readonly Callable Remove = (Callable)TRemove;

		public static readonly LString Name_Unpack = "unpack";
		public static readonly Callable Unpack = (Callable)TUnpack;

		public static void SetTableMethods( Table globals )
		{
			globals[Name_Table] = new Table()
			{
				{ Name_Insert, Insert },
				{ Name_Remove, Remove },
				{ Name_Unpack, Unpack },
			};
		}

		private static int TInsert( Thread l )
		{
			var t = (Table)l[1];
			
			int pos, n = t.GetLen() + 1;
			Value val;

			switch( l.StackTop )
			{
			case 2:
				pos = n;
				val = l[2];
				break;

			case 3:
				pos = (int)l[2];
				val = l[3];

				if( pos > n )
					n = pos;

				for( int i = n; i > pos; i-- )
					t[i] = t[i - 1];
				break;

			default:
				throw new ArgumentException( "Incorrect number of args for table.insert." );
			}

			t[pos] = val;

			return 0;
		}

		private static int TRemove( Thread l )
		{
			var t = (Table)l[1];

			int pos, n = t.GetLen();

			switch( l.StackTop )
			{
			case 1:
				pos = n;
				break;

			case 2:
				pos = (int)l[2];
				break;

			default:
				throw new ArgumentException( "Incorrect number of args for table.remove." );
			}

			var ret = t[pos];

			for( int i = pos; i < n; i++ )
				t[i] = t[i + 1];
			t[n] = new Value();

			return l.SetReturnValues( ret );
		}

		private static int TUnpack( Thread l )
		{
			var t = (Table)l[1];

			int min = l.StackTop >= 2 ? (int)l[2] : 1;
			int max = l.StackTop >= 3 ? (int)l[3] : t.GetLen();

			if( min > max )
				//empty range
				return 0;

			int n = max - min + 1;

			l.StackTop = n;
			for( int i = 0; i < n; i++ )
				l[i + 1] = t[min + i];

			return n;
		}
	}
}
