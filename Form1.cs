using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


using WebSocketSharp;
using Newtonsoft;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Windows.Forms;

using Microsoft.Win32;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;

namespace WinFormsCrownSample
{
    public class ToolOption
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class ToolUpdateRootObject
    {
        public string message_type { get; set; }
        public string session_id { get; set; }
        public string show_overlay { get; set; }
        public string tool_id { get; set; }
        public List<ToolOption> tool_options { get; set; }
        public string play_task { get; set; }
    }

    public class CrownRegisterRootObject
    {
        public string message_type { get; set; }
        public string plugin_guid { get; set; }
        public string session_id { get; set; }
        public int PID { get; set; }
        public string execName { get; set; }
    }

    public class TaskOptions
    {
        public string current_tool { get; set; }
        public string current_tool_option { get; set; }
    }

    public class Globals
    {
        public static bool touch_state{get; set;}
        public static int on_list{get; set;}
        public static int start{get; set;}
    }
    
   
   
    public class CrownRootObject
    {
        public string message_type { get; set; }
        public int device_id { get; set; }
        public int unit_id { get; set; }
        public int feature_id { get; set; }
        public string task_id { get; set; }
        public string session_id { get; set; }
        public int touch_state { get; set; }
        public TaskOptions task_options { get; set; }
        public int delta { get; set; }
        public int ratchet_delta { get; set; }
        public int time_stamp { get; set; }
        public string state { get; set; }

        //public int IsTouch = 0;

        public int ListLength = 6;
        public int OnList = 0;
        //public int ListForward = 0;
    }

    public class ToolChangeObject
    {
        public string message_type { get; set; }
        public string session_id { get; set; }
        public string tool_id { get; set; }
    }

    class MyWebSocket
    {
        public static string sessionId = "";
        public static string lastcontext = "";
        public static bool sendContextChange = false;

        [DllImport("kernel32.dll")]
        public static extern bool ProcessIdToSessionId(uint dwProcessID, int pSessionID);

        [DllImport("Kernel32.dll", EntryPoint = "WTSGetActiveConsoleSessionId")]
        public static extern int WTSGetActiveConsoleSessionId();

        private static WebSocket client;
        public static string host1 = "wss://echo.websocket.org";
        public static string host = "ws://localhost:10134";
        public static List<CrownRootObject> crownObjectList = new List<CrownRootObject>();

        //Form1 form1 = new Form1();


        public static void toolChange(string contextName)
        {
            try
            {
                ToolChangeObject toolChangeObject = new ToolChangeObject();
                toolChangeObject.message_type = "tool_change";
                toolChangeObject.session_id = sessionId;
                toolChangeObject.tool_id = contextName;

                string s = JsonConvert.SerializeObject(toolChangeObject);
                client.Send(s);

            }
            catch (Exception ex)
            {
                string err = ex.Message;
            }
        }

     

        public static void updateUIWithDeserializedData(CrownRootObject crownRootObject)
        {
            //CrownRootObject crownRootObject = JsonConvert.DeserializeObject<CrownRootObject>(msg);

            if (crownRootObject.message_type == "deactivate_plugin")
                return;

            try
            {
                if (crownRootObject.message_type == "crown_turn_event")
                {                   

                   // received a crown turn event from Craft crown
                    Trace.Write("crown ratchet delta :" + crownRootObject.ratchet_delta + " slot delta = " + crownRootObject.delta + "\n");
                    if(crownRootObject.delta > 0 && crownRootObject.ratchet_delta > 0){
                        crownRootObject.OnList += 1;
                    }else if(crownRootObject.delta < 0 && crownRootObject.ratchet_delta < 0){
                        crownRootObject.OnList -= 1;
                    }
                    Globals.on_list = crownRootObject.OnList;
                    /*
                    if(crownRootObject.OnList > crownRootObject.ListLength){
                        crownRootObject.OnList -= crownRootObject.ListLength;
                    }else if(crownRootObject.OnList < 0){
                        crownRootObject.OnList += crownRootObject.ListLength;
                    }
                    */
                    //if 

                    Trace.Write("OnList : " + crownRootObject.OnList + "\n");
                    //Thread.Sleep(100);
                }

            }
            catch (Exception ex)
            {
                string str = ex.Message;
            }

        }

        public static void SetupUIRefreshTimer()
        {

            System.Timers.Timer timer = new System.Timers.Timer(70);
            timer.Enabled = true;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
            timer.Start();

            // reconnection watch dog 
            System.Timers.Timer reconnection_timer = new System.Timers.Timer(30000);
            reconnection_timer.Enabled = true;
            reconnection_timer.Elapsed += new System.Timers.ElapsedEventHandler(connection_watchdog_timer);
            
            // start watch dog by enabling timer here
            //timer3.Start();



        }

        public static void connection_watchdog_timer(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!client.IsAlive)
            {
                client = null;
                connectWithManager();

            }          

        }


