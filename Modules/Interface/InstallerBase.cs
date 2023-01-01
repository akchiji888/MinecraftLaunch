using System;

namespace MinecraftLaunch.Modules.Interface;

public class InstallerBase
{
	public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

	public void InvokeStatusChangedEvent(string progressdescription, float progress)
	{
		this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs
		{
			ProgressDescription = progressdescription,
			Progress = progress
		});
	}
}
