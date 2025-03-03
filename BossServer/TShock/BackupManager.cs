﻿/*
TShock, a server mod for Terraria
Copyright (C) 2011-2019 Pryaxis & TShock Contributors

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.IO;
using System.Threading;
using Terraria;

namespace TShockAPI
{
    public class BackupManager
    {
        public string BackupPath { get; set; }
        public int Interval { get; set; }
        public int KeepFor { get; set; }

        private DateTime lastbackup = DateTime.UtcNow;

        public BackupManager(string path)
        {
            BackupPath = path;
        }

        public bool IsBackupTime
        {
            get { return (Interval > 0) && ((DateTime.UtcNow - lastbackup).TotalMinutes >= Interval); }
        }

        public void Backup()
        {
            lastbackup = DateTime.UtcNow;
            Thread t = new Thread(() =>
            {
                DoBackup(null);
                DeleteOld(null);
            });
            t.Name = GetString("Backup Thread");
            t.Start();

            // ThreadPool.QueueUserWorkItem(DoBackup);
            // ThreadPool.QueueUserWorkItem(DeleteOld);
        }

        private void DoBackup(object o)
        {
            try
            {
                string worldname = Main.worldPathName;
                string name = Path.GetFileName(worldname);

                Main.ActiveWorldFileData._path = Path.Combine(BackupPath, string.Format("{0}.{1:yyyy-MM-ddTHH.mm.ssZ}.bak", name, DateTime.UtcNow));

                string worldpath = Path.GetDirectoryName(Main.worldPathName);
                if (worldpath != null && !Directory.Exists(worldpath))
                    Directory.CreateDirectory(worldpath);

                if (TShock.Config.Settings.ShowBackupAutosaveMessages)
                {
                    TSPlayer.All.SendInfoMessage(GetString("Server map saving..."));
                }
                Console.WriteLine(GetString("Backing up world..."));

                SaveManager.Instance.SaveWorld();
                Console.WriteLine(GetString("World backed up."));
                Console.ForegroundColor = ConsoleColor.Gray;
                TShock.Log.Info(GetString("World backed up ({0}).", Main.worldPathName));

                Main.ActiveWorldFileData._path = worldname;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(GetString("Backup failed!"));
                Console.ForegroundColor = ConsoleColor.Gray;
                TShock.Log.Error(GetString("Backup failed!"));
                TShock.Log.Error(ex.ToString());
            }
        }

        private void DeleteOld(object o)
        {
            if (KeepFor <= 0)
                return;
            foreach (var fi in new DirectoryInfo(BackupPath).GetFiles("*.bak"))
            {
                if ((DateTime.UtcNow - fi.LastWriteTimeUtc).TotalMinutes > KeepFor)
                {
                    fi.Delete();
                }
            }
        }
    }
}
