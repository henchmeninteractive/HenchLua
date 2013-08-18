local a = { };
local b = { };

if a == b then
	return 41;
end

if a ~= a then
	return 41;
end

return 42;