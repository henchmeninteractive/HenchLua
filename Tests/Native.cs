using System;
using System.IO;
using System.Runtime.InteropServices;

internal static class Native
{
	private const string Dll = "lua.dll";

	/// <summary>
	/// Creates a new thread running in a new, independent state.
	/// </summary>
	/// <param name="alloc">The allocator function to use.</param>
	/// <param name="baton">
	/// An opaque pointer passed to each invocation of <paramref name="alloc"/>.
	/// </param>
	/// <returns>
	/// A pointer to the new state or NULL if it could not be allocated.
	/// </returns>
	[DllImport( Dll, EntryPoint = "lua_newstate", CallingConvention = CallingConvention.Cdecl )]
	public static extern IntPtr NewState( Alloc alloc, IntPtr baton );
	
	/// <summary>
	/// Creates a new Lua state with a standard allocator and a panic function
	/// that prints to standard error.
	/// </summary>
	/// <returns>
	/// A handle to the new state or null if allocation failed.
	/// </returns>
	[DllImport( Dll, EntryPoint = "luaL_newstate", CallingConvention = CallingConvention.Cdecl )]
	public static extern IntPtr NewState();
	
	/// <summary>
	/// Destroys all objects in the given Lua state (calling the corresponding
	/// garbage-collection metamethods, if any) and frees all dynamic memory
	/// used by this state.
	/// </summary>
	/// <param name="L"></param>
	[DllImport( Dll, EntryPoint = "lua_close", CallingConvention = CallingConvention.Cdecl )]
	public static extern void Close( IntPtr L );
	
	/// <summary>
	/// Creates a new thread and pushes it onto the stack.
	/// </summary>
	/// <param name="L">The thread to push the new thread onto.</param>
	/// <returns>The new thread's handle.</returns>
	/// <remarks>
	/// The new thread shares its global state with <paramref name="L"/>.
	/// </remarks>
	[DllImport( Dll, EntryPoint = "lua_newthread", CallingConvention = CallingConvention.Cdecl )]
	public static extern IntPtr NewThread( IntPtr L );

	[DllImport( Dll, EntryPoint = "lua_gettop", CallingConvention = CallingConvention.Cdecl )]
	public static extern int GetTop( IntPtr L );
	[DllImport( Dll, EntryPoint = "lua_settop", CallingConvention = CallingConvention.Cdecl )]
	public static extern void SetTop( IntPtr L, int index );

	public static void Pop( IntPtr L, int n )
	{
		SetTop( L, -(n + 1) );
	}

	[DllImport( Dll, EntryPoint = "lua_type", CallingConvention = CallingConvention.Cdecl )]
	public static extern Type GetType( IntPtr L, int index );

	/// <summary>
	/// Returns true if the value at the given index is nil.
	/// </summary>
	public static bool IsNil( IntPtr L, int index )
	{
		return GetType( L, index ) == Type.Nil;
	}

	/// <summary>
	/// Returns true if the given index refers to an invalid stack element.
	/// </summary>
	public static bool IsNone( IntPtr L, int index )
	{
		return GetType( L, index ) == Type.None;
	}

	/// <summary>
	/// Returns true if the given index refers to an invalid stack element or is nil.
	/// </summary>
	public static bool IsNoneOrNil( IntPtr L, int index )
	{
		return GetType( L, index ) <= Type.Nil;
	}

	/// <summary>
	/// Returns true if the value at the given index is a number or
	/// a string convertible to a number, and false otherwise.
	/// </summary>
	[DllImport( Dll, EntryPoint = "lua_isnumber", CallingConvention = CallingConvention.Cdecl )]
	[return: MarshalAs( UnmanagedType.Bool )]
	public static extern bool IsNumber( IntPtr L, int index );

	/// <summary>
	/// Returns true if the value at the given index is a string or number
	/// (which is convertible to a string), and false otherwise.
	/// </summary>
	[DllImport( Dll, EntryPoint = "lua_isstring", CallingConvention = CallingConvention.Cdecl )]
	[return:MarshalAs( UnmanagedType.Bool )]
	public static extern bool IsString( IntPtr L, int index );

