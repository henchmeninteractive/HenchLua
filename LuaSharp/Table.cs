using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Debug = System.Diagnostics.Debug;

namespace LuaSharp
{
	public class Table
	{
		public Table()
		{
			nodes = EmptyNodes;
		}

		public Table( int numArraySlots, int numNodes )
		{
			if( numArraySlots < 0 || numNodes < 0 )
				throw new ArgumentOutOfRangeException();

			if( numArraySlots > 0 )
				array = new Value[numArraySlots];

			nodes = EmptyNodes;
			Resize( numArraySlots, numNodes );
		}

		private Table metaTable;

		private Node[] nodes;
		private Value[] array;

		private int lastFreeNode;
		
		public int ArrayCapacity { get { return array != null ? array.Length : 0; } }
		public int NodeCapacity { get { return nodes != EmptyNodes ? nodes.Length : 0; } }
		public int Capacity { get { return ArrayCapacity + NodeCapacity; } }

		private int GetMainPosition( Value key )
		{
			int hash = key.GetHashCode();
			return (hash & 0x7FFFFFFF) % nodes.Length;
		}

		private int GetMainPosition( double key )
		{
			int hash = Value.GetHashCode( key );
			return hash % nodes.Length;
		}

		private int GetMainPosition( String key )
		{
			int hash = key.GetHashCode();
			return hash % nodes.Length;
		}

		private int GetFreePosition()
		{
			while( lastFreeNode > 0 )
			{
				lastFreeNode--;
				if( nodes[lastFreeNode].Value.Val == null )
					return lastFreeNode;
			}

			return -1;
		}

		/// <summary>
		/// Returns key as an integer if its value is an integer,
		/// else returns -1.
		/// </summary>
		private static int ValueToInt( Value key )
		{
			double num;
			if( !key.TryGetAsNumber( out num ) )
				return -1;

			int n = (int)num;
			return (double)n == num ? n : -1;
		}

		/// <summary>
		/// Finds the key's location in the table (returns 0 if not found).
		/// Note that the value at that key may be nil.
		/// </summary>
		internal int FindValue( int key )
		{
			if( array != null && key > 0 && key <= array.Length )
				return key;

			double dKey = key;

			int i = GetMainPosition( dKey );
			while( i != -1 )
			{
				var node = nodes[i];
				if( node.Key.Equals( dKey ) )
					return -(i + 1);

				i = node.Next;
			}

			return 0;
		}

		/// <summary>
		/// Finds the key's location in the table (returns 0 if not found).
		/// </summary>
		internal int FindValue( String key )
		{
			int i = GetMainPosition( key );
			while( i != -1 )
			{
				var node = nodes[i];
				if( node.Key.Equals( key ) )
					return -(i + 1);

				i = node.Next;
			}

			return 0;
		}

		internal int FindValue( Value key )
		{
			if( array != null )
			{
				int arrIdx = ValueToInt( key );
				if( arrIdx > 0 && arrIdx <= array.Length )
					return arrIdx;
			}

			int i = GetMainPosition( key );
			while( i != -1 )
			{
				var node = nodes[i];
				if( node.Key.Equals( key ) )
					return -(i + 1);

				i = node.Next;
			}

			return 0;
		}

		internal Value ReadValue( int loc )
		{
			if( loc > 0 )
				return array[loc - 1];
			if( loc < 0 )
				return nodes[-loc - 1].Value;
			return new Value();
		}

		/// <summary>
		/// Note: val must not be aliased if it holds a number!
		/// </summary>
		internal void WriteValue( int loc, Value val )
		{
			Debug.Assert( loc != 0 );

			if( loc > 0 )
			{
				array[loc - 1] = val;
			}
			else
			{
				loc = -loc - 1;

				nodes[loc].Value = val;

				if( val.Val == null )
					//don't have a GC to sweep up later,
					//so we need to clear the key out now
					//but we can't break any chains!
					nodes[loc].Key.Val = DeadKey;
			}
		}

