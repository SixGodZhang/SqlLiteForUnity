using SQLite4Unity3d;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SqlLite
{
    public class SqlLiteCache : ICache, IDisposable
    {
        private readonly uint _maxCacheSize;
        /// <summary>
        /// 每次向数据库中存储的记录项数目,避免频繁的IO操作
        /// </summary>
        private const int _pruneCacheDelta = 20;
        /// <summary>
        /// 计数器(记录缓存的记录数目)
        /// </summary>
        private int _pruneCacheCounter = 0;

        /// <summary>
        /// 数据库实力是否被释放
        /// </summary>
        private bool _disposed;
        /// <summary>
        /// 数据库名称
        /// </summary>
        private string _dbName;
        /// <summary>
        /// 数据库路径
        /// </summary>
        private string _dbPath;
        /// <summary>
        /// 数据库实例
        /// </summary>
        private SQLiteConnection _sqlite;

        /// <summary>
        /// 锁
        /// </summary>
        private object _lock = new object();

        /// <summary>
        /// 数据库中最大缓存的数目
        /// </summary>
        public uint MaxCacheSize { get { return _maxCacheSize; } }
        /// <summary>
        /// 内存中缓存的数据量
        /// </summary>
        public int PruneCacheDelta { get { return _pruneCacheDelta; } }


        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="recordItemMax"></param>
        /// <param name="dbName"></param>
        public SqlLiteCache(uint? recordItemMax = null, string dbName = "cache.db")
        {
            _maxCacheSize = recordItemMax ?? 3000;
            _dbName = dbName;
            Init();
        }

        /// <summary>
        /// 获取调试信息
        /// </summary>
        public void GetDebugMessage()
        {
            _sqlite.Trace = true;
        }

        /// <summary>
        /// SqlLite 初始化
        /// </summary>
        private void Init()
        {
            //创建数据库文件
            OpenOrCreateDb(_dbName);

            //创建索引表
            //hrmpf: multiple PKs not supported by sqlite.net
            //https://github.com/praeclarum/sqlite-net/issues/282
            //do it via plain SQL

            List<SQLiteConnection.ColumnInfo> colInfoTileset = _sqlite.GetTableInfo(typeof(recordindextabel).Name);
            if (0 == colInfoTileset.Count)
            {
                string cmdCreateTableTilesets = @"CREATE TABLE recordindextabel(
id    INTEGER PRIMARY KEY ASC AUTOINCREMENT NOT NULL UNIQUE,
name  STRING  NOT NULL
);";
                _sqlite.Execute(cmdCreateTableTilesets);
                string cmdCreateIdxNames = @"CREATE UNIQUE INDEX idx_names ON recordindextabel (name ASC);";
                _sqlite.Execute(cmdCreateIdxNames);
            }

            List<SQLiteConnection.ColumnInfo> colInfos = _sqlite.GetTableInfo(typeof(RecordSets).Name);
            if (0 == colInfos.Count)
            {
                string cmdCreateTableSql = @"CREATE TABLE RecordSets(
recordid INTEGER REFERENCES recordindextabel (id) ON DELETE CASCADE ON UPDATE CASCADE,
timestamp INTEGER NOT NULL,
data BLOB ,
lastmodified INTEGER);";
                _sqlite.Execute(cmdCreateTableSql);
                string cmdCreateIdxNamesSql = @"CREATE UNIQUE INDEX idx_recordid ON RecordSets (recordid ASC);";
                _sqlite.Execute(cmdCreateIdxNamesSql);
            }

            // some pragmas to speed things up a bit :-)
            // inserting 1,000 tiles takes 1-2 sec as opposed to ~20 sec
            string[] cmds = new string[]
            {
                "PRAGMA synchronous=OFF",
                "PRAGMA count_changes=OFF",
                "PRAGMA journal_mode=MEMORY",
                "PRAGMA temp_store=MEMORY"
            };
            foreach (var cmd in cmds)
            {
                try
                {
                    _sqlite.Execute(cmd);
                }
                catch (SQLiteException ex)
                {
                    // workaround for sqlite.net's exeception:
                    // https://stackoverflow.com/a/23839503
                    if (ex.Result != SQLite3.Result.Row)
                    {
                        UnityEngine.Debug.LogErrorFormat("{0}: {1}", cmd, ex);
                    }
                }
            }

        }

        /// <summary>
        /// 打开/创建一个数据库连接
        /// </summary>
        /// <param name="dbName"></param>
        private void OpenOrCreateDb(string dbName)
        {
            _dbPath = GetFullPath(dbName);
            _sqlite = new SQLiteConnection(_dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
#if UNITY_EDITOR
            Debug.LogFormat("SQLLite Cache path----->{0}", _dbPath);
#endif
        }

        /// <summary>
        /// 获取数据库文件的完整路径(不同平台下路径不一样)
        /// </summary>
        /// <param name="dbName"></param>
        /// <returns></returns>
        private string GetFullPath(string dbName)
        {
            string dbPath = Path.Combine(Application.persistentDataPath, "cache");
#if UNITY_EDITOT_WIN || UNITY_STANDALONE_WIN || UNITY_WSA
            dbPath = Path.GetFullPath(dbPath);
#endif
            if (!Directory.Exists(dbPath)) Directory.CreateDirectory(dbPath);
            dbPath = Path.Combine(dbPath, dbName);

            return dbPath;
        }

        public void insert(string name, CacheItem cacheItem)
        {
            try
            {
                _sqlite.BeginTransaction(true);

                int rowsAffected = _sqlite.Insert(cacheItem);
                if (1 != rowsAffected)
                {
                    throw new Exception(string.Format("tileset [{0}] was not inserted, rows affected:{1}", cacheItem.LastModified, rowsAffected));
                }
            }
            catch (Exception ex)
            {
                Debug.LogErrorFormat("could not insert tileset []: {0}",ex);
            }
            finally
            {
                _sqlite.Commit();
            }
        }

        /// <summary>
        /// 记录数目
        /// </summary>
        /// <param name="tilesetName"></param>
        /// <returns></returns>
        public long RecordCount()
        {
            return _sqlite
                .Table<RecordSets>()
                .Count();
        }

        /// <summary>
        /// 根据键值插入数据
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private int insertIndexTable(string name)
        {
            try
            {
                _sqlite.BeginTransaction(true);
                //return _sqlite.Insert(new tilesets { name = tilesetName });
                recordindextabel newTileset = new recordindextabel { name = name };
                int rowsAffected = _sqlite.Insert(newTileset);
                if (1 != rowsAffected)
                {
                    throw new Exception(string.Format("tileset [{0}] was not inserted, rows affected:{1}", name, rowsAffected));
                }
                return newTileset.id;
            }
            catch (Exception ex)
            {
                Debug.LogErrorFormat("could not insert tileset [{0}]: {1}", name, ex);
                return -1;
            }
            finally
            {
                _sqlite.Commit();
            }
        }


        /// <summary>
        /// 向SqlLite中添加数据
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cacheItem"></param>
        /// <param name="replaceIfExists"></param>
        public void Add(string name, CacheItem cacheItem, bool replaceIfExists = false)
        {

            //if data exist && no replace
            int? recordID = GetRecordID(name);
            if (recordID.HasValue && !replaceIfExists)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogFormat("name: {0} exist!", name);
#endif
                return;
            }

            //if data no exist or (data exist && replace)
            int? record_id = null;
            lock (_lock)
            {
                lock (_lock)
                {
                    record_id = GetRecordID(name);
                    if (!record_id.HasValue)
                    {
                        record_id = insertIndexTable(name);
                    }
                }
            }

            if (record_id < 0)
            {
                Debug.LogErrorFormat("could not get recordid for [{0}] tile: {1}", name, record_id);
                return;
            }

            //recordID = InsertRecord(name);
            int rowEffected = _sqlite.InsertOrReplace(new RecordSets
            {
                recordid = record_id.Value,
                timestamp = (int)DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds,
                data = cacheItem.Data,
                lastmodified = 0
            });

            if (1 != rowEffected)
                throw new Exception("Insert data Failed!");

            //检查溢出
            if (!replaceIfExists)
                _pruneCacheCounter++;

            if (0 == _pruneCacheCounter % _pruneCacheDelta)
            {
                _pruneCacheCounter = 0;
                Prune();
            }
        }

        /// <summary>
        /// 删除多余数据，保持数据在一个峰值(先进先出)
        /// </summary>
        private void Prune()
        {
            long count = _sqlite.ExecuteScalar<long>("SELECT COUNT(recordid) FROM RecordSets");
            if (count < _maxCacheSize) return;
            long toDelete = count - _maxCacheSize;
            try
            {
                // no 'ORDER BY' or 'LIMIT' possible if sqlite hasn't been compiled with 'SQLITE_ENABLE_UPDATE_DELETE_LIMIT'
                // https://sqlite.org/compile.html#enable_update_delete_limit
                // int rowsAffected = _sqlite.Execute("DELETE FROM tiles ORDER BY timestamp ASC LIMIT ?", toDelete);
                _sqlite.Execute("DELETE FROM RecordSets WHERE rowid IN ( SELECT rowid FROM RecordSets ORDER BY timestamp ASC LIMIT ? );", toDelete);
            }
            catch (Exception ex)
            {
                Debug.LogErrorFormat("error pruning: {0}", ex);
            }
        }

        /// <summary>
        /// 清空数据库文件
        /// </summary>
        public void Clear()
        {
            //already disposed
            if (null == _sqlite) { return; }

            _sqlite.Close();
            _sqlite.Dispose();
            _sqlite = null;

            Debug.LogFormat("deleting {0}", _dbPath);

            // try several times in case SQLite needs a bit more time to dispose
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    File.Delete(_dbPath);
                    return;
                }
                catch
                {
#if !WINDOWS_UWP
                    System.Threading.Thread.Sleep(100);
#else
					System.Threading.Tasks.Task.Delay(100).Wait();
#endif
                }
            }

            // if we got till here, throw on last try
            File.Delete(_dbPath);
        }

        /// <summary>
        /// 根据键值从sqllite中删除数据
        /// </summary>
        /// <param name="id"></param>
        public void Delete(string key)
        {
            int? id = GetID(key);

            if (!id.HasValue)
                return;

            _sqlite.Delete<recordindextabel>(id.Value);
        }

        /// <summary>
        /// 根据ID从SqlLite中取数据
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public CacheItem GetItem(string key)
        {
            RecordSets recordUtil = null;

            try
            {
                int? recordID = GetRecordID(key);
                if (!recordID.HasValue)
                {
                    return null;
                }
                recordUtil = _sqlite
                    .Table<RecordSets>()
                    .Where(t =>
                        t.recordid == recordID.Value
                        )
                    .FirstOrDefault();

                DateTime? lastModified = null;
                if (recordUtil.lastmodified.HasValue) { lastModified = UnixTimestampUtils.From((double)recordUtil.lastmodified.Value); }

                return new CacheItem()
                {
                    Data = recordUtil.data,
                    AddedToCacheTicksUtc = recordUtil.timestamp,
                    LastModified = lastModified
                };

            }
            catch(Exception ex)
            {
                Debug.Log("ex: " + ex.Message);
                return null;
            }
         }

        /// <summary>
        /// 根据键值从sqllite中取id
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public int? GetID(string key)
        {
            var recordSets = _sqlite
            .Table<recordindextabel>()
            .Where(ts => ts.name.Equals(key))
            .FirstOrDefault();

            return recordSets?.id;
        }

        /// <summary>
        /// 重置数据库
        /// </summary>
        public void ReInit()
        {
            if (null != _sqlite)
            {
                _sqlite.Dispose();
                _sqlite = null;
            }

            //此处直接删除数据库文件
            Clear();

            Init();
        }

        /// <summary>
        /// 根据键值查询记录是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool RecordExistByKey(string key)
        {
            var recordSets = _sqlite
                .Table<recordindextabel>()
                .Where(ts => ts.name.Equals(key))
                .FirstOrDefault();

            return recordSets != null;
        }

        /// <summary>
        /// 根据名字获取record在表中的ID
        /// </summary>
        /// <param name="recordName"></param>
        /// <returns></returns>
        private int? GetRecordID(string recordName)
        {
            var recordSets = _sqlite
                .Table<recordindextabel>()
                .Where(rn => rn.name.Equals(recordName))
                .FirstOrDefault();
            return recordSets == null ? (int?)null : recordSets.id;
        }

        ~SqlLiteCache()
        {
            Dispose(false);
        }

        private void Dispose(bool disposeManagedResources)
        {
            if (!_disposed)
            {
                if (disposeManagedResources && null == _sqlite)
                {
                    // compact db to keep file size small
                    _sqlite.Execute("VACUUM;");
                    _sqlite.Close();
                    _sqlite.Dispose();
                    _sqlite = null;
                }

                _disposed = true;
            }
            
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}