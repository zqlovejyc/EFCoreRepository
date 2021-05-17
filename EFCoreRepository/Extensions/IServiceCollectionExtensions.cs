#region License
/***
 * Copyright © 2018-2021, 张强 (943620963@qq.com).
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
using EFCoreRepository.Enums;
using EFCoreRepository.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
/****************************
* [Author] 张强
* [Date] 2020-10-19
* [Describe] IServiceCollection扩展类
* **************************/
namespace EFCoreRepository.Extensions
{
    /// <summary>
    /// IServiceCollection扩展类
    /// </summary>
    public static class IServiceCollectionExtensions
    {
        #region AddEFCoreRepository
        /// <summary>
        /// 注入EntityFrameworkCore仓储
        /// </summary>
        /// <param name="this">依赖注入服务集合</param>
        /// <param name="configuration">服务配置</param>
        /// <param name="defaultName">默认数据库名称</param>
        /// <param name="dbContextOptions">DbContext配置</param>
        /// <param name="countSyntax">分页计数语法，默认：COUNT(*)</param>
        /// <param name="lifeTime">生命周期，默认单例</param>
        /// <returns></returns>
        /// <example>
        ///     <code>
        ///     //appsetting.json
        ///     {
        ///         "Logging": {
        ///             "LogLevel": {
        ///                 "Default": "Information",
        ///                 "Microsoft": "Warning",
        ///                 "Microsoft.Hosting.Lifetime": "Information"
        ///             }
        ///         },
        ///         "AllowedHosts": "*",
        ///         "ConnectionStrings": {
        ///             "Base": [ "SqlServer", "数据库连接字符串" ],
        ///             "Sqlserver": [ "SqlServer", "数据库连接字符串" ],
        ///             "Oracle": [ "Oracle", "数据库连接字符串" ],
        ///             "MySql": [ "MySql", "数据库连接字符串" ],
        ///             "Sqlite": [ "Sqlite", "数据库连接字符串" ],
        ///             "Pgsql": [ "PostgreSql", "数据库连接字符串" ]
        ///         }
        ///     }
        ///     //Controller获取方法
        ///     private readonly IRepository _repository;
        ///     public WeatherForecastController(Func&lt;string, IRepository&gt; handler)
        ///     {
        ///         _repository = handler("Sqlserver");
        ///     }
        ///     </code>
        /// </example>
        public static IServiceCollection AddEFCoreRepository(
            this IServiceCollection @this,
            IConfiguration configuration,
            string defaultName,
            Action<DatabaseType, DbContextOptionsBuilder> dbContextOptions = null,
            string countSyntax = "COUNT(*)",
            ServiceLifetime lifeTime = ServiceLifetime.Singleton)
        {
            Func<string, IRepository> @delegate = key =>
            {
                key = key.IsNullOrEmpty() ? defaultName : key;
                var config = configuration.GetSection($"ConnectionStrings:{key}").Get<List<string>>();
                var databaseType = (DatabaseType)Enum.Parse(typeof(DatabaseType), config[0]);
                return databaseType switch
                {
                    DatabaseType.SqlServer => new SqlRepository(new DefaultDbContext(x =>
                    {
                        x.UseSqlServer(config[1]);
                        dbContextOptions?.Invoke(DatabaseType.SqlServer, x);
                    }))
                    {
                        CountSyntax = countSyntax
                    },
                    DatabaseType.MySql => new MySqlRepository(new DefaultDbContext(x =>
                    {
                        x.UseMySql(config[1], MySqlServerVersion.LatestSupportedServerVersion);
                        dbContextOptions?.Invoke(DatabaseType.MySql, x);
                    }))
                    {
                        CountSyntax = countSyntax
                    },
                    DatabaseType.Oracle => new OracleRepository(new DefaultDbContext(x =>
                    {
                        x.UseOracle(config[1]);
                        dbContextOptions?.Invoke(DatabaseType.Oracle, x);
                    }))
                    {
                        CountSyntax = countSyntax
                    },
                    DatabaseType.Sqlite => new SqliteRepository(new DefaultDbContext(x =>
                    {
                        x.UseSqlite(config[1]);
                        dbContextOptions?.Invoke(DatabaseType.Sqlite, x);
                    }))
                    {
                        CountSyntax = countSyntax
                    },
                    DatabaseType.PostgreSql => new NpgsqlRepository(new DefaultDbContext(x =>
                    {
                        x.UseNpgsql(config[1]);
                        dbContextOptions?.Invoke(DatabaseType.PostgreSql, x);
                    }))
                    {
                        CountSyntax = countSyntax
                    },
                    _ => throw new ArgumentException("数据库类型配置有误！"),
                };
            };
            switch (lifeTime)
            {
                case ServiceLifetime.Singleton:
                    @this.AddSingleton(x => @delegate);
                    break;
                case ServiceLifetime.Transient:
                    @this.AddTransient(x => @delegate);
                    break;
                case ServiceLifetime.Scoped:
                    @this.AddScoped(x => @delegate);
                    break;
                default:
                    break;
            }
            return @this;
        }

        /// <summary>
        /// 注入EntityFrameworkCore仓储
        /// </summary>
        /// <param name="this">依赖注入服务集合</param>
        /// <param name="type">数据库类型</param>
        /// <param name="dbContext">DbContext</param>
        /// <param name="countSyntax">分页计数语法，默认：COUNT(*)</param>
        /// <param name="lifeTime">生命周期，默认单例</param>
        /// <returns></returns>
        /// <example>
        ///     <code>
        ///     private readonly IRepository _repository;
        ///     public WeatherForecastController(IRepository repository)
        ///     {
        ///         _repository = repository;
        ///     }
        ///     </code>
        /// </example>
        public static IServiceCollection AddEFCoreRepository(
            this IServiceCollection @this,
            DatabaseType type,
            DbContext dbContext,
            string countSyntax = "COUNT(*)",
            ServiceLifetime lifeTime = ServiceLifetime.Singleton)
        {
            IRepository repository = type switch
            {
                DatabaseType.SqlServer => new SqlRepository(dbContext)
                {
                    CountSyntax = countSyntax
                },
                DatabaseType.MySql => new MySqlRepository(dbContext)
                {
                    CountSyntax = countSyntax
                },
                DatabaseType.Oracle => new OracleRepository(dbContext)
                {
                    CountSyntax = countSyntax
                },
                DatabaseType.Sqlite => new SqliteRepository(dbContext)
                {
                    CountSyntax = countSyntax
                },
                DatabaseType.PostgreSql => new NpgsqlRepository(dbContext)
                {
                    CountSyntax = countSyntax
                },
                _ => throw new ArgumentException("数据库类型有误！"),
            };
            switch (lifeTime)
            {
                case ServiceLifetime.Singleton:
                    @this.AddSingleton<IRepository>(repository);
                    break;
                case ServiceLifetime.Transient:
                    @this.AddTransient<IRepository>(x => repository);
                    break;
                case ServiceLifetime.Scoped:
                    @this.AddScoped<IRepository>(x => repository);
                    break;
                default:
                    break;
            }
            return @this;
        }
        #endregion
    }
}