	/// <summary>
	/// Returns true if the value at the given index is a function, and false otherwise.
	/// </summary>
	public static bool IsFunction( IntPtr L, int index )
	{
		return GetType( L, index ) == Type.Function;
	}

	[DllImport( Dll, EntryPoint = "lua_iscfunction", CallingConvention = CallingConvention.Cdecl )]
	[return: MarshalAs( UnmanagedType.Bool )]
	public static extern bool IsCFunction( IntPtr L, int index );

	public static bool IsTable( IntPtr L, int index )
	{
		return GetType( L, index ) == Type.Table;
	}

	public static bool IsThread( IntPtr L, int index )
	{
		return GetType( L, index ) == Type.Thread;
	}

	public static bool IsLightUserData( IntPtr L, int index )
	{
		return GetType( L, index ) == Type.LightUserData;
	}

	[DllImport( Dll, EntryPoint = "lua_isuserdata", CallingConvention = CallingConvention.Cdecl )]
	[return: MarshalAs( UnmanagedType.Bool )]
	public static extern bool IsUserData( IntPtr L, int index );

	[DllImport( Dll, EntryPoint = "lua_tolstring", CallingConvention = CallingConvention.Cdecl )]
	private static extern IntPtr ToLString( IntPtr L, int index, out UIntPtr len );

	/// <summary>
	/// Converts the Lua value at the given acceptable index to a string.
	/// </summary>
	/// <param name="L"></param>
	/// <param name="index"></param>
	/// <returns></returns>
	/// <remarks>
	/// The Lua value must be a string or a number; otherwise, the function returns NULL.
	/// If the value is a number, then lua_tolstring also changes the actual value in the
	/// stack to a string. (This change confuses lua_next when lua_tolstring is applied to
	/// keys during a table traversal.)
	/// </remarks>
	public static string ToString( IntPtr L, int index )
	{
		UIntPtr len;
		IntPtr p = ToLString( L, index, out len );
		return Marshal.PtrToStringAnsi( p, (int)len );
	}

	/// <summary>
	/// Loads a Lua chunk (without running it). If there are no errors,
	/// lua_load pushes the compiled chunk as a Lua function on top of
	/// the stack. Otherwise, it pushes an error message.
	/// </summary>
	/// <param name="L"></param>
	/// <param name="reader">A reader function.</param>
	/// <param name="baton">An opaque argument passed to the reader.</param>
	/// <param name="source">The chunk's name, used for error and debug information.</param>
	/// <param name="mode">
	/// Controls whether the chunk may be text or binary.
	/// "t" is text only, "b" binary only, "bt" means either-or.
	/// </param>
	/// <returns></returns>
	[DllImport( Dll, EntryPoint = "lua_load", CallingConvention = CallingConvention.Cdecl )]
	public static extern Result Load( IntPtr L, Reader reader, IntPtr baton,
		string source, string mode );

	public static Result Load( IntPtr L, Stream data, string source, string mode )
	{
		using( var sr = new StreamReader( data ) )
			return Load( L, sr.Read, IntPtr.Zero, source, mode );
	}

	private class StreamReader : IDisposable
	{
		private byte[] buffer;
		private GCHandle hBuffer;
		private IntPtr pBuffer;

		private Stream source;

		public StreamReader( Stream source )
		{
			this.source = source;
			this.buffer = new byte[2048];
			this.hBuffer = GCHandle.Alloc( buffer, GCHandleType.Pinned );
			this.pBuffer = hBuffer.AddrOfPinnedObject();
		}

		public IntPtr Read( IntPtr L, IntPtr baton, out UIntPtr size )
		{
			try
			{
				int cb = source.Read( buffer, 0, buffer.Length );

				size = (UIntPtr)cb;
				return cb != 0 ? pBuffer : IntPtr.Zero;
			}
			catch
			{
				size = UIntPtr.Zero;
				return IntPtr.Zero;
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			hBuffer.Free();
		}

		#endregion
	}

	[DllImport( Dll, EntryPoint = "luaL_loadbufferx", CallingConvention = CallingConvention.Cdecl )]
	public static extern Result LoadBuffer( IntPtr L, IntPtr buffer, UIntPtr size,
		string name, string mode );

