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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
/****************************
* [Author] 张强
* [Date] 2020-10-19
* [Describe] DbContext扩展类
* **************************/
namespace EFCoreRepository.Extensions
{
    /// <summary>
    /// DbContext扩展类
    /// </summary>
    public static class DbContextExtensions
    {
        #region SqlQuery
        /// <summary>
        /// sql查询
        /// </summary>
        /// <param name="db"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static IEnumerable<T> SqlQuery<T>(this DbContext db, string sql, params DbParameter[] parameters)
        {
            using var cmd = db.Database.GetDbConnection().CreateCommand();

            cmd.CommandText = sql;
            cmd.CommandTimeout = db.Database.GetCommandTimeout() ?? 240;

            if (cmd.Connection.State != ConnectionState.Open)
                cmd.Connection.Open();

            if (db.Database.CurrentTransaction != null)
                cmd.Transaction = db.Database.CurrentTransaction.GetDbTransaction();

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                    cmd.Parameters.Add(parameter);
            }

            return cmd.ExecuteReader().ToList<T>();
        }

        /// <summary>
        /// sql查询
        /// </summary>
        /// <param name="db"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<T>> SqlQueryAsync<T>(this DbContext db, string sql, params DbParameter[] parameters)
        {
            using var cmd = db.Database.GetDbConnection().CreateCommand();

            cmd.CommandText = sql;
            cmd.CommandTimeout = db.Database.GetCommandTimeout() ?? 240;

            if (cmd.Connection.State != ConnectionState.Open)
                cmd.Connection.Open();

            if (db.Database.CurrentTransaction != null)
                cmd.Transaction = db.Database.CurrentTransaction.GetDbTransaction();

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                    cmd.Parameters.Add(parameter);
            }

