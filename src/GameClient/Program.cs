using System;

namespace GameClient
{
#if WINDOWS
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		/// 
		[STAThread()]
		static void Main(string[] args)
		{
			using (AnglerGame game = new AnglerGame())
			{
				game.Run();
			}
		}
	}
#endif
}

