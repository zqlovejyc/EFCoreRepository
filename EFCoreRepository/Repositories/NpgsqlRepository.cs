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

using EFCoreRepository.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
/****************************
* [Author] 张强
* [Date] 2018-09-27
* [Describe] PostgreSQL仓储实现类
* **************************/
namespace EFCoreRepository.Repositories
{
    /// <summary>
    /// PostgreSQL仓储实现类
    /// </summary>
    public class NpgsqlRepository : BaseRepository,IRepository
    {
        #region Constructor
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">DbContext实例</param>
        public NpgsqlRepository(DbContext context)
        {
            DbContext = context;
            DbContext.Database.SetCommandTimeout(CommandTimeout);
        }
        #endregion

        #region Transaction
        #region Sync
        /// <summary>
        /// 开启事务
        /// </summary>
        /// <returns>IRepository</returns>
        public override IRepository BeginTrans()
        {
            if (DbContext.Database.CurrentTransaction == null)
                DbContext.Database.BeginTransaction();

            return this;
        }
        #endregion

        #region Async
        /// <summary>
        /// 开启事务
        /// </summary>
        /// <returns>IRepository</returns>
        public override async Task<IRepository> BeginTransAsync()
        {
            if (DbContext.Database.CurrentTransaction == null)
                await DbContext.Database.BeginTransactionAsync();

            return this;
        }
        #endregion
        #endregion

        #region FindList
        #region Sync
        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="isAscending">是否升序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>        
        /// <returns>返回集合和总记录数</returns>
        public override (List<T> list, long total) FindList<T>(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            if (!orderField.IsNullOrEmpty())
            {
                if (orderField.Contains(@"(/\*(?:|)*?\*/)|(\b(ASC|DESC)\b)", RegexOptions.IgnoreCase))
                    orderField = $"ORDER BY {orderField}";
                else
                    orderField = $"ORDER BY {orderField} {(isAscending ? "ASC" : "DESC")}";
            }

            var sqlQuery = $"SELECT {CountSyntax} AS \"TOTAL\" FROM ({sql}) AS T;";

            sqlQuery += $"SELECT * FROM ({sql}) AS T {orderField} LIMIT {pageSize} OFFSET {(pageSize * (pageIndex - 1))};";

            var type = typeof(T);
            if (type.IsClass && !type.IsDictionaryType() && !type.IsDynamicOrObjectType() && !type.IsStringType())
            {
                var query = DbContext.SqlQueryMultiple<dynamic>(sqlQuery, parameter);
                return (query.LastOrDefault().Select(o => (o as IDictionary<string, object>).ToEntity<T>()).ToList(), Convert.ToInt64(query.FirstOrDefault().FirstOrDefault().TOTAL ?? 0));
            }
            else
            {
                var query = DbContext.SqlQueryMultiple<T>(sqlQuery, parameter);
                return (query.LastOrDefault(), Convert.ToInt64((query.FirstOrDefault().FirstOrDefault() as IDictionary<string, object>)?["TOTAL"] ?? 0));
            }
        }

        /// <summary>
        /// with语法分页查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="isAscending">是否升序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>        
        /// <returns>返回集合和总记录数</returns>
        public override (List<T> list, long total) FindListByWith<T>(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            if (!orderField.IsNullOrEmpty())
            {
                if (orderField.Contains(@"(/\*(?:|)*?\*/)|(\b(ASC|DESC)\b)", RegexOptions.IgnoreCase))
                    orderField = $"ORDER BY {orderField}";
                else
                    orderField = $"ORDER BY {orderField} {(isAscending ? "ASC" : "DESC")}";
            }

            var sqlQuery = $"{sql} SELECT {CountSyntax} AS \"TOTAL\" FROM T;";

            sqlQuery += $"{sql} SELECT * FROM T {orderField} LIMIT {pageSize} OFFSET {(pageSize * (pageIndex - 1))};";

            var type = typeof(T);
            if (type.IsClass && !type.IsDictionaryType() && !type.IsDynamicOrObjectType() && !type.IsStringType())
            {
                var query = DbContext.SqlQueryMultiple<dynamic>(sqlQuery, parameter);
                return (query.LastOrDefault().Select(o => (o as IDictionary<string, object>).ToEntity<T>()).ToList(), Convert.ToInt64(query.FirstOrDefault().FirstOrDefault().TOTAL ?? 0));
            }
            else
            {
                var query = DbContext.SqlQueryMultiple<T>(sqlQuery, parameter);
                return (query.LastOrDefault(), Convert.ToInt64((query.FirstOrDefault().FirstOrDefault() as IDictionary<string, object>)?["TOTAL"] ?? 0));
            }
        }
        #endregion

