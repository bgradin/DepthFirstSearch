using CommonCode.GameLogic;
using CommonCode.Networking;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Threading;
using System.Reflection;
using System.Runtime.InteropServices;

namespace GameServer
{
	public enum CtrlTypes : byte
	{
		CTRL_C_EVENT = 0,
		CTRL_BREAK_EVENT = 1,
		CTRL_CLOSE_EVENT = 2,
		CTRL_LOGOFF_EVENT = 5,
		CTRL_SHUTDOWN_EVENT = 6,
	}

	internal static class NativeMethods
	{
		public delegate bool HandlerRoutine(CtrlTypes flags);

		[DllImport("kernel32")]
		internal static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);
	}

	static class GameServer
	{
		//delegate needs to match for the P/Invoke call to SetConsoleCtrlHandler
		//This handles clicking the X button in the console window and hitting CTRL+C
		static bool Close(CtrlTypes flags)
		{
			try
			{
				server.Dispose();
				Console.WriteLine("\nServer closed successfully.\n");
			}
			catch
			{
				Console.WriteLine("Error closing server!");
			}

			Console.WriteLine("Event termination from: {0}", Enum.GetName(typeof(CtrlTypes), flags));
			return true;
		}

		static void AcceptConnection(object param)
		{
			IPEndPoint ep = (IPEndPoint)param;
			Console.Write("Client connected from: ");
			if (ep.Address.ToString() == "127.0.0.1")
				Console.WriteLine("127.0.0.1:{0}", ep.Port.ToString());
			else
				Console.WriteLine(ep.Address.ToString());
			Console.Write("\n> ");
		}

		static void CheckConfig(string fName)
		{
			Config m_serverConf = new Config(fName);
			m_serverConf.Load();
			int oInt;
			bool oBool;
			string oStr;
			Console.ForegroundColor = ConsoleColor.Yellow;
			if (!m_serverConf.GetValue("passwordlen", out oInt))
			{
				Console.WriteLine("Setting default config value passwordlen=8");
				m_serverConf.Add("general", "passwordlen", 8);
			}
			if (!m_serverConf.GetValue("passwordenc", out oBool))
			{
				Console.WriteLine("Setting default config value passwordenc=true");
				m_serverConf.Add("general", "passwordenc", true);
			}
			if (!m_serverConf.GetValue("loggedinusers", out oInt))
			{
				Console.WriteLine("Setting default config value loggedinusers=50");
				m_serverConf.Add("general", "loggedinusers", 50);
			}
			if(!m_serverConf.GetValue(ConfigConst.CONF_SERVER, ConfigConst.CONF_SERVER_DUAL, out oBool))
			{
				Console.WriteLine("Setting default config value dualmode=false");
				m_serverConf.Add(ConfigConst.CONF_SERVER, ConfigConst.CONF_SERVER_DUAL, false);
			}
			if (!m_serverConf.GetValue("server", "bindaddress", out oStr))
			{
				Console.WriteLine("Setting default config value bindaddress=0.0.0.0");
				m_serverConf.Add("server", "bindaddress", "0.0.0.0");
			}
			if (!m_serverConf.GetValue("server", "port", out oInt))
			{
				Console.WriteLine("Setting default config value port=8085");
				m_serverConf.Add("server", "port", 8085);
			}
			if(!m_serverConf.GetValue(ConfigConst.CONF_GAME, ConfigConst.CONF_GAME_START_MAP, out oInt))
			{
				Console.WriteLine("Setting default config value startmap=0");
				m_serverConf.Add(ConfigConst.CONF_GAME, ConfigConst.CONF_GAME_START_MAP, 0);
			}
			if (!m_serverConf.GetValue(ConfigConst.CONF_GAME, ConfigConst.CONF_GAME_START_X, out oInt))
			{
				Console.WriteLine("Setting default config value startx=29");
				m_serverConf.Add(ConfigConst.CONF_GAME, ConfigConst.CONF_GAME_START_X, 29);
			}
			if (!m_serverConf.GetValue(ConfigConst.CONF_GAME, ConfigConst.CONF_GAME_START_Y, out oInt))
			{
				Console.WriteLine("Setting default config value starty=79");
				m_serverConf.Add(ConfigConst.CONF_GAME, ConfigConst.CONF_GAME_START_Y, 79);
			}
			if (!m_serverConf.GetValue("database", "dbuser", out oStr))
			{
				Console.ResetColor();
				Console.WriteLine("Enter a username for the database connection: ");
				string user = Console.ReadLine();
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("Setting config value dbuser=" + user);
				m_serverConf.Add("database", "dbuser", user);
			}
			if (!m_serverConf.GetValue("database", "dbpass", out oStr))
			{
				Console.ResetColor();
				Console.WriteLine("Enter the password for the specified user: ");
				string pass = Console.ReadLine();
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("Setting config value dbpass=" + pass);
				m_serverConf.Add("database", "dbpass", pass);
			}
			if (!m_serverConf.GetValue("database", "dbaddr", out oStr))
			{
				Console.ResetColor();
				Console.WriteLine("Enter the server address for the database connection: ");
				string addr = Console.ReadLine();
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("Setting config value dbaddr=" + addr);
				m_serverConf.Add("database", "dbaddr", addr);
			}
			m_serverConf.SaveChanges();
			Console.ResetColor();
		}

		static bool acceptServerConsoleInput(ref string[] input)
		{
			Console.Write("> ");
			input = Console.ReadLine().Split(' ');
			if (input.Length == 0) //make sure we don't get an IndexOutOfRangeException
				return true; //length 0 is the user pressing 'enter' without typing anything.
			else
				return input[0] != "exit";
		}

		static Server server;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
			// Create server
			CheckConfig(Server.CONFIG_NAME);
			NativeMethods.SetConsoleCtrlHandler(new NativeMethods.HandlerRoutine(Close), true);
			try
			{
				server = new Server();
				if (!server.Start(new AcceptAction(AcceptConnection)))
				{
					Console.WriteLine("Error starting server!");
					server.Close();
					return;
				}
				else
				{
					if(IPAddress.Parse(server.ListenIP).AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
						Console.WriteLine("Server started. Listening on: {0}:{1}\n", server.ListenIP, server.ListenPort);
					else
						Console.WriteLine("Server started. Listening on: [{0}]:{1}\n", server.ListenIP, server.ListenPort);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error starting server: " + ex.Message);

				if (server != null)
					server.Close();

				return;
			}

			// Load map
			Map defaultMap = null;
			try
			{
				defaultMap = new Map(typeof(GameServer).Assembly.GetManifestResourceStream("GameServer.game.bmap"));

				World.CurrentMap = new Map(defaultMap);
			}
			catch
			{
			}

			// Initialize items
			World.PopulateBacteria();

			string[] input = { "" };

			// Input loop
			while (acceptServerConsoleInput(ref input))
			{
				List<KeyValuePair<IPEndPoint, string>> playerData = server.GetClientInfo(); //can't change ordering - otherwise we'll get bugs when kicking by index
				switch (input[0].ToLower())
				{
					case "help":
						Console.WriteLine("Commands:");
						Console.WriteLine("\t clear");
						Console.WriteLine("\t cls");
						Console.WriteLine("\t exit");
						Console.WriteLine("\t help");
						Console.WriteLine("\t kick [playername|index]");
						Console.WriteLine("\t prompte [playername]");
						Console.WriteLine("\t restart");
						Console.WriteLine("\t say all [message]");
						Console.WriteLine("\t status");
						break;
					case "kick":
						if (input.Length != 2)
						{
							Console.WriteLine(errorMessage);
							break;
						}

						if (playerData.Count == 0)
						{
							Console.WriteLine(noPlayersMessage);
							break;
						}

						// Figure out kick data
						int index = 0;
						KickData kd = int.TryParse(input[1], out index) ? new KickData(index) : new KickData(input[1]);

						if (!server.SendToClient(ServerAction.ServerKick, kd))
							Console.WriteLine("There was an error with your request.");
						break;
					case "promote":
						if(input.Length != 2)
						{
							Console.WriteLine(errorMessage);
							break;
						}

						if (server.PromoteUser(input[1]))
						{
							Console.WriteLine("Promoted " + input[1] + " to admin!");
							Console.WriteLine("You should probably kick them to force a re-log so everything works");
						}
						else
							Console.WriteLine("Error promoting user " + input[1]);
						break;
					case "say":
						if (input.Length < 3 || input[1] != "all")
						{
							Console.WriteLine(errorMessage + " Try \"say all [message]\"");
							break;
						}

						if (playerData.Count == 0)
						{
							Console.WriteLine(noPlayersMessage);
							break;
						}

						// Send server message
						string output = string.Join(" ", input.Skip(2));
						if (!server.SendToClient(ServerAction.ServerMessage, new TalkData(output)))
							Console.WriteLine("There was an error with your request.");
						break;
					case "status":
						if (input.Length != 1)
						{
							Console.WriteLine(errorMessage);
							break;
						}

						if (playerData.Count == 0)
						{
							Console.WriteLine(noPlayersMessage);
							break;
						}

						int i = 0;

						foreach (KeyValuePair<IPEndPoint, string> info in playerData)
						{
							if (!string.IsNullOrEmpty(info.Value))
								Console.WriteLine("  [{0}]: {1,-21} ({2})", i, info.Key.ToString(), info.Value);
							else
								Console.WriteLine("  [{0}]: {1,-21}", i, info.Key.ToString());
							i++;
						}
						break;
					case "clear":
					case "cls":
						if (input.Length != 1)
						{
							Console.WriteLine(errorMessage);
							break;
						}

						Console.Clear();
						break;
					case "restart":
						try
						{
							server.Dispose();
							Console.Write("Closed server. Restarting...");
						}
						catch
						{
							Console.Write("Error closing server! Restarting...");
						}

						try
						{
							server = new Server();
							if (!server.Start(new AcceptAction(AcceptConnection)))
								throw new ServerStartException("Error restarting server!");
						}
						catch (Exception ex)
						{
							Console.WriteLine("Error: " + ex.Message);
						}

						if (!server.Started)
							break;
						else
							Console.WriteLine("Restarted.");
						break;
					default:
						Console.WriteLine(errorMessage);
						break;
				}
			}

			Close(CtrlTypes.CTRL_CLOSE_EVENT);
			Console.WriteLine("Press any key to continue . . .");
			Console.ReadKey(true);
		}

		const string errorMessage = "Syntax error.";
		const string noPlayersMessage = "There are no players currently connected.";
	}
}
