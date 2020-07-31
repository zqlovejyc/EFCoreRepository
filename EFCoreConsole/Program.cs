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

using System;
using Microsoft.EntityFrameworkCore;
using EFCoreRepository;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Autofac;
using System.Reflection;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;

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
            if (categoryName == "Microsoft.EntityFrameworkCore.Database.Command" && logLevel == LogLevel.Information)
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

    /// <summary>
    /// Sqlserver
    /// </summary>
    public class SqlserverDbContext : DbContext
    {
        /// <summary>
        /// OnConfiguring
        /// </summary>
        /// <param name="optionsBuilder"></param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseLoggerFactory(new LoggerFactory(new[] { new EFLoggerProvider() }))//添加日志监测
                .UseSqlServer(@"Server=192.168.70.154;Database=Drumbeat_SupplyChain_Base;Uid=root;Pwd=123;");
        }

        /// <summary>
        /// 配置实体模型，该方法在多次实例化SqlserverDbContext过程中只会执行一次
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var entityTypes = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(type => !string.IsNullOrWhiteSpace(type.Namespace))
                .Where(type => type.GetTypeInfo().IsClass)
                .Where(type => type.GetTypeInfo().BaseType != null)
                .Where(type => type != typeof(BaseEntity) && typeof(BaseEntity).IsAssignableFrom(type))
                .ToList();
            foreach (var entityType in entityTypes)
            {
                if (modelBuilder.Model.FindEntityType(entityType) != null)
                    continue;
                modelBuilder.Model.AddEntityType(entityType);
            }
        }
    }

    /// <summary>
    /// Mysql
    /// </summary>
    public class MysqlDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseLoggerFactory(new LoggerFactory(new[] { new EFLoggerProvider() }))//添加日志监测
                .UseMySQL(@"Server=192.168.70.154;Database=Drumbeat_SupplyChain_Base;Uid=root;Pwd=123;SslMode=None;Max Pool Size=1000;");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            //var assemblyEntity = DependencyContext.Default.RuntimeLibraries.SelectMany(i => i.Dependencies.Where(z => z.Name.Contains("EFCoreConsole")).Select(x => x.Name)).FirstOrDefault();
            var entityTypes = Assembly
                //.Load(assemblyEntity)
                .GetExecutingAssembly()
                .GetTypes()
                .Where(type => !string.IsNullOrWhiteSpace(type.Namespace))
                .Where(type => type.GetTypeInfo().IsClass)
                .Where(type => type.GetTypeInfo().BaseType != null)
                .Where(type => type != typeof(BaseEntity) && typeof(BaseEntity).IsAssignableFrom(type))
                .ToList();
            foreach (var entityType in entityTypes)
            {
                if (modelBuilder.Model.FindEntityType(entityType) != null)
                    continue;
                modelBuilder.Model.AddEntityType(entityType);
            }
        }
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
        public static void Main(string[] args)
        {
            var tasks = new List<Task>();
            var builder = new ContainerBuilder();
            builder.Register(c => new MysqlDbContext()).As<DbContext>();
            builder.Register(c => new MySqlRepository(c.Resolve<DbContext>())).As<IRepository>();//.SingleInstance();
            var container = builder.Build();
            MysqlFindListTest(container);
            for (int i = 0; i < 500; i++)
            {
                var task = Task.Run(() => MysqlFindListTest(container));
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());
            Console.WriteLine($"------------------------------------完成------------------------------------");
            Console.ReadLine();
        }

        /// <summary>
        /// Sqlserver测试
        /// </summary>
        public static void SqlserverFindListTest()
        {
            using (var db = new SqlRepository(new SqlserverDbContext()))
            {
                var result = db.FindList<dynamic>("select top 1 * from Base_Area");
                Console.WriteLine(JsonConvert.SerializeObject(result));
                Console.WriteLine($"当前线程ID:{Thread.CurrentThread.ManagedThreadId}");
            }
        }

        /// <summary>
        /// Mysql测试
        /// </summary>
        /// <param name="container"></param>
        public static void MysqlFindListTest(IContainer container)
        {
            using (var db = container.Resolve<IRepository>())
            {
                var result = db.FindList<dynamic>("select * from Base_Employee limit 0,1");
                Console.WriteLine(JsonConvert.SerializeObject(result));
                Console.WriteLine($"当前线程ID:{Thread.CurrentThread.ManagedThreadId}");
            }
        }
    }
}
