using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HakuCommentViewer.Plugin.Enums
{
    /// <summary>
    /// プラグイン種別
    /// </summary>
    public enum PluginType
    {
        Unkown = -1,
        Comment = 0,
        Narrator = 1,
        etc = 256
    }
}
