using System;
using System.Collections.Generic;
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

		public static readonly LString Name_Sort = "sort";
		public static readonly Callable Sort = (Callable)TSort;

		public static void SetTableMethods( Table globals )
		{
			globals[Name_Table] = new Table()
			{
				{ Name_Insert, Insert },
				{ Name_Remove, Remove },
				{ Name_Unpack, Unpack },
				{ Name_Sort, Sort },
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

		private static int TSort( Thread l )		
		{
			var list = (Table)l[1];
			var less = (Callable)l[2];

			QSort( l, list, less, 1, list.GetLen() );			

			return 0;
		}

		private static void QSort( Thread thread, Table list,
			Callable less, int l, int u )
		{
			while( l < u )
			{
				var vl = list[l];
				var vu = list[u];

				if( thread.Less( vu, vl, less ) )
				{
					//swap

					var tmp = vl;
					vl = vu;
					vu = tmp;

					list[l] = vl;
					list[u] = vu;
				}

				if( u - l == 1 )
					//only had two elements
					break;

				var i = (l + u) / 2;
				var vi = list[i];

				if( thread.Less( vi, vl, less ) )
				{
					var tmp = vl;
					vl = vi;
					vi = tmp;

					list[l] = vl;
					list[i] = vi;
				}
				else if( thread.Less( vu, vi, less ) )
				{
					var tmp = vu;
					vu = vi;
					vi = tmp;

					list[u] = vu;
					list[i] = vi;
				}

				if( u - l == 2 )
					//only had three elements
					break;

				var vp = vi;

				var j = u - 1;
				var vj = list[j];

				list[j] = vi;
				list[i] = vj;

				i = l;

				for( ; ; )
				{
					while( thread.Less( vi = list[++i], vp, less ) )
					{
						if( i >= u )
							throw new ArgumentException( "Invalid sort function." );
					}

					while( thread.Less( vp, vj = list[--j], less ) )
					{
						if( j <= l )
							throw new ArgumentException( "Invalid sort function." );
					}

					if( j < i )
						break;

					list[i] = vj;
					list[j] = vi;
				}

				list[u - 1] = list[i];
				list[i] = vp;

				if( i - l < u - i )
				{
					j = l;
					i--;
					l = i + 2;
				}
				else
				{
					j = i + 1;
					i = u;
					u = j - 2;
				}

				QSort( thread, list, less, j, i );
			}
		}
	}
}
