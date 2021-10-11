using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using taskproducer.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace taskproducer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        // POST api/<TaskController>
        [HttpPost]
        public async void Post([FromBody] TaskItem tasks)
        {
            string error = string.Empty;
            string token = string.Empty;
            try
            {
                var Client = new HttpClient();

                Dictionary<string, string> userData = new Dictionary<string, string>
                {
                    { "email", tasks.Email },
                    { "password", tasks.Password }
                };

                string Json = JsonConvert.SerializeObject(userData, Formatting.Indented);

                var content = new StringContent(Json, Encoding.UTF8, "application/json");
                //content.Headers.Add("Accept", "application/json");

                var response = await Client.PostAsync("https://reqres.in/api/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    token = System.Text.Json.JsonSerializer.Deserialize<string>(result);
                    Console.WriteLine("token:" + token);
                }
                else
                {
                    error = "No token";
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            var factory = new ConnectionFactory()
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
                Port = Convert.ToInt32(Environment.GetEnvironmentVariable("RABBITMQ_PORT"))
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "tasks",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                string message = !string.IsNullOrEmpty(token) ? tasks.Task : error;

                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "",
                                     routingKey: "tasks",
                                     basicProperties: null,
                                     body: body);
            }
        }
    }
}
