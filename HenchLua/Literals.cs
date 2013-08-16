namespace Henchmen.Lua
{
	public static class Literals
	{
		public static readonly LString VersionName = new LString( "Lua 5.2" );

		public static readonly LString TypeName_Nil = new LString( "nil" );
		public static readonly LString TypeName_Bool = new LString( "boolean" );
		public static readonly LString TypeName_Number = new LString( "number" );

		public static readonly LString TypeName_String = new LString( "string" );
		public static readonly LString TypeName_Table = new LString( "table" );
		public static readonly LString TypeName_UserData = new LString( "userdata" );
		public static readonly LString TypeName_Function = new LString( "function" );
		public static readonly LString TypeName_Thread = new LString( "thread" );

		/// <summary>
		/// In the same order as <see cref="ValueType"/>.
		/// </summary>
		public static readonly LString[] TypeNames =
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
	
		public static readonly LString TagMethod_Index = new LString( "__index" );
		public static readonly LString TagMethod_NewIndex = new LString( "__newindex" );
		public static readonly LString TagMethod_Gc = new LString( "__gc" );
		public static readonly LString TagMethod_Mode = new LString( "__mode" );
		public static readonly LString TagMethod_Len = new LString( "__len" );
		public static readonly LString TagMethod_Eq = new LString( "__eq" );
		public static readonly LString TagMethod_Add = new LString( "__add" );
		public static readonly LString TagMethod_Sub = new LString( "__sub" );
		public static readonly LString TagMethod_Mul = new LString( "__mul" );
		public static readonly LString TagMethod_Div = new LString( "__div" );
		public static readonly LString TagMethod_Mod = new LString( "__mod" );
		public static readonly LString TagMethod_Pow = new LString( "__pow" );
		public static readonly LString TagMethod_Unm = new LString( "__unm" );
		public static readonly LString TagMethod_Lt = new LString( "__lt" );
		public static readonly LString TagMethod_Le = new LString( "__le" );
		public static readonly LString TagMethod_Concat = new LString( "__concat" );
		public static readonly LString TagMethod_Call = new LString( "__call" );

		public static readonly LString TagMethod_Pairs = new LString( "__pairs" );
		public static readonly LString TagMethod_IPairs = new LString( "__ipairs" );

		public static readonly LString TagInfo_Metatable = new LString( "__metatable" );

		/// <summary>
		/// In the same order as <see cref="TagMethods"/>.
		/// </summary>
		internal static readonly LString[] TagMethodNames =
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

		//the arithmetic TMs are in the same order as the corresponding op codes
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