            return (await cmd.ExecuteReaderAsync()).ToList<T>();
        }
        #endregion

        #region SqlQueryMultiple
        /// <summary>
        /// sql查询多结果集
        /// </summary>
        /// <param name="db"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static List<List<T>> SqlQueryMultiple<T>(this DbContext db, string sql, params DbParameter[] parameters)
        {
            using var cmd = db.Database.GetDbConnection().CreateCommand();

            cmd.CommandText = sql;
            cmd.CommandTimeout = db.Database.GetCommandTimeout() ?? 240;
            if (cmd.Connection.State != ConnectionState.Open)
                cmd.Connection.Open();

            if (db.Database.CurrentTransaction != null)
                cmd.Transaction = db.Database.CurrentTransaction.GetDbTransaction();

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                    cmd.Parameters.Add(parameter);
            }

            return cmd.ExecuteReader().ToLists<T>();
        }

        /// <summary>
        /// sql查询多结果集
        /// </summary>
        /// <param name="db"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static async Task<List<List<T>>> SqlQueryMultipleAsync<T>(this DbContext db, string sql, params DbParameter[] parameters)
        {
            using var cmd = db.Database.GetDbConnection().CreateCommand();

            cmd.CommandText = sql;
            cmd.CommandTimeout = db.Database.GetCommandTimeout() ?? 240;

            if (cmd.Connection.State != ConnectionState.Open)
                cmd.Connection.Open();

            if (db.Database.CurrentTransaction != null)
                cmd.Transaction = db.Database.CurrentTransaction.GetDbTransaction();

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                    cmd.Parameters.Add(parameter);
            }

            return (await cmd.ExecuteReaderAsync()).ToLists<T>();
        }
        #endregion

        #region SqlDataTable
        /// <summary>
        /// sql查询
        /// </summary>
        /// <param name="db"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static DataTable SqlDataTable(this DbContext db, string sql, params DbParameter[] parameters)
        {
            using var cmd = db.Database.GetDbConnection().CreateCommand();

            cmd.CommandText = sql;
            cmd.CommandTimeout = db.Database.GetCommandTimeout() ?? 240;

            if (cmd.Connection.State != ConnectionState.Open)
                cmd.Connection.Open();

            if (db.Database.CurrentTransaction != null)
                cmd.Transaction = db.Database.CurrentTransaction.GetDbTransaction();

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                    cmd.Parameters.Add(parameter);
            }

            return cmd.ExecuteReader().ToDataTable();
        }

        /// <summary>
        /// sql查询
        /// </summary>
        /// <param name="db"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static async Task<DataTable> SqlDataTableAsync(this DbContext db, string sql, params DbParameter[] parameters)
        {
            using var cmd = db.Database.GetDbConnection().CreateCommand();

            cmd.CommandText = sql;
            cmd.CommandTimeout = db.Database.GetCommandTimeout() ?? 240;

            if (cmd.Connection.State != ConnectionState.Open)
                cmd.Connection.Open();

            if (db.Database.CurrentTransaction != null)
                cmd.Transaction = db.Database.CurrentTransaction.GetDbTransaction();

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                    cmd.Parameters.Add(parameter);
            }

            return (await cmd.ExecuteReaderAsync()).ToDataTable();
        }
        #endregion

        #region SqlDataSet
        /// <summary>
        /// sql查询
        /// </summary>
        /// <param name="db"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static DataSet SqlDataSet(this DbContext db, string sql, params DbParameter[] parameters)
        {
            using var cmd = db.Database.GetDbConnection().CreateCommand();

            cmd.CommandText = sql;
            cmd.CommandTimeout = db.Database.GetCommandTimeout() ?? 240;

            if (cmd.Connection.State != ConnectionState.Open)
                cmd.Connection.Open();

            if (db.Database.CurrentTransaction != null)
                cmd.Transaction = db.Database.CurrentTransaction.GetDbTransaction();

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                    cmd.Parameters.Add(parameter);
            }

            return cmd.ExecuteReader().ToDataSet();
        }

        /// <summary>
        /// sql查询
        /// </summary>
        /// <param name="db"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static async Task<DataSet> SqlDataSetAsync(this DbContext db, string sql, params DbParameter[] parameters)
        {
            using var cmd = db.Database.GetDbConnection().CreateCommand();

            cmd.CommandText = sql;
            cmd.CommandTimeout = db.Database.GetCommandTimeout() ?? 240;

            if (cmd.Connection.State != ConnectionState.Open)
                cmd.Connection.Open();

            if (db.Database.CurrentTransaction != null)
                cmd.Transaction = db.Database.CurrentTransaction.GetDbTransaction();

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                    cmd.Parameters.Add(parameter);
            }

            return (await cmd.ExecuteReaderAsync()).ToDataSet();
        }
        #endregion

        #region ExecuteSql
        /// <summary>
        /// 执行sql语句
        /// </summary>
        /// <param name="db"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static int ExecuteSql(this DbContext db, string sql, params DbParameter[] parameters)
        {
            using var cmd = db.Database.GetDbConnection().CreateCommand();

            cmd.CommandText = sql;
            cmd.CommandTimeout = db.Database.GetCommandTimeout() ?? 240;

            if (cmd.Connection.State != ConnectionState.Open)
                cmd.Connection.Open();

            if (db.Database.CurrentTransaction != null)
                cmd.Transaction = db.Database.CurrentTransaction.GetDbTransaction();

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                    cmd.Parameters.Add(parameter);
            }

            return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 执行sql语句
        /// </summary>
        /// <param name="db"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static async Task<int> ExecuteSqlAsync(this DbContext db, string sql, params DbParameter[] parameters)
        {
            using var cmd = db.Database.GetDbConnection().CreateCommand();

            cmd.CommandText = sql;
            cmd.CommandTimeout = db.Database.GetCommandTimeout() ?? 240;

            if (cmd.Connection.State != ConnectionState.Open)
                cmd.Connection.Open();

            if (db.Database.CurrentTransaction != null)
                cmd.Transaction = db.Database.CurrentTransaction.GetDbTransaction();

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                    cmd.Parameters.Add(parameter);
            }

            return await cmd.ExecuteNonQueryAsync();
        }
        #endregion

        #region ExecuteProc
        /// <summary>
        /// 执行sql存储过程
        /// </summary>
        /// <param name="db"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static int ExecuteProc(this DbContext db, string sql, params DbParameter[] parameters)
        {
            using var cmd = db.Database.GetDbConnection().CreateCommand();

            cmd.CommandText = sql;
            cmd.CommandTimeout = db.Database.GetCommandTimeout() ?? 240;
            cmd.CommandType = CommandType.StoredProcedure;

            if (cmd.Connection.State != ConnectionState.Open)
                cmd.Connection.Open();

            if (db.Database.CurrentTransaction != null)
                cmd.Transaction = db.Database.CurrentTransaction.GetDbTransaction();

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                    cmd.Parameters.Add(parameter);
            }

            return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 执行sql存储过程
        /// </summary>
        /// <param name="db"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static IEnumerable<T> ExecuteProc<T>(this DbContext db, string sql, params DbParameter[] parameters)
        {
            using var cmd = db.Database.GetDbConnection().CreateCommand();

            cmd.CommandText = sql;
            cmd.CommandTimeout = db.Database.GetCommandTimeout() ?? 240;
            cmd.CommandType = CommandType.StoredProcedure;

            if (cmd.Connection.State != ConnectionState.Open)
                cmd.Connection.Open();

            if (db.Database.CurrentTransaction != null)
                cmd.Transaction = db.Database.CurrentTransaction.GetDbTransaction();

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                    cmd.Parameters.Add(parameter);
            }

            return cmd.ExecuteReader().ToList<T>();
        }

        /// <summary>
        /// 执行sql存储过程
        /// </summary>
        /// <param name="db"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static async Task<int> ExecuteProcAsync(this DbContext db, string sql, params DbParameter[] parameters)
        {
            using var cmd = db.Database.GetDbConnection().CreateCommand();

            cmd.CommandText = sql;
            cmd.CommandTimeout = db.Database.GetCommandTimeout() ?? 240;
            cmd.CommandType = CommandType.StoredProcedure;

            if (cmd.Connection.State != ConnectionState.Open)
                cmd.Connection.Open();

            if (db.Database.CurrentTransaction != null)
                cmd.Transaction = db.Database.CurrentTransaction.GetDbTransaction();

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                    cmd.Parameters.Add(parameter);
            }

            return await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 执行sql存储过程
        /// </summary>
        /// <param name="db"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<T>> ExecuteProcAsync<T>(this DbContext db, string sql, params DbParameter[] parameters)
        {
            using var cmd = db.Database.GetDbConnection().CreateCommand();

            cmd.CommandText = sql;
            cmd.CommandTimeout = db.Database.GetCommandTimeout() ?? 240;
            cmd.CommandType = CommandType.StoredProcedure;

            if (cmd.Connection.State != ConnectionState.Open)
                cmd.Connection.Open();

            if (db.Database.CurrentTransaction != null)
                cmd.Transaction = db.Database.CurrentTransaction.GetDbTransaction();

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                    cmd.Parameters.Add(parameter);
            }

            return (await cmd.ExecuteReaderAsync()).ToList<T>();
        }
        #endregion
    }
}
