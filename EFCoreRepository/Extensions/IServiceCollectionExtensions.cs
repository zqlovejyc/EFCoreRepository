#region License
/***
 * Copyright © 2018-2022, 张强 (943620963@qq.com).
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
using System.Linq;
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
        #region AddRepository
        /// <summary>
        /// 注入泛型仓储 <para>注意：仓储没有初始化DbContext</para>
        /// </summary>
        /// <typeparam name="T">仓储类型</typeparam>
        /// <param name="this">依赖注入服务集合</param>
        /// <param name="countSyntax">分页计数语法，默认：COUNT(*)</param>
        /// <param name="lifeTime">生命周期，默认：Scoped</param>
        /// <returns></returns>
        public static IServiceCollection AddRepository<T>(
            this IServiceCollection @this,
            string countSyntax = "COUNT(*)",
            ServiceLifetime lifeTime = ServiceLifetime.Scoped)
            where T : class, IRepository, new()
        {
            T TFactory(IServiceProvider x) => new()
            {
                CountSyntax = countSyntax
            };

            IRepository IRepositoryFactory(IServiceProvider x) => new T
            {
                CountSyntax = countSyntax
            };

            switch (lifeTime)
            {
                case ServiceLifetime.Singleton:
                    @this.AddSingleton(TFactory).AddSingleton(IRepositoryFactory);
                    break;

                case ServiceLifetime.Scoped:
                    @this.AddScoped(TFactory).AddScoped(IRepositoryFactory);
                    break;

                case ServiceLifetime.Transient:
                    @this.AddTransient(TFactory).AddTransient(IRepositoryFactory);
                    break;

                default:
                    break;
            }

            return @this;
        }
        #endregion

        #region AddAllRepository
        /// <summary>
        /// 按需注入所有程序依赖的数据库仓储 <para>注意：仓储没有初始化DbContext</para>
        /// </summary>
        /// <param name="this">依赖注入服务集合</param>
        /// <param name="configuration">服务配置</param>
        /// <param name="countSyntax">分页计数语法，默认：COUNT(*)</param>
        /// <param name="connectionSection">连接字符串配置Section，默认：ConnectionStrings</param>
        /// <param name="lifeTime">生命周期，默认：Scoped</param>
        /// <returns></returns>
        public static IServiceCollection AddAllRepository(
            this IServiceCollection @this,
            IConfiguration configuration,
            string countSyntax = "COUNT(*)",
            string connectionSection = "ConnectionStrings",
            ServiceLifetime lifeTime = ServiceLifetime.Scoped)
        {
            //数据库配置
            var configs = configuration.GetSection(connectionSection).Get<Dictionary<string, List<string>>>();

            //注入所有数据库
            if (configs != null && configs.Values != null && configs.Values.Any(x => x != null && x.Any()))
            {
                var databaseTypes = configs.Values.Where(x => x != null && x.Any()).Select(x => x[0].ToLower()).Distinct();
                foreach (var databaseType in databaseTypes)
                {
                    //SqlServer
                    if (string.Equals(databaseType, "SqlServer", StringComparison.OrdinalIgnoreCase))
                        @this.AddRepository<SqlRepository>(countSyntax, lifeTime);

                    //MySql
                    if (string.Equals(databaseType, "MySql", StringComparison.OrdinalIgnoreCase))
                        @this.AddRepository<MySqlRepository>(countSyntax, lifeTime);

                    //Oracle
                    if (string.Equals(databaseType, "Oracle", StringComparison.OrdinalIgnoreCase))
                        @this.AddRepository<OracleRepository>(countSyntax, lifeTime);

                    //Sqlite
                    if (string.Equals(databaseType, "Sqlite", StringComparison.OrdinalIgnoreCase))
                        @this.AddRepository<SqliteRepository>(countSyntax, lifeTime);

                    //PostgreSql
                    if (string.Equals(databaseType, "PostgreSql", StringComparison.OrdinalIgnoreCase))
                        @this.AddRepository<NpgsqlRepository>(countSyntax, lifeTime);
                }
            }

            return @this;
        }
        #endregion

        #region GetConnectionInformation
        /// <summary>
        /// 获取数据库连接信息
        /// </summary>
        /// <param name="configuration">服务配置</param>
        /// <param name="key">数据库标识键值</param>
        /// <param name="defaultName">默认数据库名称</param>
        /// <param name="connectionSection">连接字符串配置Section，默认：ConnectionStrings</param>
        /// <returns></returns>
        public static (
            DatabaseType databaseType,
            string connectionString)
            GetConnectionInformation(
            this IConfiguration configuration,
            string key,
            string defaultName,
            string connectionSection)
        {
            //数据库标识键值
            key = key.IsNullOrEmpty() ? defaultName : key;

            //数据库配置
            var configs = configuration.GetSection($"{connectionSection}:{key}").Get<List<string>>();

            //数据库类型
            var databaseType = (DatabaseType)Enum.Parse(typeof(DatabaseType), configs[0]);

            return (databaseType, configs[1]);
        }
        #endregion

        #region CreateRepositoryFactory
        /// <summary>
        /// 创建IRepository委托，依赖AddAllRepository注入不同类型仓储
        /// </summary>
        /// <param name="provider">服务驱动</param>
        /// <param name="configuration">服务配置</param>
        /// <param name="defaultName">默认数据库名称</param>
        /// <param name="connectionSection">连接字符串配置Section，默认：ConnectionStrings</param>
        /// <param name="dbContextOptions">DbContext配置</param>
        /// <returns></returns>
        public static Func<string, IRepository> CreateRepositoryFactory(
            this IServiceProvider provider,
            IConfiguration configuration,
            string defaultName,
            string connectionSection = "ConnectionStrings",
            Action<DatabaseType, DbContextOptionsBuilder> dbContextOptions = null)
        {
            return key =>
            {
                //获取数据库连接信息
                var (databaseType, connectionString) =
                    configuration.GetConnectionInformation(key, defaultName, connectionSection);

                IRepository repository = null;

                switch (databaseType)
                {
                    case DatabaseType.SqlServer:
                        repository = provider.GetRequiredService<SqlRepository>();
                        repository.DbContext = new DefaultDbContext(x =>
                        {
                            x.UseSqlServer(connectionString);
                            dbContextOptions?.Invoke(databaseType, x);
                        });
                        break;

                    case DatabaseType.MySql:
                        repository = provider.GetRequiredService<MySqlRepository>();
                        repository.DbContext = new DefaultDbContext(x =>
                        {
                            x.UseMySql(connectionString, MySqlServerVersion.LatestSupportedServerVersion);
                            dbContextOptions?.Invoke(databaseType, x);
                        });
                        break;

                    case DatabaseType.Oracle:
                        repository = provider.GetRequiredService<OracleRepository>();
                        repository.DbContext = new DefaultDbContext(x =>
                        {
                            x.UseOracle(connectionString);
                            dbContextOptions?.Invoke(databaseType, x);
                        });
                        break;

                    case DatabaseType.Sqlite:
                        repository = provider.GetRequiredService<SqliteRepository>();
                        repository.DbContext = new DefaultDbContext(x =>
                        {
                            x.UseSqlite(connectionString);
                            dbContextOptions?.Invoke(databaseType, x);
                        });
                        break;

                    case DatabaseType.PostgreSql:
                        repository = provider.GetRequiredService<NpgsqlRepository>();
                        repository.DbContext = new DefaultDbContext(x =>
                        {
                            x.UseNpgsql(connectionString);
                            dbContextOptions?.Invoke(databaseType, x);
                        });
                        break;

                    default:
                        throw new ArgumentException($"Invalid database type `{databaseType}`.");
                }

                return repository;
            };
        }

        /// <summary>
        /// 创建IRepository委托，依赖AddAllRepository注入不同类型仓储
        /// </summary>
        /// <param name="provider">服务驱动</param>
        /// <param name="configuration">服务配置</param>
        /// <param name="defaultName">默认数据库名称</param>
        /// <param name="dbContextFactory">DbContext自定义委托</param>
        /// <param name="connectionSection">连接字符串配置Section，默认：ConnectionStrings</param>
        /// <returns></returns>
        public static Func<string, IRepository> CreateRepositoryFactory(
            this IServiceProvider provider,
            IConfiguration configuration,
            string defaultName,
            Func<string, string, DatabaseType, IServiceProvider, DbContext> dbContextFactory,
            string connectionSection = "ConnectionStrings")
        {
            return key =>
            {
                //获取数据库连接信息
                var (databaseType, connectionString) =
                    configuration.GetConnectionInformation(key, defaultName, connectionSection);

                //获取对应数据库类型的仓储
                IRepository repository = databaseType switch
                {
                    DatabaseType.SqlServer => provider.GetRequiredService<SqlRepository>(),
                    DatabaseType.MySql => provider.GetRequiredService<MySqlRepository>(),
                    DatabaseType.Oracle => provider.GetRequiredService<OracleRepository>(),
                    DatabaseType.Sqlite => provider.GetRequiredService<SqliteRepository>(),
                    DatabaseType.PostgreSql => provider.GetRequiredService<NpgsqlRepository>(),
                    _ => throw new ArgumentException($"Invalid database type `{databaseType}`.")
                };

                repository.DbContext = dbContextFactory(key, connectionString, databaseType, provider);

                return repository;
            };
        }
        #endregion

        #region AddEFCoreRepository
        /// <summary>
        /// 注入EntityFrameworkCore仓储
        /// </summary>
        /// <param name="this">依赖注入服务集合</param>
        /// <param name="configuration">服务配置</param>
        /// <param name="defaultName">默认数据库名称</param>
        /// <param name="dbContextOptions">DbContext配置</param>
        /// <param name="countSyntax">分页计数语法，默认：COUNT(*)</param>
        /// <param name="connectionSection">连接字符串配置Section，默认：ConnectionStrings</param>
        /// <param name="lifeTime">生命周期，默认：Scoped</param>
        /// <returns></returns>
        /// <remarks>
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
        /// </remarks>
        public static IServiceCollection AddEFCoreRepository(
            this IServiceCollection @this,
            IConfiguration configuration,
            string defaultName,
            Action<DatabaseType, DbContextOptionsBuilder> dbContextOptions = null,
            string countSyntax = "COUNT(*)",
            string connectionSection = "ConnectionStrings",
            ServiceLifetime lifeTime = ServiceLifetime.Scoped)
        {
            //按需注入所有依赖的仓储
            @this.AddAllRepository(configuration, countSyntax, connectionSection, lifeTime);

            //根据生命周期类型注入服务
            switch (lifeTime)
            {
                case ServiceLifetime.Singleton:
                    @this.AddSingleton(x => x.CreateRepositoryFactory(configuration, defaultName, connectionSection, dbContextOptions));
                    break;

                case ServiceLifetime.Transient:
                    @this.AddTransient(x => x.CreateRepositoryFactory(configuration, defaultName, connectionSection, dbContextOptions));
                    break;

                case ServiceLifetime.Scoped:
                    @this.AddScoped(x => x.CreateRepositoryFactory(configuration, defaultName, connectionSection, dbContextOptions));
                    break;

                default:
                    break;
            }

            return @this;
        }

        /// <summary>
        /// 注入EntityFrameworkCore仓储，<para>注意：需要事先注入DbContext</para>
        /// </summary>
        /// <param name="this">依赖注入服务集合</param>
        /// <param name="configuration">服务配置</param>
        /// <param name="defaultName">默认数据库名称</param>
        /// <param name="dbContextFactory">DbContext自定义委托</param>
        /// <param name="countSyntax">分页计数语法，默认：COUNT(*)</param>
        /// <param name="connectionSection">连接字符串配置Section，默认：ConnectionStrings</param>
        /// <param name="lifeTime">生命周期，默认：Scoped</param>
        /// <returns></returns>
        /// <remarks>
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
        /// </remarks>
        public static IServiceCollection AddEFCoreRepository(
            this IServiceCollection @this,
            IConfiguration configuration,
            string defaultName,
            Func<string, string, DatabaseType, IServiceProvider, DbContext> dbContextFactory,
            string countSyntax = "COUNT(*)",
            string connectionSection = "ConnectionStrings",
            ServiceLifetime lifeTime = ServiceLifetime.Scoped)
        {
            //按需注入所有依赖的仓储
            @this.AddAllRepository(configuration, countSyntax, connectionSection, lifeTime);

            //根据生命周期类型注入服务
            switch (lifeTime)
            {
                case ServiceLifetime.Singleton:
                    @this.AddSingleton(x => x.CreateRepositoryFactory(configuration, defaultName, dbContextFactory, connectionSection));
                    break;

                case ServiceLifetime.Transient:
                    @this.AddTransient(x => x.CreateRepositoryFactory(configuration, defaultName, dbContextFactory, connectionSection));
                    break;

                case ServiceLifetime.Scoped:
                    @this.AddScoped(x => x.CreateRepositoryFactory(configuration, defaultName, dbContextFactory, connectionSection));
                    break;

                default:
                    break;
            }

            return @this;
        }
        #endregion
    }
}
