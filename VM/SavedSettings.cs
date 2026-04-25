using Newtonsoft.Json;
using StoryManager.VM;
using StoryManager.VM.Helpers;
using StoryManager.VM.Literotica;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;

namespace StoryManager
{
    [DataContract(Name = "StoryManagerSettings", Namespace = "")]
    public class SavedSettings
    {
        [DataMember(Name = "OpenedCount")]
        public int OpenedCount { get; set; }

        [DataMember(Name = "Theme")]
        public Theme Theme { get; set; }

        [DataMember(Name = "WindowLeftPosition")]
        public double? WindowLeftPosition { get; set; }
        [DataMember(Name = "WindowTopPosition")]
        public double? WindowTopPosition { get; set; }

        [JsonIgnore]
        public bool HasPosition => WindowLeftPosition.HasValue && WindowTopPosition.HasValue;

        [DataMember(Name = "WindowWidth")]
        public double? WindowWidth { get; set; }
        [DataMember(Name = "WindowHeight")]
        public double? WindowHeight { get; set; }

        [DataMember(Name = "SidebarWidth")]
        public double? SidebarWidth { get; set; }

        [DataMember(Name = "FontSize")]
        public int? FontSize { get; set; }

        [DataMember(Name = "FontFamily")]
        public string FontFamily { get; set; }

        [DataMember(Name = "ForegroundColor")]
        public string ForegroundColor { get; set; }
        [DataMember(Name = "BackgroundColor")]
        public string BackgroundColor { get; set; }
        [DataMember(Name = "HighlightColor")]
        public string HighlightColor { get; set; }

        //  StoriesBaseFolder used to live in settings.json so the program could find the stories folder. As of 1.0.11.0
        //  the relationship is inverted: the bootstrap pointer (stories-folder.txt) names the stories folder, and
        //  settings.json lives inside it. We still deserialize this field once during migration so the legacy file's
        //  StoriesBaseFolder can seed the new stories folder, but ShouldSerializeStoriesBaseFolder returns false so
        //  the value is dropped from settings.json the next time we save.
        [DataMember(Name = "StoriesBaseFolder")]
        public string StoriesBaseFolder { get; set; }
        public bool ShouldSerializeStoriesBaseFolder() => false;

        [DataMember(Name = "RecentSelectedStory")]
        public string RecentSelectedStory { get; set; }

        [DataMember(Name = "HistorySize")]
        public int? HistorySize { get; set; }
        [DataMember(Name = "HistoryList")]
        public List<string> HistoryList { get; set; }

        [DataMember(Name = "GroupAllByAuthor")]
        public bool GroupAllByAuthor { get; set; }
        [DataMember(Name = "GroupFavoritesByAuthor")]
        public bool GroupFavoritesByAuthor { get; set; }

        [DataMember(Name = "SortingMode")]
        public SortBy SortingMode { get; set; }

        [DataMember(Name = "Keywords")]
        public string Keywords { get; set; }

        [DataMember(Name = "AuthorSettings")]
        public List<AuthorSettings> AuthorSettings { get; set; }
        [DataMember(Name = "StorySettings")]
        public List<StorySettings> StorySettings { get; set; }
        //  StoryMetadata used to be cached here as a load-time optimization, but for users with thousands of
        //  stories this caused the settings file to balloon to hundreds of MB and crash the program on startup.
        //  Per-story metadata is already persisted in each story folder's story-metadata.json file, so we no
        //  longer write this field. It is kept (and ignored on serialize) for backward-compatible reads of
        //  older settings files; the field is dropped the next time settings are saved.
        [JsonIgnore]
        public List<SerializableStory> StoryMetadata { get; set; }

        [DataMember(Name = "ShowCategory")]
        public bool ShowCategory { get; set; }
        [DataMember(Name = "ShowDateApproved")]
        public bool ShowDateApproved { get; set; }
        [DataMember(Name = "ShowReadState")]
        public bool ShowReadState { get; set; }
        [DataMember(Name = "ShowOverallRating")]
        public bool ShowOverallRating { get; set; }
        [DataMember(Name = "ShowPageCount")]
        public bool ShowPageCount { get; set; }
        [DataMember(Name = "ShowWordCount")]
        public bool ShowWordCount { get; set; }
        [DataMember(Name = "ShowUserRating")]
        public bool ShowUserRating { get; set; }
        [DataMember(Name = "ShowDateDownloaded")]
        public bool ShowDateDownloaded { get; set; }

