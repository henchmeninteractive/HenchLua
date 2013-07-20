using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Debug = System.Diagnostics.Debug;

namespace LuaSharp
{
	[Serializable]
	internal struct Instruction
	{
		public uint PackedValue;

		private const int OpCodeShift = 0;
		private const int OpCodeMask = 0x3F;

		public OpCode OpCode
		{
			get { return (OpCode)((PackedValue >> OpCodeShift) & OpCodeMask); }
			set { SetField( (int)value, OpCodeShift, OpCodeMask ); }
		}

		private const int AShift = 6;
		private const int AMask = 0xFF;

		public int A
		{
			get { return (int)(PackedValue >> AShift) & AMask; }
			set { SetField( value, AShift, AMask ); }
		}

		private const int BShift = 23;
		private const int BMask = 0x1FF;

		public int B
		{
			get { return (int)(PackedValue >> BShift) & BMask; }
			set { SetField( value, BShift, BMask ); }
		}

		private const int CShift = 14;
		private const int CMask = 0x1FF;

		public int C
		{
			get { return (int)(PackedValue >> CShift) & CMask; }
			set { SetField( value, CShift, CMask ); }
		}

		private const int BxShift = 14;
		private const int BxMask = 0x3FFFF;

		public int Bx
		{
			get { return (int)(PackedValue >> BxShift) & BxMask; }
			set { SetField( value, BxShift, BxMask ); }
		}

		private const int AxShift = 6;
		private const int AxMask = 0x3FFFFFF;

		public int Ax
		{
			get { return (int)(PackedValue >> AxShift) & AxMask; }
			set { SetField( value, AxShift, AxMask ); }
		}

		private void SetField( int value, int shift, int mask )
		{
			Debug.Assert( (value & ~mask) == 0 );
			PackedValue = (uint)(PackedValue & ~(mask << shift)) | (uint)(value << shift);
		}
	}

	/// <remarks>
	/// RK: register if high bit clear, else constant.
	/// 
	/// TableGet( Table, Key ) -> Table[Key]
	/// TableSet( Table, Key, Value )
	/// </remarks>
	[Serializable]
	internal enum OpCode
	{
		/// <summary>
		/// A B, R[A]= R[B]
		/// </summary>
		Move,
		
		/// <summary>
		/// A Bx, R[A] = K[Bx]
		/// </summary>
		LoadConstant,

		/// <summary>
		/// A, R[A] = K[extra arg]
		/// </summary>
		/// <remarks>
		/// the next 'instruction' is always ExtraArg.
		/// </remarks>
		LoadConstantEx,

		/// <summary>
		/// A B C, R[A] = (bool)B; if C -> pc++
		/// </summary>
		LoadBool,

		/// <summary>
		/// A B, R[A..A+B] = nil
		/// </summary>
		LoadNil,

		/// <summary>
		/// A B, R[A] = U[B]
		/// </summary>
		GetUpValue,

		/// <summary>
		/// A B C, R[A] = TableGet( U[B], RK[C] )
		/// </summary>
		GetUpValueTable,

		/// <summary>
		/// A B C, R[A] = GetTable( R[B], RK[C] )
		/// </summary>
		GetTable,

		/// <summary>
		/// A B C, SetTable( U[A], RK[B], RK[C] )
		/// </summary>
		SetUpValueTable,

		/// <summary>
		/// A B, U[B] = R[A]
		/// </summary>
		SetUpValue,
		
		/// <summary>
		/// A B C, SetTable( R[A], RK[B], RK[C]
		/// </summary>
		SetTable,

		/// <summary>
		/// A B C, R[A] = NewTable( FloatByte( B ) array slots, FloatByte( C ) objects )
		/// </summary>
		NewTable,

		/// <summary>
		/// A B C, R[A+1] = R[B]; R[A] = GetTable( R[B], RK[C] )
		/// </summary>
		Self,

		/// <summary>
		/// A B C, R[A] = RK[B] + RK[C]
		/// </summary>
		Add,
		
