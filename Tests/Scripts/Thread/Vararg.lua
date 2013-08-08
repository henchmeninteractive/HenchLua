function tail( _, ... )
	return ...;
end

function func( x, y, ... )
	return x + y, tail( ... );
end

return func( 40, 2, nil, true, false );