        [DataMember(Name = "SaveAfterDownloading")]
        public bool SaveAfterDownloading { get; set; }

        [DataMember(Name = "WarnIfClosingUnsavedStory")]
        public bool WarnIfClosingUnsavedStory { get; set; }

        public SavedSettings()
        {
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            OpenedCount = 1;

            Theme = Theme.DarkMode;

            WindowLeftPosition = null;
            WindowTopPosition = null;
            WindowWidth = null;
            WindowHeight = null;

            SidebarWidth = null;

            FontSize = null;
            FontFamily = null;

            ForegroundColor = null;
            BackgroundColor = null;
            HighlightColor = null;

            StoriesBaseFolder = null;

            RecentSelectedStory = null;

            HistorySize = null;
            HistoryList = new();

            GroupAllByAuthor = true;
            GroupFavoritesByAuthor = true;

            SortingMode = SortBy.Title;

            Keywords = "";

            AuthorSettings = new();
            StorySettings = new();
            StoryMetadata = new();

            ShowCategory = false;
            ShowDateApproved = false;
            ShowReadState = true;
            ShowOverallRating = false;
            ShowPageCount = false;
            ShowWordCount = false;
            ShowUserRating = false;
            ShowDateDownloaded = false;

            SaveAfterDownloading = true;

            WarnIfClosingUnsavedStory = true;
        }

        public int GetFontSize(int DefaultValue) => FontSize ?? DefaultValue;

        public string GetFontFamily(string DefaultValue) => FontFamily ?? DefaultValue;

        public Color GetForegroundColor(Color DefaultValue) => ForegroundColor == null ? DefaultValue : (Color)ColorConverter.ConvertFromString(ForegroundColor);
        public Color GetBackgroundColor(Color DefaultValue) => BackgroundColor == null ? DefaultValue : (Color)ColorConverter.ConvertFromString(BackgroundColor);
        public Color GetHighlightColor(Color DefaultValue) => HighlightColor == null ? DefaultValue : (Color)ColorConverter.ConvertFromString(HighlightColor);

        #region Serialization
#if XML_Settings
        internal const string FileExt = ".xml";
#else
        internal const string FileExt = ".json";
#endif

        internal const int MaxBackups = 5;

