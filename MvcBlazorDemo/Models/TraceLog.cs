using System;

namespace MvcBlazorDemo.Models
{
    public class TraceLog
    {
        public int Id { get; set; }
        public string TraceIdentifier { get; set; }
        public string User { get; set; }
        public string LocalIpAddress { get; set; }
        public int LocalPort { get; set; }
        public string RemoteIpAddress { get; set; }
        public int RemotePort { get; set; }
        public DateTime DateTime { get; set; }
        public string Schema { get; set; }
        public string Host { get; set; }
        public string Path { get; set; }
        public string QueryString { get; set; }
        public string RequestBody { get; set; }
    }
}
