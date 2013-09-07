using System;

namespace Henchmen.Lua
{
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
