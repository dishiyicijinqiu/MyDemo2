using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form3 : Form
    {
        private byte[] result = new byte[1024];
        //设定服务器IP地址  
        IPAddress ip = IPAddress.Parse("127.0.0.1");
        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public Form3()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                clientSocket.Connect(new IPEndPoint(ip, 8885)); //配置服务器IP与端口  
                this.listBox1.Items.Add("连接服务器成功");
            }
            catch
            {
                this.listBox1.Items.Add("连接服务器失败！");
                return;
            }
            //通过clientSocket接收数据  
            int receiveLength = clientSocket.Receive(result);
            Console.WriteLine("接收服务器消息：{0}", Encoding.ASCII.GetString(result, 0, receiveLength));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //通过 clientSocket 发送数据  
            try
            {
                string sendMessage = "client send Message Hellp" + DateTime.Now;
                clientSocket.Send(Encoding.ASCII.GetBytes(sendMessage));
                this.listBox1.Items.Add(string.Format("向服务器发送消息：{0}", sendMessage));
            }
            catch
            {
                clientSocket.Shutdown(SocketShutdown.Receive);
                clientSocket.Close();
            }
        }

        private void Form3_Load(object sender, EventArgs e)
        {

        }

        private void Form3_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (clientSocket == null)
                return;
            if (clientSocket.Connected)
                clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }
    }
}
