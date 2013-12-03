using System;
using System.Collections.Generic;

using Debug = System.Diagnostics.Debug;

namespace Henchmen.Lua
{
	public class Table : IHasMetatable, IEnumerable<KeyValuePair<Value, Value>>
	{
		public Table()
		{
			nodes = EmptyNodes;
		}

		public Table( int numArraySlots, int numNodes )
		{
			if( numArraySlots < 0 || numNodes < 0 )
				throw new ArgumentOutOfRangeException();

			nodes = EmptyNodes;
			Resize( numArraySlots, numNodes );
		}

		internal CompactValue[] array;
		
		private Node[] nodes;
		private int lastFreeNode;

		private static readonly object DeadKey = new object();
		private static readonly Node[] EmptyNodes = new Node[1] { new Node { Next = -1 } };

		private struct Node
		{
			public CompactValue Key;
			public CompactValue Value;
			public int Next;
		}

		private int GetMainPosition( Value key )
		{
			int hash = key.GetHashCode();
			return (hash & 0x7FFFFFFF) % nodes.Length;
		}

		private int GetMainPosition( CompactValue key )
		{
			int hash = key.GetHashCode();
			return (hash & 0x7FFFFFFF) % nodes.Length;
		}

		private int GetMainPosition( double key )
		{
			int hash = Value.GetHashCode( key );
			return (hash & 0x7FFFFFFF) % nodes.Length;
		}

		private int GetMainPosition( LString key )
		{
			int hash = key.GetHashCode();
			return (hash & 0x7FFFFFFF) % nodes.Length;
		}

		private int GetFreePosition()
		{
			while( lastFreeNode > 0 )
			{
				/* Warning: a subtle nuance follows:
				 * 
				 * Everywhere else in Table, we're testing node.Value
				 * for nil in order to identify free positions. However,
				 * here we test Key. This is because Key might be DeadKey,
				 * meaning that the node is part of a probing chain. Reusing
				 * the node could break that chain, so we leave it alone.
				 * 
				 * Ideally, the chaining logic (down in InsertNewKey) would be
				 * smart enough to handle this case, and we could safely reuse
				 * slots during collision resolutions, reducing the number of
				 * reallocations.
				 */

				lastFreeNode--;
				if( nodes[lastFreeNode].Key.Val == null )
					return lastFreeNode;
			}

			return -1;
		}

		private static int ValueToInt( Value key )
		{
			if( key.RefVal != Value.NumTypeTag )
				return -1;

			var num = key.NumVal;
			var ret = (int)num;

			return (double)ret == num ? ret : -1;
		}

		private static int ValueToInt( CompactValue key )
		{
			var asNum = key.Val as NumBox;
			if( asNum == null )
				return -1;

			var num = asNum.Value;
			var ret = (int)num;

			return (double)ret == num ? ret : -1;
		}

		private static int ValueToInt( double key )
		{
			var ret = (int)key;
			return (double)ret == key ? ret : -1;
		}

