using System;
using System.Runtime.Serialization;

namespace VTEX.FeedConsumer.Clients
{
    public class FeedDequeueResponse
    {
        public string EventId { get; set; }
        public string Handle { get; set; }
        public string Domain { get; set; }
        public string State { get; set; }
        public string LastState { get; set; }
        public string OrderId { get; set; }
        public DateTime LastChange { get; set; }
        public DateTime CurrentChange { get; set; }
        public DateTime AvailableDate { get; set; }
    }
}