using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace MiniWorldNode.Services;

/// <summary>
/// 管理 Windows 作业对象，以确保子进程随主进程退出。
/// </summary>
public static class WindowsJobManager
{
    private static ILogger? _logger;
    private static IntPtr _jobHandle;

    #region P/Invoke Definitions

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(IntPtr hJob, JobObjectInfoType jobObjectInfoType, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    private enum JobObjectInfoType
    {
        ExtendedLimitInformation = 9
    }

    [Flags]
    private enum JobObjectLimitFlags : uint
    {
        JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public JobObjectLimitFlags LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public IntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }

    #endregion

    /// <summary>
    /// 初始化作业对象。此方法应在应用程序启动时调用一次。
    /// </summary>
    /// <param name="logger">用于记录日志的记录器。</param>
    public static void Initialize(ILogger logger)
    {
        _logger = logger;

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger.LogInformation("非 Windows 平台，跳过 Job Object 初始化。");
            return;
        }

        _jobHandle = CreateJobObject(IntPtr.Zero, null);
        if (_jobHandle == IntPtr.Zero)
        {
            _logger.LogError("创建 Job Object 失败，错误码: {ErrorCode}", Marshal.GetLastWin32Error());
            return;
        }

        var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                LimitFlags = JobObjectLimitFlags.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
            }
        };

        int length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
        IntPtr extendedInfoPtr = Marshal.AllocHGlobal(length);
        try
        {
            Marshal.StructureToPtr(extendedInfo, extendedInfoPtr, false);
            if (!SetInformationJobObject(_jobHandle, JobObjectInfoType.ExtendedLimitInformation, extendedInfoPtr, (uint)length))
            {
                _logger.LogError("设置 Job Object 信息失败，错误码: {ErrorCode}", Marshal.GetLastWin32Error());
                CloseHandle(_jobHandle);
                _jobHandle = IntPtr.Zero;
                return;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(extendedInfoPtr);
        }

        using var currentProcess = Process.GetCurrentProcess();
        if (!AssignProcessToJobObject(_jobHandle, currentProcess.Handle))
        {
            _logger.LogWarning("将主进程分配到 Job Object 失败，错误码: {ErrorCode}", Marshal.GetLastWin32Error());
        }
        else
        {
            _logger.LogInformation("主进程已成功加入 Job Object。");
        }
    }

    /// <summary>
    /// 将指定的进程添加到作业对象中。
    /// </summary>
    /// <param name="process">要添加的进程。</param>
    /// <returns>如果成功添加则返回 true，否则返回 false。</returns>
    public static bool AddProcess(Process process)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || _jobHandle == IntPtr.Zero || process == null)
        {
            return false;
        }

        if (!AssignProcessToJobObject(_jobHandle, process.Handle))
        {
            _logger.LogWarning("将进程 {ProcessId} 分配到 Job Object 失败，错误码: {ErrorCode}", process.Id, Marshal.GetLastWin32Error());
            return false;
        }

        _logger.LogInformation("进程 {ProcessId} 已成功添加到 Job Object。", process.Id);
        return true;
    }

    /// <summary>
    /// 关闭作业对象的句柄。此方法应在应用程序退出时调用。
    /// </summary>
    public static void Dispose()
    {
        if (_jobHandle != IntPtr.Zero)
        {
            CloseHandle(_jobHandle);
            _jobHandle = IntPtr.Zero;
        }
    }
}