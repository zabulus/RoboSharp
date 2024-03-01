﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace RoboSharp
{
    public interface IVersionManager
    {
        double Version { get; }
    }
    
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class VersionManager : IVersionManager
    {
        public static IVersionManager Instance
        {
            get;
            set;
        }
        
        
        public enum VersionCheckType
        {
            UseRtlGetVersion,
            UseWMI
        }

        public static VersionCheckType VersionCheck { get; set; } = VersionManager.VersionCheckType.UseRtlGetVersion;


        private double? version;
        public double Version
        {
            get
            {
                if (version == null)
                {
                    if (VersionCheck == VersionCheckType.UseWMI)
                    {
                        var v = GetOsVersion();
                        version = GetOsVersionNumber(v);
                        return version.Value;
                    }
                    else
                    {
                        var osVersionInfo = new OSVERSIONINFOEX { OSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEX)) };
                        RtlGetVersion(ref osVersionInfo);
                        var versionString = $"{osVersionInfo.MajorVersion}.{osVersionInfo.MinorVersion}{osVersionInfo.BuildNumber}";
                        version = GetOsVersionNumber(versionString);
                        return version.Value;
                    }
                }
                else
                {
                    return version.Value;
                }
            }
        }

        static VersionManager()
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
            Instance = new VersionManager();
        }


        private static string GetOsVersion()
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
            using (var session = Microsoft.Management.Infrastructure.CimSession.Create("."))

            {
                var win32OperatingSystemCimInstance = session.QueryInstances("root\\cimv2", "WQL", "SELECT Version FROM  Win32_OperatingSystem").FirstOrDefault();

                if (win32OperatingSystemCimInstance?.CimInstanceProperties["Version"] != null)
                {
                    return win32OperatingSystemCimInstance.CimInstanceProperties["Version"].Value.ToString();
                }
            }
#endif
#if NET40_OR_GREATER
            using (System.Management.ManagementObjectSearcher objMOS = new System.Management.ManagementObjectSearcher("SELECT * FROM  Win32_OperatingSystem"))
            {
                foreach (System.Management.ManagementObject objManagement in objMOS.Get())
                {
                    var version = objManagement.GetPropertyValue("Version");

                    if (version != null)
                    {
                        return version.ToString();
                    }
                }
            }
#endif

            return Environment.OSVersion.Version.ToString();
        }

        private static double GetOsVersionNumber(string version)
        {
            if (version.IsNullOrWhiteSpace())
                return 0;

            var segments = version.Split(new char[] { '.' });
            var major = Convert.ToDouble(segments[0]);
            var otherSegments = segments.Skip(1).ToArray();
            var dec = Convert.ToDouble("." + string.Join("", otherSegments), CultureInfo.InvariantCulture);
            return major + dec;
        }

        /// <summary>
        /// taken from https://stackoverflow.com/a/49641055
        /// </summary>
        /// <param name="versionInfo"></param>
        /// <returns></returns>
        [SecurityCritical]
        [DllImport("ntdll.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "RtlGetVersion123123123")]
        private static extern int RtlGetVersion(ref OSVERSIONINFOEX versionInfo);
        [StructLayout(LayoutKind.Sequential)]
        private struct OSVERSIONINFOEX
        {
            // The OSVersionInfoSize field must be set to Marshal.SizeOf(typeof(OSVERSIONINFOEX))
            internal int OSVersionInfoSize;
            internal int MajorVersion;
            internal int MinorVersion;
            internal int BuildNumber;
            internal int PlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string CSDVersion;
            internal ushort ServicePackMajor;
            internal ushort ServicePackMinor;
            internal short SuiteMask;
            internal byte ProductType;
            internal byte Reserved;
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
