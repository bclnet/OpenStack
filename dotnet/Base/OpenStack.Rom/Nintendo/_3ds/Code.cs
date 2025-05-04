using Gee.External.Capstone;
using Gee.External.Capstone.Arm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using static Gee.External.Capstone.Arm.ArmOperandType;
using static Gee.External.Capstone.Arm.ArmRegisterId;
using static System.Console;

namespace OpenStack.Rom.Nintendo._3ds;

public unsafe class Code {

    public struct SFunction(int first, int last) {
        public int First = first;
        public int Last = last;
    }

    public string FileName;
    public bool Verbose = false;
    public int RegionCode = -1;
    public int LanguageCode = -1;
    uint[] Arm;
    byte[] Thumb;
    CapstoneArmDisassembler Handle;
    ArmInstruction[] Disasm = null;

    //public void SetFileName(string fileName) => throw new NotImplementedException();
    //public void SetVerbose(bool verbose) => throw new NotImplementedException();
    //public void SetRegionCode(int regionCode) => throw new NotImplementedException();
    //public void SetLanguageCode(int languageCode) => throw new NotImplementedException();

    public bool Lock() {
        try {
            byte[] code = [];
            using (var s = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                s.Seek(0, SeekOrigin.End);
                var codeSize = (int)s.Position;
                if (codeSize < 4) { WriteLine("ERROR: code is too short\n\n"); return false; }
                Array.Resize(ref code, codeSize);
                s.Seek(0, SeekOrigin.Begin);
                s.Read(ref code, 1, code.Length);
            }
            Arm = MemoryMarshal.Cast<byte, uint>(code).ToArray();
            Thumb = code;
            bool resultArm = LockArm(), resultThumb = resultArm && LockThumb();
            using (var s = File.Open(FileName, FileMode.Create, FileAccess.Write, FileShare.Write))
                s.Write(ref code, 1, code.Length);
            if (!resultArm && !resultThumb) WriteLine("ERROR: lock failed\n\n");
            return resultArm || resultThumb;
        }
        catch (IOException) { return false; }
    }

    bool LockArm() {
        if (Verbose) WriteLine("INFO: lock arm\n");
        try {
            var result = true;
            using (Handle = new CapstoneArmDisassembler(ArmDisassembleMode.Arm) { EnableInstructionDetails = true }) {
                if (RegionCode >= 0) {
                    if (Verbose) WriteLine("INFO: lock region arm\n");
                    var resultRegion = LockRegionArm();
                    if (!resultRegion) WriteLine("ERROR: lock region arm failed\n\n");
                    result = result && resultRegion;
                }
                if (LanguageCode >= 0) {
                    if (Verbose) WriteLine("INFO: lock language arm\n");
                    var resultLanguage = LockLanguageArm();
                    if (!resultLanguage) WriteLine("ERROR: lock language arm failed\n\n");
                    result = result && resultLanguage;
                }
                if (!result) WriteLine("ERROR: lock arm failed\n\n");
                return result;
            }
        }
        catch (CapstoneException) { WriteLine("ERROR: open arm handlefailed\n\n"); return false; }
    }

    bool LockThumb() {
        if (Verbose) WriteLine("INFO: lock thumb\n");
        try {
            var result = true;
            using (Handle = new CapstoneArmDisassembler(ArmDisassembleMode.Thumb) { EnableInstructionDetails = true }) {
                if (RegionCode >= 0) {
                    if (Verbose) WriteLine("INFO: lock region thumb\n");
                    var resultRegion = LockRegionThumb();
                    if (!resultRegion) WriteLine("ERROR: lock region thumb failed\n\n");
                    result = result && resultRegion;
                }
                if (LanguageCode >= 0) {
                    if (Verbose) WriteLine("INFO: lock language thumb\n");
                    var resultLanguage = LockLanguageThumb();
                    if (!resultLanguage) WriteLine("ERROR: lock language thumb failed\n\n");
                    result = result && resultLanguage;
                }
            }
            if (!result) WriteLine("ERROR: lock thumb failed\n\n");
            return result;
        }
        catch (CapstoneException) { WriteLine("ERROR: open thumb handle failed\n\n"); return false; }
    }

