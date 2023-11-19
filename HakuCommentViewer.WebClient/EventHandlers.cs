using System;
using Microsoft.AspNetCore.Components;

namespace HakuCommentViewer.WebClient
{
    [EventHandler("ontransitionend", typeof(EventArgs), enableStopPropagation: true, enablePreventDefault: false)]
    [EventHandler("onanimationend", typeof(EventArgs), enableStopPropagation: true, enablePreventDefault: false)]
    public static class EventHandlers
    {
    }
}
