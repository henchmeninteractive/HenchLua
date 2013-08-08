local function ffn()
	local x = 40;
	return function()
		x = x + 1;
		return x;
	end
end

local fn = ffn();

fn();
fn();
fn();

fn = ffn();

fn();
return fn();