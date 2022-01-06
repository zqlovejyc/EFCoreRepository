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

using EFCoreRepository.Enums;
using EFCoreRepository.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        /// <summary>
        /// 数据库类型
        /// </summary>
        public virtual DatabaseType DatabaseType { get; }
        #endregion

        #region Constructor
        /// <summary>
        /// 构造函数
        /// </summary>
        public BaseRepository() { }

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
        /// 同步委托队列(SyncQueue)
        /// </summary>
        public virtual ConcurrentQueue<Func<IRepository, bool>> SyncQueue { get; } = new();

        /// <summary>
        /// 加入同步委托队列(SyncQueue)
        /// </summary>
        /// <param name="func">自定义委托</param>
        /// <returns></returns>
        public virtual void AddQueue(Func<IRepository, bool> func)
        {
            SyncQueue.Enqueue(func);
        }

        /// <summary>
        /// 保存同步委托队列(SyncQueue)
        /// </summary>
        /// <param name="transaction">是否开启事务</param>
        /// <returns></returns>
        public virtual bool SaveQueue(bool transaction = true)
        {
            try
            {
                if (SyncQueue.IsEmpty)
                    return false;

                if (transaction)
                    BeginTransaction();

                var res = true;

                while (!SyncQueue.IsEmpty && SyncQueue.TryDequeue(out var func))
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

        #region UseQueryTrackingBehavior
        /// <summary>
        /// 上下文级别设置查询跟踪行为
        /// </summary>
        /// <param name="behavior"></param>
        /// <returns></returns>
        public virtual IRepository UseQueryTrackingBehavior(QueryTrackingBehavior behavior)
        {
            DbContext.ChangeTracker.QueryTrackingBehavior = behavior;

            return this;
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
        /// <param name="formattableSql">内插sql语句</param>
        /// <returns>返回受影响行数</returns>
        public virtual int ExecuteBySql(FormattableString formattableSql)
        {
            return DbContext.Database.ExecuteSqlInterpolated(formattableSql);
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
        ///  <param name="dbParameter">对应参数</param>
        /// <returns>返回受影响行数</returns>
        public virtual int ExecuteByProc(string procName, params DbParameter[] dbParameter)
        {
            return DbContext.ExecuteProc(procName, dbParameter);
        }

        /// <summary>
        /// 执行sql存储过程查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="procName"></param>
        /// <param name="dbParameter"></param>
        /// <returns></returns>
        public virtual IEnumerable<T> ExecuteByProc<T>(string procName, params DbParameter[] dbParameter)
        {
            return DbContext.ExecuteProc<T>(procName, dbParameter);
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
        /// <param name="formattableSql">内插sql语句</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> ExecuteBySqlAsync(FormattableString formattableSql)
        {
            return await DbContext.Database.ExecuteSqlInterpolatedAsync(formattableSql);
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
        ///  <param name="dbParameter">对应参数</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> ExecuteByProcAsync(string procName, params DbParameter[] dbParameter)
        {
            return await DbContext.ExecuteProcAsync(procName, dbParameter);
        }

        /// <summary>
        /// 执行sql存储过程查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="procName"></param>
        /// <param name="dbParameter"></param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<T>> ExecuteByProcAsync<T>(string procName, params DbParameter[] dbParameter)
        {
            return await DbContext.ExecuteProcAsync<T>(procName, dbParameter);
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

        /// <summary>
        /// 插入多个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entities">要插入的实体集合</param>
        /// <param name="saveChanges">是否保存更改</param>
        /// <returns>返回受影响行数</returns>
        public virtual int Insert<T>(List<T> entities, bool saveChanges = true) where T : class
        {
            return Insert(entities.AsEnumerable(), saveChanges);
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

        /// <summary>
        /// 插入多个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entities">要插入的实体集合</param>
        /// <param name="saveChanges">是否保存更改</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> InsertAsync<T>(List<T> entities, bool saveChanges = true) where T : class
        {
            return await InsertAsync(entities.AsEnumerable(), saveChanges);
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
        /// 删除多个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entities">要删除的实体集合</param>
        /// <param name="saveChanges">是否保存更改</param>
        /// <returns>返回受影响行数</returns>
        public virtual int Delete<T>(List<T> entities, bool saveChanges = true) where T : class
        {
            return Delete(entities.AsEnumerable(), saveChanges);
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
        /// 删除多个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entities">要删除的实体集合</param>
        /// <param name="saveChanges">是否保存更改</param>
        /// <returns>返回受影响行数</returns>
        public virtual async Task<int> DeleteAsync<T>(List<T> entities, bool saveChanges = true) where T : class
        {
            return await DeleteAsync(entities.AsEnumerable(), saveChanges);
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
            //获取所有T类型的状态跟踪实例
            var entries = DbContext.ChangeTracker.Entries<T>();

            //获取T类型实体的所有主键属性信息
            var primaryKeyProps = entries.Select(x => x.Properties.Where(v => v.Metadata.IsPrimaryKey()));

            //获取已经附加到上下文的实例
            var existedEntry = entries.FirstOrDefault(x =>
                primaryKeyProps.Any(props => !props.Any(prop => prop.CurrentValue?.ToString() != prop.Metadata.PropertyInfo.GetValue(entity)?.ToString())));

            var entry = DbContext.Entry(entity);

            if (entry.State == EntityState.Detached && existedEntry == null)
                DbContext.Attach(entity);

            //T类型实体属性信息
            var props = entry.Properties;

            foreach (var prop in props)
            {
                var propInfo = prop.Metadata.PropertyInfo;
                var propValue = propInfo.GetValue(entity);

                //非null且非PrimaryKey
                if (propValue != null && !prop.Metadata.IsPrimaryKey())
                {
                    if (existedEntry == null)
                        entry.Property(propInfo.Name).IsModified = true;
                    else
                    {
                        existedEntry.Property(propInfo.Name).CurrentValue = propValue;
                        existedEntry.Property(propInfo.Name).IsModified = true;
                    }
                }
                else if (propValue == null && existedEntry != null)
                {
                    existedEntry.Property(propInfo.Name).IsModified = false;
                }
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
            if (entities == null || !entities.Any())
                return 0;

            //获取所有T类型的状态跟踪实例
            var entries = DbContext.ChangeTracker.Entries<T>();

            //T类型实体属性信息
            var props = entries.First().Properties;

            //获取T类型实体的所有主键属性信息
            var primaryKeyProps = entries.Select(x => x.Properties.Where(v => v.Metadata.IsPrimaryKey()));

            foreach (var entity in entities)
            {
                var entry = DbContext.Entry(entity);

                //获取已经附加到上下文的实例
                var existedEntry = entries.FirstOrDefault(x =>
                    primaryKeyProps.Any(props => !props.Any(prop => prop.CurrentValue?.ToString() != prop.Metadata.PropertyInfo.GetValue(entity)?.ToString())));

                if (entry.State == EntityState.Detached && existedEntry == null)
                    DbContext.Attach(entity);

                foreach (var prop in props)
                {
                    var propInfo = prop.Metadata.PropertyInfo;
                    var propValue = propInfo.GetValue(entity);

                    //非null且非PrimaryKey
                    if (propValue != null && !prop.Metadata.IsPrimaryKey())
                    {
                        if (existedEntry == null)
                            entry.Property(propInfo.Name).IsModified = true;
                        else
                        {
                            existedEntry.Property(propInfo.Name).CurrentValue = propValue;
                            existedEntry.Property(propInfo.Name).IsModified = true;
                        }
                    }
                    else if (propValue == null && existedEntry != null)
                    {
                        existedEntry.Property(propInfo.Name).IsModified = false;
                    }
                }
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
        public virtual int Update<T>(List<T> entities, bool saveChanges = true) where T : class
        {
            return Update(entities, saveChanges);
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
            var entities = FindList(predicate);

            if (entities == null || !entities.Any())
                return 0;

            //获取所有T类型的状态跟踪实例
            var entry = DbContext.ChangeTracker.Entries<T>().First();

            //T类型实体属性信息
            var props = entry.Properties;

            //获取T类型实体的所有主键属性信息
            var primaryKeyProps = entry.Properties.Where(x => x.Metadata.IsPrimaryKey());

            foreach (var item in entities)
            {
                foreach (var prop in props)
                {
                    var propInfo = prop.Metadata.PropertyInfo;
                    var propValue = propInfo.GetValue(entity);

                    if (propValue != null)
                        propInfo.SetValue(item, propValue);
                    else if (!primaryKeyProps.Any(x => x.Metadata.PropertyInfo == propInfo))
                        propInfo.SetValue(item, null);
                }
            }

            return Update(entities, saveChanges);
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
            //获取所有T类型的状态跟踪实例
            var entries = DbContext.ChangeTracker.Entries<T>();

            //获取T类型实体的所有主键属性信息
            var primaryKeyProps = entries.Select(x => x.Properties.Where(v => v.Metadata.IsPrimaryKey()));

            //获取已经附加到上下文的实例
            var existedEntry = entries.FirstOrDefault(x =>
                primaryKeyProps.Any(props => !props.Any(prop => prop.CurrentValue?.ToString() != prop.Metadata.PropertyInfo.GetValue(entity)?.ToString())));

            var entry = DbContext.Entry(entity);

            if (entry.State == EntityState.Detached && existedEntry == null)
                DbContext.Attach(entity);

            //T类型实体属性信息
            var props = entry.Properties;

            foreach (var prop in props)
            {
                var propInfo = prop.Metadata.PropertyInfo;
                var propValue = propInfo.GetValue(entity);

                //非null且非PrimaryKey
                if (propValue != null && !prop.Metadata.IsPrimaryKey())
                {
                    if (existedEntry == null)
                        entry.Property(propInfo.Name).IsModified = true;
                    else
                    {
                        existedEntry.Property(propInfo.Name).CurrentValue = propValue;
                        existedEntry.Property(propInfo.Name).IsModified = true;
                    }
                }
                else if (propValue == null && existedEntry != null)
                {
                    existedEntry.Property(propInfo.Name).IsModified = false;
                }
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
            if (entities == null || !entities.Any())
                return 0;

            //获取所有T类型的状态跟踪实例
            var entries = DbContext.ChangeTracker.Entries<T>();

            //T类型实体属性信息
            var props = entries.First().Properties;

            //获取T类型实体的所有主键属性信息
            var primaryKeyProps = entries.Select(x => x.Properties.Where(v => v.Metadata.IsPrimaryKey()));

            foreach (var entity in entities)
            {
                var entry = DbContext.Entry(entity);

                //获取已经附加到上下文的实例
                var existedEntry = entries.FirstOrDefault(x =>
                    primaryKeyProps.Any(props => !props.Any(prop => prop.CurrentValue?.ToString() != prop.Metadata.PropertyInfo.GetValue(entity)?.ToString())));

                if (entry.State == EntityState.Detached && existedEntry == null)
                    DbContext.Attach(entity);

                foreach (var prop in props)
                {
                    var propInfo = prop.Metadata.PropertyInfo;
                    var propValue = propInfo.GetValue(entity);

                    //非null且非PrimaryKey
                    if (propValue != null && !prop.Metadata.IsPrimaryKey())
                    {
                        if (existedEntry == null)
                            entry.Property(propInfo.Name).IsModified = true;
                        else
                        {
                            existedEntry.Property(propInfo.Name).CurrentValue = propValue;
                            existedEntry.Property(propInfo.Name).IsModified = true;
                        }
                    }
                    else if (propValue == null && existedEntry != null)
                    {
                        existedEntry.Property(propInfo.Name).IsModified = false;
                    }
                }
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
        public virtual async Task<int> UpdateAsync<T>(List<T> entities, bool saveChanges = true) where T : class
        {
            return await UpdateAsync(entities.AsEnumerable(), saveChanges);
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
            var entities = await FindListAsync(predicate);

            if (entities == null || !entities.Any())
                return 0;

            //获取所有T类型的状态跟踪实例
            var entry = DbContext.ChangeTracker.Entries<T>().First();

            //T类型实体属性信息
            var props = entry.Properties;

            //获取T类型实体的所有主键属性信息
            var primaryKeyProps = entry.Properties.Where(x => x.Metadata.IsPrimaryKey());

            foreach (var item in entities)
            {
                foreach (var prop in props)
                {
                    var propInfo = prop.Metadata.PropertyInfo;
                    var propValue = propInfo.GetValue(entity);

                    if (propValue != null)
                        propInfo.SetValue(item, propValue);
                    else if (!primaryKeyProps.Any(x => x.Metadata.PropertyInfo == propInfo))
                        propInfo.SetValue(item, null);
                }
            }

            return await UpdateAsync(entities, saveChanges);
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
        /// <param name="formattableSql">内插sql语句</param>
        /// <returns>返回查询结果对象</returns>
        public virtual object FindObject(FormattableString formattableSql)
        {
            var (sqlFormat, parameter) = formattableSql.ToDbParameter(this.DatabaseType);

            return FindObject(sqlFormat, parameter);
        }

        /// <summary>
        /// 查询单个对象
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="dbParameter">对应参数</param>
        /// <returns>返回查询结果对象</returns>
        public virtual object FindObject(string sql, params DbParameter[] dbParameter)
        {
            return DbContext.SqlQuery<Dictionary<string, object>>(sql, dbParameter)?.FirstOrDefault()?.Select(o => o.Value).FirstOrDefault();
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
        /// <param name="formattableSql">内插sql语句</param>
        /// <returns>返回查询结果对象</returns>
        public virtual async Task<object> FindObjectAsync(FormattableString formattableSql)
        {
            var (sqlFormat, parameter) = formattableSql.ToDbParameter(this.DatabaseType);

            return await FindObjectAsync(sqlFormat, parameter);
        }

        /// <summary>
        /// 查询单个对象
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="dbParameter">对应参数</param>
        /// <returns>返回查询结果对象</returns>
        public virtual async Task<object> FindObjectAsync(string sql, params DbParameter[] dbParameter)
        {
            return (await DbContext.SqlQueryAsync<Dictionary<string, object>>(sql, dbParameter))?.FirstOrDefault()?.Select(o => o.Value).FirstOrDefault();
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
        /// <param name="formattableSql">内插sql语句</param>
        /// <returns>返回实体</returns>
        public virtual T FindEntity<T>(FormattableString formattableSql)
        {
            var (sqlFormat, parameter) = formattableSql.ToDbParameter(this.DatabaseType);

            return FindEntity<T>(sqlFormat, parameter);
        }

        /// <summary>
        /// 根据sql语句查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="dbParameter">对应参数</param>
        /// <returns>返回实体</returns>
        public virtual T FindEntity<T>(string sql, params DbParameter[] dbParameter)
        {
            var query = DbContext.SqlQuery<T>(sql, dbParameter);
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
        /// <param name="formattableSql">内插sql语句</param>
        /// <returns>返回实体</returns>
        public virtual async Task<T> FindEntityAsync<T>(FormattableString formattableSql)
        {
            var (sqlFormat, parameter) = formattableSql.ToDbParameter(this.DatabaseType);

            return await FindEntityAsync<T>(sqlFormat, parameter);
        }

        /// <summary>
        /// 根据sql语句查询单个实体
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="dbParameter">对应参数</param>
        /// <returns>返回实体</returns>
        public virtual async Task<T> FindEntityAsync<T>(string sql, params DbParameter[] dbParameter)
        {
            var query = await DbContext.SqlQueryAsync<T>(sql, dbParameter);
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
        /// <param name="formattableSql">内插sql语句</param>
        /// <returns>返回集合</returns>
        public virtual IEnumerable<T> FindList<T>(FormattableString formattableSql)
        {
            var (sqlFormat, parameter) = formattableSql.ToDbParameter(this.DatabaseType);

            return FindList<T>(sqlFormat, parameter);
        }

        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="dbParameter">对应参数</param>
        /// <returns>返回集合</returns>
        public virtual IEnumerable<T> FindList<T>(string sql, params DbParameter[] dbParameter)
        {
            return DbContext.SqlQuery<T>(sql, dbParameter);
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
        /// <param name="dbParameter">对应参数</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="isAscending">是否升序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>        
        /// <returns>返回集合和总记录数</returns>
        public virtual (List<T> list, long total) FindList<T>(string sql, DbParameter[] dbParameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            var sqlQuery = GetPageSql(false, sql, orderField, isAscending, pageSize, pageIndex);

            var type = typeof(T);
            if (type.IsClass && !type.IsDictionaryType() && !type.IsDynamicOrObjectType() && !type.IsStringType())
            {
                var query = DbContext.SqlQueryMultiple<dynamic>(sqlQuery, dbParameter);
                return (query.LastOrDefault().Select(o => (o as IDictionary<string, object>).ToEntity<T>()).ToList(), Convert.ToInt64(query.FirstOrDefault().FirstOrDefault().TOTAL ?? 0));
            }
            else
            {
                var query = DbContext.SqlQueryMultiple<T>(sqlQuery, dbParameter);
                return (query.LastOrDefault(), Convert.ToInt64((query.FirstOrDefault().FirstOrDefault() as IDictionary<string, object>)?["TOTAL"] ?? 0));
            }
        }

        /// <summary>
        /// with语法分页查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="dbParameter">对应参数</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="isAscending">是否升序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>        
        /// <returns>返回集合和总记录数</returns>
        public virtual (List<T> list, long total) FindListByWith<T>(string sql, DbParameter[] dbParameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            var sqlQuery = GetPageSql(true, sql, orderField, isAscending, pageSize, pageIndex);

            var type = typeof(T);
            if (type.IsClass && !type.IsDictionaryType() && !type.IsDynamicOrObjectType() && !type.IsStringType())
            {
                var query = DbContext.SqlQueryMultiple<dynamic>(sqlQuery, dbParameter);
                return (query.LastOrDefault().Select(o => (o as IDictionary<string, object>).ToEntity<T>()).ToList(), Convert.ToInt64(query.FirstOrDefault().FirstOrDefault().TOTAL ?? 0));
            }
            else
            {
                var query = DbContext.SqlQueryMultiple<T>(sqlQuery, dbParameter);
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
        /// <param name="formattableSql">内插sql语句</param>
        /// <returns>返回集合</returns>
        public virtual async Task<IEnumerable<T>> FindListAsync<T>(FormattableString formattableSql)
        {
            var (sqlFormat, parameter) = formattableSql.ToDbParameter(this.DatabaseType);

            return await FindListAsync<T>(sqlFormat, parameter);
        }

        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="dbParameter">对应参数</param>
        /// <returns>返回集合</returns>
        public virtual async Task<IEnumerable<T>> FindListAsync<T>(string sql, params DbParameter[] dbParameter)
        {
            return await DbContext.SqlQueryAsync<T>(sql, dbParameter);
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
        /// <param name="dbParameter">对应参数</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="isAscending">是否升序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>        
        /// <returns>返回集合和总记录数</returns>
        public virtual async Task<(List<T> list, long total)> FindListAsync<T>(string sql, DbParameter[] dbParameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            var sqlQuery = GetPageSql(false, sql, orderField, isAscending, pageSize, pageIndex);

            var type = typeof(T);
            if (type.IsClass && !type.IsDictionaryType() && !type.IsDynamicOrObjectType() && !type.IsStringType())
            {
                var query = await DbContext.SqlQueryMultipleAsync<dynamic>(sqlQuery, dbParameter);
                return (query.LastOrDefault().Select(o => (o as IDictionary<string, object>).ToEntity<T>()).ToList(), Convert.ToInt64(query.FirstOrDefault().FirstOrDefault().TOTAL ?? 0));
            }
            else
            {
                var query = await DbContext.SqlQueryMultipleAsync<T>(sqlQuery, dbParameter);
                return (query.LastOrDefault(), Convert.ToInt64((query.FirstOrDefault().FirstOrDefault() as IDictionary<string, object>)?["TOTAL"] ?? 0));
            }
        }

        /// <summary>
        /// with语法分页查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="dbParameter">对应参数</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="isAscending">是否升序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>        
        /// <returns>返回集合和总记录数</returns>
        public virtual async Task<(List<T> list, long total)> FindListByWithAsync<T>(string sql, DbParameter[] dbParameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            var sqlQuery = GetPageSql(true, sql, orderField, isAscending, pageSize, pageIndex);

            var type = typeof(T);
            if (type.IsClass && !type.IsDictionaryType() && !type.IsDynamicOrObjectType() && !type.IsStringType())
            {
                var query = await DbContext.SqlQueryMultipleAsync<dynamic>(sqlQuery, dbParameter);
                return (query.LastOrDefault().Select(o => (o as IDictionary<string, object>).ToEntity<T>()).ToList(), Convert.ToInt64(query.FirstOrDefault().FirstOrDefault().TOTAL ?? 0));
            }
            else
            {
                var query = await DbContext.SqlQueryMultipleAsync<T>(sqlQuery, dbParameter);
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
        /// <param name="formattableSql">内插sql语句</param>
        /// <returns>返回DataTable</returns>
        public virtual DataTable FindTable(FormattableString formattableSql)
        {
            var (sqlFormat, parameter) = formattableSql.ToDbParameter(this.DatabaseType);

            return FindTable(sqlFormat, parameter);
        }

        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="dbParameter">对应参数</param>
        /// <returns>返回DataTable</returns>
        public virtual DataTable FindTable(string sql, params DbParameter[] dbParameter)
        {
            return DbContext.SqlDataTable(sql, dbParameter);
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
        /// <param name="dbParameter">对应参数</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="isAscending">是否升序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>        
        /// <returns>返回DataTable和总记录数</returns>
        public virtual (DataTable table, long total) FindTable(string sql, DbParameter[] dbParameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            var sqlQuery = GetPageSql(false, sql, orderField, isAscending, pageSize, pageIndex);

            var ds = DbContext.SqlDataSet(sqlQuery, dbParameter);
            return (ds.Tables[1], Convert.ToInt64(ds.Tables[0].Rows[0]["TOTAL"]));
        }

        /// <summary>
        /// with语法分页查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="dbParameter">对应参数</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="isAscending">是否升序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>        
        /// <returns>返回DataTable和总记录数</returns>
        public virtual (DataTable table, long total) FindTableByWith(string sql, DbParameter[] dbParameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            var sqlQuery = GetPageSql(true, sql, orderField, isAscending, pageSize, pageIndex);

            var ds = DbContext.SqlDataSet(sqlQuery, dbParameter);
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
        /// <param name="formattableSql">内插sql语句</param>
        /// <returns>返回DataTable</returns>
        public virtual async Task<DataTable> FindTableAsync(FormattableString formattableSql)
        {
            var (sqlFormat, parameter) = formattableSql.ToDbParameter(this.DatabaseType);

            return await FindTableAsync(sqlFormat, parameter);
        }

        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="dbParameter">对应参数</param>
        /// <returns>返回DataTable</returns>
        public virtual async Task<DataTable> FindTableAsync(string sql, params DbParameter[] dbParameter)
        {
            return await DbContext.SqlDataTableAsync(sql, dbParameter);
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
        /// <param name="dbParameter">对应参数</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="isAscending">是否升序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>
        /// <returns>返回DataTable和总记录数</returns>
        public virtual async Task<(DataTable table, long total)> FindTableAsync(string sql, DbParameter[] dbParameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            var sqlQuery = GetPageSql(false, sql, orderField, isAscending, pageSize, pageIndex);

            var ds = await DbContext.SqlDataSetAsync(sqlQuery, dbParameter);
            return (ds.Tables[1], Convert.ToInt64(ds.Tables[0].Rows[0]["TOTAL"]));
        }

        /// <summary>
        /// with语法分页查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="dbParameter">对应参数</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="isAscending">是否升序</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">当前页码</param>        
        /// <returns>返回DataTable和总记录数</returns>
        public virtual async Task<(DataTable table, long total)> FindTableByWithAsync(string sql, DbParameter[] dbParameter, string orderField, bool isAscending, int pageSize, int pageIndex)
        {
            var sqlQuery = GetPageSql(true, sql, orderField, isAscending, pageSize, pageIndex);

            var ds = await DbContext.SqlDataSetAsync(sqlQuery, dbParameter);
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
        /// <param name="formattableSql">内插sql语句</param>
        /// <returns>返回查询结果集</returns>
        public virtual List<List<T>> FindMultiple<T>(FormattableString formattableSql)
        {
            var (sqlFormat, parameter) = formattableSql.ToDbParameter(this.DatabaseType);

            return FindMultiple<T>(sqlFormat, parameter);
        }

        /// <summary>
        /// 根据sql语句查询返回多个结果集
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="dbParameter">对应参数</param>
        /// <returns>返回查询结果集</returns>
        public virtual List<List<T>> FindMultiple<T>(string sql, params DbParameter[] dbParameter)
        {
            return DbContext.SqlQueryMultiple<T>(sql, dbParameter);
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
        /// <param name="formattableSql">内插sql语句</param>
        /// <returns>返回查询结果集</returns>
        public virtual async Task<List<List<T>>> FindMultipleAsync<T>(FormattableString formattableSql)
        {
            var (sqlFormat, parameter) = formattableSql.ToDbParameter(this.DatabaseType);

            return await FindMultipleAsync<T>(sqlFormat, parameter);
        }

        /// <summary>
        /// 根据sql语句查询返回多个结果集
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="dbParameter">对应参数</param>
        /// <returns>返回查询结果集</returns>
        public virtual async Task<List<List<T>>> FindMultipleAsync<T>(string sql, params DbParameter[] dbParameter)
        {
            return await DbContext.SqlQueryMultipleAsync<T>(sql, dbParameter);
        }
        #endregion
        #endregion

        #region Dispose
        #region Sync
        /// <summary>
        /// 释放资源
        /// </summary>
        public virtual void Dispose()
        {
            if (DbContext != null)
            {
                DbContext.Database.CloseConnection();
                DbContext.Dispose();
            }
        }
        #endregion

        #region Async
        /// <summary>
        /// 释放资源
        /// </summary>
        /// <returns></returns>
        public virtual async ValueTask DisposeAsync()
        {
            if (DbContext != null)
            {
                await DbContext.Database.CloseConnectionAsync();
                await DbContext.DisposeAsync();
            }
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
        public abstract string GetPageSql(bool isWithSyntax, string sql, string orderField, bool isAscending, int pageSize, int pageIndex);
        #endregion
    }
}
