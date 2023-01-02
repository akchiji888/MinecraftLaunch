using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftLaunch.Modules.Interface
{
    /// <summary>
    /// 分析器统一接口
    /// </summary>
    public interface IAnalyzer<T>
    {
        T AnalyseAsync();
    }
}
