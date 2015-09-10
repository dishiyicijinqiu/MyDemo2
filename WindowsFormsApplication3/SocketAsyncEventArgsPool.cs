using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace WindowsFormsApplication3
{
    public class SocketAsyncEventArgsPool
    {
        private Stack<SocketAsyncEventArgs> m_pool;
        private int _Capacity;                            // 池的最大容量
        /// <summary>
        /// 初始化对象池
        /// </summary>
        /// <param name="capacity">对象池中可以保持的最大对象SocketAsyncEventArgs的数量</param>
        public SocketAsyncEventArgsPool(int capacity)
        {
            _Capacity = capacity;
            m_pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        /// <summary>
        /// 将元素推入队列
        /// 初始化时，还需将默认容量的预构建的元素加入池中
        /// 注意：预构建的元素的 Completed 委托必须赋值
        /// </summary>
        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null) { throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null"); }
            lock (m_pool)
            {
                m_pool.Push(item);
            }
        }

        /// <summary>
        /// 从池中获取一个SocketAsyncEventArgs实例
        /// </summary>
        public SocketAsyncEventArgs Pop()
        {
            Debug.WriteLine(string.Format("{0}, m_pool.Count={1}", DateTime.Now, m_pool.Count));
            lock (m_pool)
            {
                return m_pool.Pop();
            }
        }

        // The number of SocketAsyncEventArgs instances in the pool
        public int Count
        {
            get { return m_pool.Count; }
        }

        public int Capacity
        {
            get { return _Capacity; }
        }
    }
}
