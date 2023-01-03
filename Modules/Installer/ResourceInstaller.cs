using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MinecraftLaunch.Modules.Interface;
using MinecraftLaunch.Modules.Models.Download;
using MinecraftLaunch.Modules.Models.Install;
using MinecraftLaunch.Modules.Models.Launch;
using MinecraftLaunch.Modules.Parser;
using MinecraftLaunch.Modules.Toolkits;
using Natsurainko.Toolkits.IO;
using Natsurainko.Toolkits.Network.Model;

namespace MinecraftLaunch.Modules.Installer;

public class ResourceInstaller : InstallerBase
{
	public GameCore GameCore { get; set; }

	public List<IResource> FailedResources { get; set; } = new List<IResource>();


	public static int MaxDownloadThreads { get; set; } = 128;


	public async ValueTask<ResourceInstallResponse> DownloadAsync(Action<string, float> func)
	{
		Action<string, float> func2 = func;
		Progress<(string, float)> progress = new Progress<(string, float)>();
		progress.ProgressChanged += Progress_ProgressChanged;
		TransformManyBlock<List<IResource>, IResource> manyBlock = new TransformManyBlock<List<IResource>, IResource>((List<IResource> x) => x.AsParallel().Where(delegate(IResource x)
		{
			if (string.IsNullOrEmpty(x.CheckSum) && x.Size == 0)
                return false;

            return (!FileExtension.Verify(x.ToFileInfo(), x.CheckSum) || !FileExtension.Verify(x.ToFileInfo(), x.Size)) ? true : false;
		}));
		int post = 0;
		int output = 0;
		ActionBlock<IResource> actionBlock = new ActionBlock<IResource>(async delegate(IResource resource)
		{
			await Task.Run(async() =>
			{
                post++;
                HttpDownloadRequest request = resource.ToDownloadRequest();
                if (!request.Directory.Exists)
                {
                    request.Directory.Create();
                }
                try
                {
                    if ((await HttpToolkit.HttpDownloadAsync(request)).HttpStatusCode != HttpStatusCode.OK)
                    {
                        FailedResources.Add(resource);
                    }
                }
                catch
                {
                    FailedResources.Add(resource);
                }
                output++;
                ((IProgress<(string, float)>)progress).Report(($"{output}/{post}", (float)output / (float)post));
            });
			//InvokeStatusChangedEvent($"{output}/{post}", (float)output / (float)post);
		}, new ExecutionDataflowBlockOptions
		{
			BoundedCapacity = MaxDownloadThreads,
			MaxDegreeOfParallelism = MaxDownloadThreads
		});
		IDisposable disposable = manyBlock.LinkTo(actionBlock, new DataflowLinkOptions
		{			
			PropagateCompletion = true
		});
		manyBlock.Post<List<IResource>>(GameCore.LibraryResources.Where((LibraryResource x) => x.IsEnable).Select((Func<LibraryResource, IResource>)((LibraryResource x) => x)).ToList());
		manyBlock.Post<List<IResource>>(GetFileResources().ToList());
		ITargetBlock<List<IResource>> target = manyBlock;
		target.Post(await GetAssetResourcesAsync());
		manyBlock.Complete();
		await actionBlock.Completion;
		disposable.Dispose();
		GC.Collect();
		progress.ProgressChanged -= Progress_ProgressChanged;
		return new ResourceInstallResponse
		{
			FailedResources = FailedResources,
			SuccessCount = post - FailedResources.Count,
			Total = post
		};
		void Progress_ProgressChanged(object _, (string, float) e)
		{
			func2(e.Item1, e.Item2);
		}
	}

	public IEnumerable<IResource> GetFileResources()
	{
		if (GameCore.ClientFile != null)
            yield return GameCore.ClientFile;
    }

    public async ValueTask<List<IResource>> GetAssetResourcesAsync()
	{
		if (!FileExtension.Verify(GameCore.AssetIndexFile.FileInfo, GameCore.AssetIndexFile.Size) && !FileExtension.Verify(GameCore.AssetIndexFile.FileInfo, GameCore.AssetIndexFile.CheckSum))
		{
			HttpDownloadRequest httpDownloadRequest = GameCore.AssetIndexFile.ToDownloadRequest();
			if (!httpDownloadRequest.Directory.Exists)
			{
				httpDownloadRequest.Directory.Create();
			}
			await HttpToolkit.HttpDownloadAsync(httpDownloadRequest);
		}
		return new AssetParser(new AssetJsonEntity().FromJson(await File.ReadAllTextAsync(GameCore.AssetIndexFile.ToFileInfo().FullName)), GameCore.Root).GetAssets().Select((Func<AssetResource, IResource>)((AssetResource x) => x)).ToList();
	}

	public ResourceInstaller(GameCore core)
	{
		GameCore = core;
	}
}
