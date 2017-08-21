using System;

namespace Coffee.WebJob
{
    public class MachineStatus
    {
        public Status[] StatusList { get; set; }
    }

    public class Status
    {
        public string Id { get; set; }
        public string Level { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class CoffeeFamine
    {
        public DateTime Timestamp { get; set; }
    }
}
