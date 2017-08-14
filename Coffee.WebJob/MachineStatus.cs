using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
