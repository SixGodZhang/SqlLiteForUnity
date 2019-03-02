using System;

namespace SqlLite
{
    /// <summary>
    /// 缓存的记录项主体
    /// </summary>
    public class CacheItem
    {
        /// <summary>
        /// 缓存的数据
        /// </summary>
        public byte[] Data;

        /// <summary>
        /// 增加到缓存中的UTC时间戳
        /// </summary>
        public long AddedToCacheTicksUtc;

        /// <summary>
        /// 上一次更新的时间
        /// </summary>
        public DateTime? LastModified;
    }
}
