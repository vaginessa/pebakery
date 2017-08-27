﻿/*
    Copyright (C) 2016-2017 Hajin Jang
    Licensed under GPL 3.0
 
    PEBakery is free software: you can redistribute it and/or modify
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

using PEBakery.Exceptions;
using PEBakery.Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PEBakery.Core.Commands
{
    public static class CommandPlugin
    {
        public static List<LogInfo> ExtractFile(EngineState s, CodeCommand cmd)
        {
            List<LogInfo> logs = new List<LogInfo>();

            Debug.Assert(cmd.Info.GetType() == typeof(CodeInfo_ExtractFile));
            CodeInfo_ExtractFile info = cmd.Info as CodeInfo_ExtractFile;

            string pluginFile = StringEscaper.Unescape(info.PluginFile);
            string dirName = StringEscaper.Preprocess(s, info.DirName);
            string fileName = StringEscaper.Preprocess(s, info.FileName);
            string extractTo = StringEscaper.Preprocess(s, info.ExtractTo);

            bool inCurrentPlugin = false;
            if (info.PluginFile.Equals("%PluginFile%", StringComparison.OrdinalIgnoreCase))
                inCurrentPlugin = true;
            else if (info.PluginFile.Equals("%ScriptFile%", StringComparison.OrdinalIgnoreCase))
                inCurrentPlugin = true;

            s.MainViewModel.BuildCommandProgressBarValue = 300;

            Plugin targetPlugin;
            if (inCurrentPlugin)
            {
                targetPlugin = s.CurrentPlugin;
            }
            else
            {
                string fullPath = StringEscaper.ExpandVariables(s, pluginFile);
                targetPlugin = s.Project.GetPluginByFullPath(fullPath);
                if (targetPlugin == null)
                    throw new ExecuteException($"No plugin in [{fullPath}]");
            }

            if (StringEscaper.PathSecurityCheck(extractTo, out string errorMsg) == false)
            {
                logs.Add(new LogInfo(LogState.Error, errorMsg));
                return logs;
            }

            s.MainViewModel.BuildCommandProgressBarValue = 600;

            string destPath;
            if (Directory.Exists(extractTo)) // extractTo is dir
            {
                destPath = Path.Combine(extractTo, fileName);
            }
            else // extractTo is file
            {
                if (File.Exists(extractTo))
                    logs.Add(new LogInfo(LogState.Ignore, $"File [{extractTo}] will be overwritten"));
                else
                    Directory.CreateDirectory(Path.GetDirectoryName(extractTo));
                destPath = extractTo;
            }

            using (MemoryStream ms = EncodedFile.ExtractFile(targetPlugin, dirName, fileName))
            using (FileStream fs = new FileStream(destPath, FileMode.Create, FileAccess.Write))
            {
                ms.Position = 0;
                ms.CopyTo(fs);
                ms.Close();
                fs.Close();
            }

            s.MainViewModel.BuildCommandProgressBarValue = 900;

            logs.Add(new LogInfo(LogState.Success, $"Encoded file [{fileName}] extracted to [{extractTo}]"));

            return logs;
        }

        public static List<LogInfo> ExtractAndRun(EngineState s, CodeCommand cmd)
        {
            List<LogInfo> logs = new List<LogInfo>();

            Debug.Assert(cmd.Info.GetType() == typeof(CodeInfo_ExtractAndRun));
            CodeInfo_ExtractAndRun info = cmd.Info as CodeInfo_ExtractAndRun;

            string pluginFile = StringEscaper.Unescape(info.PluginFile);
            string dirName = StringEscaper.Preprocess(s, info.DirName);
            string fileName = StringEscaper.Preprocess(s, info.FileName);
            List<string> parameters = StringEscaper.Preprocess(s, info.Params);

            bool inCurrentPlugin = false;
            if (info.PluginFile.Equals("%PluginFile%", StringComparison.OrdinalIgnoreCase))
                inCurrentPlugin = true;
            else if (info.PluginFile.Equals("%ScriptFile%", StringComparison.OrdinalIgnoreCase))
                inCurrentPlugin = true;

            s.MainViewModel.BuildCommandProgressBarValue = 200;

            Plugin targetPlugin;
            if (inCurrentPlugin)
            {
                targetPlugin = s.CurrentPlugin;
            }
            else
            {
                string fullPath = StringEscaper.ExpandVariables(s, pluginFile);
                targetPlugin = s.Project.GetPluginByFullPath(fullPath);
                if (targetPlugin == null)
                    throw new ExecuteException($"No plugin in [{fullPath}]");
            }

            string destPath = FileHelper.CreateTempFile();
            if (StringEscaper.PathSecurityCheck(destPath, out string errorMsg) == false)
            {
                logs.Add(new LogInfo(LogState.Error, errorMsg));
                return logs;
            }

            s.MainViewModel.BuildCommandProgressBarValue = 400;

            using (MemoryStream ms = EncodedFile.ExtractFile(targetPlugin, dirName, info.FileName))
            using (FileStream fs = new FileStream(destPath, FileMode.Create, FileAccess.Write))
            {
                ms.Position = 0;
                ms.CopyTo(fs);
                ms.Close();
                fs.Close();
            }

            s.MainViewModel.BuildCommandProgressBarValue = 600;

            Process proc = new Process();
            proc.StartInfo.FileName = destPath;
            proc.StartInfo.UseShellExecute = true;
            proc.StartInfo.Verb = "Open";
            proc.Start();

            s.MainViewModel.BuildCommandProgressBarValue = 800;

            logs.Add(new LogInfo(LogState.Success, $"Encoded file [{fileName}] extracted and executed"));

            return logs;
        }

        public static List<LogInfo> ExtractAllFiles(EngineState s, CodeCommand cmd)
        {
            List<LogInfo> logs = new List<LogInfo>();

            Debug.Assert(cmd.Info.GetType() == typeof(CodeInfo_ExtractAllFiles));
            CodeInfo_ExtractAllFiles info = cmd.Info as CodeInfo_ExtractAllFiles;

            string pluginFile = StringEscaper.Unescape(info.PluginFile);
            string dirName = StringEscaper.Preprocess(s, info.DirName);
            string extractTo = StringEscaper.Preprocess(s, info.ExtractTo);

            bool inCurrentPlugin = false;
            if (info.PluginFile.Equals("%PluginFile%", StringComparison.OrdinalIgnoreCase))
                inCurrentPlugin = true;
            else if (info.PluginFile.Equals("%ScriptFile%", StringComparison.OrdinalIgnoreCase))
                inCurrentPlugin = true;

            s.MainViewModel.BuildCommandProgressBarValue = 100;

            Plugin targetPlugin;
            if (inCurrentPlugin)
            {
                targetPlugin = s.CurrentPlugin;
            }
            else
            {
                string fullPath = StringEscaper.ExpandVariables(s, pluginFile);
                targetPlugin = s.Project.GetPluginByFullPath(fullPath);
                if (targetPlugin == null)
                    throw new ExecuteException($"No plugin in [{fullPath}]");
            }

            if (StringEscaper.PathSecurityCheck(extractTo, out string errorMsg) == false)
            {
                logs.Add(new LogInfo(LogState.Error, errorMsg));
                return logs;
            }

            s.MainViewModel.BuildCommandProgressBarValue = 200;

            List<string> dirs = cmd.Addr.Plugin.Sections["EncodedFolders"].Lines;
            bool dirNameValid = dirs.Any(d => d.Equals(dirName, StringComparison.OrdinalIgnoreCase));
            if (dirNameValid == false)
                throw new ExecuteException($"Folder [{dirName}] not exists in [{pluginFile}]");

            
            if (Directory.Exists(extractTo) == false)
            {
                if (File.Exists(extractTo))
                    throw new ExecuteException($"Path [{dirName}] is file, cannot extract files");
                else
                    Directory.CreateDirectory(extractTo);
            }

            int i = 0;
            Dictionary<string, string> fileDict = cmd.Addr.Plugin.Sections[dirName].IniDict;
            foreach (string file in fileDict.Keys)
            {
                using (MemoryStream ms = EncodedFile.ExtractFile(targetPlugin, dirName, file))
                using (FileStream fs = new FileStream(Path.Combine(extractTo, file), FileMode.Create, FileAccess.Write))
                {
                    ms.Position = 0;
                    ms.CopyTo(fs);
                    ms.Close();
                    fs.Close();
                }

                s.MainViewModel.BuildCommandProgressBarValue = 200 + ((fileDict.Count * i / fileDict.Count) * 800);
            }

            logs.Add(new LogInfo(LogState.Success, $"Encoded folder [{dirName}] extracted to [{extractTo}]"));

            return logs;
        }

        public static List<LogInfo> Encode(EngineState s, CodeCommand cmd)
        {
            List<LogInfo> logs = new List<LogInfo>();

            Debug.Assert(cmd.Info.GetType() == typeof(CodeInfo_Encode));
            CodeInfo_Encode info = cmd.Info as CodeInfo_Encode;

            string pluginFile = StringEscaper.Unescape(info.PluginFile);
            string dirName = StringEscaper.Preprocess(s, info.DirName);
            string filePath = StringEscaper.Preprocess(s, info.FilePath);

            bool inCurrentPlugin = false;
            if (info.PluginFile.Equals("%PluginFile%", StringComparison.OrdinalIgnoreCase))
                inCurrentPlugin = true;
            else if (info.PluginFile.Equals("%ScriptFile%", StringComparison.OrdinalIgnoreCase))
                inCurrentPlugin = true;


            Plugin targetPlugin;
            if (inCurrentPlugin)
            {
                targetPlugin = s.CurrentPlugin;
            }
            else
            {
                string fullPath = StringEscaper.ExpandVariables(s, pluginFile);
                targetPlugin = s.Project.GetPluginByFullPath(fullPath);
                if (targetPlugin == null)
                    throw new ExecuteException($"No plugin in [{fullPath}]");
            }

            s.MainViewModel.BuildCommandProgressBarValue = 200;

            // Check srcFileName contains wildcard
            if (filePath.IndexOfAny(new char[] { '*', '?' }) == -1)
            { // No Wildcard
                EncodedFile.AttachFile(targetPlugin, dirName, Path.GetFileName(filePath), filePath);
                s.MainViewModel.BuildCommandProgressBarValue = 600;
                logs.Add(new LogInfo(LogState.Success, $"[{filePath}] encoded into [{targetPlugin.FullPath}]", cmd));
            }
            else
            { // With Wildcard
                // Use FileHelper.GetDirNameEx to prevent ArgumentException of Directory.GetFiles
                string srcDirToFind = FileHelper.GetDirNameEx(filePath);
                string[] files = Directory.GetFiles(srcDirToFind, Path.GetFileName(filePath));

                if (0 < files.Length)
                { // One or more file will be copied
                    logs.Add(new LogInfo(LogState.Success, $"[{filePath}] will be encoded into [{targetPlugin.FullPath}]", cmd));
                    for (int i = 0; i < files.Length; i++)
                    {
                        EncodedFile.AttachFile(targetPlugin, dirName, Path.GetFileName(files[i]), files[i]);
                        s.MainViewModel.BuildCommandProgressBarValue = 200 + (800 * (i + 1) / files.Length);
                        logs.Add(new LogInfo(LogState.Success, $"[{files[i]}] encoded ({i + 1}/{files.Length})", cmd));
                    }

                    logs.Add(new LogInfo(LogState.Success, $"[{files.Length}] files copied", cmd));
                }
                else
                { // No file will be copied
                    s.MainViewModel.BuildCommandProgressBarValue = 600;
                    logs.Add(new LogInfo(LogState.Warning, $"Files match wildcard [{filePath}] not found", cmd));
                }
            }

            return logs;
        }
    }
}