		internal Value GetValue( int key )
		{
			return ReadValue( FindValue( key ) );
		}

		internal Value GetValue( String key )
		{
			return ReadValue( FindValue( key ) );
		}

		internal Value GetValue( Value key )
		{
			return ReadValue( FindValue( key ) );
		}

		internal void SetValue( int key, Value value )
		{
			var loc = FindValue( key );
			if( loc == 0 )
				loc = InsertNewKey( new Value( (double)key ) );
			
			WriteValue( loc, value );
		}

		internal void SetValue( String key, Value value )
		{
			var loc = FindValue( key );
			if( loc == 0 )
				loc = InsertNewKey( new Value( key ) );
			
			WriteValue( loc, value );
		}

		internal void SetValue( Value key, Value value )
		{
			var loc = FindValue( key );
			if( loc == 0 )
				loc = InsertNewKey( key );
				
			WriteValue( loc, value );
		}

		internal int InsertNewKey( Value key )
		{
			if( key.Val == null )
				throw new ArgumentNullException( "key" );

			double asNum;
			if( key.TryGetAsNumber( out asNum ) && double.IsNaN( asNum ) )
				throw new ArgumentException( "key is NaN", "key" );

			if( nodes == EmptyNodes )
				Grow( key );

		insert:

			if( array != null )
			{
				int asArrIdx = ValueToInt( key );
				if( asArrIdx > 0 && asArrIdx <= array.Length )
					return asArrIdx;
			}

			int mainPos = GetMainPosition( key );
			if( nodes[mainPos].Value.Val != null )
			{
				//we've got a collision, need to handle it

				int freePos = GetFreePosition();
				if( freePos == -1 )
				{
					Grow( key );
					goto insert;
				}

				Debug.Assert( nodes[freePos].Value.Val == null );

				int otherMainPos = GetMainPosition( nodes[mainPos].Key );
				if( mainPos == otherMainPos )
				{
					//the colliding node is already in its main position
					//this relegates the new node to the free position

					nodes[freePos].Next = nodes[mainPos].Next;
					nodes[mainPos].Next = freePos;

					mainPos = freePos;
				}
				else
				{
					//the colliding node isn't in its main position
					//shove it into the free slot instead

					//otherMainPos is the head of a chain that the
					//colliding node was injected into, scan until
					//we find the previous node in the chain (we will
					//need to relink the nodes)

					int otherPrevPos = otherMainPos;
					for( ; ; )
					{
						int next = nodes[otherPrevPos].Next;
						if( next == mainPos )
							break;
						otherPrevPos = next;
					}

					//and relink

					nodes[otherPrevPos].Next = freePos;
					nodes[freePos] = nodes[mainPos]; //takes next along for the ride
					nodes[mainPos].Next = -1;

					nodes[mainPos].Value = new Value();
				}
			}

			nodes[mainPos].Key = key;

			Debug.Assert( nodes[mainPos].Value.Val == null );

			return -(mainPos + 1);
		}

		/// <summary>
		/// Max bits used to represent an array index.
		/// </summary>
		private const int MaxBits = 30;
		/// <summary>
		/// Max length of the array part.
		/// </summary>
		private const int MaxSize = 1 << MaxBits;

