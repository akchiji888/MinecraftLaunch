using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MinecraftLaunch.Modules.Interface;
using MinecraftLaunch.Modules.Models.Download;
using MinecraftLaunch.Modules.Models.Install;
using MinecraftLaunch.Modules.Models.Launch;
using MinecraftLaunch.Modules.Toolkits;
using Newtonsoft.Json;

namespace MinecraftLaunch.Modules.Installer
{
    public partial class GameCoreInstaller : InstallerBase
    {
        public async ValueTask<InstallerResponse> InstallAsync(Action<(float, string)> action)
        {
            IProgress<(float, string)> progress = new Progress<(float, string)>();
            ((Progress<(float, string)>)progress).ProgressChanged += ProgressChanged;
            void ProgressChanged(object _, (float, string) e) => action(e);
            try
            {
                progress.Report((0.1f, "正在获取 游戏核心Json"));
                GameCoreJsonEntity entity = JsonConvert.DeserializeObject<GameCoreJsonEntity>(await HttpToolkit.GetStringAsync(CoreInfo.Url));
                if (!string.IsNullOrEmpty(Id))
                {
                    entity.Id = Id;
                }
                progress.Report((0.15f, "正在下载 游戏核心Json"));
                FileInfo fileInfo = new FileInfo(Path.Combine(GameCoreToolkit.Root.FullName, "versions", entity.Id, entity.Id + ".json"));
                if (!fileInfo.Directory.Exists)
                {
                    fileInfo.Directory.Create();
                }
                await File.WriteAllTextAsync(fileInfo.FullName, entity.ToJson(), default);
                progress.Report((0.3f, "正在下载 游戏依赖资源"));
                await new ResourceInstaller(GameCoreToolkit.GetGameCore(entity.Id)).DownloadAsync(delegate (string a, float e)
                {
                    progress.Report((0.2f + e * 0.8f, "下载中 " + a));
                });
                progress.Report((1f, "安装完成"));
                return new InstallerResponse
                {
                    Success = true,
                    GameCore = GameCoreToolkit.GetGameCore(entity.Id),
                    Exception = null
                };
            }
            catch (Exception exception)
            {
                return new InstallerResponse
                {
                    Success = false,
                    GameCore = null,
                    Exception = exception
                };
            }
        }

        public static async ValueTask<GameCoresEntity> GetGameCoresAsync()
        {
            return (await HttpToolkit.GetStringAsync(APIManager.Mojang.VersionManifest)).ToJsonEntity<GameCoresEntity>();
        }
    }

    partial class GameCoreInstaller
    {
        public GameCoreInstaller(GameCoreToolkit gameCoreToolkit, string Id)
        {
            string Id2 = Id;
            GameCoreInstaller gameCoreInstaller = this;
            GameCoreToolkit = gameCoreToolkit;
            this.Id = Id2;
            GetGameCoresAsync().GetAwaiter().GetResult().Cores.ToList().ForEach(delegate (GameCoreEmtity x)
            {
                if (x.Id == Id2)
                    gameCoreInstaller.CoreInfo = x;
            });
        }

        public GameCoreToolkit GameCoreToolkit { get; set; }

        public GameCoreEmtity CoreInfo { get; set; }

        public string Id { get; set; }
    }
}