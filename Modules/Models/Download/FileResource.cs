using System.IO;
using MinecraftLaunch.Modules.Interface;
using Natsurainko.Toolkits.Network.Model;

namespace MinecraftLaunch.Modules.Models.Download;

public class FileResource : IResource
{
	public DirectoryInfo? Root { get; set; }

	public string? Name { get; set; }

	public int Size { get; set; }

	public string? CheckSum { get; set; }

	public string? Url { get; set; }

	public FileInfo? FileInfo { get; set; }

	public HttpDownloadRequest ToDownloadRequest()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		return new HttpDownloadRequest
		{
			Directory = FileInfo.Directory,
			FileName = Name,
			Sha1 = CheckSum,
			Size = Size,
			Url = Url
		};
	}

	public FileInfo? ToFileInfo()
	{
		return FileInfo;
	}
}
