using System;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication3
{
    public partial class Form2 : Form
    {
        UdpTester test;
        public Form2()
        {
            InitializeComponent();
            test = new UdpTester();
        }
        private void Form2_Load(object sender, EventArgs e)
        {
            test.DataRecived += test_DataRecived;
            test.DataSended += test_DataSended;
        }

        void test_DataSended(object sender, DataSendedEventArgs e)
        {
            string content = Encoding.Default.GetString(e.DataBuff, 0, e.DataSize);
            this.Invoke(new Action(() =>
            {
                this.listBox1.Items.Add(string.Format("DataSended:{0}", content));
            }));
        }

        void test_DataRecived(object sender, DataRecivedEventArgs e)
        {
            string content = Encoding.Default.GetString(e.DataBuff, 0, e.DataSize);
            this.Invoke(new Action(() =>
            {
                this.listBox1.Items.Add(string.Format("DataRecived:{0}", content));
            }));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            test.Start(Convert.ToInt32(this.textBox1.Text));
            this.Invoke(new Action(() =>
            {
                this.listBox1.Items.Add(string.Format("端口{0}启动成功:", this.textBox1.Text));
            }));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var rp = new IPEndPoint(IPAddress.Parse("127.0.0.1"), Convert.ToInt32(this.textBox3.Text));
            this.test.SendData(rp, this.textBox2.Text);
        }
    }
    public class UdpTester : IDataEvent<UdpSocket>
    {
        private UdpSocket udp;
        public event EventHandler<DataRecivedEventArgs> DataRecived;
        public event EventHandler<DataSendedEventArgs> DataSended;
        public void Start(int port)
        {
            udp = new UdpSocket();
            udp.OnDataEventHandle = this;
            udp.CreateUdpSocket(port);
            udp.Start();

        }

        #region IDataEvent<UdpSocket> 成员

        public int OnDataRecived(EndPoint remoteHost, byte[] dataBuff, int dataSize)
        {
            if (DataRecived != null)
                DataRecived(this, new DataRecivedEventArgs(remoteHost, dataBuff, dataSize));
            return (dataSize);
        }

        public int OnDataSended(System.Net.EndPoint remoteHost, byte[] dataBuff, int dataSize)
        {
            if (DataSended != null)
                DataSended(this, new DataSendedEventArgs(remoteHost, dataBuff, dataSize));
            return (dataSize);
        }

        #endregion
        public void SendData(EndPoint remoteEP, string content)
        {
            byte[] bytes = Encoding.Default.GetBytes(content);
            udp.SendTo(ref bytes, bytes.Length, remoteEP);
        }
    }
    public class DataSendedEventArgs : EventArgs
    {
        public DataSendedEventArgs(EndPoint remoteHost, byte[] dataBuff, int dataSize)
        {
            RemoteHost = remoteHost;
            DataBuff = dataBuff;
            DataSize = dataSize;
        }
        public EndPoint RemoteHost { get; private set; }
        public byte[] DataBuff { get; private set; }
        public int DataSize { get; private set; }
    }
    public class DataRecivedEventArgs : EventArgs
    {
        public DataRecivedEventArgs(EndPoint remoteHost, byte[] dataBuff, int dataSize)
        {
            RemoteHost = remoteHost;
            DataBuff = dataBuff;
            DataSize = dataSize;
        }
        public EndPoint RemoteHost { get; private set; }
        public byte[] DataBuff { get; private set; }
        public int DataSize { get; private set; }
    }
}
