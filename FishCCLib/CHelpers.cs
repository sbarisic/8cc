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
		public static int MakeTemp(string Dir, int SuffixLen) {

			return 42;
		}
	}
}