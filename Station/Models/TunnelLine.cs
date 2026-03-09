using System.Collections.Generic;

namespace Station.Models
{
    public class TunnelLine
    {
        public string LineId { get; set; } = string.Empty;
        public string LineName { get; set; } = string.Empty;
        public List<TunnelNode> Nodes { get; set; } = new();
    }
}
