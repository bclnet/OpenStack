//#define DEBUG_SHADERS
using OpenStack.Graphics.Algorithms;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenStack.Graphics.OpenGL
{
    /// <summary>
    /// ShaderLoader
    /// </summary>
    public abstract class ShaderLoader
    {
        const int ShaderSeed = 0x13141516;
        Dictionary<uint, Shader> CachedShaders = [];
        Dictionary<string, List<string>> ShaderDefines = [];

        uint CalculateShaderCacheHash(string name, IDictionary<string, bool> args)
        {
            var b = new StringBuilder(); b.AppendLine(name);
            var parameters = ShaderDefines[name].Intersect(args.Keys);
            foreach (var key in parameters)
            {
                b.AppendLine(key);
                b.AppendLine(args[key] ? "t" : "f");
            }
            return MurmurHash2.Hash(b.ToString(), ShaderSeed);
        }

        protected abstract string GetShaderFileByName(string name);

        protected abstract string GetShaderSource(string name);

        public Shader CreateShader(object path, IDictionary<string, bool> args)
        {
            var name = (string)path;
            var fileName = GetShaderFileByName(name);

            // cache
            if (ShaderDefines.ContainsKey(fileName))
            {
                var shaderCacheHash = CalculateShaderCacheHash(fileName, args);
                if (CachedShaders.TryGetValue(shaderCacheHash, out var c)) return c;
            }

            // build
            var defines = new List<string>();

            // vertex shader
            var vertexShader = GL.CreateShader(ShaderType.VertexShader);
            {
                var shaderSource = GetShaderSource($"{fileName}.vert");
                GL.ShaderSource(vertexShader, PreprocessVertexShader(shaderSource, args));
                // find defines supported from source
                defines.AddRange(FindDefines(shaderSource));
            }
            GL.CompileShader(vertexShader);
            var shaderStatus = GL.GetShaderi(vertexShader, ShaderParameterName.CompileStatus);
            if (shaderStatus != 1)
            {
                GL.GetShaderInfoLog(vertexShader, out var vsInfo);
                throw new Exception($"Error setting up Vertex Shader \"{name}\": {vsInfo}");
            }

            // fragment shader
            var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            {
                var shaderSource = GetShaderSource($"{fileName}.frag");
                GL.ShaderSource(fragmentShader, UpdateDefines(shaderSource, args));
                // find render modes supported from source, take union to avoid duplicates
                defines = defines.Union(FindDefines(shaderSource)).ToList();
            }
            GL.CompileShader(fragmentShader);
            shaderStatus = GL.GetShaderi(fragmentShader, ShaderParameterName.CompileStatus);
            if (shaderStatus != 1)
            {
                GL.GetShaderInfoLog(fragmentShader, out var fsInfo);
                throw new Exception($"Error setting up Fragment Shader \"{name}\": {fsInfo}");
            }

            // render modes
            const string RenderMode = "renderMode_";
            var renderModes = defines.Where(k => k.StartsWith(RenderMode)).Select(k => k[RenderMode.Length..]).ToList();

            // build shader
            var shader = new Shader(GL.GetUniformLocation, GL.GetAttribLocation)
            {
                Name = name,
                Parameters = args,
                Program = GL.CreateProgram(),
                RenderModes = renderModes,
            };
            GL.AttachShader(shader.Program, vertexShader);
            GL.AttachShader(shader.Program, fragmentShader);
            GL.LinkProgram(shader.Program);
            GL.ValidateProgram(shader.Program);
            var linkStatus = GL.GetProgrami(shader.Program, ProgramProperty.LinkStatus);
            if (linkStatus != 1)
            {
                GL.GetProgramInfoLog(shader.Program, out var linkInfo);
                throw new Exception($"Error linking shaders: {linkInfo} (link status = {linkStatus})");
            }
            GL.DetachShader(shader.Program, vertexShader);
            GL.DeleteShader(vertexShader);
            GL.DetachShader(shader.Program, fragmentShader);
            GL.DeleteShader(fragmentShader);

#if !DEBUG_SHADERS || !DEBUG
            // cache shader
            ShaderDefines[fileName] = defines;
            var newShaderCacheHash = CalculateShaderCacheHash(fileName, args);
            CachedShaders[newShaderCacheHash] = shader;
            Console.WriteLine($"Shader {newShaderCacheHash} ({name}) ({string.Join(", ", args.Keys)}) compiled and linked succesfully");
#endif
            return shader;
        }

        public Shader CreatePlaneShader(object path, IDictionary<string, bool> args)
        {
            var name = (string)path;
            var shaderFileName = GetShaderFileByName(name);

            // vertex shader
            var vertexShader = GL.CreateShader(ShaderType.VertexShader);
            {
                var shaderSource = GetShaderSource($"plane.vert");
                GL.ShaderSource(vertexShader, shaderSource);
            }
            GL.CompileShader(vertexShader);
            var shaderStatus = GL.GetShaderi(vertexShader, ShaderParameterName.CompileStatus);
            if (shaderStatus != 1)
            {
                GL.GetShaderInfoLog(vertexShader, out var vsInfo);
                throw new Exception($"Error setting up Vertex Shader \"{name}\": {vsInfo}");
            }

            // fragment shader
            var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            {
                var shaderSource = GetShaderSource($"{shaderFileName}.frag");
                GL.ShaderSource(fragmentShader, UpdateDefines(shaderSource, args));
            }
            GL.CompileShader(fragmentShader);
            shaderStatus = GL.GetShaderi(fragmentShader, ShaderParameterName.CompileStatus);
            if (shaderStatus != 1)
            {
                GL.GetShaderInfoLog(fragmentShader, out var fsInfo);
                throw new Exception($"Error setting up Fragment Shader \"{name}\": {fsInfo}");
            }

            // build shader
            var shader = new Shader(GL.GetUniformLocation, GL.GetAttribLocation)
            {
                Name = name,
                Program = GL.CreateProgram(),
            };
            GL.AttachShader(shader.Program, vertexShader);
            GL.AttachShader(shader.Program, fragmentShader);
            GL.LinkProgram(shader.Program);
            GL.ValidateProgram(shader.Program);
            var linkStatus = GL.GetProgrami(shader.Program, ProgramProperty.LinkStatus);
            if (linkStatus != 1)
            {
                GL.GetProgramInfoLog(shader.Program, out var linkInfo);
                throw new Exception($"Error linking shaders: {linkInfo} (link status = {linkStatus}");
            }
            GL.DetachShader(shader.Program, vertexShader);
            GL.DeleteShader(vertexShader);
            GL.DetachShader(shader.Program, fragmentShader);
            GL.DeleteShader(fragmentShader);
            return shader;
        }

        // Preprocess a vertex shader's source to include the #version plus #defines for parameters
        string PreprocessVertexShader(string source, IDictionary<string, bool> args)
            => ResolveIncludes(UpdateDefines(source, args));

        // Update default defines with possible overrides from the model
        static string UpdateDefines(string source, IDictionary<string, bool> args)
        {
            // find all #define param_(paramName) (paramValue) using regex
            var defines = Regex.Matches(source, @"#define param_(\S*?) (\S*?)\s*?\n");
            foreach (Match define in defines)
                // check if this parameter is in the arguments
                if (args.TryGetValue(define.Groups[1].Value, out var value))
                {
                    // overwrite default value
                    var index = define.Groups[2].Index;
                    var length = define.Groups[2].Length;
                    source = source.Remove(index, Math.Min(length, source.Length - index)).Insert(index, value ? "1" : "0");
                }
            return source;
        }

        // Remove any #includes from the shader and replace with the included code
        string ResolveIncludes(string source)
        {
            var includes = Regex.Matches(source, @"#include ""([^""]*?)"";?\s*\n");
            foreach (Match define in includes)
            {
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

        static List<string> FindDefines(string source)
        {
            var defines = Regex.Matches(source, @"#define param_(\S+)");
            return defines.Cast<Match>().Select(_ => _.Groups[1].Value).ToList();
        }
    }

    /// <summary>
    /// ShaderDebugLoader
    /// </summary>
    public class ShaderDebugLoader : ShaderLoader
    {
        const string ShaderDirectory = "OpenStack.Graphics.OpenGL5.Shaders";

        // Map shader names to shader files
        protected override string GetShaderFileByName(string name)
        {
            switch (name)
            {
                case "plane": return "plane";
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

        protected override string GetShaderSource(string name)
        {
#if DEBUG_SHADERS && DEBUG
            using (var stream = File.Open(GetShaderDiskPath(name), FileMode.Open))
#else
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream($"{ShaderDirectory}.{name}"))
#endif
            using (var reader = new StreamReader(stream)) return reader.ReadToEnd();
        }

#if DEBUG_SHADERS && DEBUG
        // Reload shaders at runtime
        static string GetShaderDiskPath(string name) => Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName), "../../../../", ShaderDirectory.Replace(".", "/"), name);
#endif
    }
}