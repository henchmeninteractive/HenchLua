using System;

using Debug = System.Diagnostics.Debug;

namespace Henchmen.Lua
{
	public class Thread
	{
		/// <summary>
		/// The minimum amount of stack space available
		/// to C functions when they're called.
		/// </summary>
		public const int MinStack = 20;

		private Value[] stack = new Value[MinStack * 2];
		private int stackTop;

		#region Stack operations

		public int StackTop
		{
			get { return stackTop - call.StackBase; }
			set
			{
				if( value < 0 )
					throw new ArgumentOutOfRangeException( "Top" );

				int oldTop = stackTop;
				int newTop = call.StackBase + value;

				if( newTop > stack.Length )
					throw new ArgumentOutOfRangeException( "Top", "New top overflows the stack." );

				int start = Math.Min( oldTop, newTop );
				int end = Math.Max( oldTop, newTop );

				for( int i = start; i < end; i++ )
					stack[i].RefVal = null;

				stackTop = newTop;
			}
		}

		public void CheckSpace( int spaceNeeded )
		{
			if( spaceNeeded < 0 )
				throw new ArgumentOutOfRangeException( "spaceNeeded" );

			CheckStack( stackTop + spaceNeeded );
		}

		public int AbsIndex( int index )
		{
			if( index == 0 )
				return 0;

			if( index < 0 )
				index = StackTop + index + 1;

			return index;
		}

		internal int RealIndex( int index )
		{
			if( index == 0 )
				throw new ArgumentOutOfRangeException( "index" );

			if( index < 0 )
			{
				index = stackTop + index;
				if( index < call.StackBase )
					throw new ArgumentOutOfRangeException( "index" );
			}
			else
			{
				index = call.StackBase + index - 1;
				if( index >= stackTop )
					throw new ArgumentOutOfRangeException( "index" );
			}

			return index;
		}

		/// <summary>
		/// Gets or sets a value on the stack.
		/// </summary>
		/// <param name="index">
		/// The index to get or set. Positive indexes refer to elements
		/// from the bottom of the stack up (so index 1 is the lowest element
		/// on the stack). Negative indices refer to elements from the
		/// top of the stack down (so -1 is the top element on the stack).
		/// Zero is always an invalid index.
		/// </param>
		/// <remarks>
		/// If you attempt to read beyond the top or bottom of the stack,
		/// you'll get 
		/// </remarks>
		public Value this[int index]
		{
			get
			{
				if( index == 0 )
					throw new ArgumentOutOfRangeException( "index" );

				if( index < 0 )
				{
					index = stackTop + index;
					if( index < call.StackBase )
						return new Value();
				}
				else
				{
					index = call.StackBase + index - 1;
					if( index >= stackTop )
						return new Value();
				}

				return stack[index];
			}
			set { stack[RealIndex( index )] = value; }
		}

		public void Push( Value value )
		{
			if( stackTop == stack.Length )
				throw new InvalidOperationException( "Lua stack overflow." );

			stack[stackTop] = value;

			stackTop = stackTop + 1;
		}

		public void Push( Value value0, Value value1 )
		{
			if( stackTop == stack.Length )
				throw new InvalidOperationException( "Lua stack overflow." );

			stack[stackTop + 0] = value0;
			stack[stackTop + 1] = value1;

			stackTop = stackTop + 2;
		}

		public void Push( Value value0, Value value1, Value value2 )
		{
			if( stackTop == stack.Length )
				throw new InvalidOperationException( "Lua stack overflow." );

			stack[stackTop + 0] = value0;
			stack[stackTop + 1] = value1;
			stack[stackTop + 2] = value2;

			stackTop = stackTop + 3;
		}

		public void Push( Value value0, Value value1, Value value2, Value value3 )
		{
			if( stackTop == stack.Length )
				throw new InvalidOperationException( "Lua stack overflow." );

			stack[stackTop + 0] = value0;
			stack[stackTop + 1] = value1;
			stack[stackTop + 2] = value2;
			stack[stackTop + 3] = value3;

			stackTop = stackTop + 4;
		}

		public void Insert( int index, Value value )
		{
			if( stackTop == stack.Length )
				throw new InvalidOperationException( "Lua stack overflow." );

			index = RealIndex( index );

			Array.Copy( stack, index, stack, index + 1, stackTop - index );
			stack[index] = value;

			stackTop = stackTop + 1;
		}

		public void Remove( int index )
		{
			index = RealIndex( index );

			Array.Copy( stack, index + 1, stack, index, stackTop - index );
			stack[--stackTop].RefVal = null;
		}
			
		public void Pop()
		{
			int newTop = stackTop - 1;
			if( newTop < call.StackBase )
				throw new InvalidOperationException( "Can't pop more values than exist on the current frame." );

			stackTop = newTop;

			stack[newTop].RefVal = null;
		}
			
		public void Pop( int count )
		{
			if( count < 0 )
				throw new ArgumentOutOfRangeException( "count" );
			if( count == 0 )
				return;

			int newTop = stackTop - count;
			if( newTop < call.StackBase )
				throw new InvalidOperationException( "Can't pop more values than exist on the current frame." );

			for( int i = newTop; i < stackTop; i++ )
				stack[i].RefVal = null;

			stackTop = newTop;
		}

		public Value PopValue()
		{
			int newTop = stackTop - 1;
			if( newTop < call.StackBase )
				throw new InvalidOperationException( "Can't pop more values than exist on the current frame." );

			stackTop = newTop;

			var ret = stack[newTop];

			stack[newTop].RefVal = null;

			return ret;
		}

		//helpers to make it easy to set up the stack for a return
		//each overload returns the number of arguments given

		public int SetStack( Value v0 )
		{
			StackTop = 1;
			stack[call.StackBase + 0] = v0;

			return 1;
		}

		public int SetStack( Value v0, Value v1 )
		{
			StackTop = 2;
			stack[call.StackBase + 0] = v0;
			stack[call.StackBase + 1] = v1;

			return 2;
		}

