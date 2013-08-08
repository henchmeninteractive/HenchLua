local mt = { __index = { val = 42 } };
local t = { };

setmetatable( t, mt );
return t.val;