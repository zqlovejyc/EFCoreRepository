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
* [Date] 2018-09-27
* [Describe] Sqlite仓储实现类
* **************************/
namespace EFCoreRepository
{
    /// <summary>
    /// Sqlite仓储实现类
    /// </summary>
    public class SqliteRepository : IRepository
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

        /// <summary>
        /// DbContext
        /// </summary>
        public DbContext DbContext { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">DbContext实例</param>
        public SqliteRepository(DbContext context)
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

        #region ExecuteBySql
        #region Sync
        /// <summary>
        /// 执行sql语句
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <returns>返回受影响行数</returns>
        public int ExecuteBySql(string sql)
        {
            return this.DbContext.Database.ExecuteSqlRaw(sql);
        }

        /// <summary>
        /// 执行sql语句
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回受影响行数</returns>
        public int ExecuteBySql(string sql, params object[] parameter)
        {
            return this.DbContext.Database.ExecuteSqlRaw(sql, parameter);
        }

        /// <summary>
        /// 执行sql存储过程
        /// </summary>
        /// <param name="procName">存储过程名称</param>
        ///  <param name="parameter">对应参数</param>
        /// <returns>返回受影响行数</returns>
        public int ExecuteByProc(string procName, params DbParameter[] parameter)
        {
            return this.DbContext.ExecuteProc(procName, parameter);
        }

        /// <summary>
        /// 执行sql存储过程查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="procName"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public IEnumerable<T> ExecuteByProc<T>(string procName, params DbParameter[] parameter)
        {
            return this.DbContext.ExecuteProc<T>(procName, parameter);
        }
        #endregion

        #region Async
        /// <summary>
        /// 执行sql语句
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <returns>返回受影响行数</returns>
        public async Task<int> ExecuteBySqlAsync(string sql)
        {
            return await this.DbContext.Database.ExecuteSqlRawAsync(sql);
        }

        /// <summary>
        /// 执行sql语句
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回受影响行数</returns>
        public async Task<int> ExecuteBySqlAsync(string sql, params object[] parameter)
        {
            return await this.DbContext.Database.ExecuteSqlRawAsync(sql, parameter);
        }

        /// <summary>
        /// 执行sql存储过程
        /// </summary>
        /// <param name="procName">存储过程名称</param>
        ///  <param name="parameter">对应参数</param>
        /// <returns>返回受影响行数</returns>
        public async Task<int> ExecuteByProcAsync(string procName, params DbParameter[] parameter)
        {
            return await this.DbContext.ExecuteProcAsync(procName, parameter);
        }

        /// <summary>
        /// 执行sql存储过程查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="procName"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> ExecuteByProcAsync<T>(string procName, params DbParameter[] parameter)
        {
            return await this.DbContext.ExecuteProcAsync<T>(procName, parameter);
        }
        #endregion
        #endregion

        #region Insert
        #region Sync
        /// <summary>
        ///  插入单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entity">要插入的实体</param>
        /// <returns>返回受影响行数</returns>
        public int Insert<T>(T entity) where T : class
        {
            this.DbContext.Set<T>().Add(entity);
            return this.DbContext.SaveChanges();
        }

        /// <summary>
        /// 插入多个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entities">要插入的实体集合</param>
        /// <returns>返回受影响行数</returns>
        public int Insert<T>(IEnumerable<T> entities) where T : class
        {
            this.DbContext.Set<T>().AddRange(entities);
            return this.DbContext.SaveChanges();
        }
        #endregion

        #region Async
        /// <summary>
        ///  插入单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entity">要插入的实体</param>
        /// <returns>返回受影响行数</returns>
        public async Task<int> InsertAsync<T>(T entity) where T : class
        {
            await this.DbContext.Set<T>().AddAsync(entity);
            return await this.DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 插入多个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entities">要插入的实体集合</param>
        /// <returns>返回受影响行数</returns>
        public async Task<int> InsertAsync<T>(IEnumerable<T> entities) where T : class
        {
            await this.DbContext.Set<T>().AddRangeAsync(entities);
            return await this.DbContext.SaveChangesAsync();
        }
        #endregion
        #endregion

        #region Delete
        #region Sync
        /// <summary>
        /// 删除全部
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <returns>返回受影响行数</returns>
        public int Delete<T>() where T : class
        {
            var entities = this.FindList<T>();
            return this.Delete(entities);
        }

        /// <summary>
        /// 删除单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entity">要删除的实体</param>
        /// <returns>返回受影响行数</returns>
        public int Delete<T>(T entity) where T : class
        {
            this.DbContext.Set<T>().Remove(entity);
            return this.DbContext.SaveChanges();
        }

        /// <summary>
        /// 删除多个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entities">要删除的实体集合</param>
        /// <returns>返回受影响行数</returns>
        public int Delete<T>(IEnumerable<T> entities) where T : class
        {
            this.DbContext.Set<T>().RemoveRange(entities);
            return this.DbContext.SaveChanges();
        }

        /// <summary>
        /// 根据条件删除实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="predicate">条件</param>
        /// <returns>返回受影响行数</returns>
        public int Delete<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            var entities = this.FindList(predicate);
            return this.Delete(entities);
        }

        /// <summary>
        /// 根据主键删除实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="keyValues">主键值</param>
        /// <returns>返回受影响行数</returns>
        public int Delete<T>(params object[] keyValues) where T : class
        {
            var entity = this.FindEntity<T>(keyValues);
            if (entity != null)
            {
                return this.Delete(entity);
            }
            return 0;
        }
        #endregion

        #region Async
        /// <summary>
        /// 删除全部
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <returns>返回受影响行数</returns>
        public async Task<int> DeleteAsync<T>() where T : class
        {
            var entities = await this.FindListAsync<T>();
            return await this.DeleteAsync(entities);
        }

        /// <summary>
        /// 删除单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entity">要删除的实体</param>
        /// <returns>返回受影响行数</returns>
        public async Task<int> DeleteAsync<T>(T entity) where T : class
        {
            this.DbContext.Set<T>().Remove(entity);
            return await this.DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 删除多个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entities">要删除的实体集合</param>
        /// <returns>返回受影响行数</returns>
        public async Task<int> DeleteAsync<T>(IEnumerable<T> entities) where T : class
        {
            this.DbContext.Set<T>().RemoveRange(entities);
            return await this.DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 根据条件删除实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="predicate">条件</param>
        /// <returns>返回受影响行数</returns>
        public async Task<int> DeleteAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            var entities = await this.FindListAsync(predicate);
            return await this.DeleteAsync(entities);
        }

        /// <summary>
        /// 根据主键删除实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="keyValues">主键值</param>
        /// <returns>返回受影响行数</returns>
        public async Task<int> DeleteAsync<T>(params object[] keyValues) where T : class
        {
            var entity = await this.FindEntityAsync<T>(keyValues);
            if (entity != null)
            {
                return await this.DeleteAsync(entity);
            }
            return 0;
        }
        #endregion
        #endregion

        #region Update
        #region Sync
        /// <summary>
        /// 更新单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entity">要更新的实体</param>
        /// <returns>返回受影响行数</returns>
        public int Update<T>(T entity) where T : class
        {
            this.DbContext.Set<T>().Attach(entity);
            var entry = this.DbContext.Entry(entity);
            var props = entity.GetType().GetProperties();
            foreach (var prop in props)
            {
                //非null且非PrimaryKey
                if (prop.GetValue(entity, null) != null && !entry.Property(prop.Name).Metadata.IsPrimaryKey())
                {
                    entry.Property(prop.Name).IsModified = true;
                }
            }
            return this.DbContext.SaveChanges();
        }

        /// <summary>
        /// 更新多个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entities">要更新的实体集合</param>
        /// <returns>返回受影响行数</returns>
        public int Update<T>(IEnumerable<T> entities) where T : class
        {
            foreach (var entity in entities)
            {
                this.DbContext.Set<T>().Attach(entity);
                var entry = this.DbContext.Entry(entity);
                var props = entity.GetType().GetProperties();
                foreach (var prop in props)
                {
                    //非null且非PrimaryKey
                    if (prop.GetValue(entity, null) != null && !entry.Property(prop.Name).Metadata.IsPrimaryKey())
                    {
                        entry.Property(prop.Name).IsModified = true;
                    }
                }
            }
            return this.DbContext.SaveChanges();
        }

        /// <summary>
        /// 根据条件更新实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="predicate">条件</param>
        /// <param name="entity">要更新的实体</param>
        /// <returns>返回受影响行数</returns>
        public int Update<T>(Expression<Func<T, bool>> predicate, T entity) where T : class
        {
            var entities = new List<T>();
            var instances = this.FindList(predicate);
            //设置所有状态为未跟踪状态
            this.DbContext.ChangeTracker.Entries<T>().ToList().ForEach(o => o.State = EntityState.Detached);
            foreach (var instance in instances)
            {
                var properties = typeof(T).GetProperties();
                foreach (var property in properties)
                {
                    var isKey = property.GetCustomAttributes(typeof(KeyAttribute), false).Count() > 0;
                    if (isKey)
                    {
                        var keyVal = property.GetValue(instance);
                        if (keyVal != null)
                            property.SetValue(entity, keyVal);
                    }
                }
                //深度拷贝实体，避免列表中所有实体引用地址都相同
                entities.Add(MapperHelper<T, T>.MapTo(entity));
            }
            return this.Update<T>(entities);
        }
        #endregion

        #region Async
        /// <summary>
        /// 更新单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entity">要更新的实体</param>
        /// <returns>返回受影响行数</returns>
        public async Task<int> UpdateAsync<T>(T entity) where T : class
        {
            this.DbContext.Set<T>().Attach(entity);
            var entry = this.DbContext.Entry(entity);
            var props = entity.GetType().GetProperties();
            foreach (var prop in props)
            {
                //非null且非PrimaryKey
                if (prop.GetValue(entity, null) != null && !entry.Property(prop.Name).Metadata.IsPrimaryKey())
                {
                    entry.Property(prop.Name).IsModified = true;
                }
            }
            return await this.DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 更新多个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entities">要更新的实体集合</param>
        /// <returns>返回受影响行数</returns>
        public async Task<int> UpdateAsync<T>(IEnumerable<T> entities) where T : class
        {
            foreach (var entity in entities)
            {
                this.DbContext.Set<T>().Attach(entity);
                var entry = this.DbContext.Entry(entity);
                var props = entity.GetType().GetProperties();
                foreach (var prop in props)
                {
                    //非null且非PrimaryKey
                    if (prop.GetValue(entity, null) != null && !entry.Property(prop.Name).Metadata.IsPrimaryKey())
                    {
                        entry.Property(prop.Name).IsModified = true;
                    }
                }
            }
            return await this.DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 根据条件更新实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="predicate">条件</param>
        /// <param name="entity">要更新的实体</param>
        /// <returns>返回受影响行数</returns>
        public async Task<int> UpdateAsync<T>(Expression<Func<T, bool>> predicate, T entity) where T : class
        {
            var entities = new List<T>();
            var instances = await this.FindListAsync(predicate);
            //设置所有状态为未跟踪状态
            this.DbContext.ChangeTracker.Entries<T>().ToList().ForEach(o => o.State = EntityState.Detached);
            foreach (var instance in instances)
            {
                var properties = typeof(T).GetProperties();
                foreach (var property in properties)
                {
                    var isKey = property.GetCustomAttributes(typeof(KeyAttribute), false).Count() > 0;
                    if (isKey)
                    {
                        var keyVal = property.GetValue(instance);
                        if (keyVal != null)
                            property.SetValue(entity, keyVal);
                    }
                }
                //深度拷贝实体，避免列表中所有实体引用地址都相同
                entities.Add(MapperHelper<T, T>.MapTo(entity));
            }
            return await this.UpdateAsync<T>(entities);
        }
        #endregion
        #endregion

        #region FindObject
        #region Sync
        /// <summary>
        /// 查询单个对象
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <returns>返回查询结果对象</returns>
        public object FindObject(string sql)
        {
            return this.FindObject(sql, null);
        }

        /// <summary>
        /// 查询单个对象
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回查询结果对象</returns>
        public object FindObject(string sql, params DbParameter[] parameter)
        {
            return this.DbContext.SqlQuery<Dictionary<string, object>>(sql, parameter)?.FirstOrDefault()?.Select(o => o.Value).FirstOrDefault();
        }
        #endregion

        #region Async
        /// <summary>
        /// 查询单个对象
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <returns>返回查询结果对象</returns>
        public async Task<object> FindObjectAsync(string sql)
        {
            return await this.FindObjectAsync(sql, null);
        }

        /// <summary>
        /// 查询单个对象
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回查询结果对象</returns>
        public async Task<object> FindObjectAsync(string sql, params DbParameter[] parameter)
        {
            return (await this.DbContext.SqlQueryAsync<Dictionary<string, object>>(sql, parameter))?.FirstOrDefault()?.Select(o => o.Value).FirstOrDefault();
        }
        #endregion
        #endregion

        #region FindEntity
        #region Sync
        /// <summary>
        /// 根据主键查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="keyValues">主键值</param>
        /// <returns>返回实体</returns>
        public T FindEntity<T>(params object[] keyValues) where T : class
        {
            return this.DbContext.Set<T>().Find(keyValues);
        }

        /// <summary>
        /// 根据条件查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="predicate">条件</param>
        /// <returns>返回实体</returns>
        public T FindEntity<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return this.DbContext.Set<T>().Where(predicate).FirstOrDefault();
        }

        /// <summary>
        /// 根据条件查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <param name="predicate">条件</param>
        /// <returns>返回实体</returns>
        public S FindEntity<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate) where T : class
        {
            return this.DbContext.Set<T>().Where(predicate).Select(selector).FirstOrDefault();
        }

        /// <summary>
        /// 根据sql语句查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <returns>返回实体</returns>
        public T FindEntityBySql<T>(string sql)
        {
            return this.FindEntityBySql<T>(sql, null);
        }

        /// <summary>
        /// 根据sql语句查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回实体</returns>
        public T FindEntityBySql<T>(string sql, params DbParameter[] parameter)
        {
            var query = this.DbContext.SqlQuery<T>(sql, parameter);
            if (query != null)
            {
                return query.FirstOrDefault();
            }
            return default(T);
        }

        /// <summary>
        /// 根据sql语句查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回实体</returns>
        public T FindEntityBySql<T>(string sql, params object[] parameter) where T : class
        {
            return this.DbContext.Set<T>().FromSqlRaw(sql, parameter).FirstOrDefault();
        }
        #endregion

        #region Async
        /// <summary>
        /// 根据主键查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <param name="keyValues">主键值</param>
        /// <returns>返回实体</returns>
        public async Task<T> FindEntityAsync<T>(params object[] keyValues) where T : class
        {
            return await this.DbContext.Set<T>().FindAsync(keyValues);
        }

        /// <summary>
        /// 根据条件查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <param name="predicate">条件</param>
        /// <returns>返回实体</returns>
        public async Task<T> FindEntityAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return await this.DbContext.Set<T>().Where(predicate).FirstOrDefaultAsync();
        }

        /// <summary>
        /// 根据条件查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <param name="predicate">条件</param>
        /// <returns>返回实体</returns>
        public async Task<S> FindEntityAsync<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate) where T : class
        {
            return await this.DbContext.Set<T>().Where(predicate).Select(selector).FirstOrDefaultAsync();
        }

