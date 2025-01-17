﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp;
using RoboSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboSharp.UnitTests
{
    [TestClass()]
    public class RoboCommandParserTests
    {
        public static void DebuggerWriteLine(object sender, Debugger.DebugMessageArgs args)
        {
            Console.WriteLine("--- " + args.Message);
        }

        const string CmdEndText = @" /R:0 /W:30 /BYTES";

        private static RoboCommand GetNewCommand(string source = default, string destination =  default) => new RoboCommand(
            source: source.IsNullOrWhiteSpace() ?  "C:\\Source" : source,
            destination: destination.IsNullOrWhiteSpace() ? "D:\\Destination" : destination
            );

        private static string PrintCmd(IRoboCommand command ) => command.CopyOptions.Source.IsNullOrWhiteSpace() ?  $"\"\" \"\" {command}" : command.ToString();

        /// <summary>
        /// Use this one when debugging specific commands that are not deciphering for you!
        /// </summary>
        [DataRow("\"\" \"\" \"*.txt\" \"*.pdf\"", DisplayName = "No Source or Dest")]
        [DataRow("robocopy.exe \"C:\\MySource\" \"D:\\My Destination\" \"*.txt\" \"*.pdf\"", DisplayName = "quoted filters")]
        [DataRow("\"D:\\Some Folder\\robocopy.exe\" \"C:\\MySource\" \"D:\\My Destination\" *.txt *.pdf", DisplayName = "multiple unquoted filters")]
        [DataRow("c:\\windows\\system32\\robocopy.exe \"C:\\MySource\" \"D:\\My Destination\" *.txt", DisplayName = ".txt Only")]
        [DataRow("robocopy \"C:\\MySource\" \"D:\\My Destination\" /MOVE", DisplayName = "Example")]
        [TestMethod()]
        public void TestCustomParameters(string command)
        {
            Debugger.Instance.DebugMessageEvent += DebuggerWriteLine;
            IRoboCommand cmd = RoboCommandParser.Parse(command);
            Debugger.Instance.DebugMessageEvent -= DebuggerWriteLine;
            Console.WriteLine($"\n\n Input : {command}");
            Console.WriteLine($"Output : {cmd}");
            //Assert.AreEqual(command, command.ToString(), true);
        }

        [DataRow("C:\\source", "D:\\destination", DisplayName = "No Quotes")]
        [DataRow("C:\\source", "\"D:\\destination\"", DisplayName = "Destination Quotes")]
        [DataRow("\"C:\\source\"", "D:\\destination", DisplayName = "Source Quoted")]
        [DataRow("\"C:\\source\"", "\"D:\\destination\"", DisplayName = "Both Quoted")]
        [DataRow("\"C:\\source dir\"", "\"D:\\destination dir\"", DisplayName = "Both Quoted and Spaced")]
        [TestMethod()]
        public void TestSourceAndDestination(string source, string dest)
        {
            string command = $"robocopy {source} {dest} /copyall";
            IRoboCommand cmd = RoboCommandParser.Parse(command);
            Console.WriteLine(" Input : " + command);
            Console.WriteLine("Output : " + cmd);
            Assert.AreEqual(source.Trim('\"'), cmd.CopyOptions.Source, "\n\nSource is not expected value");
            Assert.AreEqual(dest.Trim('\"'), cmd.CopyOptions.Destination, "\n\nDestination is not expected value");
            Assert.IsTrue(cmd.CopyOptions.CopyAll, "\nCopyAll was Removed!");
        }

        [DataRow( (SelectionFlags)4097,  (CopyActionFlags)255, (LoggingFlags)65535, "C:\\SomeSourcePath\\My Source Folder", "D:\\SomeDestination\\My Dest Folder", DisplayName = "Test_All_Flags")]
        [DataRow(SelectionFlags.Default,  CopyActionFlags.Default, LoggingFlags.None, "C:\\SomeSourcePath\\", "D:\\SomeDestination\\",  DisplayName = "Test_Defaults")]
        [TestMethod]
        public void TestOptionFlags(SelectionFlags selectionFlags, CopyActionFlags copyFlags, LoggingFlags loggingFlags, string source, string destination)
        {
            // Transform the selection flags to a robocommand, generate the command, parse it, then test that both have the same flags. 
            // ( What the library generates should be able to be reparsed back into the library )
            RoboCommand cmd = new RoboCommand(source, destination, copyFlags, selectionFlags, loggingFlags);
            string text = PrintCmd(cmd);
            IRoboCommand cmdResult = RoboCommandParser.Parse(text);
            Assert.AreEqual(cmd.CopyOptions.Source, cmdResult.CopyOptions.Source, "\nCopyOptions.Source is not equal!");
            Assert.AreEqual(cmd.CopyOptions.Destination, cmdResult.CopyOptions.Destination, "\nCopyOptions.Destination is not equal!");
            Assert.AreEqual(cmd.CopyOptions.GetCopyActionFlags(), cmdResult.CopyOptions.GetCopyActionFlags(), $"\n\nCopy Flags are not the same!\n\nExpected:{cmd.CopyOptions.GetCopyActionFlags()}\nResult:{cmdResult.CopyOptions.GetCopyActionFlags()}");
            Assert.AreEqual(cmd.SelectionOptions.GetSelectionFlags(), cmdResult.SelectionOptions.GetSelectionFlags(), $"\n\nSelection Flags are not the same!\n\nExpected:{cmd.SelectionOptions.GetSelectionFlags()}\nResult:{cmdResult.SelectionOptions.GetSelectionFlags()}");
            Assert.AreEqual(cmd.LoggingOptions.GetLoggingActionFlags(), cmdResult.LoggingOptions.GetLoggingActionFlags(), $"\n\nLogging Flags are not the same!\n\nExpected:{cmd.LoggingOptions.GetLoggingActionFlags()}\nResult:{cmdResult.LoggingOptions.GetLoggingActionFlags()}");

            // Final test : both should produce the same ToString()
            Assert.AreEqual(cmd.ToString(), cmdResult.ToString(), $"\n\nProduced Command is not equal!\nExpected:\t{cmd}\n  Result:\t{cmdResult}");
        }

        [TestMethod]
        public void TestFileSize()
        {
            // Transform the selection flags to a robocommand, generate the command, parse it, then test that both have the same flags. 
            // ( What the library generates should be able to be reparsed back into the library )
            IRoboCommand cmd = GetNewCommand();
            cmd.SelectionOptions.MinFileSize = 1234567890;
            cmd.SelectionOptions.MaxFileSize= 0987654321;
            string text = PrintCmd(cmd);

            Debugger.Instance.DebugMessageEvent += DebuggerWriteLine;
            IRoboCommand cmdResult = RoboCommandParser.Parse(text);
            Debugger.Instance.DebugMessageEvent -= DebuggerWriteLine;
            
            Assert.AreEqual(cmd.SelectionOptions.MinFileSize, cmdResult.SelectionOptions.MinFileSize, "\n\nMinFileSize does not match!");
            Assert.AreEqual(cmd.SelectionOptions.MaxFileSize, cmdResult.SelectionOptions.MaxFileSize, "\n\nMaxFileSize does not match!");
            Assert.AreEqual(cmd.ToString(), cmdResult.ToString(), $"\n\nProduced Command is not equal!\nExpected:\t{cmd}\n  Result:\t{cmdResult}"); // Final test : both should produce the same ToString()
        }

        [DataRow("19941012", "20220910")]
        [DataRow("5", "25")]
        [TestMethod]
        public void TestFileAge(string min, string max)
        {
            // Transform the selection flags to a robocommand, generate the command, parse it, then test that both have the same flags. 
            // ( What the library generates should be able to be reparsed back into the library )
            IRoboCommand cmd = GetNewCommand();
            cmd.SelectionOptions.MinFileAge= min;
            cmd.SelectionOptions.MaxFileAge = max;
            string text = PrintCmd(cmd);

            Debugger.Instance.DebugMessageEvent += DebuggerWriteLine;
            IRoboCommand cmdResult = RoboCommandParser.Parse(text);
            Debugger.Instance.DebugMessageEvent -= DebuggerWriteLine;

            Assert.AreEqual(cmd.SelectionOptions.MinFileAge, cmdResult.SelectionOptions.MinFileAge, "\n\nMinFileAge does not match!");
            Assert.AreEqual(cmd.SelectionOptions.MaxFileAge, cmdResult.SelectionOptions.MaxFileAge, "\n\nMaxFileAge does not match!");
            Assert.AreEqual(cmd.ToString(), cmdResult.ToString(), $"\n\nProduced Command is not equal!\nExpected:\t{cmd}\n  Result:\t{cmdResult}"); // Final test : both should produce the same ToString()
        }

        [DataRow("19941012", "20220910")]
        [DataRow("5", "20201219")]
        [TestMethod]
        public void TestFileLastAccessDate(string min, string max)
        {
            // Transform the selection flags to a robocommand, generate the command, parse it, then test that both have the same flags. 
            // ( What the library generates should be able to be reparsed back into the library )
            IRoboCommand cmd = GetNewCommand();
            cmd.SelectionOptions.MinLastAccessDate = min;
            cmd.SelectionOptions.MaxLastAccessDate = max;
            string text = PrintCmd(cmd);

            Debugger.Instance.DebugMessageEvent += DebuggerWriteLine;
            IRoboCommand cmdResult = RoboCommandParser.Parse(text);
            Debugger.Instance.DebugMessageEvent -= DebuggerWriteLine;

            Assert.AreEqual(cmd.SelectionOptions.MinLastAccessDate, cmdResult.SelectionOptions.MinLastAccessDate, "\n\nMinLastAccessDate does not match!");
            Assert.AreEqual(cmd.SelectionOptions.MaxLastAccessDate, cmdResult.SelectionOptions.MaxLastAccessDate, "\n\nMaxLastAccessDate does not match!");
            Assert.AreEqual(cmd.ToString(), cmdResult.ToString(), $"\n\nProduced Command is not equal!\nExpected:\t{cmd}\n  Result:\t{cmdResult}"); // Final test : both should produce the same ToString()
        }

        [DataRow("\"C:\\Some Folder\\MyLogFile.txt\"")]
        [DataRow("C:\\MyLogFile.txt")]
        [TestMethod]
        public void TestLogging(string path)
        {
            // Transform the selection flags to a robocommand, generate the command, parse it, then test that both have the same flags. 
            // ( What the library generates should be able to be reparsed back into the library )
            IRoboCommand cmd = GetNewCommand();
            cmd.LoggingOptions.LogPath = path;
            cmd.LoggingOptions.AppendLogPath = path;
            cmd.LoggingOptions.AppendUnicodeLogPath = path;
            cmd.LoggingOptions.UnicodeLogPath = path;
            IRoboCommand cmdResult = RoboCommandParser.Parse(PrintCmd(cmd));

            // the source paths are trimmed here because they are functionally identical, but the wrapping is removed during the parsing and sanitization process during path qualification. End result command should be the same though.
            string trimmedPath = path.Trim('\"');
            Assert.AreEqual(trimmedPath, cmdResult.LoggingOptions.LogPath, "\n\nLogPath does not match!");
            Assert.AreEqual(trimmedPath, cmdResult.LoggingOptions.UnicodeLogPath, "\n\nUnicodeLogPath does not match!");
            Assert.AreEqual(trimmedPath, cmdResult.LoggingOptions.AppendLogPath, "\n\nAppendLogPath does not match!");
            Assert.AreEqual(trimmedPath, cmdResult.LoggingOptions.AppendUnicodeLogPath, "\n\nAppendUnicodeLogPath does not match!");
            Assert.AreEqual(cmd.ToString(), cmdResult.ToString(), $"\n\nProduced Command is not equal!\nExpected:\t{cmd}\n  Result:\t{cmdResult}"); // Final test : both should produce the same ToString()
        }

        [DataRow("ExcludedTestFile1.txt", "ExcludedFile2.pdf", "\"*wild card*\"", DisplayName = "Multiple Filters")]
        [DataRow("\"C:\\Some Folder\\Excluded.txt\"", DisplayName = "Quoted Filter")]
        [DataRow("C:\\Excluded.txt", DisplayName = "UnQuoted Filters")]
        [DataRow(DisplayName = "No Filter Specified")]
        [TestMethod]
        public void TestExcludedFiles(params string[] filters)
        {
            IRoboCommand cmd = GetNewCommand();
            cmd.SelectionOptions.ExcludedFiles.AddRange(filters);
            IRoboCommand cmdResult = RoboCommandParser.Parse(PrintCmd(cmd));
            
            Assert.AreEqual(cmd.ToString(), cmdResult.ToString(), $"\n\nProduced Command is not equal!\nExpected:\t{cmd}\n  Result:\t{cmdResult}"); // Final test : both should produce the same ToString()
            Console.WriteLine($"\n\n Input : {cmd}");
            Console.WriteLine($"Output : {cmdResult}");
        }

        [DataRow(@"robocopy """" """"  /XF c:\MyFile.txt /COPYALL /XF ""d:\File 2.pdf""", @"""*.*"" /COPYALL /XF c:\MyFile.txt ""d:\File 2.pdf""", DisplayName = "Multiple /XF flags with Quotes")]
        [DataRow(@"robocopy """" """"  /XF c:\MyFile.txt /COPYALL /XF d:\File2.pdf", @"""*.*"" /COPYALL /XF c:\MyFile.txt d:\File2.pdf", DisplayName = "Multiple /XF flags")]
        [DataRow(@"robocopy """" """"  /XF c:\MyFile.txt d:\File2.pdf", @"""*.*"" /XF c:\MyFile.txt d:\File2.pdf", DisplayName = "Single XF Flag with multiple Filters")]
        [TestMethod]
        public void TestExcludedFilesRaw(string input, string expected)
        {
            IRoboCommand cmdResult = RoboCommandParser.Parse(input);
            
            expected += CmdEndText;
            
            Console.WriteLine($"\n\n    Input : {input}");
            Console.WriteLine($" Expected : {expected}");
            Console.WriteLine($"   Output : {cmdResult}");

            Assert.AreEqual(expected.Trim(), cmdResult.ToString().Trim(), "Command not expected result."); // Final test : both should produce the same ToString()
            
        }

        [DataRow("D:\\Excluded Dir\\", DisplayName = "Single Exclusion - Spaced")]
        [DataRow("D:\\Excluded\\Dir\\", DisplayName = "Single Exclusion - No Spaces")]
        [DataRow("C:\\Windows\\System32", "D:\\Excluded\\Dir\\", DisplayName = " Multiple Exclusions - No Spaces")]
        [DataRow("C:\\Windows\\System32", "D:\\Excluded Dir\\", DisplayName = " Multiple Exclusions - Spaced")]
        [DataRow(DisplayName = "No Filter Specified")]
        [TestMethod]
        public void TestExcludedDirectories(params string[] filters)
        {
            // Note : Handle instances of /XD multiple times https://superuser.com/questions/482112/using-robocopy-and-excluding-multiple-directories
            IRoboCommand cmd = GetNewCommand();
            cmd.SelectionOptions.ExcludedDirectories.AddRange(filters);
            IRoboCommand cmdResult = RoboCommandParser.Parse(PrintCmd(cmd));
            
            Console.WriteLine($"\n\n Input : {PrintCmd(cmd)}");
            Console.WriteLine($"Expected : {cmd}");
            Console.WriteLine($"Output : {cmdResult}");

            Assert.AreEqual(cmd.ToString(), cmdResult.ToString(), $"\n\nProduced Command is not equal!\nExpected:\t{cmd}\n  Result:\t{cmdResult}"); // Final test : both should produce the same ToString()
            
        }

        [DataRow("*.pdf")]
        [DataRow("*.pdf", "*.txt", "*.jpg")]
        [DataRow("*.*")]
        [DataRow(DisplayName = "No Filter Specified")]
        [TestMethod]
        public void TestFileFilter(params string[] filters)
        {
            IRoboCommand cmd = GetNewCommand();
            cmd.CopyOptions.AddFileFilter(filters);
            IRoboCommand cmdResult = RoboCommandParser.Parse(PrintCmd(cmd));

            Assert.AreEqual(cmd.ToString(), cmdResult.ToString(), $"\n\nProduced Command is not equal!\nExpected:\t{cmd}\n  Result:\t{cmdResult}"); // Final test : both should produce the same ToString()
            Console.WriteLine($"\n\n Input : {cmd}");
            Console.WriteLine($"Output : {cmdResult}");
        }

        
        [DataRow("C:\\MySource \"D:\\My Destination\" \"*.txt\"")]
        [DataRow("C:\\MySource \"D:\\My Destination\" \"*.txt\" /MOVE")]
        [TestMethod]
        public void TestFileFilterRaw(string input)
        {
            // Note : Due to how RoboCommand prints out file filters, ensure input file filters are always quoted
            IRoboCommand cmdResult = RoboCommandParser.Parse(input);
            cmdResult.LoggingOptions.PrintSizesAsBytes = false;
            string expected = input += " /R:0 /W:30"; // robocommand ALWAYS prints these values

            Assert.AreEqual(expected.Trim(), cmdResult.ToString().Trim(), $"\n\nProduced Command is not equal!\nExpected:\t{expected}\n  Result:\t{cmdResult}"); // Final test : both should produce the same ToString()
            Console.WriteLine($"\n\n Input : {input}");
            Console.WriteLine($"Output : {cmdResult}");
        }

        [DataRow("*.pdf /purge /xf \"C:\\my file\"", false)]
        [DataRow("robocopy C:\\Source D:\\Dest *.pdf", true)]
        [DataRow("C:\\Source D:\\Dest *.pdf", true)]
        [DataRow("C:\\Source \"\" *.pdf", true)]
        [DataRow("C:\\Source *.pdf", true)]
        [DataRow("robocopy *.pdf", true)]
        [TestMethod]
        public void TestParseOptions(string input, bool shouldThrow)
        {
            void runTest()
            {
                Console.WriteLine(string.Format("Test String : {0}", input));
                try
                {
                    _ = RoboCommandParser.ParseOptions(input);
                }catch(Exception ex)
                {
                    Console.WriteLine(string.Format("Exception Message ({0}expected) :", shouldThrow ? "" : "NOT "));
                    Console.Write(ex.Message);
                    throw;
                }
            }
            if (shouldThrow)
            {
                Assert.ThrowsException<ArgumentException>(runTest);
            }
            else
            {
                runTest();
            }
        }
    }
}