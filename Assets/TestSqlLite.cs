using SqlLite;
using System;
using System.IO;
using UnityEngine;

namespace Assets
{
    class TestSqlLite:MonoBehaviour
    {
        private void Start()
        {
            //创建SqLite数据库的实例
            SqlLiteCache cache = new SqlLiteCache();

            //重置数据库,删除所有数据
            //cache.ReInit();

            //开启debug收集
            //cache.GetDebugMessage();

            //byte[] data = File.ReadAllBytes(Path.Combine(Application.dataPath, "earth.jpg"));


            //////////向SqlLite中添加数据
            //for (int i = 0; i < 1000; i++)
            //{
            //    cache.Add("zhangsan" + i, new CacheItem()
            //    {
            //        Data = new byte[] { 1, 2, 3, 4,(byte)new System.Random().Next(1,100) }
            //    }
            //    );
            //}

            //记录总数
            //RecordCount
            //Debug.Log("record count: " + cache.RecordCount());

            ////从根据键值从sqllite中取数据
            //CacheItem item = cache.GetItem("zhangsan55");
            //Debug.Log(item?.AddedToCacheTicksUtc);

            ////删除sqlLite中的数据
            //cache.Delete("zhangsan33");
            //Debug.Log("clear end");

            //清空sqlLite中的数据
            //cache.Clear();


            Debug.Log("end");
        }
    }
}
