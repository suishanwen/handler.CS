using System;
using System.Collections.Generic;

namespace handler.util
{
    class TaskInfo
    {

        public string ProjectName { get; set; }
        public double Price { get; set; }

        public TaskInfo(string projectName, double price)
        {
            ProjectName = projectName;
            Price = price;
        }
    }

    class TaskInfos
    {
        public static string pathName = "";

        public static void Init(string iniPathName)
        {
            pathName = iniPathName;
        }



        public static TaskInfo Get(int vm)
        {
            Dictionary<int, TaskInfo> taskInfoDict = new Dictionary<int, TaskInfo>();
            try
            {
                string taskInfos = IniReadWriter.ReadIniKeys("Command", "TaskInfos", pathName).Trim();
                if (taskInfos.Length > 0)
                {
                    string[] taskInfoArray = taskInfos.Split('|');
                    foreach (string taskInfo in taskInfoArray)
                    {
                        int key = int.Parse(taskInfo.Substring(0, taskInfo.IndexOf(":")));
                        string[] task = taskInfo.Substring(taskInfo.IndexOf(":") + 1).Split('-');
                        taskInfoDict.Add(key, new TaskInfo(task[0], double.Parse(task[1])));
                    }
                }
            }
            catch (Exception) { }
            if (taskInfoDict.ContainsKey(vm))
            {
                return taskInfoDict[vm];
            }
            return null;
        }


    }
}