        /// <summary>
        /// 根据sql语句查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <returns>返回实体</returns>
        public async Task<T> FindEntityBySqlAsync<T>(string sql)
        {
            return await this.FindEntityBySqlAsync<T>(sql, null);
        }

        /// <summary>
        /// 根据sql语句查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回实体</returns>
        public async Task<T> FindEntityBySqlAsync<T>(string sql, params DbParameter[] parameter)
        {
            var query = await this.DbContext.SqlQueryAsync<T>(sql, parameter);
            if (query != null)
            {
                return query.FirstOrDefault();
            }
            return default(T);
        }

        /// <summary>
        /// 根据sql语句查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回实体</returns>
        public async Task<T> FindEntityBySqlAsync<T>(string sql, params object[] parameter) where T : class
        {
            return await this.DbContext.Set<T>().FromSqlRaw(sql, parameter).FirstOrDefaultAsync();
        }
        #endregion
        #endregion

        #region IQueryable
        #region Sync
        /// <summary>
        /// 查询全部
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <returns>返回集合</returns>
        public IQueryable<T> IQueryable<T>() where T : class
        {
            return this.DbContext.Set<T>();
        }

        /// <summary>
        /// 查询指定列
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <returns>返回集合</returns>
        public IQueryable<S> IQueryable<T, S>(Expression<Func<T, S>> selector) where T : class
        {
            return this.DbContext.Set<T>().Select(selector);
        }

