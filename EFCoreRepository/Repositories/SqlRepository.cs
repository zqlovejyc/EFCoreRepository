﻿#region License
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
using System.Linq;
using System.Text.RegularExpressions;
/****************************
* [Author] 张强
* [Date] 2018-09-16
* [Describe] Sqlserver仓储实现类
* **************************/
namespace EFCoreRepository.Repositories
{
    /// <summary>
    /// Sqlserver仓储实现类
    /// </summary>
    public class SqlRepository : BaseRepository
    {
        #region Property
        /// <summary>
        /// 数据库类型
        /// </summary>
        public override DatabaseType DatabaseType => DatabaseType.SqlServer;
        #endregion

        #region Constructor
        /// <summary>
        /// 构造函数
        /// </summary>
        public SqlRepository() { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">DbContext实例</param>
        public SqlRepository(DbContext context) : base(context) { }
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
            string order;
            if (!orderField.IsNullOrEmpty())
            {
                if (orderField.Contains(@"(/\*(?:|)*?\*/)|(\b(ASC|DESC)\b)", RegexOptions.IgnoreCase))
                    order = $"ORDER BY {orderField}";
                else
                    order = $"ORDER BY {orderField} {(isAscending ? "ASC" : "DESC")}";
            }
            else
            {
                order = "ORDER BY (SELECT 0)";
            }

            string sqlQuery;
            var next = pageSize;
            var offset = pageSize * (pageIndex - 1);
            var rowStart = pageSize * (pageIndex - 1) + 1;
            var rowEnd = pageSize * pageIndex;
            var serverVersion = int.Parse(DbContext.Database.GetDbConnection().ServerVersion.Split('.')[0]);

            //判断是否with语法
            if (isWithSyntax)
            {
                sqlQuery = $"{sql} SELECT {CountSyntax} AS [TOTAL] FROM T;";

                if (serverVersion > 10)
                    sqlQuery += $"{sql.Remove(sql.LastIndexOf(")"), 1)} {(orderField.IsNullOrEmpty() ? "" : order)}) SELECT * FROM T OFFSET {offset} ROWS FETCH NEXT {next} ROWS ONLY;";
                else
                    sqlQuery += $"{sql},R AS (SELECT ROW_NUMBER() OVER ({order}) AS [ROWNUMBER], * FROM T) SELECT * FROM R WHERE [ROWNUMBER] BETWEEN {rowStart} AND {rowEnd};";
            }
            else
            {
                sqlQuery = $"SELECT {CountSyntax} AS [TOTAL] FROM ({sql}) AS T;";

                if (serverVersion > 10)
                    sqlQuery += $"{sql} {(orderField.IsNullOrEmpty() ? "" : order)} OFFSET {offset} ROWS FETCH NEXT {next} ROWS ONLY;";
                else
                    sqlQuery += $"SELECT * FROM (SELECT ROW_NUMBER() OVER ({order}) AS [ROWNUMBER], * FROM ({sql}) AS T) AS N WHERE [ROWNUMBER] BETWEEN {rowStart} AND {rowEnd};";
            }

            return sqlQuery;
        }
        #endregion
    }
}