		private void Grow( Value newKey )
		{
			var arrayHist = new int[MaxBits + 1];

			//count the used slots in the array part

			int numArrayKeys = 0;

			if( array != null )
			{
				for( int lg = 0, ttlg = 1, i = 1; lg <= MaxBits; lg++, ttlg *= 2 )
				{
					int lc = 0;
					int lim = ttlg;

					if( lim > array.Length )
					{
						lim = array.Length;
						if( i > lim )
							break;
					}

					for( ; i <= lim; i++ )
					{
						if( array[i - 1].Val != null )
							lc++;
					}

					arrayHist[lg] += lc;
					numArrayKeys += lc;
				}
			}

			//and sum up the rest

			int asIdx;

			int numNodeKeys = 0, numNodeIndexKeys = 0;

			for( int i = 0; i < nodes.Length; i++ )
			{
				if( nodes[i].Value.Val == null )
					continue;

				numNodeKeys++;

				asIdx = ValueToInt( nodes[i].Key );
				if( asIdx > 0 && asIdx <= MaxSize )
				{
					arrayHist[Helpers.CeilLog2( asIdx )]++;
					numNodeIndexKeys++;
				}
			}

			//and figure out the sizes

			int totalKeys = numArrayKeys + numNodeKeys;
			int totalIndexKeys = numArrayKeys + numNodeIndexKeys;

			//including the new key

			totalKeys++;

			asIdx = ValueToInt( newKey );
			if( asIdx > 0 && asIdx <= MaxSize )
			{
				arrayHist[Helpers.CeilLog2( asIdx )]++;
				totalIndexKeys++;
			}

			//moving on...

			int newArrayLen = 0, newArrayKeys = 0;

			for( int i = 0, a = 0, twoToI = 1;
				twoToI / 2 < totalIndexKeys;
				i++, twoToI *= 2 )
			{
				int hist = arrayHist[i];

				if( hist != 0 )
				{
					a += hist;

					if( a > twoToI / 2 )
					{
						newArrayLen = twoToI;
						newArrayKeys = a;
					}
				}

				if( a == totalIndexKeys )
					//we've counted everything
					break;
			}

			Debug.Assert( newArrayLen / 2 <= newArrayKeys && newArrayKeys <= newArrayLen );

			//aaaaand, resize!

			Resize( newArrayLen, totalKeys - newArrayKeys );
		}

		internal void Resize( int numArraySlots, int numNodes )
		{
			Debug.Assert( numArraySlots >= 0 && numNodes >= 0 );

			var oldArray = array;
			int oldArraySlots = oldArray != null ? oldArray.Length : 0;
			array = numArraySlots != 0 ? new Value[numArraySlots] : null;

			var oldNodes = nodes;
			if( numNodes > 0 )
			{
				int logNumNodes = Helpers.CeilLog2( numNodes );
				if( logNumNodes > MaxBits )
					throw new InvalidOperationException( "Table overflow" );

				numNodes = 1 << logNumNodes;

				nodes = new Node[numNodes];
				for( int i = 0; i < nodes.Length; i++ )
					nodes[i].Next = -1;
			}
			else
			{
				nodes = EmptyNodes;
			}

			lastFreeNode = numNodes;

			//copy from the old array to the new

			int copyArraySlots = oldArraySlots < numArraySlots ? oldArraySlots : numArraySlots;

			if( copyArraySlots != 0 )
				Array.Copy( oldArray, array, copyArraySlots );

			//move the remaining elements to the new nodes array

			if( oldArray != null )
			{
				for( int i = copyArraySlots; i < oldArray.Length; i++ )
				{
					var val = oldArray[i];
					if( val.Val != null )
						SetValue( i, val );
				}
			}

			//done with the old array

			oldArray = null;

			//on to the nodes!

			for( int i = oldNodes.Length; i-- != 0; )
			{
				var node = oldNodes[i];
				if( node.Value.Val != null )
					SetValue( node.Key, node.Value );
			}
		}

		private static readonly object DeadKey = new object();
		private static readonly Node[] EmptyNodes = new Node[1] { new Node { Next = -1 } };

		public int Count()
		{
			int ret = 0;

			if( array != null )
			{
				for( int i = 0; i < array.Length; i++ )
				{
					if( array[i].Val != null )
						ret++;
				}
			}

			for( int i = 0; i < nodes.Length; i++ )
			{
				if( nodes[i].Value.Val != null )
					ret++;
			}

			return ret;
		}

		internal struct Value
		{
			public object Val;

			public Value( bool value )
			{
				Val = value ? BoolBox.True : BoolBox.False;
			}

