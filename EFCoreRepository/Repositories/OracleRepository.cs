#region License
/***
 * Copyright © 2018-2025, 张强 (943620963@qq.com).
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

using EFCoreRepository.Enums;
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
* [Describe] Oracle仓储实现类
* **************************/
namespace EFCoreRepository.Repositories
{
    /// <summary>
    /// Oracle仓储实现类
    /// </summary>
    public class OracleRepository : BaseRepository
    {
        #region Property
        /// <summary>
        /// 数据库类型
        /// </summary>
        public override DatabaseType DatabaseType => DatabaseType.Oracle;
        #endregion

        #region Constructor
        /// <summary>
        /// 构造函数
        /// </summary>
        public OracleRepository() { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">DbContext实例</param>
        public OracleRepository(DbContext context) : base(context) { }
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
            var sqlPage = sql.Split(';');
            var sqlCount = sqlPage[0];
            var sqlQuery = sqlPage[1];

            var total = DbContext.SqlQuery<long>(sqlCount, parameter).FirstOrDefault();
            var list = DbContext.SqlQuery<T>(sqlQuery, parameter);
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
            var sqlPage = sql.Split(';');
            var sqlCount = sqlPage[0];
            var sqlQuery = sqlPage[1];

            var total = DbContext.SqlQuery<long>(sqlCount, parameter).FirstOrDefault();
            var list = DbContext.SqlQuery<T>(sqlQuery, parameter);
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
            var sqlPage = sql.Split(';');
            var sqlCount = sqlPage[0];
            var sqlQuery = sqlPage[1];

            var total = (await DbContext.SqlQueryAsync<long>(sqlCount, parameter)).FirstOrDefault();
            var list = await DbContext.SqlQueryAsync<T>(sqlQuery, parameter);
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
            var sqlPage = sql.Split(';');
            var sqlCount = sqlPage[0];
            var sqlQuery = sqlPage[1];

            var total = (await DbContext.SqlQueryAsync<long>(sqlCount, parameter)).FirstOrDefault();
            var list = await DbContext.SqlQueryAsync<T>(sqlQuery, parameter);
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
            var sqlPage = sql.Split(';');
            var sqlCount = sqlPage[0];
            var sqlQuery = sqlPage[1];

            var total = DbContext.SqlQuery<long>(sqlCount, parameter).FirstOrDefault();
            var table = DbContext.SqlDataTable(sqlQuery, parameter);
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
            var sqlPage = sql.Split(';');
            var sqlCount = sqlPage[0];
            var sqlQuery = sqlPage[1];

            var total = DbContext.SqlQuery<long>(sqlCount, parameter).FirstOrDefault();
            var table = DbContext.SqlDataTable(sqlQuery, parameter);
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
            var sqlPage = sql.Split(';');
            var sqlCount = sqlPage[0];
            var sqlQuery = sqlPage[1];

            var total = (await DbContext.SqlQueryAsync<long>(sqlCount, parameter)).FirstOrDefault();
            var table = await DbContext.SqlDataTableAsync(sqlQuery, parameter);
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
            var sqlPage = sql.Split(';');
            var sqlCount = sqlPage[0];
            var sqlQuery = sqlPage[1];

            var total = (await DbContext.SqlQueryAsync<long>(sqlCount, parameter)).FirstOrDefault();
            var table = await DbContext.SqlDataTableAsync(sqlQuery, parameter);
            return (table, total);
        }
        #endregion
        #endregion

        #region Page
        /// <summary>
        /// 获取分页语句
        /// </summary>
        /// <param name="isWithSyntax">是否with语法</param>
        /// <param name="sql">原始sql语句</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="isAscending">是否升序排序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>
        /// <returns></returns>
        public override string GetPageSql(bool isWithSyntax, string sql, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            //排序字段
            if (!orderField.IsNullOrEmpty())
            {
                if (orderField.Contains(@"(/\*(?:|)*?\*/)|(\b(ASC|DESC)\b)", RegexOptions.IgnoreCase))
                    orderField = $"ORDER BY {orderField}";
                else
                    orderField = $"ORDER BY {orderField} {(isAscending ? "ASC" : "DESC")}";
            }

            string sqlQuery;
            var next = pageSize;
            var offset = pageSize * (pageIndex - 1);
            var rowStart = pageSize * (pageIndex - 1) + 1;
            var rowEnd = pageSize * pageIndex;

            var connection = DbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                connection.Open();

            var serverVersion = int.Parse(connection.ServerVersion.Split('.')[0]);

            //判断是否with语法
            if (isWithSyntax)
            {
                sqlQuery = $"{sql} SELECT {CountSyntax} AS \"TOTAL\" FROM T;";

                if (serverVersion > 11)
                    sqlQuery += $"{sql.Remove(sql.LastIndexOf(")"), 1)} {orderField}) SELECT * FROM T OFFSET {offset} ROWS FETCH NEXT {next} ROWS ONLY";
                else
                    sqlQuery += $"{sql.Remove(sql.LastIndexOf(")"), 1)} {orderField}),R AS (SELECT ROWNUM AS ROWNUMBER,T.* FROM T WHERE ROWNUM <= {rowEnd}) SELECT * FROM R WHERE ROWNUMBER>={rowStart}";
            }
            else
            {
                sqlQuery = $"SELECT {CountSyntax} AS \"TOTAL\" FROM ({sql}) T;";

                if (serverVersion > 11)
                    sqlQuery += $"{sql} {orderField} OFFSET {offset} ROWS FETCH NEXT {next} ROWS ONLY";
                else
                    sqlQuery += $"SELECT * FROM (SELECT X.*,ROWNUM AS \"ROWNUMBER\" FROM ({sql} {orderField}) X WHERE ROWNUM <= {rowEnd}) T WHERE \"ROWNUMBER\" >= {rowStart}";
            }

            return sqlQuery;
        }
        #endregion
    }
}
