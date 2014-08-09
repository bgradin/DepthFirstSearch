using CommonCode.GameLogic;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace GameUpdater
{
	public partial class Form1 : Form
	{
		enum Status
		{
			Connecting,
			Downloading,
			Error
		}

		enum CompareResult
		{
			Same,
			Older,
			Newer
		}

		public Form1(string version, bool restartWhenDone)
		{
			status = Status.Connecting;
			allowedToClose = false;
			currentGameVersion = version;
			connector = null;
			fileList = new DataTable();
			receivedSize = 0;
			restart = restartWhenDone;

			InitializeComponent();
			UpdateProgress();

			Shown += Form1_Shown;
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			if (!allowedToClose)
				e.Cancel = true;
			else if (connector != null)
				connector.Close();

			base.OnClosing(e);
		}

		void Form1_Shown(object sender, EventArgs e)
		{
			if (!OpenMysqlConnection())
			{
				status = Status.Error;
				UpdateProgress();
				return;
			}

			if (!OpenFileList())
			{
				status = Status.Error;
				UpdateProgress();
				return;
			}

			if ((totalFileSize = GetTotalFileSize()) < 0)
			{
				status = Status.Error;
				UpdateProgress();
				return;
			}

			currentRow = null;
			status = Status.Downloading;
			StartLoadingNextFile();
		}

		void Form1_Load(object sender, EventArgs e)
		{
			UpdateProgress();
		}

		void downloader_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
		{
			status = Status.Downloading;
		}

		void downloader_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			UpdateProgress((int)(((double)(receivedSize + e.BytesReceived) / (double)totalFileSize) * 100));
		}

		void downloader_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			receivedSize += GetFileSize(currentRow);

			UpdateProgress((int)(((double)receivedSize / (double)totalFileSize) * 100));
			StartLoadingNextFile();
		}

		void FinalizeThings()
		{
			if (restart)
			{
				Process p = new Process();
				p.StartInfo.UseShellExecute = true;
				p.StartInfo.FileName = "Game.exe";
				p.Start();
			}

			allowedToClose = true;
			Application.Exit();
		}

		bool OpenMysqlConnection()
		{
			try
			{
				connector = new MySqlConnector(connectionString);
				connector.OpenConnection();
				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine("Error: {0}", e.ToString());
				return false;
			}
		}

		bool OpenFileList()
		{
			if (!connector.IsOpen)
				return false;

			try
			{
				string stm = "select * from files";
				DataTable fileList = connector.RunCommand(stm);

				for (int i = 0; i < fileList.Rows.Count; i++)
				{
					if (CompareVersions(fileList.Rows[i][1].ToString(), currentGameVersion) != CompareResult.Newer)
					{
						fileList.Rows.RemoveAt(i);
						i--;
					}
				}
				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine("Error: {0}", e.ToString());
				return false;
			}
		}

		long GetTotalFileSize()
		{
			if (fileList.Rows.Count == 0)
				return -1;

			long total = 0;

			foreach (DataRow row in fileList.Rows)
			{
				long size = GetFileSize(row);

				if (size != -1)
					total += size;
			}

			return total;
		}

		long GetFileSize(DataRow row)
		{
			string fileName = row[0].ToString();
			string fileVersion = row[1].ToString();

			try
			{
				WebRequest req = HttpWebRequest.Create(serverDirectory + fileName);
				req.Method = "HEAD";
				using (WebResponse resp = req.GetResponse())
				{
					int ContentLength;
					if (int.TryParse(resp.Headers.Get("Content-Length"), out ContentLength))
						return ContentLength;
					else
						return -1;
				}
			}
			catch (WebException e)
			{
				Console.WriteLine("Error: {0}", e.ToString());
				return -1;
			}
		}

		void StartLoadingNextFile()
		{
			if (currentRow == null)
				currentRow = fileList.Rows[0];
			else
			{
				int index = fileList.Rows.IndexOf(currentRow);

				if (fileList.Rows.Count - 1 > index)
					currentRow = fileList.Rows[index + 1];
				else
				{
					FinalizeThings();
					return;
				}
			}

			currentFileSize = GetFileSize(currentRow);

			try
			{
				downloader = new WebClient();

				string name = currentRow[0].ToString();

				if (name.Contains("\\"))
					Directory.CreateDirectory(name.Substring(0, name.LastIndexOf('\\')));

				downloader.DownloadFileAsync(new Uri(serverDirectory + name), name);
				downloader.DownloadProgressChanged += downloader_DownloadProgressChanged;
				downloader.OpenReadCompleted += downloader_OpenReadCompleted;
				downloader.DownloadFileCompleted += downloader_DownloadFileCompleted;
			}
			catch (WebException ex)
			{
				Console.WriteLine("Error: {0}", ex.ToString());
				status = Status.Error;
			}
		}

		void UpdateProgress(int percentComplete = 0)
		{
			if (status == Status.Error)
				allowedToClose = true;

			lblStatus.ForeColor = Color.FromArgb(status == Status.Error ? 255 : 0, 0, 0);

			if (status == Status.Downloading)
			{
				if (percentComplete <= 100)
					progressBar1.Value = percentComplete;
				else
					progressBar1.Value = 100;

				lblStatus.Text = percentComplete.ToString() + "%";
			}
			else
			{
				progressBar1.Value = 0;
				lblStatus.Text = status.ToString();
			}

			lblStatus.Top = progressBar1.Top + (progressBar1.Height / 2) - (lblStatus.Height / 2);
			lblStatus.Left = progressBar1.Left + (progressBar1.Width / 2) - (lblStatus.Width / 2);
		}

		CompareResult CompareVersions(string version1, string version2)
		{
			string[] nums1 = version1.Split('.'), num2 = version2.Split('.');

			for (int i = 0; i < 4; i++)
			{
				if (int.Parse(nums1[i]) < int.Parse(num2[i]))
					return CompareResult.Older;

				if (int.Parse(nums1[i]) > int.Parse(num2[i]))
					return CompareResult.Newer;
			}

			return CompareResult.Same;
		}

		Status status;
		bool allowedToClose;
		bool restart;
		string currentGameVersion;
		DataTable fileList;
		DataRow currentRow;
		long receivedSize;
		long totalFileSize;
		long currentFileSize;
		WebClient downloader;
		MySqlConnector connector;
		const string serverDirectory = @"C:\Users\Brian\Desktop\temp\";
		const string connectionString = @"server=localhost;userid=root;database=game";
	}
}
