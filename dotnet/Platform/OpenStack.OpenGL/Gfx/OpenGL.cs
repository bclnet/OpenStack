﻿//#define DEBUG_SHADERS
using OpenStack.Algorithms;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static OpenStack.Debug;

namespace OpenStack.Gfx.OpenGL;

#region Extensions

/// <summary>
/// OpenGLExtensions
/// </summary>
public static class OpenGLExtensions {
    public static OpenTK.Vector3 ToOpenTK(this Vector3 vec) => new(vec.X, vec.Y, vec.Z);
    public static OpenTK.Vector4 ToOpenTK(this Vector4 vec) => new(vec.X, vec.Y, vec.Z, vec.W);
    public static OpenTK.Matrix4 ToOpenTK(this Matrix4x4 m) => new(m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24, m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44);
}

// RenderPrimitiveType
public enum RenderPrimitiveType {
    RENDER_PRIM_POINTS = 0x0,
    RENDER_PRIM_LINES = 0x1,
    RENDER_PRIM_LINES_WITH_ADJACENCY = 0x2,
    RENDER_PRIM_LINE_STRIP = 0x3,
    RENDER_PRIM_LINE_STRIP_WITH_ADJACENCY = 0x4,
    RENDER_PRIM_TRIANGLES = 0x5,
    RENDER_PRIM_TRIANGLES_WITH_ADJACENCY = 0x6,
    RENDER_PRIM_TRIANGLE_STRIP = 0x7,
    RENDER_PRIM_TRIANGLE_STRIP_WITH_ADJACENCY = 0x8,
    RENDER_PRIM_INSTANCED_QUADS = 0x9,
    RENDER_PRIM_HETEROGENOUS = 0xA,
    RENDER_PRIM_1_CONTROL_POINT_PATCHLIST = 0xB,
    RENDER_PRIM_2_CONTROL_POINT_PATCHLIST = 0xC,
    RENDER_PRIM_3_CONTROL_POINT_PATCHLIST = 0xD,
    RENDER_PRIM_4_CONTROL_POINT_PATCHLIST = 0xE,
    RENDER_PRIM_5_CONTROL_POINT_PATCHLIST = 0xF,
    RENDER_PRIM_6_CONTROL_POINT_PATCHLIST = 0x10,
    RENDER_PRIM_7_CONTROL_POINT_PATCHLIST = 0x11,
    RENDER_PRIM_8_CONTROL_POINT_PATCHLIST = 0x12,
    RENDER_PRIM_9_CONTROL_POINT_PATCHLIST = 0x13,
    RENDER_PRIM_10_CONTROL_POINT_PATCHLIST = 0x14,
    RENDER_PRIM_11_CONTROL_POINT_PATCHLIST = 0x15,
    RENDER_PRIM_12_CONTROL_POINT_PATCHLIST = 0x16,
    RENDER_PRIM_13_CONTROL_POINT_PATCHLIST = 0x17,
    RENDER_PRIM_14_CONTROL_POINT_PATCHLIST = 0x18,
    RENDER_PRIM_15_CONTROL_POINT_PATCHLIST = 0x19,
    RENDER_PRIM_16_CONTROL_POINT_PATCHLIST = 0x1A,
    RENDER_PRIM_17_CONTROL_POINT_PATCHLIST = 0x1B,
    RENDER_PRIM_18_CONTROL_POINT_PATCHLIST = 0x1C,
    RENDER_PRIM_19_CONTROL_POINT_PATCHLIST = 0x1D,
    RENDER_PRIM_20_CONTROL_POINT_PATCHLIST = 0x1E,
    RENDER_PRIM_21_CONTROL_POINT_PATCHLIST = 0x1F,
    RENDER_PRIM_22_CONTROL_POINT_PATCHLIST = 0x20,
    RENDER_PRIM_23_CONTROL_POINT_PATCHLIST = 0x21,
    RENDER_PRIM_24_CONTROL_POINT_PATCHLIST = 0x22,
    RENDER_PRIM_25_CONTROL_POINT_PATCHLIST = 0x23,
    RENDER_PRIM_26_CONTROL_POINT_PATCHLIST = 0x24,
    RENDER_PRIM_27_CONTROL_POINT_PATCHLIST = 0x25,
    RENDER_PRIM_28_CONTROL_POINT_PATCHLIST = 0x26,
    RENDER_PRIM_29_CONTROL_POINT_PATCHLIST = 0x27,
    RENDER_PRIM_30_CONTROL_POINT_PATCHLIST = 0x28,
    RENDER_PRIM_31_CONTROL_POINT_PATCHLIST = 0x29,
    RENDER_PRIM_32_CONTROL_POINT_PATCHLIST = 0x2A,
}

