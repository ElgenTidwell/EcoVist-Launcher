using System;
using System.IO;
using System.Windows;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;

namespace EcoVist_Launcher
{
	enum LauncherStatus
	{
		Ready,
		Failed,
		DownloadingGame,
		DownloadingUpdate
	}

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private string rootPath;
		private string versionFile;
		private string patchnotes;
		private string gameZip;
		private string gameEXE;
		const double LAUNCHERVER = 1.1;

		private LauncherStatus _status;
		internal LauncherStatus Status
		{
			get => _status;
			set
			{
				_status = value;
				switch (_status)
				{
					case LauncherStatus.Ready:
						PlayButton.Content = "Play";
						break;
					case LauncherStatus.Failed:
						PlayButton.Content = "Update Failed - Retry";
						break;
					case LauncherStatus.DownloadingGame:
						PlayButton.Content = "Downloading Base Game...";
						break;
					case LauncherStatus.DownloadingUpdate:
						PlayButton.Content = "Downloading Patch...";
						break;
				}
			}
		}

		private void CheckForUpdates()
		{
			try
			{
				WebClient wc = new WebClient();
				double onlinever = double.Parse(wc.DownloadString("https://drive.google.com/uc?export=download&id=1fli70oLcTUTJgH2nFQO3_vZJoM6eIDfB"));

				if(onlinever > LAUNCHERVER)
				{
					MessageBox.Show("Your launcher is out of date! \n You should update your launcher, otherwise the game may not download correctly!", "Launcher out of date!");
					outOfDate_Warning.Text = "!OUT OF DATE LAUNCHER!";
					outOfDate_Warning.TextAlignment = TextAlignment.Center;
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show($"Error checking for launcher updates: {ex}");
			}

			if(File.Exists(versionFile))
			{
				Version localVer = new Version(File.ReadAllText(versionFile));
				VersionText.Text = localVer.ToString();

				try
				{
					WebClient wc = new WebClient();
					Version onlineVersion = new Version(wc.DownloadString("https://drive.google.com/uc?export=download&id=1tPfiu48Dr6eyrqemYdBN-f2K1C6zE94W"));

					if (onlineVersion.IsDifferentThan(localVer))
					{
						InstallGameFiles(true, onlineVersion);
					}
					else
					{
						Status = LauncherStatus.Ready;
					}
				}
				catch (Exception ex)
				{
					Status = LauncherStatus.Failed;
					MessageBox.Show($"Error checking for game updates: {ex}");
				}
			}else
			{
				InstallGameFiles(false, Version.zero);
			}
		}

		private void InstallGameFiles(bool isUpdate, Version ver)
		{
			try
			{
				WebClient wc = new WebClient();
				if(isUpdate)
				{
					Status = LauncherStatus.DownloadingUpdate;
					patchnotes = wc.DownloadString("https://drive.google.com/uc?export=download&id=1TDdaaZEr-db_eKzb0nxlM-iTCPNfQS3i");
				}
				else
				{
					Status = LauncherStatus.DownloadingGame;
					ver = new Version(wc.DownloadString("https://drive.google.com/uc?export=download&id=1tPfiu48Dr6eyrqemYdBN-f2K1C6zE94W"));
					patchnotes = wc.DownloadString("https://drive.google.com/uc?export=download&id=1TDdaaZEr-db_eKzb0nxlM-iTCPNfQS3i");
				}

				FileDownloader fd = new FileDownloader();

				fd.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
				fd.DownloadFileAsync("https://drive.google.com/uc?export=download&id=1p6eCH_RjI9xfiAdD-a1wMsVDpqlnLpv9",gameZip,ver);
			}
			catch (Exception ex)
			{
				Status = LauncherStatus.Failed;
				MessageBox.Show($"Error installing game updates: {ex}");
			}
		}

		private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
		{
			try
			{
				string onlineVer = ((Version)e.UserState).ToString();
				ZipFile.ExtractToDirectory(gameZip, rootPath, true);
				File.Delete(gameZip);

				File.WriteAllText(versionFile,onlineVer);

				VersionText.Text = onlineVer;
				PatchNotes.Text = patchnotes;
				Status = LauncherStatus.Ready;
			}
			catch (Exception ex)
			{
				Status = LauncherStatus.Failed;
				MessageBox.Show($"Error finishing download: {ex}");
			}
		}

		public MainWindow()
		{
			InitializeComponent();
			rootPath = Directory.GetCurrentDirectory();
			versionFile = Path.Combine(rootPath, "Version.txt");
			gameZip = Path.Combine(rootPath, "build.zip");
			gameEXE = Path.Combine(rootPath, "build", "EcoVist.exe");
			patchnotes = new WebClient().DownloadString("https://drive.google.com/uc?export=download&id=1TDdaaZEr-db_eKzb0nxlM-iTCPNfQS3i");
			PatchNotes.Text = patchnotes;
		}

		private void Window_ContentRendered(object sender, EventArgs e)
		{
			CheckForUpdates();
		}

		private void PlayButton_Click(object sender, RoutedEventArgs e)
		{
			if(File.Exists(gameEXE) && Status == LauncherStatus.Ready)
			{
				ProcessStartInfo processStartInfo = new ProcessStartInfo(gameEXE);
				processStartInfo.WorkingDirectory = Path.Combine(rootPath, "build");
				Process.Start(processStartInfo);

				Close();
			}
			else if(Status == LauncherStatus.Failed)
			{
				CheckForUpdates();
			}
		}
	}
	struct Version
	{
		internal static Version zero = new Version(0, 0, 0);

		short major, minor, patch;

		internal Version(short maj, short min, short pat)
		{
			this.major = maj;
			this.minor = min;
			this.patch = pat;
		}
		internal Version(string ver)
		{
			string[] _versionSplit = ver.Split('.');

			if(_versionSplit.Length != 3)
			{
				this.major = 0;
				this.minor = 0;
				this.patch = 0;
				return;
			}

			this.major = short.Parse(_versionSplit[0]);
			this.minor = short.Parse(_versionSplit[1]);
			this.patch = short.Parse(_versionSplit[2]);
		}

		internal bool IsDifferentThan(Version _other)
		{
			if(major != _other.major)
			{
				return true;
			}
			else
			{
				if (minor != _other.minor)
				{
					return true;
				}else
				{
					if (patch != _other.patch)
					{
						return true;
					}
				}
			}
			return false;
		}

		public override string ToString()
		{
			return $"{major}.{minor}.{patch}";
		}
	}
}
