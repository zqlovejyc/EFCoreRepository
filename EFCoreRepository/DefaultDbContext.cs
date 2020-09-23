#region License
/***
 * Copyright © 2018-2020, 张强 (943620963@qq.com).
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

using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Reflection;
/****************************
* [Author] 张强
* [Date] 2020-09-23
* [Describe] 默认DbContext
* **************************/
namespace EFCoreRepository
{
    /// <summary>
    /// 默认DbContext
    /// </summary>
    public class DefaultDbContext : DbContext
    {
        /// <summary>
        /// DbContext配置
        /// </summary>
        private readonly Action<DbContextOptionsBuilder> _options;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="options"></param>
        public DefaultDbContext(Action<DbContextOptionsBuilder> options)
        {
            _options = options;
        }

        /// <summary>
        /// OnConfiguring
        /// </summary>
        /// <param name="optionsBuilder"></param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _options?.Invoke(optionsBuilder);
        }

        /// <summary>
        /// OnModelCreating
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var entityTypes = AssemblyHelper
                                .GetTypesFromAssembly()
                                .Where(type =>
                                    !type.Namespace.IsNullOrWhiteSpace() &&
                                    type.GetTypeInfo().IsClass &&
                                    type.GetTypeInfo().BaseType != null &&
                                    type != typeof(BaseEntity) &&
                                    typeof(BaseEntity).IsAssignableFrom(type));

            if (entityTypes?.Count() > 0)
            {
                foreach (var entityType in entityTypes)
                {
                    if (modelBuilder.Model.FindEntityType(entityType) != null)
                        continue;
                    modelBuilder.Model.AddEntityType(entityType);
                }
            }
        }
    }
}
