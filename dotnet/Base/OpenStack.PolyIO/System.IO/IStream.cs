namespace System.IO;

/// <summary>
/// IStream
/// </summary>
public interface IStream {
    Stream GetStream();
}

/// <summary>
/// IWriteToStream
/// </summary>
public interface IWriteToStream {
    void WriteToStream(Stream stream);
}
