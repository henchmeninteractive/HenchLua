using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuaSharp
{
	internal static class TypeInfo
	{
		public static readonly String VersionName = new String( "Lua 5.2" );

		public static readonly String TypeName_Nil = new String( "nil" );
		public static readonly String TypeName_Bool = new String( "boolean" );
		public static readonly String TypeName_Number = new String( "number" );

		public static readonly String TypeName_String = new String( "string" );
		public static readonly String TypeName_Table = new String( "table" );
		public static readonly String TypeName_UserData = new String( "userdata" );
		public static readonly String TypeName_Function = new String( "function" );
		public static readonly String TypeName_Thread = new String( "thread" );

		/// <summary>
		/// In the same order as <see cref="ValueType"/>.
		/// </summary>
		public static readonly String[] TypeNames =
		{
			TypeName_Nil,

			TypeName_Bool,
			TypeName_Number,

			TypeName_String,
			TypeName_Table,
			TypeName_UserData,
			TypeName_Function,
			TypeName_Thread,
		};

		public static readonly String TagMethod_Index = new String( "__index" );
		public static readonly String TagMethod_NewIndex = new String( "__newindex" );
		public static readonly String TagMethod_Gc = new String( "__gc" );
		public static readonly String TagMethod_Mode = new String( "__mode" );
		public static readonly String TagMethod_Len = new String( "__len" );
		public static readonly String TagMethod_Eq = new String( "__eq" );
		public static readonly String TagMethod_Add = new String( "__add" );
		public static readonly String TagMethod_Sub = new String( "__sub" );
		public static readonly String TagMethod_Mul = new String( "__mul" );
		public static readonly String TagMethod_Div = new String( "__div" );
		public static readonly String TagMethod_Mod = new String( "__mod" );
		public static readonly String TagMethod_Pow = new String( "__pow" );
		public static readonly String TagMethod_Unm = new String( "__unm" );
		public static readonly String TagMethod_Lt = new String( "__lt" );
		public static readonly String TagMethod_Le = new String( "__le" );
		public static readonly String TagMethod_Concat = new String( "__concat" );
		public static readonly String TagMethod_Call = new String( "__call" );

		/// <summary>
		/// In the same order as <see cref="TagMethods"/>.
		/// </summary>
		public static readonly String[] TagMethodNames =
		{
			TagMethod_Index,
			TagMethod_NewIndex,
			TagMethod_Gc,
			TagMethod_Mode,
			TagMethod_Len,
			TagMethod_Eq,
			TagMethod_Add,
			TagMethod_Sub,
			TagMethod_Mul,
			TagMethod_Div,
			TagMethod_Mod,
			TagMethod_Pow,
			TagMethod_Unm,
			TagMethod_Lt,
			TagMethod_Le,
			TagMethod_Concat,
			TagMethod_Call,
		};

	}

	internal enum TagMethods
	{
		Index,
		NewIndex,
		Gc,
		Mode,
		Len,
		Eq,
		Add,
		Sub,
		Mul,
		Div,
		Mod,
		Pow,
		Unm,
		Lt,
		Le,
		Concat,
		Call,
	}
}
