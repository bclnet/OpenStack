using System.Runtime.InteropServices;

namespace OpenStack.Rom.Nintendo._3ds;

internal unsafe class ExtHeader {
    [StructLayout(LayoutKind.Sequential)]
    public struct SystemInfoFlagStruct {
        public fixed byte Reserved[5];
        public byte Flag;
        public ushort RemasterVersion;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CodeSegmentInfo {
        public uint Address;
        public uint NumMaxPages;
        public uint CodeSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CodeSetInfo {
        public ulong Name;
        public SystemInfoFlagStruct Flags;
        public CodeSegmentInfo TextSectionInfo;
        public uint StackSize;
        public CodeSegmentInfo ReadOnlySectionInfo;
        public fixed byte Reserved1[4];
        public CodeSegmentInfo DataSectionInfo;
        public uint BssSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CoreInfo {
        public CodeSetInfo CodeSetInfo;
        public fixed ulong DepedencyList[48];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SystemInfoStruct {
        public ulong SaveDataSize;
        public ulong JumpId;
        public fixed byte Reserved2[48];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SystemControlInfo {
        public CoreInfo CoreInfo;
        public SystemInfoStruct SystemInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ARM11SystemLocalCapabilityFlags {
        public uint CoreVersion;
        public fixed byte Reserved[2];
        public byte mixed;
        public readonly byte IdealProcessor => (byte)(mixed & 0x2);
        public readonly byte AffinityMask => (byte)(mixed & 0x2);
        public readonly byte SystemMode => (byte)(mixed & 0x4);
        public byte MainThreadPriority;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StorageInfoFlags {
        public fixed byte StorageAccessInfo[7];
        public byte OtherAttributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StorageInfoStruct {
        public ulong ExtSaveDataId;
        public ulong SystemSaveDataId;
        public ulong StorageAccessableUniqueIds;
        public StorageInfoFlags InfoFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ARM11SystemLocalCapabilities {
        public ulong ProgramId;
        public ARM11SystemLocalCapabilityFlags Flags;
        public byte MaxCpu;
        public byte Reserved0;
        public fixed byte ResourceLimits[15 * 2];
        public StorageInfoStruct StorageInfo;
        public fixed ulong ServiceAccessControl[32];
        public fixed byte Reserved[31];
        public byte ResourceLimitCategory;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ARM11KernelCapabilities {
        public fixed uint Descriptor[28];
        public fixed byte Reserved[16];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AccessControlInfo {
        public ARM11SystemLocalCapabilities m_ARM11SystemLocalCapabilities;
        public ARM11KernelCapabilities m_ARM11KernelCapabilities;
        public fixed byte m_ARM9AccessControlInfo[16];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NcchExtendedHeader {
        public SystemControlInfo m_SystemControlInfo;
        public AccessControlInfo m_AccessControlInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NcchAccessControlExtended {
        public fixed byte m_RsaSignature[256];
        public fixed byte m_NcchHeaderPublicKey[256];
        public AccessControlInfo m_AccessControlInfoDescriptor;
    }
}