		public int SetStack( Value v0, Value v1, Value v2 )
		{
			StackTop = 3;
			stack[call.StackBase + 0] = v0;
			stack[call.StackBase + 1] = v1;
			stack[call.StackBase + 2] = v2;

			return 3;
		}

		public int SetStack( Value v0, Value v1, Value v2, Value v3 )
		{
			StackTop = 4;
			stack[call.StackBase + 0] = v0;
			stack[call.StackBase + 1] = v1;
			stack[call.StackBase + 2] = v2;
			stack[call.StackBase + 3] = v3;

			return 4;
		}

		//like SetStack, however these may later be optimized
		//to return directly to the caller's stack - these must
		//only be called by callbacks which immediately return
		//the value returned by this function (which may or may
		//not be the number of results set!)

		public int SetNilReturnValue()
		{
			StackTop = 1;
			stack[call.StackBase + 0].RefVal = null;
			return 1;
		}

		public int SetReturnValues( Value v0 )
		{
			StackTop = 1;
			stack[call.StackBase + 0] = v0;

			return 1;
		}

		public int SetReturnValues( Value v0, Value v1 )
		{
			StackTop = 2;
			stack[call.StackBase + 0] = v0;
			stack[call.StackBase + 1] = v1;

			return 2;
		}

		public int SetReturnValues( Value v0, Value v1, Value v2 )
		{
			StackTop = 3;
			stack[call.StackBase + 0] = v0;
			stack[call.StackBase + 1] = v1;
			stack[call.StackBase + 2] = v2;

			return 3;
		}

		public Value[] GetStackElements( int index0, int index1 )
		{
			if( index0 <= 0 || index1 <= 0 )
				throw new ArgumentOutOfRangeException( "Only base-relative indexes are allowed." );

			if( index1 < index0 )
				return new Value[0];

			index0 = RealIndex( index0 );
			index1 = RealIndex( index1 );

			int count = index1 - index0 + 1;

			var ret = new Value[count];
			Array.Copy( stack, index0, ret, 0, count );

			return ret;
		}

		#endregion

		private void CheckStack( int minLen )
		{
			if( stack.Length < minLen )
			{
				int growLen = stack.Length;
				growLen = growLen < 256 ? growLen * 2 : growLen + MinStack;
				Array.Resize( ref stack, Math.Max( growLen, minLen ) );
			}
		}

		private CallInfo call = new CallInfo() { StackTop = -1 }; //the top of the call stack
		private CallInfo[] callInfos = new CallInfo[16]; //the rest of the call stack
		private int numCallInfos; //the number of items in the call stack
		
		private struct CallInfo
		{
			public int StackBase;
			public int StackTop;

			/// <summary>
			/// The currently executing op.
			/// </summary>
			public int PC;

			public object Callable;

			/// <summary>
			/// Where to find the function's varargs.
			/// </summary>
			public int VarArgsIndex;

			/// <summary>
			/// Where to stick the results.
			/// </summary>
			public int ResultIndex;
			/// <summary>
			/// How many results we want.
			/// </summary>
			public int ResultCount;
		}

