using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using YAFC.Model;

namespace YAFC {
    public partial class Preferences {
        public static readonly Preferences Instance;
        public static readonly string appDataFolder;
        private static readonly string fileName;

        public static readonly string autosaveFilename;

        [JsonSourceGenerationOptions(WriteIndented = true, IgnoreReadOnlyProperties = true)]
        [JsonSerializable(typeof(Preferences))]
        [JsonSerializable(typeof(ProjectDefinition))]
        public partial class PreferenceJsonContext : JsonSerializerContext {

        }

        static Preferences() {
            appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                appDataFolder = Path.Combine(appDataFolder, "YAFC");
            }

            if (!string.IsNullOrEmpty(appDataFolder) && !Directory.Exists(appDataFolder)) {
                _ = Directory.CreateDirectory(appDataFolder);
            }

            autosaveFilename = Path.Combine(appDataFolder, "autosave.yafc");
            fileName = Path.Combine(appDataFolder, "yafc.config");
            if (File.Exists(fileName)) {
                try {
                    var options = new JsonSerializerOptions(JsonUtils.DefaultOptions) {
                        TypeInfoResolver = PreferenceJsonContext.Default
                    };
                    // We can ignore IL2026 because we set the TypeInfoResolver above.
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
                    Instance = JsonSerializer.Deserialize(File.ReadAllBytes(fileName), typeof(Preferences), options) as Preferences;
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
                    return;
                }
                catch (Exception ex) {
                    Console.Error.WriteException(ex);
                }
            }
            Instance = new Preferences();
        }

        public void Save() {
            var options = new JsonSerializerOptions(JsonUtils.DefaultOptions) {
                TypeInfoResolver = PreferenceJsonContext.Default
            };
            // We can ignore IL2026 because we set the TypeInfoResolver above.
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
            byte[] data = JsonSerializer.SerializeToUtf8Bytes(this, typeof(Preferences), options);
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
            File.WriteAllBytes(fileName, data);
        }
        public ProjectDefinition[] recentProjects { get; set; } = Array.Empty<ProjectDefinition>();
        public bool darkMode { get; set; }
        public string language { get; set; } = "en";
        public string overrideFont { get; set; }

        public void AddProject(string path, string dataPath, string modsPath, bool expensiveRecipes) {
            recentProjects = recentProjects.Where(x => string.Compare(path, x.path, StringComparison.InvariantCultureIgnoreCase) != 0)
                .Prepend(new ProjectDefinition { path = path, modsPath = modsPath, dataPath = dataPath, expensive = expensiveRecipes }).ToArray();
            Save();
        }
    }

    public partial class ProjectDefinition {
        public string path { get; set; }
        public string dataPath { get; set; }
        public string modsPath { get; set; }
        public bool expensive { get; set; }
    }
}
