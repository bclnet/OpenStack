using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OpenStack.ExtServices;

public static class FFmpegService {
    const string BIN_HOST_URI_LINUX_X64 = "https://github.com/TASEmulators/ffmpeg-binaries/raw/master/ffmpeg-4.4.1-static-linux-x64.7z";
    const string BIN_HOST_URI_WIN_X64 = "https://github.com/TASEmulators/ffmpeg-binaries/raw/master/ffmpeg-4.4.1-static-windows-x64.7z";
    //public static readonly string Url = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? BIN_HOST_URI_LINUX_X64 : BIN_HOST_URI_WIN_X64;
    public static string FFmpegPath => Path.Combine("C:", "bin", RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "ffmpeg" : "ffmpeg.exe");

    public class AudioQueryResult {
        public bool IsAudio;
    }

    public struct RunResult {
        public string Text;
        public int ExitCode;
    }

    static string[] Escape(IEnumerable<string> args) => [.. args.Select(s => s.Contains(' ') ? $"\"{s}\"" : s)];

    //note: accepts . or : in the stream stream/substream separator in the stream ID format, since that changed at some point in FFMPEG history
    //if someone has a better idea how to make the determination of whether an audio stream is available, I'm all ears
    static readonly Regex Rx_HasAudio = new(@"Stream \#(\d*(\.|\:)\d*)\: Audio", RegexOptions.Compiled);

    public static AudioQueryResult QueryAudio(string path) {
        var stdout = Run("-i", path).Text;
        return new AudioQueryResult {
            IsAudio = Rx_HasAudio.Matches(stdout).Count > 0
        };
    }

    /// <summary>
    /// queries whether this service is available. if ffmpeg is broken or missing, then you can handle it gracefully
    /// </summary>
    public static bool QueryServiceAvailable(out string version) {
        version = null;
        try {
            var text = Run("-version").Text;
            var idx = text.IndexOf("ffmpeg version");
            if (idx == -1) return false;
            version = text[idx..text.IndexOf(" ", idx)];
            return true;
        }
        catch { return false; }
    }

    public static RunResult Run(params string[] args) {
        args = Escape(args);
        var b = new StringBuilder();
        for (var i = 0; i < args.Length; i++) {
            b.Append(args[i]);
            if (i != args.Length - 1) b.Append(' ');
        }
        var proc = new Process {
            StartInfo = new ProcessStartInfo(FFmpegPath, b.ToString()) {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };
        b.Length = 0;
        var m = new Mutex();
        var outputCloseEvent = new TaskCompletionSource<bool>();
        var errorCloseEvent = new TaskCompletionSource<bool>();
        proc.OutputDataReceived += (s, e) => {
            if (e.Data == null) outputCloseEvent.SetResult(true);
            else { m.WaitOne(); b.Append(e.Data); m.ReleaseMutex(); }
        };
        proc.ErrorDataReceived += (s, e) => {
            if (e.Data == null) errorCloseEvent.SetResult(true);
            else { m.WaitOne(); b.Append(e.Data); m.ReleaseMutex(); }
        };
        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        proc.WaitForExit();
        m.WaitOne();
        var text = b.ToString();
        m.ReleaseMutex();
        return new RunResult { ExitCode = proc.ExitCode, Text = text };
    }

    public static byte[] DecodeAudio(string path) {
        var tempfile = Path.GetTempFileName();
        try {
            var res = Run("-i", path, "-xerror", "-f", "wav", "-ar", "44100", "-ac", "2", "-acodec", "pcm_s16le", "-y", tempfile);
            if (res.ExitCode != 0) throw new InvalidOperationException($"Failure running ffmpeg for audio decode. here was its output:\r\n{res.Text}");
            var ret = File.ReadAllBytes(tempfile);
            if (ret.Length == 0) throw new InvalidOperationException($"Failure running ffmpeg for audio decode. here was its output:\r\n{res.Text}");
            return ret;
        }
        finally { File.Delete(tempfile); }
    }
}