		/// <summary>
		/// Runs the code at the top of the callstack.
		/// </summary>
		internal void Execute()
		{
			Value[] upValues = null;
			var proto = call.Callable as Proto;

			if( proto == null )
			{
				var asClosure = call.Callable as Closure;
				if( asClosure == null )
				{
					ExecuteUserCode();
					return;
				}

				proto = asClosure.Proto;
				upValues = asClosure.UpValues;
			}

			var stackBase = call.StackBase;

			var code = proto.Code;
			var consts = proto.Constants;

			for( int pc = call.PC; pc < code.Length; call.PC = ++pc )
			{
				var op = code[pc];
				
				var opCode = op.OpCode;
				switch( opCode )
				{
				case OpCode.Move:
					stack[stackBase + op.A] = stack[stackBase + op.B];
					break;

				case OpCode.LoadConstant:
					stack[stackBase + op.A] = consts[op.Bx];
					break;

				case OpCode.LoadConstantEx:
					{
						var op2 = code[++pc];
						Debug.Assert( op2.OpCode == OpCode.ExtraArg );
						stack[stackBase + op.A] = consts[op2.Ax];
					}
					break;

				case OpCode.LoadBool:
					stack[stackBase + op.A].RefVal =
						op.B != 0 ? Value.TrueTag : Value.FalseTag;
					if( op.C != 0 )
						pc++;
					break;

				case OpCode.LoadNil:
					{
						int a = stackBase + op.A;
						int b = op.B;
						do { stack[a++].SetNil(); } while( b-- != 0 );
					}
					break;

				case OpCode.GetUpValue:
					ReadUpValue( ref upValues[op.B], out stack[stackBase + op.A] );
					break;

				case OpCode.GetUpValueTable:
					{
						int c = op.C;
						var key = (c & Instruction.BitK) != 0 ?
							consts[c & ~Instruction.BitK] :
							stack[stackBase + c];

						Value upVal;
						ReadUpValue( ref upValues[op.B], out upVal );

						GetTable( upVal, ref key, stackBase + op.A );
					}
					break;

				case OpCode.GetTable:
					{
						int c = op.C;
						var key = (c & Instruction.BitK) != 0 ?
							consts[c & ~Instruction.BitK] :
							stack[stackBase + c];

						GetTable( stack[stackBase + op.B],
							ref key, stackBase + op.A );
					}
					break;

				case OpCode.SetUpValueTable:
					{
						int b = op.B;
						var key = (b & Instruction.BitK) != 0 ?
							consts[b & ~Instruction.BitK] :
							stack[stackBase + b];

						int c = op.C;
						var value = (c & Instruction.BitK) != 0 ?
							consts[c & ~Instruction.BitK] :
							stack[stackBase + c];

						Value upVal;
						ReadUpValue( ref upValues[op.A], out upVal );

						SetTable( upVal, ref key, ref value );
					}
					break;

				case OpCode.SetUpValue:
					WriteUpValue( ref upValues[op.B], ref stack[stackBase + op.A] );
					break;

				case OpCode.SetTable:
					{
						int b = op.B;
						var key = (b & Instruction.BitK) != 0 ?
							consts[b & ~Instruction.BitK] :
							stack[stackBase + b];

						int c = op.C;
						var value = (c & Instruction.BitK) != 0 ?
							consts[c & ~Instruction.BitK] :
							stack[stackBase + c];

						SetTable( stack[stackBase + op.A],
							ref key, ref value );
					}
					break;

				case OpCode.NewTable:
					{
						int nArr = Helpers.FbToInt( op.B );
						int nNod = Helpers.FbToInt( op.C );

						stack[stackBase + op.A].RefVal = new Table( nArr, nNod );
					}
					break;

				case OpCode.Self:
					{
						stack[stackBase + op.A + 1] = stack[stackBase + op.B];
						var table = stack[stackBase + op.B];

						int c = op.C;
						var key = (c & Instruction.BitK) != 0 ?
							consts[c & ~Instruction.BitK] :
							stack[stackBase + c];

						GetTable( table, ref key, stackBase + op.A );
					}
					break;

				case OpCode.Add:
				case OpCode.Sub:
				case OpCode.Mul:
				case OpCode.Div:
				case OpCode.Mod:
				case OpCode.Pow:
					{
						var ib = op.B;
						var b = (ib & Instruction.BitK) != 0 ?
							consts[ib & ~Instruction.BitK] :
							stack[stackBase + ib];

						var ic = op.C;
						var c = (ic & Instruction.BitK) != 0 ?
							consts[ic & ~Instruction.BitK] :
							stack[stackBase + ic];

						if( b.RefVal == Value.NumTypeTag &&
							c.RefVal == Value.NumTypeTag )
						{
							double rv, bv = b.NumVal, cv = c.NumVal;

							switch( op.OpCode )
							{
							case OpCode.Add: rv = bv + cv; break;
							case OpCode.Sub: rv = bv - cv; break;
							case OpCode.Mul: rv = bv * cv; break;
							case OpCode.Div: rv = bv / cv; break;
							case OpCode.Mod: rv = bv % cv; break;
							case OpCode.Pow: rv = Math.Pow( bv, cv ); break;
							default: Debug.Assert( false ); rv = 0; break;
							}

							stack[stackBase + op.A].Set( rv );
						}
						else
						{
							DoArith( TagMethods.Add + (opCode - OpCode.Add),
								b, c, out stack[stackBase + op.A] );
						}
					}
					break;

				case OpCode.Negate:
					{
						var ib = op.B;
						var b = (ib & Instruction.BitK) != 0 ?
							consts[ib & ~Instruction.BitK] :
							stack[stackBase + ib];

						if( b.RefVal == Value.NumTypeTag )
						{
							stack[stackBase + op.A].Set( -b.NumVal );
						}
						else
						{
							DoArith( TagMethods.Unm, b, b,
								out stack[stackBase + op.A] );
						}
					}
					break;

				case OpCode.Not:
					{
						var ib = op.B;
						var b = (ib & Instruction.BitK) != 0 ?
							consts[ib & ~Instruction.BitK].RefVal :
							stack[stackBase + ib].RefVal;

						var res = b == null || b == Value.FalseTag;
						stack[stackBase + op.A].RefVal = res ? Value.TrueTag : Value.FalseTag;
					}
					break;

				case OpCode.Len:
					{
						var ib = op.B;
						var b = (ib & Instruction.BitK) != 0 ?
							consts[ib & ~Instruction.BitK] :
							stack[stackBase + ib];

						DoGetLen( ref b, out stack[stackBase + op.A] );
					}
					break;

				case OpCode.Concat:
					DoConcat( stackBase + op.B, op.C - op.B + 1, out stack[stackBase + op.A] );
					break;

				case OpCode.Jmp:
					{
						var a = op.A;
						if( a != 0 )
							CloseUpValues( stackBase + a - 1 );
						pc += op.SBx;
					}
					break;

				case OpCode.Eq:
				case OpCode.Lt:
				case OpCode.Le:
					{
						int b = op.B;
						var bv = (b & Instruction.BitK) != 0 ?
							consts[b & ~Instruction.BitK] :
							stack[stackBase + b];

						int c = op.C;
						var cv = (c & Instruction.BitK) != 0 ?
							consts[c & ~Instruction.BitK] :
							stack[stackBase + c];

						bool test;

						switch( opCode )
						{
						case OpCode.Eq:
							test = bv.RefVal == cv.RefVal ?
								bv.RefVal != Value.NumTypeTag || bv.NumVal == cv.NumVal :
								Equal( ref bv, ref cv );
							break;

						case OpCode.Lt:
							test = (bv.RefVal == Value.NumTypeTag && cv.RefVal == Value.NumTypeTag) ?
								bv.NumVal < cv.NumVal :
								Less( ref bv, ref cv );
							break;

						case OpCode.Le:
							test = (bv.RefVal == Value.NumTypeTag && cv.RefVal == Value.NumTypeTag) ?
								bv.NumVal <= cv.NumVal :
								LessEqual( ref bv, ref cv );
							break;

						default: Debug.Assert( false ); test = false; break;
						}

						if( test != (op.A != 0) )
						{
							pc++;
						}
						else
						{
							op = code[++pc];
							Debug.Assert( op.OpCode == OpCode.Jmp );
							goto case OpCode.Jmp;
						}
					}
					break;

				case OpCode.Test:
					{
						var a = stack[stackBase + op.A].RefVal;
						var test = a == null || a == Value.FalseTag;

						if( (op.C != 0) == test )
						{
							pc++;
						}
						else
						{
							op = code[++pc];
							Debug.Assert( op.OpCode == OpCode.Jmp );
							goto case OpCode.Jmp;
						}
					}
					break;

				case OpCode.TestSet:
					{
						var b = stack[stackBase + op.B].RefVal;
						var test = b == null || b == Value.FalseTag;

						if( (op.C != 0) == test )
						{
							pc++;
						}
						else
						{
							stack[stackBase + op.A] = stack[stackBase + op.B];

							op = code[++pc];
							Debug.Assert( op.OpCode == OpCode.Jmp );
							goto case OpCode.Jmp;
						}
					}
					break;

				case OpCode.Call:
					{
						int funcIdx = stackBase + op.A;

						int numArgs = op.B - 1;
						if( numArgs == -1 )
							numArgs = stackTop - funcIdx - 1;

						int numRetVals = op.C - 1;

						call.PC++; //return to the next instruction
						BeginCall( funcIdx, numArgs, numRetVals ); //valid because CallReturnAll == -1

						Execute();

						EndCall();
					}
					break;

				case OpCode.TailCall:
					{
						//ToDo: actually properly implement the tail call...

						if( proto.InnerProtos != null )
							CloseUpValues( stackBase );

						int funcIdx = stackBase + op.A;
						object func = stack[funcIdx].RefVal;

						int numArgs = op.B - 1;
						if( numArgs == -1 )
							numArgs = stackTop - funcIdx - 1;

						int resultIndex = call.ResultIndex;

						call.PC++; //return to the next instruction
						BeginCall( funcIdx, numArgs, call.ResultCount );
						call.ResultIndex = resultIndex;

						Execute();

						EndCall();

						pc = code.Length;
					}
					break;

				case OpCode.Return:
					{
						if( proto.InnerProtos != null )
							CloseUpValues( stackBase );

						var a = stackBase + op.A;
						var b = op.B;

						int numRet =  b != 0 ? b - 1 : stackTop - a;

						var retIdx = call.ResultIndex;
						var retCount = call.ResultCount;
						
						if( retCount != CallReturnAll && numRet > retCount )
							numRet = retCount;

						if( retIdx != a )
						{
							for( int i = 0; i < numRet; i++ )
								stack[retIdx + i] = stack[a + i];
						}

						if( retCount == CallReturnAll )
						{
							stackTop = retIdx + numRet;
						}
						else
						{
							for( int i = numRet; i < retCount; i++ )
								stack[retIdx + i].RefVal = null;
						}

						pc = code.Length;
					}
					break;

				case OpCode.ForLoop:
					{
						var ai = stackBase + op.A;

						Debug.Assert( stack[ai + 0].RefVal == Value.NumTypeTag );
						Debug.Assert( stack[ai + 1].RefVal == Value.NumTypeTag );
						Debug.Assert( stack[ai + 2].RefVal == Value.NumTypeTag );

						var idx = stack[ai].NumVal;
						var limit = stack[ai + 1].NumVal;
						var step = stack[ai + 2].NumVal;

						idx += step;

						if( step < 0 ? idx >= limit : idx <= limit )
						{
							pc += op.SBx;
							stack[ai].NumVal = idx;
							stack[ai + 3].Set( idx );
						}
					}
					break;

				case OpCode.ForPrep:
					{
						int iInit = stackBase + op.A;
						int iLimit = iInit + 1;
						int iStep = iInit + 2;

						if( !ToNumber( ref stack[iInit] ) )
							throw new InvalidOperandException( "Initial value of for loops must be a number." );
						if( !ToNumber( ref stack[iLimit] ) )
							throw new InvalidOperandException( "Limit value of for loops must be a number." );
						if( !ToNumber( ref stack[iStep] ) )
							throw new InvalidOperandException( "Step value of for loops must be a number." );

						stack[iInit].NumVal -= stack[iStep].NumVal;

						pc += op.SBx;
					}
					break;

				case OpCode.TForCall:
					{
						int ia = stackBase + op.A;

						int callBase = ia + 3;
						stack[callBase + 2] = stack[ia + 2];
						stack[callBase + 1] = stack[ia + 1];
						stack[callBase + 0] = stack[ia + 0];

						stackTop = callBase + 3;

						int numRet = op.C;

						call.PC = ++pc;

						BeginCall( callBase, 2, numRet );
						call.ResultIndex = callBase;

						Execute();

						EndCall();

						op = code[pc];
						Debug.Assert( op.OpCode == OpCode.TForLoop );
					}
					goto case OpCode.TForLoop;

				case OpCode.TForLoop:
					{
						int ia = stackBase + op.A;
						if( stack[ia + 1].RefVal != null )
						{
							stack[ia] = stack[ia + 1];
							pc += op.SBx;
						}
					}
					break;

				case OpCode.SetList:
					{
						var ia = stackBase + op.A;

						var n = op.B;
						var c = op.C;

						if( n == 0 )
							n = stackTop - ia - 1;

						if( c == 0 )
						{
							var opEx = code[++pc];
							Debug.Assert( opEx.OpCode == OpCode.ExtraArg );
							c = opEx.Ax;
						}

						var table = (Table)stack[ia];

						var last = (c - 1) * Instruction.FieldsPerFlush + n;

						if( last > table.ArrayCapacity )
							table.Resize( last, table.NodeCapacity );
						var tableArray = table.array;

						for( ; n > 0; n-- )
							tableArray[--last].Set( ref stack[ia + n] );

						//stackTop = call.Top;
					}
					break;

				case OpCode.Closure:
					stack[stackBase + op.A].RefVal =
						CreateClosure( proto.InnerProtos[op.Bx], upValues );
					break;

				case OpCode.Vararg:
					{
						int srcIdx = call.VarArgsIndex;
						int destIdx = stackBase + op.A;

						int numVarArgs = call.StackBase - srcIdx;
						int numWanted = op.B - 1;

						if( numWanted == -1 )
						{
							numWanted = numVarArgs;
							stackTop = destIdx + numWanted;
						}

						int numHad = numWanted < numVarArgs ? numWanted : numVarArgs;
						for( int i = 0; i < numHad; i++ )
							stack[destIdx + i] = stack[srcIdx + i];

						for( int i = numHad; i < numWanted; i++ )
							stack[destIdx + i].RefVal = null;
					}
					break;

				default:
					throw new InvalidBytecodeException();
				}
			}
		}

