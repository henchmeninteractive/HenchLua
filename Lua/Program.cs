using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuaSharp
{
	interface ICall
	{
		void Call();
	}

	class DCall : ICall
	{
		public void Call()
		{
		}
	}

	class VCall : ICall
	{
		public virtual void Call()
		{
		}
	}

	class VCall2 : VCall
	{
		public override void Call()
		{
		}
	}

	class Program
	{
		static void Main( string[] args )
		{
			RunTest();

			Console.WriteLine();
			Console.WriteLine( "For real now" );

			RunTest();

			Console.ReadLine();
		}

		private static void RunTest()
		{
			var watch = new System.Diagnostics.Stopwatch();

			var dCall = new DCall();

			const int Iters = 1000000000;



			watch.Start();
			for( int i = 0; i < Iters; i++ )
				dCall.Call();
			watch.Stop();

			Console.WriteLine( "dCall: {0}", watch.Elapsed );
			watch.Reset();



			var idCall = (ICall)dCall;

			watch.Start();
			for( int i = 0; i < Iters; i++ )
				idCall.Call();
			watch.Stop();

			Console.WriteLine( "idCall: {0}", watch.Elapsed );
			watch.Reset();



			var vCall = new VCall();

			watch.Start();
			for( int i = 0; i < Iters; i++ )
				vCall.Call();
			watch.Stop();

			Console.WriteLine( "vCall: {0}", watch.Elapsed );
			watch.Reset();



			var ivCall = (ICall)vCall;

			watch.Start();
			for( int i = 0; i < Iters; i++ )
				ivCall.Call();
			watch.Stop();

			Console.WriteLine( "ivCall: {0}", watch.Elapsed );
			watch.Reset();



			var v2Call = (VCall)new VCall2();

			watch.Start();
			for( int i = 0; i < Iters; i++ )
				v2Call.Call();
			watch.Stop();

			Console.WriteLine( "v2Call: {0}", watch.Elapsed );
			watch.Reset();



			var iv2Call = (ICall)v2Call;

			watch.Start();
			for( int i = 0; i < Iters; i++ )
				iv2Call.Call();
			watch.Stop();

			Console.WriteLine( "iv2Call: {0}", watch.Elapsed );
			watch.Reset();



			var fdCall = (Action)dCall.Call;

			watch.Start();
			for( int i = 0; i < Iters; i++ )
				fdCall();
			watch.Stop();

			Console.WriteLine( "fdCall: {0}", watch.Elapsed );
			watch.Reset();



			var fvCall = (Action)vCall.Call;

			watch.Start();
			for( int i = 0; i < Iters; i++ )
				fvCall();
			watch.Stop();

			Console.WriteLine( "fvCall: {0}", watch.Elapsed );
			watch.Reset();



			var fv2Call = (Action)v2Call.Call;

			watch.Start();
			for( int i = 0; i < Iters; i++ )
				fv2Call();
			watch.Stop();

			Console.WriteLine( "fv2Call: {0}", watch.Elapsed );
			watch.Reset();
		}
	}
}
