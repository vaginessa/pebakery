﻿/*
    Copyright (C) 2017-2018 Hajin Jang
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

    Additional permission under GNU GPL version 3 section 7

    If you modify this program, or any covered work, by linking
    or combining it with external libraries, containing parts
    covered by the terms of various license, the licensors of
    this program grant you additional permission to convey the
    resulting work. An external library is a library which is
    not derived from or based on this program. 
*/

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PEBakery.Core;
using PEBakery.WPF;
using System.Linq;

namespace PEBakery.Tests.Core
{
    public enum ErrorCheck
    {
        Success = 0,
        Warning = 10,
        Overwrite = 11,
        Error = 20,
        ParserError = 21,
    }

    [TestClass]
    public class EngineTests
    {
        #region Static Fields
        public static Project Project;
        public static Logger Logger;
        public static string BaseDir;
        #endregion

        #region CreateEngineState, DummySectionAddress
        public static EngineState CreateEngineState(bool doCopy = true, Script sc = null)
        {
            // Clone is needed for parallel test execution (Partial Deep Clone)
            EngineState s;
            if (doCopy)
            {
                Project project = EngineTests.Project.PartialDeepCopy();
                Logger logger = EngineTests.Logger;
                MainViewModel model = new MainViewModel();
                if (sc == null)
                    s = new EngineState(project, logger, model, EngineMode.RunAll);
                else
                    s = new EngineState(project, logger, model, EngineMode.RunOne, sc);
            }
            else
            {
                Project.Variables.ResetVariables(VarsType.Local);
                MainViewModel model = new MainViewModel();
                if (sc == null)
                    s = new EngineState(Project, Logger, model, EngineMode.RunAll);
                else
                    s = new EngineState(Project, Logger, model, EngineMode.RunOne, sc);
            }

            s.LogMode = LogMode.NoDelay;
            s.TestMode = true;

            return s;
        }

        public static SectionAddress DummySectionAddress()
        {
            return new SectionAddress(Project.MainScript, Project.MainScript.Sections["Process"]);
        }
        #endregion

        #region Eval
        public static List<LogInfo> Eval(EngineState s, string rawCode, CodeType type, ErrorCheck check)
        {
            SectionAddress addr = DummySectionAddress();
            return Eval(s, addr, rawCode, type, check, out _);
        }

        public static List<LogInfo> Eval(EngineState s, string rawCode, CodeType type, ErrorCheck check, out CodeCommand cmd)
        {
            SectionAddress addr = DummySectionAddress();
            return Eval(s, addr, rawCode, type, check, out cmd);
        }

        public static List<LogInfo> Eval(EngineState s, SectionAddress addr, string rawCode, CodeType type, ErrorCheck check)
        {
            return Eval(s, addr, rawCode, type, check, out _);
        }

        public static List<LogInfo> Eval(EngineState s, SectionAddress addr, string rawCode, CodeType type, ErrorCheck check, out CodeCommand cmd)
        {
            // Create CodeCommand
            cmd = CodeParser.ParseStatement(rawCode, addr);
            if (cmd.Type == CodeType.Error)
            {
                CodeInfo_Error info = cmd.Info.Cast<CodeInfo_Error>();
                Console.WriteLine(info.ErrorMessage);

                Assert.AreEqual(ErrorCheck.ParserError, check);
                return new List<LogInfo>();
            }
            Assert.AreEqual(type, cmd.Type);

            // Run CodeCommand
            List<LogInfo> logs = Engine.ExecuteCommand(s, cmd);

            // Assert
            CheckErrorLogs(logs, check);

            // Return logs
            return logs;
        }
        #endregion

        #region EvalLines
        public static List<LogInfo> EvalLines(EngineState s, List<string> rawCodes, ErrorCheck check)
        {
            return EvalLines(s, rawCodes, check, out _);
        }

        public static List<LogInfo> EvalLines(EngineState s, List<string> rawCodes, ErrorCheck check, out List<CodeCommand> cmds)
        {
            // Create CodeCommand
            SectionAddress addr = DummySectionAddress();
            cmds = CodeParser.ParseStatements(rawCodes, addr, out List<LogInfo> errorLogs);
            if (errorLogs.Any(x => x.State == LogState.Error))
            {
                Assert.AreEqual(ErrorCheck.ParserError, check);
                return new List<LogInfo>();
            }
            
            // Reset halt flags
            s.ResetHaltFlags();

            // Run CodeCommands
            return Engine.RunCommands(s, addr, cmds, s.CurSectionParams, s.CurDepth);
        }
        #endregion

