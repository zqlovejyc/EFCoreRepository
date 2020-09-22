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
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
/****************************
* [Author] 张强
* [Date] 2020-09-17
* [Describe] 数据操作仓储抽象基类
* **************************/
namespace EFCoreRepository
{
    /// <summary>
    /// 数据操作仓储抽象基类
    /// </summary>
    public abstract class BaseRepository
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
        /// DbContext
        /// </summary>
        public virtual DbContext DbContext { get; set; }

        /// <summary>
        /// 分页计数语法，默认COUNT(*)
        /// </summary>
        public virtual string CountSyntax { get; set; } = "COUNT(*)";

        /// <summary>
        /// 超时时长，默认240s
        /// </summary>
        public virtual int CommandTimeout
        {
            get
            {
                return commandTimeout;
            }
            set
            {
                commandTimeout = value;
                DbContext.Database.SetCommandTimeout(commandTimeout);
            }
        }

        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public virtual string ConnectionString
        {
            get
            {
                return connectionString ?? DbContext.Database.GetDbConnection().ConnectionString;
            }
            set
            {
                connectionString = value;
                DbContext.Database.GetDbConnection().ConnectionString = connectionString;
            }
        }

        /// <summary>
        /// 事务对象
        /// </summary>
        public virtual DbTransaction Transaction
        {
            get
            {
                return transaction ?? DbContext.Database.CurrentTransaction?.GetDbTransaction();
            }
            set
            {
                transaction = value;
                DbContext.Database.UseTransaction(transaction);
            }
        }
        #endregion

        #region ExecuteBySql
        #region Sync
        /// <summary>
        /// 执行sql语句
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <returns>返回受影响行数</returns>
        public virtual int ExecuteBySql(string sql)
        {
            return DbContext.Database.ExecuteSqlRaw(sql);
        }

        /// <summary>
        /// 执行sql语句
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回受影响行数</returns>
        public virtual int ExecuteBySql(string sql, params object[] parameter)
        {
            return DbContext.Database.ExecuteSqlRaw(sql, parameter);
        }

        /// <summary>
        /// 执行sql存储过程
        /// </summary>
        /// <param name="procName">存储过程名称</param>
        ///  <param name="parameter">对应参数</param>
        /// <returns>返回受影响行数</returns>
        public virtual int ExecuteByProc(string procName, params DbParameter[] parameter)
        {
            return DbContext.ExecuteProc(procName, parameter);
        }

        /// <summary>
        /// 执行sql存储过程查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="procName"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public virtual IEnumerable<T> ExecuteByProc<T>(string procName, params DbParameter[] parameter)
        {
            return DbContext.ExecuteProc<T>(procName, parameter);
        }
        #endregion

        #region Async
        /// <summary>
        /// 执行sql语句
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> ExecuteBySqlAsync(string sql)
        {
            return await DbContext.Database.ExecuteSqlRawAsync(sql);
        }

        /// <summary>
        /// 执行sql语句
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> ExecuteBySqlAsync(string sql, params object[] parameter)
        {
            return await DbContext.Database.ExecuteSqlRawAsync(sql, parameter);
        }

        /// <summary>
        /// 执行sql存储过程
        /// </summary>
        /// <param name="procName">存储过程名称</param>
        ///  <param name="parameter">对应参数</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> ExecuteByProcAsync(string procName, params DbParameter[] parameter)
        {
            return await DbContext.ExecuteProcAsync(procName, parameter);
        }

