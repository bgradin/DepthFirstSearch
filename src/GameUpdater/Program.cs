using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GameUpdater
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Application.Exit();
				return;
			}

			// Verify the command line parameter(s)
			string[] s = args[0].Split('.');

			foreach (string str in s)
			{
				int t;

				if (!int.TryParse(str, out t))
				{
					Application.Exit();
					return;
				}
			}

			bool restart = args.Length > 1 && args[1] == "-r";

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Form1(args[0], restart));
		}
	}
}
