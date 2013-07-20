using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuaSharp
{
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
