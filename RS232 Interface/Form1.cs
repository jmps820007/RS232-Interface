using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Timers;

namespace RS232_Interface
{
    public partial class Form1 : Form
    {
        private SerialPort sp = new SerialPort();       //建立串列傳輸物件
        bool isOpen = false;                            //串口打開旗標
        bool isSetProperty = false;                     //參數設定旗標
        bool isHex = false;                             //16進制顯示旗標
        bool isFileOpen = false;                        //文件開啟旗標
        string RecvDataTextL = null;                    //

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //鎖住視窗大小
            this.MaximumSize = this.Size;                       
            this.MinimumSize = this.Size;
            this.MinimizeBox = false;

            for(int i = 0; i < 10; i++)
            {
                cbxComPort.Items.Add("COM" + (i + 1).ToString());
            }

            cbxComPort.SelectedIndex = 0;              //設定顯示的 COM 預設值

            //列出常用的鮑率
            cbxBaudRate.Items.Add("1200");
            cbxBaudRate.Items.Add("2400");
            cbxBaudRate.Items.Add("4800");
            cbxBaudRate.Items.Add("9600");
            cbxBaudRate.Items.Add("19200");
            cbxBaudRate.Items.Add("38400");
            cbxBaudRate.Items.Add("43000");
            cbxBaudRate.Items.Add("56000");
            cbxBaudRate.Items.Add("57600");
            cbxBaudRate.Items.Add("115200");
            cbxBaudRate.Items.Add("117600");
            cbxBaudRate.Items.Add("240000");
            cbxBaudRate.SelectedIndex = 3;

            //列出停止位
            cbxStopBits.Items.Add("0");
            cbxStopBits.Items.Add("1");
            cbxStopBits.Items.Add("1.5");
            cbxStopBits.Items.Add("2");
            cbxStopBits.SelectedIndex = 1;

            //列出資料長度
            cbxDataBits.Items.Add("8");
            cbxDataBits.Items.Add("7");
            cbxDataBits.SelectedIndex = 0;

            //列出奇偶位
            cbxParity.Items.Add("None");
            cbxParity.Items.Add("Odd");
            cbxParity.Items.Add("Even");
            cbxParity.SelectedIndex = 0;

            rdbChar.Checked = true;
        }

        private void btnCheckCom_Click(object sender, EventArgs e)              //檢查COM按鈕
        {
            bool comExistence = false;                  //有無COM旗標
            cbxComPort.Items.Clear();                   //清除當前COM列表清單

            for (int i = 0; i < 10; i++)                //循環檢查每個COM
            {
                try
                {
                    SerialPort sp = new SerialPort("COM" + (i + 1).ToString());
                    sp.Open();
                    sp.Close();
                    cbxComPort.Items.Add("COM" + (i + 1).ToString());
                    comExistence = true;
                }
                catch(Exception)
                {
                    continue;
                }
            }
            if (comExistence)
            {
                cbxComPort.SelectedIndex = 0;           //選擇第一個找到的連接埠
            }
            else
            {
                MessageBox.Show("沒有任何可用的COM", "錯誤");
            }
        }
        private bool CheckPortSetting()     //檢查連接埠是否設定
        {
            if (cbxComPort.Text.Trim() == "") return false;
            if (cbxBaudRate.Text.Trim() == "") return false;
            if (cbxDataBits.Text.Trim() == "") return false;
            if (cbxParity.Text.Trim() == "") return false;
            if (cbxStopBits.Text.Trim() == "") return false;
            return true;
        }

        private bool CheckSendData()        //檢查發送數據textbox有無內容
        {
            if (tbxReceiveData.Text.Trim() == "") return false;
            return true;
        }

        private void SetPortProperty()      //設定連接埠
        {
            sp = new SerialPort();
            sp.PortName = cbxComPort.Text.Trim();                       //設定COM
            sp.BaudRate = Convert.ToInt32(cbxBaudRate.Text.Trim());     //設定Baudrate
            sp.DataBits = Convert.ToInt32(cbxDataBits.Text.Trim());     //設定資料長度
            sp.ReadTimeout = -1;                                        //設置超時讀取時間，設置為infinite
            float f = Convert.ToSingle(cbxStopBits.Text.Trim());        //設定Stopbit
            if (f == 0)
            { sp.StopBits = StopBits.None; }
            else if (f == 1)
            { sp.StopBits = StopBits.One; }
            else if (f == 1.5)
            { sp.StopBits = StopBits.OnePointFive; }
            else if (f == 2)
            { sp.StopBits = StopBits.Two; }
            else                                                        //Stopbit預設為1
            { sp.StopBits = StopBits.One; }

            string s = cbxParity.Text.Trim();                           //設置檢查位
            if (s.CompareTo("None") == 0) 
            { sp.Parity = Parity.None; }
            else if (s.CompareTo("Odd") == 0)
            { sp.Parity = Parity.Odd; }
            else if (s.CompareTo("Even") == 0)
            { sp.Parity = Parity.Even; }
            else
            { sp.Parity = Parity.None; }

            //Control.CheckForIllegalCrossThreadCalls = false;
            //定義Data Received 事件，當連接埠收到數據後觸發事件
            sp.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
            if (rdbHex.Checked)
            { isHex = true; }
            else
            { isHex = false; }
        }



