using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using MinecraftLaunch.Modules.Interface;
using MinecraftLaunch.Modules.Models.Download;
using MinecraftLaunch.Modules.Models.Install;
using MinecraftLaunch.Modules.Models.Launch;
using MinecraftLaunch.Modules.Toolkits;
using Natsurainko.Toolkits.IO;
using Natsurainko.Toolkits.Network;
using Natsurainko.Toolkits.Network.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MinecraftLaunch.Modules.Installer;

public class OptiFineInstaller : InstallerBase
{
	public string CustomId { get; private set; }

	public GameCoreToolkit GameCoreLocator { get; private set; }

	public string JavaPath { get; private set; }

	public OptiFineInstallEntity OptiFineBuild { get; private set; }

	public string PackageFile { get; private set; }

	public async ValueTask<InstallerResponse> InstallAsync(Action<(float, string)> action)
	{
		Action<(float, string)> action2 = action;
		IProgress<(float, string)> progress2 = new Progress<(float, string)>();
		((Progress<(float, string)>)progress2).ProgressChanged += ProgressChanged;

        #region Download PackageFile
        progress2.Report((0f, "开始下载 OptiFine 安装包"));
        if (string.IsNullOrEmpty(PackageFile) || !File.Exists(PackageFile))
        {
            var downloadResponse = await DownloadOptiFinePackageFromBuildAsync(this.OptiFineBuild, GameCoreLocator.Root, (progress, message) =>
            {
                progress2.Report((0.15f * progress, "下载 OptiFine 安装包中 " + message));
            });

            if (downloadResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new HttpRequestException(downloadResponse.HttpStatusCode.ToString());

            PackageFile = downloadResponse.FileInfo.FullName;
        }
        #endregion

        progress2.Report((0.45f, "开始解析 OptiFine 安装包"));
		using (ZipArchive archive = ZipFile.OpenRead(PackageFile))
		{
			string launchwrapper = "1.12";
			if (archive.GetEntry("launchwrapper-of.txt") != null)
			{
				launchwrapper = ZipExtension.GetString(archive.GetEntry("launchwrapper-of.txt"));
			}
			progress2.Report((0.55f, "开始检查继承的核心"));
			if (GameCoreLocator.GetGameCore(OptiFineBuild.McVersion) == null)
			{
				await new GameCoreInstaller(GameCoreLocator, OptiFineBuild.McVersion).InstallAsync(delegate((float, string) e)
				{
					progress2.Report((0.45f + 0.15000004f * e.Item1, "正在下载继承的游戏核心：" + e.Item2));
				});
			}

			OptiFineGameCoreJsonEntity entity = new OptiFineGameCoreJsonEntity
			{
				Id = (string.IsNullOrEmpty(CustomId) ? $"{OptiFineBuild.McVersion}-OptiFine_{OptiFineBuild.Type}_{OptiFineBuild.Patch}" : CustomId),
				InheritsFrom = OptiFineBuild.McVersion,
				Time = DateTime.Now.ToString("O"),
				ReleaseTime = DateTime.Now.ToString("O"),
				Type = "release",
				Libraries = new List<MinecraftLaunch.Modules.Models.Launch.LibraryJsonEntity>
				{
					new MinecraftLaunch.Modules.Models.Launch.LibraryJsonEntity
					{
						Name = $"optifine:Optifine:{OptiFineBuild.McVersion}_{OptiFineBuild.Type}_{OptiFineBuild.Patch}"
					},
					new MinecraftLaunch.Modules.Models.Launch.LibraryJsonEntity
					{
						Name = (launchwrapper.Equals("1.12") ? "net.minecraft:launchwrapper:1.12" : ("optifine:launchwrapper-of:" + launchwrapper))
					}
				},
				MainClass = "net.minecraft.launchwrapper.Launch",
				Arguments = new MinecraftLaunch.Modules.Models.Install.ArgumentsJsonEntity
				{
					Game = new()
					{
						"--tweakClass",
					    "optifine.OptiFineTweaker"
					}
				}
			};
			progress2.Report((0.7f, "开始写入文件"));
			progress2.Report((0.75f, "开始分析是否安装模组加载器"));
			string id = (string.IsNullOrEmpty(CustomId) ? $"{OptiFineBuild.McVersion}-OptiFine-{OptiFineBuild.Type}_{OptiFineBuild.Patch}" : CustomId);
			bool flag;
			try
			{
				flag = GameCoreLocator.GetGameCore(id)?.HasModLoader ?? false;
			}
			catch (Exception)
			{
				flag = false;
			}

			if (!flag)
			{
				FileInfo versionJsonFile = new FileInfo(Path.Combine(GameCoreLocator.Root.FullName, "versions", entity.Id, entity.Id + ".json"));
				if (!versionJsonFile.Directory.Exists)
				{
					versionJsonFile.Directory.Create();
				}
				File.WriteAllText(versionJsonFile.FullName, entity.ToJson(IsIndented: true));
			}
			FileInfo launchwrapperFile = new LibraryResource
			{
				Name = entity.Libraries[1].Name,
				Root = GameCoreLocator.Root
			}.ToFileInfo();
			if (!launchwrapper.Equals("1.12"))
			{
				if (!launchwrapperFile.Directory.Exists)
				{
					launchwrapperFile.Directory.Create();
				}
				archive.GetEntry("launchwrapper-of-" + launchwrapper + ".jar").ExtractToFile(launchwrapperFile.FullName, overwrite: true);
			}
			else if (!launchwrapperFile.Exists)
			{
				await HttpToolkit.HttpDownloadAsync(new LibraryResource
				{
					Name = entity.Libraries[1].Name,
					Root = GameCoreLocator.Root
				}.ToDownloadRequest());
			}
			string inheritsFromFile = Path.Combine(GameCoreLocator.Root.FullName, "versions", OptiFineBuild.McVersion, OptiFineBuild.McVersion + ".jar");
			string v = Path.Combine(GameCoreLocator.Root.FullName, "versions", id);
			File.Copy(inheritsFromFile, Path.Combine(v, entity.Id + ".jar"), overwrite: true);
			FileInfo optiFineLibraryFiles = new LibraryResource
			{
				Name = entity.Libraries[0].Name,
				Root = GameCoreLocator.Root
			}.ToFileInfo();
			string optiFineLibraryFile = optiFineLibraryFiles.FullName;
			if (!optiFineLibraryFiles.Directory.Exists)
			{
				optiFineLibraryFiles.Directory.Create();
			}
			progress2.Report((0.85f, "运行安装程序处理器中"));
			InvokeStatusChangedEvent("运行安装程序处理器中", 0.85f);
			using Process process = Process.Start(new ProcessStartInfo(JavaPath)
			{
				UseShellExecute = false,
				WorkingDirectory = GameCoreLocator.Root.FullName,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				Arguments = string.Join(" ", "-cp", PackageFile, "optifine.Patcher", inheritsFromFile, PackageFile, optiFineLibraryFile)
			});
			List<string> outputs = new List<string>();
			List<string> errorOutputs = new List<string>();
			process.OutputDataReceived += delegate(object _, DataReceivedEventArgs args)
			{
				if (!string.IsNullOrEmpty(args.Data))
				{
					outputs.Add(args.Data);
				}
			};
			process.ErrorDataReceived += delegate(object _, DataReceivedEventArgs args)
			{
				if (!string.IsNullOrEmpty(args.Data))
				{
					outputs.Add(args.Data);
					errorOutputs.Add(args.Data);
				}
			};
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			await Task.Run((Func<bool>)process.WaitForInputIdle);
			if (flag)
			{
				FileInfo fileInfo = new FileInfo(optiFineLibraryFile);
				Console.WriteLine(fileInfo.FullName);
				string mods = Path.Combine(GameCoreLocator.Root.FullName, "versions", id, "mods");
				if (!Directory.Exists(mods))
				{
					Directory.CreateDirectory(mods);
				}
				Path.Combine(mods, fileInfo.Name);
				try
				{
					fileInfo.CopyTo(Path.Combine(mods, fileInfo.Name), overwrite: true);
				}
				finally
				{
					fileInfo.Directory.Delete(recursive: true);
				}
			}
			progress2.Report((1f, "安装完成"));
			InvokeStatusChangedEvent("安装完成", 1f);
			return new InstallerResponse
			{
				Success = true,
				GameCore = GameCoreLocator.GetGameCore(id),
				Exception = null
			};
		}
		void ProgressChanged(object _, (float, string) e)
		{
			action2(e);
		}
	}

	public static async Task<OptiFineInstallEntity[]> GetOptiFineBuildsFromMcVersionAsync(string mcVersion)
	{
		_ = 1;
		try
		{
			using HttpResponseMessage responseMessage = await HttpWrapper.HttpGetAsync((APIManager.Current.Host.Equals(APIManager.Mojang.Host) ? APIManager.Bmcl.Host : APIManager.Current.Host) + "/optifine/" + mcVersion, (Tuple<string, string>)null, HttpCompletionOption.ResponseContentRead);
			responseMessage.EnsureSuccessStatusCode();
			List<OptiFineInstallEntity> source = JsonConvert.DeserializeObject<List<OptiFineInstallEntity>>(await responseMessage.Content.ReadAsStringAsync());
			List<OptiFineInstallEntity> preview = source.Where((OptiFineInstallEntity x) => x.Patch.StartsWith("pre")).ToList();
			List<OptiFineInstallEntity> release = source.Where((OptiFineInstallEntity x) => !x.Patch.StartsWith("pre")).ToList();
			release.Sort((OptiFineInstallEntity a, OptiFineInstallEntity b) => (a.Type + "_" + a.Patch).CompareTo(b.Type + "_" + b.Patch));
			preview.Sort((OptiFineInstallEntity a, OptiFineInstallEntity b) => (a.Type + "_" + a.Patch).CompareTo(b.Type + "_" + b.Patch));
			List<OptiFineInstallEntity> list = preview.Union(release).ToList();
			list.Reverse();
			return list.ToArray();
		}
		catch
		{
			return Array.Empty<OptiFineInstallEntity>();
		}
	}

	public static Task<HttpDownloadResponse> DownloadOptiFinePackageFromBuildAsync(OptiFineInstallEntity build, DirectoryInfo directory, Action<float, string> progressChangedAction)
	{
		string downloadUrl = $"{(APIManager.Current.Host.Equals(APIManager.Mojang.Host) ? APIManager.Bmcl.Host : APIManager.Current.Host)}/optifine/{build.McVersion}/{build.Type}/{build.Patch}";
		return HttpWrapper.HttpDownloadAsync(new HttpDownloadRequest
		{
			Url = downloadUrl,
			Directory = directory
		}, progressChangedAction);
	}

	public OptiFineInstaller()
	{
		new HttpClient();
	}

	public OptiFineInstaller(GameCoreToolkit coreLocator, OptiFineInstallEntity build, string javaPath, string packageFile = null, string customId = null)
	{
		OptiFineBuild = build;
		JavaPath = javaPath;
		PackageFile = packageFile;
		GameCoreLocator = coreLocator;
		CustomId = customId;
	}
}
