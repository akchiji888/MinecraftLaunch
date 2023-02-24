using System;

namespace MinecraftLaunch.Modules.Interface;

public class InstallerBase
{
	public event EventHandler<ProgressChangedEventArgs>? ProgressChanged;

	internal void InvokeStatusChangedEvent(float progress, string progressdescription)
	{
		this.ProgressChanged?.Invoke(this, new()
		{
			ProgressDescription = progressdescription,
			Progress = progress
		});
	}
}
