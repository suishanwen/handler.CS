﻿using System.Windows.Forms;

namespace handler.util
{
    class SwitchUtil
    {
        public static void swichVm(string vm1,string vm2,TextBox textBox,string customPath, string taskName,string pathShare)
        {
            if (StringUtil.isEmpty(textBox.Text))
            {
                if (!StringUtil.isEmpty(vm1) && !StringUtil.isEmpty(vm2))
                {
                    for (int i = int.Parse(vm1); i <= int.Parse(vm2); i++)
                    {
                        IniReadWriter.WriteIniKeys("Command", "CacheMemory" + i, "", pathShare + "/TaskPlus.ini");
                        IniReadWriter.WriteIniKeys("Command", "CustomPath" + i, customPath, pathShare + "/TaskPlus.ini");
                        IniReadWriter.WriteIniKeys("Command", "TaskName" + i, taskName, pathShare + "/Task.ini");
                        IniReadWriter.WriteIniKeys("Command", "TaskChange" + i, "1", pathShare + "/Task.ini");
                    }
                }
                else
                {
                    MessageBox.Show("虚拟机不能为空！");
                }

            }
            else
            {
                IniReadWriter.WriteIniKeys("Command", "CacheMemory" + textBox.Text, "", pathShare + "/TaskPlus.ini");
                IniReadWriter.WriteIniKeys("Command", "CustomPath" + textBox.Text, customPath, pathShare + "/TaskPlus.ini");
                IniReadWriter.WriteIniKeys("Command", "TaskName" + textBox.Text, taskName, pathShare + "/Task.ini");
                IniReadWriter.WriteIniKeys("Command", "TaskChange" + textBox.Text, "1", pathShare + "/Task.ini");
                textBox.Text = "";
            }
        }
    }
}
