using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Net.Http;
using Newtonsoft.Json;
using System.Data.SqlClient;

namespace Coffee.WebJob
{
    class Program
    {
        static void Main()
        {
            Task t = new Task(DownloadPageAsync);
            t.Start();
            Console.ReadLine();
        }


        // Fetching coffee data
        static async void DownloadPageAsync()
        {
            string endpoint = ConfigurationManager.AppSettings["endPoint"];
            List<Status> statusList = new List<Status>();

            // Getting data with httpclient
            using (HttpClient client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync(endpoint))
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
                }
            }
        }


        // Sending latest coffee data to db
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

    }
}

