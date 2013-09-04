HenchLua
===================

HenchLua is an implementation of the Lua VM in C#.

HenchLua is designed for .NET projects where calling out to unmanaged code isn't an option (for instance, web-based projects which must produce verifiably safe code). HenchLua is also suited for use on mobile platforms, with full support for Mono's mobile profile and its static native compiler.

HenchLua is **not**, however, a feature-complete implementation of Lua. We're adding features as we need them, and a lot is missing. That said, the code is fairly straight-forward (it's *certainly* more readable than the canonical Lua implementation), so adding additional features shouldn't be much of a burden. Bug reports and patches are appreciated, though feature requests are fairly low priority for the time being.

HenchLua is based on Lua 5.2.

What Works
-------------------

Thus far, the following features work at least well enough to be useful:

* Lua's primitive types, the byte-oriented string (`LString`) and the array/dictionary hybrid `Table`.
* Loading bytecode.
* Running bytecode.
* The `__index` metamethod.
* Parts of select standard libraries (`pairs`, `table.insert`, `math.sin`, etc).

What's Missing
-------------------

Many features are incomplete or outright missing. The main ones are:

* Most metamethods other than `__index`.
* Some string/number conversions.
* Coroutines.
* The Lua compiler.
* The debug libraries.
* Weak keys and values (we use the .NET GC directly, implementing these imposes significant overhead).

Design Goals
===================

HenchLua is designed to work in constrained .NET environments. That means we strive to always satisfy the following constraints:

* HenchLua compiles against compact or otherwise limited .NET Frmework profiles.
* We don't P/Invoke or use unsafe code.
* We don't allocate memory on the heap in cases where the standard Lua runtime itself wouldn't do so. In general, this means that if your Lua scripts are good about allocating memory, then HenchLua won't go and ruin all your hard work.
* We keep the live object graph as simple as possible. This helps keep GC cycles quick, particularly on platforms with weak GC implementations (such as XNA's Xbox 360 runtime).

As noted above, HenchLua is *not*, (I repeat, **not**) intended to be a feature-complete implementation of the Lua runtime. Apart from features that we simply haven't found a need for yet, there are numerous compromises which have been made in the interest of performance and efficiency. (However, HenchLua is still *conformant enough* to run a great deal of our existing Lua code - these compromises have not, by any means, rendered it useless.) If your scripts rely on subtle details of the Lua VM's implementation to function correctly, chances are they won't fare well in our VM.

HenchLua is designed to be efficient first and conformant to standard Lua second. If you need the reverse, check out the excellent [KopiLua](https://github.com/NLua/KopiLua) project.

The API
-------------------

HenchLua also discards most of Lua's standard APIs. Since HenchLua objects are just .NET objects, there's no reason to keep them behind a firewall, and values and tables can be created and accessed directly. The Lua stack is only necessary (or even relevant) when calling in or out of Lua code.

Lua types are represented as instances of the `Value` struct, which has a number of conversion operators that make it easy to convert to and from standard types. (Warning: there are a few rough edges on the interface.) `Value` is a fairly compact type: it's the size of an `object` reference and a `double`, and it uses a few clever tricks to avoid boxing (the only time converting to a `Value` allocates additional heap memory is when constructing instances which represent user-data of types which have special meaning to the runtime, such as `byte[]`).

Lua strings are represented by `LString` instances. `LString` is basically just a byte-oriented string, and it converts to and from the standard `string` type using the `UTF-8` encoding. Initializing an `LString` from a string does allocate memory, but those allocations can usually be done ahead of time and cached, so this is rarely a runtime issue. Internally, `LString` points directly to a `byte[]`, which contains some header data (namely the hash code) followed by the string value.

Lua tables are instances of the `Table` type, and they can be used almost like a `Dictionary<Value, Value>` (I say almost since they behave differently with respect to null/nil keys and values).

A block of bytecode can be loaded using `Function.Load`, and the resulting `Function` object can be run using an instance of the `Thread` type, which stores the state needed to keep track of executing Lua bytecode.

A `Thread` can run both Lua bytecode and .NET methods with a compatible signature (see `UserCallback` and `UserFunction`). These types together are represented using the `Callable` struct, which has an implicit conversion from both `Function` and compatible delegate types.

Standard libraries are accessed using the static types in the `Libs` namespace. Readonly fields are provided with the names and `Callable` instances of each function. A helper method is also provided to make it easy to register all of the library's members at once in a `Table` which will be used as the global environment when loading Lua code.