    bool LockRegionArm() {
        if (Verbose) WriteLine("INFO: find arm nn::cfg::CTR::detail::IpcUser::GetRegion\n");
        SFunction functionGetRegion = new();
        FindGetRegionFunctionArm(ref functionGetRegion);
        if (functionGetRegion.First == functionGetRegion.Last || functionGetRegion.Last == 0) return false;
        if (Verbose) {
            WriteLine("INFO: nn::cfg::CTR::detail::IpcUser::GetRegion\n");
            WriteLine("INFO:   func:\n");
            WriteLine($"INFO:     first: {functionGetRegion.First * 4:08X}\n");
            WriteLine($"INFO:     last:  {functionGetRegion.Last * 4:08X}\n");
        }
        return PatchGetRegionFunctionArm(functionGetRegion);
    }

    bool LockRegionThumb() {
        if (Verbose) WriteLine("INFO: find thumb nn::cfg::CTR::detail::IpcUser::GetRegion\n");
        SFunction functionGetRegion = new();
        FindGetRegionFunctionThumb(ref functionGetRegion);
        if (functionGetRegion.First == functionGetRegion.Last || functionGetRegion.Last == 0) return false;
        if (Verbose) {
            WriteLine("INFO: nn::cfg::CTR::detail::IpcUser::GetRegion\n");
            WriteLine("INFO:   func:\n");
            WriteLine($"INFO:     first: {functionGetRegion.First:08X}\n");
            WriteLine($"INFO:     last:  {functionGetRegion.Last:8X}\n");
        }
        return PatchGetRegionFunctionThumb(functionGetRegion);
    }

    bool LockLanguageArm() {
        if (Verbose) WriteLine("INFO: find arm nn::cfg::CTR::GetLanguage\n");
        SFunction functionGetLanguage = new();
        FindGetLanguageFunctionArm(functionGetLanguage);
        if (functionGetLanguage.First == functionGetLanguage.Last || functionGetLanguage.Last == 0) return false;
        if (Verbose) {
            WriteLine("INFO: nn::cfg::CTR::GetLanguage\n");
            WriteLine("INFO:   func:\n");
            WriteLine($"INFO:     first: {functionGetLanguage.First * 4:08X}\n");
            WriteLine($"INFO:     last:  {functionGetLanguage.Last * 4:08X}\n");
        }
        return PatchGetLanguageFunctionArm(functionGetLanguage);
    }

    bool LockLanguageThumb() {
        if (Verbose) WriteLine("INFO: find thumb nn::cfg::CTR::GetLanguageRaw\n");
        SFunction functionGetLanguage = new();
        FindGetLanguageFunctionThumb(functionGetLanguage);
        if (functionGetLanguage.First == functionGetLanguage.Last || functionGetLanguage.Last == 0) return false;
        if (Verbose) {
            WriteLine("INFO: nn::cfg::CTR::GetLanguageRaw\n");
            WriteLine("INFO:   func:\n");
            WriteLine($"INFO:     first: {functionGetLanguage.First:08X}\n");
            WriteLine($"INFO:     last:  {functionGetLanguage.Last:08X}\n");
        }
        return PatchGetLanguageFunctionThumb(functionGetLanguage);
    }