        #region Async
        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="isAscending">是否升序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>        
        /// <returns>返回集合和总记录数</returns>
        public override async Task<(List<T> list, long total)> FindListAsync<T>(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            if (!orderField.IsNullOrEmpty())
            {
                if (orderField.Contains(@"(/\*(?:|)*?\*/)|(\b(ASC|DESC)\b)", RegexOptions.IgnoreCase))
                    orderField = $"ORDER BY {orderField}";
                else
                    orderField = $"ORDER BY {orderField} {(isAscending ? "ASC" : "DESC")}";
            }

            var sqlQuery = $"SELECT {CountSyntax} AS \"TOTAL\" FROM ({sql}) AS T;";

            sqlQuery += $"SELECT * FROM ({sql}) AS T {orderField} LIMIT {pageSize} OFFSET {(pageSize * (pageIndex - 1))};";

            var type = typeof(T);
            if (type.IsClass && !type.IsDictionaryType() && !type.IsDynamicOrObjectType() && !type.IsStringType())
            {
                var query = await DbContext.SqlQueryMultipleAsync<dynamic>(sqlQuery, parameter);
                return (query.LastOrDefault().Select(o => (o as IDictionary<string, object>).ToEntity<T>()).ToList(), Convert.ToInt64(query.FirstOrDefault().FirstOrDefault().TOTAL ?? 0));
            }
            else
            {
                var query = await DbContext.SqlQueryMultipleAsync<T>(sqlQuery, parameter);
                return (query.LastOrDefault(), Convert.ToInt64((query.FirstOrDefault().FirstOrDefault() as IDictionary<string, object>)?["TOTAL"] ?? 0));
            }
        }

        /// <summary>
        /// with语法分页查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="isAscending">是否升序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>        
        /// <returns>返回集合和总记录数</returns>
        public override async Task<(List<T> list, long total)> FindListByWithAsync<T>(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            if (!orderField.IsNullOrEmpty())
            {
                if (orderField.Contains(@"(/\*(?:|)*?\*/)|(\b(ASC|DESC)\b)", RegexOptions.IgnoreCase))
                    orderField = $"ORDER BY {orderField}";
                else
                    orderField = $"ORDER BY {orderField} {(isAscending ? "ASC" : "DESC")}";
            }

            var sqlQuery = $"{sql} SELECT {CountSyntax} AS \"TOTAL\" FROM T;";

            sqlQuery += $"{sql} SELECT * FROM T {orderField} LIMIT {pageSize} OFFSET {(pageSize * (pageIndex - 1))};";

            var type = typeof(T);
            if (type.IsClass && !type.IsDictionaryType() && !type.IsDynamicOrObjectType() && !type.IsStringType())
            {
                var query = await DbContext.SqlQueryMultipleAsync<dynamic>(sqlQuery, parameter);
                return (query.LastOrDefault().Select(o => (o as IDictionary<string, object>).ToEntity<T>()).ToList(), Convert.ToInt64(query.FirstOrDefault().FirstOrDefault().TOTAL ?? 0));
            }
            else
            {
                var query = await DbContext.SqlQueryMultipleAsync<T>(sqlQuery, parameter);
                return (query.LastOrDefault(), Convert.ToInt64((query.FirstOrDefault().FirstOrDefault() as IDictionary<string, object>)?["TOTAL"] ?? 0));
            }
        }
        #endregion
        #endregion

