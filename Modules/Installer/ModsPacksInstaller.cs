using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MinecraftLaunch.Modules.Interface;
using MinecraftLaunch.Modules.Models.Download;
using MinecraftLaunch.Modules.Models.Install;
using MinecraftLaunch.Modules.Toolkits;
using Natsurainko.Toolkits.IO;
using Natsurainko.Toolkits.Network;
using Natsurainko.Toolkits.Network.Model;
using Newtonsoft.Json;

namespace MinecraftLaunch.Modules.Installer;

public class ModsPacksInstaller : InstallerBase
{
	private int _totalDownloaded;

	private int _needToDownload;

	private int _failedFiles = -1;

	public string ModPacksPath { get; set; }

	public string GamePath { get; set; }

	public string ApiKey { get; set; }

	public string GameId { get; set; }

	public async ValueTask<InstallerResponse> InstallAsync(Action<(float, string)> action)
	{
		Action<(float, string)> action2 = action;
		IProgress<(float, string)> progress = new Progress<(float, string)>();
		((Progress<(float, string)>)progress).ProgressChanged += ProgressChanged;
		progress.Report((0.1f, "开始获取整合包信息"));

		ModsPacksModel info = await GetModsPacksInfo();
		_needToDownload = info.Files.Count;
		string idpath = Path.Combine(Path.GetFullPath(GamePath), "versions", string.IsNullOrEmpty(GameId) ? info.Name : GameId);
		DirectoryInfo di = new DirectoryInfo(Path.Combine(idpath, "mods"));
		if (!di.Exists) {		
			di.Create();
		}
		progress.Report((0.4f, "开始解析整合包模组链接"));

		TransformManyBlock<IEnumerable<ModsPacksFileModel>, (long, long)> urlBlock = new TransformManyBlock<IEnumerable<ModsPacksFileModel>, (long, long)>((IEnumerable<ModsPacksFileModel> urls) => urls.Select((ModsPacksFileModel file) => (file.ProjectId, file.FileId)));
		using (ZipArchive subPath = ZipFile.OpenRead(ModPacksPath))
		{
			foreach (ZipArchiveEntry i in subPath.Entries)
			{
				if (i.FullName.StartsWith("overrides") && !string.IsNullOrEmpty(ZipExtension.GetString(subPath.GetEntry(i.FullName))))
				{
					string cutpath = i.FullName.Replace("overrides/", string.Empty);
					FileInfo v = new FileInfo(Path.Combine(idpath, cutpath));
					if (!Directory.Exists(Path.Combine(idpath, v.Directory.FullName)))
					{
						Directory.CreateDirectory(Path.Combine(idpath, v.Directory.FullName));
					}
					ZipExtension.ExtractTo(subPath.GetEntry(i.FullName), Path.Combine(idpath, cutpath));
				}
			}
		}
		GameCoreToolkit.GetGameCore(GamePath, GameId);
		progress.Report((0.45f, "开始下载整合包模组"));

		ActionBlock<(long, long)> actionBlock = new ActionBlock<(long, long)>(async delegate((long, long) t)
		{
			_ = 1;
			try
			{
				string url = await GetDownloadUrl(t.Item1, t.Item2);
				if ((await HttpWrapper.HttpDownloadAsync(new HttpDownloadRequest
				{
					Url = url,
					Directory = di,
					FileName = Path.GetFileName(url)
				})).HttpStatusCode != HttpStatusCode.OK)
				{
					_failedFiles++;
				}
				_totalDownloaded++;
				int e2 = _totalDownloaded / _needToDownload;
				progress.Report((0.2f + (float)e2 * 0.8f, $"下载Mod中：{_totalDownloaded}/{_needToDownload}"));
			}
			catch (Exception)
			{
				_failedFiles++;
			}
		}, new ExecutionDataflowBlockOptions
		{
			BoundedCapacity = 32,
			MaxDegreeOfParallelism = 32
		});
		DataflowLinkOptions linkOptions = new DataflowLinkOptions
		{
			PropagateCompletion = true
		};
		urlBlock.LinkTo(actionBlock, linkOptions);
		urlBlock.Post(info.Files);
		urlBlock.Complete();
		await actionBlock.Completion;
		progress.Report((1f, "安装完成"));

		if (_failedFiles != -1)
		{
			return new InstallerResponse
			{
				Exception = null,
				GameCore = null,
				Success = false
			};
		}
		return new InstallerResponse
		{
			Exception = null,
			GameCore = new GameCoreToolkit(GamePath).GetGameCore(GameId),
			Success = true
		};
		void ProgressChanged(object _, (float, string) e)
		{
			action2(e);
		}
	}

	public async ValueTask<ModsPacksModel> GetModsPacksInfo()
	{
		string json = string.Empty;
		using ZipArchive zipinfo = ZipFile.OpenRead(ModPacksPath);
		if (zipinfo.GetEntry("manifest.json") != null)
		{
			json = ZipExtension.GetString(zipinfo.GetEntry("manifest.json"));
		}
		return await ValueTask.FromResult(json.ToJsonEntity<ModsPacksModel>());
	}

	public async ValueTask<string> GetDownloadUrl(long addonId, long fileId)
	{
		string BaseUrl = "https://api.curseforge.com/v1";
		string reqUrl = $"{BaseUrl}/mods/{addonId}/files/{fileId}/download-url";
		using HttpResponseMessage res = await new HttpClient().SendAsync(Req(HttpMethod.Get, reqUrl));
		return JsonConvert.DeserializeObject<DataModel<string>>(await res.Content.ReadAsStringAsync())?.Data;
	}

	private HttpRequestMessage Req(HttpMethod method, string url)
	{
		return new HttpRequestMessage(method, url)
		{
			Headers = { { "x-api-key", ApiKey } }
		};
	}

	public ModsPacksInstaller(string modPacksPath, string gamePath, string apiKey, string gameid = null)
	{
		ModPacksPath = modPacksPath;
		GamePath = gamePath;
		ApiKey = apiKey;
		GameId = gameid;
	}
}
