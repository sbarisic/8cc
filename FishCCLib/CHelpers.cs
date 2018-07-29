using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;

namespace FishCCLib {
	public static unsafe class CHelpers {
		[DllExport("csharp_clean", CallingConvention.Cdecl)]
		public static string Clean(string In) {
			return Path.GetFullPath(In);
		}

		[DllExport("csharp_strncasecmp", CallingConvention.Cdecl)]
		public static int StrNCaseCmp(IntPtr A, IntPtr B, int Cnt) {
			string StrA = Marshal.PtrToStringAnsi(A, Cnt);
			string StrB = Marshal.PtrToStringAnsi(B, Cnt);

			return StrCaseCmpImpl(StrA, StrB);
		}

		[DllExport("csharp_strcasecmp", CallingConvention.Cdecl)]
		public static int StrCaseCmp(string A, string B) {
			return StrCaseCmpImpl(A, B);
		}

		public static int StrCaseCmpImpl(string A, string B) {
			if (A.ToLower() == B.ToLower())
				return 0;
			return 1;
		}

		[DllExport("csharp_dirname", CallingConvention.Cdecl)]
		public static string Dirname(string Dir) {
			if (!(Dir = Dir.Replace("/", "\\")).Contains("\\"))
				return ".";

			return Path.GetDirectoryName(Dir);
		}

		[DllExport("csharp_basename", CallingConvention.Cdecl)]
		public static string Basename(string Dir) {
			return Path.GetFileName(Dir);
		}


		[DllExport("csharp_mkstemps", CallingConvention.Cdecl)]
		public static int MakeTemp(IntPtr Dir, int SuffixLen) {
			int Len = 0;
			while (Marshal.ReadByte(Dir, Len) != 0)
				Len++;

			string DirStr = Marshal.PtrToStringAnsi(Dir, Len);

			Random Rnd = new Random();
			string RndFileName = "_tmp_" + Rnd.Next(10000, 99999) + Path.GetExtension(DirStr);

			byte[] Bytes = Encoding.ASCII.GetBytes(RndFileName);
			Marshal.Copy(Bytes, 0, Dir, Bytes.Length);
			Marshal.WriteByte(Dir, Bytes.Length, 0);

			return 42;
		}
	}
}