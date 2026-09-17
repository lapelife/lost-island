// ============================================================
// IPoolable.cs
// 对象池接口 - 可被对象池管理的对象必须实现此接口
// ============================================================

namespace LostIsland.Infrastructure
{
    /// <summary>
    /// 对象池对象接口
    /// 继承此接口的对象可以被ObjectPool管理
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 从池中取出时调用
        /// </summary>
        void OnGetFromPool();

        /// <summary>
        /// 回收回池中时调用
        /// </summary>
        void OnReturnToPool();

        /// <summary>
        /// 是否已经在池中（防止重复回收）
        /// </summary>
        bool IsInPool { get; set; }
    }
}