        public static void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {                

                int totalDeltaValue = 0;
                int totalRatchetDeltaValue = 0;
                if (crownObjectList == null || crownObjectList.Count == 0)
                {
                    //Trace.Write("Queue is empty\n");
                    return;
                }
                else
                {
                    //Trace.Write("Queue size is: " + crownObjectList.Count + "\n");//
                }
                
                string currentToolOption = crownObjectList[0].task_options.current_tool_option;

                //Trace.Write("currentToolOption is: " + currentToolOption + "\n");//
                CrownRootObject crownRootObject = crownObjectList[0];
                int count = 0;
                for (int i = 0; i < crownObjectList.Count; i++)
                {
                    if (currentToolOption == crownObjectList[i].task_options.current_tool_option)
                    {
                        totalDeltaValue = totalDeltaValue + crownObjectList[i].delta;
                        totalRatchetDeltaValue = totalRatchetDeltaValue + crownObjectList[i].ratchet_delta;
                    }
                    else
                        break;

                    count++;
                }

                if (crownObjectList.Count >= 1)
                {
                    crownObjectList.Clear();

                    crownRootObject.delta = totalDeltaValue;
                    crownRootObject.ratchet_delta = totalRatchetDeltaValue;
                    //Trace.Write("Ratchet delta is :" + totalRatchetDeltaValue + "\n");//
                    updateUIWithDeserializedData(crownRootObject);

                }

            }
            catch (Exception ex)
            {
                string str = ex.Message;
            }
          

        }

        public static void wrapperUpdateUI(string msg)
        {
            Trace.Write("msg :" + msg + "\n");
            //form1.FormUpdate();
            CrownRootObject crownRootObject = JsonConvert.DeserializeObject<CrownRootObject>(msg);

            if ((crownRootObject.message_type == "crown_turn_event"))
            {
                crownObjectList.Add(crownRootObject);
                //Trace.Write("xxxx");//
                //Trace.Write("msg :" + msg + "\n");
            }
            else if (crownRootObject.message_type == "register_ack")
            {
                // save the session id as this is used for any communication with Logi Options 
                sessionId = crownRootObject.session_id;
                //toolChange("nothing");
                lastcontext = "";

                if (sendContextChange)
                {
                    sendContextChange = false;
                    MyWebSocket.toolChange("nothing");
                }
                else
                {

                    toolChange("nothing");
                }
                
            }
            else if (crownRootObject.message_type == "deactivate_plugin" || crownRootObject.message_type == "activate_plugin"  )
            {
                // our app has been activated or deactivated
            }
            else if (crownRootObject.message_type == "crown_touch_event")
            {
                // crown touch event
                Trace.Write("crown touch event :" + msg + "\n");
                if ( crownRootObject.touch_state == 1)
                Globals.touch_state = true;
                else
                Globals.touch_state = false;

                Globals.on_list = crownRootObject.OnList;


                //if(msg == "0") IsTouch = 0;
            }

  
        }

        public static void openUI(string msg)
        {
            string str = msg;
        }

        public static void closeConnection()
        {
          
        }


        public static void displayError(string msg)
        {
            string str = msg;
        }

        public static void connectWithManager()
        {
            try
            {
                client = new WebSocket(host);

                client.OnOpen += (ss, ee) =>
                    openUI(string.Format("Connected to {0} successfully", host));
                client.OnError += (ss, ee) =>
                    displayError("Error: " + ee.Message);

                client.OnMessage += (ss, ee) =>
                    wrapperUpdateUI(ee.Data);

                client.OnClose += (ss, ee) =>
                    closeConnection();

                client.Connect();

                // build the connection request packet 
                Process currentProcess = Process.GetCurrentProcess();
                CrownRegisterRootObject registerRootObject = new CrownRegisterRootObject();
                registerRootObject.message_type = "register";              
                registerRootObject.plugin_guid = "17c257a1-9773-4812-96f5-ec3fd7518012";
                registerRootObject.execName = "WinFormsCrownSample.exe";           
                registerRootObject.PID = Convert.ToInt32(currentProcess.Id);
                string s = JsonConvert.SerializeObject(registerRootObject);


                // only connect to active session process
                registerRootObject.PID = Convert.ToInt32(currentProcess.Id);
                int activeConsoleSessionId = WTSGetActiveConsoleSessionId();
                int currentProcessSessionId = Process.GetCurrentProcess().SessionId;

                // if we are running in active session?
                if (currentProcessSessionId == activeConsoleSessionId)
                {
                    client.Send(s);
                }
                else
                {
                    Trace.TraceInformation("Inactive user session. Skipping connect");
                }


            }
            catch (Exception ex)
            {
                string str = ex.Message;
            }
        }

        public static void init()
        {
            try
            {
                // setup timers 
                SetupUIRefreshTimer();

                // setup connnection 
                connectWithManager();
            }
            catch (Exception ex)
            {
                string str = ex.Message;
            }
        }

        
    }

    public partial class Form1 : Form
    {
        public Label Home;
        public Label ModeSetting;
        public Label Game1Mode;
        public Label Game2Mode;
        public Label Game3Mode;
        public Label Unlock;
        public Label Check;
        //public Button test;
        //CrownRootObject crownRootObject = new CrownRootObject();

        public Form1()
        {
            InitializeComponent();

            // start the connnection process 
            MyWebSocket.init();

            Globals.start = 50;

            this.Home = new Label();
            this.ModeSetting = new Label();
            this.Game1Mode = new Label();
            this.Game2Mode = new Label();
            this.Game3Mode = new Label();
            this.Unlock = new Label();
            this.Check = new Label();

            //this.test = new Button();
            CrownRootObject crownRootObject = new CrownRootObject();
           
            
            this. SuspendLayout();
            /*
            this.test.Location = new Point(10, 80);
            this.test.Size = new Size(20, 20);
            this.test.Click += new EventHandler(this.test_Click);
            */
            this.Home.Location = new Point(25, 10);
            this.Home.Font = new Font("Helvetica", 12F);
            this.Home.Name = "    Home   ";
            this.Home.Size = new Size(100, 20);

            this.ModeSetting.Location = new Point(125, 10);
            this.ModeSetting.Font = new Font("Helvetica", 12F);
            this.ModeSetting.Name = "ModeSetting";
            this.ModeSetting.Size = new Size(100, 20);

            this.Game1Mode.Location = new Point(250, 10);
            this.Game1Mode.Font = new Font("Helvetica", 12F);
            this.Game1Mode.Name = "Game1Mode";
            this.Game1Mode.Size = new Size(100, 20);

            this.Game2Mode.Location = new Point(375, 10);
            this.Game2Mode.Font = new Font("Helvetica", 12F);
            this.Game2Mode.Name = "Game2Mode";
            this.Game2Mode.Size = new Size(100, 20);

            this.Game3Mode.Location = new Point(500, 10);
            this.Game3Mode.Font = new Font("Helvetica", 12F);
            this.Game3Mode.Name = "Game3Mode";
            this.Game3Mode.Size = new Size(100, 20);

            this.Unlock.Location = new Point(625, 10);
            this.Unlock.Font = new Font("Helvetica", 12F);
            this.Unlock.Name = "Unlock";
            this.Unlock.Size = new Size(100, 20);
            
            this.Check.Location = new Point(25, 25);
            this.Check.Font = new Font("Helvetica", 12F);
            this.Check.Name = "Check";
            this.Check.Size = new Size(20, 20);
            this.Check.Text = "o";

            this.ClientSize = new Size(800, 125);
            this.Controls.Add(this.Home);
            this.Controls.Add(this.ModeSetting);
            this.Controls.Add(this.Game1Mode);
            this.Controls.Add(this.Game2Mode);
            this.Controls.Add(this.Game3Mode);
            this.Controls.Add(this.Unlock);
            this.Controls.Add(this.Check);
            //this.Controls.Add(this.test);
            this.BackColor = Color.LightGray;
            this.Name = "CraftDemo";
            this.Text = "CraftDemo";
            
            this. ResumeLayout(false);

            Timer MyTimer = new Timer();
            MyTimer.Interval = (100); // 45 mins
            MyTimer.Tick += new EventHandler(MyTimer_Tick);
            MyTimer.Start();


        }

        private void MyTimer_Tick(object sender, EventArgs e)
        {
       
             
            if(Globals.touch_state == true) 
            { 
                this.Home.Text = "   Home   ";
                this.ModeSetting.Text = "ModeSetting";
                this.Game1Mode.Text = "Game1Mode";
                this.Game2Mode.Text = "Game2Mode";
                this.Game3Mode.Text = "Game3Mode";
                this.Unlock.Text = "     Unlock";
                this.Check.Text = "o";
            }
            else if(Globals.touch_state == false) 
            {
                this.Home.Text = "";
                this.ModeSetting.Text = "";
                this.Game1Mode.Text = "";
                this.Game2Mode.Text = "";
                this.Game3Mode.Text = "";
                this.Unlock.Text = "";
                this.Check.Text = "";
            }

            

            if(Globals.start + Globals.on_list*125 > 700){
                this.Check.Location = new Point(Globals.start + Globals.on_list*125 - 625, 25);
                Globals.start = Globals.start + Globals.on_list*125 - 625;
            }
            else if(Globals.start + Globals.on_list*125 < 40){
                this.Check.Location = new Point(Globals.start + Globals.on_list*125 + 625, 25);
                Globals.start = Globals.start + Globals.on_list*125 + 625;
            }
            else{
                this.Check.Location = new Point(Globals.start + Globals.on_list*125, 25);
                Globals.start = Globals.start + Globals.on_list*125;
            }
        }

        /*
        private void test_Click(object sender, EventArgs e){
            if(crownRootObject == null) Home.Text = "xxx";
            else if(crownRootObject.touch_state > 0) Home.Text = "Home";
            else Home.Text = "";
        }
        */
        
        
        
    }
   
}

