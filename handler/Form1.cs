﻿using controller.util;
using handler.util;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace handler
{
    public partial class form1 : Form
    {
        private string pathShare;   //共享路径
        private int no;     //编号
        private int delay;  //拨号延时
        private int overTime;   //超时
        private Thread main;    //主线程
        private Thread hwndThread;  //用来处理句柄阻塞事件线程
        private IntPtr threadHwnd;  //线程句柄
        private string taskName;    //任务名
        private string projectName;     //用于核对taskName
        private string taskPath;    //任务路径
        private string customPath = "";  //本地任务路径
        private string taskChange;  //任务切换标识
        private string cacheMemory; //项目缓存
        private string workerId;    //工号
        private string inputId;     //输入工号标识
        private string tail;    //分号标识
        private RASDisplay ras; //ADSL对象
        private string adslName;    //拨号名称
        private bool isAutoVote = false; //自动投票标识
        private bool failTooMuch = false;//失败太多标识
        private int timerChecked = 0;//timer检测标识
        private int succCount = 0;//成功数
        private int overTimeCount = 0; //投票超时次数
        private bool ie8 = isIE8();
        private string jiutianCode = "Afx:400000:b:10011:1900015:0";
        private string workingPath = Environment.CurrentDirectory; //当前工作路径

        private const string TASK_SYS_UPDATE = "Update";
        private const string TASK_SYS_WAIT_ORDER = "待命";
        private const string TASK_SYS_SHUTDOWN = "关机";
        private const string TASK_SYS_RESTART = "重启";
        private const string TASK_SYS_NET_TEST = "网络测试";
        private const string TASK_VOTE_JIUTIAN = "九天";
        private const string TASK_VOTE_YUANQIU = "圆球";
        private const string TASK_VOTE_MM = "MM";
        private const string TASK_VOTE_JT = "JT";
        private const string TASK_VOTE_ML = "ML";
        private const string TASK_VOTE_DM = "DM";
        private const string TASK_VOTE_JZ = "JZ";
        private const string TASK_VOTE_HY = "HY";
        private const string TASK_VOTE_OUTDO = "Outdo";
        private const string TASK_VOTE_PROJECT = "投票项目";

        public form1()
        {
            InitializeComponent();
            Hotkey.RegisterHotKey(this.Handle, 10, Hotkey.MODKEY.None, Keys.F10);
            Hotkey.RegisterHotKey(this.Handle, 11, Hotkey.MODKEY.None, Keys.F9);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312)
            {
                switch (m.WParam.ToInt32())
                {
                    case 10:
                        button2_Click(null, null);
                        button1_Click(null, null);
                        break;
                    case 11:
                        button2_Click(null, null);
                        break;
                    default:
                        break;
                }
                return;
            }
            base.WndProc(ref m);
        }
        

        //启动程序，检查配置文件，启动主线程
        private void Form1_Load(object sender, EventArgs e)
        {

            if (File.Exists(@".\handler.ini"))
            {
                pathShare = IniReadWriter.ReadIniKeys("Command", "gongxiang", "./handler.ini");
                no = int.Parse(IniReadWriter.ReadIniKeys("Command", "bianhao", "./handler.ini"));
                adslName = IniReadWriter.ReadIniKeys("Command", "adslName", "./handler.ini");
                try
                {
                    delay = int.Parse(IniReadWriter.ReadIniKeys("Command", "yanchi", "./handler.ini"));
                }
                catch (Exception)
                {
                    delay = 0;
                }
                textBox1.Text = no.ToString();
                button1_Click(null, null);
            }
            else
            {
                MessageBox.Show("请设置handler.ini");
                Close();
            }
        }

        //改变编号
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            IniReadWriter.WriteIniKeys("Command", "bianhao", textBox1.Text, "./handler.ini");
        }

        //启动主线程
        public void button1_Click(object sender, EventArgs e)
        {
            notifyIcon1.Icon =(Icon) Properties.Resources.ResourceManager.GetObject("running");
            button1.Enabled = false;
            button2.Enabled = true;
            writeLogs(workingPath + "/log.txt", "");//清空日志
            this.timer1.Enabled = true;
            this.WindowState = FormWindowState.Minimized;
            main = new Thread(_main);
            main.Start();
        }


        //终止主线程

        private void mainThreadClose()
        {
            notifyIcon1.Icon = (Icon)Properties.Resources.ResourceManager.GetObject("stop");
            cache();
            timer1.Enabled = false;
            button2.Enabled = false;
            button1.Enabled = true;
            main.Abort();
        }

        //点击停止
        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            button1.Enabled = true;
            mainThreadClose();
        }

        //关闭程序，缓存项目
        private void form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            mainThreadClose();
        }

        //双击托盘
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
        }

        //写txt
        private void writeLogs(string pathName, string content)
        {
            if (content.Equals(""))
            {
                StreamWriter sw = new StreamWriter(pathName);
                sw.Write("");
                sw.Close();
            }
            else
            {
                StreamWriter sw = File.AppendText(pathName);
                sw.WriteLine(content);
                sw.Close();
            }
        }


        //判断当前是否为系统任务
        private bool isSysTask()
        {
            return taskName.Equals(TASK_SYS_UPDATE) || taskName.Equals(TASK_SYS_WAIT_ORDER) || taskName.Equals(TASK_SYS_SHUTDOWN) || taskName.Equals(TASK_SYS_RESTART) || taskName.Equals(TASK_SYS_NET_TEST);
        }

        //判断当前是否为投票项目
        private bool isVoteTask()
        {
            String auto = IniReadWriter.ReadIniKeys("Command", "isAutoVote", pathShare + "/CF.ini");
            isAutoVote = auto == "1";
            return taskName.Equals(TASK_VOTE_JIUTIAN) || taskName.Equals(TASK_VOTE_YUANQIU) || taskName.Equals(TASK_VOTE_MM) || taskName.Equals(TASK_VOTE_ML) || taskName.Equals(TASK_VOTE_JZ) || taskName.Equals(TASK_VOTE_JT) || taskName.Equals(TASK_VOTE_DM) || taskName.Equals(TASK_VOTE_OUTDO) || taskName.Equals(TASK_VOTE_PROJECT);
        }

        //缓存
        private void cache()
        {
            if (isSysTask())
            {
                return;
            }
            string path = "";
            if (customPath.Equals(taskPath))
            {
                path = taskPath;
            }
            else
            {
                path = "Writein";
            }
            cacheMemory = "TaskName-" + taskName + "`TaskPath-" + path + "`Worker:" + workerId;
            IniReadWriter.WriteIniKeys("Command", "CacheMemory" + no, cacheMemory, pathShare + "/TaskPlus.ini");
        }

        //通过进程名获取进程
        private Process[] getProcess(string proName)
        {
            //writeLogs(workingPath + "/log.txt", "getProcess:" + proName);
            if (StringUtil.isEmpty(proName) && !StringUtil.isEmpty(taskPath))
            {
                proName = taskPath.Substring(taskPath.LastIndexOf("\\") + 1);
            }
            proName = proName.Replace(".exe", "");
            return Process.GetProcessesByName(proName);
        }

        //获取项目进程
        private Process[] processCheck()
        {
            Process[] pros1 = getProcess("AutoUpdate.dll");
            if (pros1.Length > 0)
            {
                foreach (Process p in pros1)
                {
                    p.Kill();
                }
            }
            string process1 = "vote.exe";
            string process2 = "register.exe";
            Process[] process = getProcess(process1);
            if (process.Length > 0)
            {
                return process;
            }
            process = getProcess(process2);
            if (process.Length > 0)
            {
                return process;
            }
            if (process.Length > 0)
            {
                return process;
            }
            return getProcess("");
        }

        //关闭进程
        private void killProcess(bool stopIndicator)
        {
            //传票结束
            if (stopIndicator && isVoteTask())
            {
                writeLogs(workingPath + "/log.txt", "stop vote!");
                if (taskName.Equals(TASK_VOTE_JIUTIAN))
                {
                    IntPtr hwnd = HwndUtil.FindWindow("WTWindow", null);
                    if (hwnd != IntPtr.Zero)
                    {
                        hwnd = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "SysTabControl32", "");
                        hwnd = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "Button", "");
                        hwnd = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "Button", "结束投票");
                        writeLogs(workingPath + "/log.txt", "九天结束 句柄为" + hwnd);
                        HwndUtil.clickHwnd(hwnd);
                        int s = 0;
                        IntPtr hwndEx = IntPtr.Zero;
                        do
                        {
                            Thread.Sleep(500);
                            hwnd = HwndUtil.FindWindow("WTWindow", null);
                            hwndEx = HwndUtil.FindWindow("#32770", "信息：");
                            if (s % 10 == 0&& hwnd != IntPtr.Zero)
                            {
                                IntPtr hwnd0 = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "SysTabControl32", "");
                                hwnd0 = HwndUtil.FindWindowEx(hwnd0, IntPtr.Zero, "Button", "");
                                hwnd0 = HwndUtil.FindWindowEx(hwnd0, IntPtr.Zero, "Button", "结束投票");
                                HwndUtil.clickHwnd(hwnd0);
                            }
                            if (hwndEx != IntPtr.Zero)
                            {
                                HwndUtil.closeHwnd(hwndEx);
                                s = 90;
                            }
                            s++;
                        } while (hwnd != IntPtr.Zero && s < 90);
                    }
                }else if (taskName.Equals(TASK_VOTE_YUANQIU))
                {
                    IntPtr hwnd = HwndUtil.FindWindow("TForm1", null);
                    if (hwnd != IntPtr.Zero)
                    {
                        IntPtr hwndEx = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "TButton", "停止");
                        while (!Net.isOnline())
                        {
                            Thread.Sleep(500);
                        }
                        createHwndThread(hwndEx);
                        Thread.Sleep(5000);
                    }
                    
                }
                else if (taskName.Equals(TASK_VOTE_JZ))
                {
                    IntPtr hwnd = HwndUtil.FindWindow("TMainForm", null);
                    if (hwnd != IntPtr.Zero)
                    {
                        IntPtr hwndTGroupBox = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "TGroupBox", "当前状态");
                        IntPtr hwndEx = HwndUtil.FindWindowEx(hwndTGroupBox, IntPtr.Zero, "TButton", "停 止");
                        while (!Net.isOnline())
                        {
                            Thread.Sleep(500);
                        }
                        createHwndThread(hwndEx);
                        Thread.Sleep(5000);
                    }
                }
                else if (taskName.Equals(TASK_VOTE_JT))
                {
                    IntPtr hwnd = HwndUtil.FindWindow("ThunderRT6FormDC", null);
                    if (hwnd != IntPtr.Zero)
                    {
                        IntPtr hwndEx = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, null, "停止投票");
                        while (!Net.isOnline())
                        {
                            Thread.Sleep(500);
                        }
                        createHwndThread(hwndEx);
                        Thread.Sleep(5000);
                    }
                }
                else if (taskName.Equals(TASK_VOTE_HY))
                {
                    IntPtr hwnd = HwndUtil.FindWindow("WTWindow", null);
                    if (hwnd != IntPtr.Zero)
                    {
                        IntPtr hwndEx = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "Button", "停止");
                        while (!Net.isOnline())
                        {
                            Thread.Sleep(500);
                        }
                        createHwndThread(hwndEx);
                        Thread.Sleep(5000);
                    }
                }
                else if (taskName.Equals(TASK_VOTE_MM))
                {
                    IntPtr hwnd = HwndUtil.FindWindow("WTWindow", null);
                    if (hwnd != IntPtr.Zero)
                    {
                        IntPtr hwndEx = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, null, "停止投票");
                        while (!Net.isOnline())
                        {
                            Thread.Sleep(500);
                        }
                        createHwndThread(hwndEx);
                        int s = 0;
                        IntPtr hwndTip;
                        do
                        {
                            s++;
                            hwndTip = HwndUtil.FindWindow(null, "投票软件提示");
                            Thread.Sleep(500);
                        } while (hwndTip == IntPtr.Zero && s < 60);
                    }
                }
            }
            IntPtr hwndCheck = HwndUtil.FindWindow("WTWindow", null);
            if (hwndCheck != IntPtr.Zero)
            {
                IntPtr hwndEx = HwndUtil.FindWindowEx(hwndCheck, IntPtr.Zero, "SysTabControl32", "");
                hwndEx = HwndUtil.FindWindowEx(hwndEx, IntPtr.Zero, "Button", "");
                hwndEx = HwndUtil.FindWindowEx(hwndEx, IntPtr.Zero, "Button", "结束投票");
                if(hwndEx != IntPtr.Zero)
                {
                    HwndUtil.clickHwnd(hwndEx);
                }
                else
                {
                    HwndUtil.closeHwnd(hwndCheck);
                }
            }
            Process[] process = processCheck();
            if (process.Length > 0)
            {
                if (isVoteTask())
                {
                    int counter = 1;
                    while (!Net.isOnline() && counter < 60)
                    {
                        counter++;
                        Thread.Sleep(500);
                    }
                }

                foreach (Process p in process)
                {
                    writeLogs(workingPath + "/log.txt", "killProcess  :" + p.ToString());
                    try
                    {
                        p.Kill();
                    }
                    catch (Exception e) {
                        writeLogs(workingPath + "/log.txt", "killProcess  :" + e.ToString());
                    }
                }
            }
        }

        //切换任务流程
        private void taskChangeProcess(bool stopIndicator)
        {
            if (!File.Exists(@".\explorer-restart.bat"))
            {
                String[] Lines = { @"taskkill /f /im explorer.exe & start explorer.exe" };
                File.WriteAllLines(@".\explorer-restart.bat", Lines, Encoding.GetEncoding("GBK"));
            }
            //重启资源管理器
            //startProcess(workingPath+@"\explorer-restart.bat");
            succCount = 0;
            failTooMuch = false;
            timerChecked = 0;
            if (StringUtil.isEmpty(taskName))
            {
                taskName = IniReadWriter.ReadIniKeys("Command", "TaskName" + no, pathShare + "/Task.ini");
            }
            killProcess(getStopIndicator());
            if (adslName != "宽带连接")
            {
                rasOperate("disconnest");
            }
            taskName = IniReadWriter.ReadIniKeys("Command", "TaskName" + no, pathShare + "/Task.ini");
            taskChange = IniReadWriter.ReadIniKeys("Command", "taskChange" + no, pathShare + "/Task.ini");
            notifyIcon1.Text = "taskName:" + taskName + "\ntaskChange:" + taskChange;
            changeTask();
        }

        //切换任务
        private void changeTask()
        {
            if (taskChange.Equals("1"))
            {
                overTimeCount = 0;
                workerId = IniReadWriter.ReadIniKeys("Command", "worker", pathShare + "/CF.ini");
                inputId = IniReadWriter.ReadIniKeys("Command", "printgonghao", pathShare + "/CF.ini");
                tail = IniReadWriter.ReadIniKeys("Command", "tail", pathShare + "/CF.ini");
                customPath = IniReadWriter.ReadIniKeys("Command", "customPath" + no, pathShare + "/TaskPlus.ini");
                writeLogs(workingPath + "/log.txt", "taskChange:"+ customPath);
            }
            else
            {
                overTimeCount++;
            }
            if (StringUtil.isEmpty(workerId))
            {
                workerId = IniReadWriter.ReadIniKeys("Command", "worker", pathShare + "/CF.ini");
            }
            if (taskName.Equals(TASK_SYS_WAIT_ORDER))//待命
            {
                if(adslName != "宽带连接")
                {
                    rasOperate("disconnest");
                }
                taskName = IniReadWriter.ReadIniKeys("Command", "TaskName" + no, pathShare + "/Task.ini");
                if (taskName.Equals(TASK_SYS_WAIT_ORDER))
                {
                    IniReadWriter.WriteIniKeys("Command", "TaskChange" + no, "0", pathShare + "/Task.ini");
                    IniReadWriter.WriteIniKeys("Command", "customPath" + no, "", pathShare + "/TaskPlus.ini");
                }
            }
            else if (taskName.Equals(TASK_SYS_NET_TEST))//网络TEST
            {
                if (Net.isOnline())
                {
                    rasOperate("disconnest");
                }
                Thread.Sleep(500);
                rasOperate("connect");
                Thread.Sleep(500);
                if (!Net.isOnline())
                {
                    rasOperate("connect");
                    Thread.Sleep(1000);
                }
                if (Net.isOnline())
                {
                    if (adslName != "宽带连接")
                    {
                        ras.Disconnect();
                    }
                    IniReadWriter.WriteIniKeys("Command", "TaskName" + no, TASK_SYS_WAIT_ORDER, pathShare + "/Task.ini");
                    IniReadWriter.WriteIniKeys("Command", "TaskChange" + no, "0", pathShare + "/Task.ini");
                    IniReadWriter.WriteIniKeys("Command", "customPath" + no, "", pathShare + "/TaskPlus.ini");
                }
                else
                {
                    netError("error");
                }
            }
            else if (taskName.Equals(TASK_SYS_SHUTDOWN))//关机
            {
                IniReadWriter.WriteIniKeys("Command", "TaskName" + no, TASK_SYS_WAIT_ORDER, pathShare + "/Task.ini");
                IniReadWriter.WriteIniKeys("Command", "TaskChange" + no, "0", pathShare + "/Task.ini");
                IniReadWriter.WriteIniKeys("Command", "customPath" + no, "", pathShare + "/TaskPlus.ini");
                Process.Start("shutdown.exe", "-s -t 0");
                mainThreadClose();
            }
            else if (taskName.Equals(TASK_SYS_RESTART))//重启
            {
                string computerRename = IniReadWriter.ReadIniKeys("Command", "computerRename", pathShare + "/CF.ini");
                if (!StringUtil.isEmpty(computerRename))
                {
                    Computer.apiSetComputerNameEx(5, computerRename + "-" + no);
                }
                IniReadWriter.WriteIniKeys("Command", "TaskName" + no, TASK_SYS_WAIT_ORDER, pathShare + "/Task.ini");
                IniReadWriter.WriteIniKeys("Command", "TaskChange" + no, "0", pathShare + "/Task.ini");
                IniReadWriter.WriteIniKeys("Command", "customPath" + no, "", pathShare + "/TaskPlus.ini");
                Process.Start("shutdown.exe", "-r -t 0");
                mainThreadClose();
            }
            else if (taskName.Equals(TASK_SYS_UPDATE))//升级
            {
                IniReadWriter.WriteIniKeys("Command", "TaskName" + no, TASK_SYS_WAIT_ORDER, pathShare + "/Task.ini");
                IniReadWriter.WriteIniKeys("Command", "TaskChange" + no, "0", pathShare + "/Task.ini");
                IniReadWriter.WriteIniKeys("Command", "customPath" + no, "", pathShare + "/TaskPlus.ini");
                updateSoft();
                mainThreadClose();
            }
            else if (isVoteTask())//投票
            {
                netCheck();
                if (customPath.Equals(""))
                {
                    IniReadWriter.WriteIniKeys("Command", "TaskName" + no, TASK_SYS_WAIT_ORDER, pathShare + "/Task.ini");
                    IniReadWriter.WriteIniKeys("Command", "CacheMemory" + no, "", pathShare + "/TaskPlus.ini");
                    taskChangeProcess(false);
                    return;
                }
                if (taskChange.Equals("1"))
                {
                    if (customPath.Substring(customPath.LastIndexOf("\\") + 1) == "vote.exe")
                    {
                        startProcess(customPath.Substring(0, customPath.Length - 9) + @"\启动九天.bat");
                        taskName = TASK_VOTE_JIUTIAN;
                    }
                    else
                    {
                        startProcess(customPath);
                        taskName = TASK_VOTE_PROJECT;
                        IntPtr hwnd0, hwnd1, hwnd2, hwnd3, hwnd4;
                        do
                        {
                            hwnd0 = HwndUtil.FindWindow("WTWindow", null);
                            hwnd1 = HwndUtil.FindWindow("TForm1", null);
                            hwnd2 = HwndUtil.FindWindow("ThunderRT6FormDC", null);
                            hwnd3 = HwndUtil.FindWindow("obj_Form", null);
                            hwnd4 = HwndUtil.FindWindow("TMainForm", null);
                            if (hwnd0 != IntPtr.Zero)
                            {
                                StringBuilder title = new StringBuilder(512);
                                int i = HwndUtil.GetWindowText(hwnd0, title, 512);
                                if (title.ToString().Substring(0, 6) == "自动投票工具")
                                {
                                    taskName = TASK_VOTE_MM;
                                }
                                else if (title.ToString().Substring(0, 8) == "VOTE2016")
                                {
                                    taskName = TASK_VOTE_ML;
                                }
                                else if (title.ToString().IndexOf("自动投票软件") != -1)
                                {
                                    taskName = TASK_VOTE_HY;
                                }
                            }
                            else if (hwnd1 != IntPtr.Zero)
                            {
                                taskName = TASK_VOTE_YUANQIU;
                            }
                            else if (hwnd2 != IntPtr.Zero)
                            {
                                taskName = TASK_VOTE_JT;
                            }
                            else if (hwnd3 != IntPtr.Zero)
                            {
                                taskName = TASK_VOTE_DM;
                            }
                            else if (hwnd4 != IntPtr.Zero)
                            {
                                taskName = TASK_VOTE_JZ;
                            }
                            Thread.Sleep(500);
                        }

                        while (taskName.Trim().Equals(TASK_VOTE_PROJECT));
                    }
                    bool safeWrite = false;
                    Thread.Sleep(no % 10 * 50);
                    do
                    {
                        try
                        {
                            IniReadWriter.WriteIniKeys("Command", "TaskName" + no, taskName, pathShare + "/Task.ini");
                            Thread.Sleep(200);
                            string taskNameCheck = IniReadWriter.ReadIniKeys("Command", "TaskName" + no, pathShare + "/Task.ini");
                            if (StringUtil.isEmpty(taskNameCheck) || !taskNameCheck.Equals(taskName))
                            {
                                writeLogs(workingPath + "/log.txt", "TaskName Write Error!");//清空日志
                                IniReadWriter.WriteIniKeys("Command", "TaskName" + no, taskName, pathShare + "/Task.ini");
                                throw new Exception();
                            }
                            safeWrite = true;
                        }
                        catch (Exception e)
                        {
                            Thread.Sleep(no % 10 * 50);
                        }
                    } while (!safeWrite);
                }
                if (taskName.Equals(TASK_VOTE_JIUTIAN))
                {
                    if (!taskChange.Equals("1"))
                    {
                        startProcess(customPath.Substring(0, customPath.Length - 9) + @"\启动九天.bat");
                        Thread.Sleep(500);
                    }
                    jiutianStart();
                }
                else
                {
                    if (!taskChange.Equals("1"))
                    {
                        startProcess(customPath);
                        Thread.Sleep(500);
                    }
                    if (taskName.Equals(TASK_VOTE_MM))
                    {
                        mmStart();
                    }
                    else if (taskName.Equals(TASK_VOTE_ML))
                    {
                        //ML开始程序
                    }
                    else if (taskName.Equals(TASK_VOTE_YUANQIU))
                    {
                        yuanqiuStart();
                    }
                    else if (taskName.Equals(TASK_VOTE_JT))
                    {
                        jtStart();
                    }
                    else if (taskName.Equals(TASK_VOTE_DM))
                    {
                        //DM开始程序
                    }
                    else if (taskName.Equals(TASK_VOTE_JZ))
                    {
                        jzStart();
                    }
                    else if (taskName.Equals(TASK_VOTE_HY))
                    {
                        hyStart();
                    }
                }
                taskPath = customPath;
            }else
            {
                taskName = TASK_SYS_WAIT_ORDER;
            }

            taskMonitor();
        }

        //NAME检测
        private bool nameCheck()
        {
            taskChange = IniReadWriter.ReadIniKeys("Command", "TaskChange" + no, pathShare + "/Task.ini");
            if (taskChange.Equals("1"))
            {
                taskName = IniReadWriter.ReadIniKeys("Command", "TaskName" + no, pathShare + "/Task.ini");
                if (!taskName.Equals(projectName))
                {
                    taskChangeProcess(false);
                    return false;
                }
            }
            return true;
        }

        //结束启动
        private void finishStart()
        {
            if (!nameCheck())
            {
                return;
            }
            IniReadWriter.WriteIniKeys("Command", "TaskChange" + no, "0", pathShare + "/Task.ini");
        }

        //hwndThread创建
        private void createHwndThread(IntPtr hwnd)
        {
            threadHwnd = hwnd;
            hwndThread = new Thread(clickHwndByThread);
            hwndThread.Start();
        }

        //处理句柄操作线程
        private void clickHwndByThread()
        {
            HwndUtil.clickHwnd(threadHwnd);
        }

        //圆球启动
        private void yuanqiuStart()
        {
            IntPtr hwnd = IntPtr.Zero;
            projectName = TASK_VOTE_YUANQIU;
            do
            {
                if (!nameCheck())
                {
                    return;
                }
                hwnd = HwndUtil.FindWindow("TForm1", null);
                Thread.Sleep(1000);
            }
            while (hwnd == IntPtr.Zero);
            Thread.Sleep(1000);
            //设置拨号延迟
            IntPtr hwndTGroupBox = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "TGroupBox", "设置");
            IntPtr hwndEx = HwndUtil.FindWindowEx(hwndTGroupBox, IntPtr.Zero, "TEdit", null);
            hwndEx = HwndUtil.FindWindowEx(hwndTGroupBox, hwndEx, "TEdit", null);
            hwndEx = HwndUtil.FindWindowEx(hwndTGroupBox, hwndEx, "TEdit", null);
            hwndEx = HwndUtil.FindWindowEx(hwndTGroupBox, hwndEx, "TEdit", null);
            hwndEx = HwndUtil.FindWindowEx(hwndTGroupBox, hwndEx, "TEdit", null);
            hwndEx = HwndUtil.FindWindowEx(hwndTGroupBox, hwndEx, "TEdit", null);
            hwndEx = HwndUtil.FindWindowEx(hwndTGroupBox, hwndEx, "TEdit", null);
            HwndUtil.setText(hwndEx, (delay / 1000).ToString());
            //设置工号
            if (inputId.Equals("1"))
            {
                String id = workerId;
                if (tail.Equals("1"))
                {
                    id = workerId + "-" + (no > 9 ? no.ToString() : "0" + no);
                }
                hwndTGroupBox = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "TGroupBox", "会员");
                hwndEx = HwndUtil.FindWindowEx(hwndTGroupBox, IntPtr.Zero, "TEdit", null);
                HwndUtil.setText(hwndEx, id);
                hwndEx = HwndUtil.FindWindowEx(hwndTGroupBox, hwndEx, "TEdit", null);
                HwndUtil.setText(hwndEx, id);
                hwndEx = HwndUtil.FindWindowEx(hwndTGroupBox, hwndEx, "TEdit", null);
                HwndUtil.setText(hwndEx, id);
                hwndEx = HwndUtil.FindWindowEx(hwndTGroupBox, hwndEx, "TEdit", null);
                HwndUtil.setText(hwndEx, id);
            }
            //开始投票
            hwndEx = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "TButton", "开始");
            createHwndThread(hwndEx);
            finishStart();
        }

        //JZ启动
        private void jzStart()
        {
            IntPtr hwnd = IntPtr.Zero;
            projectName = TASK_VOTE_JZ;
            do
            {
                if (!nameCheck())
                {
                    return;
                }
                hwnd = HwndUtil.FindWindow("TMainForm", null);
                Thread.Sleep(500);
            }
            while (hwnd == IntPtr.Zero);
            //设置拨号延迟

            IntPtr hwndEx = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "TEdit", null);
            hwndEx = HwndUtil.FindWindowEx(hwnd, hwndEx, "TEdit", null);
            hwndEx = HwndUtil.FindWindowEx(hwnd, hwndEx, "TEdit", null);
            HwndUtil.setText(hwndEx, (delay / 1000).ToString());
            //设置工号
            if (inputId.Equals("1"))
            {
                String id = workerId;
                if (tail.Equals("1"))
                {
                    id = workerId + "-" + (no > 9 ? no.ToString() : "0" + no);
                }
                IntPtr hwndTGroupBox0 = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "TGroupBox", "会员选项");
                hwndEx = HwndUtil.FindWindowEx(hwndTGroupBox0, IntPtr.Zero, "TEdit", null);
                hwndEx = HwndUtil.FindWindowEx(hwndTGroupBox0, hwndEx, "TEdit", null);
                hwndEx = HwndUtil.FindWindowEx(hwndTGroupBox0, hwndEx, "TEdit", null);
                HwndUtil.setText(hwndEx, id);
            }
            //开始投票
            IntPtr hwndTGroupBox = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "TGroupBox", "当前状态");
            hwndEx = HwndUtil.FindWindowEx(hwndTGroupBox, IntPtr.Zero, "TButton", "开 始");
            createHwndThread(hwndEx);
            finishStart();
        }

        //HY启动
        private void hyStart()
        {
            IntPtr hwnd = IntPtr.Zero;
            projectName = TASK_VOTE_HY;
            do
            {
                if (!nameCheck())
                {
                    return;
                }
                hwnd = HwndUtil.FindWindow("WTWindow", null);
                Thread.Sleep(500);
            }
            while (hwnd == IntPtr.Zero);
            //设置拨号延迟
            IntPtr hwndCf = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "Button", "设置");

            IntPtr hwndEx = HwndUtil.FindWindowEx(hwndCf, IntPtr.Zero, "Edit", null);
            hwndEx = HwndUtil.FindWindowEx(hwndCf, hwndEx, "Edit", null);
            hwndEx = HwndUtil.FindWindowEx(hwndCf, hwndEx, "Edit", null);
            hwndEx = HwndUtil.FindWindowEx(hwndCf, hwndEx, "Edit", null);
            HwndUtil.setText(hwndEx, (delay / 1000).ToString());
            //设置工号
            if (inputId.Equals("1"))
            {
                String id = workerId;
                if (tail.Equals("1"))
                {
                    id = workerId + "-" + (no > 9 ? no.ToString() : "0" + no);
                }
                IntPtr hwndId = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "Button", "会员");
                hwndEx = HwndUtil.FindWindowEx(hwndId, IntPtr.Zero, "Edit", null);
                hwndEx = HwndUtil.FindWindowEx(hwndId, hwndEx, "Edit", null);
                hwndEx = HwndUtil.FindWindowEx(hwndId, hwndEx, "Edit", null);
                HwndUtil.setText(hwndEx, id);
            }
            //开始投票
            IntPtr hwndStart = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "Button", "投票");
            createHwndThread(hwndStart);
            finishStart();
        }

        //HY到票检测
        private bool hyOverCheck()
        {
            IntPtr hwnd = HwndUtil.FindWindow("#32770", "信息：");
            if (hwnd != IntPtr.Zero)
            {
                HwndUtil.closeHwnd(hwnd);
                IniReadWriter.WriteIniKeys("Command", "OVER", "1", pathShare + @"\CF.ini");
                return true;
            }
            return false;
        }

        //JT启动
        private void jtStart()
        {
            IntPtr hwnd = IntPtr.Zero;
            projectName = TASK_VOTE_JT;
            do
            {
                if (!nameCheck())
                {
                    return;
                }
                hwnd = HwndUtil.FindWindow("ThunderRT6FormDC", null);
                Thread.Sleep(500);
            }
            while (hwnd == IntPtr.Zero);
            //设置拨号延迟

            IntPtr ThunderRT6Frame = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "ThunderRT6Frame", "设置");
            IntPtr hwndEx = HwndUtil.FindWindowEx(ThunderRT6Frame, IntPtr.Zero, "ThunderRT6TextBox", null);
            hwndEx = HwndUtil.FindWindowEx(ThunderRT6Frame, hwndEx, "ThunderRT6TextBox", null);
            HwndUtil.setText(hwndEx, (delay / 1000).ToString());
            //设置工号
            if (inputId.Equals("1"))
            {
                String id = workerId;
                if (tail.Equals("1"))
                {
                    id = workerId + "-" + (no > 9 ? no.ToString() : "0" + no);
                }
                ThunderRT6Frame = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "ThunderRT6Frame", "会员");
                hwndEx = HwndUtil.FindWindowEx(ThunderRT6Frame, IntPtr.Zero, "ThunderRT6TextBox", null);
                HwndUtil.setText(hwndEx, id);
            }
            //开始投票
            hwndEx = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, null, "自动投票");
            createHwndThread(hwndEx);
            finishStart();
        }

        //MM启动
        private void mmStart()
        {
            IntPtr hwnd = IntPtr.Zero;
            projectName = TASK_VOTE_MM;
            do
            {
                if (!nameCheck())
                {
                    return;
                }
                hwnd = HwndUtil.FindWindow("WTWindow", null);
                Thread.Sleep(500);
            }
            while (hwnd == IntPtr.Zero);
            //设置拨号延迟
            IntPtr ButtonHwnd = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "Button", "设置");
            IntPtr hwndEx = HwndUtil.FindWindowEx(ButtonHwnd, IntPtr.Zero, "Edit", "2");
            if (hwndEx == IntPtr.Zero)
            {
                hwndEx = HwndUtil.FindWindowEx(ButtonHwnd, IntPtr.Zero, "Edit", "3");
            }
            HwndUtil.setText(hwndEx, (delay / 1000).ToString());
            //设置工号
            if (inputId.Equals("1"))
            {
                String id = workerId;
                if (tail.Equals("1"))
                {
                    id = workerId + "-" + (no > 9 ? no.ToString() : "0" + no);
                }
                ButtonHwnd = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "Button", "会员");
                hwndEx = HwndUtil.FindWindowEx(ButtonHwnd, IntPtr.Zero, "Edit", null);
                hwndEx = HwndUtil.FindWindowEx(ButtonHwnd, hwndEx, "Edit", null);
                hwndEx = HwndUtil.FindWindowEx(ButtonHwnd, hwndEx, "Edit", null);
                hwndEx = HwndUtil.FindWindowEx(ButtonHwnd, hwndEx, "Edit", null);
                hwndEx = HwndUtil.FindWindowEx(ButtonHwnd, hwndEx, "Edit", null);
                HwndUtil.setText(hwndEx, id);
            }
            //开始投票
            hwndEx = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, null, "自动投票");
            createHwndThread(hwndEx);
            finishStart();
        }

        //九天启动
        private void jiutianStart()
        {
            IntPtr hwnd = IntPtr.Zero;
            IntPtr hwndSysTabControl32 = IntPtr.Zero;
            IntPtr workCondition = IntPtr.Zero;
            IntPtr preparedCheck = IntPtr.Zero;
            IntPtr startButton = IntPtr.Zero;
            projectName = TASK_VOTE_JIUTIAN;
            int count = 0;
            do
            {
                if (!nameCheck())
                {
                    return;
                }
                hwnd = HwndUtil.FindWindow("WTWindow", null);
                hwndSysTabControl32 = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "SysTabControl32", "");
                workCondition = HwndUtil.FindWindowEx(hwndSysTabControl32, IntPtr.Zero, "Button", "工作情况");
                preparedCheck = HwndUtil.FindWindowEx(workCondition, IntPtr.Zero, "Afx:400000:b:10011:1900015:0", "加载成功 可开始投票");
                jiutianCode = "Afx:400000:b:10011:1900015:0";
                if (preparedCheck == IntPtr.Zero)
                {
                    //WIN7
                    preparedCheck = HwndUtil.FindWindowEx(workCondition, IntPtr.Zero, "Afx:400000:b:10003:1900015:0", "加载成功 可开始投票");
                    jiutianCode = "Afx:400000:b:10003:1900015:0";
                }
                if (preparedCheck == IntPtr.Zero)
                {
                    //WIN10
                    preparedCheck = HwndUtil.FindWindowEx(workCondition, IntPtr.Zero, "Afx:400000:b:10003:900015:0", "加载成功 可开始投票");
                    jiutianCode = "Afx:400000:b:10003:900015:0";
                }
                startButton = HwndUtil.FindWindowEx(hwndSysTabControl32, IntPtr.Zero, "Button", "");
                startButton = HwndUtil.FindWindowEx(startButton, IntPtr.Zero, "Button", "开始投票");
                Thread.Sleep(500);
                if (hwnd != IntPtr.Zero&& count == 60)
                {
                    Process.Start("shutdown.exe", "-r -t 0");
                    mainThreadClose();
                }
            }
            while (preparedCheck == IntPtr.Zero || startButton == IntPtr.Zero);
            //设置拨号延迟
            IntPtr hwndEx = HwndUtil.FindWindowEx(hwndSysTabControl32, IntPtr.Zero, "Button", "拨号设置");
            hwndEx = HwndUtil.FindWindowEx(hwndEx, IntPtr.Zero, "SysTabControl32", "");
            hwndEx = HwndUtil.FindWindowEx(hwndEx, IntPtr.Zero, "Edit", null);
            HwndUtil.setText(hwndEx, delay.ToString());
            //设置工号
            if (inputId.Equals("1"))
            {
                String id = workerId;
                if (tail.Equals("1"))
                {
                    id = workerId + "-" + (no > 9 ? no.ToString() : "0" + no);
                }
                hwndEx = HwndUtil.FindWindowEx(hwndSysTabControl32, IntPtr.Zero, "Button", "请输入工号");
                hwndEx = HwndUtil.FindWindowEx(hwndEx, IntPtr.Zero, "Edit", null);
                HwndUtil.setText(hwndEx, id);
            }
            HwndUtil.clickHwnd(startButton);
            Thread.Sleep(500);
            finishStart();
        }

        private void ThreadProcess(object obj)
        {
            string pathName = obj.ToString();
            try
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = pathName;
                info.Arguments = "";
                info.WorkingDirectory = pathName.Substring(0, pathName.LastIndexOf("\\"));
                info.WindowStyle = ProcessWindowStyle.Normal;
                Process pro = Process.Start(info);
                pro.WaitForExit();
                pro.Close();
                Thread.Sleep(500);
            }
            catch (Exception e)
            {
                writeLogs(workingPath + "/log.txt", "拒绝访问:" + pathName);
                switchWatiOrder();
                Process.Start("shutdown.exe", "-r -t 0");
            }

        }

        //通过路径启动进程
        private void startProcess(string pathName)
        {
            Thread t = new Thread(new ParameterizedThreadStart(ThreadProcess));
            t.Start(pathName);
        }

        //通过路径启动进程
        private void StartProcess(string pathName)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = pathName;
            info.Arguments = "";
            info.WorkingDirectory = pathName.Substring(0, pathName.LastIndexOf("\\"));
            info.WindowStyle = ProcessWindowStyle.Normal;
            Process pro = Process.Start(info);
            Thread.Sleep(500);
        }

        //升级程序
        private void updateSoft()
        {
            if (!Net.isRealOnline())
            {
                rasOperate("connect");
            }
            writeLogs("./log.txt", "开始下载:更新");
            string pathName = "./handler-new.exe";
            string url = "http://bitcoinrobot.cn/file/handler.exe";
            bool isDownloading = true;
            HttpManager httpManager = HttpManager.getInstance();
            do
            {
                try
                {
                    httpManager.HttpDownloadFile(url, pathName);
                    isDownloading = false;
                }
                catch (Exception)
                {
                    writeLogs("./log.txt", "更新下载异常，重新下载");
                    File.Delete(pathName);
                    Thread.Sleep(1000);
                }
            } while (isDownloading);
            if (!File.Exists("./update.bat"))
            {
                string path = "";
                string line1 = "Taskkill /F /IM handler.exe";
                string line2 = "ping -n 3 127.0.0.1>nul";
                string line3 = "del /s /Q " + @""""+ Environment.CurrentDirectory + "\\handler.exe"+@"""";
                string line4 = "ping -n 3 127.0.0.1>nul";
                string line5 = @"ren """ + Environment.CurrentDirectory + @"\\handler-new.exe"" ""handler.exe""";
                string line6 = "ping -n 3 127.0.0.1>nul";
                string line7 = "start " + @""""" "+@"""" + Environment.CurrentDirectory + "\\handler.exe"+ @"""";
                string[] lines = { "@echo off", line1, line2, line3, line4, line5, line6, line7 };
                File.WriteAllLines(@"./update.bat", lines, Encoding.GetEncoding("GBK"));
            }

            StartProcess(Environment.CurrentDirectory + "\\update.bat");
            Application.Exit();//退出整个应用程序
        }

        //显示通知
        private void showNotification(string content, ToolTipIcon toolTipIcon)
        {
            notifyIcon1.ShowBalloonTip(0, content, DateTime.Now.ToLocalTime().ToString(), toolTipIcon);

        }


        //检测IE版本
        private static bool isIE8()
        {
            RegistryKey mreg;
            mreg = Registry.LocalMachine;
            try
            {
                mreg = mreg.CreateSubKey("software\\Microsoft\\Internet Explorer");
            }catch(Exception e)
            {
                return false;
            }
            return mreg.GetValue("Version").ToString().Substring(0,1)=="8";
        }

        private static string InvokeCmd(string cmdArgs)
        {
            string Tstr = "";
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            p.StandardInput.WriteLine(cmdArgs);
            p.StandardInput.WriteLine("exit");
            Tstr = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            p.Close();
            return Tstr;
        }

        //ADSL操作
        private void rasOperate(string type)
        {

            if (type.Equals("connect"))
            {
                if (Net.isOnline())
                {
                    return;
                }
                if (ie8)
                {
                    Thread.Sleep(200);
                    Thread rasThread = new Thread(rasConnect);
                    rasThread.Start();
                    bool online = false;
                    bool err = false;
                    int count = 0;
                    do
                    {
                        online = Net.isOnline();
                        if (!online)
                        {
                            Thread.Sleep(500);
                            IntPtr hwnd = HwndUtil.FindWindow("#32770", "连接到 " + adslName + " 时出错");
                            if (hwnd != IntPtr.Zero)
                            {
                                HwndUtil.closeHwnd(hwnd);
                                err = true;
                            }
                        }
                        count++;
                    } while (!online && !err);
                }
                else
                {
                    if (adslName == "宽带连接")
                    {
                        String user = IniReadWriter.ReadIniKeys("Command", "user", "./handler.ini");
                        String password = IniReadWriter.ReadIniKeys("Command", "password", "./handler.ini");
                        InvokeCmd("rasdial.exe 宽带连接" + " " + user + " " + password);
                    }
                    else
                    {
                        ras = new RASDisplay();
                        ras.Connect(adslName);
                    }

                }
            }
            else
            {
                ras = new RASDisplay();
                ras.Disconnect();
            }
        }

        //ras子线程，处理IE8线程阻塞
        private void rasConnect()
        {
            writeLogs(workingPath + "/log.txt", "rasConnect");
            ras = new RASDisplay();
            ras.Disconnect();
            if (adslName == "宽带连接")
            {
                String user = IniReadWriter.ReadIniKeys("Command", "user", "./handler.ini");
                String password = IniReadWriter.ReadIniKeys("Command", "password", "./handler.ini");
                InvokeCmd("rasdial.exe 宽带连接" + " " + user + " " + password);
            }
            else
            {
                ras.Connect(adslName);
            }
        }

        //网络检测
        private void netCheck()
        {
            showNotification("正在初始化网络...", ToolTipIcon.Info);
            bool online = Net.isOnline();
            if (!online)
            {
                rasOperate("connect");
                Thread.Sleep(1000);
                online = Net.isOnline();
                if (!online)
                {
                    rasOperate("connect");
                    Thread.Sleep(1000);
                    online = Net.isOnline();
                    if (!online)
                    {
                        string exception = IniReadWriter.ReadIniKeys("Command", "exception", "./handler.ini");
                        if (exception == "1")
                        {
                            netError("error");
                        }
                        else
                        {
                            IniReadWriter.WriteIniKeys("Command", "exception", "1", "./handler.ini");
                            Process.Start("shutdown.exe", "-r -t 0");
                            mainThreadClose();
                        }
                    }
                }
            }
            IniReadWriter.WriteIniKeys("Command", "exception", "0", "./handler.ini");
            string arrDrop = IniReadWriter.ReadIniKeys("Command", "ArrDrop", pathShare + "/CF.ini");
            if (!StringUtil.isEmpty(arrDrop))
            {
                arrDrop = " " + arrDrop;
            }
            if (arrDrop.IndexOf(" " + no + " |") != -1)
            {
                IniReadWriter.WriteIniKeys("Command", "ArrDrop", arrDrop.Replace(" " + no + " |", ""), pathShare + "/CF.ini");
            }
        }

        //网络异常处理
        private void netError(string type)
        {
            String val = IniReadWriter.ReadIniKeys("Command", "Val", pathShare + "/CF.ini");
            if (StringUtil.isEmpty(val))
            {
                if (type.Equals("exception"))
                {
                    IniReadWriter.WriteIniKeys("Command", "Val", no.ToString(), pathShare + "/CF.ini");//正数 异常
                }
                else
                {
                    IniReadWriter.WriteIniKeys("Command", "Val", (-no).ToString(), pathShare + "/CF.ini");//负数 掉线
                }
                mainThreadClose();
            }
            else
            {
                Thread.Sleep(2000);
                netError(type);
            }

        }

        //到票待命
        private void switchWatiOrder()
        {
            IniReadWriter.WriteIniKeys("Command", "CustomPath" + no, "", pathShare + @"\TaskPlus.ini");
            IniReadWriter.WriteIniKeys("Command", "TaskName" + no, TASK_SYS_WAIT_ORDER, pathShare + @"\Task.ini");
            IniReadWriter.WriteIniKeys("Command", "TaskChange" + no, "1", pathShare + @"\Task.ini");
        }

        //圆球到票检测
        private bool yuanqiuOverCheck()
        {
            IntPtr hwnd = HwndUtil.FindWindow("TMessageForm", "register");
            if (hwnd != IntPtr.Zero)
            {
                if (isAutoVote)
                {
                    addVoteProjectNameDropedTemp(false);
                }
                HwndUtil.closeHwnd(hwnd);
                IniReadWriter.WriteIniKeys("Command", "OVER", "1", pathShare + @"\CF.ini");
                return true;
            }
            return false;
        }

        //JZ到票检测
        private bool jzOverCheck()
        {
            IntPtr hwnd = HwndUtil.FindWindow("TMessageForm", null);
            if (hwnd != IntPtr.Zero)
            {
                HwndUtil.closeHwnd(hwnd);
                IniReadWriter.WriteIniKeys("Command", "OVER", "1", pathShare + @"\CF.ini");
                return true;
            }
            return false;
        }

        //JT到票检测
        private bool jtOverCheck()
        {
            IntPtr hwnd = HwndUtil.FindWindow(null, "VOTETOOL");
            if (hwnd != IntPtr.Zero)
            {
                HwndUtil.closeHwnd(hwnd);
                IniReadWriter.WriteIniKeys("Command", "OVER", "1", pathShare + @"\CF.ini");
                return true;
            }
            return false;
        }

        //MM到票检测
        private bool mmOverCheck()
        {
            IntPtr hwnd = HwndUtil.FindWindow(null, "投票软件提示");
            if (hwnd != IntPtr.Zero)
            {
                if (isAutoVote)
                {
                    addVoteProjectNameDropedTemp(false);
                }
                HwndUtil.closeHwnd(hwnd);
                IniReadWriter.WriteIniKeys("Command", "OVER", "1", pathShare + @"\CF.ini");
                return true;
            }
            return false;
        }

        //九天到票检测
        private bool jiutianOverCheck(ref int s)
        {
            IntPtr hwnd = HwndUtil.FindWindow("WTWindow", null);
            if (hwnd == IntPtr.Zero)
            {
                s++;
            }
            else
            {
                s = 0;
            }
            if (s > 5)
            {
                if (isAutoVote)
                {
                    addVoteProjectNameDropedTemp(false);
                }
                IniReadWriter.WriteIniKeys("Command", "OVER", "1", pathShare + @"\CF.ini");
                return true;
            }
            return false;
        }

        //添加黑名单项目 临
        private void addVoteProjectNameDropedTemp(bool isAllProject)
        {
            string projectName = IniReadWriter.ReadIniKeys("Command", "ProjectName", pathShare + "/AutoVote.ini");
            if (isAllProject)
            {
                projectName = projectName.Substring(0, projectName.IndexOf("_"));
            }
            string voteProjectNameDroped = IniReadWriter.ReadIniKeys("Command", "voteProjectNameDropedTemp", pathShare + "/AutoVote.ini");
            int dropVote = 0;
            try
            {
                dropVote = int.Parse(IniReadWriter.ReadIniKeys("Command", "dropVote", pathShare + "/AutoVote.ini"));
            }
            catch (Exception) { }
            finally
            {
                dropVote++;
            }
            IniReadWriter.WriteIniKeys("Command", "dropVote", dropVote.ToString(), pathShare + "/AutoVote.ini");
            if (StringUtil.isEmpty(voteProjectNameDroped) || voteProjectNameDroped.IndexOf(projectName) == -1)
            {
                int validDrop;
                try
                {
                    validDrop = int.Parse(IniReadWriter.ReadIniKeys("Command", "validDrop", pathShare + "/AutoVote.ini"));
                }
                catch (Exception)
                {
                    validDrop = 1;
                }
                if (dropVote >= validDrop)
                {
                    voteProjectNameDroped += StringUtil.isEmpty(voteProjectNameDroped) ? projectName : "|" + projectName;
                    IniReadWriter.WriteIniKeys("Command", "voteProjectNameDropedTemp", voteProjectNameDroped, pathShare + "/AutoVote.ini");
                }
            }
        }

        //添加黑名单项目
        private void addVoteProjectNameDroped(bool isAllProject)
        {
            string projectName = IniReadWriter.ReadIniKeys("Command", "ProjectName", pathShare + "/AutoVote.ini");
            if (isAllProject)
            {
                projectName = projectName.Substring(0, projectName.IndexOf("_"));
            }
            string voteProjectNameDroped = IniReadWriter.ReadIniKeys("Command", "voteProjectNameDroped", pathShare + "/AutoVote.ini");
            int dropVote=0;
            try
            {
                dropVote = int.Parse(IniReadWriter.ReadIniKeys("Command", "dropVote", pathShare + "/AutoVote.ini"));
            }
            catch (Exception){ }
            finally
            {
                dropVote++;
            }
            IniReadWriter.WriteIniKeys("Command", "dropVote", dropVote.ToString(), pathShare + "/AutoVote.ini");
            if (StringUtil.isEmpty(voteProjectNameDroped) || voteProjectNameDroped.IndexOf(projectName) == -1)
            {
                int validDrop;
                try
                {
                    validDrop = int.Parse(IniReadWriter.ReadIniKeys("Command", "validDrop", pathShare + "/AutoVote.ini"));
                }
                catch(Exception)
                {
                    validDrop = 1;
                }
                if (dropVote >= validDrop)
                {
                    voteProjectNameDroped += StringUtil.isEmpty(voteProjectNameDroped) ? projectName : "|" + projectName;
                    IniReadWriter.WriteIniKeys("Command", "voteProjectNameDroped", voteProjectNameDroped, pathShare + "/AutoVote.ini");
                }
            }
        }

        //九天限人检测
        private bool jiutianRestrictCheck()
        {
            IntPtr hwnd = HwndUtil.FindWindow("#32770", "信息提示");
            if (hwnd != IntPtr.Zero)
            {
                if (isAutoVote)
                {
                    addVoteProjectNameDroped(false);
                }
                HwndUtil.closeHwnd(hwnd);
                return true;
            }
            return false;
        }

        //九天禁止虚拟机检测
        private bool jiutianVmBanCheck()
        {
            IntPtr hwnd = HwndUtil.FindWindow("#32770", "信息：");
            IntPtr hwndEx = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "Static", "本任务禁止在虚拟机内运行");
            if (hwndEx != IntPtr.Zero)
            {
                if (isAutoVote)
                {
                    addVoteProjectNameDroped(false);
                }
                HwndUtil.closeHwnd(hwnd);
                return true;
            }
            return false;
        }
        //九天验证码输入检测
        private bool isIdentifyCode()
        {
            IntPtr hwnd = HwndUtil.FindWindow("WTWindow", null);
            IntPtr hwndSysTabControl32 = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "SysTabControl32", "");
            IntPtr testHwnd = HwndUtil.FindWindowEx(hwndSysTabControl32, IntPtr.Zero, "Button", "输入验证码后回车,看不清直接回车切换");
            if (testHwnd != IntPtr.Zero)
            {
                addVoteProjectNameDroped(true);
                killProcess(false);
                rasOperate("disconnect");
                return true;
            }
            return false;
        }

        //获取 是否需要传票关闭
        private bool getStopIndicator()
        {
            if (taskName.Equals(TASK_VOTE_JIUTIAN))
            {
                IntPtr hwnd = HwndUtil.FindWindow("WTWindow", null);
                if (hwnd != IntPtr.Zero)
                {
                
                    IntPtr hwndSysTabControl32 = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "SysTabControl32", "");
                    IntPtr hwndStat = HwndUtil.FindWindowEx(hwndSysTabControl32, IntPtr.Zero, "Button", "投票统计");
                    IntPtr hwndEx = HwndUtil.FindWindowEx(hwndStat, IntPtr.Zero, jiutianCode, "运行时间");
                    if (hwndEx == IntPtr.Zero)
                    {
                        //WIN7主机环境
                        hwndEx = HwndUtil.FindWindowEx(hwndStat, IntPtr.Zero, jiutianCode, "运行时间");
                    }
                    if (hwndEx == IntPtr.Zero)
                    {
                        //WIN10主机环境
                        hwndEx = HwndUtil.FindWindowEx(hwndStat, IntPtr.Zero, jiutianCode, "运行时间");
                    }
                    hwndEx = HwndUtil.FindWindowEx(hwndStat, hwndEx,jiutianCode, null);
                    hwndEx = HwndUtil.FindWindowEx(hwndStat, hwndEx, jiutianCode, null);
                    hwndEx = HwndUtil.FindWindowEx(hwndStat, hwndEx, jiutianCode, null);
                    StringBuilder unUpload = new StringBuilder(512);
                    HwndUtil.GetWindowText(hwndEx, unUpload, unUpload.Capacity);
                    return int.Parse(unUpload.ToString()) > 0;
                }
                return false;
            }else
            {
                return true;
            }
            
        }

        //获取九天成功数
        private int getJiutianSucc()
        {
            IntPtr hwnd = HwndUtil.FindWindow("WTWindow", null);
            IntPtr hwndSysTabControl32 = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "SysTabControl32", "");
            IntPtr hwndStat = HwndUtil.FindWindowEx(hwndSysTabControl32, IntPtr.Zero, "Button", "投票统计");
            IntPtr hwndEx = HwndUtil.FindWindowEx(hwndStat, IntPtr.Zero, jiutianCode, "超时票数");
            hwndEx = HwndUtil.FindWindowEx(hwndStat, hwndEx, jiutianCode, null);
            try
            {
                hwndEx = HwndUtil.FindWindowEx(hwndStat, hwndEx, jiutianCode, null);
                hwndEx = HwndUtil.FindWindowEx(hwndStat, hwndEx, jiutianCode, null);
                StringBuilder succ = new StringBuilder(512);
                HwndUtil.GetWindowText(hwndEx, succ, 512);
                return int.Parse(succ.ToString());
            }
            catch (Exception) {
                writeLogs(workingPath + "/log.txt", "获取九天成功失败！");
            }
            return 0;

        }


        //获取圆球成功数
        private int getYuanqiuSucc()
        {
            IntPtr hwnd = HwndUtil.FindWindow("TForm1", null);
            IntPtr hwndTGroupBox = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "TGroupBox", "状态");
            IntPtr hwndEx = HwndUtil.FindWindowEx(hwndTGroupBox, IntPtr.Zero, "TEdit", null);
            try
            {
                StringBuilder succ = new StringBuilder(512);
                HwndUtil.GetWindowText(hwndEx, succ, 512);
                return int.Parse(succ.ToString());
            }
            catch (Exception)
            {
                writeLogs(workingPath + "/log.txt", "获取圆球成功失败！");
            }
            return 0;

        }


        //九天成功检测
        private bool jiutianFailTooMuch()
        {
            IntPtr hwnd = HwndUtil.FindWindow("WTWindow", null);
            IntPtr hwndSysTabControl32 = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "SysTabControl32", "");
            IntPtr hwndStat = HwndUtil.FindWindowEx(hwndSysTabControl32, IntPtr.Zero, "Button", "投票统计");
            IntPtr hwndEx = HwndUtil.FindWindowEx(hwndStat, IntPtr.Zero, jiutianCode, "超时票数");
            hwndEx = HwndUtil.FindWindowEx(hwndStat, hwndEx, jiutianCode, null);
            StringBuilder duration = new StringBuilder(512);
            HwndUtil.GetWindowText(hwndEx, duration, duration.Capacity);
            int min;
            try
            {
                min = int.Parse(duration.ToString().Split('：')[1]);
            }
            catch (Exception)
            {
                min=0;
            }
            if (min >= 2)
            {
                hwndEx = HwndUtil.FindWindowEx(hwndStat, hwndEx, jiutianCode, null);
                hwndEx = HwndUtil.FindWindowEx(hwndStat, hwndEx, jiutianCode, null);
                StringBuilder succ = new StringBuilder(512);
                HwndUtil.GetWindowText(hwndEx, succ, 512);
                int success = int.Parse(succ.ToString());
                if (success / min < 2)
                {
                    writeLogs(workingPath + "/log.txt", "Fail Too Much ---> success:" + success + ",min:" + min);//清空日志
                    return true;
                }
            }
            return false;
        }


        //获取MM成功数
        private int getMMSucc()
        {
            IntPtr hwnd = HwndUtil.FindWindow("WTWindow", null);
            IntPtr ButtonHwnd = HwndUtil.FindWindowEx(hwnd, IntPtr.Zero, "Button", "统计");
            IntPtr hwndEx = HwndUtil.FindWindowEx(ButtonHwnd, IntPtr.Zero, "Edit", null);
            hwndEx = HwndUtil.FindWindowEx(ButtonHwnd, hwndEx, "Edit", null);
            hwndEx = HwndUtil.FindWindowEx(ButtonHwnd, hwndEx, "Edit", null);
            hwndEx = HwndUtil.FindWindowEx(ButtonHwnd, hwndEx, "Edit", null);
            hwndEx = HwndUtil.FindWindowEx(ButtonHwnd, hwndEx, "Edit", null);
            try
            {
                return int.Parse(HwndUtil.GetControlText(hwndEx));
            }
            catch (Exception) {
                writeLogs(workingPath + "/log.txt", "获取mm成功失败！");
            }
            return 0;
        }

        //清理托盘
        private void refreshIcon()
        {
            Rectangle rect = new Rectangle();
            rect = Screen.GetWorkingArea(this);
            int startX = rect.Width - 200;//屏幕高
            int startY = rect.Height + 20;//屏幕高
            for (; startX < rect.Width; startX += 3)
            {
                MouseKeyboard.SetCursorPos(startX, startY);
            }
        }

        //任务监控
        private void taskMonitor()
        {
            refreshIcon();
            overTime = int.Parse(IniReadWriter.ReadIniKeys("Command", "cishu", pathShare + "/CF.ini"));
            int delay = 1000;
            if (adslName == "宽带连接")
            {
                overTime *= 2;
                delay /= 2;
            }
            int p = 0;
            int s = 0;
            bool isOnline = false;
            int circle = 0;
            do
            {
                if (ie8)
                {
                    IntPtr adslErr = HwndUtil.FindWindow("#32770", "连接到 " + adslName + " 时出错");
                    if (adslErr != IntPtr.Zero)
                    {
                        HwndUtil.closeHwnd(adslErr);
                    }
                }
                IntPtr adslExcp = HwndUtil.FindWindow("#32770", "网络连接");
                if (adslExcp != IntPtr.Zero)
                {
                    HwndUtil.closeHwnd(adslExcp);
                }
                isOnline = Net.isOnline();
                if (isOnline)
                {
                    long kbs = Net.GetNetStatic(adslName);
                    if(adslName != "宽带连接" && isAutoVote && kbs > 2048)
                    {
                        writeLogs(workingPath + "/log.txt",taskName+ "流量大于2M,拉黑！");
                        addVoteProjectNameDroped(false);
                        switchWatiOrder();
                    }
                }
                taskChange = IniReadWriter.ReadIniKeys("Command", "taskChange" + no, pathShare + "/Task.ini");
                if (taskChange.Equals("1"))
                {
                    taskChangeProcess(true);
                    return;
                }
                if (isSysTask())
                {
                    p = 0;
                }
                if (isAutoVote && (overTimeCount >= 2|| failTooMuch))
                {
                    writeLogs(workingPath + "/log.txt", "超时2次或一分钟成功低于2,拉黑！");
                    addVoteProjectNameDroped(false);
                    switchWatiOrder();
                }
                if (taskName.Equals(TASK_VOTE_JIUTIAN) && p > 0)
                {
                    if (jiutianOverCheck(ref s) || jiutianRestrictCheck() || jiutianVmBanCheck()|| isIdentifyCode())
                    {
                        switchWatiOrder();
                    }
                  
                }
                else if (taskName.Equals(TASK_VOTE_MM))
                {
                    if (mmOverCheck())
                    {
                        killProcess(false);
                        switchWatiOrder();
                    }
                }
                else if (taskName.Equals(TASK_VOTE_YUANQIU))
                {
                    if (yuanqiuOverCheck())
                    {
                        killProcess(false);
                        switchWatiOrder();
                    }
                }
                else if (taskName.Equals(TASK_VOTE_JT))
                {
                    if (jtOverCheck())
                    {
                        killProcess(false);
                        switchWatiOrder();
                    }
                }
                else if (taskName.Equals(TASK_VOTE_ML))
                {
                    //ML到票检测
                }
                else if (taskName.Equals(TASK_VOTE_DM))
                {
                    //DM到票检测
                }
                else if (taskName.Equals(TASK_VOTE_JZ))
                {
                    if (jzOverCheck())
                    {
                        killProcess(false);
                        switchWatiOrder();
                    }
                }
                else if (taskName.Equals(TASK_VOTE_HY))
                {
                    if (hyOverCheck())
                    {
                        killProcess(false);
                        switchWatiOrder();
                    }
                }
                else if (taskName.Equals(TASK_VOTE_OUTDO))
                {
                    //OUTDO到票检测
                }
                if (isOnline)
                {
                    p = p < 0 ? 1 : ++p;
                }
                else
                {
                    circle++;
                    p = p > 0 ? -1 : --p;

                }
                label2.Text = p.ToString();
                Thread.Sleep(delay);
            }
            while (p == 0 || (p > 0 && p < overTime) || (p < 0 && p > -overTime));
            if (taskName.Equals(TASK_VOTE_MM))
            {
                rasOperate("disconnect");
                //启动拨号定时timer
                rasOperate("connect");
            }
            else
            {
                taskChangeProcess(false);
                return;
            }
            taskMonitor();
        }

        //主线程
        public void _main()
        {
            //进程初始化部分
            string now = DateTime.Now.ToLocalTime().ToString();
            taskChange = IniReadWriter.ReadIniKeys("Command", "TaskChange" + no, pathShare + "/Task.ini");
            if (taskChange.Equals("1"))
            {
                taskChangeProcess(true);
            }
            else
            {
                cacheMemory = IniReadWriter.ReadIniKeys("Command", "CacheMemory" + no, pathShare + "/TaskPlus.ini");
                if (!StringUtil.isEmpty(cacheMemory))
                {
                    string[] arr = cacheMemory.Split('`');
                    taskName = arr[0].Substring(9);
                    taskPath = arr[1].Substring(9);
                    workerId = arr[2].Substring(7);
                    if (!StringUtil.isEmpty(workerId))
                    {
                        inputId = "1";
                        tail = "1";
                    }
                    if (taskPath.Equals("Writein"))
                    {
                        //if (taskName.Equals(TASK_HANGUP_MM2))
                        //{
                        //    taskPath = IniReadWriter.ReadIniKeys("Command", "mm2", pathShare + "/CF.ini");
                        //}
                    }
                    else
                    {
                        customPath = taskPath;
                    }

                    Process[] pros = getProcess("");
                    if (pros.Length > 0)
                    {
                        notifyIcon1.ShowBalloonTip(0, now, taskName + "运行中,进入维护状态", ToolTipIcon.Info);
                    }
                    else
                    {
                        if (taskPath.Equals("Writein"))
                        {
                            notifyIcon1.ShowBalloonTip(0, now, "发现项目缓存,通过写入路径启动" + taskName, ToolTipIcon.Info);
                        }
                        else
                        {
                            notifyIcon1.ShowBalloonTip(0, now, "发现项目缓存,通过自定义路径启动" + taskName, ToolTipIcon.Info);
                        }
                        changeTask();
                    }
                    IniReadWriter.WriteIniKeys("Command", "CacheMemory" + no, "", pathShare + "/TaskPlus.ini");
                }
                else
                {
                    //无缓存待命
                    taskName = TASK_SYS_WAIT_ORDER;
                    netCheck();
                    Thread.Sleep(1000);
                    if (adslName != "宽带连接")
                    {
                        rasOperate("disconnect");
                    }
                    notifyIcon1.ShowBalloonTip(0, now, "未发现项目缓存,待命中...\n请通过控制与监控端启动" + no + "号虚拟机", ToolTipIcon.Info);
                }
                try
                {
                    taskMonitor();
                }
                catch (ThreadAbortException)
                {

                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (isVoteTask())
            {
                int succ = 0;
                if (taskName.Equals(TASK_VOTE_JIUTIAN))
                {
                    timerChecked++;
                    succ = getJiutianSucc();
                }
                else if (taskName.Equals(TASK_VOTE_MM))
                {
                    timerChecked++;
                    succ = getMMSucc();
                }
                else if (taskName.Equals(TASK_VOTE_YUANQIU))
                {
                    timerChecked++;
                    succ = getYuanqiuSucc();
                }
                if (succ - succCount < 2 && timerChecked >= 2)
                {
                    failTooMuch = true;
                }
                writeLogs(workingPath + "/log.txt", "success:" + succ + " last:" + succCount);
                succCount = succ;
            }
        }
    }
}