        /// <summary>
        /// 根据条件查询
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <param name="predicate">条件</param>
        /// <returns>返回集合</returns>
        public IQueryable<T> IQueryable<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return this.DbContext.Set<T>().Where(predicate);
        }

        /// <summary>
        /// 根据条件查询并排序
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <param name="predicate">条件</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="orderTypes">排序类型，默认正序排序</param>
        /// <returns>返回集合</returns>
        public IQueryable<T> IQueryable<T>(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = this.DbContext.Set<T>().Where(predicate);
            //多个字段排序
            if (orderField.Body is NewExpression newExpression)
            {
                IOrderedQueryable<T> order = null;
                for (var i = 0; i < newExpression.Members.Count; i++)
                {
                    //指定排序类型
                    if (i <= orderTypes.Length - 1)
                    {
                        if (orderTypes[i] == OrderType.Descending)
                        {
                            if (i > 0)
                                order = order.ThenByDescending(newExpression.Members[i].Name);
                            else
                                order = query.OrderByDescending(newExpression.Members[i].Name);
                        }
                        else
                        {
                            if (i > 0)
                                order = order.ThenBy(newExpression.Members[i].Name);
                            else
                                order = query.OrderBy(newExpression.Members[i].Name);
                        }
                    }
                    else
                    {
                        if (i > 0)
                            order = order.ThenBy(newExpression.Members[i].Name);
                        else
                            order = query.OrderBy(newExpression.Members[i].Name);
                    }
                }
                return order;
            }
            //单个字段排序
            else
            {
                if (orderTypes.FirstOrDefault() == OrderType.Descending)
                    return query.OrderByDescending(orderField);
                else
                    return query.OrderBy(orderField);
            }
        }

        /// <summary>
        /// 根据条件查询指定列
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <param name="predicate">条件</param>
        /// <returns>返回集合</returns>
        public IQueryable<S> IQueryable<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate) where T : class
        {
            return this.DbContext.Set<T>().Where(predicate).Select(selector);
        }

        /// <summary>
        /// 根据条件查询指定列并排序
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <param name="predicate">条件</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="orderTypes">排序类型，默认正序排序</param>
        /// <returns>返回集合</returns>
        public IQueryable<S> IQueryable<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = this.DbContext.Set<T>().Where(predicate);
            //多个字段排序
            if (orderField.Body is NewExpression newExpression)
            {
                IOrderedQueryable<T> order = null;
                for (var i = 0; i < newExpression.Members.Count; i++)
                {
                    //指定排序类型
                    if (i <= orderTypes.Length - 1)
                    {
                        if (orderTypes[i] == OrderType.Descending)
                        {
                            if (i > 0)
                                order = order.ThenByDescending(newExpression.Members[i].Name);
                            else
                                order = query.OrderByDescending(newExpression.Members[i].Name);
                        }
                        else
                        {
                            if (i > 0)
                                order = order.ThenBy(newExpression.Members[i].Name);
                            else
                                order = query.OrderBy(newExpression.Members[i].Name);
                        }
                    }
                    else
                    {
                        if (i > 0)
                            order = order.ThenBy(newExpression.Members[i].Name);
                        else
                            order = query.OrderBy(newExpression.Members[i].Name);
                    }
                }
                return order.Select(selector);
            }
            //单个字段排序
            else
            {
                if (orderTypes.FirstOrDefault() == OrderType.Descending)
                    return query.OrderByDescending(orderField).Select(selector);
                else
                    return query.OrderBy(orderField).Select(selector);
            }
        }
        #endregion

        #region Async
        /// <summary>
        /// 查询全部
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <returns>返回集合</returns>
        public async Task<IQueryable<T>> IQueryableAsync<T>() where T : class
        {
            return await Task.FromResult(this.DbContext.Set<T>());
        }

        /// <summary>
        /// 查询指定列
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <returns>返回集合</returns>
        public async Task<IQueryable<S>> IQueryableAsync<T, S>(Expression<Func<T, S>> selector) where T : class
        {
            return await Task.FromResult(this.DbContext.Set<T>().Select(selector));
        }

        /// <summary>
        /// 根据条件查询
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <param name="predicate">条件</param>
        /// <returns>返回集合</returns>
        public async Task<IQueryable<T>> IQueryableAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return await Task.FromResult(this.DbContext.Set<T>().Where(predicate));
        }

        /// <summary>
        /// 根据条件查询并排序
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <param name="predicate">条件</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="orderTypes">排序类型，默认正序排序</param>
        /// <returns>返回集合</returns>
        public async Task<IQueryable<T>> IQueryableAsync<T>(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = this.DbContext.Set<T>().Where(predicate);
            //多个字段排序
            if (orderField.Body is NewExpression newExpression)
            {
                IOrderedQueryable<T> order = null;
                for (var i = 0; i < newExpression.Members.Count; i++)
                {
                    //指定排序类型
                    if (i <= orderTypes.Length - 1)
                    {
                        if (orderTypes[i] == OrderType.Descending)
                        {
                            if (i > 0)
                                order = order.ThenByDescending(newExpression.Members[i].Name);
                            else
                                order = query.OrderByDescending(newExpression.Members[i].Name);
                        }
                        else
                        {
                            if (i > 0)
                                order = order.ThenBy(newExpression.Members[i].Name);
                            else
                                order = query.OrderBy(newExpression.Members[i].Name);
                        }
                    }
                    else
                    {
                        if (i > 0)
                            order = order.ThenBy(newExpression.Members[i].Name);
                        else
                            order = query.OrderBy(newExpression.Members[i].Name);
                    }
                }
                return await Task.FromResult(order);
            }
            //单个字段排序
            else
            {
                if (orderTypes.FirstOrDefault() == OrderType.Descending)
                    return await Task.FromResult(query.OrderByDescending(orderField));
                else
                    return await Task.FromResult(query.OrderBy(orderField));
            }
        }

        /// <summary>
        /// 根据条件查询指定列
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <param name="predicate">条件</param>
        /// <returns>返回集合</returns>
        public async Task<IQueryable<S>> IQueryableAsync<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate) where T : class
        {
            return await Task.FromResult(this.DbContext.Set<T>().Where(predicate).Select(selector));
        }

        /// <summary>
        /// 根据条查询指定列并排序
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <param name="predicate">条件</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="orderTypes">排序类型，默认正序排序</param>
        /// <returns>返回集合</returns>
        public async Task<IQueryable<S>> IQueryableAsync<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = this.DbContext.Set<T>().Where(predicate);
            //多个字段排序
            if (orderField.Body is NewExpression newExpression)
            {
                IOrderedQueryable<T> order = null;
                for (var i = 0; i < newExpression.Members.Count; i++)
                {
                    //指定排序类型
                    if (i <= orderTypes.Length - 1)
                    {
                        if (orderTypes[i] == OrderType.Descending)
                        {
                            if (i > 0)
                                order = order.ThenByDescending(newExpression.Members[i].Name);
                            else
                                order = query.OrderByDescending(newExpression.Members[i].Name);
                        }
                        else
                        {
                            if (i > 0)
                                order = order.ThenBy(newExpression.Members[i].Name);
                            else
                                order = query.OrderBy(newExpression.Members[i].Name);
                        }
                    }
                    else
                    {
                        if (i > 0)
                            order = order.ThenBy(newExpression.Members[i].Name);
                        else
                            order = query.OrderBy(newExpression.Members[i].Name);
                    }
                }
                return await Task.FromResult(order.Select(selector));
            }
            //单个字段排序
            else
            {
                if (orderTypes.FirstOrDefault() == OrderType.Descending)
                    return await Task.FromResult(query.OrderByDescending(orderField).Select(selector));
                else
                    return await Task.FromResult(query.OrderBy(orderField).Select(selector));
            }
        }
        #endregion
        #endregion

        #region FindList
        #region Sync
        /// <summary>
        /// 查询全部
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <returns>返回集合</returns>
        public IEnumerable<T> FindList<T>() where T : class
        {
            return this.DbContext.Set<T>().ToList();
        }

        /// <summary>
        /// 查询指定列
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <returns>返回集合</returns>
        public IEnumerable<S> FindList<T, S>(Expression<Func<T, S>> selector) where T : class
        {
            return this.DbContext.Set<T>().Select(selector).ToList();
        }

        /// <summary>
        /// 查询全部并排序
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <param name="orderField">排序字段</param>
        /// <param name="orderTypes">排序类型，默认正序排序</param>
        /// <returns>返回集合</returns>
        public IEnumerable<T> FindList<T>(Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = this.DbContext.Set<T>();
            //多个字段排序
            if (orderField.Body is NewExpression newExpression)
            {
                IOrderedQueryable<T> order = null;
                for (var i = 0; i < newExpression.Members.Count; i++)
                {
                    //指定排序类型
                    if (i <= orderTypes.Length - 1)
                    {
                        if (orderTypes[i] == OrderType.Descending)
                        {
                            if (i > 0)
                                order = order.ThenByDescending(newExpression.Members[i].Name);
                            else
                                order = query.OrderByDescending(newExpression.Members[i].Name);
                        }
                        else
                        {
                            if (i > 0)
                                order = order.ThenBy(newExpression.Members[i].Name);
                            else
                                order = query.OrderBy(newExpression.Members[i].Name);
                        }
                    }
                    else
                    {
                        if (i > 0)
                            order = order.ThenBy(newExpression.Members[i].Name);
                        else
                            order = query.OrderBy(newExpression.Members[i].Name);
                    }
                }
                return order.ToList();
            }
            //单个字段排序
            else
            {
                if (orderTypes.FirstOrDefault() == OrderType.Descending)
                    return query.OrderByDescending(orderField).ToList();
                else
                    return query.OrderBy(orderField).ToList();
            }
        }

        /// <summary>
        /// 查询指定列并排序
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="orderTypes">排序类型，默认正序排序</param>
        /// <returns>返回集合</returns>
        public IEnumerable<S> FindList<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = this.DbContext.Set<T>();
            //多个字段排序
            if (orderField.Body is NewExpression newExpression)
            {
                IOrderedQueryable<T> order = null;
                for (var i = 0; i < newExpression.Members.Count; i++)
                {
                    //指定排序类型
                    if (i <= orderTypes.Length - 1)
                    {
                        if (orderTypes[i] == OrderType.Descending)
                        {
                            if (i > 0)
                                order = order.ThenByDescending(newExpression.Members[i].Name);
                            else
                                order = query.OrderByDescending(newExpression.Members[i].Name);
                        }
                        else
                        {
                            if (i > 0)
                                order = order.ThenBy(newExpression.Members[i].Name);
                            else
                                order = query.OrderBy(newExpression.Members[i].Name);
                        }
                    }
                    else
                    {
                        if (i > 0)
                            order = order.ThenBy(newExpression.Members[i].Name);
                        else
                            order = query.OrderBy(newExpression.Members[i].Name);
                    }
                }
                return order.Select(selector).ToList();
            }
            //单个字段排序
            else
            {
                if (orderTypes.FirstOrDefault() == OrderType.Descending)
                    return query.OrderByDescending(orderField).Select(selector).ToList();
                else
                    return query.OrderBy(orderField).Select(selector).ToList();
            }
        }

        /// <summary>
        /// 根据条件查询
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <param name="predicate">条件</param>
        /// <returns>返回集合</returns>
        public IEnumerable<T> FindList<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return this.DbContext.Set<T>().Where(predicate).ToList();
        }

        /// <summary>
        /// 根据条件查询并排序
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <param name="predicate">条件</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="orderTypes">排序类型，默认正序排序</param>
        /// <returns>返回集合</returns>
        public IEnumerable<T> FindList<T>(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = this.DbContext.Set<T>().Where(predicate);
            //多个字段排序
            if (orderField.Body is NewExpression newExpression)
            {
                IOrderedQueryable<T> order = null;
                for (var i = 0; i < newExpression.Members.Count; i++)
                {
                    //指定排序类型
                    if (i <= orderTypes.Length - 1)
                    {
                        if (orderTypes[i] == OrderType.Descending)
                        {
                            if (i > 0)
                                order = order.ThenByDescending(newExpression.Members[i].Name);
                            else
                                order = query.OrderByDescending(newExpression.Members[i].Name);
                        }
                        else
                        {
                            if (i > 0)
                                order = order.ThenBy(newExpression.Members[i].Name);
                            else
                                order = query.OrderBy(newExpression.Members[i].Name);
                        }
                    }
                    else
                    {
                        if (i > 0)
                            order = order.ThenBy(newExpression.Members[i].Name);
                        else
                            order = query.OrderBy(newExpression.Members[i].Name);
                    }
                }
                return order.ToList();
            }
            //单个字段排序
            else
            {
                if (orderTypes.FirstOrDefault() == OrderType.Descending)
                    return query.OrderByDescending(orderField).ToList();
                else
                    return query.OrderBy(orderField).ToList();
            }
        }

        /// <summary>
        /// 根据条件查询
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <param name="predicate">条件</param>
        /// <returns>返回集合</returns>
        public IEnumerable<S> FindList<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate) where T : class
        {
            return this.DbContext.Set<T>().Where(predicate).Select(selector).ToList();
        }

        /// <summary>
        /// 根据条件查询指定列并排序
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <param name="predicate">条件</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="orderTypes">排序类型，默认正序排序</param>
        /// <returns>返回集合</returns>
        public IEnumerable<S> FindList<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = this.DbContext.Set<T>().Where(predicate);
            //多个字段排序
            if (orderField.Body is NewExpression newExpression)
            {
                IOrderedQueryable<T> order = null;
                for (var i = 0; i < newExpression.Members.Count; i++)
                {
                    //指定排序类型
                    if (i <= orderTypes.Length - 1)
                    {
                        if (orderTypes[i] == OrderType.Descending)
                        {
                            if (i > 0)
                                order = order.ThenByDescending(newExpression.Members[i].Name);
                            else
                                order = query.OrderByDescending(newExpression.Members[i].Name);
                        }
                        else
                        {
                            if (i > 0)
                                order = order.ThenBy(newExpression.Members[i].Name);
                            else
                                order = query.OrderBy(newExpression.Members[i].Name);
                        }
                    }
                    else
                    {
                        if (i > 0)
                            order = order.ThenBy(newExpression.Members[i].Name);
                        else
                            order = query.OrderBy(newExpression.Members[i].Name);
                    }
                }
                return order.Select(selector).ToList();
            }
            //单个字段排序
            else
            {
                if (orderTypes.FirstOrDefault() == OrderType.Descending)
                    return query.OrderByDescending(orderField).Select(selector).ToList();
                else
                    return query.OrderBy(orderField).Select(selector).ToList();
            }
        }

        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <returns>返回集合</returns>
        public IEnumerable<T> FindList<T>(string sql)
        {
            return this.FindList<T>(sql, null);
        }

        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回集合</returns>
        public IEnumerable<T> FindList<T>(string sql, params DbParameter[] parameter)
        {
            return this.DbContext.SqlQuery<T>(sql, parameter);
        }

        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回集合</returns>
        public IEnumerable<T> FindList<T>(string sql, params object[] parameter) where T : class
        {
            return this.DbContext.Set<T>().FromSqlRaw(sql, parameter).ToList();
        }

        /// <summary>
        /// 根据条件分页查询
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="predicate">条件</param>
        /// <param name="orderField">排序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>
        /// <param name="orderTypes">排序类型，默认正序排序</param>
        /// <returns>返回集合和总记录数</returns>
        public (IEnumerable<T> list, long total) FindList<T>(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, int pageSize, int pageIndex, params OrderType[] orderTypes) where T : class
        {
            IOrderedQueryable<T> order = null;
            var query = this.DbContext.Set<T>().Where(predicate);
            var total = query.Count();
            //多个字段排序
            if (orderField.Body is NewExpression newExpression)
            {
                for (var i = 0; i < newExpression.Members.Count; i++)
                {
                    //指定排序类型
                    if (i <= orderTypes.Length - 1)
                    {
                        if (orderTypes[i] == OrderType.Descending)
                        {
                            if (i > 0)
                                order = order.ThenByDescending(newExpression.Members[i].Name);
                            else
                                order = query.OrderByDescending(newExpression.Members[i].Name);
                        }
                        else
                        {
                            if (i > 0)
                                order = order.ThenBy(newExpression.Members[i].Name);
                            else
                                order = query.OrderBy(newExpression.Members[i].Name);
                        }
                    }
                    else
                    {
                        if (i > 0)
                            order = order.ThenBy(newExpression.Members[i].Name);
                        else
                            order = query.OrderBy(newExpression.Members[i].Name);
                    }
                }
            }
            //单个字段排序
            else
            {
                if (orderTypes.FirstOrDefault() == OrderType.Descending)
                    order = query.OrderByDescending(orderField);
                else
                    order = query.OrderBy(orderField);
            }
            //分页
            query = order.Skip(pageSize * (pageIndex - 1)).Take(pageSize);
            return (query.ToList(), total);
        }

        /// <summary>
        /// 根据条件分页查询
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <param name="predicate">条件</param>
        /// <param name="orderField">排序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>
        /// <param name="orderTypes">排序类型，默认正序排序</param>
        /// <returns>返回集合和总记录数</returns>
        public (IEnumerable<S> list, long total) FindList<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, int pageSize, int pageIndex, params OrderType[] orderTypes) where T : class
        {
            IOrderedQueryable<T> order = null;
            var query = this.DbContext.Set<T>().Where(predicate);
            var total = query.Count();
            //多个字段排序
            if (orderField.Body is NewExpression newExpression)
            {
                for (var i = 0; i < newExpression.Members.Count; i++)
                {
                    //指定排序类型
                    if (i <= orderTypes.Length - 1)
                    {
                        if (orderTypes[i] == OrderType.Descending)
                        {
                            if (i > 0)
                                order = order.ThenByDescending(newExpression.Members[i].Name);
                            else
                                order = query.OrderByDescending(newExpression.Members[i].Name);
                        }
                        else
                        {
                            if (i > 0)
                                order = order.ThenBy(newExpression.Members[i].Name);
                            else
                                order = query.OrderBy(newExpression.Members[i].Name);
                        }
                    }
                    else
                    {
                        if (i > 0)
                            order = order.ThenBy(newExpression.Members[i].Name);
                        else
                            order = query.OrderBy(newExpression.Members[i].Name);
                    }
                }
            }
            //单个字段排序
            else
            {
                if (orderTypes.FirstOrDefault() == OrderType.Descending)
                    order = query.OrderByDescending(orderField);
                else
                    order = query.OrderBy(orderField);
            }
            //分页
            query = order.Skip(pageSize * (pageIndex - 1)).Take(pageSize);
            return (query.Select(selector).ToList(), total);
        }

        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="isAscending">是否升序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>        
        /// <returns>返回集合和总记录数</returns>
        public (List<T> list, long total) FindList<T>(string sql, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            return this.FindList<T>(sql, null, orderField, isAscending, pageSize, pageIndex);
        }

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
        public (List<T> list, long total) FindList<T>(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            if (!orderField.IsNullOrEmpty())
            {
                if (orderField.Contains(@"(/\*(?:|)*?\*/)|(\b(ASC|DESC)\b)", RegexOptions.IgnoreCase))
                    orderField = $"ORDER BY {orderField}";
                else
                    orderField = $"ORDER BY {orderField} {(isAscending ? "ASC" : "DESC")}";
            }
            var type = typeof(T);
            if (!type.Name.Contains("Dictionary`2") && type.IsClass && type.Name != "Object" && type.Name != "String")
            {
                var query = this.DbContext.SqlQueryMultiple<dynamic>($"SELECT COUNT(1) AS Total FROM ({sql}) AS T;SELECT * FROM ({sql}) AS X {orderField} LIMIT {pageSize} OFFSET {(pageSize * (pageIndex - 1))};", parameter);
                return (query.LastOrDefault().Select(o => (o as IDictionary<string, object>).ToEntity<T>()).ToList(), Convert.ToInt64(query.FirstOrDefault().FirstOrDefault().Total ?? 0));
            }
            else
            {
                var query = this.DbContext.SqlQueryMultiple<T>($"SELECT COUNT(1) AS Total FROM ({sql}) AS T;SELECT * FROM ({sql}) AS X {orderField} LIMIT {pageSize} OFFSET {(pageSize * (pageIndex - 1))};", parameter);
                return (query.LastOrDefault(), Convert.ToInt64((query.FirstOrDefault().FirstOrDefault() as IDictionary<string, object>)?["Total"] ?? 0));
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
        public (List<T> list, long total) FindListByWith<T>(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            if (!orderField.IsNullOrEmpty())
            {
                if (orderField.Contains(@"(/\*(?:|)*?\*/)|(\b(ASC|DESC)\b)", RegexOptions.IgnoreCase))
                    orderField = $"ORDER BY {orderField}";
                else
                    orderField = $"ORDER BY {orderField} {(isAscending ? "ASC" : "DESC")}";
            }
            var type = typeof(T);
            if (!type.Name.Contains("Dictionary`2") && type.IsClass && type.Name != "Object" && type.Name != "String")
            {
                var query = this.DbContext.SqlQueryMultiple<dynamic>($"{sql} SELECT COUNT(1) AS Total FROM T;{sql} SELECT * FROM T {orderField} LIMIT {pageSize} OFFSET {(pageSize * (pageIndex - 1))};", parameter);
                return (query.LastOrDefault().Select(o => (o as IDictionary<string, object>).ToEntity<T>()).ToList(), Convert.ToInt64(query.FirstOrDefault().FirstOrDefault().Total ?? 0));
            }
            else
            {
                var query = this.DbContext.SqlQueryMultiple<T>($"{sql} SELECT COUNT(1) AS Total FROM T;{sql} SELECT * FROM T {orderField} LIMIT {pageSize} OFFSET {(pageSize * (pageIndex - 1))};", parameter);
                return (query.LastOrDefault(), Convert.ToInt64((query.FirstOrDefault().FirstOrDefault() as IDictionary<string, object>)?["Total"] ?? 0));
            }
        }
        #endregion

        #region Async
        /// <summary>
        /// 查询全部
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <returns>返回集合</returns>
        public async Task<IEnumerable<T>> FindListAsync<T>() where T : class
        {
            return await this.DbContext.Set<T>().ToListAsync();
        }

        /// <summary>
        /// 查询指定列
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <returns>返回集合</returns>
        public async Task<IEnumerable<S>> FindListAsync<T, S>(Expression<Func<T, S>> selector) where T : class
        {
            return await this.DbContext.Set<T>().Select(selector).ToListAsync();
        }

        /// <summary>
        /// 查询全部并排序
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <param name="orderField">排序字段</param>
        /// <param name="orderTypes">排序类型，默认正序排序</param>
        /// <returns>返回集合</returns>
        public async Task<IEnumerable<T>> FindListAsync<T>(Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = this.DbContext.Set<T>();
            //多个字段排序
            if (orderField.Body is NewExpression newExpression)
            {
                IOrderedQueryable<T> order = null;
                for (var i = 0; i < newExpression.Members.Count; i++)
                {
                    //指定排序类型
                    if (i <= orderTypes.Length - 1)
                    {
                        if (orderTypes[i] == OrderType.Descending)
                        {
                            if (i > 0)
                                order = order.ThenByDescending(newExpression.Members[i].Name);
                            else
                                order = query.OrderByDescending(newExpression.Members[i].Name);
                        }
                        else
                        {
                            if (i > 0)
                                order = order.ThenBy(newExpression.Members[i].Name);
                            else
                                order = query.OrderBy(newExpression.Members[i].Name);
                        }
                    }
                    else
                    {
                        if (i > 0)
                            order = order.ThenBy(newExpression.Members[i].Name);
                        else
                            order = query.OrderBy(newExpression.Members[i].Name);
                    }
                }
                return await order.ToListAsync();
            }
            //单个字段排序
            else
            {
                if (orderTypes.FirstOrDefault() == OrderType.Descending)
                    return await query.OrderByDescending(orderField).ToListAsync();
                else
                    return await query.OrderBy(orderField).ToListAsync();
            }
        }

        /// <summary>
        /// 查询指定列并排序
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="orderTypes">排序类型，默认正序排序</param>
        /// <returns>返回集合</returns>
        public async Task<IEnumerable<S>> FindListAsync<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = this.DbContext.Set<T>();
            //多个字段排序
            if (orderField.Body is NewExpression newExpression)
            {
                IOrderedQueryable<T> order = null;
                for (var i = 0; i < newExpression.Members.Count; i++)
                {
                    //指定排序类型
                    if (i <= orderTypes.Length - 1)
                    {
                        if (orderTypes[i] == OrderType.Descending)
                        {
                            if (i > 0)
                                order = order.ThenByDescending(newExpression.Members[i].Name);
                            else
                                order = query.OrderByDescending(newExpression.Members[i].Name);
                        }
                        else
                        {
                            if (i > 0)
                                order = order.ThenBy(newExpression.Members[i].Name);
                            else
                                order = query.OrderBy(newExpression.Members[i].Name);
                        }
                    }
                    else
                    {
                        if (i > 0)
                            order = order.ThenBy(newExpression.Members[i].Name);
                        else
                            order = query.OrderBy(newExpression.Members[i].Name);
                    }
                }
                return await order.Select(selector).ToListAsync();
            }
            //单个字段排序
            else
            {
                if (orderTypes.FirstOrDefault() == OrderType.Descending)
                    return await query.OrderByDescending(orderField).Select(selector).ToListAsync();
                else
                    return await query.OrderBy(orderField).Select(selector).ToListAsync();
            }
        }

        /// <summary>
        /// 根据条件查询
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <param name="predicate">条件</param>
        /// <returns>返回集合</returns>
        public async Task<IEnumerable<T>> FindListAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return await this.DbContext.Set<T>().Where(predicate).ToListAsync();
        }

        /// <summary>
        /// 根据条件查询并排序
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <param name="predicate">条件</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="orderTypes">排序类型，默认正序排序</param>
        /// <returns>返回集合</returns>
        public async Task<IEnumerable<T>> FindListAsync<T>(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = this.DbContext.Set<T>().Where(predicate);
            //多个字段排序
            if (orderField.Body is NewExpression newExpression)
            {
                IOrderedQueryable<T> order = null;
                for (var i = 0; i < newExpression.Members.Count; i++)
                {
                    //指定排序类型
                    if (i <= orderTypes.Length - 1)
                    {
                        if (orderTypes[i] == OrderType.Descending)
                        {
                            if (i > 0)
                                order = order.ThenByDescending(newExpression.Members[i].Name);
                            else
                                order = query.OrderByDescending(newExpression.Members[i].Name);
                        }
                        else
                        {
                            if (i > 0)
                                order = order.ThenBy(newExpression.Members[i].Name);
                            else
                                order = query.OrderBy(newExpression.Members[i].Name);
                        }
                    }
                    else
                    {
                        if (i > 0)
                            order = order.ThenBy(newExpression.Members[i].Name);
                        else
                            order = query.OrderBy(newExpression.Members[i].Name);
                    }
                }
                return await order.ToListAsync();
            }
            //单个字段排序
            else
            {
                if (orderTypes.FirstOrDefault() == OrderType.Descending)
                    return await query.OrderByDescending(orderField).ToListAsync();
                else
                    return await query.OrderBy(orderField).ToListAsync();
            }
        }

        /// <summary>
        /// 根据条件查询指定列
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <param name="predicate">条件</param>
        /// <returns>返回集合</returns>
        public async Task<IEnumerable<S>> FindListAsync<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate) where T : class
        {
            return await this.DbContext.Set<T>().Where(predicate).Select(selector).ToListAsync();
        }

        /// <summary>
        /// 根据条件查询指定列并排序
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <param name="predicate">条件</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="orderTypes">排序类型，默认正序排序</param>
        /// <returns>返回集合</returns>
        public async Task<IEnumerable<S>> FindListAsync<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = this.DbContext.Set<T>().Where(predicate);
            //多个字段排序
            if (orderField.Body is NewExpression newExpression)
            {
                IOrderedQueryable<T> order = null;
                for (var i = 0; i < newExpression.Members.Count; i++)
                {
                    //指定排序类型
                    if (i <= orderTypes.Length - 1)
                    {
                        if (orderTypes[i] == OrderType.Descending)
                        {
                            if (i > 0)
                                order = order.ThenByDescending(newExpression.Members[i].Name);
                            else
                                order = query.OrderByDescending(newExpression.Members[i].Name);
                        }
                        else
                        {
                            if (i > 0)
                                order = order.ThenBy(newExpression.Members[i].Name);
                            else
                                order = query.OrderBy(newExpression.Members[i].Name);
                        }
                    }
                    else
                    {
                        if (i > 0)
                            order = order.ThenBy(newExpression.Members[i].Name);
                        else
                            order = query.OrderBy(newExpression.Members[i].Name);
                    }
                }
                return await order.Select(selector).ToListAsync();
            }
            //单个字段排序
            else
            {
                if (orderTypes.FirstOrDefault() == OrderType.Descending)
                    return await query.OrderByDescending(orderField).Select(selector).ToListAsync();
                else
                    return await query.OrderBy(orderField).Select(selector).ToListAsync();
            }
        }

        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <returns>返回集合</returns>
        public async Task<IEnumerable<T>> FindListAsync<T>(string sql)
        {
            return await this.FindListAsync<T>(sql, null);
        }

        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回集合</returns>
        public async Task<IEnumerable<T>> FindListAsync<T>(string sql, params DbParameter[] parameter)
        {
            return await this.DbContext.SqlQueryAsync<T>(sql, parameter);
        }

        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回集合</returns>
        public async Task<IEnumerable<T>> FindListAsync<T>(string sql, params object[] parameter) where T : class
        {
            return await this.DbContext.Set<T>().FromSqlRaw(sql, parameter).ToListAsync();
        }

        /// <summary>
        /// 根据条件分页查询
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="predicate">条件</param>
        /// <param name="orderField">排序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>
        /// <param name="orderTypes">排序类型，默认正序排序</param>
        /// <returns>返回集合和总记录数</returns>
        public async Task<(IEnumerable<T> list, long total)> FindListAsync<T>(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, int pageSize, int pageIndex, params OrderType[] orderTypes) where T : class
        {
            IOrderedQueryable<T> order = null;
            var query = this.DbContext.Set<T>().Where(predicate);
            var total = await query.CountAsync();
            //多个字段排序
            if (orderField.Body is NewExpression newExpression)
            {
                for (var i = 0; i < newExpression.Members.Count; i++)
                {
                    //指定排序类型
                    if (i <= orderTypes.Length - 1)
                    {
                        if (orderTypes[i] == OrderType.Descending)
                        {
                            if (i > 0)
                                order = order.ThenByDescending(newExpression.Members[i].Name);
                            else
                                order = query.OrderByDescending(newExpression.Members[i].Name);
                        }
                        else
                        {
                            if (i > 0)
                                order = order.ThenBy(newExpression.Members[i].Name);
                            else
                                order = query.OrderBy(newExpression.Members[i].Name);
                        }
                    }
                    else
                    {
                        if (i > 0)
                            order = order.ThenBy(newExpression.Members[i].Name);
                        else
                            order = query.OrderBy(newExpression.Members[i].Name);
                    }
                }
            }
            //单个字段排序
            else
            {
                if (orderTypes.FirstOrDefault() == OrderType.Descending)
                    order = query.OrderByDescending(orderField);
                else
                    order = query.OrderBy(orderField);
            }
            //分页
            query = order.Skip(pageSize * (pageIndex - 1)).Take(pageSize);
            return (await query.ToListAsync(), total);
        }

        /// <summary>
        /// 根据条件分页查询
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <param name="predicate">条件</param>
        /// <param name="orderField">排序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>
        /// <param name="orderTypes">排序类型，默认正序排序</param>
        /// <returns>返回集合和总记录数</returns>
        public async Task<(IEnumerable<S> list, long total)> FindListAsync<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, int pageSize, int pageIndex, params OrderType[] orderTypes) where T : class
        {
            IOrderedQueryable<T> order = null;
            var query = this.DbContext.Set<T>().Where(predicate);
            var total = await query.CountAsync();
            //多个字段排序
            if (orderField.Body is NewExpression newExpression)
            {
                for (var i = 0; i < newExpression.Members.Count; i++)
                {
                    //指定排序类型
                    if (i <= orderTypes.Length - 1)
                    {
                        if (orderTypes[i] == OrderType.Descending)
                        {
                            if (i > 0)
                                order = order.ThenByDescending(newExpression.Members[i].Name);
                            else
                                order = query.OrderByDescending(newExpression.Members[i].Name);
                        }
                        else
                        {
                            if (i > 0)
                                order = order.ThenBy(newExpression.Members[i].Name);
                            else
                                order = query.OrderBy(newExpression.Members[i].Name);
                        }
                    }
                    else
                    {
                        if (i > 0)
                            order = order.ThenBy(newExpression.Members[i].Name);
                        else
                            order = query.OrderBy(newExpression.Members[i].Name);
                    }
                }
            }
            //单个字段排序
            else
            {
                if (orderTypes.FirstOrDefault() == OrderType.Descending)
                    order = query.OrderByDescending(orderField);
                else
                    order = query.OrderBy(orderField);
            }
            //分页
            query = order.Skip(pageSize * (pageIndex - 1)).Take(pageSize);
            return (await query.Select(selector).ToListAsync(), total);
        }

        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="isAscending">是否升序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>        
        /// <returns>返回集合和总记录数</returns>
        public async Task<(List<T> list, long total)> FindListAsync<T>(string sql, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            return await this.FindListAsync<T>(sql, null, orderField, isAscending, pageSize, pageIndex);
        }

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
        public async Task<(List<T> list, long total)> FindListAsync<T>(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            if (!orderField.IsNullOrEmpty())
            {
                if (orderField.Contains(@"(/\*(?:|)*?\*/)|(\b(ASC|DESC)\b)", RegexOptions.IgnoreCase))
                    orderField = $"ORDER BY {orderField}";
                else
                    orderField = $"ORDER BY {orderField} {(isAscending ? "ASC" : "DESC")}";
            }
            var type = typeof(T);
            if (!type.Name.Contains("Dictionary`2") && type.IsClass && type.Name != "Object" && type.Name != "String")
            {
                var query = await this.DbContext.SqlQueryMultipleAsync<dynamic>($"SELECT COUNT(1) AS Total FROM ({sql}) AS T;SELECT * FROM ({sql}) AS X {orderField} LIMIT {pageSize} OFFSET {(pageSize * (pageIndex - 1))};", parameter);
                return (query.LastOrDefault().Select(o => (o as IDictionary<string, object>).ToEntity<T>()).ToList(), Convert.ToInt64(query.FirstOrDefault().FirstOrDefault().Total ?? 0));
            }
            else
            {
                var query = await this.DbContext.SqlQueryMultipleAsync<T>($"SELECT COUNT(1) AS Total FROM ({sql}) AS T;SELECT * FROM ({sql}) AS X {orderField} LIMIT {pageSize} OFFSET {(pageSize * (pageIndex - 1))};", parameter);
                return (query.LastOrDefault(), Convert.ToInt64((query.FirstOrDefault().FirstOrDefault() as IDictionary<string, object>)?["Total"] ?? 0));
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
        public async Task<(List<T> list, long total)> FindListByWithAsync<T>(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            if (!orderField.IsNullOrEmpty())
            {
                if (orderField.Contains(@"(/\*(?:|)*?\*/)|(\b(ASC|DESC)\b)", RegexOptions.IgnoreCase))
                    orderField = $"ORDER BY {orderField}";
                else
                    orderField = $"ORDER BY {orderField} {(isAscending ? "ASC" : "DESC")}";
            }
            var type = typeof(T);
            if (!type.Name.Contains("Dictionary`2") && type.IsClass && type.Name != "Object" && type.Name != "String")
            {
                var query = await this.DbContext.SqlQueryMultipleAsync<dynamic>($"{sql} SELECT COUNT(1) AS Total FROM T;{sql} SELECT * FROM T {orderField} LIMIT {pageSize} OFFSET {(pageSize * (pageIndex - 1))};", parameter);
                return (query.LastOrDefault().Select(o => (o as IDictionary<string, object>).ToEntity<T>()).ToList(), Convert.ToInt64(query.FirstOrDefault().FirstOrDefault().Total ?? 0));
            }
            else
            {
                var query = await this.DbContext.SqlQueryMultipleAsync<T>($"{sql} SELECT COUNT(1) AS Total FROM T;{sql} SELECT * FROM T {orderField} LIMIT {pageSize} OFFSET {(pageSize * (pageIndex - 1))};", parameter);
                return (query.LastOrDefault(), Convert.ToInt64((query.FirstOrDefault().FirstOrDefault() as IDictionary<string, object>)?["Total"] ?? 0));
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
        /// <returns>返回DataTable</returns>
        public DataTable FindTable(string sql)
        {
            return this.FindTable(sql, null);
        }

        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回DataTable</returns>
        public DataTable FindTable(string sql, params DbParameter[] parameter)
        {
            return this.DbContext.SqlDataTable(sql, parameter);
        }

        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="isAscending">是否升序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>        
        /// <returns>返回DataTable和总记录数</returns>
        public (DataTable table, long total) FindTable(string sql, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            return this.FindTable(sql, null, orderField, isAscending, pageSize, pageIndex);
        }

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
        public (DataTable table, long total) FindTable(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            if (!orderField.IsNullOrEmpty())
            {
                if (orderField.Contains(@"(/\*(?:|)*?\*/)|(\b(ASC|DESC)\b)", RegexOptions.IgnoreCase))
                    orderField = $"ORDER BY {orderField}";
                else
                    orderField = $"ORDER BY {orderField} {(isAscending ? "ASC" : "DESC")}";
            }
            var ds = this.DbContext.SqlDataSet($"SELECT COUNT(1) AS Total FROM ({sql}) AS T;SELECT * FROM ({sql}) AS X {orderField} LIMIT {pageSize} OFFSET {(pageSize * (pageIndex - 1))};", parameter);
            return (ds.Tables[1], Convert.ToInt64(ds.Tables[0].Rows[0]["Total"]));
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
        public (DataTable table, long total) FindTableByWith(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            if (!orderField.IsNullOrEmpty())
            {
                if (orderField.Contains(@"(/\*(?:|)*?\*/)|(\b(ASC|DESC)\b)", RegexOptions.IgnoreCase))
                    orderField = $"ORDER BY {orderField}";
                else
                    orderField = $"ORDER BY {orderField} {(isAscending ? "ASC" : "DESC")}";
            }
            var ds = this.DbContext.SqlDataSet($"{sql} SELECT COUNT(1) AS Total FROM T;{sql} SELECT * FROM T {orderField} LIMIT {pageSize} OFFSET {(pageSize * (pageIndex - 1))};", parameter);
            return (ds.Tables[1], Convert.ToInt64(ds.Tables[0].Rows[0]["Total"]));
        }
        #endregion

        #region Async
        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <returns>返回DataTable</returns>
        public async Task<DataTable> FindTableAsync(string sql)
        {
            return await this.FindTableAsync(sql, null);
        }

        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回DataTable</returns>
        public async Task<DataTable> FindTableAsync(string sql, params DbParameter[] parameter)
        {
            return await this.DbContext.SqlDataTableAsync(sql, parameter);
        }

        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="isAscending">是否升序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>
        /// <returns>返回DataTable和总记录数</returns>
        public async Task<(DataTable table, long total)> FindTableAsync(string sql, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            return await this.FindTableAsync(sql, null, orderField, isAscending, pageSize, pageIndex);
        }

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
        public async Task<(DataTable table, long total)> FindTableAsync(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            if (!orderField.IsNullOrEmpty())
            {
                if (orderField.Contains(@"(/\*(?:|)*?\*/)|(\b(ASC|DESC)\b)", RegexOptions.IgnoreCase))
                    orderField = $"ORDER BY {orderField}";
                else
                    orderField = $"ORDER BY {orderField} {(isAscending ? "ASC" : "DESC")}";
            }
            var ds = await this.DbContext.SqlDataSetAsync($"SELECT COUNT(1) AS Total FROM ({sql}) AS T;SELECT * FROM ({sql}) AS X {orderField} LIMIT {pageSize} OFFSET {(pageSize * (pageIndex - 1))};", parameter);
            return (ds.Tables[1], Convert.ToInt64(ds.Tables[0].Rows[0]["Total"]));
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
        public async Task<(DataTable table, long total)> FindTableByWithAsync(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            if (!orderField.IsNullOrEmpty())
            {
                if (orderField.Contains(@"(/\*(?:|)*?\*/)|(\b(ASC|DESC)\b)", RegexOptions.IgnoreCase))
                    orderField = $"ORDER BY {orderField}";
                else
                    orderField = $"ORDER BY {orderField} {(isAscending ? "ASC" : "DESC")}";
            }
            var ds = await this.DbContext.SqlDataSetAsync($"{sql} SELECT COUNT(1) AS Total FROM T;{sql} SELECT * FROM T {orderField} LIMIT {pageSize} OFFSET {(pageSize * (pageIndex - 1))};", parameter);
            return (ds.Tables[1], Convert.ToInt64(ds.Tables[0].Rows[0]["Total"]));
        }
        #endregion
        #endregion

        #region FindMultiple
        #region Sync
        /// <summary>
        /// 根据sql语句查询返回多个结果集
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <returns>返回查询结果集</returns>
        public List<List<T>> FindMultiple<T>(string sql)
        {
            return this.FindMultiple<T>(sql, null);
        }

        /// <summary>
        /// 根据sql语句查询返回多个结果集
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回查询结果集</returns>
        public List<List<T>> FindMultiple<T>(string sql, params DbParameter[] parameter)
        {
            return this.DbContext.SqlQueryMultiple<T>(sql, parameter);
        }
        #endregion

        #region Async
        /// <summary>
        /// 根据sql语句查询返回多个结果集
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <returns>返回查询结果集</returns>
        public async Task<List<List<T>>> FindMultipleAsync<T>(string sql)
        {
            return await this.FindMultipleAsync<T>(sql, null);
        }

        /// <summary>
        /// 根据sql语句查询返回多个结果集
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回查询结果集</returns>
        public async Task<List<List<T>>> FindMultipleAsync<T>(string sql, params DbParameter[] parameter)
        {
            return await this.DbContext.SqlQueryMultipleAsync<T>(sql, parameter);
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