    void FindGetRegionFunctionArm(ref SFunction function) {
        ArmInstruction insn; ArmInstructionDetail detail;
        var functions = new List<SFunction>();
        for (var i = 0; i < Arm.Length; i++) {
            // mov r0, #0x20000
            if (Arm[i] == 0xE3A00802) {
                SFunction func = new(i, i);
                FindFunctionArm(ref func);
                for (var j = i + 1; j < func.Last; j++)
                    // svc 0x32
                    if (Arm[j] == 0xEF000032) { functions.Add(func); break; }
            }
        }
        // nn::cfg::CTR::detail::Initialize
        for (var i = 0; i < Arm.Length; i++) {
            // nn::srv::Initialize
            // nn::Result
            // Level	-5 LEVEL_PERMANENT
            // Summary	5 SUMMARY_INVALID_STATE
            // Module	64 MODULE_NN_CFG
            if (Arm[i] == 0xD8A103F9)
                for (var j = i - 4; j < i + 4; j++)
                    if (j >= 0 && j < Arm.Length)
                        foreach (var func in functions)
                            // nn::cfg::CTR::detail::IpcUser::s_Session
                            if (func.Last + 1 < Arm.Length && Arm[j] == Arm[func.Last + 1]) { function.First = func.First; function.Last = func.Last; return; }
        }
        for (var i = 0; i < Arm.Length; i++) {
            // mov r0, #0x20000
            if (Arm[i] == 0xE3A00802) {
                SFunction func = new(i, i);
                FindFunctionArm(ref func);
                for (var j = i + 1; j < func.Last; j++) {
                    // nn::svc::SendSyncRequest
                    Disasm = Handle.Disassemble(MemoryMarshal.Cast<uint, byte>(Arm.AsSpan(j)).ToArray(), 4, 0x100000 + j * 4);
                    if (Disasm.Length > 1 && (insn = Disasm[0]) != null)
                        if (insn.Mnemonic == "bl" && (detail = insn.Details) != null && detail.Operands.Length == 1) {
                            var armOp0 = detail.Operands[0];
                            if (armOp0.Type == Immediate && armOp0.Immediate >= 0x100000 && armOp0.Immediate + 8 <= 0x100000 + Arm.Length * 4 && armOp0.Immediate % 4 == 0) {
                                var kFunction = (armOp0.Immediate - 0x100000) / 4;
                                // svc 0x32
                                // bx lr
                                if (Arm[kFunction] == 0xEF000032 && Arm[kFunction + 1] == 0xE12FFF1E) { functions.Add(func); break; }
                            }
                        }
                }
            }
        }
        // nn::cfg::CTR::detail::Initialize
        for (var i = 0; i < Arm.Length; i++) {
            // nn::srv::Initialize
            // nn::Result
            // Level	-5 LEVEL_PERMANENT
            // Summary	5 SUMMARY_INVALID_STATE
            // Module	64 MODULE_NN_CFG
            if (Arm[i] == 0xD8A103F9)
                for (var j = i - 4; j < i + 4; j++)
                    if (j >= 0 && j < Arm.Length)
                        foreach (var func in functions)
                            // nn::cfg::CTR::detail::IpcUser::s_Session
                            if (func.Last + 1 < Arm.Length && Arm[j] == Arm[func.Last + 1]) { function.First = func.First; function.Last = func.Last; return; }
        }
    }

    void FindGetRegionFunctionThumb(ref SFunction function) {
        ArmInstruction insn; ArmInstructionDetail detail;
        SFunction functionGetLanguage = new();
        FindGetLanguageFunctionThumb(functionGetLanguage);
        if (functionGetLanguage.First == functionGetLanguage.Last || functionGetLanguage.Last == 0) return;
        // nn::cfg::CTR::GetLanguage
        // nn::cfg::CTR::GetLanguageRaw()
        int getLanguage = -1, codeSizeMax = 4;
        for (var i = 0; i < Thumb.Length; i += 2) {
            var over = false;
            codeSizeMax = Thumb.Length - i;
            if (codeSizeMax > 4) codeSizeMax = 4;
            Disasm = Handle.Disassemble(Thumb.AsSpan(i).ToArray(), codeSizeMax, 0x100000 + i);
            if (Disasm.Length != 0 && (insn = Disasm[0]) != null) //!cs_insn_group(m_uHandle, m_pInsn, ARM_GRP_THUMB2)) {
                if (insn.Mnemonic == "bl" && (detail = insn.Details) != null && detail.Operands.Length == 1) {
                    var armOp0 = detail.Operands[0];
                    if (armOp0.Type == Immediate && armOp0.Immediate == 0x100000 + functionGetLanguage.First - 2) over = true;
                }
            //cs_free(Disasm, Disasm.Length);
            if (over) { getLanguage = i; break; }
        }
        if (getLanguage < 0) return;
        int getRegion = -1, codeSize = 4;
        for (var i = getLanguage + 4; i < Thumb.Length; i += codeSize) {
            var over = false;
            codeSizeMax = Thumb.Length - i;
            if (codeSizeMax > 4) codeSizeMax = 4;
            codeSize = 2;
            Disasm = Handle.Disassemble(Thumb.AsSpan(i).ToArray(), codeSizeMax, 0x100000 + i);
            //Handle.GetInstructionGroupName(ArmInstructionGroupId.ARM_GRP_THUMB2);
            if (Disasm.Length > 0 && (insn = Disasm[0]) != null) { //!cs_insn_group(m_uHandle, m_pInsn, ARM_GRP_THUMB2)) {
                codeSize = insn.Bytes.Length;
                if (insn.Mnemonic == "bl" && (detail = insn.Details) != null && detail.Operands.Length == 1) {
                    var armOp0 = detail.Operands[0];
                    if (armOp0.Type == Immediate && armOp0.Immediate >= 0x100000 && armOp0.Immediate < 0x100000 + Thumb.Length) {
                        getRegion = armOp0.Immediate - 0x100000;
                        over = true;
                    }
                }
            }
            //cs_free(m_pInsn, m_uDisasmCount);
            if (over) break;
        }
        SFunction functionGetRegion = new(getRegion, getRegion);
        FindFunctionThumb(ref functionGetRegion);
        for (var i = functionGetRegion.First + 2; i < functionGetRegion.Last; i += codeSize) {
            var over = false;
            codeSizeMax = Thumb.Length - i;
            if (codeSizeMax > 4) codeSizeMax = 4;
            codeSize = 2;
            Disasm = Handle.Disassemble(Thumb.AsSpan(i).ToArray(), codeSizeMax, 0x100000 + i);
            if (Disasm.Length > 0 && (insn = Disasm[0]) != null) {  //&& !cs_insn_group(m_uHandle, m_pInsn, ARM_GRP_THUMB2)) {
                codeSize = insn.Bytes.Length;
                if (insn.Mnemonic == "bl" && (detail = insn.Details) != null && detail.Operands.Length == 1) {
                    var armOp0 = detail.Operands[0];
                    if (armOp0.Type == Immediate && armOp0.Immediate >= 0x100000 && armOp0.Immediate < 0x100000 + Thumb.Length) {
                        getRegion = armOp0.Immediate - 0x100000;
                        over = true;
                    }
                }
            }
            //cs_free(m_pInsn, m_uDisasmCount);
            if (over) break;
        }
        function.Last = function.First = getRegion + 4;
        FindFunctionThumb(ref function);
    }

