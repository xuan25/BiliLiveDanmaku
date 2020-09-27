using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiliLiveDanmaku.Modules
{
    public interface IModuleConfig
    {
        IModule CreateModule();
    }
}
