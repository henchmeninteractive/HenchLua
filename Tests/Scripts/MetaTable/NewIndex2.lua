local mt =
{
	__newindex = { },
};
local t = { };

setmetatable( t, mt );
t.val = 42;

return mt.__newindex.val;