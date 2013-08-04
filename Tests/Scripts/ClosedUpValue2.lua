local x = 38;

local function ffn()
	return function()
		x = x + 1;
	end
end

local fn = ffn();

fn();
fn();

x = x + 1;

fn();

return x;