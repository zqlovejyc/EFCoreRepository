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

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
/****************************
* [Author] 张强
* [Date] 2018-09-16
* [Describe] Sqlserver仓储实现类
* **************************/
namespace EFCoreRepository
{
    /// <summary>
    /// Sqlserver仓储实现类
    /// </summary>
    public class SqlRepository : BaseRepository, IRepository
    {
        #region Field
        /// <summary>
        /// 私有数据库连接字符串
        /// </summary>
        private string connectionString;

        /// <summary>
        /// 私有事务对象
        /// </summary>
        private DbTransaction transaction;

        /// <summary>
        /// 私有超时时长
        /// </summary>
        private int commandTimeout = 240;
        #endregion

        #region Property
        /// <summary>
        /// 超时时长，默认240s
        /// </summary>
        public virtual int CommandTimeout
        {
            get
            {
                return this.commandTimeout;
            }
            set
            {
                this.commandTimeout = value;
                this.DbContext.Database.SetCommandTimeout(this.commandTimeout);
            }
        }

        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public virtual string ConnectionString
        {
            get
            {
                return this.connectionString ?? this.DbContext.Database.GetDbConnection().ConnectionString;
            }
            set
            {
                this.connectionString = value;
                this.DbContext.Database.GetDbConnection().ConnectionString = this.connectionString;
            }
        }

        /// <summary>
        /// 分页计数语法，默认COUNT(1)
        /// </summary>
        public virtual string CountSyntax { get; set; } = "COUNT(1)";