    void FindGetLanguageFunctionArm(SFunction function) {
        ArmInstruction insn; ArmInstructionDetail detail;
        for (var i = 0; i < Arm.Length; i++) {
            // nn::cfg::CTR::detail::GetConfig
            // key
            if (Arm[i] == 0xA0002) {
                var index = i - 4;
                if (index < 0) index = 0;
                SFunction func = new(index, index);
                FindFunctionArm(ref func);
                for (var j = func.First + 1; j < func.Last; j++) {
                    var over = false;
                    Disasm = Handle.Disassemble(MemoryMarshal.Cast<uint, byte>(Arm.AsSpan(j)).ToArray(), 4, 0x100000 + j * 4);
                    if (Disasm.Length > 0 && (insn = Disasm[0]) != null) {
                        if (insn.Mnemonic == "ldr" && (detail = insn.Details) != null && detail.Operands.Length > 1) {
                            var armOp0 = detail.Operands[0];
                            var armOp1 = detail.Operands[1];
                            // ldr rm, =0xA0002
                            if (armOp0.Type == Register && armOp1.Type == Memory && armOp1.Register.Id == ARM_REG_PC && armOp1.Memory.Displacement == (i - j - 2) * 4) over = true;
                        }
                    }
                    //cs_free(m_pInsn, m_uDisasmCount);
                    if (over) { function.First = func.First; function.Last = func.Last; return; }
                }
            }
        }
    }

    void FindGetLanguageFunctionThumb(SFunction function) {
        ArmInstruction insn; ArmInstructionDetail detail;
        for (var i = 0; i < Thumb.Length; i += 4) {
            // nn::cfg::CTR::detail::GetConfig
            // key
            if (MemoryMarshal.Cast<byte, uint>(Thumb.AsSpan(i, 2))[0] == 0xA0002) {
                var offset = i - 16;
                if (offset < 0) offset = 0;
                SFunction func = new(offset, offset);
                FindFunctionThumb(ref func);
                var codeSize = 4;
                for (var j = func.First; j < func.Last; j += codeSize) {
                    var over = false;
                    var codeSizeMax = func.Last - j;
                    if (codeSizeMax > 4) codeSizeMax = 4;
                    codeSize = 2;
                    Disasm = Handle.Disassemble(Thumb.AsSpan(j).ToArray(), codeSizeMax, 0x100000 + j);
                    if (Disasm.Length > 0 && (insn = Disasm[0]) != null) { // !cs_insn_group(m_uHandle, m_pInsn, ARM_GRP_THUMB2)) {
                        codeSize = insn.Bytes.Length;
                        if (insn.Mnemonic == "ldr" && (detail = insn.Details) != null && detail.Operands.Length > 1) {
                            var armOp0 = detail.Operands[0];
                            var armOp1 = detail.Operands[1];
                            // ldr rm, =0xA0002
                            if (armOp0.Type == Register && armOp1.Type == Memory && armOp1.Register.Id == ARM_REG_PC && armOp1.Memory.Displacement == i - j - 4) over = true;
                        }
                    }
                    //cs_free(m_pInsn, m_uDisasmCount);
                    if (over) { function.First = func.First; function.Last = func.Last; return; }
                }
            }
        }
    }

