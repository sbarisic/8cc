using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FishAsm;

namespace FishcodeVM {
	public unsafe class FishMemory {
		byte[] Memory;

		public int Length {
			get {
				return Memory.Length;
			}
		}

		public FishMemory(int Size) {
			Memory = new byte[Size];
		}

		public byte this[int Idx] {
			get {
				return Memory[Idx];
			}

			set {
				Memory[Idx] = value;
			}
		}

		public byte[] this[int Idx, int Len] {
			get {
				return Read(Idx, Len);
			}
		}

		public void Write(int Idx, byte[] Bytes) {
			Array.Copy(Bytes, 0, Memory, Idx, Bytes.Length);
		}

		public void Write<T>(int Idx, T Val) where T : struct {
			GCHandle PinHandle = GCHandle.Alloc(Val, GCHandleType.Pinned);
			byte[] Bytes = new byte[Marshal.SizeOf<T>()];
			Marshal.Copy(PinHandle.AddrOfPinnedObject(), Bytes, 0, Bytes.Length);
			PinHandle.Free();

			Write(Idx, Bytes);
		}

		public T Read<T>(int Idx) where T : struct {
			if (typeof(T) == typeof(byte))
				return (T)(object)Memory[Idx];

			object RetObj = default(T);
			GCHandle PinHandle = GCHandle.Alloc(RetObj, GCHandleType.Pinned);
			Marshal.Copy(Memory, Idx, PinHandle.AddrOfPinnedObject(), Marshal.SizeOf<T>());
			PinHandle.Free();
			return (T)RetObj;
		}

		public T Read<T>(ulong Idx) where T : struct {
			return Read<T>((int)Idx);
		}

		public byte[] Read(int Idx, int Len) {
			byte[] Mem = new byte[Len];
			Array.Copy(Memory, Idx, Mem, 0, Len);
			return Mem;
		}
	}

	class Register {
		Registers ThisReg;
		ulong Value;

		Func<Registers, ulong> GetFunc;
		Action<Registers, ulong> SetFunc;

		public Register(Registers Reg, Func<Registers, ulong> GetFunc = null, Action<Registers, ulong> SetFunc = null) {
			ThisReg = Reg;
			this.GetFunc = GetFunc;
			this.SetFunc = SetFunc;
		}

		public ulong Get() {
			if (GetFunc != null)
				return GetFunc(ThisReg);

			return Value;
		}

		public void Set(ulong Val) {
			if (SetFunc != null) {
				SetFunc(ThisReg, Val);
				return;
			}

			Value = Val;
		}

		public override string ToString() {
			return string.Format("{0} = 0x{1:X}", ThisReg, Value);
		}
	}

	public class FishVM {
		FishMemory Memory;
		Dictionary<Registers, Register> Regs;
		bool Running;

		int RegSize(Registers R) {
			if (R >= Registers.B0 && R <= Registers.B9)
				return sizeof(int);

			if (R >= Registers.C0 && R <= Registers.C9)
				return sizeof(byte);

			if (R >= Registers.D0 && R <= Registers.D9)
				return sizeof(short);

			return sizeof(long);
		}

		ulong ReadReg(Registers R) {
			return Regs[R].Get();
		}

		void WriteReg(Registers R, ulong Val) {
			Regs[R].Set(Val);
		}

		Register CreateDelegateReg(Registers Reg, Registers UnderlyingReg, int Bits) {
			ulong MaxVal = (ulong)Math.Pow(2, Bits) - 1;

			return new Register(Reg, (R) => {
				return ReadReg(UnderlyingReg) & MaxVal;
			}, (R, Val) => {
				ulong NewVal = (ReadReg(UnderlyingReg) & (MaxVal << Bits)) | (Val & MaxVal);
				WriteReg(UnderlyingReg, NewVal);
			});
		}

		void CreateDelegateRegRange(Registers Min, Registers Max, int Bits) {
			for (int i = (int)Min; i <= (int)Max; i++)
				Regs.Add((Registers)i, CreateDelegateReg((Registers)i, (Registers)(i - (int)Min + 1), Bits));
		}

		public FishVM() {
			Regs = new Dictionary<Registers, Register>();
			Regs.Add(Registers.SP, new Register(Registers.SP));
			Regs.Add(Registers.BP, new Register(Registers.BP));
			Regs.Add(Registers.PC, new Register(Registers.PC));

			for (int i = (int)Registers.A0; i <= (int)Registers.A9; i++)
				Regs.Add((Registers)i, new Register((Registers)i));
			CreateDelegateRegRange(Registers.C0, Registers.C9, 8);
			CreateDelegateRegRange(Registers.B0, Registers.B9, 32);
			CreateDelegateRegRange(Registers.D0, Registers.D9, 16);

			Memory = new FishMemory(1024);
			WriteReg(Registers.SP, (ulong)Memory.Length);
			WriteReg(Registers.BP, ReadReg(Registers.SP));

			Running = true;
		}

