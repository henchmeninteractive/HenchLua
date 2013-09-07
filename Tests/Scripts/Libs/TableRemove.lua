local t = { 10, 20, 999, 30, 40 }
table.remove( t, 3 );

for i = 1, #t do
	if t[i] ~= 10 * i then
		return 41;
	end
end

return 42;