		private void DoGetLen( ref Value val, out Value ret )
		{
			var asStr = val.RefVal as byte[];
			if( asStr != null )
			{
				ret.RefVal = Value.NumTypeTag;
				ret.NumVal = asStr.Length - LString.BufferDataOffset;

				return;
			}

			var asTable = val.RefVal as Table;
			if( asTable != null )
			{
				ret.RefVal = Value.NumTypeTag;
				ret.NumVal = asTable.GetLen();

				return;
			}

			throw new NotImplementedException();
		}

		private void DoConcat( int index, int count, out Value ret )
		{
			Debug.Assert( count >= 2 );

			int top = index + count;

			do
			{
				int numCat = 2;

				if( !ValToStr( ref stack[top - 1] ) ||
					!ValToStr( ref stack[top - 2] ) )
				{
					throw new NotImplementedException();
				}
				else
				{
					//top two values are strings
					//see if we have more

					int total =
						(stack[top - 1].RefVal as byte[]).Length +
						(stack[top - 2].RefVal as byte[]).Length;

					while( numCat < count )
					{
						int idx = top - numCat - 1;

						if( !ValToStr( ref stack[idx] ) )
							break;

						total += (stack[idx].RefVal as byte[]).Length;
						numCat++;
					}

					total -= numCat * LString.BufferDataOffset;

					var catBuf = LString.InternalAllocBuffer( total );
					
					for( int o = LString.BufferDataOffset, i = 0; i < numCat; i++ )
					{
						var str = stack[top - numCat + i].RefVal as byte[];
						var len = str.Length - LString.BufferDataOffset;

						Array.Copy( str, LString.BufferDataOffset, catBuf, o, len );

						o += len;
					}

					var catStr = LString.InternalFinishBuffer( catBuf );

					stack[top - numCat].RefVal = catStr.InternalData;
				}

				count -= numCat - 1; //numCat string sturned into 1
				top -= numCat - 1;
			}
			while( count != 1 );

			ret = stack[index];
		}

