using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace OpenStack;

public static class Profiler {
    public class ProfileData {
        public static ProfileData Empty = new(null, 0d);
        uint LastIndex;
        readonly double[] m_LastTimes = new double[ProfileTimeCount];

        public ProfileData(string[] context, double time) {
            Context = context;
            LastIndex = 0;
            AddNewHitLength(time);
        }

        public double LastTime => m_LastTimes[LastIndex % ProfileTimeCount];

        public double TimeInContext {
            get {
                var time = 0d;
                for (var i = 0; i < ProfileTimeCount; i++) time += m_LastTimes[i];
                return time;
            }
        }

        public double AverageTime => TimeInContext / ProfileTimeCount;
        public string[] Context;

        public bool MatchesContext(string[] context) {
            if (Context.Length != context.Length) return false;
            for (var i = 0; i < Context.Length; i++)
                if (Context[i] != context[i]) return false;
            return true;
        }

        public void AddNewHitLength(double time) {
            m_LastTimes[LastIndex % ProfileTimeCount] = time;
            LastIndex++;
        }

        public override string ToString() {
            var name = string.Empty;
            for (var i = 0; i < Context.Length; i++) {
                if (name != string.Empty) name += ":";
                name += Context[i];
            }
            return $"{name} - {TimeInContext:0.0}ms";
        }
    }

    readonly struct ContextAndTick(string name, long tick) {
        public readonly string Name = name;
        public readonly long Tick = tick;
        public override string ToString() => $"{Name} [{Tick}]";
    }

    public const int ProfileTimeCount = 60;
    static readonly List<ContextAndTick> Context = [];
    static readonly List<Tuple<string[], double>> ThisFrameData = [];
    static readonly List<ProfileData> AllFrameData = [];
    static readonly ProfileData TotalTimeData = new(null, 0d);
    static readonly Stopwatch Timer = Stopwatch.StartNew();
    static long BeginFrameTicks;

    public static double LastFrameTimeMS { get; private set; }

    public static double TrackedTime => TotalTimeData.TimeInContext;

    public static bool Enabled = false;

    public static void BeginFrame() {
        if (!Enabled) return;
        if (ThisFrameData.Count > 0) {
            foreach (Tuple<string[], double> t in ThisFrameData) {
                var added = false;
                foreach (var t1 in AllFrameData) if (t1.MatchesContext(t.Item1)) { t1.AddNewHitLength(t.Item2); added = true; break; }
                if (!added) AllFrameData.Add(new ProfileData(t.Item1, t.Item2));
            }
            ThisFrameData.Clear();
        }
        BeginFrameTicks = Timer.ElapsedTicks;
    }

    public static void EndFrame() {
        if (!Enabled) return;
        LastFrameTimeMS = (Timer.ElapsedTicks - BeginFrameTicks) * 1000d / Stopwatch.Frequency;
        TotalTimeData.AddNewHitLength(LastFrameTimeMS);
    }

    public static void EnterContext(string context_name) {
        if (!Enabled) return;
        Context.Add(new ContextAndTick(context_name, Timer.ElapsedTicks));
    }

    public static void ExitContext(string context_name) {
        if (!Enabled) return;
        if (Context[^1].Name != context_name) Log.Error("ExitProfiledContext: context_name does not match current context.");
        var context = new string[Context.Count];
        for (var i = 0; i < Context.Count; i++) context[i] = Context[i].Name;
        var ms = (Timer.ElapsedTicks - Context[Context.Count - 1].Tick) * 1000d / Stopwatch.Frequency;
        ThisFrameData.Add(new Tuple<string[], double>(context, ms));
        Context.RemoveAt(Context.Count - 1);
    }

    public static bool InContext(string context_name) {
        if (!Enabled || Context.Count == 0) return false;
        return Context[^1].Name == context_name;
    }

    public static ProfileData GetContext(string context_name) {
        if (!Enabled) return ProfileData.Empty;
        for (var i = 0; i < AllFrameData.Count; i++)
            if (AllFrameData[i].Context[^1] == context_name) return AllFrameData[i];
        return ProfileData.Empty;
    }


}