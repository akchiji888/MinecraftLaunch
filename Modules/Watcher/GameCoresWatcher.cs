using MinecraftLaunch.Modules.Interface;
using MinecraftLaunch.Modules.Toolkits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftLaunch.Modules.Watcher
{
    /// <summary>
    /// 游戏核心监视器
    /// </summary>
    public class GameCoresWatcher : IWatcher
    {
        public GameCoresWatcher(GameCoreToolkit toolkit) {       
            Toolkit = toolkit;
        }

        public GameCoreToolkit Toolkit { get; private set; }

        public void StartWatch() {       
            //System.IO.di
        }
    }
}
