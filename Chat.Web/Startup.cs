using Chat.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.DataProtection;
using Chat.Core.Models;
using Chat.Web.Hubs;
using Microsoft.AspNetCore.Identity;
using Chat.Core.Interfaces;
using Chat.Database.Services;
using Chat.Web.Services;
using Chat.Web.RabbitMQ;

namespace Chat.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            string connectionString = GetDatabaseConnectionString();

            services.AddControllersWithViews();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString)
            );
            services.AddDatabaseDeveloperPageExceptionFilter();
            services.AddDefaultIdentity<ChatUser>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            // Stores identity keys in database
            services.AddDataProtection()
                .PersistKeysToDbContext<ApplicationDbContext>();

            services.AddSignalR();

            /* Web only services */
            services.AddSingleton<ICommandService, CommandService>();

            /* Database services */
            services.AddScoped<IMessageService, MessageService>();
            services.AddScoped<IUserService, UserService>();

            /* Rabbit services */
            services.AddSingleton<IUserBotQueueProducer, UserBotQueueProducer>();
            services.AddHostedService<BotUsersQueueConsumer>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Chat}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
                endpoints.MapHub<ChatHub>("/chatter");
            });
        }

        private string GetDatabaseConnectionString()
        {
            // The next lines will help us to connect to a docker database instance
            string connectionString = "";

            string database = Configuration["DBDATABASE"];
            string host = Configuration["DBHOST"];
            string password = Configuration["DBPASSWORD"];
            string port = Configuration["DBPORT"];
            string user = Configuration["DBUSER"];

            // If any of the variables is null, get connectionString from appSettings.json
            if (new List<string>() { database, host, password, port }.Any(s => s == null))
            {
                connectionString = Configuration.GetConnectionString("DatabaseConnection");
            }
            else
            {
                connectionString = $"Server={host}, {port};Database={database};User Id={user};Password={password};";
            }
            return connectionString;
        }

    }
}
