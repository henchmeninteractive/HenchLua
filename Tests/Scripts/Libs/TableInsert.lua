local t = { 10, 20, 30 }
table.insert( t, 40 );

for i = 1, #t do
	if t[i] ~= 10 * i then
		return 41;
	end
end

return 42;