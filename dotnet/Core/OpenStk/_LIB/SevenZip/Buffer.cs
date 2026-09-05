namespace SevenZip.Buffer;

#region InBuffer

public class InBuffer {
    byte[] m_Buffer;
    uint m_Pos;
    uint m_Limit;
    uint m_BufferSize;
    System.IO.Stream m_Stream;
    bool m_StreamWasExhausted;
    ulong m_ProcessedSize;

    public InBuffer(uint bufferSize) {
        m_Buffer = new byte[bufferSize];
        m_BufferSize = bufferSize;
    }

    public void Init(System.IO.Stream stream) {
        m_Stream = stream;
        m_ProcessedSize = 0;
        m_Limit = 0;
        m_Pos = 0;
        m_StreamWasExhausted = false;
    }

    public bool ReadBlock() {
        if (m_StreamWasExhausted)
            return false;
        m_ProcessedSize += m_Pos;
        int aNumProcessedBytes = m_Stream.Read(m_Buffer, 0, (int)m_BufferSize);
        m_Pos = 0;
        m_Limit = (uint)aNumProcessedBytes;
        m_StreamWasExhausted = (aNumProcessedBytes == 0);
        return (!m_StreamWasExhausted);
    }


    public void ReleaseStream() {
        // m_Stream.Close(); 
        m_Stream = null;
    }

    public bool ReadByte(byte b) // check it
    {
        if (m_Pos >= m_Limit)
            if (!ReadBlock())
                return false;
        b = m_Buffer[m_Pos++];
        return true;
    }

    public byte ReadByte() {
        // return (byte)m_Stream.ReadByte();
        if (m_Pos >= m_Limit)
            if (!ReadBlock())
                return 0xFF;
        return m_Buffer[m_Pos++];
    }

    public ulong GetProcessedSize() {
        return m_ProcessedSize + m_Pos;
    }
}

#endregion

#region OutBuffer

public class OutBuffer {
    byte[] m_Buffer;
    uint m_Pos;
    uint m_BufferSize;
    System.IO.Stream m_Stream;
    ulong m_ProcessedSize;

    public OutBuffer(uint bufferSize) {
        m_Buffer = new byte[bufferSize];
        m_BufferSize = bufferSize;
    }

    public void SetStream(System.IO.Stream stream) { m_Stream = stream; }
    public void FlushStream() { m_Stream.Flush(); }
    public void CloseStream() { m_Stream.Close(); }
    public void ReleaseStream() { m_Stream = null; }

    public void Init() {
        m_ProcessedSize = 0;
        m_Pos = 0;
    }

    public void WriteByte(byte b) {
        m_Buffer[m_Pos++] = b;
        if (m_Pos >= m_BufferSize)
            FlushData();
    }

    public void FlushData() {
        if (m_Pos == 0)
            return;
        m_Stream.Write(m_Buffer, 0, (int)m_Pos);
        m_Pos = 0;
    }

    public ulong GetProcessedSize() { return m_ProcessedSize + m_Pos; }
}

#endregion