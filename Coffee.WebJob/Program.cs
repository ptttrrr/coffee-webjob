using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.Azure.WebJobs;


namespace Coffee.WebJob
{
    class Program
    {
        static void Main()
        {
            JobHostConfiguration config = new JobHostConfiguration();
            config.UseTimers();
            JobHost host = new JobHost(config);
            host.RunAndBlock();
        }
    }
}

