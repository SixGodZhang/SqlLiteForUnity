# SqlLiteForUnity

- [项目介绍](#项目介绍)
- [项目目录结构及功能介绍](#项目目录结构及功能介绍)
- [SqlLite中具体操作介绍](#SqlLite中具体操作介绍)


# 项目介绍
此项目是基于Unity和Sqllite,目的主要有两个:
1. 跨平台实现数据的存储
2. no sql. 无sql语句，轻松实现数据的操作

一般来说，使用sqllite做本地存储的数据量不是很大，所以为了实现封装各种不同的数据结构，我将所有的数据全部以blob结构存储，这样一来就解决了数据的差异性，在存储时我会使用一个id和和一个key唯一标识这条记录，所有的CRUD都可以基于key值。所以我只需要记录每条记录的key即可拿到其在sqlite中对应的数据。

# 项目目录结构及功能介绍
SqlLite  
|-------Cache	数据缓存  
        |-----RecordSets	缓存的数据结构  
        |-----SqliteCache	与Sqllite的交互操作  
|-------Core	交互操作核心代码  
		|-----SQLite	核心  
|-------CacheItem	客户端缓存的数据结构  
|-------ICache	封装的一些操作接口  

# SqlLite中具体操作介绍

## 创建数据库实例
``` csharp
            SqlLiteCache cache = new SqlLiteCache();
```

## 添加数据
``` csharp
            //////向SqlLite中添加数据
            cache.Add("zhangsan", new CacheItem()
            {
                Data = new byte[] { 1,2,3,4},
            });
```

## 修改数据
``` csharp
            cache.Add("zhangsan", new CacheItem()
            {
                Data = new byte[] { 1,2,3,4},
            },
            true);
```

## 取数据
``` csharp
            CacheItem item = cache.GetItem("zhangsan");
            Debug.Log(item?.AddedToCacheTicksUtc);
```

## 删除数据
``` csharp
            cache.Delete("d3f1f50d-1b94-4bfc-b356-699710f72807");
            Debug.Log("clear end");
```

## 清理数据库文件
``` csharp
            cache.Clear();
```

测试测试