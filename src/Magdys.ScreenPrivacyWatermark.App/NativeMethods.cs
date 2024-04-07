using System.Runtime.InteropServices;

namespace Magdys.ScreenPrivacyWatermark.App;

/// <summary>
/// This class contains methods that are implemented in unmanaged code (native methods).
/// </summary>
internal static partial class NativeMethods
{
    /// <summary>
    /// This enum contains extended window styles used for window creation.
    /// Source: https://docs.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles
    /// </summary>
    //[Flags]
    internal enum ExtendedWindowStyles
    {
        /// <summary>
        /// Retrieves the extended window styles.
        /// </summary>
        GWL_EXSTYLE = -20,

        /// <summary>
        /// Default style.
        /// </summary>
        None = 0,

        /// <summary>
        /// The window is transparent. Mouse events pass through to beneath windows.
        /// </summary>
        WS_EX_TRANSPARENT = 0x20,

        /// <summary>
        /// The window is intended to be used as a tool window. This style results in a smaller caption bar and hides the window from task manager.
        /// </summary>
        WS_EX_TOOLWINDOW = 0x80,

        /// <summary>
        /// The window should stay on top of all others.
        /// </summary>
        WS_EX_TOPMOST = 0x00000008,

        /// <summary>
        /// The window is a layered window. This style cannot be used if the window has a class style of either CS_OWNDC or CS_CLASSDC.
        /// </summary>
        WS_EX_LAYERED = 0x80000,

        /// <summary>
        /// The window has a border with a raised edge.
        /// </summary>
        WS_EX_WINDOWEDGE = 0x00000100,

        /// <summary>
        /// The window does not become the foreground window when the user clicks it. The system does not bring this window to the foreground when the user minimizes or closes the foreground window.
        /// </summary>
        WS_EX_NOACTIVATE = 0x08000000,

        /// <summary>
        /// The window is palette window, which is a modeless dialog box that presents an array of commands.
        /// </summary>
        WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST
    }

    //[Flags]
    internal enum ProcessAccessRights
    {
        // None
        None = 0,

        // Required to suspend or resume a process.
        PROCESS_TERMINATE = 0x0001,

        // Required to create a thread.
        PROCESS_CREATE_THREAD = 0x0002,

        // Required to perform an operation on the address space of a process (see VirtualProtectEx and WriteProcessMemory).
        PROCESS_VM_OPERATION = 0x0008,

        // Required to read memory in a process using ReadProcessMemory.
        PROCESS_VM_READ = 0x0010,

        // Required to write to memory in a process using WriteProcessMemory.
        PROCESS_VM_WRITE = 0x0020,

        // Required to duplicate a handle using DuplicateHandle.
        PROCESS_DUP_HANDLE = 0x0040,

        // Required to create a process.
        PROCESS_CREATE_PROCESS = 0x0080,

        // Required to set memory limits using SetProcessWorkingSetSize.
        PROCESS_SET_QUOTA = 0x0100,

        // Required to set certain information about a process, such as its priority class (see SetPriorityClass).
        PROCESS_SET_INFORMATION = 0x0200,

        // Required to retrieve certain information about a process, such as its token, exit code, and priority class (see OpenProcessToken, GetExitCodeProcess, GetPriorityClass, and IsProcessInJob).
        PROCESS_QUERY_INFORMATION = 0x0400,

        // Required to suspend or resume a process.
        PROCESS_SUSPEND_RESUME = 0x0800,

        // Required to retrieve certain information about a process (see QueryFullProcessImageName). A handle that has the PROCESS_QUERY_INFORMATION access right is automatically granted PROCESS_QUERY_LIMITED_INFORMATION. Windows Server 2003 and Windows XP/2000:  This access right is not supported.
        PROCESS_QUERY_LIMITED_INFORMATION = 0x1000,

        // Required to delete the object.
        DELETE = 0x00010000,

        // Required to read information in the security descriptor for the object, not including the information in the SACL. To read or write the SACL, you must request the ACCESS_SYSTEM_SECURITY access right. For more information, see SACL Access Right.
        READ_CONTROL = 0x00020000,

        // Required to modify the DACL in the security descriptor for the object.
        WRITE_DAC = 0x00040000,

        // Required to change the owner in the security descriptor for the object.
        WRITE_OWNER = 0x00080000,

        STANDARD_RIGHTS_REQUIRED = 0x000f0000,

        // The right to use the object for synchronization. This enables a thread to wait until the object is in the signaled state.
        SYNCHRONIZE = 0x00100000,

        PROCESS_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0xFFF,
    }


    // Import the GetCurrentProcess function from kernel32.dll. This function returns a pseudo handle for the current process.
    [LibraryImport("kernel32.dll")]
    internal static partial IntPtr GetCurrentProcess();

    // Import the GetKernelObjectSecurity function from advapi32.dll. This function retrieves a copy of the security descriptor for a kernel object.
    // SetLastError = true allows the .NET runtime to preserve the Win32 error code so it can be accessed if the function call fails.
    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)] // Specify that the bool return type should be marshalled as a Win32 BOOL.
    internal static partial bool GetKernelObjectSecurity(IntPtr Handle, int securityInformation, [Out] byte[] pSecurityDescriptor, uint nLength, out uint lpnLengthNeeded);

    // Import the SetKernelObjectSecurity function from advapi32.dll. This function sets the security descriptor for a kernel object.
    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)] // Specify that the bool return type should be marshalled as a Win32 BOOL.
    internal static partial bool SetKernelObjectSecurity(IntPtr Handle, int securityInformation, [In] byte[] pSecurityDescriptor);

    [LibraryImport("ntdll.dll", SetLastError = true)]
    internal static partial void RtlSetProcessIsCritical(uint v1, uint v2, uint v3);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsIconic(IntPtr hWnd);
}