        /// <summary>
        /// 执行sql存储过程查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="procName"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<T>> ExecuteByProcAsync<T>(string procName, params DbParameter[] parameter)
        {
            return await DbContext.ExecuteProcAsync<T>(procName, parameter);
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
        public virtual int Insert<T>(T entity) where T : class
        {
            DbContext.Set<T>().Add(entity);
            return DbContext.SaveChanges();
        }

        /// <summary>
        /// 插入多个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entities">要插入的实体集合</param>
        /// <returns>返回受影响行数</returns>
        public virtual int Insert<T>(IEnumerable<T> entities) where T : class
        {
            DbContext.Set<T>().AddRange(entities);
            return DbContext.SaveChanges();
        }
        #endregion

        #region Async
        /// <summary>
        ///  插入单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entity">要插入的实体</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> InsertAsync<T>(T entity) where T : class
        {
            await DbContext.Set<T>().AddAsync(entity);
            return await DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 插入多个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entities">要插入的实体集合</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> InsertAsync<T>(IEnumerable<T> entities) where T : class
        {
            await DbContext.Set<T>().AddRangeAsync(entities);
            return await DbContext.SaveChangesAsync();
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
        public virtual int Delete<T>() where T : class
        {
            var entities = FindList<T>();
            return Delete(entities);
        }

        /// <summary>
        /// 删除单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entity">要删除的实体</param>
        /// <returns>返回受影响行数</returns>
        public virtual int Delete<T>(T entity) where T : class
        {
            DbContext.Set<T>().Remove(entity);
            return DbContext.SaveChanges();
        }

        /// <summary>
        /// 删除多个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entities">要删除的实体集合</param>
        /// <returns>返回受影响行数</returns>
        public virtual int Delete<T>(IEnumerable<T> entities) where T : class
        {
            DbContext.Set<T>().RemoveRange(entities);
            return DbContext.SaveChanges();
        }

        /// <summary>
        /// 根据条件删除实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="predicate">条件</param>
        /// <returns>返回受影响行数</returns>
        public virtual int Delete<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            var entities = FindList(predicate);
            return Delete(entities);
        }

        /// <summary>
        /// 根据主键删除实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="keyValues">主键值</param>
        /// <returns>返回受影响行数</returns>
        public virtual int Delete<T>(params object[] keyValues) where T : class
        {
            var entity = FindEntity<T>(keyValues);
            if (entity != null)
            {
                return Delete(entity);
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
        public virtual async Task<int> DeleteAsync<T>() where T : class
        {
            var entities = await FindListAsync<T>();
            return await DeleteAsync(entities);
        }

        /// <summary>
        /// 删除单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entity">要删除的实体</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> DeleteAsync<T>(T entity) where T : class
        {
            DbContext.Set<T>().Remove(entity);
            return await DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 删除多个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entities">要删除的实体集合</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> DeleteAsync<T>(IEnumerable<T> entities) where T : class
        {
            DbContext.Set<T>().RemoveRange(entities);
            return await DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 根据条件删除实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="predicate">条件</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> DeleteAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            var entities = await FindListAsync(predicate);
            return await DeleteAsync(entities);
        }

        /// <summary>
        /// 根据主键删除实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="keyValues">主键值</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> DeleteAsync<T>(params object[] keyValues) where T : class
        {
            var entity = await FindEntityAsync<T>(keyValues);
            if (entity != null)
            {
                return await DeleteAsync(entity);
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
        public virtual int Update<T>(T entity) where T : class
        {
            DbContext.Set<T>().Attach(entity);
            var entry = DbContext.Entry(entity);
            var props = entity.GetType().GetProperties();
            foreach (var prop in props)
            {
                //非null且非PrimaryKey
                if (prop.GetValue(entity, null) != null && !entry.Property(prop.Name).Metadata.IsPrimaryKey())
                {
                    entry.Property(prop.Name).IsModified = true;
                }
            }
            return DbContext.SaveChanges();
        }

        /// <summary>
        /// 更新多个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entities">要更新的实体集合</param>
        /// <returns>返回受影响行数</returns>
        public virtual int Update<T>(IEnumerable<T> entities) where T : class
        {
            foreach (var entity in entities)
            {
                DbContext.Set<T>().Attach(entity);
                var entry = DbContext.Entry(entity);
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
            return DbContext.SaveChanges();
        }

        /// <summary>
        /// 根据条件更新实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="predicate">条件</param>
        /// <param name="entity">要更新的实体</param>
        /// <returns>返回受影响行数</returns>
        public virtual int Update<T>(Expression<Func<T, bool>> predicate, T entity) where T : class
        {
            var entities = new List<T>();
            var instances = FindList(predicate);
            //设置所有状态为未跟踪状态
            DbContext.ChangeTracker.Entries<T>().ToList().ForEach(o => o.State = EntityState.Detached);
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
            return Update<T>(entities);
        }
        #endregion

        #region Async
        /// <summary>
        /// 更新单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entity">要更新的实体</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> UpdateAsync<T>(T entity) where T : class
        {
            DbContext.Set<T>().Attach(entity);
            var entry = DbContext.Entry(entity);
            var props = entity.GetType().GetProperties();
            foreach (var prop in props)
            {
                //非null且非PrimaryKey
                if (prop.GetValue(entity, null) != null && !entry.Property(prop.Name).Metadata.IsPrimaryKey())
                {
                    entry.Property(prop.Name).IsModified = true;
                }
            }
            return await DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 更新多个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entities">要更新的实体集合</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> UpdateAsync<T>(IEnumerable<T> entities) where T : class
        {
            foreach (var entity in entities)
            {
                DbContext.Set<T>().Attach(entity);
                var entry = DbContext.Entry(entity);
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
            return await DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 根据条件更新实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="predicate">条件</param>
        /// <param name="entity">要更新的实体</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> UpdateAsync<T>(Expression<Func<T, bool>> predicate, T entity) where T : class
        {
            var entities = new List<T>();
            var instances = await FindListAsync(predicate);
            //设置所有状态为未跟踪状态
            DbContext.ChangeTracker.Entries<T>().ToList().ForEach(o => o.State = EntityState.Detached);
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
            return await UpdateAsync<T>(entities);
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
        public virtual object FindObject(string sql)
        {
            return FindObject(sql, null);
        }

        /// <summary>
        /// 查询单个对象
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回查询结果对象</returns>
        public virtual object FindObject(string sql, params DbParameter[] parameter)
        {
            return DbContext.SqlQuery<Dictionary<string, object>>(sql, parameter)?.FirstOrDefault()?.Select(o => o.Value).FirstOrDefault();
        }
        #endregion

        #region Async
        /// <summary>
        /// 查询单个对象
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <returns>返回查询结果对象</returns>
        public virtual async Task<object> FindObjectAsync(string sql)
        {
            return await FindObjectAsync(sql, null);
        }

        /// <summary>
        /// 查询单个对象
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回查询结果对象</returns>
        public virtual async Task<object> FindObjectAsync(string sql, params DbParameter[] parameter)
        {
            return (await DbContext.SqlQueryAsync<Dictionary<string, object>>(sql, parameter))?.FirstOrDefault()?.Select(o => o.Value).FirstOrDefault();
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
        public virtual T FindEntity<T>(params object[] keyValues) where T : class
        {
            return DbContext.Set<T>().Find(keyValues);
        }

        /// <summary>
        /// 根据sql语句查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <returns>返回实体</returns>
        public virtual T FindEntity<T>(string sql)
        {
            return FindEntity<T>(sql, null);
        }

        /// <summary>
        /// 根据sql语句查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回实体</returns>
        public virtual T FindEntity<T>(string sql, params DbParameter[] parameter)
        {
            var query = DbContext.SqlQuery<T>(sql, parameter);
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
        public virtual T FindEntity<T>(string sql, params object[] parameter) where T : class
        {
            return DbContext.Set<T>().FromSqlRaw(sql, parameter).FirstOrDefault();
        }

        /// <summary>
        /// 根据条件查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="predicate">条件</param>
        /// <returns>返回实体</returns>
        public virtual T FindEntity<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return DbContext.Set<T>().Where(predicate).FirstOrDefault();
        }

        /// <summary>
        /// 根据条件查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <param name="predicate">条件</param>
        /// <returns>返回实体</returns>
        public virtual S FindEntity<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate) where T : class
        {
            return DbContext.Set<T>().Where(predicate).Select(selector).FirstOrDefault();
        }
        #endregion

        #region Async
        /// <summary>
        /// 根据主键查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <param name="keyValues">主键值</param>
        /// <returns>返回实体</returns>
        public virtual async Task<T> FindEntityAsync<T>(params object[] keyValues) where T : class
        {
            return await DbContext.Set<T>().FindAsync(keyValues);
        }

        /// <summary>
        /// 根据sql语句查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <returns>返回实体</returns>
        public virtual async Task<T> FindEntityAsync<T>(string sql)
        {
            return await FindEntityAsync<T>(sql, null);
        }

        /// <summary>
        /// 根据sql语句查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回实体</returns>
        public virtual async Task<T> FindEntityAsync<T>(string sql, params DbParameter[] parameter)
        {
            var query = await DbContext.SqlQueryAsync<T>(sql, parameter);
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
        public virtual async Task<T> FindEntityAsync<T>(string sql, params object[] parameter) where T : class
        {
            return await DbContext.Set<T>().FromSqlRaw(sql, parameter).FirstOrDefaultAsync();
        }

        /// <summary>
        /// 根据条件查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <param name="predicate">条件</param>
        /// <returns>返回实体</returns>
        public virtual async Task<T> FindEntityAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return await DbContext.Set<T>().Where(predicate).FirstOrDefaultAsync();
        }

        /// <summary>
        /// 根据条件查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <param name="predicate">条件</param>
        /// <returns>返回实体</returns>
        public virtual async Task<S> FindEntityAsync<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate) where T : class
        {
            return await DbContext.Set<T>().Where(predicate).Select(selector).FirstOrDefaultAsync();
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
        public virtual IQueryable<T> IQueryable<T>() where T : class
        {
            return DbContext.Set<T>();
        }

        /// <summary>
        /// 查询指定列
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <returns>返回集合</returns>
        public virtual IQueryable<S> IQueryable<T, S>(Expression<Func<T, S>> selector) where T : class
        {
            return DbContext.Set<T>().Select(selector);
        }

        /// <summary>
        /// 根据条件查询
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <param name="predicate">条件</param>
        /// <returns>返回集合</returns>
        public virtual IQueryable<T> IQueryable<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return DbContext.Set<T>().Where(predicate);
        }

        /// <summary>
        /// 根据条件查询并排序
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <param name="predicate">条件</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="orderTypes">排序类型，默认正序排序</param>
        /// <returns>返回集合</returns>
        public virtual IQueryable<T> IQueryable<T>(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = DbContext.Set<T>().Where(predicate);
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
        public virtual IQueryable<S> IQueryable<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate) where T : class
        {
            return DbContext.Set<T>().Where(predicate).Select(selector);
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
        public virtual IQueryable<S> IQueryable<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = DbContext.Set<T>().Where(predicate);
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
        public virtual async Task<IQueryable<T>> IQueryableAsync<T>() where T : class
        {
            return await Task.FromResult(DbContext.Set<T>());
        }

        /// <summary>
        /// 查询指定列
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <returns>返回集合</returns>
        public virtual async Task<IQueryable<S>> IQueryableAsync<T, S>(Expression<Func<T, S>> selector) where T : class
        {
            return await Task.FromResult(DbContext.Set<T>().Select(selector));
        }

        /// <summary>
        /// 根据条件查询
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <param name="predicate">条件</param>
        /// <returns>返回集合</returns>
        public virtual async Task<IQueryable<T>> IQueryableAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return await Task.FromResult(DbContext.Set<T>().Where(predicate));
        }

        /// <summary>
        /// 根据条件查询并排序
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <param name="predicate">条件</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="orderTypes">排序类型，默认正序排序</param>
        /// <returns>返回集合</returns>
        public virtual async Task<IQueryable<T>> IQueryableAsync<T>(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = DbContext.Set<T>().Where(predicate);
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
        public virtual async Task<IQueryable<S>> IQueryableAsync<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate) where T : class
        {
            return await Task.FromResult(DbContext.Set<T>().Where(predicate).Select(selector));
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
        public virtual async Task<IQueryable<S>> IQueryableAsync<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = DbContext.Set<T>().Where(predicate);
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
        public virtual IEnumerable<T> FindList<T>() where T : class
        {
            return DbContext.Set<T>().ToList();
        }

        /// <summary>
        /// 查询指定列
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <returns>返回集合</returns>
        public virtual IEnumerable<S> FindList<T, S>(Expression<Func<T, S>> selector) where T : class
        {
            return DbContext.Set<T>().Select(selector).ToList();
        }

        /// <summary>
        /// 查询全部并排序
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <param name="orderField">排序字段</param>
        /// <param name="orderTypes">排序类型，默认正序排序</param>
        /// <returns>返回集合</returns>
        public virtual IEnumerable<T> FindList<T>(Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = DbContext.Set<T>();
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
        public virtual IEnumerable<S> FindList<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = DbContext.Set<T>();
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
        public virtual IEnumerable<T> FindList<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return DbContext.Set<T>().Where(predicate).ToList();
        }

        /// <summary>
        /// 根据条件查询并排序
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <param name="predicate">条件</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="orderTypes">排序类型，默认正序排序</param>
        /// <returns>返回集合</returns>
        public virtual IEnumerable<T> FindList<T>(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = DbContext.Set<T>().Where(predicate);
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
        public virtual IEnumerable<S> FindList<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate) where T : class
        {
            return DbContext.Set<T>().Where(predicate).Select(selector).ToList();
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
        public virtual IEnumerable<S> FindList<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = DbContext.Set<T>().Where(predicate);
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
        public virtual IEnumerable<T> FindList<T>(string sql)
        {
            return FindList<T>(sql, null);
        }

        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回集合</returns>
        public virtual IEnumerable<T> FindList<T>(string sql, params DbParameter[] parameter)
        {
            return DbContext.SqlQuery<T>(sql, parameter);
        }

        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回集合</returns>
        public virtual IEnumerable<T> FindList<T>(string sql, params object[] parameter) where T : class
        {
            return DbContext.Set<T>().FromSqlRaw(sql, parameter).ToList();
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
        public virtual (IEnumerable<T> list, long total) FindList<T>(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, int pageSize, int pageIndex, params OrderType[] orderTypes) where T : class
        {
            IOrderedQueryable<T> order = null;
            var query = DbContext.Set<T>().Where(predicate);
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
        public virtual (IEnumerable<S> list, long total) FindList<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, int pageSize, int pageIndex, params OrderType[] orderTypes) where T : class
        {
            IOrderedQueryable<T> order = null;
            var query = DbContext.Set<T>().Where(predicate);
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
        public virtual (List<T> list, long total) FindList<T>(string sql, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            return FindList<T>(sql, null, orderField, isAscending, pageSize, pageIndex);
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
        public abstract (List<T> list, long total) FindList<T>(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex);

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
        public abstract (List<T> list, long total) FindListByWith<T>(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex);
        #endregion

        #region Async
        /// <summary>
        /// 查询全部
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <returns>返回集合</returns>
        public virtual async Task<IEnumerable<T>> FindListAsync<T>() where T : class
        {
            return await DbContext.Set<T>().ToListAsync();
        }

        /// <summary>
        /// 查询指定列
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <returns>返回集合</returns>
        public virtual async Task<IEnumerable<S>> FindListAsync<T, S>(Expression<Func<T, S>> selector) where T : class
        {
            return await DbContext.Set<T>().Select(selector).ToListAsync();
        }

        /// <summary>
        /// 查询全部并排序
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <param name="orderField">排序字段</param>
        /// <param name="orderTypes">排序类型，默认正序排序</param>
        /// <returns>返回集合</returns>
        public virtual async Task<IEnumerable<T>> FindListAsync<T>(Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = DbContext.Set<T>();
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
        public virtual async Task<IEnumerable<S>> FindListAsync<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = DbContext.Set<T>();
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
        public virtual async Task<IEnumerable<T>> FindListAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return await DbContext.Set<T>().Where(predicate).ToListAsync();
        }

        /// <summary>
        /// 根据条件查询并排序
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <param name="predicate">条件</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="orderTypes">排序类型，默认正序排序</param>
        /// <returns>返回集合</returns>
        public virtual async Task<IEnumerable<T>> FindListAsync<T>(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = DbContext.Set<T>().Where(predicate);
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
        public virtual async Task<IEnumerable<S>> FindListAsync<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate) where T : class
        {
            return await DbContext.Set<T>().Where(predicate).Select(selector).ToListAsync();
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
        public virtual async Task<IEnumerable<S>> FindListAsync<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            var query = DbContext.Set<T>().Where(predicate);
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
        public virtual async Task<IEnumerable<T>> FindListAsync<T>(string sql)
        {
            return await FindListAsync<T>(sql, null);
        }

        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回集合</returns>
        public virtual async Task<IEnumerable<T>> FindListAsync<T>(string sql, params DbParameter[] parameter)
        {
            return await DbContext.SqlQueryAsync<T>(sql, parameter);
        }

        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回集合</returns>
        public virtual async Task<IEnumerable<T>> FindListAsync<T>(string sql, params object[] parameter) where T : class
        {
            return await DbContext.Set<T>().FromSqlRaw(sql, parameter).ToListAsync();
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
        public virtual async Task<(IEnumerable<T> list, long total)> FindListAsync<T>(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, int pageSize, int pageIndex, params OrderType[] orderTypes) where T : class
        {
            IOrderedQueryable<T> order = null;
            var query = DbContext.Set<T>().Where(predicate);
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
        public virtual async Task<(IEnumerable<S> list, long total)> FindListAsync<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, int pageSize, int pageIndex, params OrderType[] orderTypes) where T : class
        {
            IOrderedQueryable<T> order = null;
            var query = DbContext.Set<T>().Where(predicate);
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
        public virtual async Task<(List<T> list, long total)> FindListAsync<T>(string sql, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            return await FindListAsync<T>(sql, null, orderField, isAscending, pageSize, pageIndex);
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
        public abstract Task<(List<T> list, long total)> FindListAsync<T>(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex);

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
        public abstract Task<(List<T> list, long total)> FindListByWithAsync<T>(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex);
        #endregion
        #endregion

        #region FindTable
        #region Sync
        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <returns>返回DataTable</returns>
        public virtual DataTable FindTable(string sql)
        {
            return FindTable(sql, null);
        }

        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回DataTable</returns>
        public virtual DataTable FindTable(string sql, params DbParameter[] parameter)
        {
            return DbContext.SqlDataTable(sql, parameter);
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
        public virtual (DataTable table, long total) FindTable(string sql, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            return FindTable(sql, null, orderField, isAscending, pageSize, pageIndex);
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
        public abstract (DataTable table, long total) FindTable(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex);

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
        public abstract (DataTable table, long total) FindTableByWith(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex);
        #endregion

        #region Async
        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <returns>返回DataTable</returns>
        public virtual async Task<DataTable> FindTableAsync(string sql)
        {
            return await FindTableAsync(sql, null);
        }

        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回DataTable</returns>
        public virtual async Task<DataTable> FindTableAsync(string sql, params DbParameter[] parameter)
        {
            return await DbContext.SqlDataTableAsync(sql, parameter);
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
        public virtual async Task<(DataTable table, long total)> FindTableAsync(string sql, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            return await FindTableAsync(sql, null, orderField, isAscending, pageSize, pageIndex);
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
        public abstract Task<(DataTable table, long total)> FindTableAsync(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex);

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
        public abstract Task<(DataTable table, long total)> FindTableByWithAsync(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex);
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
        public virtual List<List<T>> FindMultiple<T>(string sql)
        {
            return FindMultiple<T>(sql, null);
        }

        /// <summary>
        /// 根据sql语句查询返回多个结果集
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回查询结果集</returns>
        public virtual List<List<T>> FindMultiple<T>(string sql, params DbParameter[] parameter)
        {
            return DbContext.SqlQueryMultiple<T>(sql, parameter);
        }
        #endregion

        #region Async
        /// <summary>
        /// 根据sql语句查询返回多个结果集
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <returns>返回查询结果集</returns>
        public virtual async Task<List<List<T>>> FindMultipleAsync<T>(string sql)
        {
            return await FindMultipleAsync<T>(sql, null);
        }

        /// <summary>
        /// 根据sql语句查询返回多个结果集
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="parameter">对应参数</param>
        /// <returns>返回查询结果集</returns>
        public virtual async Task<List<List<T>>> FindMultipleAsync<T>(string sql, params DbParameter[] parameter)
        {
            return await DbContext.SqlQueryMultipleAsync<T>(sql, parameter);
        }
        #endregion
        #endregion
    }
}