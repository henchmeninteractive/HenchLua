﻿using System;
using System.Text;

using Debug = System.Diagnostics.Debug;

namespace Henchmen.Lua
{
	/// <summary>
	/// Represents a byte-oriented string.
	/// </summary>
	/// <remarks>
	/// These strings may contain nulls and are not null-terminated.
	/// </remarks>
	[Serializable]
	public struct LString
	{
		public static readonly LString Empty = new LString( string.Empty );

		internal byte[] InternalData;

		public bool IsNil { get { return InternalData == null; } }

		/// <summary>
		/// Initializes a String from a .NET String and
		/// the desired byte encoding.
		/// </summary>
		/// <param name="str">The string.</param>
		/// <param name="encoding">The desired encoding.</param>
		public LString( string str, Encoding encoding )
		{
			if( str == null )
			{
				InternalData = null;
				return;
			}

			if( encoding == null )
				throw new ArgumentNullException( "encoding" );

			var len = encoding.GetByteCount( str );
			
			InternalData = new byte[4 + len];
			encoding.GetBytes( str, 0, str.Length, InternalData, 4 );

			UpdateHashCode();
		}

		/// <summary>
		/// Initializes a String from a .NET String using UTF8.
		/// </summary>
		/// <param name="str">The string.</param>
		public LString( string str )
			: this( str, Encoding.UTF8 )
		{
		}

		public static implicit operator LString( string str )
		{
			return new LString( str );
		}

		/// <summary>
		/// Initializes a String from an array of raw bytes.
		/// </summary>
		/// <param name="rawBytes">The byte array.</param>
		/// <param name="index">The start of the subrange to use.</param>
		/// <param name="count">The length of the subrange to use.</param>
		public LString( byte[] rawBytes, int index, int count )
		{
			if( rawBytes == null )
				throw new ArgumentNullException( "rawBytes" );

			if( index < 0 )
				throw new ArgumentOutOfRangeException( "index" );
			if( count < 0 )
				throw new ArgumentOutOfRangeException( "count" );
			if( rawBytes.Length - index < count )
				throw new ArgumentOutOfRangeException( "count" );

			this.InternalData = new byte[4 + rawBytes.Length];
			Array.Copy( rawBytes, index, this.InternalData, 4, count );

			UpdateHashCode();
		}

		/// <summary>
		/// Initializes a String from an array of raw bytes.
		/// </summary>
		/// <param name="rawBytes">The byte array.</param>
		public LString( byte[] rawBytes )
		{
			if( rawBytes == null )
				throw new ArgumentNullException( "rawBytes" );

			this.InternalData = new byte[4 + rawBytes.Length];
			Array.Copy( rawBytes, 0, this.InternalData, 4, rawBytes.Length );

			UpdateHashCode();
		}
		
		internal static LString InternalFromData( byte[] internalData )
		{
			Debug.Assert( internalData != null );
			return new LString() { InternalData = internalData };
		}

		internal const int BufferDataOffset = 4;
		internal static byte[] InternalAllocBuffer( int length )
		{
			Debug.Assert( length >= 0 );
			return new byte[length + 4];
		}

		internal static LString InternalFinishBuffer( byte[] buffer )
		{
			Debug.Assert( buffer != null );
			var ret = new LString { InternalData = buffer };
			ret.UpdateHashCode();
			return ret;
		}

		/// <summary>
		/// Gets the length of the string, in bytes.
		/// </summary>
		public int Lenght { get { return InternalData != null ? InternalData.Length - 4 : 0; } }

		public byte this[int index]
		{
			get
			{
				if( InternalData == null || index < 0 || index > InternalData.Length - 4 )
					throw new ArgumentOutOfRangeException( "index" );

				return InternalData[4 + index];
			}
		}

		public LString Substring( int index, int count )
		{
			if( InternalData == null )
				return new LString();

			if( count == 0 )
				return LString.Empty;

			if( index < 0 )
				throw new ArgumentOutOfRangeException( "index" );
			if( count < 0 )
				throw new ArgumentOutOfRangeException( "count" );
			if( Lenght - index < count )
				throw new ArgumentOutOfRangeException( "count" );

			return new LString( InternalData, 4 + index, count );
		}

		public LString Substring( int index )
		{
			return Substring( index, Lenght - index );
		}

		internal static bool InternalEquals( byte[] a, byte[] b )
		{
			if( a == b )
				return true;

			if( a == null || b == null ||
				a.Length != b.Length )
				return false;

			for( int i = 0; i < a.Length; i++ )
			{
				if( a[i] != b[i] )
					return false;
			}

			return true;
		}

		/// <summary>
		/// Compares this string to another for equality.
		/// </summary>
		public bool Equals( LString other )
		{
			return InternalEquals( InternalData, other.InternalData );
		}

		public override bool Equals( object obj )
		{
			return obj is LString && Equals( (LString)obj );
		}

		public static bool operator ==( LString a, LString b )
		{
			return InternalEquals( a.InternalData, b.InternalData );
		}

		public static bool operator !=( LString a, LString b )
		{
			return !InternalEquals( a.InternalData, b.InternalData );
		}

		private void UpdateHashCode()
		{
			int length = InternalData.Length - 4;
			int step = (length >> 5) + 1;

			int hash = length;
			for( int i = length; i >= step; i -= step )
				hash ^= (hash << 5) + (hash >> 2) + InternalData[4 + i - 1];

			InternalData[0] = (byte)(hash >> 24);
			InternalData[1] = (byte)(hash >> 16);
			InternalData[2] = (byte)(hash >> 8);
			InternalData[3] = (byte)(hash >> 0);
		}

		internal static int InternalGetHashCode( byte[] internalData )
		{
			Debug.Assert( internalData != null );
			
			return
				(internalData[0] << 24) |
				(internalData[1] << 16) |
				(internalData[2] << 8) |
				(internalData[3] << 0);
		}

		public override int GetHashCode()
		{
			return InternalData != null ? InternalGetHashCode( InternalData ) : 0;
		}

		/// <summary>
		/// Gets the string's contents as a .NET string
		/// using the specified encoding.
		/// </summary>
		/// <param name="encoding">The encoding to use.</param>
		public string ToString( Encoding encoding )
		{
			if( encoding == null )
				throw new ArgumentNullException( "encoding" );

			return encoding.GetString( InternalData, 4, InternalData.Length - 4 );
		}

		/// <summary>
		/// Gets the string's contents as a .NET string
		/// using the UTF8 encoding.
		public override string ToString()
		{
			return ToString( Encoding.UTF8 );
		}
	}
}