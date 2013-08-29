--[[
	This is a fun little bug where the stack would be
	reallocated while invoking a non-Lua __index metamethod
	in an OpCode.Self, and the returned value would be
	assigned to the old stack.
]]

local mt =
{
	__index = g__index,
};

local t =
{
	fn_ = function( self, level )
		local x = level;
		local y = x;
		local z = y - 1;

		if z >= 0 then
			return self:fn( y - 1 );
		else
			return 42;
		end
	end,
};

setmetatable( t, mt );

return t:fn( 50 );