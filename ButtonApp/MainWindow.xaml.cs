using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using ButtonApp.lib;
using System.Runtime.InteropServices;
using ButtonApp.lib.InputStruct;

namespace ButtonApp
{
    public partial class MainWindow : Window
    {

        private IntPtr thisHandle;
        private uint threadID;
        private Process thisProcess;
        private Process targetProcess;
        private uint targetProcessID;
        bool controlChecked = false;
        bool shiftChecked = false;
        bool altChecked = false;
        VirtualKeyShort currentKey;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            thisHandle = new WindowInteropHelper(this).Handle;
            threadID = NativeMethods.GetCurrentThreadId();
            thisProcess = Process.GetCurrentProcess();

            // Set window to always be on top of all other windows
            NativeMethods.SetWindowPos(thisHandle, new IntPtr(-1), 0, 0, 0, 0, NativeMethods.SetWindowPosFlags.SWP_ASYNCWINDOWPOS | NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE | NativeMethods.SetWindowPosFlags.SWP_NOMOVE | NativeMethods.SetWindowPosFlags.SWP_NOSIZE);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Get Next Window in Z-Order
            IntPtr nextWindow = NativeMethods.GetWindow(thisHandle, 2);
            
            NativeMethods.GetWindowThreadProcessId(nextWindow, out targetProcessID);
            targetProcess = Process.GetProcessById((int)targetProcessID);

            bool isVisible = false;
            isVisible = NativeMethods.IsWindowVisible(nextWindow);

            string windowTitle = targetProcess.MainWindowTitle;

            // Look for windows that have a different process than this app, are visible, and have a name.
            while (targetProcess.Id == thisProcess.Id || !isVisible || string.IsNullOrEmpty(windowTitle)) 
            {        
                // Get next window in heirarchy
                nextWindow = NativeMethods.GetWindow(nextWindow, 2);

                // Check if its a graphical window
                isVisible = NativeMethods.IsWindowVisible(nextWindow);

                // We only want to interact with visible windows
                if (isVisible)
                {
                    NativeMethods.GetWindowThreadProcessId(nextWindow, out targetProcessID);
                    targetProcess = Process.GetProcessById((int)targetProcessID);

                    windowTitle = targetProcess.MainWindowTitle;
                 }
            }
            
            if (nextWindow != IntPtr.Zero)
            {
                ProcessThreadCollection nextWindowThreads = targetProcess.Threads;
                uint nextWindowThreadID = (uint)nextWindowThreads[0].Id;
                
                // Attach threads together
                NativeMethods.AttachThreadInput(nextWindowThreadID, threadID, true);

                IntPtr oldHandle = NativeMethods.SetActiveWindow(targetProcess.MainWindowHandle);

                INPUT structInput = new INPUT();
                
                structInput.type = 1;
                uint inputArraySize = 1;
                int inputStructSize = Marshal.SizeOf(structInput);
                
                structInput.U.ki.wScan = ScanCodeShort.NONCONVERT;
                structInput.U.ki.time = 0;
                structInput.U.ki.dwFlags = 0;

                // key down CTRL
                if (controlChecked) {
                    structInput.U.ki.wVk = VirtualKeyShort.CONTROL;
                    NativeMethods.SendInput(inputArraySize, ref structInput, inputStructSize);
                }
                
                // key down SHIFT
                if (shiftChecked) {
                    structInput.U.ki.wScan = ScanCodeShort.MODECHANGE;
                    structInput.U.ki.wVk = VirtualKeyShort.SHIFT;
                    NativeMethods.SendInput(inputArraySize, ref structInput, inputStructSize);
                }

                // key down ALT
                if (altChecked) {
                    structInput.U.ki.wScan = ScanCodeShort.MODECHANGE;
                    structInput.U.ki.wVk = VirtualKeyShort.MENU;
                    NativeMethods.SendInput(inputArraySize, ref structInput, inputStructSize);
                }
         
                // key down
                structInput.U.ki.wScan = ScanCodeShort.MODECHANGE;
                structInput.U.ki.wVk = currentKey;
                NativeMethods.SendInput(inputArraySize, ref structInput, inputStructSize);
            
                // key up 
                structInput.U.ki.dwFlags = KEYEVENTF.KEYUP;
                NativeMethods.SendInput(inputArraySize, ref structInput, inputStructSize);
            
                // key up SHIFT
                if (shiftChecked) {
                    structInput.U.ki.wVk = VirtualKeyShort.SHIFT;
                    NativeMethods.SendInput(inputArraySize, ref structInput, inputStructSize);
                }

                // key up ALT
                if (altChecked)
                {
                    structInput.U.ki.wVk = VirtualKeyShort.MENU;
                    NativeMethods.SendInput(inputArraySize, ref structInput, inputStructSize);
                }

                // key up CONTROL
                if (controlChecked)
                {
                    structInput.U.ki.wVk = VirtualKeyShort.CONTROL;
                    NativeMethods.SendInput(inputArraySize, ref structInput, inputStructSize);
                }
                
                // Unattach threads
                NativeMethods.AttachThreadInput(nextWindowThreadID, threadID, false);
            }
        }

        private void Control_Checked(object sender, RoutedEventArgs e)
        {
            controlChecked = true;
        }

        private void Shift_Checked(object sender, RoutedEventArgs e)
        {
            shiftChecked = true;
        }

        private void Alt_Checked(object sender, RoutedEventArgs e)
        {
            altChecked = true;
        }

        private void Control_Unchecked(object sender, RoutedEventArgs e)
        {
            controlChecked = false;
        }

        private void Shift_Unchecked(object sender, RoutedEventArgs e)
        {
            shiftChecked = false;
        }

        private void Alt_Unchecked(object sender, RoutedEventArgs e)
        {
            altChecked = false;
        }

        private void Text_Changed(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            string text = textBox.Text;
            if (!string.IsNullOrEmpty(text))
            {
                // Convert character to Virtual Key Hex
                char[] chars = text.ToCharArray();
                short s = NativeMethods.VkKeyScan(chars[0]);
                currentKey = (VirtualKeyShort)s;
            }
        }
    }
}
