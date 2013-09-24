local lval;

local mt =
{
	__newindex = function( t, k, v )
		lval = v;
	end,
};
local t = { };

setmetatable( t, mt );
t.val = 42;

return lval;