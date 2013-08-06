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

			public UpValueKind Kind;
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

		internal static Proto Load( BinaryReader reader, bool isChunk )
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
					innerProtos[i] = Load( reader, false );
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
					upValues[i].Kind = reader.ReadByte() != 0 ?
						UpValueKind.StackPointing : UpValueKind.ValuePointing;
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

			var ret = isChunk ? new ChunkProto() : new Proto();

			ret.NumParams = numParams;
			ret.HasVarArgs = hasVarArgs;
			ret.MaxStack = maxStack;

			ret.Code = code;
			ret.Constants = constants;
			ret.InnerProtos = innerProtos;

			ret.UpValues = upValues;

			ret.startLine = lineDefined;
			ret.endLine = lastLineDefined;

#if DEBUG_API
			ret.source = source;
			ret.lineInfo = lineInfos;
			ret.localVars = localVars;
#endif

			return ret;
		}

		private static String LoadString( BinaryReader reader )
		{
			var len = reader.ReadUInt32(); //size_t
			if( len == 0 )
				return new String();

			if( len > int.MaxValue )
				throw new InvalidDataException( "Can't load ENORMOUS string constant." );

			len--;

			var buf = String.InternalAllocBuffer( (int)len );
			reader.BaseStream.Read( buf, String.BufferDataOffset, (int)len );
			var ret = String.InternalFinishBuffer( buf );

			if( reader.ReadByte() != 0 )
				throw new InvalidDataException( "Malformed string constant - it's not null-terminated." );

			return ret;
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

		internal static void MarkValueCopyingUpvalues( Proto proto, Proto parent )
		{
			var upVals = proto.UpValues;
			if( upVals == null )
				return;

			var parentUpVals = parent != null ? parent.UpValues : null;

			bool hasCandidates = false;
			for( int i = 0; i < upVals.Length; i++ )
			{
				var upVal = upVals[i];

				if( parentUpVals == null || 
					(upVal.Kind == UpValueKind.ValuePointing &&
					parentUpVals[upVal.Index].Kind == UpValueKind.ValueCopying) )
				{
					hasCandidates = true;
					upVals[i].Kind = UpValueKind.ValueCopying; //optimism!					
				}
			}

			//codescan

			if( hasCandidates )
			{
				var code = proto.Code;

				for( int i = 0; i < code.Length; i++ )
				{
					var op = code[i];
					if( op.OpCode != OpCode.SetUpValue )
						continue;

					if( upVals[op.B].Kind == UpValueKind.ValueCopying )
						upVals[op.B].Kind = UpValueKind.ValuePointing;
				}
			}

			//get the inner protos

			var inner = proto.InnerProtos;
			if( inner != null )
			{
				for( int i = 0; i < inner.Length; i++ )
					MarkValueCopyingUpvalues( inner[i], proto );
			}

			//push the flag up to the parent

			if( parentUpVals != null )
			{
				for( int i = 0; i < upVals.Length; i++ )
				{
					var upVal = upVals[i];

					if( upVal.Kind == UpValueKind.ValuePointing &&
						parentUpVals[upVal.Index].Kind == UpValueKind.ValueCopying )
					{
						parentUpVals[upVal.Index].Kind = UpValueKind.ValuePointing;
					}
				}
			}

			//and now push the values down again

			if( parent == null )
				FinishMarkingValueCopyingUpvalues( proto, null );
		}

		private static void FinishMarkingValueCopyingUpvalues( Proto proto, Proto parent )
		{
			var upVals = proto.UpValues;

			var inner = proto.InnerProtos;
			if( inner != null )
			{
				for( int i = 0; i < inner.Length; i++ )
				{
					var child = inner[i];
					var childVals = child.UpValues;

					if( childVals != null )
					{
						for( int j = 0; j < childVals.Length; j++ )
						{
							var upVal = childVals[j];
							if( upVal.Kind == UpValueKind.ValueCopying &&
								upVals[upVal.Index].Kind != UpValueKind.ValueCopying )
							{
								childVals[j].Kind = UpValueKind.ValuePointing;
							}
						}
					}

					FinishMarkingValueCopyingUpvalues( child, proto );
				}
			}
		}
	}

	internal class ChunkProto : Proto
	{
	}

	internal enum UpValueKind : byte
	{
		/// <summary>
		/// The upvalue points at the stack. Normal close semantics apply.
		/// </summary>
		StackPointing,
		/// <summary>
		/// The upvalue points at a parent's upvalue. Normal close semantics apply.
		/// </summary>
		ValuePointing,
		/// <summary>
		/// The upvalue points at a parent's upvalue which is never modified.
		/// The upvalue can be treated as a simple value in all cases.
		/// </summary>
		ValueCopying,
	}
}
