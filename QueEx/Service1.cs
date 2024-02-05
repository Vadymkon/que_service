using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace QueEx
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }
        //private Thread TH1;

        /*TH1 = new Thread(CodeAct);
        TH1.Start();*/
        protected override void OnStart(string[] args)
        {
            //    Thread t = new Thread(new ThreadStart(CodeAct ));
            Thread t = new Thread(() => { CodeAct(); });
            t.Start();



        }

        void CodeAct() {

            Dictionary<String, double> TasksQue = new Dictionary<string, double> { };
            bool Thread2NotWorking = true;
            log("The Service is starting...", 0);

            Thread t2 = new Thread(async () =>
            {
            log("The t2 is starting...", 0);
                Thread2NotWorking = false;
                while (TasksQue.Count > 0)
                {
                    for (int i = 0; i < getReg("3") && i < TasksQue.Count; i++) //if tasks are - make it
                    {
                        String TaskNamebyIndex = TasksQue.Keys.ToList()[i]; //bcs Dictionary
                        TasksQue[TaskNamebyIndex] += 100.0/getReg("2"); // ++percents
                        String newTaskName = newStateOfClaim(TaskNamebyIndex, Convert.ToInt32(TasksQue[TaskNamebyIndex])); //
                        RENAME_RegKey(ref TasksQue, TaskNamebyIndex, newTaskName);
                        if (!newTaskName.Contains(".")) // if Task COMPLETED
                        {
                            TasksQue.Remove(newTaskName);//if already not contain "."
                            log(newTaskName.Substring(0, 9), 4);
                        }
                        await Task.Delay(2000);
                    }
                }
                Thread2NotWorking = true;
            });

            Thread t1 = new Thread(async () =>
            { 
                while (true)
                {
                    List<String> newClaims = getNewClaimsElementsList();
                    if (newClaims.Count != 0)
                    {
                        var trashList = new List<String>{ };
                        newClaims.ForEach( //remove wrong tasks
                            x =>
                            {
                                if (!regexCheckClaim(x))
                                {
                                    deleteRegKey(x);
                                    log(x, code: 2);
                                    trashList.Add(x);
                                }
                                getClaimsList().ForEach(y =>
                                {
                                    if (x == y.Substring(0, 9))
                                    {
                                        deleteRegKey(x);
                                        log(x, code: 3);
                                        trashList.Add(x);
                                    }
                                });
                            });
                        trashList.ForEach(x => newClaims.Remove(x)); //clean list (need bcs foreach-delete problem)
                        //after cleaning
                    }
                    if (newClaims.Count != 0)
                    { 
                    //Substring PEREDELATI
                    //ONLY 1 TASK with MIN number
                    String taskMinClaim = $"Task_{toNumb4(newClaims.Min(x => Convert.ToInt32(x.Substring(5, 4))))}"; //minimal
                    newClaims.Clear(); //newClaims is not needed now
                    log(taskMinClaim + " мінімальна таска", 0); //checking
                    TasksQue.Add(taskMinClaim, 0); //add it to QUE
                    RENAME_RegKey(ref TasksQue, taskMinClaim, newStateOfClaim(taskMinClaim, 0)); //queing claim-task
                    if (TasksQue.Count > 0 && Thread2NotWorking) t2.Start(); //if t2 is not already working so ... START IT!
                    }
                    await Task.Delay(getReg("1") * 1000); //pause in seconds
                }
            });
             t1.Start();
        }
        protected override void OnStop()
        {

            //if somethink IS NOT done, so reunit them;
            String path = @"Software\Task_Queue\Claims";
              List<String> Claims = new List<String>();
              RegistryKey key = Registry.LocalMachine.OpenSubKey(path,true); // @"SOFTWARE\KONCHA"
              if (key != null && key.GetSubKeyNames().Length != 0)
              {
                Claims.AddRange(key.GetSubKeyNames());
                Claims.ForEach(x =>
                {
                    if (x.Length > 10 && !x.Contains("COMPLETED"))
                    {
                        deleteRegKey(x);
                        key.CreateSubKey(x.Substring(0,9));
                        log($"Task {x.Substring(0,9)} Reunit", 0);
                    }
                });
              }
          //  Cmd("net start queservice");
        }

        /* DICTIONARY
         * taskname : percentdone
         Dictionary<String,double> Tasks = new Dictionary<string, double>
        {
            { "Tom", 3.4},
            { "Sam", 2.5}
        };
        */


        /* 1 thread
         
         * follows HKLM/Software/Task_Queue/Claims for claims
         * checks their correctlys(dont takes somethink with [III...])
            not ==> remove it, loged
         * AND GET ONLY ONE WITH MIN-NUMBER))0)
            add to que, remake one (with Queued name), loged
         * if (que.have.somethink and Thread2NotWorking(*bool)) start2Thread
         * time delay (Y from reg) // HKLM/Software/Task_Queue/Parameters/Task_Claim_Check_Period
         */

        /* 2 thread (renamer)
          WORKS ONLY IF THREAD 1 START IT
        
        while (que.have.somethink) {
        bool works = true;
                // que for one-time-making
        * for (int Y = HKLM/Software/Task_Queue/Parameters/Task_Execution_Quantity) 
        * Tasks[name] += getReg("Task_Execution_Duration")/100;
        * RENAME(claimY, newStateOfClaim(claimY, Tasks[name])
    
            * checks is there "." includes in name. 
         if yes ==> delete from que-list

         delay (2s)
        }
        */

        /* NEEDS FUNCTIONS:
         * 
         * + int getRegValue(path)
         * 
         * + string[] getNewClaimsElementsList (if includes "[..II], Queued, progress" ignores)
         * 
         * + bool regexCheckClaim()
         * 
         * + log(string name, int code) //logs by name and code-script
         * 
         * + deleteRegKey(string name)
         * 
         * + RENAME_RegKey(string oldname, string newname)
         * 
         * + string newStateOfClaim(string name) // see what state now, and return next state
         * 
         */



        /// <param name="parametr">Put number of param
        ///  <para>* Task_Claim_Check_Period - 1</para> 
        ///  <para>* Task_Execution_Duration - 2</para> 
        ///  <para>* Task_Execution_Quantity - 3</para>   
        /// </param>
        int getReg(String parametr, String path = @"Software\Task_Queue\Parameters")
        {
            switch (parametr)
            {
                case "1":
                    parametr = "Task_Claim_Check_Period";
                    break;
                case "2":
                    parametr = "Task_Execution_Duration";
                    break;
                case "3":
                    parametr = "Task_Execution_Quantity";
                    break;
                default:
                    break;
            }
            RegistryKey key = Registry.LocalMachine.OpenSubKey(path); // @"SOFTWARE\KONCHA"
            if (key != null && key.GetValue(parametr) != null)
            {

                String showmessage = "";
                showmessage = key.GetValue(parametr).ToString();
                return Convert.ToInt32(showmessage);
                log(showmessage,0);

                /*
                 * MULTISTRING
                var stringList = new List<string>(key.GetValue(parametr) as string[]);
                stringList.ForEach(a => showmessage += $"{a}\n");
                MessageBox.Show(showmessage);
                */
            }
            else log($"nope {key == null}",0);
            return -1;
        }

        List<String> getNewClaimsElementsList(String path = @"Software\Task_Queue\Claims")
        {

            List<String> Claims = new List<String>();


            RegistryKey key = Registry.LocalMachine.OpenSubKey(path); // @"SOFTWARE\KONCHA"
            if (key != null && key.GetSubKeyNames().Length != 0)
            {
                Claims.AddRange(new List<string>(key.GetSubKeyNames() as string[])
                    .Where(x => !x.ToLower().Contains("completed") && !x.ToLower().Contains("progress") && !x.ToLower().Contains("queued")));
            }
            else log($"Nothing in Claims",0);
            return Claims;
        }
        List<String> getClaimsList(String path = @"Software\Task_Queue\Claims")
        {

            List<String> Claims = new List<String>();
            RegistryKey key = Registry.LocalMachine.OpenSubKey(path); // @"SOFTWARE\KONCHA"
            if (key != null && key.GetSubKeyNames().Length != 0)
                Claims.AddRange(key.GetSubKeyNames().ToList().Where(x => x.Length > 9));
            else log($"Nothing in Claims",0);
            return Claims;
        }

        bool regexCheckClaim(string line)
        {
            return new Regex(@"^Task_\d{4}").IsMatch(line);
        }


        /// <param name="code">Put number of code
        ///  <para>* 1 - SuccessBegun</para> 
        ///  <para>* 2 - SyntaxError</para> 
        ///  <para>* 3 - Already have</para>   
        ///  <para>* 4 - COMPLETED</para>   
        /// </param>
        void log(String name, int code)
        {
            String message = "";
            switch (code)
            {
                case 1:
                    message = $"Задача {name} успішно прийнята в обробку...";
                    break;
                case 2:
                    message = $"ПОМИЛКА розміщення заявки {name}. Некоректний синтаксис ...";
                    break;
                case 3:
                    message = $"ПОМИЛКА розміщення заявки {name}. Номер вже існує ...";
                    break;
                case 4:
                    message = $"Задача {name} успішно ВИКОНАНА!";
                    break;
                default:
                    message = name;
                    break;
            }
            File.AppendAllText ($@"C:\Windows\Logs\TaskQueue_{DateTime.Today.ToShortDateString().Replace("/", "-")}.log", $"-----------------------------------<{DateTime.UtcNow}>-----------------------------------\r\n");
            File.AppendAllText ($@"C:\Windows\Logs\TaskQueue_{DateTime.Today.ToShortDateString().Replace("/", "-")}.log", message+"\r\n");
        }    

        void deleteRegKey(String name, String path = @"Software\Task_Queue\Claims")
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(path,true); // @"SOFTWARE\KONCHA"
            if (key == null)
            {
                log("There's key is null.", 0);
                return; 
            }
           // key.GetSubKeyNames().ToList().ForEach(x => log($"'{x}'",0));
            if (!key.GetSubKeyNames().Contains(name))
            {
                log($"This no in RegSubKeys like {name}. But it trying to delete some regKey. Somethink wrong. ErrorSource: deleteRegKey", 0);
                return;
            }
            key.DeleteSubKeyTree(name);
            
        }

        String newStateOfClaim(String taskName, int percentDone)
        {
            taskName = taskName.Trim();
            if (taskName.Length == 9) return taskName + "-[....................]-Queued";
            taskName = taskName.Substring(0, 11);
            int countI = percentDone / 5;
            for (int i = 0; i < countI; i++) taskName += "I"; //add "I"
            for (int i = 0; i < 20 - countI; i++) taskName += "."; //add "."
            //close + Progress info | or if . = 0 then COMPLETED 
            if (percentDone == 100) { 
                taskName += "]-COMPLETED";
                log(taskName.Substring(0, 11), 4);
            }
            else taskName += $"]-In progress - {percentDone} % completed";

            return taskName;
        }
        void RENAME_RegKey(ref Dictionary<String, double> TasksQue, String oldname, String newname, String path = @"Software\Task_Queue\Claims")
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(path,true); // @"SOFTWARE\KONCHA"

            if (key == null)
            {
                log("There's key is null.  Source: RENAME_RegKey ", 0);
                return;
            }

          //  key.GetSubKeyNames().ToList().ForEach(x => log($"'{x}'", 0));
            if (!key.GetSubKeyNames().Contains(oldname))
            {
                log($"This no in RegSubKeys like '{oldname}' . Source: RENAME_RegKey ", 0);
                return;
            }

            double pOfTasks = TasksQue[oldname];
                deleteRegKey(oldname);          TasksQue.Remove(oldname);
                key.CreateSubKey(newname);      TasksQue.Add(newname, pOfTasks);
        }

        //1 --> 0001
        string toNumb4(int line)
        {
            string answer = "";
            for (int i = 0; i < 4-line.ToString().Length; i++)
                answer += "0";
            answer += line;
            return answer;
        }

        void Cmd(string line)
        {
            Process.Start(new ProcessStartInfo { FileName = "cmd", Arguments = $"/c {line}", WindowStyle = ProcessWindowStyle.Hidden });
        }

    }
}
