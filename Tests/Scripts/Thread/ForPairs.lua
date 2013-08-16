local t =
{
	a = 10,
	b = 12,
	c = 20,
};

local sum = 0;

local numA = 0;
local numB = 0;
local numC = 0;

local numOther = 0;

for k, v in pairs( t ) do
	if k == "a" then
		numA = numA + 1;
	elseif k == "b" then
		numB = numB + 1;
	elseif k == "c" then
		numC = numC + 1;
	else
		numOther = numOther + 1;
	end

	sum = sum + v;
end

return
	numA == 1 and
	numB == 1 and
	numC == 1 and
	numOther == 0 and
	sum == 42;