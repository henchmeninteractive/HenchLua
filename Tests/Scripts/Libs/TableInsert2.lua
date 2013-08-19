local t = { 10, 20, 40, 50 }
table.insert( t, 3, 30 );

for i = 1, #t do
	if t[i] ~= 10 * i then
		return 41;
	end
end

return 42;