    void FindFunctionArm(ref SFunction function) {
        ArmInstruction insn;
        for (var i = function.Last; i < Arm.Length; i++) {
            var over = false;
            Disasm = Handle.Disassemble(MemoryMarshal.Cast<uint, byte>(Arm.AsSpan(i)).ToArray(), 4, 0x100000 + i * 4);
            if (Disasm.Length > 0 && (insn = Disasm[0]) != null)
                if (insn.Mnemonic == "pop" || insn.Mnemonic == "bx" || insn.Mnemonic == "lr") over = true;
            //cs_free(m_pInsn, m_uDisasmCount);
            if (over) { function.Last = i; break; }
        }
        for (var i = function.First; i >= 0; i--) {
            var over = false;
            Disasm = Handle.Disassemble(MemoryMarshal.Cast<uint, byte>(Arm.AsSpan(i)).ToArray(), 4, 0x100000 + i * 4);
            if (Disasm.Length > 0 && (insn = Disasm[0]) != null)
                if (insn.Mnemonic == "push") over = true;
            //cs_free(m_pInsn, m_uDisasmCount);
            if (over) { function.First = i; break; }
        }
    }

    void FindFunctionThumb(ref SFunction function) {
        ArmInstruction insn;
        int codeSize = 4, codeSizeMax = 4;
        for (var i = function.Last; i < Thumb.Length; i += codeSize) {
            var over = false;
            codeSizeMax = Thumb.Length - i;
            if (codeSizeMax > 4) codeSizeMax = 4;
            codeSize = 2;
            Disasm = Handle.Disassemble(Thumb.AsSpan(i).ToArray(), codeSizeMax, 0x100000 + i);
            if (Disasm.Length > 0 && (insn = Disasm[0]) != null) { // !cs_insn_group(m_uHandle, m_pInsn, ARM_GRP_THUMB2)) {
                codeSize = insn.Bytes.Length;
                if (insn.Mnemonic == "pop" || insn.Mnemonic == "bx" && insn.Operand == "lr") over = true;
            }
            //cs_free(m_pInsn, m_uDisasmCount);
            if (over) { function.Last = i; break; }
        }
        int codeSizeCache = 0;
        for (var i = function.First; i >= 0; i -= codeSize) {
            var over = false;
            if (codeSizeCache == 0) {
                codeSizeMax = Thumb.Length - i;
                if (codeSizeMax > 4) codeSizeMax = 4;
            }
            else {
                codeSizeMax = codeSizeCache;
                codeSizeCache = 0;
            }
            codeSize = 2;
            Disasm = Handle.Disassemble(Thumb.AsSpan(i).ToArray(), codeSizeMax, 0x100000 + i);
            if (Disasm.Length == 0 && i >= 2) { //|| cs_insn_group(m_uHandle, m_pInsn, ARM_GRP_THUMB2)) {
                i -= 2;
                codeSizeMax += 2;
                Disasm = Handle.Disassemble(Thumb.AsSpan(i).ToArray(), codeSizeMax, 0x100000 + i);
            }
            if (Disasm.Length == 2 && (insn = Disasm[0]) != null) {
                codeSizeCache = insn.Bytes.Length;
                i += codeSizeCache;
                codeSizeMax -= codeSizeCache;
                Disasm = Handle.Disassemble(Thumb.AsSpan(i).ToArray(), codeSizeMax, 0x100000 + i);
            }
            if (Disasm.Length > 0 && (insn = Disasm[0]) != null) {
                if (codeSizeCache == 0) {
                    codeSize = 4;
                    if (i < 4) codeSize = 2;
                }
                else codeSize = codeSizeCache;
                if (insn.Mnemonic == "push") over = true;
            }
            //cs_free(m_pInsn, m_uDisasmCount);
            if (over) { function.First = i; break; }
        }
    }