	/// <summary>
	/// Writes the function on top of the stack (without popping it!) as a binary chunk.
	/// </summary>
	/// <param name="L"></param>
	/// <param name="writer">The writer function to send data to.</param>
	/// <param name="baton">An opaque handle passed to <paramref name="writer"/.></param>
	/// <returns>
	/// Returns zero if the operation succeeded or the error code returned by
	/// the last call to <paramref name="writer"/>.
	/// </returns>
	[DllImport( Dll, EntryPoint = "lua_dump", CallingConvention = CallingConvention.Cdecl )]
	public static extern int Dump( IntPtr L, Writer writer, IntPtr baton );

	public static int Dump( IntPtr L, Stream dest )
	{
		var writer = new DumpWriter( dest );
		return Dump( L, writer.Write, IntPtr.Zero );
	}

	private class DumpWriter
	{
		private byte[] buffer;
		private Stream dest;

		public DumpWriter( Stream dest )
		{
			this.dest = dest;
			this.buffer = new byte[2048];
		}

		public int Write( IntPtr L, IntPtr data, UIntPtr size, IntPtr baton )
		{
			try
			{
				ulong cb = (ulong)size;
				while( cb > 0 )
				{
					int n = (int)Math.Min( cb, (ulong)buffer.Length );
					
					Marshal.Copy( data, buffer, 0, n );
					
					dest.Write( buffer, 0, n );

					data = (IntPtr)((long)data + n);
					cb -= (ulong)n;
				}

				return 0;
			}
			catch
			{
				return 1;
			}
		}
	}

	/// <summary>
	/// The type of the memory-allocation function used by Lua states.
	/// The allocator function must provide a functionality similar to realloc,
	/// but not exactly the same.
	/// </summary>
	/// <param name="baton">
	/// The opaque pointer passed to <see cref="NewState"/>.
	/// </param>
	/// <param name="p">
	/// Null if allocating, otherwise the pointer to the block to free or resize.
	/// </param>
	/// <param name="oldSize">
	/// The old size of the block. If <paramref name="p"/> is null then this is
	/// the type of object being allocated.
	/// </param>
	/// <param name="newSize">
	/// The size of the block to return.
	/// </param>
	/// <returns>
	/// The pointer to the allocated or reallocated block, or null if freeing or
	/// if allocation failed.
	/// </returns>
	[UnmanagedFunctionPointer( CallingConvention.Cdecl )]
	public delegate IntPtr Alloc( IntPtr baton, IntPtr p, UIntPtr oldSize, UIntPtr newSize );

	/// <summary>
	/// Reads a bit of data for <see cref="Load"/>.
	/// </summary>
	/// <param name="L"></param>
	/// <param name="baton">The data parameter passed to <see cref="Load"/>.</param>
	/// <param name="size">
	/// On exit, the size of the returned buffer.
	/// A return value of zero signals the end of the data.
	/// </param>
	/// <returns>
	/// A pointer to a buffer containing the next chunk of data. This buffer
	/// must be valid until the next call to the reader. Returning null signals
	/// the end of the buffer.
	/// </returns>
	[UnmanagedFunctionPointer( CallingConvention.Cdecl )]
	public delegate IntPtr Reader( IntPtr L, IntPtr baton, out UIntPtr size );

	/// <summary>
	/// Writes a chunk of data from <see cref="Dump"/>.
	/// </summary>
	/// <param name="L"></param>
	/// <param name="data">A pointer to the data to write.</param>
	/// <param name="size">The size of the data to write.</param>
	/// <param name="baton">The opaque pointer passed to <see cref="Dump"/>.</param>
	/// <returns>
	/// Return zero to indicate success, or any other value to abort the dump.
	/// </returns>
	[UnmanagedFunctionPointer( CallingConvention.Cdecl )]
	public delegate int Writer( IntPtr L, IntPtr data, UIntPtr size, IntPtr baton );

	public enum Result : int
	{
		OK = 0,
		Yield = 1,
		RunError = 2,
		SyntaxError = 3,
		MemoryError = 4,
		GcMetamethodError = 5,
		Error = 6,
	}

	public enum Type : int
	{
		None = -1,
		Nil = 0,
		Boolean = 1,
		LightUserData = 2,
		Number = 3,
		String = 4,
		Table = 5,
		Function = 6,
		UserData = 7,
		Thread = 8,
	}
}
