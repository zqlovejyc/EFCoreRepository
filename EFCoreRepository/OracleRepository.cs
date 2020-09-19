﻿#region License
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
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
/****************************
* [Author] 张强
* [Date] 2018-09-27
* [Describe] Oracle仓储实现类
* **************************/
namespace EFCoreRepository
{
    /// <summary>
    /// Oracle仓储实现类
    /// </summary>
    public class OracleRepository : BaseRepository, IRepository
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
        public int CommandTimeout
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
        public string ConnectionString
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
        public string CountSyntax { get; set; } = "COUNT(1)";

        /// <summary>
        /// 事务对象
        /// </summary>
        public DbTransaction Transaction
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
        public OracleRepository(DbContext context)
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

            var sqlCount = $"SELECT {CountSyntax} AS \"TOTAL\" FROM ({sql}) T";

            var sqlQuery = $"SELECT * FROM (SELECT X.*,ROWNUM AS \"ROWNUMBER\" FROM ({sql} {orderField}) X WHERE ROWNUM <= {pageSize * pageIndex}) T WHERE \"ROWNUMBER\" >= {pageSize * (pageIndex - 1) + 1}";

            var total = this.DbContext.SqlQuery<long>(sqlCount, parameter).FirstOrDefault();
            var list = this.DbContext.SqlQuery<T>(sqlQuery, parameter);
            return (list?.ToList(), total);
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

            var sqlCount = $"{sql} SELECT {CountSyntax} AS \"TOTAL\" FROM T;";

            var sqlQuery = $"{sql.Remove(sql.LastIndexOf(")"), 1)} {orderField}),R AS (SELECT ROWNUM AS ROWNUMBER,T.* FROM T WHERE ROWNUM <= {pageSize * pageIndex}) SELECT * FROM R WHERE ROWNUMBER>={pageSize * (pageIndex - 1) + 1}";

            var total = this.DbContext.SqlQuery<long>(sqlCount, parameter).FirstOrDefault();
            var list = this.DbContext.SqlQuery<T>(sqlQuery, parameter);
            return (list?.ToList(), total);
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

            var sqlCount = $"SELECT {CountSyntax} AS \"TOTAL\" FROM ({sql}) T";

            var sqlQuery = $"SELECT * FROM (SELECT X.*,ROWNUM AS \"ROWNUMBER\" FROM ({sql} {orderField}) X WHERE ROWNUM <= {pageSize * pageIndex}) T WHERE \"ROWNUMBER\" >= {pageSize * (pageIndex - 1) + 1}";

            var total = (await this.DbContext.SqlQueryAsync<long>(sqlCount, parameter)).FirstOrDefault();
            var list = await this.DbContext.SqlQueryAsync<T>(sqlQuery, parameter);
            return (list?.ToList(), total);
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

            var sqlCount = $"{sql} SELECT {CountSyntax} AS \"TOTAL\" FROM T;";

            var sqlQuery = $"{sql.Remove(sql.LastIndexOf(")"), 1)} {orderField}),R AS (SELECT ROWNUM AS ROWNUMBER,T.* FROM T WHERE ROWNUM <= {pageSize * pageIndex}) SELECT * FROM R WHERE ROWNUMBER>={pageSize * (pageIndex - 1) + 1}";

            var total = (await this.DbContext.SqlQueryAsync<long>(sqlCount, parameter)).FirstOrDefault();
            var list = await this.DbContext.SqlQueryAsync<T>(sqlQuery, parameter);
            return (list?.ToList(), total);
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

            var sqlCount = $"SELECT {CountSyntax} AS \"TOTAL\" FROM ({sql}) T";

            var sqlQuery = $"SELECT * FROM (SELECT X.*,ROWNUM AS \"ROWNUMBER\" FROM ({sql} {orderField}) X WHERE ROWNUM <= {pageSize * pageIndex}) T WHERE \"ROWNUMBER\" >= {pageSize * (pageIndex - 1) + 1}";

            var total = this.DbContext.SqlQuery<long>(sqlCount, parameter).FirstOrDefault();
            var table = this.DbContext.SqlDataTable(sqlQuery, parameter);
            return (table, total);
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

            var sqlCount = $"{sql} SELECT {CountSyntax} AS \"TOTAL\" FROM T;";

            var sqlQuery = $"{sql.Remove(sql.LastIndexOf(")"), 1)} {orderField}),R AS (SELECT ROWNUM AS ROWNUMBER,T.* FROM T WHERE ROWNUM <= {pageSize * pageIndex}) SELECT * FROM R WHERE ROWNUMBER>={pageSize * (pageIndex - 1) + 1}";

            var total = this.DbContext.SqlQuery<long>(sqlCount, parameter).FirstOrDefault();
            var table = this.DbContext.SqlDataTable(sqlQuery, parameter);
            return (table, total);
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

            var sqlCount = $"SELECT {CountSyntax} AS \"TOTAL\" FROM ({sql}) T";

            var sqlQuery = $"SELECT * FROM (SELECT X.*,ROWNUM AS \"ROWNUMBER\" FROM ({sql} {orderField}) X WHERE ROWNUM <= {pageSize * pageIndex}) T WHERE \"ROWNUMBER\" >= {pageSize * (pageIndex - 1) + 1}";

            var total = (await this.DbContext.SqlQueryAsync<long>(sqlCount, parameter)).FirstOrDefault();
            var table = await this.DbContext.SqlDataTableAsync(sqlQuery, parameter);
            return (table, total);
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

            var sqlCount = $"{sql} SELECT {CountSyntax} AS \"TOTAL\" FROM T;";

            var sqlQuery = $"{sql.Remove(sql.LastIndexOf(")"), 1)} {orderField}),R AS (SELECT ROWNUM AS ROWNUMBER,T.* FROM T WHERE ROWNUM <= {pageSize * pageIndex}) SELECT * FROM R WHERE ROWNUMBER>={pageSize * (pageIndex - 1) + 1}";

            var total = (await this.DbContext.SqlQueryAsync<long>(sqlCount, parameter)).FirstOrDefault();
            var table = await this.DbContext.SqlDataTableAsync(sqlQuery, parameter);
            return (table, total);
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