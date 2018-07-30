using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishAsm {
	public static class Utils {
		public static byte[] Merge(params byte[][] Bytes) {
			List<byte> AllBytes = new List<byte>();
			foreach (var ByteArr in Bytes)
				AllBytes.AddRange(ByteArr);
			return AllBytes.ToArray();
		}
	}

	public struct Instruction {
		public int Size;
		public int ProgramOffset;

		public string InstructionMainLabel;
		public string Label;
		public string Source;

		public Opcodes Opcode;
		public byte[] Operands;

		public bool IsLabel;

		public bool IsData;
		public bool RequiresPatch;

		public void CalculateSize() {
			if (IsLabel) {
				Size = 0;
				return;
			}

			if (IsData)
				Size = Operands.Length;
			else
				Size = 1 + Operands.Length;
		}

		public override string ToString() {
			string RetStr = "";

			if (IsLabel)
				RetStr += string.Format("0x{0:X8} (0x{1:X}) {2}:", ProgramOffset, Size, Label);
			else {
				if (IsData) {
					const int MaxLen = 50;
					RetStr += string.Format("0x{0:X8} (0x{1:X})     {2}", ProgramOffset, Size, Source.Length >= MaxLen ? Source.Substring(0, MaxLen - 3) + "..." : Source);
				} else {
					RetStr += string.Format("0x{0:X8} (0x{1:X})     {2}", ProgramOffset, Size, Opcode);

					string Spaces = new string(' ', 40 - RetStr.Length);
					RetStr += string.Format("{0}0x{1:X2} {2}", Spaces, (byte)Opcode, string.Join(" ", Operands.Select(B => string.Format("0x{0:X2}", B))));
				}
			}

			return RetStr;
		}

		public static Instruction CreateLabel(string Source, string Label) {
			Instruction I = new Instruction();
			I.RequiresPatch = false;

			I.Source = Source;
			I.Label = Label;
			I.IsLabel = true;

			I.Opcode = Opcodes.INVALID;
			I.Operands = new byte[] { };
			I.CalculateSize();
			return I;
		}

		public static Instruction CreateData(string Source, byte[] Bytes) {
			Instruction I = new Instruction();
			I.RequiresPatch = false;
			I.Source = Source;
			I.Opcode = Opcodes.INVALID;

			I.Operands = Bytes;
			I.IsData = true;
			I.CalculateSize();
			return I;
		}

		public static Instruction CreateInstruction(int ProgramOffset, string Source, Opcodes Opcode, string[] Args, bool IsPatching, int PatchValue) {
			Instruction I = new Instruction();
			I.RequiresPatch = false;
			I.Source = Source;
			I.Label = "";
			I.ProgramOffset = ProgramOffset;

			I.Opcode = Opcode;

			switch (Opcode) {
				case Opcodes.INVALID:
				case Opcodes.NOP:
				case Opcodes.RET:
					I.Operands = new byte[] { };
					break;

				case Opcodes.INT:
					I.Operands = BitConverter.GetBytes(uint.Parse(Args[1]));
					break;

				case Opcodes.POP:
				case Opcodes.PUSH:
					I.Operands = new byte[] { (byte)Enum.Parse(typeof(Registers), Args[1]) };
					break;

				case Opcodes.MOVE_R:
					I.Operands = new byte[] { (byte)Enum.Parse(typeof(Registers), Args[1]), (byte)Enum.Parse(typeof(Registers), Args[2]) };
					break;

				case Opcodes.MOV_CST_REG:
				case Opcodes.ADD:
				case Opcodes.SUB:
					I.Operands = Utils.Merge(new byte[] { (byte)Enum.Parse(typeof(Registers), Args[1]) }, BitConverter.GetBytes(long.Parse(Args[2])));
					break;

				case Opcodes.LEA:
					I.RequiresPatch = true;
					I.Label = Args[2];
					I.Operands = new byte[] { (byte)Enum.Parse(typeof(Registers), Args[1]), 0, 0, 0, 0 };

					if (IsPatching)
						I.Operands = Utils.Merge(new byte[] { (byte)Enum.Parse(typeof(Registers), Args[1]) }, BitConverter.GetBytes(PatchValue - ProgramOffset));
					break;

				case Opcodes.CALL:
					I.RequiresPatch = true;
					I.Label = Args[1];

					I.Operands = new byte[] { 0, 0, 0, 0 };

					if (IsPatching)
						I.Operands = BitConverter.GetBytes(PatchValue - ProgramOffset);
					break;

				default:
					throw new NotImplementedException("Opcode not implemented " + Opcode);
			}

			if (I.RequiresPatch && IsPatching)
				I.RequiresPatch = false;

			I.CalculateSize();
			return I;
		}

		public byte[] ToByteArray() {
			if (IsLabel)
				return new byte[] { };

			if (IsData)
				return Operands;

			if (Operands != null && Operands.Length > 0)
				return Utils.Merge(new byte[] { (byte)Opcode }, Operands);

			return new byte[] { (byte)Opcode };
		}
	}

	public class AssembledProgram {
		static Exception CreateException(string Msg) {
			return new Exception(Msg);
		}

		static Exception CreateException(string Msg, string Msg2) {
			return CreateException(string.Format("{0}\n{1}", Msg, Msg2));
		}

		static Exception CreateException(string Msg, string Fmt, params object[] Args) {
			return CreateException(string.Format("{0}\n{1}", Msg, string.Format(Fmt, Args)));
		}

		Dictionary<string, List<Instruction>> Sections;
		List<Instruction> CurrentSection;
		List<string> GlobalLabels;
		string CurrentMainLabel;

		public AssembledProgram() {
			Sections = new Dictionary<string, List<Instruction>>();
			GlobalLabels = new List<string>();

			SetCurrentSection("text");
		}

		void SetCurrentSection(string Name) {
			if (!Sections.ContainsKey(Name))
				Sections.Add(Name, new List<Instruction>());

			if (CurrentSection != Sections[Name]) {
				CurrentSection = Sections[Name];

				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("SECTION '{0}'", Name);
				Console.ResetColor();
			}
		}

		void AssemblerDirective(string Line, string[] Args) {
			switch (Args[0].ToUpper()) {
				case ".SECTION":
					SetCurrentSection(Args[1]);
					break;

				case ".GLOBAL":
					if (!GlobalLabels.Contains(Args[1]))
						GlobalLabels.Add(Args[1]);
					break;

				case ".STRING":
					AddInstruction(Instruction.CreateData(Line, Utils.Merge(Encoding.ASCII.GetBytes(Args[1]), new byte[] { 0 })));
					break;

				default:
					throw CreateException("Unknown assembler directive", Line);
			}
		}

		void Assemble(string Line, string[] Args) {
			if (Line.EndsWith(":")) {
				if (Args.Length != 1)
					throw CreateException("Labels cannot contain spaces");

				string Label = Args[0].Substring(0, Args[0].Length - 1);
				AddInstruction(Instruction.CreateLabel(Line, Label));
				return;
			}

			if (Enum.TryParse(Args[0], out Opcodes Opcode)) {
				AddInstruction(Instruction.CreateInstruction(0, Line, Opcode, Args, false, 0));
			} else
				throw CreateException("Invalid opcode", Line);
		}

		void AddInstruction(Instruction I) {
			int CurrentOffset = 0;


			if (I.IsLabel)
				if (I.Label.StartsWith(".")) {
					if (string.IsNullOrWhiteSpace(CurrentMainLabel))
						throw CreateException("Sub-label in invalid location", I.Source);

					I.Label = CurrentMainLabel + I.Label;
				} else
					CurrentMainLabel = I.Label;
			else
				I.InstructionMainLabel = CurrentMainLabel;

			foreach (var Inst in CurrentSection) {
				CurrentOffset += Inst.Size;

				/*if (I.IsLabel && Inst.IsLabel && I.Label == Inst.Label && !I.Label.StartsWith("."))
					throw CreateException("Duplicate label", I.Label);*/
			}

			I.ProgramOffset = CurrentOffset;
			if (I.RequiresPatch)
				Console.ForegroundColor = ConsoleColor.Green;

			Console.WriteLine(I);

			Console.ResetColor();
			CurrentSection.Add(I);
		}

		int RelocateSection(List<Instruction> Section, int Offset) {
			for (int i = 0; i < Section.Count; i++) {
				Instruction Inst = Section[i];
				Inst.ProgramOffset = Offset;
				Offset += Inst.Size;
				Section[i] = Inst;
			}

			return Offset;
		}

		public void AssembleLine(string Line) {
			Line = Line.Trim();
			if (string.IsNullOrWhiteSpace(Line))
				return;

			if (Line.StartsWith(";"))
				return;

			string[] Args = ParseLine(Line).ToArray();
			if (Line.StartsWith(".") && !(Line.StartsWith(".LABEL") || Line.StartsWith(".__END")))
				AssemblerDirective(Line, Args);
			else
				Assemble(Line, Args);
		}

		public void RelocateAll(int TextOffset = 0) {
			int NextFreeOffset = RelocateSection(Sections["text"], TextOffset);

			foreach (var Section in Sections) {
				if (Section.Key == "text")
					continue;

				NextFreeOffset = RelocateSection(Section.Value, NextFreeOffset);
			}
		}

		List<Instruction> GetInstructionArray() {
			List<Instruction> Instructions = new List<Instruction>();

			foreach (var Section in Sections)
				Instructions.AddRange(Section.Value);

			return Instructions;
		}

		int FindLabelAddress(List<Instruction> Instructions, string LabelName) {
			foreach (var I in Instructions)
				if (I.IsLabel && I.Label == LabelName)
					return I.ProgramOffset;

			throw CreateException("Unresolved symbol", LabelName);
		}

		public byte[] Link() {
			List<Instruction> Instructions = GetInstructionArray();

			for (int i = 0; i < Instructions.Count; i++) {
				if (Instructions[i].RequiresPatch) {
					Instruction I = Instructions[i];
					string Lbl = I.Label;

					if (Lbl.StartsWith("."))
						Lbl = I.InstructionMainLabel + Lbl;

					I = Instruction.CreateInstruction(I.ProgramOffset, I.Source, I.Opcode, ParseLine(I.Source).ToArray(), true, FindLabelAddress(Instructions, Lbl));
					Instructions[i] = I;
				}
			}

			return Instructions.SelectMany(I => I.ToByteArray()).ToArray();
		}

		static StringBuilder ParseBuilder = new StringBuilder();
		static IEnumerable<string> ParseLine(string Line) {
			ParseBuilder.Length = 0;
			//Line = Line.Replace("=", " = ").Replace(";", " ; ").Replace("\r", "").Replace("\n", "");
			bool InsideQuote = false;

			const string Symbols = "+-=;";
			const string SkipSymbols = " ,";

			char LastChr = (char)0;
			foreach (var Chr in Line) {
				if (!InsideQuote && SkipSymbols.Contains(Chr)) {
					if (ParseBuilder.Length > 0) {
						yield return ParseBuilder.ToString();
						ParseBuilder.Length = 0;
					}
				} else if (!InsideQuote && Symbols.Contains(Chr)) {
					if (ParseBuilder.Length > 0) {
						yield return ParseBuilder.ToString();
						ParseBuilder.Length = 0;
					}

					yield return Chr.ToString();
				} else if (Chr == '"' && LastChr != '\\')
					InsideQuote = !InsideQuote;
				else if (Chr == '"' && LastChr == '\\') {
					ParseBuilder.Length--;
					ParseBuilder.Append('"');
				} else
					ParseBuilder.Append(Chr);

				LastChr = Chr;
			}

			if (ParseBuilder.Length > 0)
				yield return ParseBuilder.ToString();
		}
	}
}