			public Value( double value )
			{
				Val = new NumBox( value );
			}

			public Value( String value )
			{
				if( value.InternalData == null )
					throw new ArgumentNullException();

				Val = value.InternalData;
			}

			public bool TryGetAsNumber( out double num )
			{
				var asBox = Val as NumBox;
				if( asBox != null )
				{
					num = asBox.Value;
					return true;
				}

				num = 0;
				return false;
			}

			public void Set( bool value )
			{
				Val = value ? BoolBox.True : BoolBox.False;
			}

			public void Set( double value )
			{
				var asBox = Val as NumBox;
				if( asBox != null )
					asBox.Value = value;
				else
					Val = new NumBox( value );
			}

			public void Set( String value )
			{
				if( value.InternalData == null )
					throw new ArgumentNullException();

				Val = value.InternalData;
			}

			public void Set( object value )
			{
				var asUserByteArray = value as byte[];
				if( asUserByteArray != null )
					SetUserByteArray( asUserByteArray );
				else
					Val = value;
			}

			private void SetUserByteArray( byte[] value )
			{
				var asWrapper = Val as UserByteArrayWrapper;
				if( asWrapper != null )
					asWrapper.Value = value;
				else
					Val = new UserByteArrayWrapper( value );
			}

			public bool Equals( double value )
			{
				var asNum = Val as NumBox;
				return asNum != null && asNum.Value == value;
			}

			public bool Equals( String value )
			{
				var asStr = Val as byte[];
				return asStr != null && String.InternalEquals( asStr, value.InternalData );
			}

			public bool Equals( Value other )
			{
				if( Val == other.Val )
					return true;

				var asNum = Val as NumBox;
				if( asNum != null )
				{
					var asOtherNum = other.Val as NumBox;
					if( asOtherNum != null )
						return asNum.Value == asOtherNum.Value;
				}

				var asStr = Val as byte[];
				if( asStr != null )
				{
					var asOtherStr = other.Val as byte[];
					if( asOtherStr != null )
						return String.InternalEquals( asStr, asOtherStr );
				}

				var asWrapper = Val as UserByteArrayWrapper;
				if( asWrapper != null )
				{
					var asOtherWrapper = other.Val as UserByteArrayWrapper;
					if( asOtherWrapper != null )
						return asWrapper.Value == asOtherWrapper.Value;
				}

				return false;
			}

			public override bool Equals( object obj )
			{
				return obj is Value && Equals( (Value)obj );
			}

			public override int GetHashCode()
			{
				//bool, nil (nil is just a sanity check)

				if( Val == null || Val == BoolBox.False )
					return 0;

				if( Val == BoolBox.True )
					return 1;

				//number

				var asNumBox = Val as NumBox;
				if( asNumBox != null )
					return GetHashCode( asNumBox.Value );

				//string

				var asStr = Val as byte[];
				if( asStr != null )
					return String.InternalGetHashCode( asStr );

				//userdata, closure, function...

				var val = Val;

				var asWrapper = val as UserByteArrayWrapper;
				if( asWrapper != null )
					val = asWrapper.Value;				

				return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode( val );
			}

			public static int GetHashCode( double value )
			{
				var i = BitConverter.DoubleToInt64Bits( value );
				return (int)i ^ (int)(i >> 32);
			}

			public ValueType ValueType
			{
				get
				{
					if( Val == null )
						return ValueType.Nil;

					if( Val == BoolBox.True ||
						Val == BoolBox.False )
						return ValueType.Bool;

					if( Val is NumBox )
						return ValueType.Number;

					if( Val is byte[] )
						return ValueType.String;

					if( Val is Table )
						return ValueType.Table;

					if( Callable.IsCallable( Val ) )
						return ValueType.Function;

					return ValueType.UserData;
				}
			}
		}

		private struct Node
		{
			public Value Key, Value;
			public int Next;
		}

