#region License
/***
 * Copyright © 2018, 张强 (943620963@qq.com).
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * without warranties or conditions of any kind, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion

using EFCoreRepository.DbContexts;
using EFCoreRepository.Extensions;
using EFCoreRepository.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Threading.Tasks;

namespace EFCoreConsole
{
    /// <summary>
    /// EFLoggerProvider
    /// </summary>
    public class EFLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new EFLogger(categoryName);
        public void Dispose() { }
    }

    /// <summary>
    /// EFLogger
    /// </summary>
    public class EFLogger : ILogger
    {
        private readonly string categoryName;
        public EFLogger(string categoryName) => this.categoryName = categoryName;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            //ef core执行数据库查询时的categoryName为Microsoft.EntityFrameworkCore.Database.Command,日志级别为Information
            if (categoryName == DbLoggerCategory.Database.Command.Name && logLevel == LogLevel.Information)
            {
                var logContent = formatter(state, exception);
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(logContent);
                Console.ResetColor();
            }
        }
        public IDisposable BeginScope<TState>(TState state) => null;
    }

    [Table("Students")]
    public class Student : BaseEntity
    {
        [Key]
        public string Id { get; set; }
        public string Name { get; set; }
        public int? Age { get; set; }
        public string Address { get; set; }
        public DateTime? CreateDate { get; set; }
    }

    /// <summary>
    /// Program
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main
        /// </summary>
        /// <param name="args"></param>
        public static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();
            //services.AddEFCoreRepository(DatabaseType.MySql, new DefaultDbContext(x =>
            //{
            //    x.UseLoggerFactory(new LoggerFactory(new[] { new EFLoggerProvider() }));
            //    x.EnableSensitiveDataLogging(true);
            //    x.UseMySql(@"Server=192.168.70.154;Database=MyDatabase;Uid=root;Pwd=123;SslMode=None;");
            //}));

            services.AddEFCoreRepository(configuration, "MySqlTest", (x, y) =>
            {
                y.UseLoggerFactory(new LoggerFactory(new[] { new EFLoggerProvider() }));
                y.EnableSensitiveDataLogging(true);
            });

            var provider = services.BuildServiceProvider();
            var handler = provider.GetService<Func<string, IRepository>>();
            var repo = handler("MySqlTest");
            var result = await repo.FindListAsync<Student>(x => x.Id == "1", x => x.CreateDate, 10, 1);
            Console.WriteLine(JsonConvert.SerializeObject(result));

            Console.WriteLine($"------------------------------------完成------------------------------------");
            Console.ReadLine();
        }
    }
}
