using System;
using System.Collections.Generic;
using System.IO;

namespace Shared
{
    
    public class EnvLoader
    {
        public static Dictionary<string, string> LoadEnv(string path)
        {
            var env = new Dictionary<string, string>();
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    return env;
                }

                foreach (var raw in File.ReadAllLines(path))
                {
                    var line = raw?.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#", StringComparison.Ordinal)) continue;

                    var idx = line.IndexOf('=');
                    if (idx <= 0) continue;

                    var key = line.Substring(0, idx).Trim();
                    var val = line.Substring(idx + 1).Trim();

                    if (string.IsNullOrEmpty(key)) continue;

                    // Remove surrounding matching quotes if present
                    if (val.Length >= 2)
                    {
                        if ((val.StartsWith("\"", StringComparison.Ordinal) && val.EndsWith("\"", StringComparison.Ordinal)) ||
                            (val.StartsWith("'", StringComparison.Ordinal) && val.EndsWith("'", StringComparison.Ordinal)))
                        {
                            val = val.Substring(1, val.Length - 2);
                        }
                    }

                    try
                    {
                        env.Add(key, val);
                    }
                    catch
                    {
                        // Ignore individual variable set failures; continue processing others.
                    }
                }
            }
            catch
            {
                // best-effort only; don't fail startup if .env parsing has problems
            }
            return env;
        }
    }
}