		public void LoadProgram(byte[] Program, int MemoryLocation = 0) {
			Memory.Write(MemoryLocation, Program);
		}

		void Push<T>(T Val) where T : struct {
			WriteReg(Registers.SP, ReadReg(Registers.SP) - (ulong)Marshal.SizeOf<T>());
			Memory.Write((int)ReadReg(Registers.SP), Val);
		}

		void Push(Registers R) {
			Push(ReadReg(R));
		}

		T Pop<T>() where T : struct {
			T Ret = Memory.Read<T>(ReadReg(Registers.SP));
			WriteReg(Registers.SP, ReadReg(Registers.SP) + (ulong)Marshal.SizeOf<T>());
			return Ret;
		}

		void Pop(Registers R) {
			WriteReg(R, Pop<ulong>());
		}

		void Interrupt(uint Num) {
			if (Num == 42) {
				Running = false;
				return;
			}

			if (Num == 100) {
				//Console.WriteLine(">> Write");

				int Start = (int)ReadReg(Registers.A5);

				byte B = 0;
				while ((B = Memory.Read<byte>(Start++)) != 0)
					Console.Write((char)B);

				//Console.WriteLine();

			} else if (Num == 101) {
				//Console.WriteLine(">> WriteLine");

				int Start = (int)ReadReg(Registers.A5);

				byte B = 0;
				while ((B = Memory.Read<byte>(Start++)) != 0)
					Console.Write((char)B);

				Console.WriteLine();
			}
		}

		public bool Step() {
			ulong PC = ReadReg(Registers.PC);
			ulong InstructionSize = 1;

			bool IsBranch = false;
			ulong BranchTo = 0;

			Opcodes Opcode = (Opcodes)Memory.Read<byte>(PC);
			//Console.WriteLine(Opcode);

			switch (Opcode) {
				case Opcodes.INVALID:
					throw new Exception("Invalid opcode encountered");

				case Opcodes.NOP:
					break;

				case Opcodes.RET: {
						IsBranch = true;
						BranchTo = Pop<ulong>();
						break;
					}

				case Opcodes.POP:
				case Opcodes.PUSH: {
						InstructionSize += 1;

						Registers Reg = (Registers)Memory.Read<byte>((int)PC + 1);

						if (Opcode == Opcodes.POP)
							Pop(Reg);
						else
							Push(Reg);
						break;
					}

				case Opcodes.MOVE_R: {
						InstructionSize += 2;

						Registers Dest = (Registers)Memory.Read<byte>((int)PC + 1);
						Registers Src = (Registers)Memory.Read<byte>((int)PC + 2);
						WriteReg(Dest, ReadReg(Src));
						break;
					}

				case Opcodes.MOV_CST_REG:
				case Opcodes.ADD:
				case Opcodes.SUB: {
						InstructionSize += 1 + sizeof(long);

						Registers Dest = (Registers)Memory.Read<byte>((int)PC + 1);
						long Cst = Memory.Read<long>((int)PC + 2);

						if (Opcode == Opcodes.MOV_CST_REG)
							WriteReg(Dest, (ulong)Cst);
						else if (Opcode == Opcodes.ADD)
							WriteReg(Dest, (ulong)((long)ReadReg(Dest) + Cst));
						else if (Opcode == Opcodes.SUB)
							WriteReg(Dest, (ulong)((long)ReadReg(Dest) - Cst));
						break;
					}

				case Opcodes.LEA: {
						InstructionSize += 1 + sizeof(int);

						Registers Dest = (Registers)Memory.Read<byte>((int)PC + 1);
						int Offset = Memory.Read<int>((int)PC + 2);
						WriteReg(Dest, (ulong)((long)ReadReg(Registers.PC) + Offset));
						break;
					}

				case Opcodes.CALL: {
						InstructionSize += sizeof(int);
						int Offset = Memory.Read<int>((int)PC + 1);

						IsBranch = true;
						BranchTo = (ulong)((long)ReadReg(Registers.PC) + Offset);

						Push(ReadReg(Registers.PC) + InstructionSize);
						break;
					}

				case Opcodes.INT: {
						InstructionSize += sizeof(uint);
						Interrupt(Memory.Read<uint>((int)PC + 1));
						break;
					}

				default:
					throw new Exception("Unimplemented opcode " + Opcode);
			}

			if (IsBranch)
				WriteReg(Registers.PC, BranchTo);
			else
				WriteReg(Registers.PC, PC + InstructionSize);

			//ulong A0 = ReadReg(Registers.A0);
			return Running;
		}

		public void Run() {
			while (Step())
				;
		}
	}
}