        /// <summary>The directory holding the bootstrap pointer. Also the directory legacy versions (&lt;= 1.0.10.0) used for settings.json itself.</summary>
        internal static string BootstrapDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), nameof(StoryManager));

        /// <summary>One-line text file holding the absolute path of the user's stories folder. Lives in <see cref="BootstrapDirectory"/>.
        /// Settings live alongside the stories themselves, so this pointer is what tells the program where to look.</summary>
        internal const string BootstrapPointerFilename = "stories-folder.txt";
        internal static string GetBootstrapPointerPath() => Path.Combine(BootstrapDirectory, BootstrapPointerFilename);

        internal static string GetSettingsFilename() => $"settings{FileExt}";
        internal static string GetBackupFilename(int Number) => $"settings-bak{Number}{FileExt}";
        internal static string GetSettingsPath(string SettingsDirectory) => Path.Combine(SettingsDirectory, GetSettingsFilename());
        internal static string GetBackupPath(string SettingsDirectory, int Number) => Path.Combine(SettingsDirectory, GetBackupFilename(Number));

        /// <summary>Resolves which folder holds <c>settings.json</c>. Reads the bootstrap pointer if present; otherwise migrates from
        /// the legacy %MyDocuments%\StoryManager location (copying settings.json and rotating backups into the resolved stories folder)
        /// and writes the bootstrap pointer for future launches.</summary>
        /// <param name="DefaultStoriesFolder">Folder to use if neither a bootstrap pointer nor a legacy settings.json supplies one.</param>
        internal static string ResolveStoriesFolder(string DefaultStoriesFolder)
        {
            //  1. An existing bootstrap pointer wins, even if the path it points at no longer exists. Returning a missing
            //     path is intentional: it lets MainViewModel surface its "Stories folder not found" warning to the user
            //     (rather than silently switching to a different folder and overwriting whatever's there).
            string PointerPath = GetBootstrapPointerPath();
            if (File.Exists(PointerPath))
            {
                try
                {
                    string Pointed = File.ReadAllText(PointerPath).Trim();
                    if (!string.IsNullOrEmpty(Pointed))
                        return Pointed;
                }
                catch (Exception ex) { Debug.WriteLine($"Failed reading bootstrap pointer at '{PointerPath}':\n{ex}"); }
            }

            //  2. One-time migration. Read the legacy settings.json (if any) just enough to extract StoriesBaseFolder; that
            //     becomes the new stories folder. Then copy the legacy settings.json + backups into it.
            string LegacySettingsPath = Path.Combine(BootstrapDirectory, GetSettingsFilename());
            string ResolvedFolder = DefaultStoriesFolder;
            if (File.Exists(LegacySettingsPath))
            {
                try
                {
                    SavedSettings Legacy = GeneralUtils.DeserializeJson<SavedSettings>(File.ReadAllText(LegacySettingsPath));
                    if (!string.IsNullOrEmpty(Legacy?.StoriesBaseFolder))
                        ResolvedFolder = Legacy.StoriesBaseFolder;
                }
                catch (Exception ex) { Debug.WriteLine($"Failed reading legacy settings to determine stories folder:\n{ex}"); }

                try
                {
                    Directory.CreateDirectory(ResolvedFolder);
                    //  Copy (don't move) so the legacy files remain intact for manual rollback if anything looks wrong.
                    //  CopyIfMissing also avoids clobbering anything the user might already have placed in the new folder.
                    CopyIfMissing(LegacySettingsPath, GetSettingsPath(ResolvedFolder));
                    for (int i = 1; i <= MaxBackups; i++)
                    {
                        string LegacyBackup = Path.Combine(BootstrapDirectory, GetBackupFilename(i));
                        if (File.Exists(LegacyBackup))
                            CopyIfMissing(LegacyBackup, GetBackupPath(ResolvedFolder, i));
                    }
                }
                catch (Exception ex) { Debug.WriteLine($"Failed migrating legacy settings into stories folder:\n{ex}"); }
            }

            //  3. Persist the bootstrap pointer so future launches skip both branches above.
            WriteBootstrapPointer(ResolvedFolder);

            return ResolvedFolder;
        }

        private static void CopyIfMissing(string Source, string Destination)
        {
            if (!File.Exists(Source) || File.Exists(Destination))
                return;
            string DestDir = Path.GetDirectoryName(Destination);
            if (!string.IsNullOrEmpty(DestDir))
                Directory.CreateDirectory(DestDir);
            File.Copy(Source, Destination);
        }

        /// <summary>Persists <paramref name="StoriesFolder"/> to the bootstrap pointer file so future launches read settings.json
        /// from inside it.</summary>
        internal static void WriteBootstrapPointer(string StoriesFolder)
        {
            try
            {
                Directory.CreateDirectory(BootstrapDirectory);
                File.WriteAllText(GetBootstrapPointerPath(), StoriesFolder);
            }
            catch (Exception ex) { Debug.WriteLine($"Failed writing bootstrap pointer:\n{ex}"); }
        }

        internal bool Save(string FilePath)
        {
#if XML_Settings
            XMLSerializer.Serialize(this, FilePath, out bool Success, out Exception Error);
            if (!Success)
                Debug.WriteLine(Error.ToString());
            return Success;
#else
            //  Atomic write: serialize and write to a sibling .tmp file, then atomically replace the destination.
            //  Without this, a process kill mid-WriteAllText leaves settings.json truncated and unrecoverable.
            string TempPath = FilePath + ".tmp";
            try
            {
                string Dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(Dir))
                    Directory.CreateDirectory(Dir);
                string Json = GeneralUtils.SerializeJson(this, true);
                File.WriteAllText(TempPath, Json);
                File.Move(TempPath, FilePath, overwrite: true);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                try { if (File.Exists(TempPath)) File.Delete(TempPath); }
                catch { }
                return false;
            }