#endregion

#region Shader

/// <summary>
/// ShaderLoader
/// </summary>
public abstract class ShaderLoader {
    const int ShaderSeed = 0x13141516;

    readonly Dictionary<uint, Shader> CachedShaders = [];
    readonly Dictionary<string, List<string>> ShaderDefines = [];

    uint CalculateShaderCacheHash(string name, IDictionary<string, bool> args) {
        var b = new StringBuilder(); b.AppendLine(name);
        var parameters = ShaderDefines[name].Intersect(args.Keys);
        foreach (var key in parameters) {
            b.AppendLine(key);
            b.AppendLine(args[key] ? "t" : "f");
        }
        return MurmurHash2.Hash(b.ToString(), ShaderSeed);
    }

    protected abstract string GetShaderFileByName(string name);

    protected abstract string GetShaderSource(string name);

    public Shader CreateShader(object path, IDictionary<string, bool> args) {
        var name = (string)path;
        var cache = !name.StartsWith("#");
        var shaderFileName = GetShaderFileByName(name);

        // cache
        if (cache && ShaderDefines.ContainsKey(shaderFileName)) {
            var shaderCacheHash = CalculateShaderCacheHash(shaderFileName, args);
            if (CachedShaders.TryGetValue(shaderCacheHash, out var c)) return c;
        }

        // defines
        List<string> defines = [];

        // vertex shader
        var vertexShader = GL.CreateShader(ShaderType.VertexShader);
        {
            var shaderSource = GetShaderSource($"{shaderFileName}.vert");
            GL.ShaderSource(vertexShader, PreprocessVertexShader(shaderSource, args));
            // defines: find defines supported from source
            defines.AddRange(FindDefines(shaderSource));
        }
        GL.CompileShader(vertexShader);
        GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out var shaderStatus);
        if (shaderStatus != 1) {
            GL.GetShaderInfoLog(vertexShader, out var vsInfo);
            throw new Exception($"Error setting up Vertex Shader \"{name}\": {vsInfo}");
        }

        // fragment shader
        var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        {
            var shaderSource = GetShaderSource($"{shaderFileName}.frag");
            GL.ShaderSource(fragmentShader, UpdateDefines(shaderSource, args));
            // defines: find render modes supported from source, take union to avoid duplicates
            defines = defines.Union(FindDefines(shaderSource)).ToList();
        }
        GL.CompileShader(fragmentShader);
        GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out shaderStatus);
        if (shaderStatus != 1) {
            GL.GetShaderInfoLog(fragmentShader, out var fsInfo);
            throw new Exception($"Error setting up Fragment Shader \"{name}\": {fsInfo}");
        }

        // defines: find render modes
        const string RenderMode = "renderMode_";
        var renderModes = defines.Where(k => k.StartsWith(RenderMode)).Select(k => k[RenderMode.Length..]).ToList();

        // build shader
        var shader = new Shader(GL.GetUniformLocation, GL.GetAttribLocation) {
            Name = name,
            Parameters = args,
            Program = GL.CreateProgram(),
            RenderModes = renderModes,
        };
        GL.AttachShader(shader.Program, vertexShader);
        GL.AttachShader(shader.Program, fragmentShader);
        GL.LinkProgram(shader.Program);
        GL.ValidateProgram(shader.Program);
        GL.GetProgram(shader.Program, GetProgramParameterName.LinkStatus, out var linkStatus);
        GL.DetachShader(shader.Program, vertexShader);
        GL.DeleteShader(vertexShader);
        GL.DetachShader(shader.Program, fragmentShader);
        GL.DeleteShader(fragmentShader);
        if (linkStatus != 1) {
            GL.GetProgramInfoLog(shader.Program, out var linkInfo);
            throw new Exception($"Error linking shaders: {linkInfo} (link status = {linkStatus})");
        }