		private bool ValToStr( ref Value val )
		{
			if( val.RefVal is byte[] )
				return true;

			if( val.RefVal != Value.NumTypeTag )
				return false;

			int len = Helpers.NumToStr( fmtBuf, 0, val.NumVal, 14 );
			val = new LString( fmtBuf, 0, len );

			return true;
		}

		public void ConvertToString( ref Value val )
		{
			if( !ValToStr( ref val ) )
				val = new Value();
		}

		private byte[] fmtBuf = new byte[32];
		internal Libs.StringLib.MatchCaptureRec[] matchCapCache;
		
		private byte[] strBuilderBuf;
		internal struct StrBuilder
		{
			public byte[] Buffer;
			public int Length;

			public void Append( byte b )
			{
				if( Length == Buffer.Length )
					Array.Resize( ref Buffer, Buffer.Length * 2 );

				Buffer[Length++] = b;
			}

			public void Append( byte[] b, int idx, int len )
			{
				var newLen = Length + len;
				if( newLen > Buffer.Length )
					Array.Resize( ref Buffer, Math.Max( Buffer.Length * 2, newLen ) );

				Array.Copy( b, idx, Buffer, Length, len );
				Length = newLen;
			}

			public void Append( LString str )
			{
				if( str.Length != 0 )
					Append( str.InternalData, LString.BufferDataOffset, str.Length );
			}

			public LString ToLString()
			{
				return new LString( Buffer, 0, Length );
			}
		}

		internal StrBuilder GetStrBuilder( int minCapacity )
		{
			StrBuilder ret;

			if( strBuilderBuf != null && strBuilderBuf.Length >= minCapacity )
			{
				ret.Buffer = strBuilderBuf;
				strBuilderBuf = null;
			}
			else
			{
				ret.Buffer = new byte[minCapacity];
			}

			ret.Length = 0;

			return ret;
		}

		internal void RetireStrBuilder( StrBuilder b )
		{
			Debug.Assert( b.Buffer != null );
			if( strBuilderBuf == null || strBuilderBuf.Length < b.Buffer.Length )
				strBuilderBuf = b.Buffer;
		}

		private void ExecuteUserCode()
		{
			int numRet = CallUserCode();

			int retSrc = stackTop - numRet;

			int retIndex = call.ResultIndex;
			int retCount = call.ResultCount;

			if( retCount != CallReturnAll && numRet > retCount )
				numRet = retCount;
			
			var stack = this.stack;

			for( int i = 0; i < numRet; i++ )
				stack[retIndex + i] = stack[retSrc + i];

			if( retCount == CallReturnAll )
			{
				stackTop = retIndex + numRet;
			}
			else
			{
				for( int i = numRet; i < retCount; i++ )
					stack[retIndex + i].RefVal = null;
			}
		}

		private int CallUserCode()
		{
			var asFunc = call.Callable as UserFunction;
			if( asFunc != null )
				return asFunc.Execute( this );

			var asCb = call.Callable as UserCallback;
			if( asCb != null )
				return asCb( this );

			throw new ArgumentException( "Attempting to call a non-callable object." );
		}

