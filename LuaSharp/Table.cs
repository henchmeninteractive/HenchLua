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

		private int GetMainPosition( String key )
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

		internal int FindValueInNodes( int key )
		{
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
			return FindValue( key ) != 0;
		}

		public bool ContainsKey( String key )
		{
			return FindValue( key ) != 0;
		}

		public bool ContainsKey( Value key )
		{
			return FindValue( key ) != 0;
		}

		public bool TryGetValue( Value key, out Value value )
		{
			var loc = FindValue( key );

			if( loc == 0 )
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
			if( loc != 0 )
				throw new ArgumentException( "An element with that key already exists." );

			loc = InsertNewKey( new CompactValue( key ) );
			WriteValue( loc, ref value );
		}

		public bool Remove( Value key )
		{
			var loc = FindValue( key );
			if( loc == 0 )
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
		public Value this[String key]
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

		public Value RawGet( String key )
		{
			Value ret;

			int loc = FindValue( key );
			ReadValue( loc, out ret );

			return ret;
		}

		public void RawSet( String key, Value value )
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

			if( nodes == EmptyNodes )
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

		internal Table metaTable;
		public Table MetaTable { get { return metaTable; } set { metaTable = value; } }
	}
}
