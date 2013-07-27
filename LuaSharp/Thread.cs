using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Debug = System.Diagnostics.Debug;

namespace LuaSharp
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

		public struct StackOps
		{
			private Thread owner;

			internal StackOps( Thread owner )
			{
				this.owner = owner;
			}

			public int Top
			{
				get { return owner.stackTop - owner.call.StackBase; }
			}

			public void CheckSpace( int spaceNeeded )
			{
				if( spaceNeeded < 0 )
					throw new ArgumentOutOfRangeException( "spaceNeeded" );

				int minLen = owner.stackTop + spaceNeeded;

				if( owner.stack.Length < minLen )
				{
					int growLen = owner.stack.Length;
					growLen = growLen < 256 ? growLen * 2 : growLen + MinStack;
					Array.Resize( ref owner.stack, Math.Max( growLen, minLen ) );
				}
			}

			public void Push( Value value )
			{
				owner.stack[owner.stackTop++] = value;
			}
		}

		public StackOps Stack { get { return new StackOps( this ); } }

		private CallInfo call; //the top of the call stack
		private CallInfo[] callInfos; //the rest of the call stack
		private int numCallInfos; //the number of items in the call stack
		
		private struct CallInfo
		{
			public int StackBase;
			public int Top;

			/// <summary>
			/// The currently executing op.
			/// </summary>
			public int PC;

			public object Callable;
		}

		/// <summary>
		/// Runs the code at the top of the callstack.
		/// </summary>
		internal void Execute()
		{
			var stackBase = call.StackBase;

			Value[] upValues = null;
			var proto = call.Callable as Proto;

			if( proto == null )
			{
				var asClosure = call.Callable as Closure;
				Debug.Assert( asClosure != null );

				proto = asClosure.Proto;
				upValues = asClosure.UpValues;
			}
			
			var code = proto.Code;
			var consts = proto.Constants;

			for( int pc = call.PC; pc < code.Length; call.PC = ++pc )
			{
				var op = code[pc];

				switch( op.OpCode )
				{
				case OpCode.Move:
					stack[stackBase + op.A] = stack[stackBase + op.B];
					break;

				case OpCode.LoadConstant:
					stack[stackBase + op.A] = consts[op.Bx];
					break;

				case OpCode.LoadConstantEx:
					{
						var op2 = code[pc++];
						Debug.Assert( op2.OpCode == OpCode.ExtraArg );
						stack[stackBase + op.A] = consts[op2.Ax];
					}
					break;

				case OpCode.LoadBool:
					stack[stackBase + op.A].RefVal =
						op.B != 0 ? BoolBox.True : BoolBox.False;
					if( op.C != 0 )
						pc++;
					break;

				case OpCode.LoadNil:
					{
						int a = stackBase + op.A;
						int b = op.B;
						do { stack[a].SetNil(); } while( b-- != 0 );
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

						var table = (Table)upVal.RefVal;
						GetTable( table, ref key, out stack[stackBase + op.A] );
					}
					break;

				case OpCode.GetTable:
					{
						int c = op.C;
						var key = (c & Instruction.BitK) != 0 ?
							consts[c & ~Instruction.BitK] :
							stack[stackBase + c];

						GetTable( stack[stackBase + op.B].RefVal,
							ref key, out stack[stackBase + op.A] );
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
						ReadUpValue( ref upValues[op.B], out upVal );

						var table = (Table)upVal.RefVal;
						SetTable( table, ref key, ref value );
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

						SetTable( stack[stackBase + op.A].RefVal,
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
						var table = stack[stackBase + op.B].RefVal;

						int c = op.C;
						var key = (c & Instruction.BitK) != 0 ?
							consts[c & ~Instruction.BitK] :
							stack[stackBase + c];

						GetTable( table, ref key, out stack[stackBase + op.A] );
					}
					break;

				case OpCode.Add:
					{
						var b = stack[stackBase + op.B];
						var c = stack[stackBase + op.C];

						if( b.RefVal == Value.NumTypeTag &&
							c.RefVal == Value.NumTypeTag )
						{
							stack[stackBase + op.A].Set( b.NumVal + c.NumVal );
						}
						else
						{
							DoArith( TypeInfo.TagMethod_Add, b.RefVal, c.RefVal,
								out stack[stackBase + op.A] );
						}
					}
					break;

				case OpCode.Sub:
					{
						var b = stack[stackBase + op.B];
						var c = stack[stackBase + op.C];

						if( b.RefVal == Value.NumTypeTag &&
							c.RefVal == Value.NumTypeTag )
						{
							stack[stackBase + op.A].Set( b.NumVal - c.NumVal );
						}
						else
						{
							DoArith( TypeInfo.TagMethod_Sub, b.RefVal, c.RefVal,
								out stack[stackBase + op.A] );
						}
					}
					break;

				case OpCode.Mul:
					{
						var b = stack[stackBase + op.B];
						var c = stack[stackBase + op.C];

						if( b.RefVal == Value.NumTypeTag &&
							c.RefVal == Value.NumTypeTag )
						{
							stack[stackBase + op.A].Set( b.NumVal * c.NumVal );
						}
						else
						{
							DoArith( TypeInfo.TagMethod_Mul, b.RefVal, c.RefVal,
								out stack[stackBase + op.A] );
						}
					}
					break;

				case OpCode.Div:
					{
						var b = stack[stackBase + op.B];
						var c = stack[stackBase + op.C];

						if( b.RefVal == Value.NumTypeTag &&
							c.RefVal == Value.NumTypeTag )
						{
							stack[stackBase + op.A].Set( b.NumVal / c.NumVal );
						}
						else
						{
							DoArith( TypeInfo.TagMethod_Div, b.RefVal, c.RefVal,
								out stack[stackBase + op.A] );
						}
					}
					break;

				case OpCode.Mod:
					{
						var b = stack[stackBase + op.B];
						var c = stack[stackBase + op.C];

						if( b.RefVal == Value.NumTypeTag &&
							c.RefVal == Value.NumTypeTag )
						{
							//yes, this is the correct mod formula
							stack[stackBase + op.A].Set( b.NumVal % c.NumVal );
						}
						else
						{
							DoArith( TypeInfo.TagMethod_Mod, b.RefVal, c.RefVal,
								out stack[stackBase + op.A] );
						}
					}
					break;

				case OpCode.Pow:
					{
						var b = stack[stackBase + op.B];
						var c = stack[stackBase + op.C];

						if( b.RefVal == Value.NumTypeTag &&
							c.RefVal == Value.NumTypeTag )
						{
							stack[stackBase + op.A].Set( Math.Pow( b.NumVal, c.NumVal ) );
						}
						else
						{
							DoArith( TypeInfo.TagMethod_Add, b.RefVal, c.RefVal,
								out stack[stackBase + op.A] );
						}
					}
					break;

				case OpCode.Negate:
					{
						var b = stack[stackBase + op.B];
						if( b.RefVal == Value.NumTypeTag )
						{
							stack[stackBase + op.A].Set( -b.NumVal );
						}
						else
						{
							DoArith( TypeInfo.TagMethod_Unm, b.RefVal, b.RefVal,
								out stack[stackBase + op.A] );
						}
					}
					break;

				case OpCode.Not:
					{
						var b = stack[stackBase + op.B].RefVal;
						var res = b == null || b == BoolBox.False;
						stack[stackBase + op.A].RefVal = res ? BoolBox.True : BoolBox.False;
					}
					break;

				case OpCode.Len:
					throw new NotImplementedException();

				case OpCode.Concat:
					throw new NotImplementedException();

				case OpCode.Jmp:
					{
						var a = op.A;
						if( a != 0 )
							CloseUpValues( stackBase + a - 1 );
						pc += op.SBx;
					}
					break;

				case OpCode.Eq:
					{
						int b = op.B;
						var bv = (b & Instruction.BitK) != 0 ?
							consts[b & ~Instruction.BitK] :
							stack[stackBase + b];

						int c = op.C;
						var cv = (c & Instruction.BitK) != 0 ?
							consts[c & ~Instruction.BitK] :
							stack[stackBase + c];

						bool test =
							(bv.RefVal == cv.RefVal &&
							(bv.RefVal != Value.NumTypeTag || bv.NumVal == cv.NumVal)) ||
							Equal( ref bv, ref cv );

						if( test != (op.A != 0) )
						{
							pc++;
						}
						else
						{
							op = code[pc++];
							Debug.Assert( op.OpCode == OpCode.Jmp );
							goto case OpCode.Jmp;
						}
					}
					break;

				case OpCode.Lt:
					{
						int b = op.B;
						var bv = (b & Instruction.BitK) != 0 ?
							consts[b & ~Instruction.BitK] :
							stack[stackBase + b];

						int c = op.C;
						var cv = (c & Instruction.BitK) != 0 ?
							consts[c & ~Instruction.BitK] :
							stack[stackBase + c];

						bool test =
							(bv.RefVal == Value.NumTypeTag &&
							cv.RefVal == Value.NumTypeTag &&
							bv.NumVal < cv.NumVal) ||
							Less( ref bv, ref cv );

						if( test != (op.A != 0) )
						{
							pc++;
						}
						else
						{
							op = code[pc++];
							Debug.Assert( op.OpCode == OpCode.Jmp );
							goto case OpCode.Jmp;
						}
					}
					break;

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

						bool test =
							(bv.RefVal == Value.NumTypeTag &&
							cv.RefVal == Value.NumTypeTag &&
							bv.NumVal <= cv.NumVal) ||
							LessEqual( ref bv, ref cv );

						if( test != (op.A != 0) )
						{
							pc++;
						}
						else
						{
							op = code[pc++];
							Debug.Assert( op.OpCode == OpCode.Jmp );
							goto case OpCode.Jmp;
						}
					}
					break;

				case OpCode.Test:
					{
						var a = stack[stackBase + op.A].RefVal;
						var test = a == null || a == BoolBox.False;

						if( (op.C != 0) == test )
						{
							pc++;
						}
						else
						{
							op = code[pc++];
							Debug.Assert( op.OpCode == OpCode.Jmp );
							goto case OpCode.Jmp;
						}
					}
					break;

				case OpCode.TestSet:
					{
						var b = stack[stackBase + op.B].RefVal;
						var test = b == null || b == BoolBox.False;

						if( (op.C != 0) == test )
						{
							pc++;
						}
						else
						{
							stack[stackBase + op.A] = stack[stackBase + op.B];

							op = code[pc++];
							Debug.Assert( op.OpCode == OpCode.Jmp );
							goto case OpCode.Jmp;
						}
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
					throw new NotImplementedException();

				case OpCode.TForCall:
					throw new NotImplementedException();
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
							var opEx = code[pc++];
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

						stackTop = call.Top;
					}
					break;

				case OpCode.Closure:
					stack[stackBase + op.A].RefVal =
						CreateClosure( proto.InnerProtos[op.Bx], upValues );
					break;

				case OpCode.Vararg:
					throw new NotImplementedException();

				default:
					throw new InvalidBytecodeException();
				}
			}
		}

		private void GetTable( object obj, ref Value key, out Value value )
		{
			//ToDo: this should be a full get

			var table = obj as Table;

			int loc = table.FindValue( key );
			table.ReadValue( loc, out value );
		}

		private void SetTable( object obj, ref Value key, ref Value value )
		{
			//ToDo: this should be a full set

			var table = obj as Table;

			int loc = table.FindValue( key );
			table.WriteValue( loc, ref value );
		}

		private void DoArith( String opName, object a, object b, out Value ret )
		{
			throw new NotImplementedException();
		}

		private bool Equal( ref Value a, ref Value b )
		{
			throw new NotImplementedException();
		}

		private bool Less( ref Value a, ref Value b )
		{
			throw new NotImplementedException();
		}

		private bool LessEqual( ref Value a, ref Value b )
		{
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
			public Value[] storage;
			public int Index;
		}



		internal void RegisterOpenUpvalue( Value[] storage, int index )
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Closes all open upvalues pointing to stack locations >= index.
		/// </summary>
		private void CloseUpValues( int index )
		{
			throw new NotImplementedException();
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
				if( desc.InStack )
				{
					//create an open upvalue
					upValues[i].RefVal = Value.OpenUpValueTag;
					upValues[i].NumVal = stackBase + desc.Index;

					RegisterOpenUpvalue( upValues, i );
				}
				else
				{
					//point at a parent upvalue
					var parentVal = parentUpValues[desc.Index];

					var asClosed = parentVal.RefVal as ValueBox;
					if( asClosed == null )
					{
						//we need to force the value closed

						if( parentVal.RefVal == Value.OpenUpValueTag )
						{
							//convert an open upval to a simple value

							parentVal = stack[(int)parentVal.NumVal];
							//unmark it here or just skip it later?
						}

						//conver the simple value to a fully closed upvalue

						asClosed = new ValueBox( parentVal );
						parentUpValues[desc.Index].RefVal = asClosed;
					}

					upValues[i].RefVal = asClosed;
				}
			}

			return new Closure() { Proto = proto, UpValues = upValues };
		}
	}
}
