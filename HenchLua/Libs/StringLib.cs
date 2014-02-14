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

		public static readonly LString Name_Find = "find";
		public static readonly Callable Find = (Callable)SFind;

		public static readonly LString Name_Match = "match";
		public static readonly Callable Match = (Callable)SMatch;

		public static void SetStringMethods( Table globals )
		{
			globals[Name_String] = new Table()
			{
				{ Name_Rep, Rep },
				{ Name_Find, Find },
				{ Name_Match, Match },
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

		private static int SFind( Thread l )
		{
			return FindCore( l, true );
		}

		private static int SMatch( Thread l )
		{
			return FindCore( l, false );
		}

		private static int FindCore( Thread l, bool isFind )
		{
			var str = (LString)l[1];
			var pat = (LString)l[2];

			int init = l.StackTop >= 3 ? (int)l[3] : 1;
			if( init < 0 )
				init = str.Length + init;

			if( init < 1 )
				init = 1;
			else if( init > str.Length + 1 )
				return l.SetNilReturnValue();

			init--;

			if( isFind && (l[4].ToBool() || !HasPatternSpecials( pat )) )
			{
				//do a plain search
				int idx = str.IndexOf( pat, init );
				if( idx != -1 )
					return l.SetReturnValues( idx + 1, idx + pat.Length );
				else
					return l.SetNilReturnValue();
			}
			else
			{
				MatchState ms;
				InitMatchState( out ms, l );

				ms.Str = str.InternalData;
				ms.StrInit = LString.BufferDataOffset;

				ms.Pat = pat.InternalData;
				int patInit = LString.BufferDataOffset;

				bool anchor = pat[0] == '^';
				if( anchor )
					patInit++;

				int sPos = LString.BufferDataOffset + init;

				do {
					Debug.Assert( ms.MatchDepth == MaxCCalls );

					ms.Level = 0;
					var res = SMatch( ref ms, sPos, patInit );
					if( res != -1 )
					{
						if( isFind )
						{
							l.StackTop = 0;
							l.Push( sPos + 1 );
							l.Push( res );
							return PushCaptures( ref ms, -1, -1 ) + 2;
						}
						else
						{
							return PushCaptures( ref ms, sPos, res );
						}
					}
				} while( sPos++ < ms.Str.Length && !anchor );

				RetireMatchState( ref ms );
				return l.SetNilReturnValue();
			}
		}

		private static bool HasPatternSpecials( LString pat )
		{
			var pDat = pat.InternalData;
			for( int i = LString.BufferDataOffset; i < pDat.Length; i++ )
			{
				switch( pDat[i] )
				{
				case (byte)'^':
				case (byte)'$':
				case (byte)'*':
				case (byte)'+':
				case (byte)'?':
				case (byte)'.':
				case (byte)'(':
				case (byte)'[':
				case (byte)'%':
				case (byte)'-':
					return true;
				}
			}

			return false;
		}

		private const int MaxCaptures = 32;
		private const int MaxCCalls = 200;

		private const int CapUnfinished = -1;
		private const int CapPosition = -2;

		private static void InitMatchState( out MatchState ms, Thread l )
		{
			ms = new MatchState();

			ms.L = l;
			ms.Captures = l.matchCapCache;
			if( ms.Captures == null )
				ms.Captures = new MatchCaptureRec[MaxCaptures];
			else
				//steal it, in case we end up recursing (next guy allocs, too bad)
				l.matchCapCache = null;

			ms.MatchDepth = MaxCCalls;			
		}

		private static void RetireMatchState( ref MatchState ms )
		{
			ms.L.matchCapCache = ms.Captures;
		}

		private struct MatchState
		{
			public Thread L;

			public byte[] Str;
			public int StrInit;

			public byte[] Pat;

			public int Level;
			public MatchCaptureRec[] Captures;

			public int MatchDepth;
		}

		internal struct MatchCaptureRec
		{
			public int Pos, Len;
		}

		private static int PushCaptures( ref MatchState ms, int sPos, int esPos, bool retireMs = true )
		{
			int nLevels = (ms.Level == 0 && sPos != -1) ? 1 : ms.Level;

			for( int i = 0; i < nLevels; i++ )
				PushCapture( ref ms, i, sPos, esPos );

			if( retireMs )
				RetireMatchState( ref ms );

			return nLevels;
		}

		private static void PushCapture( ref MatchState ms, int i, int sPos, int esPos )
		{
			if( i > ms.Level )
			{
				if( i == 0 )
					ms.L.Push( new LString( ms.Str, sPos, esPos - sPos ) );
				else
					throw new ArgumentException( "Invalid capture index." );
			}
			else
			{
				var cap = ms.Captures[i];

				if( cap.Len == CapUnfinished )
					throw new ArgumentException( "Unfinished capture." );

				if( cap.Len == CapPosition )
					ms.L.Push( cap.Pos - LString.BufferDataOffset + 1 );
				else
					ms.L.Push( new LString( ms.Str, cap.Pos, cap.Len ) );
			}
		}

		private static int SMatch( ref MatchState ms, int sPos, int pPos )
		{
			if( ms.MatchDepth-- == 0 )
				throw new ArgumentException( "Pattern too complex" );

			var str = ms.Str;
			var pat = ms.Pat;

		init:
			if( pPos == pat.Length )
			{
				ms.MatchDepth++;
				return sPos;
			}

			switch( pat[pPos] )
			{
			case (byte)'(':
				if( pPos < pat.Length && pat[pPos + 1] == (byte)')' )
					sPos = StartCapture( ref ms, sPos, pPos + 2, CapPosition );
				else
					sPos = StartCapture( ref ms, sPos, pPos + 1, CapUnfinished );
				break;

			case (byte)')':
				sPos = EndCapture( ref ms, sPos, pPos + 1 );
				break;

			case (byte)'$':
				if( pPos + 1 != pat.Length )
					goto default;
				sPos = (sPos == str.Length) ? sPos : -1;
				break;

			case (byte)'%':
				if( pPos == pat.Length )
					goto default;

				switch( pat[pPos + 1] )
				{
				case (byte)'b':
					sPos = MatchBalance( ref ms, sPos, pPos + 2 );
					if( sPos != -1 )
					{
						pPos += 4;
						goto init;
					}
					break;

				case (byte)'f':
					{
						pPos += 2;
						if( pPos >= pat.Length || pat[pPos] != (byte)'[' )
							throw new ArgumentException( "Missing [ after %f in pattern" );

						var ep = ClassEnd( ref ms, pPos );
						var prev = sPos == ms.StrInit ? (byte)'\0' : str[sPos - 1];
						if( !MatchBracketClass( ref ms, prev, pPos, ep - 1 ) &&
							MatchBracketClass( ref ms, str[sPos], pPos, ep - 1 ) )
						{
							pPos = ep;
							goto init;
						}

						sPos = -1;
					}
					break;

				case (byte)'0':
				case (byte)'1':
				case (byte)'2':
				case (byte)'3':
				case (byte)'4':
				case (byte)'5':
				case (byte)'6':
				case (byte)'7':
				case (byte)'8':
				case (byte)'9':
					sPos = MatchCapture( ref ms, sPos, pat[pPos + 1] );
					if( sPos != -1 )
					{
						pPos += 2;
						goto init;
					}
					break;

				default:
					goto o_default;
				}
				break;

			default:
			o_default:
				{
					var ep = ClassEnd( ref ms, pPos );

					if( !SingleMatch( ref ms, sPos, pPos, ep ) )
					{
						switch( pat[ep] )
						{
						case (byte)'*':
						case (byte)'?':
						case (byte)'-':
							pPos = ep + 1;
							goto init;

						default:
							sPos = -1;
							break;
						}
					}
					else
					{
						switch( pat[ep] )
						{
						case (byte)'?':
							{
								var res = SMatch( ref ms, sPos + 1, ep + 1 );
								if( res == -1 )
								{
									pPos = ep + 1;
									goto init;
								}

								sPos = res;
							}
							break;
						
						case (byte)'+':
							sPos++;
							goto case (byte)'*';

						case (byte)'*':
							sPos = MaxExpand( ref ms, sPos, pPos, ep );
							break;

						case (byte)'-':
							sPos = MinExpand( ref ms, sPos, pPos, ep );
							break;

						default:
							sPos++;
							pPos = ep;
							goto init;
						}
					}
				}
				break;
			}

			ms.MatchDepth++;
			return sPos;
		}

		private static int StartCapture( ref MatchState ms, int sPos, int pPos, int flag )
		{
			throw new NotImplementedException();
		}

		private static int EndCapture( ref MatchState ms, int sPos, int pPos )
		{
			throw new NotImplementedException();
		}

		private static int MatchBalance( ref MatchState ms, int sPos, int pPos )
		{
			throw new NotImplementedException();
		}

		private static bool MatchClass( byte ch, byte pat )
		{
			throw new NotImplementedException();
		}

		private static bool MatchBracketClass( ref MatchState ms, byte ch, int pPos, int epPos )
		{
			throw new NotImplementedException();
		}

		private static int ClassEnd( ref MatchState ms, int pPos )
		{
			var pat = ms.Pat;

			switch( pat[pPos++] )
			{
			case (byte)'%':
				if( pPos == pat.Length )
					throw new ArgumentException( "Malformed pattern (ends with %%)" );
				return pPos + 1;

			case (byte)'[':
				if( pat[pPos] == (byte)'^' )
					pPos++;

				do
				{
					if( pPos == pat.Length )
						throw new ArgumentException( "Malformed pattern (missing ']')" );

					if( pat[pPos++] == (byte)'%' && pPos < pat.Length )
						pPos++;
				} while( pat[pPos] != (byte)']' );

				return pPos + 1;
			
			default:
				return pPos;
			}

			throw new NotImplementedException();
		}

		private static int MatchCapture( ref MatchState ms, int sPos, byte pch )
		{
			throw new NotImplementedException();
		}

		private static bool SingleMatch( ref MatchState ms, int sPos, int pPos, int ep )
		{
			var pat = ms.Pat;
			var str = ms.Str;

			if( sPos == str.Length )
				return false;

			var c = str[sPos];
			var p = pat[pPos];

			switch( p )
			{
			case (byte)'.':
				return true;

			case (byte)'%':
				return MatchClass( c, pat[pPos + 1] );

			case (byte)'[':
				return MatchBracketClass( ref ms, c, pPos, ep - 1 );

			default:
				return p == c;
			}
		}

		private static int MaxExpand( ref MatchState ms, int sPos, int pPos, int ep )
		{
			throw new NotImplementedException();
		}

		private static int MinExpand( ref MatchState ms, int sPos, int pPos, int ep )
		{
			throw new NotImplementedException();
		}
	}
}