		private void BeginCall( int funcIdx, int numArgs, int numResults )
		{
			var callable = stack[funcIdx].RefVal;

			if( callable == null )
				throw new ArgumentNullException( "Attempt to call a nil value." );

			var proto = callable as Proto;

			if( proto == null )
			{
				var asClosure = callable as Closure;
				if( asClosure != null )
					proto = asClosure.Proto;

				if( proto == null && !Callable.IsUserCallable( callable ) )
					throw new ArgumentException( "Attempting to call a non-callable object." );
			}

			int numVarArgs = 0;
			if( proto != null && proto.HasVarArgs )
				numVarArgs = Math.Max( numArgs - proto.NumParams, 0 );

			int newStackBase = funcIdx + 1;

			if( numVarArgs != 0 )
				newStackBase += numArgs;

			int maxStack = proto != null ? proto.MaxStack : Thread.MinStack;

			int newStackTop = newStackBase + maxStack;
			CheckStack( newStackTop );

			if( numVarArgs != 0 )
			{
				//got at least proto.NumParams on the stack
				//move them to the right spot

				for( int i = 0; i < proto.NumParams; i++ )
				{
					int srcIdx = funcIdx + 1 + i;

					stack[newStackBase + i] = stack[srcIdx];
					stack[srcIdx].RefVal = null;
				}
			}
			else if( proto != null )
			{
				//complete the missing args

				for( int i = numArgs; i < proto.NumParams; i++ )
					stack[newStackBase + i].RefVal = null;
			}

			if( numCallInfos == callInfos.Length )
				Array.Resize( ref callInfos, callInfos.Length + 16 );
			callInfos[numCallInfos++] = call;

			call.Callable = callable;
			call.StackBase = newStackBase;
			call.StackTop = proto != null ? newStackBase + proto.MaxStack : -1;

			call.PC = 0;

			call.ResultIndex = funcIdx;
			call.ResultCount = numResults;

			call.VarArgsIndex = newStackBase - numVarArgs;

			stackTop = newStackBase + numArgs;
		}

		private void EndCall()
		{
			Debug.Assert( numCallInfos != 0 );
			call = callInfos[--numCallInfos];
		}

		private void CallMetaMethod( object metaMethod, ref Value arg0, ref Value arg1, out Value ret )
		{
			int saveTop = stackTop;

			int stackBase = call.StackTop != -1 ? call.StackTop : stackTop;
			CheckStack( stackBase + 3 );

			stack[stackBase].RefVal = metaMethod;
			stack[stackBase + 1] = arg0;
			stack[stackBase + 2] = arg1;

			BeginCall( stackBase, 2, 1 );

			Execute();

			ret = stack[stackBase];

			EndCall();

			stackTop = saveTop;
		}

		private void CallMetaMethod2( object metaMethod, ref Value arg0, ref Value arg1, ref Value arg2 )
		{
			int saveTop = stackTop;

			int stackBase = call.StackTop != -1 ? call.StackTop : stackTop;
			CheckStack( stackBase + 4 );

			stack[stackBase].RefVal = metaMethod;
			stack[stackBase + 1] = arg0;
			stack[stackBase + 2] = arg1;
			stack[stackBase + 3] = arg2;

			BeginCall( stackBase, 3, 0 );

			Execute();

			EndCall();

			stackTop = saveTop;
		}

		private const int MaxMtLoop = 100;

		private Value GetTable( Value obj, ref Value key )
		{
			Value ret;

			for( int i = 0; i < MaxMtLoop; i++ )
			{
				Table metaTable;

				var asTable = obj.RefVal as Table;
				if( asTable != null )
				{
					int loc = asTable.FindValue( key );
					if( loc != 0 )
					{
						asTable.ReadValue( loc, out ret );
						if( ret.RefVal != null )
							return ret;
					}

					metaTable = asTable.metatable;

					if( metaTable == null )
						return new Value();
				}
				else
				{
					metaTable = GetMetatable( ref obj );

					if( metaTable == null )
						throw new InvalidOperandException( "Can't index the given value." );
				}

				var index = metaTable[Literals.TagMethod_Index];

				if( Callable.IsCallable( index.RefVal ) )
				{
					CallMetaMethod( index.RefVal, ref obj, ref key, out ret );
					return ret;
				}

				obj = index;				
			}

			throw new LuaException( "Very long metatable __index cycle." );
		}

		private void GetTable( Value obj, ref Value key, int retStkIdx )
		{
			/*
				This MUST NOT be rewritten as a single line!

				Doing so is incorrect because the left-hand
				expression (most pertinently, "stack") is
				evaluated before the right-hand expression.
				This is a problem because GetTable can change
				the value of stack (growing it, for instance,
				to accommodate a metamethod call), and the return
				value must land in the NEW stack.
			*/
			
			var val = GetTable( obj, ref key );
			stack[retStkIdx] = val;
		}

		/// <summary>
		/// Performs a full table get, invoking the __index
		/// metamethod if appropriate.
		/// </summary>
		public Value GetTable( IHasMetatable table, Value key )
		{
			if( table == null )
				throw new ArgumentNullException( "table" );

			Value tVal;
			tVal.RefVal = table;
			tVal.NumVal = 0;

			return GetTable( tVal, ref key );
		}

		private void SetTable( Value obj, ref Value key, ref Value value )
		{
			for( int i = 0; i < MaxMtLoop; i++ )
			{
				var newIndex = new Value();

				var asTable = obj.RefVal as Table;
				if( asTable != null )
				{
					int loc = asTable.FindValue( key );
					if( asTable.IsLocNilOrEmpty( loc ) )
						newIndex = GetMetamethod( ref obj, Literals.TagMethod_NewIndex );

					if( newIndex.RefVal == null )
					{
						if( loc == 0 )
							loc = asTable.InsertNewKey( new CompactValue( key ) );

						asTable.WriteValue( loc, ref value );
						
						return;
					}
				}
				else
				{
					newIndex = GetMetamethod( ref obj, Literals.TagMethod_NewIndex );

					if( newIndex.RefVal == null )
						throw new InvalidOperandException( "Can't index the given value." );
				}

				if( Callable.IsCallable( newIndex.RefVal ) )
				{
					CallMetaMethod2( newIndex.RefVal, ref obj, ref key, ref value );
					return;
				}

				obj = newIndex;
			}
		}