		#region FindValue

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
		/// Finds the key's location in the table (assumes we already
		/// know the key won't be in the array part). Never returns
		/// zero for nil values.
		/// </summary>
		/// <param name="key">The key to find.</param>
		internal int FindValueInNodes( int key )
		{
			Debug.Assert( array == null || key > array.Length );

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
		/// Note that the value at that key may be nil.
		/// </summary>
		internal int FindValue( LString key )
		{
			if( key.InternalData == null )
				throw new ArgumentNullException( "key" );

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

		/// <summary>
		/// Finds the key's location in the table (returns 0 if not found).
		/// Note that the value at that key may be nil.
		/// </summary>
		internal int FindValue( Value key )
		{
			if( key.RefVal == null )
				throw new ArgumentNullException( "key" );

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

		/// <summary>
		/// Finds the key's location in the table (returns 0 if not found).
		/// Note that the value at that key may be nil.
		/// </summary>
		internal int FindValue( CompactValue key )
		{
			if( key.Val == null )
				throw new ArgumentNullException( "key" );

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

		#endregion

		#region Some methods to make this look a little more like a .NET dictionary

		public bool ContainsKey( int key )
		{
			return !IsLocNilOrEmpty( FindValue( key ) );
		}

		public bool ContainsKey( LString key )
		{
			return !IsLocNilOrEmpty( FindValue( key ) );
		}

		public bool ContainsKey( Value key )
		{
			return !IsLocNilOrEmpty( FindValue( key ) );
		}

		public bool TryGetValue( Value key, out Value value )
		{
			var loc = FindValue( key );

			if( IsLocNilOrEmpty( loc ) )
			{
				value = Value.Nil;
				return false;
			}

			ReadValue( loc, out value );
			return true;
		}

		public void Add( Value key, Value value )
		{
			if( key.RefVal == null )
				throw new ArgumentNullException( "Key is nil." );

			var loc = FindValue( key );
			if( loc < 0 || (loc > 0 && array[loc - 1].Val != null) )
				throw new ArgumentException( "An element with that key already exists." );

			loc = InsertNewKey( new CompactValue( key ) );
			WriteValue( loc, ref value );
		}

		public bool Remove( Value key )
		{
			var loc = FindValue( key );
			if( IsLocNilOrEmpty( loc ) )
				return false;

			var nil = Value.Nil;
			WriteValue( loc, ref nil );

			return true;
		}

		public void Clear()
		{
			if( array != null )
				Array.Clear( array, 0, array.Length );

			if( nodes != EmptyNodes )
			{
				for( int i = 0; i < nodes.Length; i++ )
					nodes[i] = new Node() { Next = -1 };
			}
		}

		/// <summary>
		/// Clears the table and resizes it sunderlying
		/// storage to accept new data.
		/// </summary>
		public void Clear( int numArraySlots, int numNodes )
		{
			if( numArraySlots < 0 || numNodes < 0 )
				throw new ArgumentOutOfRangeException();

			if( array != null )
			{

				array = null;
			}

			nodes = EmptyNodes;

			Resize( numArraySlots, numNodes );
		}

		#endregion

		/// <summary>
		/// Take care not to clone the returned value, as you may
		/// accidentally turn a number into a reference type!
		/// </summary>
		internal bool ReadValue( int loc, out CompactValue value )
		{
			if( loc == 0 )
			{
				value = new CompactValue();
				return false;
			}

			value = loc > 0 ? array[loc - 1] : nodes[-loc - 1].Value;
			return true;
		}

		internal bool ReadValue( int loc, out Value value )
		{
			if( loc == 0 )
			{
				value = new Value();
				return false;
			}

			var val = loc > 0 ? array[loc - 1] : nodes[-loc - 1].Value;
			val.ToValue( out value );

			return true;
		}

		internal bool IsLocNilOrEmpty( int loc )
		{
			return loc > 0 ? array[loc - 1].Val == null : loc == 0;
		}

		internal void WriteValue( int loc, ref Value val )
		{
			Debug.Assert( loc != 0 );

			var rVal = val.RefVal;

			if( rVal == Value.NumTypeTag )
			{
				//reuse an existing box if we can

				var box = (loc > 0 ? array[loc - 1].Val : nodes[-loc - 1].Value.Val) as NumBox;
				if( box == null )
					box = new NumBox();
				box.Value = val.NumVal;
				
				rVal = box;
			}

			if( loc > 0 )
			{
				array[loc - 1].Val = rVal;
			}
			else
			{
				loc = -loc - 1;

				nodes[loc].Value.Val = rVal;

				if( rVal == null )
					//don't have a GC to sweep up later,
					//so we need to clear the key out now,
					//but we can't break any chains!
					nodes[loc].Key.Val = DeadKey;
			}
		}

		#region this[], RawGet, RawSet

		/// <summary>
		/// Gets or sets a value in the table.
		/// This is a raw operation, it does
		/// not invoke metatable methods.
		/// </summary>
		public Value this[Value key]
		{
			get
			{
				Value ret;

				int loc = FindValue( key );
				ReadValue( loc, out ret );

				return ret;
			}

			set
			{
				int loc = FindValue( key );
				if( loc == 0 )
					loc = InsertNewKey( new CompactValue( key ) );
				WriteValue( loc, ref value );
			}
		}

		/// <summary>
		/// Gets or sets a value in the table.
		/// This is a raw operation, it does
		/// not invoke metatable methods.
		/// </summary>
		public Value this[int key]
		{
			get
			{
				Value ret;

				int loc = FindValue( key );
				ReadValue( loc, out ret );

				return ret;
			}

			set
			{
				int loc = FindValue( key );
				if( loc == 0 )
					loc = InsertNewKey( new CompactValue( key ) );
				WriteValue( loc, ref value );
			}
		}

		/// <summary>
		/// Gets or sets a value in the table.
		/// This is a raw operation, it does
		/// not invoke metatable methods.
		/// </summary>
		public Value this[LString key]
		{
			get
			{
				Value ret;

				int loc = FindValue( key );
				ReadValue( loc, out ret );

				return ret;
			}

			set
			{
				int loc = FindValue( key );
				if( loc == 0 )
					loc = InsertNewKey( new CompactValue( key ) );
				WriteValue( loc, ref value );
			}
		}

		public Value RawGet( Value key )
		{
			Value ret;

			int loc = FindValue( key );
			ReadValue( loc, out ret );

			return ret;			
		}

		public void RawSet( Value key, Value value )
		{
			int loc = FindValue( key );
			if( loc == 0 )
				loc = InsertNewKey( new CompactValue( key ) );
			WriteValue( loc, ref value );
		}

		public Value RawGet( int key )
		{
			Value ret;

			int loc = FindValue( key );
			ReadValue( loc, out ret );

			return ret;
		}

		public void RawSet( int key, Value value )
		{
			int loc = FindValue( key );
			if( loc == 0 )
				loc = InsertNewKey( new CompactValue( key ) );
			WriteValue( loc, ref value );
		}

		public Value RawGet( LString key )
		{
			Value ret;

			int loc = FindValue( key );
			ReadValue( loc, out ret );

			return ret;
		}

		public void RawSet( LString key, Value value )
		{
			int loc = FindValue( key );
			if( loc == 0 )
				loc = InsertNewKey( new CompactValue( key ) );
			WriteValue( loc, ref value );
		}

		#endregion

		internal int InsertNewKey( CompactValue key )
		{
			if( key.Val == null )
				throw new ArgumentNullException( "key" );

			var asNumKey = key.Val as NumBox;
			if( asNumKey != null && double.IsNaN( asNumKey.Value ) )
				throw new ArgumentException( "key is NaN", "key" );

			if( nodes == EmptyNodes && !isResizing )
				//the resizing case happens when we grow a
				//node-only table into an array-only table
				Grow( key );

		insert:

			if( array != null && asNumKey != null )
			{
				int asArrIdx = ValueToInt( asNumKey.Value );
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

				Debug.Assert( nodes[freePos].Key.Val == null );
				Debug.Assert( nodes[freePos].Value.Val == null );
				Debug.Assert( nodes[freePos].Next == -1 );

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

					nodes[mainPos].Value.Val = null;
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

		private void Grow( CompactValue newKey )
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

#if DEBUG
		private bool isResizing;
#endif

		internal void Resize( int numArraySlots, int numNodes )
		{
#if DEBUG
			Debug.Assert( !isResizing );
			isResizing = true;
#endif
			Debug.Assert( numArraySlots >= 0 && numNodes >= 0 );

			var oldArray = array;
			int oldArraySlots = oldArray != null ? oldArray.Length : 0;
			array = numArraySlots != 0 ? new CompactValue[numArraySlots] : null;

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
				//we know that none of these values will land in the array,
				//so we temporarily null it out in order to prevent InsertNewKey
				//from doing an unnecessary "does it land in the array part" check

				var newArr = array;
				array = null;

				for( int i = copyArraySlots; i < oldArray.Length; i++ )
				{
					var val = oldArray[i];
					if( val.Val == null )
						continue;

					var key = new CompactValue( i );
					var loc = InsertNewKey( key );

					Debug.Assert( loc < 0 );
					nodes[-loc - 1].Value = val;
				}

				array = newArr;
				oldArray = null;
			}

			//on to the nodes!

			for( int i = oldNodes.Length; i-- != 0; )
			{
				var node = oldNodes[i];
				if( node.Value.Val == null )
					continue;

				int loc = InsertNewKey( node.Key );
				
				if( loc > 0 )
					array[loc - 1] = node.Value;
				else
					nodes[-loc - 1].Value = node.Value;
			}

#if DEBUG
			isResizing = false;
#endif
		}

		/// <summary>
		/// Finds a boundary slot. A boundary slot is identified by an integer i
		/// such that t[i] is non-nil and t[i + 1] is nil (and 0 if t[1] is nil).
		/// </summary>
		public int GetLen()
		{
			int low, high; //low is always 0 or a non-nil index

			var array = this.array;
			if( array != null )
			{
				high = array.Length;

				if( array[high - 1].Val == null )
				{
					//there's a boundary in the array part, binsearch for it

					low = 0;
					while( high - low > 1 )
					{
						int m = (low + high) / 2;
						if( array[m - 1].Val != null )
							low = m;
						else
							high = m;
					}

					return low;
				}
				else if( nodes == EmptyNodes )
					return high;
			}
			else
			{
				high = 0;
			}

			//search the nodes

			low = high;
			high++;

			while( FindValueInNodes( high ) != 0 )
			{
				if( high > int.MaxValue / 2 )
				{
					//someone's not playing nice with the table construction, fall back to a linear search

					int i = array != null ? array.Length : 1;
					while( i > 0 && FindValueInNodes( i ) == 0 )
						i++;
					return i - 1;
				}

				low = high;
				high *= 2;
			}

			while( high - low > 1 )
			{
				int m = (low + high) / 2;
				if( FindValueInNodes( m ) != 0 )
					low = m;
				else
					high = m;
			}

			return low;
		}

		#region GetNext, Enumerator, IEnumerable

		/// <summary>
		/// Gets the next element in the table during an enumeration.
		/// </summary>
		/// <param name="key">
		/// On entry, the key of the previous value or <c>nil</c> to get the first entry.
		/// On exit, the key of the current value or <c>nil</c> if there are no more entries.
		/// </param>
		/// <param name="value">
		/// The current value or <c>nil</c> if there is none.
		/// </param>
		/// <returns>
		/// <c>true</c> if a value was retrieved, otherwise false.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// <paramref name="key"/> could not be found in the table.
		/// </exception>
		/// <remarks>
		/// Note that this is not strictly equivalent to Lua's implementation of next as
		/// it is not stable with respect to keys being set to null during traversal. If
		/// you need a stable iteration, use the enumerator returned by <see cref="GetEnumerator"/>.
		/// </remarks>
		public bool GetNext( ref Value key, out Value value )
		{
			int loc;

			if( key.RefVal != null )
			{
				loc = FindValue( key );
				if( loc == 0 )
					throw new ArgumentException( "Invalid key passed to GetNext." );
			}
			else
			{
				loc = 0;
			}

			return GetNext( ref loc, out key, out value );
		}

		internal bool GetNext( ref int loc, out Value key, out Value value )
		{
			if( loc < 0 )
			{
				//the last value was found at loc, get the real
				//index and advance to the next node

				loc = -loc - 1;
				loc++;

				//and scan

				goto scanNodes;
			}
			else if( loc > 0 || array != null )
			{
				//the next key's in the array part

				//note that because of how FindValue encodes its
				//result, loc is already one past of the index of
				//the key (that is, it points to the next key)

				while( loc < array.Length )
				{
					var val = array[loc++];

					if( val.Val != null )
					{
						key.RefVal = Value.NumTypeTag;
						key.NumVal = loc;

						val.ToValue( out value );

						return true;
					}
				}

				//we've run off the end of the array, start on the nodes
			}

			loc = 0;

		scanNodes:

			for( ; loc < nodes.Length; loc++ )
			{
				if( nodes[loc].Value.Val == null )
					continue;

				nodes[loc].Key.ToValue( out key );
				nodes[loc].Value.ToValue( out value );

				loc = -(loc + 1);

				return true;
			}

			//found nothing
			key = new Value();
			value = new Value();
			return false;
		}

		public struct Enumerator : IEnumerator<KeyValuePair<Value, Value>>
		{
			internal Enumerator( Table owner )
			{
				this.owner = owner;
				loc = 0;
				key = value = new Value();
			}

			private Table owner;
			private int loc;
			private Value key, value;

			#region IEnumerator<KeyValuePair<Value,Value>> Members

			public KeyValuePair<Value, Value> Current
			{
				get { return new KeyValuePair<Value, Value>( key, value ); }
			}

			#endregion

			#region IDisposable Members

			public void Dispose()
			{
			}

			#endregion

			#region IEnumerator Members

			object System.Collections.IEnumerator.Current
			{
				get { return new KeyValuePair<Value, Value>( key, value ); }
			}

			public bool MoveNext()
			{
				return owner.GetNext( ref loc, out key, out value );
			}

			public void Reset()
			{
				loc = 0;
				key = value = new Value();
			}

			#endregion
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator( this );
		}

		#region IEnumerable<KeyValuePair<Value,Value>> Members

		IEnumerator<KeyValuePair<Value, Value>> IEnumerable<KeyValuePair<Value, Value>>.GetEnumerator()
		{
			return new Enumerator( this );
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return new Enumerator( this );
		}

		#endregion

		#endregion

		#region Misc accessors

		public int ArrayCapacity { get { return array != null ? array.Length : 0; } }
		public int NodeCapacity { get { return nodes != EmptyNodes ? nodes.Length : 0; } }
		public int Capacity { get { return ArrayCapacity + NodeCapacity; } }

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

		#endregion

		internal Table metatable;
		public Table Metatable { get { return metatable; } set { metatable = value; } }
	}
}