#endif
        }

        /// <summary>Async equivalent of <see cref="Save(string)"/> that performs JSON serialization and disk I/O off the UI thread.</summary>
        internal Task<bool> SaveAsync(string FilePath) => Task.Run(() => Save(FilePath));

        /// <summary>Loads settings.json from <paramref name="SettingsDirectory"/>. If the file is unreadable it is renamed to
        /// <c>settings-corrupt-{timestamp}.json</c> in the same folder and a defaults instance is returned.</summary>
        /// <param name="Exists">True if a settings file was found and parsed; false otherwise.</param>
        internal static SavedSettings Load(string SettingsDirectory, out bool Exists)
        {
#if XML_Settings
            Settings Settings = XMLSerializer.Deserialize<Settings>(GetSettingsPath(SettingsDirectory));
            if (Settings == null)
            {
                Settings = new Settings();
                Exists = false;
            }
            else
                Exists = true;
            return Settings;
#else
            string SettingsPath = GetSettingsPath(SettingsDirectory);
            Exists = File.Exists(SettingsPath);
            if (!Exists)
                return new SavedSettings();

            try
            {
                return GeneralUtils.DeserializeJson<SavedSettings>(File.ReadAllText(SettingsPath));
            }
            catch (Exception ex)
            {
                //  The settings file exists but couldn't be parsed (manually edited, partial write from a prior crash,
                //  schema mismatch, etc.). Quarantine it instead of silently overwriting on the next save.
                Debug.WriteLine($"Failed to load settings from '{SettingsPath}':\n{ex}");
                try
                {
                    Directory.CreateDirectory(SettingsDirectory);
                    string Quarantined = Path.Combine(
                        SettingsDirectory,
                        $"settings-corrupt-{DateTime.Now:yyyyMMdd-HHmmss}{FileExt}");
                    File.Move(SettingsPath, Quarantined, overwrite: false);
                    Debug.WriteLine($"Renamed unreadable settings file to '{Quarantined}'");
                }
                catch (Exception renameEx) { Debug.WriteLine($"Failed to quarantine corrupt settings file: {renameEx}"); }

                Exists = false;
                return new SavedSettings();
            }
#endif
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext sc) { }
        [OnSerialized]
        private void OnSerialized(StreamingContext sc) { }
        [OnDeserializing]
        private void OnDeserializing(StreamingContext sc) => InitializeDefaults();
        [OnDeserialized]
        private void OnDeserialized(StreamingContext sc) { }
