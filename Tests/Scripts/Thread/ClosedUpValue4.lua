local y = 0;

local function fnn( x )
	local ret = { };

	function ret:fn()
		return x + y;
	end

	return ret;
end

local f42 = fnn( 42 );
fnn( 41 );
fnn( 40 );
fnn( 39 );

return f42:fn();