using MinecraftLaunch.Modules.Interface;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftLaunch.Modules.Toolkits
{
    /// <summary>
    /// 存档操作工具箱
    /// </summary>
    public partial class SavesToolkit
    {
        public ValueTask<ImmutableArray<decimal>> LoadAllAsync()
        {
            var names = Directory.GetDirectories(Path.Combine(Toolkit.Root.FullName, "versions"));
            names.ToList().ForEach(x =>
            {

            });
            throw new NotImplementedException();
        }
    }

    partial class SavesToolkit
    {
        public SavesToolkit(string path)
        {
            var info = new DirectoryInfo(path);
            Toolkit = new(info);
        }

        public SavesToolkit(DirectoryInfo info)
        {
            Toolkit = new(info);
        }

        public SavesToolkit(GameCoreToolkit toolkit)
        {
            Toolkit = toolkit;
        }
    }

    partial class SavesToolkit
    {
        public GameCoreToolkit Toolkit { get; set; }
    }
}
