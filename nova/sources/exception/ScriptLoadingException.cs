using System;
using System.Text;

namespace Nova.Exceptions;

public class ScriptLoadingException(string msg, FlowChartNode node = null, string extraInfo = null)
    : ArgumentException(ErrorMessage(msg, node, extraInfo))
{
    private static string ErrorMessage(string msg, FlowChartNode node, string extraInfo)
    {
        var sb = new StringBuilder();
        sb.Append("Nova: ").Append(msg).Append('.');

        if (node != null || extraInfo != null)
        {
            sb.Append(" Exception occurs at");
            if (node != null)
            {
                sb.Append(" node:").Append(node.Name);
            }
            if (extraInfo != null)
            {
                sb.Append(' ').Append(extraInfo);
            }
        }

        return sb.ToString();
    }
}
