using System;
using System.Collections.Generic;
using System.Text;

using Debug = System.Diagnostics.Debug;

namespace Henchmen.Lua.Libs
{
	public static class StringLib
	{
		public static readonly LString Name_String = Literals.TypeName_String;

		public static readonly LString Name_Rep = "rep";
		public static readonly Callable Rep = (Callable)SRep;

		public static void SetStringMethods( Table globals )
		{
			globals[Name_String] = new Table()
			{
				{ Name_Rep, Rep },
			};
		}

		private static int SRep( Thread l )
		{
			var str = (LString)l[1];
			int n = (int)l[2];

			if( n == 0 )
				return l.SetReturnValues( LString.Empty );

			if( n < 0 )
				throw new ArgumentOutOfRangeException( "negative repeat count" );
			
			var sep = l.StackTop >= 3 ? (LString)l[3] : LString.Empty;

			if( n == 1 )
				return l.SetReturnValues( str );

			int strOfs, strLen, sepOfs, sepLen;
			byte[] strBuf, sepBuf;

			str.UnsafeGetDataBuffer( out strBuf, out strOfs, out strLen );
			sep.UnsafeGetDataBuffer( out sepBuf, out sepOfs, out sepLen );

			var retLen = strLen * n + sepLen * (n - 1);
			
			var retBuf = LString.InternalAllocBuffer( retLen );
			int retOfs = LString.BufferDataOffset;

			Buffer.BlockCopy( strBuf, strOfs, retBuf, retOfs, strLen );

			if( sepLen == 0 )
			{
				for( int i = 1; i < n; i++ )
					Buffer.BlockCopy( strBuf, strOfs, retBuf, retOfs += strLen, strLen );
			}
			else
			{
				for( int i = 1; i < n; i++ )
				{
					Buffer.BlockCopy( sepBuf, sepOfs, retBuf, retOfs += strLen, sepLen );
					Buffer.BlockCopy( strBuf, strOfs, retBuf, retOfs += sepLen, strLen );
				}
			}

			Debug.Assert( retOfs + strLen == retBuf.Length );

			return l.SetReturnValues( LString.InternalFinishBuffer( retBuf ) );
		}
	}
}