		private class UserByteArrayWrapper
		{
			public byte[] Value;
			
			public UserByteArrayWrapper()
			{
			}

			public UserByteArrayWrapper( byte[] value )
			{
				this.Value = value;
			}
		}
	}

	public struct TableAccessor
	{
		public struct KeyOrValue
		{
			internal Table.Value Val;
			public ValueType Type { get { return Val.ValueType; } }

			public void SetNil()
			{
				Val.Val = null;
			}

			public void Set( bool value )
			{
				Val.Set( value );
			}

			public void Set( int value )
			{
				Val.Val = null; //create a new box
				Val.Set( (double)value );
			}

			public void Set( double value )
			{
				Val.Val = null; //create a new box
				Val.Set( value );
			}

			public void Set( String value )
			{
				Val.Set( value );
			}

			public void Set( Table value )
			{
				Val.Val = value;
			}

			public void Set( Callable value )
			{
				Val.Val = value.Val;
			}

			public void Set( object value )
			{
				if( value is ValueType )
				{
					//the slow case...

					SetFromValueType( value );
					return;
				}

				Val.Set( value );
			}

			private void SetFromValueType( object value )
			{
				//core types first

				if( value is bool )
					Set( (bool)value );
				else if( value is int )
					Set( (int)value );
				else if( value is double )
					Set( (double)value );
				else if( value is String )
					Set( (String)value );
				else if( value is Callable )
					Set( (Callable)value );

				//and now all the odd cases (note the leading else!)

				else if( value is uint )
					Set( (double)(uint)value );
				else if( value is float )
					Set( (double)(float)value );
				else if( value is sbyte )
					Set( (double)(sbyte)value );
				else if( value is byte )
					Set( (double)(byte)value );
				else if( value is short )
					Set( (double)(short)value );
				else if( value is ushort )
					Set( (double)(ushort)value );

				//unsure whether I should make this an error or not...

				else
					Val.Set( value );
			}

			public bool IsNil { get { return Val.Val == null; } }

			/// <summary>
			/// Returns true if the value is non-nil and not false.
			/// </summary>
			public bool ToBool()
			{
				return Val.Val != null && Val.Val != BoolBox.True;
			}

			/// <summary>
			/// Gets the number's value or zero if it's not a number.
			/// This method does NOT parse strings.
			/// </summary>
			public double ToDouble()
			{
				double ret;
				Val.TryGetAsNumber( out ret );
				return ret;
			}

			/// <summary>
			/// Gets the number's value or zero if it's not a number.
			/// Non-integers are truncated.
			/// This method does NOT parse strings.
			/// </summary>
			public int ToInt32()
			{
				double ret;
				Val.TryGetAsNumber( out ret );
				return (int)ret;
			}

			/// <summary>
			/// Gets the number's value or zero if it's not a number.
			/// Non-integers are truncated.
			/// This method does NOT parse strings.
			/// </summary>
			public uint ToUInt32()
			{
				double ret;
				Val.TryGetAsNumber( out ret );
				return (uint)ret;
			}

			/// <summary>
			/// Gets the value as a string, returning a null string
			/// if the value isn't a string. Does NOT convert values.
			/// </summary>
			public new String ToString()
			{
				String ret;
				ret.InternalData = Val.Val as byte[];
				return ret;
			}
		}

		public KeyOrValue Key, Value;

		public void Clear()
		{
			Key.SetNil();
			Value.SetNil();
		}

		public void RawGet( Table table )
		{
			if( table == null )
				throw new ArgumentNullException( "table" );
			if( Key.IsNil )
				throw new ArgumentNullException( "Key" );

			Value.Val = table.GetValue( Key.Val );			
		}

		public void RawSet( Table table )
		{
			if( table == null )
				throw new ArgumentNullException( "table" );
			if( Key.IsNil )
				throw new ArgumentNullException( "Key" );

			table.SetValue( Key.Val, Value.Val );
		}
	}
}
