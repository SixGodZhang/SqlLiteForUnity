namespace SqlLite
{
    public interface ICache
    {
        /// <summary>
        /// sqlLite 存储的最大的记录项数目
        /// </summary>
        uint MaxCacheSize { get; }

        /// <summary>
        /// 增加数据
        /// </summary>
        /// <param name="name">Name,数据项名称</param>
        /// <param name="cacheItem">记录项主体</param>
        /// <param name="replaceIfExists">是否覆盖已存在的记录项</param>
        void Add(string name, CacheItem cacheItem, bool replaceIfExists);

        /// <summary>
        /// 从SqlLite中取得数据
        /// </summary>
        /// <param name="id">ID,唯一标识</param>
        /// <returns></returns>
        CacheItem GetItem(string id);

        /// <summary>
        /// 清空SqlLite中的所有数据
        /// </summary>
        void Clear();

        /// <summary>
        /// 清空SqlLite中对应ID的记录项
        /// </summary>
        /// <param name="id"></param>
        void Delete(string id);

        /// <summary>
        /// 重新初始化SqlLite,通常在Clear之后调用
        /// </summary>
        void ReInit();
    }
}