t = { };

t[#t + 1] = true;
t[#t + 1] = false;
t[#t + 1] = true;
t[#t + 1] = false;
t[#t + 1] = true;

return 8 * #t + 2;