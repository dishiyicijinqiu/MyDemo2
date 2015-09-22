IO端口完成
http://blogs.msdn.com/b/junfeng/archive/2008/12/01/threadpool-bindhandle.aspx


I mentioned that we can use ThreadPool.BindHandle to implement asynchronous IO. Here are roughly the steps necessary to make it happen:
1.       Create an overlapped file handle

            SafeFileHandle handle = CreateFile(
                                filename,
                                Win32.GENERIC_READ_ACCESS,
                                Win32.FILE_SHARE_READ | Win32.FILE_SHARE_WRITE | Win32.FILE_SHARE_DELETE,
                                (IntPtr)null,
                                Win32.OPEN_EXISTING,
                                Win32.FILE_FLAG_OVERLAPPED,
                                new SafeFileHandle(IntPtr.Zero, false));
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(
           string lpFileName,
           uint dwDesiredAccess,
           uint dwShareMode,
            //SECURITY_ATTRIBUTES lpSecurityAttributes,
           IntPtr lpSecurityAttributes,
           uint dwCreationDisposition,
           int dwFlagsAndAttributes,
           SafeFileHandle hTemplateFile);
2.       Bind the handle to thread pool.

            if (!ThreadPool.BindHandle(handle))
            {
                Console.WriteLine("Fail to BindHandle to threadpool.");
                return;
        }
3.       Prepare your asynchronous IO callback.

                byte[] bytes = new byte[0x8000];
 
                IOCompletionCallback iocomplete = delegate(uint errorCode, uint numBytes, NativeOverlapped* _overlapped)
                {
                    unsafe
                    {
                        try
                        {
                            if (errorCode == Win32.ERROR_HANDLE_EOF)
                                Console.WriteLine("End of file in callback.");
 
                            if (errorCode != 0 && numBytes != 0)
                            {
                                Console.WriteLine("Error {0} when reading file.", errorCode);
                            }
                            Console.WriteLine("Read {0} bytes.", numBytes);
                        }
                        finally
                        {
                            Overlapped.Free(pOverlapped);
                        }
                    }
                };   
 
4.       Create a NativeOverlapped* pointer.

                    Overlapped overlapped = new Overlapped();
 
                    NativeOverlapped* pOverlapped = overlapped.Pack(iocomplete, bytes);
 
                pOverlapped->OffsetLow = (int)offset;
5.       Call the asynchronous IO API and pass the NativeOverlapped * to it.

                    fixed (byte* p = bytes)
                    {
                        r = ReadFile(handle, p, bytes.Length, IntPtr.Zero, pOverlapped);
                        if (r == 0)
                        {
                            r = Marshal.GetLastWin32Error();
                            if (r == Win32.ERROR_HANDLE_EOF)
                            {
                                Console.WriteLine("Done.");
                                break;
                            }
 
                            if (r != Win32.ERROR_IO_PENDING)
                            {
                                Console.WriteLine("Failed to read file. LastError is {0}", Marshal.GetLastWin32Error());
                                Overlapped.Free(pOverlapped);
                                return;
                            }
                        }
                    }
 
        [DllImport("KERNEL32.dll", SetLastError = true)]
        unsafe internal static extern int ReadFile(
            SafeFileHandle handle,
            byte* bytes,
            int numBytesToRead,
            IntPtr numBytesRead_mustBeZero,
            NativeOverlapped* overlapped);
 
Your IO callback will be invoked by CLR thread when the IO completed.
 
So when should you use ThreadPool.BindHandle? The answer is almost *Never*. .Net Framework's FileStream class internally uses ThreadPool.BindHandle to implement the async IO. You should always use FileStream if possible.