#if !DEBUG_SHADERS || !DEBUG
        // cache shader
        if (cache) {
            ShaderDefines[shaderFileName] = defines;
            var newShaderCacheHash = CalculateShaderCacheHash(shaderFileName, args);
            CachedShaders[newShaderCacheHash] = shader;
            Log($"Shader {name}({string.Join(", ", args.Keys)}) compiled and linked succesfully");
        }
#endif
        return shader;
    }

    // Preprocess a vertex shader's source to include the #version plus #defines for parameters
    string PreprocessVertexShader(string source, IDictionary<string, bool> args)
        => ResolveIncludes(UpdateDefines(source, args));

    // Update default defines with possible overrides from the model
    static string UpdateDefines(string source, IDictionary<string, bool> args) {
        // find all #define param_(paramName) (paramValue) using regex
        var defines = Regex.Matches(source, @"#define param_(\S*?) (\S*?)\s*?\n");
        foreach (Match define in defines)
            // check if this parameter is in the arguments
            if (args.TryGetValue(define.Groups[1].Value, out var value)) {
                // overwrite default value
                var index = define.Groups[2].Index;
                var length = define.Groups[2].Length;
                source = source.Remove(index, Math.Min(length, source.Length - index)).Insert(index, value ? "1" : "0");
            }
        return source;
    }

    // Remove any #includes from the shader and replace with the included code
    string ResolveIncludes(string source) {
        var includes = Regex.Matches(source, @"#include ""([^""]*?)"";?\s*\n");
        foreach (Match define in includes) {
            // read included code
            var includedCode = GetShaderSource(define.Groups[1].Value);
            // recursively resolve includes in the included code. (Watch out for cyclic dependencies!)
            includedCode = ResolveIncludes(includedCode);
            if (!includedCode.EndsWith("\n")) includedCode += "\n";
            // replace the include with the code
            source = source.Replace(define.Value, includedCode);
        }
        return source;
    }

    static List<string> FindDefines(string source) {
        var defines = Regex.Matches(source, @"#define param_(\S+)");
        return defines.Cast<Match>().Select(_ => _.Groups[1].Value).ToList();
    }
}

/// <summary>
/// ShaderDebugLoader
/// </summary>
public class ShaderDebugLoader : ShaderLoader {
    const string ShaderDirectory = "OpenStack.OpenGL.Gfx.Shaders";

    // Map shader names to shader files
    protected override string GetShaderFileByName(string name) {
        switch (name) {
            case "plane": return "plane";
            case "testtri": return "testtri";
            case "vrf.error": return "error";
            case "vrf.grid": return "debug_grid";
            case "vrf.picking": return "picking";
            case "vrf.particle.sprite": return "particle_sprite";
            case "vrf.particle.trail": return "particle_trail";
            case "tools_sprite.vfx": return "sprite";
            case "vr_unlit.vfx": return "vr_unlit";
            case "vr_black_unlit.vfx": return "vr_black_unlit";
            case "water_dota.vfx": return "water";
            case "hero.vfx":
            case "hero_underlords.vfx": return "dota_hero";
            case "multiblend.vfx": return "multiblend";
            default:
                if (name.StartsWith("vr_")) return "vr_standard";
                // Console.WriteLine($"Unknown shader {name}, defaulting to simple.");
                return "simple";
        }
    }

    protected override string GetShaderSource(string name) {
#if DEBUG_SHADERS && DEBUG
        var stream = File.Open(GetShaderDiskPath(name), FileMode.Open);
#else
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{ShaderDirectory}.{name}");
#endif
        using var r = new StreamReader(stream); return r.ReadToEnd();
    }

#if DEBUG_SHADERS && DEBUG
    // Reload shaders at runtime
    static string GetShaderDiskPath(string name) => Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName), "../../../../", ShaderDirectory.Replace(".", "/"), name);
#endif
}

#endregion
