using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace YAFC.Model
{
    public class Project : ModelObject
    {
        public uint projectVersion => undo.version;
        public string attachedFileName { get; private set; }
        public bool justCreated { get; private set; } = true;
        public ProjectSettings settings { get; }
        public List<ProjectPage> pages { get; } = new List<ProjectPage>();
        public new UndoSystem undo => base.undo;
        private uint lastSavedState;
        public Project() : base(new UndoSystem())
        {
            settings = new ProjectSettings(this);
        }

        public event Action metaInfoChanged;

        protected internal override void ThisChanged()
        {
            base.ThisChanged();
            metaInfoChanged?.Invoke();
        }

        public static Project ReadFromFile(string path)
        {
            Project proj;
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                var reader = new Utf8JsonReader(File.ReadAllBytes(path));
                reader.Read();
                proj = SerializationMap<Project>.DeserializeFromJson(null, ref reader);
                proj.justCreated = false;
                if (!reader.IsFinalBlock)
                    throw new JsonException("Json was not consumed to the end!");
            } else proj = new Project();
            proj.attachedFileName = path;
            return proj;
        }

        public void Save(string fileName)
        {
            if (lastSavedState == projectVersion && fileName == attachedFileName)
                return;
            using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                using (var writer = new Utf8JsonWriter(fs, JsonUtils.DefaultWriterOptions))
                {
                    SerializationMap<Project>.SerializeToJson(this, writer);
                }
            }
            attachedFileName = fileName;
            lastSavedState = projectVersion;
        }
    }

    public class ProjectSettings : ModelObject
    {
        public readonly Project project;
        public List<FactorioObject> milestones { get; } = new List<FactorioObject>();
        public ulong milestonesUnlockedMask { get; set; }
        public ProjectSettings(Project project) : base(project)
        {
            this.project = project;
        }
    }
}