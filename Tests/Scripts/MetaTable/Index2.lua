local mt = { __index = function( obj, key ) if key == "val" then return 42; end end };
local t = { };

setmetatable( t, mt );
return t.val;