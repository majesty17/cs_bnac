using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Text.RegularExpressions;

namespace BNAC
{
    public partial class Form1 : Form
    {
        //软件配置
        private static String VERSION = "20160824";
        //private static String AUTHOR = "Majesty";

        //服务端配置
        private string server_host = "172.22.1.144";
        private int server_port = 10001;
        private int buffer_size = 2048;
        
        //运行状态
        //Zpublic static bool main_running = true;
        private Thread myTh;

        public Form1()
        {
            InitializeComponent();
        }


        //启动初始化
        private void Form1_Load(object sender, EventArgs e) {
            setStatus(0);
            label_ver.Text = "版本:"+Form1.VERSION;


            


            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
        }


        //设置状态:
        //0,未登录
        //1,已登录
        //2，登录中
        public void setStatus(int status) {
            switch (status) { 
                case 0:
                    btn_login.Enabled = true;
                    textBox_passwd.Enabled = true;
                    textBox_username.Enabled = true;
                    label_status.Text = "未登录";
                    break;
                case 1:
                    btn_login.Enabled = false;
                    textBox_passwd.Enabled = false;
                    textBox_username.Enabled = false;
                    label_status.Text = "已登录";
                    this.WindowState = FormWindowState.Minimized;
                    break;
                case 2:
                    btn_login.Enabled = false;
                    textBox_passwd.Enabled = false;
                    textBox_username.Enabled = false;
                    label_status.Text = "登录中...";
                    break;
                default:
                    break;
            }
        }
        //登录按钮
        private void btn_login_Click(object sender, EventArgs e){


            string username = textBox_username.Text.Trim();
            string passwd = textBox_passwd.Text.Trim();

            if (username == null || username.Equals("") || passwd == null || passwd.Equals("")) {
                MessageBox.Show("密码，用户名不能为空！","警告");
                return;
            }




            MyThread myThread = new MyThread(server_host, server_port, username, passwd, buffer_size, this);
            myTh = new Thread(new ThreadStart(myThread.ThreadProc));
            myTh.IsBackground = true;
            myTh.Start();
            //进入连接中状态
            setStatus(2);
        }




        //通知栏单击
        private void notifyIcon1_Click(object sender, EventArgs e) {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
                this.Activate();
                this.ShowInTaskbar = true;
            }
        }


        //窗体大小更改:
        //最小的话，显示通知栏图标；
        private void Form1_SizeChanged(object sender, EventArgs e) {
            if (this.WindowState == FormWindowState.Minimized) {
                notifyIcon1.Visible = true;
                this.ShowInTaskbar = false;
            }
        }

        //点击状态栏图标，恢复窗口
        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e) {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
        }


        //测试
        private void button_test_Click(object sender, EventArgs e) {
            Util.rsaEncrypt(textBox_passwd.Text.Trim());
        }


    }


}
