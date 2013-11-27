local t = { a = 1, b = 2, c = 3, d = 4 };

for k, v in pairs( t ) do
	if k == "b" then
		t[k] = nil;
	end
end

local sum = 0;
for k, v in pairs( t ) do
	sum = sum + v;
end

return sum;