#endregion Serialization
    }

    [DataContract(Name = "AuthorSettings", Namespace = "")]
    public class AuthorSettings
    {
        [DataMember(Name = "Author")]
        public string Author { get; set; }

        [DataMember(Name = "UserRating")]
        public double? UserRating { get; set; }

        [DataMember(Name = "UserNotes")]
        public string UserNotes { get; set; }

        [DataMember(Name = "IsFavorited")]
        public bool IsFavorited { get; set; }
        [DataMember(Name = "IsIgnored")]
        public bool IsIgnored { get; set; }
        [DataMember(Name = "IsRead")]
        public bool IsRead { get; set; }
        [DataMember(Name = "IsExpanded")]
        public bool IsExpanded { get; set; }

        public AuthorSettings()
        {
            InitializeDefaults();
        }

        public AuthorSettings(AuthorGroup Group)
            : this()
        {
            Author = Group.AuthorName;

            UserRating = Group.UserRating;
            UserNotes = Group.UserNotes;

            IsFavorited = Group.IsFavorited;
            IsIgnored = Group.IsIgnored;
            IsRead = Group.IsRead;
            IsExpanded = Group.IsExpanded;
        }

        public void ApplyTo(AuthorGroup Group)
        {
            if (Group == null)
                return;

            Group.UserRating = UserRating;
            Group.UserNotes = UserNotes;

            Group.IsFavorited = IsFavorited;
            Group.IsIgnored = IsIgnored;
            Group.IsRead = IsRead;
            Group.IsExpanded = IsExpanded;
        }

        private void InitializeDefaults()
        {
            Author = null;

            UserRating = null;
            UserNotes = null;

            IsFavorited = false;
            IsIgnored = false;
            IsRead = false;
            IsExpanded = true;
        }

        #region Serialization
        [OnSerializing]
        private void OnSerializing(StreamingContext sc) { }
        [OnSerialized]
        private void OnSerialized(StreamingContext sc) { }
        [OnDeserializing]
        private void OnDeserializing(StreamingContext sc) => InitializeDefaults();
        [OnDeserialized]
        private void OnDeserialized(StreamingContext sc) { }
        #endregion Serialization
    }

    [DataContract(Name = "StorySettings", Namespace = "")]
    public class StorySettings
    {
        [DataMember(Name = "Author")]
        public string Author { get; set; }
        [DataMember(Name = "Title")]
        public string Title { get; set; }

        [DataMember(Name = "Rating")]
        public double? UserRating { get; set; }

        [DataMember(Name = "UserNotes")]
        public string UserNotes { get; set; }

        [DataMember(Name = "UserTags")]
        public List<string> UserTags { get; set; }

        [DataMember(Name = "IsFavorited")]
        public bool IsFavorited { get; set; }
        [DataMember(Name = "IsIgnored")]
        public bool IsIgnored { get; set; }
        [DataMember(Name = "IsRead")]
        public bool IsRead { get; set; }

        /// <summary>If <see langword="true" />, indicates that the user has added this story to their 'Read-later' list</summary>
        [DataMember(Name = "QueuedAt")]
        public DateTime? QueuedAt { get; set; }
        [DataMember(Name = "LastOpenedAt")]
        public DateTime? LastOpenedAt { get; set; }
        [DataMember(Name = "DownloadedAt")]
        public DateTime? DownloadedAt { get; set; }

        [DataMember(Name = "RecentPosition")]
        public Bookmark RecentPosition { get; set; }

        public StorySettings()
        {
            InitializeDefaults();
        }

        public StorySettings(LiteroticaStory Story)
            : this()
        {
            Author = Story.AuthorName;
            Title = Story.Title;

            UserRating = Story.UserRating;
            UserNotes = Story.UserNotes;
            UserTags = new(); //TODO

            IsFavorited = Story.IsStoryFavorited;
            IsIgnored = Story.IsIgnored;
            IsRead = Story.IsRead;

            QueuedAt = Story.QueuedAt;
            LastOpenedAt = Story.LastOpenedAt;
            DownloadedAt = Story.DownloadedAt;

            RecentPosition = Story.RecentPosition;
        }

        public void ApplyTo(LiteroticaStory Story)
        {
            if (Story == null)
                return;

            Story.UserRating = UserRating;
            Story.UserNotes = UserNotes;
            //Story.UserTags = UserTags //TODO

            Story.IsStoryFavorited = IsFavorited;
            Story.IsIgnored = IsIgnored;
            Story.IsRead = IsRead;

            Story.QueuedAt = QueuedAt;
            Story.LastOpenedAt = LastOpenedAt;

            Story.RecentPosition = RecentPosition;
        }

        private void InitializeDefaults()
        {
            Author = null;
            Title = null;

            UserRating = null;
            UserNotes = "";

            UserTags = new();

            IsFavorited = false;
            IsIgnored = false;
            IsRead = false;

            QueuedAt = null;
            LastOpenedAt = null;
            DownloadedAt = null;

            RecentPosition = null;
        }

        #region Serialization
        [OnSerializing]
        private void OnSerializing(StreamingContext sc) { }
        [OnSerialized]
        private void OnSerialized(StreamingContext sc) { }
        [OnDeserializing]
        private void OnDeserializing(StreamingContext sc) => InitializeDefaults();
        [OnDeserialized]
        private void OnDeserialized(StreamingContext sc) { }
        #endregion Serialization
    }
}
