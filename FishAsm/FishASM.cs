using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace FishAsm {
	class Program {
		static void Main(string[] Args) {
			AssembledProgram Prog = new AssembledProgram();

			foreach (var Arg in Args) {
				if (File.Exists(Arg))
					Assemble(Arg, Prog);
			}

			if (File.Exists("system_calls.fishasm"))
				Assemble("system_calls.fishasm", Prog);

			Prog.RelocateAll();
			File.WriteAllBytes("program.fishcode", Prog.Link());
		}

		static void Error(string File, int Line, string Msg) {
			Console.WriteLine("ERROR in '{0}' line {1}; {2}", File, Line, Msg);
		}

		static void Assemble(string SrcFile, AssembledProgram Prog) {
			Console.WriteLine("Assembling '{0}'", SrcFile);
			string[] Lines = File.ReadAllLines(SrcFile);

			for (int i = 0; i < Lines.Length; i++) {
				if (Debugger.IsAttached) {
					Prog.AssembleLine(Lines[i]);
				} else {
					try {
						Prog.AssembleLine(Lines[i]);
					} catch (Exception E) {
						Error(SrcFile, i + 1, E.Message);
						return;
					}
				}
			}
		}
	}
}
