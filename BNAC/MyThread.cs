using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Net;

namespace BNAC
{
    class MyThread {
        //要用到的属性，也就是我们要传递的参数
        private string name;
        private string passwd;
        private string server;
        private int port;
        private int buffer_size;

        private Form1 mainform;


        //心跳时间 ms
        private int heart_beat = 60000;

        //包含参数的构造函数
        public MyThread(string _server,int _port,string _name, string _passwd,int _buffer,Form1 _form) {
            this.name = _name;
            this.passwd = Util.rsaEncrypt(_passwd);
            this.server = _server;
            this.port = _port;
            this.buffer_size = _buffer;
            this.mainform = _form;
        }


        public void ThreadProc() {
            Console.WriteLine("MyThread:coming!");

            TcpClient tcp = new TcpClient();
            try { 
                tcp.Connect(server, port);
            }
            catch (SocketException e){
                Console.WriteLine("MyThread:TCP连接失败:" + e.Message);
                this.mainform.notifyIcon1.BalloonTipText = "TCP连接失败1，请检查网络！";
                this.mainform.notifyIcon1.ShowBalloonTip(1000);
                this.mainform.setStatus(0);
                return;
            }
            
            if (tcp.Connected==false) {
                Console.WriteLine("MyThread:TCP连接失败;");
                this.mainform.notifyIcon1.BalloonTipText = "TCP连接失败2，请检查网络！";
                this.mainform.notifyIcon1.ShowBalloonTip(1000);
                this.mainform.setStatus(0);
                return;
            }
            //这里应该tcp建链成功了
            try
            {
                Console.WriteLine("MyThread:tcp connect ok!");

                //第一步
                NetworkStream streamToServer = tcp.GetStream();

                byte[] buffer = Encoding.Default.GetBytes("ASK_ENCODE\r\nPLATFORM:MAC\r\nVERSION:1.0.1.22\r\nCLIENTID:BNAC_"+Util.getUuid()+"\r\n\r\n");
                streamToServer.Write(buffer, 0, buffer.Length);
                streamToServer.Flush();

                buffer = new byte[buffer_size];
                int bytesRead = streamToServer.Read (buffer, 0, buffer_size);
                Console.WriteLine("MyThread:read length:"+bytesRead);
                string readout = Encoding.Default.GetString(buffer);
                
                if (readout.StartsWith("601") == false)
                {
                    Console.WriteLine("MyThread:error ASK_ENCODE");
                    throw new Exception();
                }
                //Console.WriteLine("MyThread:serverout:" + readout);

                int xor_num = xor_num = Convert.ToInt32(readout.Substring(15, 1));
                Console.WriteLine("MyThread:xor_num is <<" + xor_num + ">>不知道干嘛的");
                

                //第二步
                buffer = Encoding.Default.GetBytes("OPEN_SESAME\r\nSESAME_MD5:INVALID MD5\r\n\r\n");
                streamToServer.Write(buffer, 0, buffer.Length);
                streamToServer.Flush();
                buffer = new byte[buffer_size];
                bytesRead = streamToServer.Read(buffer, 0, buffer_size);
                Console.WriteLine("MyThread:read length:" + bytesRead);
                readout = Encoding.Default.GetString(buffer);
                if (readout.StartsWith("603") == false)
                {
                    Console.WriteLine("MyThread:error OPEN_SESAME");
                    throw new Exception();
                }
                //Console.WriteLine("MyThread:serverout:" + readout);


                //第三步
                buffer = Encoding.Default.GetBytes("SESAME_VALUE\r\nVALUE:0\r\n\r\n");
                streamToServer.Write(buffer, 0, buffer.Length);
                streamToServer.Flush();
                buffer = new byte[buffer_size];
                bytesRead = streamToServer.Read(buffer, 0, buffer_size);
                Console.WriteLine("MyThread:read length:" + bytesRead);
                readout = Encoding.Default.GetString(buffer);
                if (readout.StartsWith("604") == false)
                {
                    Console.WriteLine("MyThread:error SESAME_VALUE");
                    throw new Exception();
                }
                //Console.WriteLine("MyThread:serverout:" + readout);
                
                //第四步，要发密码了
                buffer = Encoding.Default.GetBytes("AUTH\r\nOS:MAC\r\nUSER:"+name+"\r\nPASS:"+passwd+"\r\nAUTH_TYPE:DOMAIN\r\n\r\n");
                streamToServer.Write(buffer, 0, buffer.Length);
                streamToServer.Flush();
                buffer = new byte[buffer_size];
                bytesRead = streamToServer.Read(buffer, 0, buffer_size);
                Console.WriteLine("MyThread:read length:" + bytesRead);
                readout = Encoding.Default.GetString(buffer);
                MatchCollection vMatchs1 = Regex.Matches(readout, @"SESSION_ID:(\d+)");
                MatchCollection vMatchs2 = Regex.Matches(readout, @"ROLE:(\d+)");
                if (readout.StartsWith("288") == false || vMatchs1.Count==0 || vMatchs2.Count==0){
                    Console.WriteLine("MyThread:error AUTH");
                    throw new Exception();
                }
                //Console.WriteLine("MyThread:serverout:" + readout);
                string session_id = vMatchs1[0].Value.Substring(11);
                string role = vMatchs2[0].Value.Substring(5); 

                Console.WriteLine("MyThread: session id is <" + session_id + ">");
                Console.WriteLine("MyThread: role id is <" + role + ">");


                //第五步：最后挣扎
                string sendstr = "PUSH\r\nTIME:" + Util.getTime(session_id,((System.Net.IPEndPoint)tcp.Client.LocalEndPoint).Address.ToString()) + "\r\nSESSIONID:" + session_id + "\r\nROLE:" + role + "\r\n\r\n";
                //Console.WriteLine("MyThread:last send is <" + sendstr + ">");
                buffer = Encoding.Default.GetBytes(sendstr);
                streamToServer.Write(buffer, 0, buffer.Length);
                streamToServer.Flush();
                buffer = new byte[buffer_size];
                bytesRead = streamToServer.Read(buffer, 0, buffer_size);
                readout = Encoding.Default.GetString(buffer);
                //streamToServer.Close();
                if (readout.StartsWith("220") == false) {
                    Console.WriteLine("MyThread:error PUSH");
                    throw new Exception();
                }
                //Console.WriteLine("MyThread:serverout:" + readout);

                //进入主心跳循环


                this.mainform.notifyIcon1.BalloonTipText = "小海贼登录成功";
                this.mainform.notifyIcon1.ShowBalloonTip(1000);
                this.mainform.setStatus(1);


                int heartbeat_ct = 1;
                while (true){
                    Console.WriteLine("MyThread: circling!");
                    IPEndPoint host = new IPEndPoint(IPAddress.Parse(server), port);
                    UdpClient udp = new UdpClient();
                    
                    sendstr = "KEEP_ALIVE\r\nSESSIONID:" + session_id + "\r\nUSER:" + name + "\r\nAUTH_TYPE:DOMAIN\r\nHEARTBEAT_INDEX:" + heartbeat_ct + "\r\n\r\n";
                    buffer = Encoding.Default.GetBytes(sendstr);

                    int sendret=udp.Send(buffer, buffer.Length, host);
                    Console.WriteLine("MyThread: circling...udp send return:"+sendret);
                    heartbeat_ct++;


                    Thread.Sleep(heart_beat);
                }

            }
            catch (Exception e) {
                this.mainform.notifyIcon1.BalloonTipText = "握手失败，请检查网络！";
                this.mainform.notifyIcon1.ShowBalloonTip(1000);
                this.mainform.setStatus(0);
                Console.WriteLine("except:"+e.Message);
            }


            Console.WriteLine("MyThread:quit!");
            this.mainform.setStatus(0);
        }
    }
}
