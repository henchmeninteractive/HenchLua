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

		public static readonly LString Name_Sub = "sub";
		public static readonly Callable Sub = (Callable)SSub;

		public static readonly LString Name_Find = "find";
		public static readonly Callable Find = (Callable)SFind;

		public static readonly LString Name_Match = "match";
		public static readonly Callable Match = (Callable)SMatch;

		public static readonly LString Name_GSub = "gsub";
		public static readonly Callable GSub = (Callable)SGsub;

		public static readonly LString Name_GMatch = "gmatch";
		public static readonly Callable GMatch = (Callable)SGMatch;

		public static readonly LString Name_Char = "char";
		public static readonly Callable Char = (Callable)SChar;

		public static readonly LString Name_Len = "len";
		public static readonly Callable Len = (Callable)SLen;

		public static readonly LString Name_Upper = "upper";
		public static readonly Callable Upper = (Callable)SUpper;

		public static readonly LString Name_Lower = "lower";
		public static readonly Callable Lower = (Callable)SLower;
		
		public static void SetStringMethods( Table globals )
		{
			globals[Name_String] = new Table()
			{
				{ Name_Rep, Rep },
				{ Name_Char, Char },
				{ Name_Len, Len },
				{ Name_Upper, Upper },
				{ Name_Lower, Lower },
				{ Name_Sub, Sub },
				{ Name_Find, Find },
				{ Name_Match, Match },
				{ Name_GSub, GSub },
				{ Name_GMatch, GMatch },
			};
		}

		private static int SLen( Thread l )
		{
			var str = (LString)l[1];
			return l.SetReturnValues( str.Length );
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

		private static int SChar( Thread l )
		{
			var buf = LString.InternalAllocBuffer( l.StackTop );
			for( int i = LString.BufferDataOffset; i < buf.Length; i++ )
			{
				var cch = (int)l[i - LString.BufferDataOffset + 1];
				if( cch < 0 || cch > 0xFF )
					throw new ArgumentException( "value out of range" );
				buf[i] = (byte)cch;
			}
			return l.SetReturnValues( LString.InternalFinishBuffer( buf ) );
		}

		private static int SUpper( Thread l )
		{
			var str = (LString)l[1];
			var strDat = str.InternalData;

			var ret = LString.InternalAllocBuffer( str.Length );

			for( int i = LString.BufferDataOffset; i < ret.Length; i++ )
			{
				var ch = strDat[i];
				if( ch >= (byte)'a' && ch <= (byte)'z' )
					ch = (byte)((byte)'A' + (ch - (byte)'a'));
				ret[i] = ch;
			}

			return l.SetReturnValues( LString.InternalFinishBuffer( ret ) );
		}

		private static int SLower( Thread l )
		{
			var str = (LString)l[1];
			var strDat = str.InternalData;

			var ret = LString.InternalAllocBuffer( str.Length );

			for( int i = LString.BufferDataOffset; i < ret.Length; i++ )
			{
				var ch = strDat[i];
				if( ch >= (byte)'A' && ch <= (byte)'Z' )
					ch = (byte)((byte)'a' + (ch - (byte)'A'));
				ret[i] = ch;
			}

			return l.SetReturnValues( LString.InternalFinishBuffer( ret ) );
		}

		private static int StrIdxArg( LString str, int idx )
		{
			if( idx == 0 )
				return -1;

			if( idx > 0 )
				idx--;
			else if( idx < 0 )
				idx += str.Length;

			if( idx < 0 )
				idx = 0;
			else if( idx > str.Length )
				idx = str.Length;

			return idx;
		}

		private static int SSub( Thread l )
		{
			var str = (LString)l[1];
			
			var beg = StrIdxArg( str, (int)l[2] );
			var end = l.StackTop >= 3 ? StrIdxArg( str, (int)l[3] ) : str.Length - 1;

			if( beg <= end )
				return l.SetReturnValues( str.Substring( beg, end - beg + 1 ) );
			else
				return l.SetReturnValues( LString.Empty );
		}

		private static int SFind( Thread l )
		{
			return FindCore( l, true );
		}

		private static int SMatch( Thread l )
		{
			return FindCore( l, false );
		}

		private static int SGMatch( Thread l )
		{
			var str = (LString)l[1];
			var pat = (LString)l[2];

			return l.SetReturnValues( (Callable)new GMatcher( str, pat ) );
		}

		private class GMatcher : UserFunction
		{
			public GMatcher( LString str, LString pat )
			{
				if( str.IsNil || pat.IsNil )
					throw new ArgumentNullException();

				this.str = str.InternalData;
				this.pat = pat.InternalData;
			}

			private byte[] str, pat;
			private int sIdx = LString.BufferDataOffset;

			public override int Execute( Thread l )
			{
				MatchState ms;
				InitMatchState( out ms, l );

				ms.Str = str;
				ms.StrInit = LString.BufferDataOffset;
				
				ms.Pat = pat;

				while( sIdx <= str.Length )
				{
					Debug.Assert( ms.MatchDepth == MaxCCalls );
					ms.Level = 0;

					var e = SMatch( ref ms, sIdx, LString.BufferDataOffset );
					if( e != -1 )
					{
						int s = sIdx;
						sIdx = s != e ? e : s + 1;
						return PushCaptures( ref ms, s, e );
					}

					sIdx++;
				}

				RetireMatchState( ref ms );

				return 0;
			}
		}

		private static int FindCore( Thread l, bool isFind )
		{
			var str = (LString)l[1];
			var pat = (LString)l[2];

			int init = l.StackTop >= 3 ? StrIdxArg( str, (int)l[3] ) : 0;
			if( init == -1 )
				init = 0;
			
			if( init == str.Length && init != 0 )
				return l.SetNilReturnValue();

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

				bool anchor = patInit < ms.Pat.Length && ms.Pat[patInit] == (byte)'^';
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
							l.Push( sPos - LString.BufferDataOffset + 1 );
							l.Push( res - LString.BufferDataOffset );
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

		private static int SGsub( Thread l )
		{
			var str = (LString)l[1];
			var pat = (LString)l[2];

			var subst = l[3];
			var subTy = subst.ValueType;

			switch( subTy )
			{
			case LValueType.Number:
				l.ConvertToString( ref subst );
				break;

			case LValueType.String:
			case LValueType.Function:
			case LValueType.Table:
				break;

			default:
				throw new ArgumentException( "string/function/table expected" );
			}

			var max = l.StackTop >= 4 ? (int)l[4] : int.MaxValue;
	
			MatchState ms;
			InitMatchState( out ms, l );

			ms.Str = str.InternalData;
			ms.StrInit = LString.BufferDataOffset;

			ms.Pat = pat.InternalData;
			int patInit = LString.BufferDataOffset;

			bool anchor = patInit < ms.Pat.Length && ms.Pat[patInit] == (byte)'^';
			if( anchor )
				patInit++;

			var strBuilder = l.GetStrBuilder( str.Length * 2 );

			int sPos = LString.BufferDataOffset;

			int n = 0;
			while( n < max )
			{
				ms.Level = 0;
				Debug.Assert( ms.MatchDepth == MaxCCalls );

				var e = SMatch( ref ms, sPos, patInit );
				if( e != -1 )
				{
					n++;

					Value substVal;

					switch( subTy )
					{
					case LValueType.Function:
						{
							l.Push( subst );

							var nCap = PushCaptures( ref ms, sPos, e, false );
							l.Call( nCap, 1 );
							
							substVal = l.PopValue();
						}
						break;

					case LValueType.Table:
						PushCapture( ref ms, 0, sPos, e );
						substVal = l.GetTable( (Table)subst, l.PopValue() );
						break;

					case LValueType.Number:
						//it's already been made a string
						substVal = subst;
						break;

					case LValueType.String:
						//need to handle escape sequences
						{
							var sb = (byte[])subst.RefVal;
							for( int i = LString.BufferDataOffset; i < sb.Length; i++ )
							{
								var ch = sb[i];

								if( ch != (byte)'%' )
								{
									strBuilder.Append( ch );
									continue;
								}

								if( ++i == sb.Length )
									throw new ArgumentException( "Invalid use of % in replacement string" );

								ch = sb[i];
								
								if( ch == (byte)'%' )
								{
									strBuilder.Append( ch );
									continue;
								}

								switch( ch )
								{
								case (byte)'0':
									strBuilder.Append( ms.Str, sPos, e - sPos );
									break;

								case (byte)'1':
								case (byte)'2':
								case (byte)'3':
								case (byte)'4':
								case (byte)'5':
								case (byte)'6':
								case (byte)'7':
								case (byte)'8':
								case (byte)'9':
									{
										int idx = ch - (byte)'1';
										PushCapture( ref ms, idx, sPos, e );
										substVal = l.PopValue();
										l.ConvertToString( ref substVal );
										strBuilder.Append( (LString)substVal );										
									}
									break;

								default:
									throw new ArgumentException( "Invalid use o f% in replacement string" );
								}
							}
						}

						substVal = new Value(); //hush, little compiler
						break;

					default:
						substVal = new Value();
						break;
					}

					if( subTy != LValueType.String )
					{
						//strings already appended, need to handle this case now

						if( !substVal.ToBool() )
						{
							strBuilder.Append( ms.Str, sPos, e - sPos );
						}
						else
						{
							l.ConvertToString( ref substVal );
							strBuilder.Append( (LString)substVal );
						}
					}
				}

				if( e != -1 && e > sPos )
					sPos = e;
				else if( sPos < ms.Str.Length )
					strBuilder.Append( ms.Str[sPos++] );
				else
					break;

				if( anchor )
					break;
			}

			strBuilder.Append( ms.Str, sPos, ms.Str.Length - sPos );
			var ret = strBuilder.ToLString();
			
			l.RetireStrBuilder( strBuilder );
			RetireMatchState( ref ms );

			return l.SetReturnValues( ret, n );
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
			if( i >= ms.Level )
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
							MatchBracketClass( ref ms, sPos < str.Length ? str[sPos] : (byte)0, pPos, ep - 1 ) )
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
						switch( ep < pat.Length ? pat[ep] : (byte)0 )
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
						switch( ep < pat.Length ? pat[ep] : (byte)0 )
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

		private static int StartCapture( ref MatchState ms, int sPos, int pPos, int what )
		{
			var lv = ms.Level;
			if( lv >= MaxCaptures )
				throw new ArgumentException( "too many captures" );

			ms.Captures[lv] = new MatchCaptureRec() { Pos = sPos, Len = what };
			ms.Level = lv + 1;

			var res = SMatch( ref ms, sPos, pPos );
			if( res == -1 )
				ms.Level = lv;

			return res;
		}

		private static int EndCapture( ref MatchState ms, int sPos, int pPos )
		{
			int l = ms.Level;
			for( l--; l >= 0; l-- )
			{
				if( ms.Captures[l].Len == CapUnfinished )
					break;
			}

			if( l < 0 )
				throw new ArgumentException( "invalid pattern capture" );

			ms.Captures[l].Len = sPos - ms.Captures[l].Pos;
			
			var res = SMatch( ref ms, sPos, pPos );
			if( res == -1 )
				ms.Captures[l].Len = CapUnfinished;

			return res;
		}

		private static int MatchBalance( ref MatchState ms, int sPos, int pPos )
		{
			var str = ms.Str;
			var pat = ms.Pat;
			
			if( pPos >= pat.Length - 1 )
				throw new ArgumentException( "malformed pattern (missing arguments to %b)" );

			var b = pat[pPos];

			if( sPos < str.Length && str[sPos] != b )
				return -1;

			var e = pat[pPos + 1];

			int count = 1;
			while( ++sPos < str.Length )
			{
				var ch = str[sPos];

				if( ch == e )
				{
					if( --count == 0 )
						return sPos + 1;
				}
				else if( ch == b )
					count++;
			}

			return -1;
		}

		private static bool MatchClass( byte ch, byte cl )
		{
			if( (cl & 0x80) != 0 )
				return ch == cl;

			var isNeg = char.IsUpper( (char)cl );
			if( isNeg )
				cl = (byte)char.ToLower( (char)cl );

			bool ret;

			if( (ch & 0x80) != 0 )
			{
				ret = ch == cl;
				return isNeg ? !ret : ret;
			}
			
			switch( cl )
			{
			case (byte)'a':
				ret = (ch >= (byte)'a' && ch <= (byte)'z') ||
					(ch >= (byte)'A' && ch <= (byte)'Z');
				break;

			case (byte)'c': 
				ret = (ch >= 0 && ch <= 31) || ch == 127;
				break;

			case (byte)'d':
				ret = ch >= (byte)'0' && ch <= (byte)'9';
				break;

			case (byte)'g':
				ret = ch >= 33 && ch <= 126;
				break;
			
			case (byte)'l':
				ret = ch >= (byte)'a' && ch <= (byte)'z';
				break;
			
			case (byte)'p':
				ret = (ch >= 33 && ch <= 47) ||
					(ch >= 58 && ch <= 64) ||
					(ch >= 91 && ch <= 96) ||
					(ch >= 123 && ch <= 126);
				break;
			
			case (byte)'s':
				ret = (ch >= 9 && ch <= 13) || ch == (byte)' ';
				break;

			case (byte)'u':
				ret = ch >= (byte)'A' && ch <= (byte)'Z';
				break;

			case (byte)'w':
				ret = (ch >= (byte)'a' && ch <= (byte)'z') ||
					(ch >= (byte)'A' && ch <= (byte)'Z') ||
					(ch >= (byte)'0' && ch <= (byte)'9');
				break;

			case (byte)'x':
				ret = (ch >= (byte)'0' && ch <= (byte)'9') ||
					(ch >= (byte)'a' && ch <= (byte)'f') ||
					(ch >= (byte)'A' && ch <= (byte)'F');
				break;

			case (byte)'z':
				ret = ch == 0;
				break;
			
			default:
				return ch == cl;
			}

			if( isNeg )
				ret = !ret;

			return ret;
		}

		private static bool MatchBracketClass( ref MatchState ms, byte ch, int pPos, int epPos )
		{
			var pat = ms.Pat;

			bool sig = true;
			if( pat[pPos + 1] == (byte)'^' )
			{
				sig = false;
				pPos++;
			}

			while( ++pPos < epPos )
			{
				var pp = pat[pPos];

				if( pp == (byte)'%' )
				{
					pPos++;
					if( MatchClass( ch, pat[pPos] ) )
						return sig;
				}
				else if( pPos + 2 < epPos && pat[pPos + 1] == (byte)'-' )
				{
					pPos += 2;
					if( pp <= ch && ch <= pat[pPos] )
						return sig;
				}
				else if( pp == ch )
				{
					return sig;
				}
			}

			return !sig;
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
		}

		private static int MatchCapture( ref MatchState ms, int sPos, byte l )
		{
			l -= (byte)'1';

			if( l < 0 || l >= ms.Level || ms.Captures[l].Len == CapUnfinished )
				throw new ArgumentException( "invalid capture index" );

			var cap = ms.Captures[l];
			var str = ms.Str;

			if( str.Length - sPos >= cap.Len &&
				Helpers.MemEq( str, sPos, str, cap.Pos, cap.Len ) )
			{
				return sPos + cap.Len;
			}
			
			return -1;
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
			int i = 0;
			while( SingleMatch( ref ms, sPos + i, pPos, ep ) )
				i++;

			while( i >= 0 )
			{
				var res = SMatch( ref ms, sPos + i, ep + 1 );
				if( res != -1 )
					return res;
				i--;
			}

			return -1;
		}

		private static int MinExpand( ref MatchState ms, int sPos, int pPos, int ep )
		{
			for( ; ; )
			{
				var res = SMatch( ref ms, sPos, ep + 1 );
				if( res != -1 )
					return res;
				else if( SingleMatch( ref ms, sPos, pPos, ep ) )
					sPos++;
				else
					return -1;
			}
		}
	}
}