        private void btnOpenCom_Click(object sender, EventArgs e)
        {
            if (isOpen == false)
            {
                if (!CheckPortSetting())    //檢查通訊埠設置
                {
                    MessageBox.Show("請設定通訊埠參數!", "錯誤");
                    return;
                }
                if(isSetProperty ==false)   //如通訊埠未設定，設定通訊埠參數
                {
                    SetPortProperty();
                    isSetProperty = true;
                }
                try  //打開通訊埠
                {
                    sp.Open();
                    isOpen = true;
                    btnOpenCom.Text = "關閉通訊埠";
                    cbxBaudRate.Enabled = false;
                    cbxComPort.Enabled = false;
                    cbxDataBits.Enabled = false;
                    cbxParity.Enabled = false;
                    cbxStopBits.Enabled = false;
                }
                catch(Exception)
                {
                    isSetProperty = false;
                    isOpen = false;
                    MessageBox.Show("通訊埠開啟失敗", "錯誤");
                }
            }
            else
            {
                try
                {
                    sp.Close();
                    isOpen = false;
                    isSetProperty = false;
                    btnOpenCom.Text = "開啟通訊埠";
                    cbxBaudRate.Enabled = true;
                    cbxComPort.Enabled = true;
                    cbxDataBits.Enabled = true;
                    cbxParity.Enabled = true;
                    cbxStopBits.Enabled = true;
                }
                catch (Exception)
                {

                }
            }
        }

        private void btnSend_Click(object sender, EventArgs e)      //發送數據
        {
            if (CheckSendData() == false)
            {
                MessageBox.Show("請輸入數據");
                return;
            }

            if(isOpen == true)      //數據寫入通訊埠
            {
                try
                {
                    System.Text.UTF8Encoding utf8 = new System.Text.UTF8Encoding();
                    Byte[] writeBytes = utf8.GetBytes(tbxSendData.Text);
                    sp.Write(writeBytes, 0, writeBytes.Length);
                }
                catch (Exception)
                {
                    MessageBox.Show("發送數據錯誤", "錯誤");
                    return;
                }
            }
            else
            {
                MessageBox.Show("通訊埠未打開");
                return;
            }
        }

        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)      //接收數據
        {
            System.Threading.Thread.Sleep(100);   //延時100ms 等待接收數據完成
            //多執行緒無法直接調用介面控制元件(由主執行緒建立)會導致衝突，使用invoke跨執行緒使用 UI(主執行緒)修改其狀態
            try
            {
                if (isHex == false)
                {
                    System.Text.UTF8Encoding utf8 = new System.Text.UTF8Encoding();
                    Byte[] readBytes = new Byte[sp.BytesToRead];
                    sp.Read(readBytes, 0, readBytes.Length);
                    String decodedString = utf8.GetString(readBytes);
                    SetText(decodedString + " ");
                }
                else
                {
                    Byte[] ReceivedData = new Byte[sp.BytesToRead];
                    sp.Read(ReceivedData, 0, ReceivedData.Length);
                    string RecvDataText = null;
                    string s = string.Empty;
                    for (int i = 0; i < ReceivedData.Length; i++)
                    {
                        RecvDataText += (ReceivedData[i].ToString("X2") + " ");
                        s += (char)ReceivedData[i];
                    }
                    SetText(RecvDataText + " ");
                    RecvDataTextL = s;
                }
            }
            catch (Exception)
            {

            }
        }

        delegate void SetTextCallback(string text);
        private void SetText(string text)
        {
            if(this.tbxReceiveData.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.tbxReceiveData.Text += text;
            }
        }


        private void btnCleanData_Click(object sender, EventArgs e)     //清除數據
        {
            tbxReceiveData.Text = "";
            tbxSendData.Text = "";
            RecvDataTextL = "";
        }
    }
}
