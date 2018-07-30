using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishcodeVM {
	class Program {
		static FishVM VM;

		static void Main(string[] Args) {
			VM = new FishVM();

			foreach (var Arg in Args) {
				if (File.Exists(Arg)) {
					VM.LoadProgram(File.ReadAllBytes(Arg));
					VM.Run();
				}
			}

			Console.ReadLine();
		}
	}
}
