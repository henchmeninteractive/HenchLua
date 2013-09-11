namespace Henchmen.Lua
{
	public static class Literals
	{
		public static readonly LString VersionName = "Lua 5.2";

		public static readonly LString TypeName_Nil = "nil";
		public static readonly LString TypeName_Bool = "boolean";
		public static readonly LString TypeName_Number = "number";

		public static readonly LString TypeName_String = "string";
		public static readonly LString TypeName_Table = "table";
		public static readonly LString TypeName_UserData = "userdata";
		public static readonly LString TypeName_Function = "function";
		public static readonly LString TypeName_Thread = "thread";

		/// <summary>
		/// In the same order as <see cref="LValueType"/>.
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
	
		public static readonly LString TagMethod_Index = "__index";
		public static readonly LString TagMethod_NewIndex = "__newindex";
		public static readonly LString TagMethod_Gc = "__gc";
		public static readonly LString TagMethod_Mode = "__mode";
		public static readonly LString TagMethod_Len = "__len";
		public static readonly LString TagMethod_Eq = "__eq";
		public static readonly LString TagMethod_Add = "__add";
		public static readonly LString TagMethod_Sub = "__sub";
		public static readonly LString TagMethod_Mul = "__mul";
		public static readonly LString TagMethod_Div = "__div";
		public static readonly LString TagMethod_Mod = "__mod";
		public static readonly LString TagMethod_Pow = "__pow";
		public static readonly LString TagMethod_Unm = "__unm";
		public static readonly LString TagMethod_Lt = "__lt";
		public static readonly LString TagMethod_Le = "__le";
		public static readonly LString TagMethod_Concat = "__concat";
		public static readonly LString TagMethod_Call = "__call";

		public static readonly LString TagMethod_Pairs = "__pairs";
		public static readonly LString TagMethod_IPairs = "__ipairs";

		public static readonly LString TagInfo_Metatable = "__metatable";

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

		public static readonly LString Symbol_Hash = "#";
		public static readonly LString Symbol_Plus = "+";
		public static readonly LString Symbol_Minus = "-";
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