		/// <summary>
		/// A B C, R[A] = RK[B] - RK[C]
		/// </summary>
		Sub,

		/// <summary>
		/// A B C, R[A] = RK[B] * RK[C]
		/// </summary>
		Mul,

		/// <summary>
		/// A B C, R[A] = RK[B] / RK[C]
		/// </summary>
		Div,

		/// <summary>
		/// A B C, R[A] = RK[B] % RK[C]
		/// </summary>
		Mod,

		/// <summary>
		/// A B C, R[A] = RK[B] ^ RK[C]
		/// </summary>
		Pow,

		/// <summary>
		/// A B, R[A] = -RK[B]
		/// </summary>
		Negate,

		/// <summary>
		/// A B, R[A] = Not( RK[B] )
		/// </summary>
		Not,

		/// <summary>
		/// A B, R[A] = Length( RK[B] )
		/// </summary>
		Len,

		/// <summary>
		/// A B C, R[A] = R[B] .. ... .. R[C]
		/// </summary>
		Concat,

		/// <summary>
		/// A sBx, pc += sBx; if A -> close all upvalues >= R[A] + 1
		/// </summary>
		Jmp,
		
		/// <summary>
		/// A B C, if( (RK[B] == RK[C]) != A ) -> pc++
		/// </summary>
		Eq,
		
		/// <summary>
		/// A B C, if( (RK[B] &lt; RK[C]) != A ) -> pc++
		/// </summary>
		Lt,

		/// <summary>
		/// A B C, if( (RK[B] &lt;= RK[C]) != A ) -> pc++
		/// </summary>
		Le,

		/// <summary>
		/// A C, if( (bool)R[A] != A ) -> pc++
		/// </summary>
		Test,
		/// <summary>
		/// A B C, if( (bool)R[B] == C ) -> R[A] = R[B] else pc++
		/// </summary>
		TestSet,

		/// <summary>
		/// A B C, R[A..A+C-2] = Call( R[A], R[A+1..A+B-1] )
		/// </summary>
		/// <remarks>
		/// if (B == 0) then B = top. If (C == 0), then `top' is set to last_result+1,
		/// so next open instruction (OP_CALL, OP_RETURN, OP_SETLIST) may use `top'.
		/// </remarks>
		Call,
		
		/// <summary>
		/// A B, return Call( R[A], R[A+1..A+B-1] )
		/// </summary>
		TailCall,

		/// <summary>
		/// A B, return R[A..A+B-2]
		/// </summary>
		/// <remarks>
		/// if (B == 0) then return up to `top'.
		/// </remarks>
		Return,

		/// <summary>
		/// A sBx, R[A] += R[A+2]; if( R[A] &lt;?= R[A+1] ) -> { pc += sBx, R[A+3] = R[A] }
		/// </summary>
		ForLoop,
		/// <summary>
		/// A sBx, R[A] -= R[A+2]; pc += sBx
		/// </summary>
		ForPrep,

		/// <summary>
		/// A C, R(A+3), ... ,R(A+2+C) := R(A)(R(A+1), R(A+2));
		/// </summary>
		TForCall,
		/// <summary>
		/// A sBx, if R(A+1) ~= nil then { R(A)=R(A+1); pc += sBx }
		/// </summary>
		TForLoop,

		/// <summary>
		/// A B C, R(A)[(C-1)*FPF+i] := R(A+i), 1 &lt;= i &lt;= B
		/// </summary>
		SetList,

		/// <summary>
		/// A Bx, R[A] = Closure( Proto[Bx] )
		/// </summary>
		Closure,

		/// <summary>
		/// A B, R[A..A+B-2] = vararg
		/// </summary>
		/// <remarks>
		/// if (B == 0) then use actual number of varargs and
		/// set top (like in OP_CALL with C == 0).
		/// </remarks>
		Vararg,

		/// <summary>
		/// Ax, Ax is an argument for the previous opcode
		/// </summary>
		ExtraArg,
	}
}