    bool PatchGetRegionFunctionArm(SFunction function) {
        ArmInstruction insn; ArmInstructionDetail detail;
        for (var i = function.First + 1; i < function.Last; i++) {
            var over = false;
            var rt = -1;
            Disasm = Handle.Disassemble(MemoryMarshal.Cast<uint, byte>(Arm.AsSpan(i)).ToArray(), 4, 0x100000 + i * 4);
            if (Disasm.Length > 0 && (insn = Disasm[0]) != null) {
                if (insn.Mnemonic == "ldrb" && (detail = insn.Details) != null && detail.Operands.Length > 0) {
                    var armOp0 = detail.Operands[0];
                    if (armOp0.Type == Register)
                        switch (armOp0.Register.Id) {
                            case ARM_REG_R0: rt = 0; break;
                            case ARM_REG_R1: rt = 1; break;
                            case ARM_REG_R2: rt = 2; break;
                            case ARM_REG_R3: rt = 3; break;
                            case ARM_REG_R4: rt = 4; break;
                            case ARM_REG_R5: rt = 5; break;
                            case ARM_REG_R6: rt = 6; break;
                            case ARM_REG_R7: rt = 7; break;
                            case ARM_REG_R8: rt = 8; break;
                            case ARM_REG_R9: rt = 9; break;
                            case ARM_REG_R10: rt = 10; break;
                            case ARM_REG_R11: rt = 11; break;
                            case ARM_REG_R12: rt = 12; break;
                            case ARM_REG_R13: rt = 13; break; // ARM_REG_SP
                            case ARM_REG_R14: rt = 14; break; // ARM_REG_LR
                            case ARM_REG_R15: rt = 15; break; // ARM_REG_PC
                        }
                }
                if (rt >= 0) over = true;
            }
            //cs_free(m_pInsn, m_uDisasmCount);
            if (over) {
                Arm[i] = 0xE3A00000U | (uint)(rt << 12) | (uint)RegionCode;
                WriteLine($"INFO:   modify:  {i * 4:08X}  mov r{rt}, #0x{RegionCode:x} ; {Arm[i] & 0xFF:02X} {Arm[i] >> 8 & 0xFF:02X} {Arm[i] >> 16 & 0xFF:02X} {Arm[i] >> 24 & 0xFF:02X}\n");
                return true;
            }
        }
        return false;
    }

    bool PatchGetRegionFunctionThumb(SFunction function) {
        ArmInstruction insn; ArmInstructionDetail detail;
        var codeSize = 4;
        for (var i = function.First + 2; i < function.Last; i += codeSize) {
            var over = false;
            var rt = -1;
            var codeSizeMax = Thumb.Length - i;
            if (codeSizeMax > 4) codeSizeMax = 4;
            codeSize = 2;
            Disasm = Handle.Disassemble(Thumb.AsSpan(i).ToArray(), codeSizeMax, 0x100000 + i);
            if (Disasm.Length > 0 && (insn = Disasm[0]) != null) {
                codeSize = insn.Bytes.Length;
                if (insn.Mnemonic == "ldrb" && (detail = insn.Details) != null && detail.Operands.Length > 0) {
                    var armOp0 = detail.Operands[0];
                    if (armOp0.Type == Register)
                        switch (armOp0.Register.Id) {
                            case ARM_REG_R0: rt = 0; break;
                            case ARM_REG_R1: rt = 1; break;
                            case ARM_REG_R2: rt = 2; break;
                            case ARM_REG_R3: rt = 3; break;
                            case ARM_REG_R4: rt = 4; break;
                            case ARM_REG_R5: rt = 5; break;
                            case ARM_REG_R6: rt = 6; break;
                            case ARM_REG_R7: rt = 7; break;
                            case ARM_REG_R8: rt = 8; break;
                            case ARM_REG_R9: rt = 9; break;
                            case ARM_REG_R10: rt = 10; break;
                            case ARM_REG_R11: rt = 11; break;
                            case ARM_REG_R12: rt = 12; break;
                            case ARM_REG_R13: rt = 13; break;// ARM_REG_SP
                            case ARM_REG_R14: rt = 14; break; // ARM_REG_LR
                            case ARM_REG_R15: rt = 15; break; // ARM_REG_PC
                        }
                }
                if (rt >= 0) over = true;
            }
            //cs_free(m_pInsn, m_uDisasmCount);
            if (over) {
                fixed (byte* _ = &Thumb[i]) *(uint*)_ = 0x2000U | (uint)(rt << 8) | (uint)RegionCode;
                WriteLine($"INFO:   modify:  {i:08X}  mov r{rt}, #0x{RegionCode:x} ; {Thumb[i]:02X} {Thumb[i + 1]:02X}\n");
                return true;
            }
        }
        return false;
    }

