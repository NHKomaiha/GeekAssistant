﻿
using System.Windows.Forms;

namespace GeekAssistant.Modules.Global {
    internal static class SetTooltipInfo {
        public static void Run(ref ToolTip ToolTipName, Control control, string ToolTipTitle, string ToolTipText) {
            if (c.S.ShowToolTips) {
                if (ToolTipTitle != ToolTipName.ToolTipTitle) {
                    ToolTipName.ToolTipTitle = ToolTipTitle;
                }

                if (ToolTipText != ToolTipName.GetToolTip(control)) {
                    ToolTipName.SetToolTip(control, ToolTipText);
                }
            } else {
                ToolTipName.SetToolTip(control, "");
            }
        }
    }
}
