using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HakuCommentViewer.Plugin.Enums
{
    /// <summary>
    /// プラグイン動作状況
    /// </summary>
    public enum PluginStatus
    {
        Unkown = -1,
        Unload = 0,
        Loaded = 1,
        Starting = 10,
        Running = 11,
        Stoping = 12,
        Stoped = 13,
        Failed = 14,
        etc = 256
    }
}