    bool PatchGetLanguageFunctionArm(SFunction function) {
        ArmInstruction insn; ArmInstructionDetail detail;
        for (var i = function.Last - 1; i > function.First; i--) {
            var over = false;
            Disasm = Handle.Disassemble(MemoryMarshal.Cast<uint, byte>(Arm.AsSpan(i)).ToArray(), 4, 0x100000 + i * 4);
            if (Disasm.Length > 0 && (insn = Disasm[0]) != null)
                if (insn.Mnemonic == "ldrb" && (detail = insn.Details) != null && detail.Operands.Length > 0) {
                    var armOp0 = detail.Operands[0];
                    if (armOp0.Type == Register && armOp0.Register.Id == ARM_REG_R0) over = true;
                }
            //cs_free(m_pInsn, m_uDisasmCount);
            if (over) {
                Arm[i] = 0xE3A00000U | (uint)LanguageCode;
                WriteLine($"INFO:   modify:  {i * 4:08X}  mov r0, #0x{LanguageCode:x} ; {Arm[i] & 0xFF:02X} {Arm[i] >> 8 & 0xFF:02X} {Arm[i] >> 16 & 0xFF:02X} {Arm[i] >> 24 & 0xFF:02X}\n");
                return true;
            }
        }
        return false;
    }

    bool PatchGetLanguageFunctionThumb(SFunction function) {
        ArmInstruction insn; ArmInstructionDetail detail;
        var codeSize = 4;
        var codeSizeMax = 4;
        var codeSizeCache = 0;
        for (var i = function.Last - 2; i > function.First; i -= codeSize) {
            var over = false;
            if (codeSizeCache == 0) {
                codeSizeMax = Thumb.Length - i;
                if (codeSizeMax > 4) codeSizeMax = 4;
            }
            else { codeSizeMax = codeSizeCache; codeSizeCache = 0; }
            codeSize = 2;
            Disasm = Handle.Disassemble(Thumb.AsSpan(i).ToArray(), codeSizeMax, 0x100000 + i);
            if ((Disasm.Length == 0 && i > function.First + 2)) { // || cs_insn_group(m_uHandle, m_pInsn, ARM_GRP_THUMB2)) {
                i -= 2;
                codeSizeMax += 2;
                Disasm = Handle.Disassemble(Thumb.AsSpan(i).ToArray(), codeSizeMax, 0x100000 + i);
            }
            if (Disasm.Length == 2 && (insn = Disasm[0]) != null) {
                codeSizeCache = insn.Bytes.Length;
                i += codeSizeCache;
                codeSizeMax -= codeSizeCache;
                Disasm = Handle.Disassemble(Thumb.AsSpan(i).ToArray(), codeSizeMax, 0x100000 + i);
            }
            if (Disasm.Length > 0 && (insn = Disasm[0]) != null) {
                if (codeSizeCache == 0) {
                    codeSize = 4;
                    if (i <= function.Last + 4) codeSize = 2;
                }
                else codeSize = codeSizeCache;
                if (insn.Mnemonic == "ldrb" && (detail = insn.Details) != null && detail.Operands.Length > 0) {
                    var armOp0 = detail.Operands[0];
                    if (armOp0.Type == Register && armOp0.Register.Id == ARM_REG_R0) over = true;
                }
            }
            //cs_free(m_pInsn, m_uDisasmCount);
            if (over) {
                fixed (byte* _ = &Thumb[i]) *(uint*)_ = 0x2000U | (uint)LanguageCode;
                WriteLine($"INFO:   modify:  {i:08X}  mov r0, #0x{LanguageCode:x} ; {Thumb[i]:02X} {Thumb[i + 1]:02X}\n");
                return true;
            }
        }
        return false;
    }
}