        #region FindTable
        #region Sync
        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="isAscending">是否升序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>        
        /// <returns>返回DataTable和总记录数</returns>
        public override (DataTable table, long total) FindTable(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            if (!orderField.IsNullOrEmpty())
            {
                if (orderField.Contains(@"(/\*(?:|)*?\*/)|(\b(ASC|DESC)\b)", RegexOptions.IgnoreCase))
                    orderField = $"ORDER BY {orderField}";
                else
                    orderField = $"ORDER BY {orderField} {(isAscending ? "ASC" : "DESC")}";
            }

            var sqlQuery = $"SELECT {CountSyntax} AS \"TOTAL\" FROM ({sql}) AS T;";

            sqlQuery += $"SELECT * FROM ({sql}) AS T {orderField} LIMIT {pageSize} OFFSET {(pageSize * (pageIndex - 1))};";

            var ds = DbContext.SqlDataSet(sqlQuery, parameter);
            return (ds.Tables[1], Convert.ToInt64(ds.Tables[0].Rows[0]["TOTAL"]));
        }

        /// <summary>
        /// with语法分页查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="isAscending">是否升序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>        
        /// <returns>返回DataTable和总记录数</returns>
        public override (DataTable table, long total) FindTableByWith(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            if (!orderField.IsNullOrEmpty())
            {
                if (orderField.Contains(@"(/\*(?:|)*?\*/)|(\b(ASC|DESC)\b)", RegexOptions.IgnoreCase))
                    orderField = $"ORDER BY {orderField}";
                else
                    orderField = $"ORDER BY {orderField} {(isAscending ? "ASC" : "DESC")}";
            }

            var sqlQuery = $"{sql} SELECT {CountSyntax} AS \"TOTAL\" FROM T;";

            sqlQuery += $"{sql} SELECT * FROM T {orderField} LIMIT {pageSize} OFFSET {(pageSize * (pageIndex - 1))};";

            var ds = DbContext.SqlDataSet(sqlQuery, parameter);
            return (ds.Tables[1], Convert.ToInt64(ds.Tables[0].Rows[0]["TOTAL"]));
        }
        #endregion

        #region Async
        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="isAscending">是否升序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>
        /// <returns>返回DataTable和总记录数</returns>
        public override async Task<(DataTable table, long total)> FindTableAsync(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            if (!orderField.IsNullOrEmpty())
            {
                if (orderField.Contains(@"(/\*(?:|)*?\*/)|(\b(ASC|DESC)\b)", RegexOptions.IgnoreCase))
                    orderField = $"ORDER BY {orderField}";
                else
                    orderField = $"ORDER BY {orderField} {(isAscending ? "ASC" : "DESC")}";
            }

            var sqlQuery = $"SELECT {CountSyntax} AS \"TOTAL\" FROM ({sql}) AS T;";

            sqlQuery += $"SELECT * FROM ({sql}) AS T {orderField} LIMIT {pageSize} OFFSET {(pageSize * (pageIndex - 1))};";

            var ds = await DbContext.SqlDataSetAsync(sqlQuery, parameter);
            return (ds.Tables[1], Convert.ToInt64(ds.Tables[0].Rows[0]["TOTAL"]));
        }

        /// <summary>
        /// with语法分页查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="isAscending">是否升序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>        
        /// <returns>返回DataTable和总记录数</returns>
        public override async Task<(DataTable table, long total)> FindTableByWithAsync(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            if (!orderField.IsNullOrEmpty())
            {
                if (orderField.Contains(@"(/\*(?:|)*?\*/)|(\b(ASC|DESC)\b)", RegexOptions.IgnoreCase))
                    orderField = $"ORDER BY {orderField}";
                else
                    orderField = $"ORDER BY {orderField} {(isAscending ? "ASC" : "DESC")}";
            }

            var sqlQuery = $"{sql} SELECT {CountSyntax} AS \"TOTAL\" FROM T;";

            sqlQuery += $"{sql} SELECT * FROM T {orderField} LIMIT {pageSize} OFFSET {(pageSize * (pageIndex - 1))};";

            var ds = await DbContext.SqlDataSetAsync(sqlQuery, parameter);
            return (ds.Tables[1], Convert.ToInt64(ds.Tables[0].Rows[0]["TOTAL"]));
        }
        #endregion
        #endregion

        #region Dispose
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Close();
        }
        #endregion
    }
}