		public void SetTable( IHasMetatable table, Value key, Value value )
		{
			if( table == null )
				throw new ArgumentNullException( "table" );
			
			Value tVal;
			tVal.RefVal = table;
			tVal.NumVal = 0;

			SetTable( tVal, ref key, ref value );
		}

		private Table GetMetatable( ref Value val )
		{
			var asTable = val.RefVal as Table;
			if( asTable != null )
				return asTable.metatable;

			var asHasMt = val.RefVal as IHasMetatable;
			if( asHasMt != null )
				return asHasMt.Metatable;

			return null;
		}

		private Value GetMetamethod( ref Value val, LString tmName )
		{
			var mt = GetMetatable( ref val );
			return mt != null ? mt[tmName] : Value.Nil;
		}

		private bool ToNumber( ref Value val )
		{
			if( val.RefVal == Value.NumTypeTag )
				return true;

			var asStr = val.RefVal as byte[];
			if( asStr != null )
			{
				if( Helpers.StrToNum( asStr, LString.BufferDataOffset,
					asStr.Length - LString.BufferDataOffset, out val.NumVal ) )
				{
					val.RefVal = Value.NumTypeTag;
					return true;
				}
			}

			return false;
		}

		private void DoArith( TagMethods opCode, Value a, Value b, out Value ret )
		{
			if( ToNumber( ref a ) && (opCode == TagMethods.Unm || ToNumber( ref b ) ) )
			{
				double na = a.NumVal, nb = b.NumVal, rv;

				switch( opCode )
				{
				case TagMethods.Add: rv = na + nb; break;
				case TagMethods.Sub: rv = na - nb; break;
				case TagMethods.Mul: rv = na * nb; break;
				case TagMethods.Div: rv = na / nb; break;
				case TagMethods.Mod: rv = na % nb; break;
				case TagMethods.Pow: rv = Math.Pow( na, nb ); break;

				case TagMethods.Unm: rv = -na; break;

				default: Debug.Assert( false ); rv = 0; break;
				}

				ret.RefVal = Value.NumTypeTag;
				ret.NumVal = rv;

				return;
			}

			var opName = Literals.TagMethodNames[(int)opCode];

			throw new NotImplementedException();
		}

		private bool Equal( ref Value a, ref Value b )
		{
			var asStr = a.RefVal as byte[];
			if( asStr != null )
				return LString.InternalEquals( asStr, b.RefVal as byte[] );

			if( a.RefVal == Value.TrueTag || a.RefVal == Value.FalseTag )
				return b.RefVal == a.RefVal;

			//and now we go into the full eq

			var ta = a.ValueType;
			var tb = b.ValueType;

			if( ta != tb || (ta != LValueType.Table && ta != LValueType.UserData) )
				return false;

			//grab the metamethods

			var eqa = GetMetamethod( ref a, Literals.TagMethod_Eq );
			var eqb = GetMetamethod( ref b, Literals.TagMethod_Eq );

			if( eqa != eqb || eqa.IsNil )
				return false;

			Value ret;
			CallMetaMethod( eqa.RefVal, ref a, ref b, out ret );

			return ret.ToBool();
		}

		private bool Less( ref Value a, ref Value b )
		{
			var asStrA = a.RefVal as byte[];
			var asStrB = b.RefVal as byte[];

			if( asStrA != null && asStrB != null )
			{
				//here we break from standard Lua - we
				//do a culture-insensitive comparison
				//where it does not (culture-sensitive
				//sorting would generate garbage)

				return LString.InternalCompareOrdinal( asStrA, asStrB ) < 0;
			}

			throw new NotImplementedException();
		}

		internal bool Less( Value a, Value b, Callable less )
		{
			if( less.IsNotNil )
			{
				SetStack( less, a, b );
				Call( 2, 1 );
				return PopValue().ToBool();
			}

			if( a.RefVal == Value.NumTypeTag && b.RefVal == Value.NumTypeTag )
				return a.NumVal < b.NumVal;

			return Less( ref a, ref b );
		}

		private bool LessEqual( ref Value a, ref Value b )
		{
			var asStrA = a.RefVal as byte[];
			var asStrB = b.RefVal as byte[];

			if( asStrA != null && asStrB != null )
			{
				//here we break from standard Lua - we
				//do a culture-insensitive comparison
				//where it does not (culture-sensitive
				//sorting would generate garbage)

				return LString.InternalCompareOrdinal( asStrA, asStrB ) <= 0;
			}

			throw new NotImplementedException();
		}

		private void ReadUpValue( ref Value upVal, out Value ret )
		{
			if( upVal.RefVal == Value.OpenUpValueTag )
			{
				ret = stack[(int)upVal.NumVal];
				return;
			}

			var asClosed = upVal.RefVal as ValueBox;
			if( asClosed != null )
			{
				ret = asClosed.Value;
				return;
			}

			//simple value
			ret = upVal;
		}

		private void WriteUpValue( ref Value upVal, ref Value value )
		{
			if( upVal.RefVal == Value.OpenUpValueTag )
			{
				stack[(int)upVal.NumVal] = value;
				return;
			}

			var asClosed = upVal.RefVal as ValueBox;
			if( asClosed != null )
			{
				asClosed.Value = value;
				return;
			}

			//simple value
			upVal = value;
		}

		private struct OpenUpValue
		{
			public Value[] UpValueStorage;
			public int UpValueIndex;
			public int StackIndex;
		}

		private OpenUpValue[] openUpValues = new OpenUpValue[32];
		private int numOpenUpValues;

