using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Debug = System.Diagnostics.Debug;

namespace LuaSharp
{
	internal class Proto : Function
	{
		internal Instruction[] Code;

		internal Value[] Constants;
		internal UpValDesc[] UpValues;

		internal Proto[] InnerProtos;

		internal struct UpValDesc
		{
#if DEBUG_API
			public String Name;
#endif

			public bool InStack;
			public byte Index;
		}

		internal byte NumParams;
		internal byte MaxStack;
		internal bool HasVarArgs;

#if DEBUG_API
		private int[] lineInfo;
		LocalVarDesc[] localVars;

		private struct LocalVarDesc
		{
			public String Name;
			public int StartPC, endPC;
		}

		String source;
#endif

		private int startLine, endLine;

		internal static Proto Load( BinaryReader reader )
		{
			var lineDefined = reader.ReadInt32();
			var lastLineDefined = reader.ReadInt32();

			var numParams = reader.ReadByte();
			var hasVarArgs = reader.ReadByte() != 0;
			var maxStack = reader.ReadByte();

			//code

			var codeLen = reader.ReadInt32();
			var code = new Instruction[codeLen];
			for( int i = 0; i < code.Length; i++ )
				code[i].PackedValue = reader.ReadUInt32();

			//constants

			Value[] constants;

			var numConsts = reader.ReadInt32();
			if( numConsts != 0 )
			{
				constants = new Value[numConsts];
				for( int i = 0; i < constants.Length; i++ )
				{
					var type = reader.ReadByte();
					switch( type )
					{
					case 0: //nil
						break;

					case 1: //bool
						constants[i].Set( reader.ReadByte() != 0 );
						break;

					case 3: //number
						constants[i].Set( reader.ReadDouble() );
						break;

					case 4: //string
						constants[i].Set( LoadString( reader ) );
						break;

					default:
						throw new InvalidDataException( "Invalid constant type." );
					}
				}
			}
			else
			{
				constants = null;
			}

			//inner functions

			Proto[] innerProtos;

			int numInnerProtos = reader.ReadInt32();
			if( numInnerProtos != 0 )
			{
				innerProtos = new Proto[numInnerProtos];
				for( int i = 0; i < innerProtos.Length; i++ )
					innerProtos[i] = Load( reader );
			}
			else
			{
				innerProtos = null;
			}

			//upvalues

			UpValDesc[] upValues;

			int numUpValues = reader.ReadInt32();
			if( numUpValues != 0 )
			{
				upValues = new UpValDesc[numUpValues];
				for( int i = 0; i < upValues.Length; i++ )
				{
					upValues[i].InStack = reader.ReadByte() != 0;
					upValues[i].Index = reader.ReadByte();
				}
			}
			else
			{
				upValues = null;
			}

			//debug info
#if DEBUG_API
			var source = LoadString( reader );

			int[] lineInfos;

			int numLineInfos = reader.ReadInt32();
			if( numLineInfos != 0 )
			{
				lineInfos = new int[numLineInfos];
				for( int i = 0; i < lineInfos.Length; i++ )
					lineInfos[i] = reader.ReadInt32();
			}
			else
			{
				lineInfos = null;
			}

			LocalVarDesc[] localVars;

			int numLocalVars = reader.ReadInt32();
			if( numLocalVars != 0 )
			{
				localVars = new LocalVarDesc[numLocalVars];
				for( int i = 0; i < numLocalVars; i++ )
				{
					localVars[i].Name = LoadString( reader );
					localVars[i].StartPC = reader.ReadInt32();
					localVars[i].endPC = reader.ReadInt32();
				}
			}
			else
			{
				localVars = null;
			}

			int numUpValueInfos = reader.ReadInt32();
			if( numUpValueInfos > numUpValues )
				throw new InvalidDataException( "Too much debug info." );

			for( int i = 0; i < numUpValueInfos; i++ )
				upValues[i].Name = LoadString( reader );
#else
			SkipString( reader ); //source
			int numLineInfos = reader.ReadInt32();
			reader.BaseStream.Seek( numLineInfos * 4, SeekOrigin.Current );
			int numLocalVars = reader.ReadInt32();
			for( int i = 0; i < numLocalVars; i++ )
			{
				SkipString( reader );
				reader.BaseStream.Seek( 8, SeekOrigin.Current );
			}
			int numUpValueInfos = reader.ReadInt32();
			if( numUpValueInfos > numUpValues )
				throw new InvalidDataException( "Too much debug info." );
			for( int i = 0; i < numUpValueInfos; i++ )
				SkipString( reader );
#endif

			//and done!

			return new Proto()
			{
				NumParams = numParams,
				HasVarArgs = hasVarArgs,
				MaxStack = maxStack,

				Code = code,
				Constants = constants,
				InnerProtos = innerProtos,

				UpValues = upValues,

				startLine = lineDefined,
				endLine = lastLineDefined,

#if DEBUG_API
				source = source,
				lineInfo = lineInfos,
				localVars = localVars,
#endif
			};
		}

		private static String LoadString( BinaryReader reader )
		{
			var len = reader.ReadUInt32(); //size_t
			if( len == 0 )
				return new String();

			if( len > int.MaxValue )
				throw new InvalidDataException( "Can't load ENORMOUS string constant." );

			var buf = String.InternalAllocBuffer( (int)len );
			reader.BaseStream.Read( buf, String.BufferDataOffset, (int)len );
			return String.InternalFinishBuffer( buf );
		}

		private static void SkipString( BinaryReader reader )
		{
			var len = reader.ReadUInt32();
			reader.BaseStream.Seek( len, SeekOrigin.Current );
		}

		private struct LoadState
		{
			public String Name;
			private Stream byteCode;

			public LoadState( Stream byteCode, String name )
			{
				this.byteCode = byteCode;
				this.Name = name;
			}
		}

		
	}
}
