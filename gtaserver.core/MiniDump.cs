﻿using Sentry;
using Sentry.Extensibility;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace GTAServer
{
    class MiniDump : ISentryEventExceptionProcessor
    {
        // https://www.pinvoke.net/default.aspx/dbghelp/MiniDumpWriteDump.html
        [DllImport("Dbghelp.dll")]
        static extern bool MiniDumpWriteDump(
            IntPtr hProcess,
            uint ProcessId,
            IntPtr hFile,
            int DumpType,
            ref MINIDUMP_EXCEPTION_INFORMATION ExceptionParam,
            IntPtr UserStreamParam,
            IntPtr CallbackParam);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentProcessId();

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        public void Process(Exception exception, SentryEvent sentryEvent)
        {
            // create folder "crashes"
            var crashes = Path.Combine(AppContext.BaseDirectory, "Crashes");
            Directory.CreateDirectory(crashes);

            // open filestream to write minidump to crashes folder
            var file = new FileStream(Path.Combine(crashes, $"{Guid.NewGuid()}.dmp"), FileMode.Create);
            var info = new MINIDUMP_EXCEPTION_INFORMATION
            {
                ThreadId = GetCurrentThreadId(),
                ExceptionPointers = Marshal.GetExceptionPointers(),
                ClientPointers = 1
            };

            MiniDumpWriteDump(
                GetCurrentProcess(),
                GetCurrentProcessId(),
                file.SafeFileHandle.DangerousGetHandle(),
                (int) MINIDUMP_TYPE.MiniDumpWithFullMemory,
                ref info,
                IntPtr.Zero,
                IntPtr.Zero);

            file.Close();

            // inform the user a minidump has been written
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("A minidump has been written to " + file.Name + ", please send this file to the developers.");
            Console.ResetColor();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct MINIDUMP_EXCEPTION_INFORMATION
    {
        public uint ThreadId;
        public IntPtr ExceptionPointers;
        public int ClientPointers;
    }

    enum MINIDUMP_TYPE
    {
        MiniDumpNormal = 0,
        MiniDumpWithFullMemory = 2
    }
}
