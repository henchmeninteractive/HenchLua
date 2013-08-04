local x = 38;

local function fn()
	x = x + 1;
end

fn();
fn();

x = x + 1;

fn();

return x;