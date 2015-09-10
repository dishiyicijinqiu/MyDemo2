using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace WindowsFormsApplication3
{
    public class UdpSocket : IDisposable
    {

        [DllImport("Ws2_32.dll")]
        private static extern IntPtr WSAJoinLeaf(IntPtr sck, IntPtr name, int nameLen, IntPtr lpCallerData, IntPtr lpCalleeData, IntPtr lpSQOS, IntPtr lpGQOS, int dwFlags);

        #region 私有数据成员
        private System.Net.Sockets.Socket m_UdpSocket;

        private System.Net.IPEndPoint m_DefaultBind;                    // 默认绑定目标(本地监听)
        private System.Net.IPEndPoint m_DefaultTarget;                    // 默认发送目标
        private System.Net.IPAddress m_DefaultMultcastGroup;            // 默认加入的组播组

        private SocketAsyncEventArgsPool m_SendArgPool;                    // 发送参数池

        private System.Net.Sockets.SocketAsyncEventArgs m_RecvArgs;        // 接收的默认参数构造
        private byte[] m_RecvBuff;                                        // 接收缓冲

        private long m_TotalPacksByRecv;                                // 收到的总数据包数
        private long m_TotalBytesByRecv;                                // 收到的总字节数
        private long m_TotalBytesBySend;                                // 发出的总字节数
        #endregion

        public IDataEvent<UdpSocket> OnDataEventHandle;                    // 数据处理类必须实现此接口，并将其引用传递到这里

        #region 属性实现代码块
        public long TotalPacksByRecv
        {
            get { return m_TotalPacksByRecv; }
            set { m_TotalPacksByRecv = value; }
        }

        public long TotalBytesByRecv
        {
            get { return m_TotalBytesByRecv; }
            set { m_TotalBytesByRecv = value; }
        }

        public long TotalBytesBySend
        {
            get { return m_TotalBytesBySend; }
            set { m_TotalBytesBySend = value; }
        }

        public IPEndPoint DefaultTarget
        {
            get { return m_DefaultTarget; }
            set { m_DefaultTarget = value; }
        }

        public EndPoint LocalEndPoint
        {
            get
            {
                if (m_UdpSocket != null)
                    return m_UdpSocket.LocalEndPoint;
                else
                    return (null);
            }
        }

        public SocketClient.WorkingStatusConst WorkingStatus
        {
            get
            {
                if (m_UdpSocket == null)
                    return (SocketClient.WorkingStatusConst.cs_NotCreated);
                else
                    return (SocketClient.WorkingStatusConst.cs_Connected);
            }
        }

        #endregion

        public UdpSocket()
            : this(null, null, null)
        {
        }

        /// <summary>
        /// 同时设定默认的发送目标
        /// </summary>
        public UdpSocket(System.Net.IPEndPoint defaultTarget)
            : this(defaultTarget, null, null)
        {
        }

        /// <summary>
        /// 构造函数
        /// 同时设定默认的发送目标、需要加入的广播组地址
        /// </summary>
        public UdpSocket(System.Net.IPEndPoint defaultTarget, IPAddress multicastGroupAddr)
            : this(defaultTarget, multicastGroupAddr, null)
        {
        }


        /// <summary>
        /// 构造函数
        /// 同时设定默认的发送目标、需要加入的广播组地址、绑定的本地地址
        /// </summary>
        public UdpSocket(System.Net.IPEndPoint defaultTarget, IPAddress multicastGroupAddr, System.Net.IPEndPoint defaultBindingEndPoint)
        {
            m_DefaultTarget = defaultTarget;
            m_DefaultMultcastGroup = multicastGroupAddr;
            m_DefaultBind = defaultBindingEndPoint;

            m_SendArgPool = new SocketAsyncEventArgsPool(8);
            for (int i = 0; i < 8; i++)
            {
                SocketAsyncEventArgs ae = new SocketAsyncEventArgs();
                ae.UserToken = this;
                ae.Completed += new EventHandler<SocketAsyncEventArgs>(Udp_OnSendCompleted);
                m_SendArgPool.Push(ae);
            }
        }

        #region 创建UdpSocket

        // 绑定到本地的IP地址/端口号
        public bool CreateUdpSocket(int nLocalPort)
        {
            return (CreateUdpSocket(new IPEndPoint(IPAddress.Any, nLocalPort)));
        }

        /// <summary>
        /// 绑定到本地的IP地址/端口号
        /// </summary>
        /// <param name="sLocalAddr"></param>
        /// <param name="nLocalPort"></param>
        /// <returns></returns>
        public bool CreateUdpSocket(string sLocalAddr, int nLocalPort)
        {
            Debug.Assert(m_UdpSocket == null, "UDP已经建立，调用顺序可能出错。");

            if ((String.IsNullOrEmpty(sLocalAddr)) || (sLocalAddr == "127.0.0.1"))
            {
                m_DefaultBind = new IPEndPoint(IPAddress.Any, nLocalPort);
            }
            else
            {
                m_DefaultBind = new IPEndPoint(IPAddress.Parse(sLocalAddr), nLocalPort);
            }

            return (CreateUdpSocket(m_DefaultBind));
        }

        /// <summary>
        /// 绑定到本地的IP地址/端口号
        /// </summary>
        public bool CreateUdpSocket(IPEndPoint bindAddr)
        {
            Debug.Assert(m_UdpSocket == null, "UDP已经建立，调用顺序可能出错。");

            m_DefaultBind = bindAddr;
            try
            {
                m_UdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                m_UdpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                m_UdpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                m_UdpSocket.DontFragment = true;
                m_UdpSocket.EnableBroadcast = true;
                m_UdpSocket.Ttl = 5;

                m_UdpSocket.Bind(bindAddr);

                if (m_DefaultMultcastGroup != null)
                {
                    JoinMemberShip(m_DefaultMultcastGroup);
                }

                m_RecvBuff = new byte[1500];
                m_RecvArgs = new SocketAsyncEventArgs();
                m_RecvArgs.UserToken = this;
                m_RecvArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                m_RecvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(this.Udp_OnReceiveCompleted);

            }
            catch (SocketException exp)
            {
                throw new Exception(string.Format("CreateUdpSocket()运行时异常:DefaultBind={0},DefaultMultcastGroup={1}", m_DefaultBind, m_DefaultMultcastGroup),
                                    exp);
            }

            return (true);
        }

        #endregion 创建UdpSocket

        // 启动异步接收进程
        public bool Start()
        {
            Debug.Assert(m_UdpSocket != null, "UDP尚未建立，需要首先调用CreateUdpSocket()方法创建此对象.");

            PostReceive();

            return (true);
        }

        /// <summary>
        /// 重新启动 UdpClient
        /// </summary>
        /// <returns></returns>
        private bool ReStart()
        {
            if (m_UdpSocket != null)
            {
                m_UdpSocket.Close();
                m_UdpSocket = null;
            }
            try
            {
                CreateUdpSocket(m_DefaultBind);

            }
            catch (SocketException e)
            {
                Debug.Assert(false, e.ToString());
            }

            Start();
            return (true);
        }

        public bool Stop()
        {
            if (m_UdpSocket != null)
            {
                m_UdpSocket.Close();
                m_UdpSocket = null;
            }
            return (true);
        }

        /// <summary>
        /// 加入组播组
        /// 必须在UdpSocket与本地地址 Bind 之后，Start 之前才可以加入组播组
        /// </summary>
        public bool JoinMemberShip(IPAddress groupAddress)
        {
            if (m_UdpSocket == null) return (false);

            try
            {
                //MulticastOption multicastOption = new MulticastOption(groupAddress);
                //m_UdpSocket.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multicastOption);

                WinsockSockAddr sckaddr = new WinsockSockAddr(groupAddress);
                WSAJoinLeaf(this.m_UdpSocket.Handle, sckaddr.PinnedSockAddr, Marshal.SizeOf(typeof(WinsockSockAddr.SOCKADDR_IN)), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 4);
            }
            catch (SocketException e)
            {
                Debug.Print(e.ToString());
            }


            return (true);
        }

        public bool LeaveMemberShip(IPAddress groupAddress)
        {
            if (m_UdpSocket == null) return (false);
            try
            {
                MulticastOption multicastOption = new MulticastOption(groupAddress);
                m_UdpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, multicastOption);
            }
            catch (SocketException e)
            {
                Debug.Print(e.ToString());
            }
            return (true);
        }

        #region 异步发送、接收数据代码块
        public void SendTo(ref byte[] data, int dataSize)
        {
            SendTo(ref data, dataSize, m_DefaultTarget);
        }

        public void SendTo(ref byte[] data, int dataSize, EndPoint remoteEP)
        {
            SocketAsyncEventArgs m_SendArgs = m_SendArgPool.Pop();
            m_SendArgs.RemoteEndPoint = remoteEP;
            m_SendArgs.SetBuffer(data, 0, dataSize);

            //m_SendArgs.UserToken = this;
            //m_SendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(Udp_OnSendCompleted);

            bool block = m_UdpSocket.SendToAsync(m_SendArgs);

            Debug.WriteLine(string.Format("UdpSocket::SendTo={0}", block));
        }

        private void PostReceive()
        {
            bool block = false;

            while (block == false)
            {
                m_RecvArgs.SetBuffer(m_RecvBuff, 0, m_RecvBuff.Length);
                block = m_UdpSocket.ReceiveFromAsync(m_RecvArgs);

                Debug.WriteLine(string.Format("UdpSocket::ReceiveFromAsync={0}", block));
            }

        }

        private void Udp_OnReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            UdpSocket udp = (UdpSocket)e.UserToken;
            Socket sck = (Socket)udp.m_UdpSocket;

            if (e.SocketError == SocketError.Success)
            {

                IPEndPoint remoteEP = (IPEndPoint)e.RemoteEndPoint;

                byte[] buff = e.Buffer;

                if (buff != null)
                {
                    m_TotalPacksByRecv++;
                    m_TotalBytesByRecv += e.BytesTransferred;
                    if (OnDataEventHandle != null)
                    {
                        try
                        {
                            OnDataEventHandle.OnDataRecived(remoteEP, buff, e.BytesTransferred);
                        }
                        catch (NullReferenceException e1)
                        {
                            Debug.Print("Udp_OnReceiveCompleted() 出现 NullReferenceException 错误：" + e1.ToString());
                        }
                        catch (Exception e1)
                        {
                            Debug.Print("Udp_OnReceiveCompleted() 错误：" + e1.ToString());
                        }
                        finally
                        {
                            // Do nothing
                        }
                    }

                }
            }

            // 发出下一个接收动作
            PostReceive();

        }

        private void Udp_OnSendCompleted(object sender, SocketAsyncEventArgs e)
        {
            Socket sck = (Socket)m_UdpSocket;

            if (e.Buffer != null)
            {
                m_TotalBytesBySend += e.Buffer.Length;

                if (OnDataEventHandle != null)
                {
                    try
                    {
                        OnDataEventHandle.OnDataSended(e.RemoteEndPoint, e.Buffer, e.Buffer.Length);
                    }
                    catch (SocketException e1)
                    {
                        switch (e1.SocketErrorCode)
                        {
                            case SocketError.HostUnreachable:            // 这种错误直接忽略即可
                                break;
                            case SocketError.HostNotFound:                // 这种错误直接忽略即可
                                break;
                            case SocketError.NetworkUnreachable:
                                Debug.Assert(false, "UdpSocket::SendCallback()出现错误：" + e1.ToString());
                                break;
                            default:
                                Debug.Assert(false, "UdpSocket::SendCallback()出现错误：" + e1.ToString());
                                break;
                        }
                    }
                }
            }

            m_SendArgPool.Push(e);
        }

        #endregion 异步发送、接收数据代码块

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }

            Stop();

            m_UdpSocket = null;
            OnDataEventHandle = null;
        }

        #region IDisposable 成员

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
    public interface IDataEvent<T>
    {
        int OnDataRecived(System.Net.EndPoint remoteHost, byte[] dataBuff, int dataSize);
        int OnDataSended(System.Net.EndPoint remoteHost, byte[] dataBuff, int dataSize);
    }
    public sealed unsafe class WinsockSockAddr
    {
        const Int16 AF_INET = 2;
        const Int16 AF_INET6 = 23;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct SOCKADDR_IN
        {
            public Int16 _family;
            public Int16 _port;
            public Byte _addr0;
            public Byte _addr1;
            public Byte _addr2;
            public Byte _addr3;
            public Int32 _nothing;
        }

        static readonly int SIZEOF_SOCKADDR_IN = Marshal.SizeOf(typeof(SOCKADDR_IN));

        [StructLayout(LayoutKind.Sequential)]
        internal struct SOCKADDR_IN6
        {
            public Int16 _family;
            public Int16 _port;
            public Int32 _flowInfo;
            public Byte _addr0;
            public Byte _addr1;
            public Byte _addr2;
            public Byte _addr3;
            public Byte _addr4;
            public Byte _addr5;
            public Byte _addr6;
            public Byte _addr7;
            public Byte _addr8;
            public Byte _addr9;
            public Byte _addr10;
            public Byte _addr11;
            public Byte _addr12;
            public Byte _addr13;
            public Byte _addr14;
            public Byte _addr15;
            public Int32 _scopeID;
        }

        static readonly int SIZEOF_SOCKADDR_IN6 = Marshal.SizeOf(typeof(SOCKADDR_IN6));

        // Depending on the family type of address represented, either a SOCKADDR_IN
        // or a SOCKADDR_IN6 will be referenced by _addr.  We'll pin the same object
        // to _pinAddr, and finally keep a IntPtr to the alloc.
        object _addr;
        GCHandle _pinAddr;
        IntPtr _pAddr;

        public WinsockSockAddr(IPEndPoint source)
            : this(source.Address, (short)source.Port)
        {
        }

        public WinsockSockAddr(IPAddress source)
            : this(source, 0)
        {
        }

        public WinsockSockAddr(IPAddress source, short port)
        {
            _pAddr = (IntPtr)0;

            if (source.AddressFamily == AddressFamily.InterNetwork)
            {
                SOCKADDR_IN a;
                Byte[] addr = source.GetAddressBytes();
                Debug.Assert(addr.Length == 4);

                a._family = AF_INET;
                a._port = IPAddress.HostToNetworkOrder(port);
                a._addr0 = addr[0];
                a._addr1 = addr[1];
                a._addr2 = addr[2];
                a._addr3 = addr[3];
                a._nothing = 0;

                _addr = a;
            }
            else if (source.AddressFamily == AddressFamily.InterNetworkV6)
            {
                SOCKADDR_IN6 a;
                Byte[] addr = source.GetAddressBytes();
                Debug.Assert(addr.Length == 16);

                a._family = AF_INET6;
                a._port = IPAddress.HostToNetworkOrder(port);
                a._flowInfo = 0;
                a._addr0 = addr[0];
                a._addr1 = addr[1];
                a._addr2 = addr[2];
                a._addr3 = addr[3];
                a._addr4 = addr[4];
                a._addr5 = addr[5];
                a._addr6 = addr[6];
                a._addr7 = addr[7];
                a._addr8 = addr[8];
                a._addr9 = addr[9];
                a._addr10 = addr[10];
                a._addr11 = addr[11];
                a._addr12 = addr[12];
                a._addr13 = addr[13];
                a._addr14 = addr[14];
                a._addr15 = addr[15];
                a._scopeID = (Int32)source.ScopeId;

                _addr = a;
            }
            else
            {
                throw new ArgumentException();
            }

            _pinAddr = GCHandle.Alloc(_addr, GCHandleType.Pinned);
            _pAddr = _pinAddr.AddrOfPinnedObject();
        }


        void Close()
        {
            if (_pinAddr.IsAllocated)
            {
                _pinAddr.Free();
            }

            _addr = null;
            _pAddr = (IntPtr)0;
        }

        ~WinsockSockAddr()
        {
            Close();
        }

        public IntPtr PinnedSockAddr
        { get { return _pAddr; } }
    }
    public class SocketClient
    {
        public enum WorkingStatusConst : ushort
        {
            cs_NotCreated = 0x8000, // 套接字尚未创建
            cs_Created = 0x0000, // 套接字已创建（已与本地IP/Port绑定），但尚未连接
            cs_NotConnected = 0x0001,
            cs_Connecting = 0x0002,
            cs_Connected = 0x0004,
            cs_DisConnecting = 0x0008, // 正在断开连接过程中
            cs_SilentMoment = 0x0010, // 静默阶段：一般用于在socket被对方断开后，静默一段时间再重新建立连接

            cs_IsDisposed = cs_NotCreated, // 别名：表示底层套结字无法访问
        };
    }
}
