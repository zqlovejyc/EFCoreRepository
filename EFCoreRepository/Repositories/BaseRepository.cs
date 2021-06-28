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

using EFCoreRepository.Enums;
using EFCoreRepository.Extensions;
using EFCoreRepository.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Concurrent;
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
namespace EFCoreRepository.Repositories
{
    /// <summary>
    /// 数据操作仓储抽象基类
    /// </summary>
    public abstract class BaseRepository : IRepository
    {
        #region Field
        /// <summary>
        /// 私有数据库连接字符串
        /// </summary>
        private string _connectionString;

        /// <summary>
        /// 私有事务对象
        /// </summary>
        private DbTransaction _transaction;

        /// <summary>
        /// 私有超时时长
        /// </summary>
        private int _commandTimeout = 240;
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
                return _commandTimeout;
            }
            set
            {
                _commandTimeout = value;
                DbContext.Database.SetCommandTimeout(_commandTimeout);
            }
        }

        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public virtual string ConnectionString
        {
            get
            {
                return _connectionString ?? DbContext.Database.GetDbConnection().ConnectionString;
            }
            set
            {
                _connectionString = value;
                DbContext.Database.GetDbConnection().ConnectionString = _connectionString;
            }
        }

        /// <summary>
        /// 事务对象
        /// </summary>
        public virtual DbTransaction Transaction
        {
            get
            {
                return _transaction ?? DbContext.Database.CurrentTransaction?.GetDbTransaction();
            }
            set
            {
                _transaction = value;
                DbContext.Database.UseTransaction(_transaction);
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">DbContext实例</param>
        public BaseRepository(DbContext context)
        {
            DbContext = context;
            DbContext.Database.SetCommandTimeout(CommandTimeout);
        }
        #endregion

        #region Queue
        #region Sync
        /// <summary>
        /// 同步委托队列(Queue)
        /// </summary>
        public virtual ConcurrentQueue<Func<IRepository, bool>> Queue { get; } = new();

        /// <summary>
        /// 加入同步委托队列(Queue)
        /// </summary>
        /// <param name="func">自定义委托</param>
        /// <returns></returns>
        public virtual void AddQueue(Func<IRepository, bool> func)
        {
            Queue.Enqueue(func);
        }

        /// <summary>
        /// 保存同步委托队列(Queue)
        /// </summary>
        /// <param name="transaction">是否开启事务</param>
        /// <returns></returns>
        public virtual bool SaveQueue(bool transaction = true)
        {
            try
            {
                if (Queue.IsEmpty)
                    return false;

                if (transaction)
                    BeginTransaction();

                var res = true;

                while (!Queue.IsEmpty && Queue.TryDequeue(out var func))
                    res = res && func(this);

                if (transaction)
                    Commit();

                return res;
            }
            catch (Exception)
            {
                if (transaction)
                    Rollback();

                throw;
            }
        }
        #endregion

        #region Async
        /// <summary>
        /// 异步委托队列(AsyncQueue)
        /// </summary>
        public virtual ConcurrentQueue<Func<IRepository, Task<bool>>> AsyncQueue { get; } = new();

        /// <summary>
        /// 加入异步委托队列(AsyncQueue)
        /// </summary>
        /// <param name="func">自定义委托</param>
        /// <returns></returns>
        public virtual void AddQueue(Func<IRepository, Task<bool>> func)
        {
            AsyncQueue.Enqueue(func);
        }

        /// <summary>
        /// 保存异步委托队列(AsyncQueue)
        /// </summary>
        /// <param name="transaction">是否开启事务</param>
        /// <returns></returns>
        public virtual async Task<bool> SaveQueueAsync(bool transaction = true)
        {
            try
            {
                if (AsyncQueue.IsEmpty)
                    return false;

                if (transaction)
                    await BeginTransactionAsync();

                var res = true;

                while (!AsyncQueue.IsEmpty && AsyncQueue.TryDequeue(out var func))
                    res = res && await func(this);

                if (transaction)
                    await CommitAsync();

                return res;
            }
            catch (Exception)
            {
                if (transaction)
                    await RollbackAsync();

                throw;
            }
        }
        #endregion
        #endregion

        #region SaveChanges
        #region Sync
        /// <summary>
        /// 保存更改
        /// </summary>
        /// <returns></returns>
        public int SaveChanges() => DbContext.SaveChanges();
        #endregion

        #region Async
        /// <summary>
        /// 保存更改
        /// </summary>
        /// <returns></returns>
        public async Task<int> SaveChangesAsync() => await DbContext.SaveChangesAsync();
        #endregion
        #endregion

        #region Transaction
        #region Sync
        /// <summary>
        /// 开启事务
        /// </summary>
        /// <returns>IRepository</returns>
        public virtual IRepository BeginTransaction()
        {
            if (DbContext.Database.CurrentTransaction == null)
                DbContext.Database.BeginTransaction();

            return this;
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public virtual void Commit()
        {
            DbContext.Database.CommitTransaction();
            DbContext.Database.CurrentTransaction?.Dispose();
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public virtual void Rollback()
        {
            DbContext.Database.RollbackTransaction();
            DbContext.Database.CurrentTransaction?.Dispose();
        }

        /// <summary>
        /// 执行事务，内部自动开启事务、提交和回滚事务
        /// </summary>
        /// <param name="handler">自定义委托</param>
        /// <param name="rollback">事务回滚处理委托</param>
        public virtual void ExecuteTransaction(Action<IRepository> handler, Action<Exception> rollback = null)
        {
            IRepository repository = null;
            try
            {
                if (handler != null)
                {
                    repository = BeginTransaction();
                    handler(repository);
                    repository.Commit();
                }
            }
            catch (Exception ex)
            {
                repository?.Rollback();

                if (rollback != null)
                    rollback(ex);
                else
                    throw;
            }
        }

        /// <summary>
        /// 执行事务，根据自定义委托返回值内部自动开启事务、提交和回滚事务
        /// </summary>
        /// <param name="handler">自定义委托</param>
        /// <param name="rollback">事务回滚处理委托，注意：自定义委托返回false时，rollback委托的异常参数为null</param>
        public virtual void ExecuteTransaction(Func<IRepository, bool> handler, Action<Exception> rollback = null)
        {
            IRepository repository = null;
            try
            {
                if (handler != null)
                {
                    repository = BeginTransaction();
                    var res = handler(repository);
                    if (res)
                        repository.Commit();
                    else
                    {
                        repository.Rollback();
                        rollback?.Invoke(null);
                    }
                }
            }
            catch (Exception ex)
            {
                repository?.Rollback();

                if (rollback != null)
                    rollback(ex);
                else
                    throw;
            }
        }
        #endregion

        #region Async
        /// <summary>
        /// 开启事务
        /// </summary>
        /// <returns>IRepository</returns>
        public virtual async Task<IRepository> BeginTransactionAsync()
        {
            if (DbContext.Database.CurrentTransaction == null)
                await DbContext.Database.BeginTransactionAsync();

            return this;
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        /// <returns></returns>
        public virtual async Task CommitAsync()
        {
            await DbContext.Database.CommitTransactionAsync();

            if (DbContext.Database.CurrentTransaction != null)
                await DbContext.Database.CurrentTransaction.DisposeAsync();
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public virtual async Task RollbackAsync()
        {
            await DbContext.Database.RollbackTransactionAsync();

            if (DbContext.Database.CurrentTransaction != null)
                await DbContext.Database.CurrentTransaction.DisposeAsync();
        }

        /// <summary>
        /// 执行事务，内部自动开启事务、提交和回滚事务
        /// </summary>
        /// <param name="handler">自定义委托</param>
        /// <param name="rollback">事务回滚处理委托</param>
        public virtual async Task ExecuteTransactionAsync(Func<IRepository, Task> handler, Func<Exception, Task> rollback = null)
        {
            IRepository repository = null;
            try
            {
                if (handler != null)
                {
                    repository = await BeginTransactionAsync();
                    await handler(repository);
                    await repository.CommitAsync();
                }
            }
            catch (Exception ex)
            {
                if (repository != null)
                    await repository.RollbackAsync();

                if (rollback != null)
                    await rollback(ex);
                else
                    throw;
            }
        }

        /// <summary>
        /// 执行事务，根据自定义委托返回值内部自动开启事务、提交和回滚事务
        /// </summary>
        /// <param name="handler">自定义委托</param>
        /// <param name="rollback">事务回滚处理委托，注意：自定义委托返回false时，rollback委托的异常参数为null</param>
        public virtual async Task ExecuteTransactionAsync(Func<IRepository, Task<bool>> handler, Func<Exception, Task> rollback = null)
        {
            IRepository repository = null;
            try
            {
                if (handler != null)
                {
                    repository = await BeginTransactionAsync();
                    var res = await handler(repository);
                    if (res)
                        await repository.CommitAsync();
                    else
                    {
                        await repository.RollbackAsync();

                        if (rollback != null)
                            await rollback(null);
                    }
                }
            }
            catch (Exception ex)
            {
                if (repository != null)
                    await repository.RollbackAsync();

                if (rollback != null)
                    await rollback(ex);
                else
                    throw;
            }
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
        /// <param name="saveChanges">是否保存更改</param>
        /// <returns>返回受影响行数</returns>
        public virtual int Insert<T>(T entity, bool saveChanges = true) where T : class
        {
            DbContext.Set<T>().Add(entity);

            if (!saveChanges)
                return 0;

            return DbContext.SaveChanges();
        }

        /// <summary>
        /// 插入多个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entities">要插入的实体集合</param>
        /// <param name="saveChanges">是否保存更改</param>
        /// <returns>返回受影响行数</returns>
        public virtual int Insert<T>(IEnumerable<T> entities, bool saveChanges = true) where T : class
        {
            DbContext.Set<T>().AddRange(entities);

            if (!saveChanges)
                return 0;

            return DbContext.SaveChanges();
        }
        #endregion

        #region Async
        /// <summary>
        ///  插入单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entity">要插入的实体</param>
        /// <param name="saveChanges">是否保存更改</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> InsertAsync<T>(T entity, bool saveChanges = true) where T : class
        {
            await DbContext.Set<T>().AddAsync(entity);

            if (!saveChanges)
                return 0;

            return await DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 插入多个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entities">要插入的实体集合</param>
        /// <param name="saveChanges">是否保存更改</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> InsertAsync<T>(IEnumerable<T> entities, bool saveChanges = true) where T : class
        {
            await DbContext.Set<T>().AddRangeAsync(entities);

            if (!saveChanges)
                return 0;

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
        public virtual int Delete<T>(bool saveChanges = true) where T : class
        {
            var entities = FindList<T>();
            return Delete(entities, saveChanges);
        }

        /// <summary>
        /// 删除单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entity">要删除的实体</param>
        /// <param name="saveChanges">是否保存更改</param>
        /// <returns>返回受影响行数</returns>
        public virtual int Delete<T>(T entity, bool saveChanges = true) where T : class
        {
            DbContext.Set<T>().Remove(entity);

            if (!saveChanges)
                return 0;

            return DbContext.SaveChanges();
        }

        /// <summary>
        /// 删除多个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entities">要删除的实体集合</param>
        /// <param name="saveChanges">是否保存更改</param>
        /// <returns>返回受影响行数</returns>
        public virtual int Delete<T>(IEnumerable<T> entities, bool saveChanges = true) where T : class
        {
            DbContext.Set<T>().RemoveRange(entities);

            if (!saveChanges)
                return 0;

            return DbContext.SaveChanges();
        }

        /// <summary>
        /// 根据条件删除实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="predicate">条件</param>
        /// <param name="saveChanges">是否保存更改</param>
        /// <returns>返回受影响行数</returns>
        public virtual int Delete<T>(Expression<Func<T, bool>> predicate, bool saveChanges = true) where T : class
        {
            var entities = FindList(predicate);
            return Delete(entities, saveChanges);
        }

        /// <summary>
        /// 根据主键删除实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="saveChanges">是否保存更改</param>
        /// <param name="keyValues">主键值</param>
        /// <returns>返回受影响行数</returns>
        public virtual int Delete<T>(bool saveChanges = true, params object[] keyValues) where T : class
        {
            var entity = FindEntity<T>(keyValues);
            if (entity == null)
                return 0;

            return Delete(entity, saveChanges);
        }
        #endregion

        #region Async
        /// <summary>
        /// 删除全部
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="saveChanges">是否保存更改</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> DeleteAsync<T>(bool saveChanges = true) where T : class
        {
            var entities = await FindListAsync<T>();
            return await DeleteAsync(entities, saveChanges);
        }

        /// <summary>
        /// 删除单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entity">要删除的实体</param>
        /// <param name="saveChanges">是否保存更改</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> DeleteAsync<T>(T entity, bool saveChanges = true) where T : class
        {
            DbContext.Set<T>().Remove(entity);

            if (!saveChanges)
                return 0;

            return await DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 删除多个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entities">要删除的实体集合</param>
        /// <param name="saveChanges">是否保存更改</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> DeleteAsync<T>(IEnumerable<T> entities, bool saveChanges = true) where T : class
        {
            DbContext.Set<T>().RemoveRange(entities);

            if (!saveChanges)
                return 0;

            return await DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 根据条件删除实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="predicate">条件</param>
        /// <param name="saveChanges">是否保存更改</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> DeleteAsync<T>(Expression<Func<T, bool>> predicate, bool saveChanges = true) where T : class
        {
            var entities = await FindListAsync(predicate);
            return await DeleteAsync(entities, saveChanges);
        }

        /// <summary>
        /// 根据主键删除实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="saveChanges">是否保存更改</param>
        /// <param name="keyValues">主键值</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> DeleteAsync<T>(bool saveChanges = true, params object[] keyValues) where T : class
        {
            var entity = await FindEntityAsync<T>(keyValues);
            if (entity == null)
                return 0;

            return await DeleteAsync(entity, saveChanges);
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
        /// <param name="saveChanges">是否保存更改</param>
        /// <returns>返回受影响行数</returns>
        public virtual int Update<T>(T entity, bool saveChanges = true) where T : class
        {
            var entry = DbContext.Entry(entity);

            if (entry.State == EntityState.Detached)
                DbContext.Attach(entity);

            var props = entity.GetType().GetProperties();

            foreach (var prop in props)
            {
                //非null且非PrimaryKey
                if (prop.GetValue(entity, null) != null && !entry.Property(prop.Name).Metadata.IsPrimaryKey())
                    entry.Property(prop.Name).IsModified = true;
            }

            if (!saveChanges)
                return 0;

            return DbContext.SaveChanges();
        }

        /// <summary>
        /// 更新多个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entities">要更新的实体集合</param>
        /// <param name="saveChanges">是否保存更改</param>
        /// <returns>返回受影响行数</returns>
        public virtual int Update<T>(IEnumerable<T> entities, bool saveChanges = true) where T : class
        {
            foreach (var entity in entities)
            {
                var entry = DbContext.Entry(entity);
                if (entry.State == EntityState.Detached)
                    DbContext.Attach(entity);

                var props = entity.GetType().GetProperties();
                foreach (var prop in props)
                {
                    //非null且非PrimaryKey
                    if (prop.GetValue(entity, null) != null && !entry.Property(prop.Name).Metadata.IsPrimaryKey())
                        entry.Property(prop.Name).IsModified = true;
                }
            }

            if (!saveChanges)
                return 0;

            return DbContext.SaveChanges();
        }

        /// <summary>
        /// 根据条件更新实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="predicate">条件</param>
        /// <param name="entity">要更新的实体</param>
        /// <param name="saveChanges">是否保存更改</param>
        /// <returns>返回受影响行数</returns>
        public virtual int Update<T>(Expression<Func<T, bool>> predicate, T entity, bool saveChanges = true) where T : class
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
                    var isKey = property.GetCustomAttributes(typeof(KeyAttribute), false).Any();
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

            return Update<T>(entities, saveChanges);
        }
        #endregion

        #region Async
        /// <summary>
        /// 更新单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entity">要更新的实体</param>
        /// <param name="saveChanges">是否保存更改</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> UpdateAsync<T>(T entity, bool saveChanges = true) where T : class
        {
            DbContext.Set<T>().Attach(entity);
            var entry = DbContext.Entry(entity);
            var props = entity.GetType().GetProperties();

            foreach (var prop in props)
            {
                //非null且非PrimaryKey
                if (prop.GetValue(entity, null) != null && !entry.Property(prop.Name).Metadata.IsPrimaryKey())
                    entry.Property(prop.Name).IsModified = true;
            }

            if (!saveChanges)
                return 0;

            return await DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 更新多个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entities">要更新的实体集合</param>
        /// <param name="saveChanges">是否保存更改</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> UpdateAsync<T>(IEnumerable<T> entities, bool saveChanges = true) where T : class
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
                        entry.Property(prop.Name).IsModified = true;
                }
            }

            if (!saveChanges)
                return 0;

            return await DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 根据条件更新实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="predicate">条件</param>
        /// <param name="entity">要更新的实体</param>
        /// <param name="saveChanges">是否保存更改</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> UpdateAsync<T>(Expression<Func<T, bool>> predicate, T entity, bool saveChanges = true) where T : class
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
                    var isKey = property.GetCustomAttributes(typeof(KeyAttribute), false).Any();
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

            return await UpdateAsync<T>(entities, saveChanges);
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
            return default;
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

        /// <summary>
        /// 根据条件查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="predicate">条件</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="orderTypes">排序类型</param>
        /// <returns>返回实体</returns>
        public virtual T FindEntity<T>(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            return DbContext.Set<T>().Where(predicate).ToOrderedQueryable(orderField, orderTypes).FirstOrDefault();
        }

        /// <summary>
        /// 根据条件查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <param name="predicate">条件</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="orderTypes">排序类型</param>
        /// <returns>返回实体</returns>
        public virtual S FindEntity<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            return DbContext.Set<T>().Where(predicate).ToOrderedQueryable(orderField, orderTypes).Select(selector).FirstOrDefault();
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
            return default;
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

        /// <summary>
        /// 根据条件查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>        
        /// <param name="predicate">条件</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="orderTypes">排序类型</param>
        /// <returns>返回实体</returns>
        public virtual async Task<T> FindEntityAsync<T>(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            return await DbContext.Set<T>().Where(predicate).ToOrderedQueryable(orderField, orderTypes).FirstOrDefaultAsync();
        }

        /// <summary>
        /// 根据条件查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <typeparam name="S">泛型类型</typeparam>
        /// <param name="selector">选择指定列</param>
        /// <param name="predicate">条件</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="orderTypes">排序类型</param>
        /// <returns>返回实体</returns>
        public virtual async Task<S> FindEntityAsync<T, S>(Expression<Func<T, S>> selector, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderField, params OrderType[] orderTypes) where T : class
        {
            return await DbContext.Set<T>().Where(predicate).ToOrderedQueryable(orderField, orderTypes).Select(selector).FirstOrDefaultAsync();
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
            return DbContext.Set<T>().Where(predicate).ToOrderedQueryable(orderField, orderTypes);
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
            return DbContext.Set<T>().Where(predicate).ToOrderedQueryable(orderField, orderTypes).Select(selector);
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
            var queryable = DbContext.Set<T>().Where(predicate).ToOrderedQueryable(orderField, orderTypes);
            return await Task.FromResult(queryable);
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
            var queryable = DbContext.Set<T>().Where(predicate).ToOrderedQueryable(orderField, orderTypes).Select(selector);
            return await Task.FromResult(queryable);
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
            return DbContext.Set<T>().ToOrderedQueryable(orderField, orderTypes).ToList();
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
            return DbContext.Set<T>().ToOrderedQueryable(orderField, orderTypes).Select(selector).ToList();
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
            return DbContext.Set<T>().Where(predicate).ToOrderedQueryable(orderField, orderTypes).ToList();
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
            return DbContext.Set<T>().Where(predicate).ToOrderedQueryable(orderField, orderTypes).Select(selector).ToList();
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
            var query = DbContext.Set<T>().Where(predicate);
            var total = query.Count();
            var order = query.ToOrderedQueryable(orderField, orderTypes);

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
            var query = DbContext.Set<T>().Where(predicate);
            var total = query.Count();
            var order = query.ToOrderedQueryable(orderField, orderTypes);

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
        public virtual (List<T> list, long total) FindList<T>(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            var sqlQuery = GetPageSql(false, sql, orderField, isAscending, pageSize, pageIndex);

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
        public virtual (List<T> list, long total) FindListByWith<T>(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            var sqlQuery = GetPageSql(true, sql, orderField, isAscending, pageSize, pageIndex);

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
            return await DbContext.Set<T>().ToOrderedQueryable(orderField, orderTypes).ToListAsync();
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
            return await DbContext.Set<T>().ToOrderedQueryable(orderField, orderTypes).Select(selector).ToListAsync();
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
            return await DbContext.Set<T>().Where(predicate).ToOrderedQueryable(orderField, orderTypes).ToListAsync();
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
            return await DbContext.Set<T>().Where(predicate).ToOrderedQueryable(orderField, orderTypes).Select(selector).ToListAsync();
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
            var query = DbContext.Set<T>().Where(predicate);
            var total = await query.CountAsync();
            var order = query.ToOrderedQueryable(orderField, orderTypes);

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
            var query = DbContext.Set<T>().Where(predicate);
            var total = await query.CountAsync();
            var order = query.ToOrderedQueryable(orderField, orderTypes);

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
        public virtual async Task<(List<T> list, long total)> FindListAsync<T>(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            var sqlQuery = GetPageSql(false, sql, orderField, isAscending, pageSize, pageIndex);

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
        public virtual async Task<(List<T> list, long total)> FindListByWithAsync<T>(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            var sqlQuery = GetPageSql(true, sql, orderField, isAscending, pageSize, pageIndex);

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
        public virtual (DataTable table, long total) FindTable(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            var sqlQuery = GetPageSql(false, sql, orderField, isAscending, pageSize, pageIndex);

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
        public virtual (DataTable table, long total) FindTableByWith(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            var sqlQuery = GetPageSql(true, sql, orderField, isAscending, pageSize, pageIndex);

            var ds = DbContext.SqlDataSet(sqlQuery, parameter);
            return (ds.Tables[1], Convert.ToInt64(ds.Tables[0].Rows[0]["TOTAL"]));
        }
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
        public virtual async Task<(DataTable table, long total)> FindTableAsync(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            var sqlQuery = GetPageSql(false, sql, orderField, isAscending, pageSize, pageIndex);

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
        public virtual async Task<(DataTable table, long total)> FindTableByWithAsync(string sql, DbParameter[] parameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            var sqlQuery = GetPageSql(true, sql, orderField, isAscending, pageSize, pageIndex);

            var ds = await DbContext.SqlDataSetAsync(sqlQuery, parameter);
            return (ds.Tables[1], Convert.ToInt64(ds.Tables[0].Rows[0]["TOTAL"]));
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

        #region Close
        #region Sync
        /// <summary>
        /// 关闭连接
        /// </summary>
        public virtual void Close()
        {
            DbContext.Database.CloseConnection();
            DbContext.Dispose();
        }
        #endregion

        #region Async
        /// <summary>
        /// 关闭连接
        /// </summary>
        public virtual async ValueTask CloseAsync()
        {
            await DbContext.Database.CloseConnectionAsync();
            await DbContext.DisposeAsync();
        }
        #endregion
        #endregion

        #region Dispose
        #region Sync
        /// <summary>
        /// 释放资源
        /// </summary>
        public virtual void Dispose() => Close();
        #endregion

        #region Async
        /// <summary>
        /// 释放资源
        /// </summary>
        /// <returns></returns>
        public virtual async ValueTask DisposeAsync() => await CloseAsync();
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
        public abstract string GetPageSql(bool isWithSyntax, string sql, string orderField, bool isAscending, int pageSize, int pageIndex);
        #endregion
    }
}
