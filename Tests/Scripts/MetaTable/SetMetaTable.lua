local mt = { };
local t = { };

setmetatable( t, mt );
return getmetatable( t ) == mt;