using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Debug = System.Diagnostics.Debug;

namespace LuaSharp
{
	public abstract class Function
	{
		public static Function Load( Stream byteCode, Table globals )
		{
			if( byteCode == null )
				throw new ArgumentNullException( "byteCode" );

			//load the chunk header, validate

			var header = Header.Load( byteCode );
			var expected = Header.ExpectedHeader();

			if( header.Signature != Header.LuaBSignature )
				throw new InvalidDataException( "The stream doesn't contain Lua bytecode." );
			if( header.Version != Header.LuaVersion5_2 ||
				header.Format != Header.StandardFormat )
				throw new InvalidDataException( "The bytecode version doesn't match the runtime." );

			if( header.IsLittleEndian != expected.IsLittleEndian ||
				header.SizeOfInt != expected.SizeOfInt ||
				header.SizeOfSizeT != expected.SizeOfSizeT ||
				header.SizeOfInstruction != expected.SizeOfInstruction ||
				header.SizeOfNumber != expected.SizeOfNumber ||
				header.NumbersAreInts != expected.NumbersAreInts )
				throw new InvalidDataException( "The bytecode was compiled for a different platform." );

			if( header.Tail != Header.LuaCTail )
				throw new InvalidDataException( "The bytecode is corrupt. It may have been treated as a text file by some tool." );

			var reader = new BinaryReader( byteCode );

			var proto = Proto.Load( reader );

			if( proto.UpValues == null )
				return proto;

			if( proto.UpValues.Length != 1 )
				throw new InvalidBytecodeException( "Chunks can't have more than one upvalue." );

			return new Closure() { Proto = proto, UpValues = new Value[] { globals } };
		}

		private struct Header
		{
			public const uint LuaBSignature = 0x1B4C7561;
			public const byte LuaVersion5_2 = 5 * 16 + 2;
			public const byte StandardFormat = 0;
			public const ulong LuaCTail = 0x000019930D0A1A0A;

			public uint Signature;

			public byte Version;
			public byte Format;

			public bool IsLittleEndian;
			public byte SizeOfInt;
			public byte SizeOfSizeT;
			public byte SizeOfInstruction;
			public byte SizeOfNumber;
			public bool NumbersAreInts;

			public ulong Tail;

			public static Header ExpectedHeader()
			{
				Header ret;

				ret.Signature = LuaBSignature;

				ret.Version = LuaVersion5_2;
				ret.Format = StandardFormat;

				ret.IsLittleEndian = true;

				ret.SizeOfInt = 4;
				ret.SizeOfSizeT = 4;
				ret.SizeOfInstruction = 4;
				ret.SizeOfNumber = 8;
				ret.NumbersAreInts = false;

				ret.Tail = LuaCTail;

				return ret;
			}

			public static Header Load( Stream stream )
			{
				Header ret;

				ret.Signature = (uint)LoadStr( stream, 4 );

				ret.Version = ReadByte( stream );
				ret.Format = ReadByte( stream );

				ret.IsLittleEndian = ReadByte( stream ) != 0;

				ret.SizeOfInt = ReadByte( stream );
				ret.SizeOfSizeT = ReadByte( stream );
				ret.SizeOfInstruction = ReadByte( stream );
				ret.SizeOfNumber = ReadByte( stream );
				ret.NumbersAreInts = ReadByte( stream ) != 0;

				ret.Tail = LoadStr( stream, 6 );

				return ret;
			}

			private static ulong LoadStr( Stream stream, int n )
			{
				ulong ret = 0;

				for( int i = 0; i < n; i++ )
					ret = (ret << 8) | ReadByte( stream );

				return ret;
			}

			private static byte ReadByte( Stream stream )
			{
				int b = stream.ReadByte();
				if( b == -1 )
					throw new EndOfStreamException();

				Debug.Assert( b >= 0 && b < 0xFF );

				return (byte)b;
			}
		}
	}

	internal class Closure : Function
	{
		internal Proto Proto;

		/// <summary>
		/// Upvalues are specially encoded. They have three forms:
		/// 
		/// The first form is the open form. In the open form the upvalue
		/// points at a slot on the stack. These are represented by Values
		/// where RefVal == OpenUpValueTag, and NumVal == the stack index.
		/// 
		/// The second form is the closed form. These are long-lived upvalues
		/// which are boxed. They're Values where RefVal is a ValueBox.
		/// 
		/// The last form is as simple values.
		/// 
		/// Open upvalues must be registered with the thread whose stack they
		/// point at. Copying an open upvalue (when constructing a new closure)
		/// creates another, identical, open upvalue, which must also be registerd
		/// with the thread.
		/// 
		/// Copying a closed upvalue simply copies the variable. The .NET GC
		/// can handle the rest.
		/// 
		/// Copying a simple value promotes the simple value to a closed upvalue,
		/// and then proceeds as though it's a normal copy of a closed upvalue.
		/// This is because both upvalues are conceptually the same object, and we
		/// can't just mutate the value, in case it changes.
		/// 
		/// There is one final case: when closing an open upvalue to which no
		/// other upvalues point, we can convert it to a simple value rather
		/// than to a full closed upvalue. This can alleviate some allocation
		/// overhead when functions construct closures that capture their arguments.
		/// </summary>
		internal Value[] UpValues;
	}
}
