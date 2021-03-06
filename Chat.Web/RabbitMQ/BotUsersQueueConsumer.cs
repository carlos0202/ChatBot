using System;
using System.Linq;
using System.Net.Http;
using Chat.Core.Interfaces;
using Chat.Core;
using Chat.Web.Models;
using Microsoft.Extensions.Configuration;
using Chat.Core.Services;
using Chat.Core.Utils;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.SignalR;
using Chat.Web.Hubs;
using Chat.Core.Entities;

namespace Chat.Web.RabbitMQ
{
    public class BotUsersQueueConsumer : BackgroundService, IBotUsersQueueConsumer
    {
        private HttpClient _client = new HttpClient();
        private IConsumerService _consumerService;
        private IHubContext<ChatHub> _hubContext;

        public BotUsersQueueConsumer(IConfiguration configuration, IHubContext<ChatHub> hubContext) :
            base()
        {
            _consumerService = new ConsumerService(Helper.GetConnection(configuration, "RabbitConnectionString"));
            _hubContext = hubContext;
        }


        public void WaitForBotResponse()
        {
            _consumerService.Consume<ChatMessage>
            (
                Constants.BOT_USERS_QUEUE,
                async botMessage =>
                {
                    /* It wasnt clear in the docs if I should save in the database the response for the command
                     * So, As the request command isn`t being saved, I didnt save the response
                     */
                    await _hubContext.Clients.All.SendAsync("receive", botMessage);
                }
            );
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            this.WaitForBotResponse();
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _consumerService.Dispose();
            base.Dispose();
        }
    }
}