        #region EvalOptLines
        /// <summary>
        /// Eval for multiple lines of code
        /// </summary>
        /// <param name="s"></param>
        /// <param name="opType">Use null to check if rawCodes is not opitimzed</param>
        /// <param name="rawCodes"></param>
        /// <param name="check"></param>
        /// <returns></returns>
        public static List<LogInfo> EvalOptLines(EngineState s, CodeType? opType, List<string> rawCodes, ErrorCheck check)
        {
            SectionAddress addr = DummySectionAddress();
            return EvalOptLines(s, addr, opType, rawCodes, check, out _);
        }

        /// <summary>
        /// Eval for multiple lines of code
        /// </summary>
        /// <param name="s"></param>
        /// <param name="opType">Use null to check if rawCodes is not opitimzed</param>
        /// <param name="rawCodes"></param>
        /// <param name="check"></param>
        /// <returns></returns>
        public static List<LogInfo> EvalOptLines(EngineState s, CodeType? opType, List<string> rawCodes, ErrorCheck check, out List<CodeCommand> cmds)
        {
            SectionAddress addr = DummySectionAddress();
            return EvalOptLines(s, addr, opType, rawCodes, check, out cmds);
        }

        /// <summary>
        /// Eval for multiple lines of code
        /// </summary>
        /// <param name="s"></param>
        /// <param name="addr"></param>
        /// <param name="opType">Use null to check if rawCodes is not opitimzed</param>
        /// <param name="rawCodes"></param>
        /// <param name="check"></param>
        /// <returns></returns>
        public static List<LogInfo> EvalOptLines(EngineState s, SectionAddress addr, CodeType? opType, List<string> rawCodes, ErrorCheck check)
        {
            return EvalOptLines(s, addr, opType, rawCodes, check, out _);
        }

        /// <summary>
        /// Eval for multiple lines of code
        /// </summary>
        /// <param name="s"></param>
        /// <param name="addr"></param>
        /// <param name="opType">Use null to check if rawCodes is not opitimzed</param>
        /// <param name="rawCodes"></param>
        /// <param name="check"></param>
        /// <returns></returns>
        public static List<LogInfo> EvalOptLines(EngineState s, SectionAddress addr, CodeType? opType, List<string> rawCodes, ErrorCheck check, out List<CodeCommand> cmds)
        {
            // Parse CodeCommand
            cmds = CodeParser.ParseStatements(rawCodes, addr, out List<LogInfo> errorLogs);

            if (errorLogs.Any(x => x.State == LogState.Error))
            {
                Assert.AreEqual(ErrorCheck.ParserError, check);
                return new List<LogInfo>();
            }

            if (opType is CodeType type)
            {
                Assert.AreEqual(1, cmds.Count);
                Assert.AreEqual(type, cmds[0].Type);
            }
            else
            {
                Assert.IsTrue(1 < cmds.Count);
            }

            // Reset halt flags
            s.ResetHaltFlags();

            // Run CodeCommands
            return Engine.RunCommands(s, addr, cmds, s.CurSectionParams, s.CurDepth);
        }
        #endregion

        #region CheckErrorLogs
        public static void CheckErrorLogs(List<LogInfo> logs, ErrorCheck check)
        {
            switch (check)
            {
                case ErrorCheck.Success:
                    foreach (LogInfo log in logs)
                    {
                        Assert.IsTrue(log.State != LogState.Error);
                        Assert.IsTrue(log.State != LogState.CriticalError);
                        Assert.IsTrue(log.State != LogState.Warning);
                    }
                    break;
                case ErrorCheck.Warning:
                    {
                        bool result = false;
                        foreach (LogInfo log in logs)
                        {
                            Assert.IsTrue(log.State != LogState.Error);
                            Assert.IsTrue(log.State != LogState.CriticalError);
                            if (log.State == LogState.Warning)
                                result = true;
                        }
                        Assert.IsTrue(result);
                    }
                    break;
                case ErrorCheck.Overwrite:
                    {
                        bool result = false;
                        foreach (LogInfo log in logs)
                        {
                            Assert.IsTrue(log.State != LogState.Error);
                            Assert.IsTrue(log.State != LogState.CriticalError);
                            if (log.State == LogState.Overwrite)
                                result = true;
                        }
                        Assert.IsTrue(result);
                    }
                    break;
                case ErrorCheck.Error:
                    {
                        bool result = false;
                        foreach (LogInfo log in logs)
                        {
                            Assert.IsTrue(log.State != LogState.CriticalError);
                            if (log.State == LogState.Error)
                                result = true;
                        }
                        Assert.IsTrue(result);
                    }
                    break;
                default:
                    Assert.Fail();
                    break;
            }
        }
        #endregion
    }
}
