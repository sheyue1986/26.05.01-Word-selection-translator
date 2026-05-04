using System.Runtime.InteropServices;
using System.Text;

namespace DesktopAiTranslator.Services;

public sealed class CredentialService
{
    public string Protect(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return "";
        }

        var bytes = Encoding.UTF8.GetBytes(plainText);
        var input = DataBlob.FromBytes(bytes);
        try
        {
            if (!CryptProtectData(ref input, "DesktopAiTranslator", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, out var output))
            {
                return "";
            }

            try
            {
                var protectedBytes = output.ToBytes();
                return Convert.ToBase64String(protectedBytes);
            }
            finally
            {
                LocalFree(output.pbData);
            }
        }
        finally
        {
            input.Free();
        }
    }

    public string Unprotect(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return "";
        }

        var bytes = Convert.FromBase64String(cipherText);
        var input = DataBlob.FromBytes(bytes);
        try
        {
            if (!CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, out var output))
            {
                return "";
            }

            try
            {
                return Encoding.UTF8.GetString(output.ToBytes());
            }
            finally
            {
                LocalFree(output.pbData);
            }
        }
        finally
        {
            input.Free();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int cbData;
        public IntPtr pbData;

        public static DataBlob FromBytes(byte[] bytes)
        {
            var blob = new DataBlob
            {
                cbData = bytes.Length,
                pbData = Marshal.AllocHGlobal(bytes.Length)
            };
            Marshal.Copy(bytes, 0, blob.pbData, bytes.Length);
            return blob;
        }

        public byte[] ToBytes()
        {
            var bytes = new byte[cbData];
            Marshal.Copy(pbData, bytes, 0, cbData);
            return bytes;
        }

        public void Free()
        {
            if (pbData != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(pbData);
                pbData = IntPtr.Zero;
            }
        }
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(
        ref DataBlob pDataIn,
        string? szDataDescr,
        IntPtr pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        int dwFlags,
        out DataBlob pDataOut);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(
        ref DataBlob pDataIn,
        IntPtr ppszDataDescr,
        IntPtr pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        int dwFlags,
        out DataBlob pDataOut);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);
}