		internal void RegisterOpenUpvalue( Value[] storage, int index, int stackIndex )
		{
			if( numOpenUpValues == openUpValues.Length )
				Array.Resize( ref openUpValues, openUpValues.Length + 32 );

			OpenUpValue rec;
			rec.UpValueStorage = storage;
			rec.UpValueIndex = index;
			rec.StackIndex = stackIndex;

			//insert it in the right spot
			int idx = 0;
			int max = numOpenUpValues - 1;

			while( idx <= max )
			{
				int mid = (idx + max) / 2;
				int cmp = openUpValues[mid].StackIndex - stackIndex;

				if( cmp == 0 )
				{
					idx = mid;
					break;
				}

				if( cmp > 0 )
					max = mid - 1;
				else
					idx = mid + 1;
			}

			Debug.Assert( numOpenUpValues == 0 ||
				(idx == 0 || openUpValues[idx - 1].StackIndex <= rec.StackIndex) &&
				(idx == numOpenUpValues || openUpValues[idx].StackIndex >= rec.StackIndex) );

			if( idx < numOpenUpValues )
				Array.Copy( openUpValues, idx, openUpValues, idx + 1, numOpenUpValues - idx );

			openUpValues[idx] = rec;

			numOpenUpValues++;
		}

		/// <summary>
		/// Closes all open upvalues pointing to stack locations >= index.
		/// </summary>
		private void CloseUpValues( int index )
		{
			int i;
			for( i = numOpenUpValues - 1; i >= 0; i-- )
			{
				var rec = openUpValues[i];

				if( rec.StackIndex < index )
					break;

				var stk = stack[rec.StackIndex];

				var asUpValRef = stk.RefVal as Value[];
				if( asUpValRef == null )
				{
					//values initially close to simple values

					rec.UpValueStorage[rec.UpValueIndex] = stk;

					if( !(stk.RefVal is ValueBox) )
					{
						//keep track of where we closed this value to
						//if another open value closes on this slot, we
						//need to properly box it and fix both references

						stack[rec.StackIndex].RefVal = rec.UpValueStorage;
						stack[rec.StackIndex].NumVal = rec.UpValueIndex;
					}
				}
				else
				{
					//the value's already been closed to a simple value

					int upIdx = (int)stk.NumVal;
					var box = new ValueBox { Value = asUpValRef[upIdx] };
					
					asUpValRef[upIdx].RefVal = box;
					rec.UpValueStorage[rec.UpValueIndex].RefVal = box;

					//we don't want to go through this path each time
					//so we put the box on the stack, any further upvalues
					//closing on this slot will take the short path above

					stack[rec.StackIndex].RefVal = box;
				}

				openUpValues[i].UpValueStorage = null;
			}

			//restore the original stack values (ToDo: find out if this is necessary)

			for( int j = i + 1; j < numOpenUpValues; j++ )
			{
				var stkIdx = openUpValues[j].StackIndex;
				var stk = stack[stkIdx];

				var asUpValRef = stk.RefVal as Value[];
				if( asUpValRef != null )
				{
					stack[stkIdx] = asUpValRef[(int)stk.NumVal];
					continue;
				}

				var asBox = stk.RefVal as ValueBox;
				if( asBox != null )
				{
					stack[stkIdx] = asBox.Value;
					continue;
				}
			}

			//and... done!

			numOpenUpValues = i + 1;
		}

		private Function CreateClosure( Proto proto, Value[] parentUpValues )
		{
			var stackBase = call.StackBase;

			var upValDesc = proto.UpValues;
			if( upValDesc == null )
				//we don't wrap simple protos in full closures
				return proto;

			var upValues = new Value[upValDesc.Length];
			for( int i = 0; i < upValDesc.Length; i++ )
			{
				var desc = upValDesc[i];
				if( desc.Kind == UpValueKind.StackPointing )
				{
					//create an open upvalue
					int stackIndex = stackBase + desc.Index;

					upValues[i].RefVal = Value.OpenUpValueTag;
					upValues[i].NumVal = stackIndex;

					RegisterOpenUpvalue( upValues, i, stackIndex );
				}
				else
				{
					var parentVal = parentUpValues[desc.Index];
					
					if( parentVal.RefVal == Value.OpenUpValueTag )
					{
						RegisterOpenUpvalue( upValues, i, (int)parentVal.NumVal );
					}
					else if( desc.Kind != UpValueKind.ValueCopying )
					{
						//this is the pint where we need to force simple upvalues closed

						var asClosed = parentVal.RefVal as ValueBox;
						if( asClosed == null )
						{
							asClosed = new ValueBox() { Value = parentVal };
							parentVal.RefVal = asClosed;

							parentUpValues[desc.Index].RefVal = asClosed;
						}
					}

					upValues[i] = parentVal;
				}
			}

			return new Closure() { Proto = proto, UpValues = upValues };
		}

		public const int CallReturnAll = -1;

		public void Call( int numArgs, int numResults )
		{
			if( numArgs < 0 )
				throw new ArgumentOutOfRangeException( "numArgs" );
			if( numResults != CallReturnAll && numResults < 0 )
				throw new ArgumentOutOfRangeException( "numResults" );

			int fnIndex = stackTop - numArgs - 1;
			if( fnIndex < call.StackBase )
				throw new ArgumentOutOfRangeException( "numArgs", "Trying to call a function with more arguments than have been pushed onto the stack." );

			BeginCall( fnIndex, numArgs, numResults );

			if( numResults != CallReturnAll && call.ResultIndex + numResults > stack.Length )
			{
				EndCall();
				throw new ArgumentOutOfRangeException( "numArgs", "The function call would overflow the stack." );
			}

			Execute();

			if( numResults != CallReturnAll )
				stackTop = call.ResultIndex + numResults;

			EndCall();
		}

		public void Call( Callable func, int numArgs, int numResults )
		{
			if( numArgs < 0 )
				throw new ArgumentOutOfRangeException( "numArgs" );
			if( numResults != CallReturnAll && numResults < 0 )
				throw new ArgumentOutOfRangeException( "numResults" );

			if( numArgs != 0 )
				Insert( -numArgs, func );
			else
				Push( func );

			Call( numArgs, numResults );
		}
	}
}