        /// <summary>
        /// 事务对象
        /// </summary>
        public virtual DbTransaction Transaction
        {
            get
            {
                return this.transaction ?? this.DbContext.Database.CurrentTransaction?.GetDbTransaction();
            }
            set
            {
                this.transaction = value;
                this.DbContext.Database.UseTransaction(this.transaction);
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">DbContext实例</param>
        public SqlRepository(DbContext context)
        {
            this.DbContext = context;
            this.DbContext.Database.SetCommandTimeout(this.commandTimeout);
        }
        #endregion

        #region Transaction
        #region Sync
        /// <summary>
        /// 开启事务
        /// </summary>
        /// <returns>IRepository</returns>
        public IRepository BeginTrans()
        {
            this.DbContext.Database.BeginTransaction();
            return this;
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public void Commit()
        {
            this.DbContext.Database.CommitTransaction();
            this.DbContext.Database.CurrentTransaction?.Dispose();
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public void Rollback()
        {
            this.DbContext.Database.RollbackTransaction();
            this.DbContext.Database.CurrentTransaction?.Dispose();
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Close()
        {
            this.DbContext.Database.CloseConnection();
            this.DbContext.Dispose();
        }
        #endregion

        #region Async
        /// <summary>
        /// 开启事务
        /// </summary>
        /// <returns>IRepository</returns>
        public async Task<IRepository> BeginTransAsync()
        {
            await this.DbContext.Database.BeginTransactionAsync();
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
            else
            {
                orderField = "ORDER BY (SELECT 0)";
            }

            var sqlQuery = $"SELECT {CountSyntax} AS [TOTAL] FROM ({sql}) AS T;";

            var serverVersion = int.Parse(this.DbContext.Database.GetDbConnection().ServerVersion.Split('.')[0]);

            if (serverVersion > 10)
                sqlQuery += $"SELECT * FROM ({sql}) AS T {orderField} OFFSET {((pageIndex - 1) * pageSize)} ROWS FETCH NEXT {pageSize} ROWS ONLY;";
            else
                sqlQuery += $"SELECT * FROM (SELECT ROW_NUMBER() OVER ({orderField}) AS [ROWNUMBER], * FROM ({sql}) AS T) AS N WHERE [ROWNUMBER] BETWEEN {((pageIndex - 1) * pageSize + 1)} AND {(pageIndex * pageSize)};";

            var type = typeof(T);
            if (!type.Name.Contains("Dictionary`2") && type.IsClass && type.Name != "Object" && type.Name != "String")
            {
                var query = this.DbContext.SqlQueryMultiple<dynamic>(sqlQuery, parameter);
                return (query.LastOrDefault().Select(o => (o as IDictionary<string, object>).ToEntity<T>()).ToList(), Convert.ToInt64(query.FirstOrDefault().FirstOrDefault().TOTAL ?? 0));
            }
            else
            {
                var query = this.DbContext.SqlQueryMultiple<T>(sqlQuery, parameter);
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
            else
            {
                orderField = "ORDER BY (SELECT 0)";
            }

            var sqlQuery = $"{sql} SELECT {CountSyntax} AS [TOTAL] FROM T;";

            var serverVersion = int.Parse(this.DbContext.Database.GetDbConnection().ServerVersion.Split('.')[0]);

            if (serverVersion > 10)
                sqlQuery += $"{sql} SELECT * FROM T {orderField} OFFSET {((pageIndex - 1) * pageSize)} ROWS FETCH NEXT {pageSize} ROWS ONLY;";
            else
                sqlQuery += $"{sql},R AS (SELECT ROW_NUMBER() OVER ({orderField}) AS [ROWNUMBER], * FROM T) SELECT * FROM R WHERE [ROWNUMBER] BETWEEN {((pageIndex - 1) * pageSize + 1)} AND {(pageIndex * pageSize)};";

            var type = typeof(T);
            if (!type.Name.Contains("Dictionary`2") && type.IsClass && type.Name != "Object" && type.Name != "String")
            {
                var query = this.DbContext.SqlQueryMultiple<dynamic>(sqlQuery, parameter);
                return (query.LastOrDefault().Select(o => (o as IDictionary<string, object>).ToEntity<T>()).ToList(), Convert.ToInt64(query.FirstOrDefault().FirstOrDefault().TOTAL ?? 0));
            }
            else
            {
                var query = this.DbContext.SqlQueryMultiple<T>(sqlQuery, parameter);
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
            else
            {
                orderField = "ORDER BY (SELECT 0)";
            }

            var sqlQuery = $"SELECT {CountSyntax} AS [TOTAL] FROM ({sql}) AS T;";

            var serverVersion = int.Parse(this.DbContext.Database.GetDbConnection().ServerVersion.Split('.')[0]);

            if (serverVersion > 10)
                sqlQuery += $"SELECT * FROM ({sql}) AS T {orderField} OFFSET {((pageIndex - 1) * pageSize)} ROWS FETCH NEXT {pageSize} ROWS ONLY;";
            else
                sqlQuery += $"SELECT * FROM (SELECT ROW_NUMBER() OVER ({orderField}) AS [ROWNUMBER], * FROM ({sql}) AS T) AS N WHERE [ROWNUMBER] BETWEEN {((pageIndex - 1) * pageSize + 1)} AND {(pageIndex * pageSize)};";

            var type = typeof(T);
            if (!type.Name.Contains("Dictionary`2") && type.IsClass && type.Name != "Object" && type.Name != "String")
            {
                var query = await this.DbContext.SqlQueryMultipleAsync<dynamic>(sqlQuery, parameter);
                return (query.LastOrDefault().Select(o => (o as IDictionary<string, object>).ToEntity<T>()).ToList(), Convert.ToInt64(query.FirstOrDefault().FirstOrDefault().TOTAL ?? 0));
            }
            else
            {
                var query = await this.DbContext.SqlQueryMultipleAsync<T>(sqlQuery, parameter);
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
            else
            {
                orderField = "ORDER BY (SELECT 0)";
            }

            var sqlQuery = $"{sql} SELECT {CountSyntax} AS [TOTAL] FROM T;";

            var serverVersion = int.Parse(this.DbContext.Database.GetDbConnection().ServerVersion.Split('.')[0]);

            if (serverVersion > 10)
                sqlQuery += $"{sql} SELECT * FROM T {orderField} OFFSET {((pageIndex - 1) * pageSize)} ROWS FETCH NEXT {pageSize} ROWS ONLY;";
            else
                sqlQuery += $"{sql},R AS (SELECT ROW_NUMBER() OVER ({orderField}) AS [ROWNUMBER], * FROM T) SELECT * FROM R WHERE [ROWNUMBER] BETWEEN {((pageIndex - 1) * pageSize + 1)} AND {(pageIndex * pageSize)};";

            var type = typeof(T);
            if (!type.Name.Contains("Dictionary`2") && type.IsClass && type.Name != "Object" && type.Name != "String")
            {
                var query = await this.DbContext.SqlQueryMultipleAsync<dynamic>(sqlQuery, parameter);
                return (query.LastOrDefault().Select(o => (o as IDictionary<string, object>).ToEntity<T>()).ToList(), Convert.ToInt64(query.FirstOrDefault().FirstOrDefault().TOTAL ?? 0));
            }
            else
            {
                var query = await this.DbContext.SqlQueryMultipleAsync<T>(sqlQuery, parameter);
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
            else
            {
                orderField = "ORDER BY (SELECT 0)";
            }

            var sqlQuery = $"SELECT {CountSyntax} AS [TOTAL] FROM ({sql}) AS T;";

            var serverVersion = int.Parse(this.DbContext.Database.GetDbConnection().ServerVersion.Split('.')[0]);

            if (serverVersion > 10)
                sqlQuery += $"SELECT * FROM ({sql}) AS T {orderField} OFFSET {((pageIndex - 1) * pageSize)} ROWS FETCH NEXT {pageSize} ROWS ONLY;";
            else
                sqlQuery += $"SELECT * FROM (SELECT ROW_NUMBER() OVER ({orderField}) AS [ROWNUMBER], * FROM ({sql}) AS T) AS N WHERE [ROWNUMBER] BETWEEN {((pageIndex - 1) * pageSize + 1)} AND {(pageIndex * pageSize)};";

            var ds = this.DbContext.SqlDataSet(sqlQuery, parameter);
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
            else
            {
                orderField = "ORDER BY (SELECT 0)";
            }

            var sqlQuery = $"{sql} SELECT {CountSyntax} AS [TOTAL] FROM T;";

            var serverVersion = int.Parse(this.DbContext.Database.GetDbConnection().ServerVersion.Split('.')[0]);

            if (serverVersion > 10)
                sqlQuery += $"{sql} SELECT * FROM T {orderField} OFFSET {((pageIndex - 1) * pageSize)} ROWS FETCH NEXT {pageSize} ROWS ONLY;";
            else
                sqlQuery += $"{sql},R AS (SELECT ROW_NUMBER() OVER ({orderField}) AS [ROWNUMBER], * FROM T) SELECT * FROM R WHERE [ROWNUMBER] BETWEEN {((pageIndex - 1) * pageSize + 1)} AND {(pageIndex * pageSize)};";

            var ds = this.DbContext.SqlDataSet(sqlQuery, parameter);
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
            else
            {
                orderField = "ORDER BY (SELECT 0)";
            }

            var sqlQuery = $"SELECT {CountSyntax} AS [TOTAL] FROM ({sql}) AS T;";

            var serverVersion = int.Parse(this.DbContext.Database.GetDbConnection().ServerVersion.Split('.')[0]);

            if (serverVersion > 10)
                sqlQuery += $"SELECT * FROM ({sql}) AS T {orderField} OFFSET {((pageIndex - 1) * pageSize)} ROWS FETCH NEXT {pageSize} ROWS ONLY;";
            else
                sqlQuery += $"SELECT * FROM (SELECT ROW_NUMBER() OVER ({orderField}) AS [ROWNUMBER], * FROM ({sql}) AS T) AS N WHERE [ROWNUMBER] BETWEEN {((pageIndex - 1) * pageSize + 1)} AND {(pageIndex * pageSize)};";

            var ds = await this.DbContext.SqlDataSetAsync(sqlQuery, parameter);
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
            else
            {
                orderField = "ORDER BY (SELECT 0)";
            }

            var sqlQuery = $"{sql} SELECT {CountSyntax} AS [TOTAL] FROM T;";

            var serverVersion = int.Parse(this.DbContext.Database.GetDbConnection().ServerVersion.Split('.')[0]);

            if (serverVersion > 10)
                sqlQuery += $"{sql} SELECT * FROM T {orderField} OFFSET {((pageIndex - 1) * pageSize)} ROWS FETCH NEXT {pageSize} ROWS ONLY;";
            else
                sqlQuery += $"{sql},R AS (SELECT ROW_NUMBER() OVER ({orderField}) AS [ROWNUMBER], * FROM T) SELECT * FROM R WHERE [ROWNUMBER] BETWEEN {((pageIndex - 1) * pageSize + 1)} AND {(pageIndex * pageSize)};";

            var ds = await this.DbContext.SqlDataSetAsync(sqlQuery, parameter);
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
            this.Close();
        }
        #endregion
    }
}