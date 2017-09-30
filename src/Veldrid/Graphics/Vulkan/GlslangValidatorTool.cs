using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Veldrid.Graphics.Vulkan
{
    /// <summary>
    /// Wraps invocations to glslangValidator for compiling shaders.
    /// </summary>
    public static class GlslangValidatorTool
    {
        private static readonly string s_exePath = FindExePath();

        public static bool IsAvailable() => s_exePath != null;

        public static byte[] CompileBytecode(ShaderStages stage, string code, string entryPoint)
        {
            string tempInputFile = Path.GetTempFileName();
            File.WriteAllText(tempInputFile, code);
            string tempOutputFile = Path.GetTempFileName();
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(s_exePath);
                StringBuilder args = new StringBuilder();
                args.Append(tempInputFile);
                args.Append(" -o "); // Output file
                args.Append(tempOutputFile);
                args.Append(" -V "); // "Vulkan semantics"
                args.Append("-S "); // Stage name
                args.Append(GetStageArgName(stage));
                args.Append(" -e "); // Entry point name
                args.Append(entryPoint);
                psi.Arguments = args.ToString();
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                Process p = Process.Start(psi);
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    string error = p.StandardError.ReadToEnd();
                    throw new VeldridException("Error compiling GLSL to SPIR-V bytecode: " + error);
                }

                return File.ReadAllBytes(tempOutputFile);
            }
            finally
            {
                File.Delete(tempInputFile);
                File.Delete(tempOutputFile);
            }
        }

        private static string GetStageArgName(ShaderStages stage)
        {
            switch (stage)
            {
                case ShaderStages.Vertex:
                    return "vert";
                case ShaderStages.TessellationControl:
                    return "tesc";
                case ShaderStages.TessellationEvaluation:
                    return "tese";
                case ShaderStages.Geometry:
                    return "geom";
                case ShaderStages.Fragment:
                    return "frag";
                default: throw Illegal.Value<ShaderStages>();
            }
        }

        private static string FindExePath()
        {
            string sdkDir = Environment.GetEnvironmentVariable("VULKAN_SDK");
            if (sdkDir != null)
            {
                string extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;
                string path = Path.Combine(sdkDir, "bin", "glslangValidator" + extension);
                if (File.Exists(path))
                {
                    return path;
                }
            }
            else // Try to run it anyways?
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "glslangValidator";
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                try
                {
                    Process.Start(psi);
                    return "glslangValidator";
                }
                catch { }
            }

            return null;
        }
    }
}
