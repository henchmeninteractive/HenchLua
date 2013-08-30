assertEqual( "_3e-05", "_" .. 0.00003 );
assertEqual( "_4.5e+15", "_" .. 4500000000000000 );
assertEqual( "_4.5", "_" .. 4.5 );
assertEqual( "_0.7", "_" .. 0.7 );
assertEqual( "_0", "_" .. 0 );
assertEqual( "_4", "_" .. 4 );

return 42;