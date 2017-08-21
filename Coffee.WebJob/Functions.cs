using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Data.SqlClient;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Diagnostics;

namespace Coffee.WebJob
{
    public class Functions
    {

        public static DateTime LastHour { get; set; }

        // Triggered every 15 minutes, between 8am-5pm, mon-fri.
        public static void TimerJob([TimerTrigger("0 */1 8-17 * * 1-5")] TimerInfo timerInfo) //temp to every minutes
        {
            // Init task to consume coffee data
            Task t = new Task(DownloadPageAsync);
            t.Start();

            Console.WriteLine("Timer job fired.");
        }


        // Fetching coffee data
        [NoAutomaticTrigger]
        static async void DownloadPageAsync()
        {
            string publicEndpoint = ConfigurationManager.AppSettings["endPoint"];
            List<Status> statusList = new List<Status>();
           

            // Getting data with httpclient
            using (HttpClient client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync(publicEndpoint))
            using (HttpContent content = response.Content)
            {
                // Reading result and filling
                string result = await content.ReadAsStringAsync();
                statusList = JsonConvert.DeserializeObject<List<Status>>(result);

                if (statusList != null)
                {
                    Console.WriteLine("TimeStamp: " + statusList.Last().Timestamp);
                    Console.WriteLine("Level: " + statusList.Last().Level);

                    // Sending last coffee level and timestamp to azure db
                    ExecuteSqlQuery(statusList.Last());

                    // Checking if last hour has been at 0%
                    CheckLastHour(statusList);
                }
            }
        }


        // Sending latest coffee data to db
        [NoAutomaticTrigger]
        static void ExecuteSqlQuery(Status latestStatus)
        {
            string connectionString = ConfigurationManager.AppSettings["azureDbConnectionString"];

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "INSERT INTO CoffeeStatus (Level, Timestamp) VALUES (@Level, @Timestamp)";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("Level", latestStatus.Level);
                    cmd.Parameters.AddWithValue("Timestamp", latestStatus.Timestamp);

                    connection.Open();

                    int result = cmd.ExecuteNonQuery();

                    if (result < 0)
                        System.Diagnostics.Trace.TraceError("Error when executing sql insert command.");
                }
            };
        }


        // Check if coffee maker has been empty for at least an hour
        static void CheckLastHour(List<Status> statusList)
        {
            List<Status> lastHourStatus = new List<Status>();
            bool outOfCoffee = false;

            // Time one hour reversed
            SetTime().Wait();

            // Get level status from last hour
            lastHourStatus = statusList.Where(t => t.Timestamp > LastHour).Select(s => new Status { Level = s.Level, Timestamp = s.Timestamp }).ToList();
  
            // Check if all level entries are zero, get last timestamp and send to alert
            foreach (Status s in lastHourStatus)
            {
                if (s.Level == "0.0")
                    outOfCoffee = true;
                else
                    outOfCoffee = true; // CHANGE AFTER TEST!!
            }

            DateTime timestamp = lastHourStatus.Select(s => s.Timestamp).Last();

            if (outOfCoffee)
                AlertIfCoffeeNeedsRefill(timestamp).Wait();
            else
                Trace.TraceInformation("Coffee levels have not reached disaster mode.");
        }


        static async Task<DateTime> SetTime()
        {
           // compensate for API jetlag
           LastHour = DateTime.Now.AddHours(-3);
           await Task.Delay(2000);
           return LastHour;
        }


        // Calling API about low coffee levels which triggers an zapier.com function that sends an email alert
        [NoAutomaticTrigger]
        static async Task AlertIfCoffeeNeedsRefill(DateTime _timestamp)
        {
            string personalEndpoint = ConfigurationManager.AppSettings["myEndpoint"];

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(personalEndpoint + "api/CoffeeStatus");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var sendData = new CoffeeFamine { Timestamp = _timestamp };
                var content = new StringContent(sendData.ToString());

                HttpResponseMessage response = await client.PostAsync(personalEndpoint, content);

                if (!response.IsSuccessStatusCode)
                    Trace.TraceError("Error when trying to post to external API: " + response.StatusCode);
            }
        }

    }
}
