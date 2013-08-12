using System;

namespace HenchLua
{
	internal sealed class BoolBox
	{
		public readonly bool Value;
		private BoolBox( bool value )
		{
			Value = value;
		}

		public static readonly BoolBox True = new BoolBox( true );
		public static readonly BoolBox False = new BoolBox( false );

		public override string ToString()
		{
			return Value.ToString();
		}
	}

	internal sealed class NumBox
	{
		public double Value;

		public NumBox()
		{
		}

		public NumBox( double value )
		{
			this.Value = value;
		}

		public override string ToString()
		{
			return Value.ToString();
		}
	}

	internal sealed class ValueBox
	{
		public Value Value;

		public ValueBox()
		{
		}

		public ValueBox( Value value )
		{
			this.Value = value;
		}

		public override string ToString()
		{
			return Value.ToString();
		}
	}
}
