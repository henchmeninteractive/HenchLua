local t = { 243, 234, 4421, 233, 3421, 23453, 34, 3453, 76756, 07878, 453, 54799, 53, 1, 09, 453, 8667, 34, 234, 4365, 09896, 4324, 87562, 5453, 521, 9586743, 44321, 241465, 2343, 4345, 443 };

table.sort( t );

for i = 2, #t do
	if t[i - 1] > t[i] then
		return -1;
	end
end

table.sort( t );

for i = 2, #t do
	if t[i - 1] > t[i] then
		return -2;
	end
end

table.sort( t, function( a, b ) return b < a; end )

for i = 2, #t do
	if t[i - 1] < t[i] then
		return -3;
	end
end

return 42;