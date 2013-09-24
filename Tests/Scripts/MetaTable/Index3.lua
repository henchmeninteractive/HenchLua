local mt = { __index = function( obj, key ) if key == 2 then return 42; end end };
local t = { -1, nil, "WRONG", 5, 234, 11, 99 };
setmetatable( t, mt );
return t[2];