using Microsoft.WindowsAPICodePack.Dialogs;
using Prism.Commands;
using StoryManager.UI;
using StoryManager.VM.Helpers;
using StoryManager.VM.Literotica;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Xceed.Wpf.Toolkit.Primitives;

namespace StoryManager.VM
{
    public enum SortBy
    {
        Title,
        FirstChapterDate,
        LastChapterDate
    }

    public class Settings : ViewModelBase
    {
        private SavedSettings _PreviousSessionSettings;
        /// <summary>The DTO loaded from the stories folder's settings.json. Replaced by <see cref="LoadFromCurrentStoriesDirectory"/>
        /// when the user switches stories folders so per-story / per-author lookups reflect the active library.</summary>
        public SavedSettings PreviousSessionSettings => _PreviousSessionSettings;

        public MainViewModel MVM { get; }

        public DisplaySettings DisplaySettings { get; }

        private SettingsWindow _Window;
        public SettingsWindow Window
        {
            get => _Window;
            private set
            {
                if (_Window != value)
                {
                    _Window = value;
                    NPC(nameof(Window));

                    void OnWindowClosed(object sender, EventArgs e)
                    {
                        if (sender is SettingsWindow window)
                        {
                            window.Closed -= OnWindowClosed;
                            if (Window == window)
                                Window = null;
                        }
                    }

                    if (Window != null)
                        Window.Closed += OnWindowClosed;
                }
            }
        }

        public DelegateCommand<object> OpenWindow => new(_ => OpenOrActivateWindow());
        public void OpenOrActivateWindow()
        {
            if (Window != null)
                Window.Activate();
            else
            {
                Window = new(this);
                Window.Owner = MVM.Window;
                Window.Show();
            }
        }

        private string DefaultStoriesDirectory { get; }
        internal bool IsUsingDefaultStoriesDirectory => StoriesDirectory == DefaultStoriesDirectory;

        private string _StoriesDirectory;
        public string StoriesDirectory => _StoriesDirectory;

        /// <summary>Switches <see cref="StoriesDirectory"/> to <paramref name="NewPath"/> and reloads settings.json from
        /// inside it via <see cref="LoadFromCurrentStoriesDirectory"/>. Callers (e.g. <see cref="MainViewModel.ReloadStoriesFolderAsync"/>)
        /// are responsible for saving any current-folder state before calling this.</summary>
        internal void SwitchStoriesDirectory(string NewPath)
        {
            if (_StoriesDirectory == NewPath)
                return;
            _StoriesDirectory = NewPath;
            NPC(nameof(StoriesDirectory));
            //  Persist the bootstrap pointer immediately so a crash before close-time save still leaves
            //  the next launch pointing at the right folder.
            SavedSettings.WriteBootstrapPointer(_StoriesDirectory);
            LoadFromCurrentStoriesDirectory(UpdateDocument: true);
        }

        public const int DefaultHistorySize = 25;

        private int _HistorySize;
        public int HistorySize
        {
            get => _HistorySize;
            set
            {
                if (_HistorySize != value)
                {
                    _HistorySize = value;
                    NPC(nameof(HistorySize));
                }
            }
        }

        #region Theme
        private static readonly Dictionary<Theme, ColorPalette> DefaultColorPalettes = new()
        {
            { Theme.LightMode, new("LightTheme1", Color.FromRgb(10, 10, 10), Color.FromRgb(240, 240, 240), Color.FromArgb(192, 255, 255, 0)) },
            { Theme.DarkMode, new("DarkTheme1", Color.FromRgb(170, 182, 255), Color.FromRgb(32, 32, 32), Color.FromArgb(192, 255, 0, 0)) }
        };

        private Theme _Theme;
        public Theme Theme => _Theme;

        public async Task SetThemeAsync(Theme Value, bool IsInitializing, bool UpdateDocument)
        {
            if (_Theme != Value || IsInitializing)
            {
                bool IsUsingDefaultColors = IsUsingDefaultForegroundColor && IsUsingDefaultBackgroundColor && IsUsingDefaultHighlightColor;

                _Theme = Value;
                NPC(nameof(Theme));
                NPC(nameof(IsLightMode));
                NPC(nameof(IsDarkMode));

                if (IsInitializing || IsUsingDefaultColors)
                {
                    await SetForegroundColorAsync(DefaultColorPalettes[Theme].ForegroundColor, UpdateDocument);
                    await SetBackgroundColorAsync(DefaultColorPalettes[Theme].BackgroundColor, UpdateDocument);
                    await SetHighlightColorAsync(DefaultColorPalettes[Theme].HighlightColor, UpdateDocument);
                }
            }
        }

        /// <summary>Only intended to be used by data-binding on the UI. To set this value programmatically, use <see cref="SetThemeAsync(Theme, bool, bool)"/> instead.</summary>
        public bool IsLightMode
        {
            get => Theme == Theme.LightMode;
            set { if (value) _ = SetThemeAsync(Theme.LightMode, false, true); }
        }

        /// <summary>Only intended to be used by data-binding on the UI. To set this value programmatically, use <see cref="SetThemeAsync(Theme, bool, bool)"/> instead.</summary>
        public bool IsDarkMode
        {
            get => Theme == Theme.DarkMode;
            set { if (value) _ = SetThemeAsync(Theme.DarkMode, false, true); }
        }
        #endregion Theme

        #region FontSize
        public const int DefaultFontSize = 16;
        private bool IsUsingDefaultFontSize => FontSize == DefaultFontSize;

        public int BindableFontSize
        {
            get => FontSize;
            set => _ = SetFontSizeAsync(value, true);
        }

        private int _FontSize;
        public int FontSize => _FontSize;
        public async Task SetFontSizeAsync(int Value, bool UpdateDocument)
        {
            int ActualValue = Math.Clamp(Value, 10, 50);
            if (FontSize != ActualValue)
            {
                _FontSize = ActualValue;
                NPC(nameof(FontSize));
                NPC(nameof(BindableFontSize));

                if (UpdateDocument)
                    await MVM.RefreshFontSizeAsync();
            }
        }
        #endregion FontSize

        #region FontFamily
        private readonly HashSet<string> ValidFontFamilies = new InstalledFontCollection().Families.Select(x => x.Name).ToHashSet();

        public const string DefaultFontFamily = "Times New Roman";
        public bool IsUsingDefaultFontFamily => FontFamily == DefaultFontFamily;

        public string BindableFontFamily
        {
            get => FontFamily;
            set => _ = SetFontFamilyAsync(value, true);
        }

        private string _FontFamily;
        public string FontFamily => _FontFamily;
        public async Task SetFontFamilyAsync(string Value, bool UpdateDocument)
        {
            if (FontFamily != Value && ValidFontFamilies.Contains(Value))
            {
                _FontFamily = Value;
                NPC(nameof(FontFamily));
                NPC(nameof(BindableFontFamily));
                NPC(nameof(IsUsingDefaultFontFamily));

                if (UpdateDocument)
                    await MVM.RefreshFontFamilyAsync();
            }
        }

        public DelegateCommand<object> ResetFontFamily => new(_ => _ = SetFontFamilyAsync(DefaultFontFamily, true));
        #endregion FontFamily

        #region Colors
        public static IEnumerable<ColorPalette> StaticColorPalettes
        {
            get
            {
                static Color GetGrayscaleColor(byte Value) => Color.FromRgb(Value, Value, Value);

                Color YellowHighlight1 = Color.FromArgb(192, 255, 233, 88);
                Color YellowHighlight2 = Color.FromArgb(192, 220, 192, 56);

                foreach (ColorPalette ThemedPalette in DefaultColorPalettes.Values)
                    yield return ThemedPalette;

                for (int i = 0; i < 3; i++)
                    yield return new($"Gray-{i + 1}", Colors.Black, GetGrayscaleColor((byte)(173 + i * 16)), YellowHighlight1);
                for (int i = 0; i < 3; i++)
                    yield return new($"White-{i + 1}", GetGrayscaleColor((byte)(i * 16)), GetGrayscaleColor((byte)(byte.MaxValue - i * 16)), YellowHighlight1);
                for (int i = 0; i < 3; i++)
                    yield return new($"Black-{i + 1}", GetGrayscaleColor((byte)(byte.MaxValue - i * 16)), GetGrayscaleColor((byte)(i * 16)), YellowHighlight2);

                Color Red1 = Color.FromRgb(224, 16, 16);
                for (int i = 0; i < 3; i++)
                    yield return new($"Gray-Red-{i + 1}", Red1, GetGrayscaleColor((byte)(173 + i * 16)), YellowHighlight1);
                for (int i = 0; i < 3; i++)
                    yield return new($"White-Red-{i + 1}", Red1, GetGrayscaleColor((byte)(byte.MaxValue - i * 16)), YellowHighlight1);
                for (int i = 0; i < 3; i++)
                    yield return new($"Black-Red-{i + 1}", Red1, GetGrayscaleColor((byte)(i * 24)), YellowHighlight2);

                for (int i = 0; i < 3; i++)
                    yield return new($"Gray-Green-{i + 1}", Color.FromRgb(0, 140, 0), GetGrayscaleColor((byte)(173 + i * 16)), YellowHighlight1);
                for (int i = 0; i < 3; i++)
                    yield return new($"White-Green-{i + 1}", Color.FromRgb(0, 180, 0), GetGrayscaleColor((byte)(byte.MaxValue - i * 16)), YellowHighlight1);
                for (int i = 0; i < 3; i++)
                    yield return new($"Black-Green-{i + 1}", Color.FromRgb(16, 224, 16), GetGrayscaleColor((byte)(i * 24)), YellowHighlight2);

                for (int i = 0; i < 3; i++)
                    yield return new($"Gray-Blue-{i + 1}", Color.FromRgb(12, 60, 240), GetGrayscaleColor((byte)(173 + i * 16)), YellowHighlight1);
                for (int i = 0; i < 3; i++)
                    yield return new($"White-Blue-{i + 1}", Color.FromRgb(12, 60, 240), GetGrayscaleColor((byte)(byte.MaxValue - i * 16)), YellowHighlight1);
                for (int i = 0; i < 3; i++)
                    yield return new($"Black-Blue-{i + 1}", Color.FromRgb(16, 120, 232), GetGrayscaleColor((byte)(i * 24)), YellowHighlight2);
            }
        }

        public IReadOnlyList<ColorPalette> PresetColorPalettes { get; } = StaticColorPalettes.ToList();

        public int SelectedColorPaletteIndex
        {
            get => -1;
            set
            {
                if (value != -1)
                {
                    _ = SetColorPaletteAsync(PresetColorPalettes[value], true);
                    Window.Dispatcher.BeginInvoke(() => Window.PresetColorPalettesDropdown.SelectedItem = null, DispatcherPriority.ApplicationIdle);
                }
            }
        }

        private async Task SetColorPaletteAsync(ColorPalette Value, bool UpdateDocument)
        {
            await SetForegroundColorAsync(Value.ForegroundColor, UpdateDocument);
            await SetBackgroundColorAsync(Value.BackgroundColor, UpdateDocument);
            await SetHighlightColorAsync(Value.HighlightColor, UpdateDocument);
        }

        private bool IsUsingDefaultForegroundColor => ForegroundColor == DefaultColorPalettes[Theme].ForegroundColor;
        private bool IsUsingDefaultBackgroundColor => BackgroundColor == DefaultColorPalettes[Theme].BackgroundColor;
        private bool IsUsingDefaultHighlightColor => HighlightColor == DefaultColorPalettes[Theme].HighlightColor;

        public Color BindableForegroundColor
        {
            get => ForegroundColor;
            set => _ = SetForegroundColorAsync(value, true);
        }

        private Color _ForegroundColor;
        public Color ForegroundColor => _ForegroundColor;
        public async Task SetForegroundColorAsync(Color Value, bool UpdateDocument)
        {
            if (ForegroundColor != Value)
            {
                _ForegroundColor = Value;
                NPC(nameof(ForegroundColor));
                NPC(nameof(BindableForegroundColor));

                if (UpdateDocument)
                    await MVM.RefreshForegroundColorAsync();
            }
        }

        public Color BindableBackgroundColor
        {
            get => BackgroundColor;
            set => _ = SetBackgroundColorAsync(value, true);
        }

        private Color _BackgroundColor;
        public Color BackgroundColor => _BackgroundColor;
        public async Task SetBackgroundColorAsync(Color Value, bool UpdateDocument)
        {
            if (BackgroundColor != Value)
            {
                _BackgroundColor = Value;
                NPC(nameof(BackgroundColor));
                NPC(nameof(BindableBackgroundColor));

                if (UpdateDocument)
                    await MVM.RefreshBackgroundColorAsync();
            }
        }

        public Color BindableHighlightColor
        {
            get => HighlightColor;
            set => _ = SetHighlightColorAsync(value, true);
        }

        private Color _HighlightColor;
        public Color HighlightColor => _HighlightColor;
        public async Task SetHighlightColorAsync(Color Value, bool UpdateDocument)
        {
            if (HighlightColor != Value)
            {
                _HighlightColor = Value;
                NPC(nameof(HighlightColor));
                NPC(nameof(BindableHighlightColor));

                if (UpdateDocument)
                    await MVM.RefreshHighlightColorAsync();
            }
        }
        #endregion Colors

        #region Keywords
        /// <summary>Only intended to be used by data-binding on the UI.<br/>
        /// To set this value programmatically, use <see cref="SetCommaDelimitedKeywordsAsync(string, bool)"/> instead.</summary>
        public string BindableCommaDelimitedKeywords
        {
            get => CommaDelimitedKeywords;
            set => _ = SetCommaDelimitedKeywordsAsync(value, true);
        }

        private string _CommaDelimitedKeywords;
        /// <summary>A comma-delimited list of words to highlight in the document.</summary>
        public string CommaDelimitedKeywords => _CommaDelimitedKeywords;
        public async Task SetCommaDelimitedKeywordsAsync(string Value, bool UpdateDocument)
        {
            if (CommaDelimitedKeywords != Value)
            {
                _CommaDelimitedKeywords = Value;
                NPC(nameof(CommaDelimitedKeywords));
                NPC(nameof(BindableCommaDelimitedKeywords));

                if (UpdateDocument)
                    await MVM.HighlightKeywords();
            }
        }

        public IReadOnlyList<string> GetKeywords() => CommaDelimitedKeywords == null ? new List<string>() : CommaDelimitedKeywords.Split(',').ToList();
        #endregion Keywords

        #region Grouping
        private bool _GroupAllByAuthor;
        public bool GroupAllByAuthor
        {
            get => _GroupAllByAuthor;
            set
            {
                if (_GroupAllByAuthor != value)
                {
                    _GroupAllByAuthor = value;
                    NPC(nameof(GroupAllByAuthor));
                    GroupAllByAuthorChanged?.Invoke(this, GroupAllByAuthor);
                }
            }
        }

        public event EventHandler<bool> GroupAllByAuthorChanged;

        private bool _GroupFavoritesByAuthor;
        public bool GroupFavoritesByAuthor
        {
            get => _GroupFavoritesByAuthor;
            set
            {
                if (_GroupFavoritesByAuthor != value)
                {
                    _GroupFavoritesByAuthor = value;
                    NPC(nameof(GroupFavoritesByAuthor));
                    GroupFavoritesByAuthorChanged?.Invoke(this, GroupFavoritesByAuthor);
                }
            }
        }

        public event EventHandler<bool> GroupFavoritesByAuthorChanged;
        #endregion Grouping

        #region Sorting
        private SortBy _SortingMode;
        public SortBy SortingMode
        {
            get => _SortingMode;
            set
            {
                if (_SortingMode != value)
                {
                    _SortingMode = value;
                    NPC(nameof(SortingMode));
                    NPC(nameof(SortByTitle));
                    NPC(nameof(SortByFirstChapterDate));
                    NPC(nameof(SortByLastChapterDate));
                    SortingModeChanged?.Invoke(this, SortingMode);
                }
            }
        }

        public bool SortByTitle
        {
            get => SortingMode == SortBy.Title;
            set { if (value) SortingMode = SortBy.Title; }
        }

        public bool SortByFirstChapterDate
        {
            get => SortingMode == SortBy.FirstChapterDate;
            set { if (value) SortingMode = SortBy.FirstChapterDate; }
        }

        public bool SortByLastChapterDate
        {
            get => SortingMode == SortBy.LastChapterDate;
            set { if (value) SortingMode = SortBy.LastChapterDate; }
        }

        public event EventHandler<SortBy> SortingModeChanged;
        #endregion Sorting

        private bool _WarnIfClosingUnsavedStory;
        public bool WarnIfClosingUnsavedStory
        {
            get => _WarnIfClosingUnsavedStory;
            set
            {
                if (_WarnIfClosingUnsavedStory != value)
                {
                    _WarnIfClosingUnsavedStory = value;
                    NPC(nameof(WarnIfClosingUnsavedStory));
                }
            }
        }

        public Settings(MainViewModel MVM)
        {
            this.MVM = MVM;

            //  Resolve the stories folder FIRST. settings.json now lives inside that folder, so the location of
            //  settings.json is no longer fixed — it follows wherever the stories live. ResolveStoriesFolder reads
            //  the bootstrap pointer (stories-folder.txt) or migrates from the legacy %MyDocuments% location on first
            //  run after upgrade. The returned path may not exist on disk (e.g., bootstrap pointer references an offline
            //  drive); MainViewModel.LoadStoriesAsync will surface the existing "Stories folder not found" warning in
            //  that case and skip persisting changes.
            DefaultStoriesDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), nameof(StoryManager), "Stories", "Literotica");
            _StoriesDirectory = SavedSettings.ResolveStoriesFolder(DefaultStoriesDirectory);

            //  DisplaySettings + the per-story / per-author lookup dicts are stable references for the lifetime of
            //  this Settings instance; LoadFromCurrentStoriesDirectory mutates their contents in place rather than
            //  replacing them so existing data bindings (and the FilterSettings.FiltersChanged subscriber wired in
            //  MainViewModel's ctor) don't break.
            DisplaySettings = new();
            AuthorSettingsByName = new();
            StorySettingsByAuthorAndTitle = new();

            //  UpdateDocument is false on initial load: the WebView2 hasn't been initialized yet, so async refresh
            //  calls would no-op or fail. The MVM completes WebView2 setup later in LoadStoriesAsync.
            LoadFromCurrentStoriesDirectory(UpdateDocument: false);
        }

        /// <summary>Reads <c>settings.json</c> from <see cref="StoriesDirectory"/> and applies all of its contents into this
        /// <see cref="Settings"/> instance: the per-story / per-author lookup dicts, display flags, sort/group/history prefs,
        /// theme, font, colors, and keywords.</summary>
        /// <param name="UpdateDocument">Whether the appearance refresh calls (font, colors) should re-execute scripts in the
        /// WebView2. False during initial program load (the WebView2 isn't ready yet); true on a later directory-change reload.</param>
        private void LoadFromCurrentStoriesDirectory(bool UpdateDocument)
        {
            _PreviousSessionSettings = SavedSettings.Load(_StoriesDirectory, out _);

            //  Per-story / per-author maps that RegisterStory consults when a newly-loaded LiteroticaStory is first added.
            //  Clear-and-repopulate (rather than replacing the dictionary instance) so anything holding a reference still sees
            //  the current data.
            AuthorSettingsByName.Clear();
            foreach (AuthorSettings A in _PreviousSessionSettings.AuthorSettings)
            {
                if (!string.IsNullOrEmpty(A.Author) && !AuthorSettingsByName.ContainsKey(A.Author))
                    AuthorSettingsByName[A.Author] = A;
            }
            StorySettingsByAuthorAndTitle.Clear();
            foreach (StorySettings S in _PreviousSessionSettings.StorySettings.DistinctBy(x => MainViewModel.GetStoryUniqueKey(x.Author, x.Title)))
                StorySettingsByAuthorAndTitle[MainViewModel.GetStoryUniqueKey(S.Author, S.Title)] = S;

            //  Display flags (DisplaySettings is a stable reference; mutate fields in place).
            DisplaySettings.ShowCategory = _PreviousSessionSettings.ShowCategory;
            DisplaySettings.ShowDateApproved = _PreviousSessionSettings.ShowDateApproved;
            DisplaySettings.ShowReadState = _PreviousSessionSettings.ShowReadState;
            DisplaySettings.ShowOverallRating = _PreviousSessionSettings.ShowOverallRating;
            DisplaySettings.ShowPageCount = _PreviousSessionSettings.ShowPageCount;
            DisplaySettings.ShowWordCount = _PreviousSessionSettings.ShowWordCount;
            DisplaySettings.ShowUserRating = _PreviousSessionSettings.ShowUserRating;
            DisplaySettings.ShowDateDownloaded = _PreviousSessionSettings.ShowDateDownloaded;

            HistorySize = _PreviousSessionSettings.HistorySize ?? DefaultHistorySize;
            GroupAllByAuthor = _PreviousSessionSettings.GroupAllByAuthor;
            GroupFavoritesByAuthor = _PreviousSessionSettings.GroupFavoritesByAuthor;
            SortingMode = _PreviousSessionSettings.SortingMode;
            WarnIfClosingUnsavedStory = _PreviousSessionSettings.WarnIfClosingUnsavedStory;

            _ = SetThemeAsync(_PreviousSessionSettings.Theme, true, UpdateDocument);
            _ = SetCommaDelimitedKeywordsAsync(_PreviousSessionSettings.Keywords, UpdateDocument);
            _ = SetFontSizeAsync(_PreviousSessionSettings.GetFontSize(DefaultFontSize), UpdateDocument);
            _ = SetFontFamilyAsync(_PreviousSessionSettings.GetFontFamily(DefaultFontFamily), UpdateDocument);
            _ = SetForegroundColorAsync(_PreviousSessionSettings.GetForegroundColor(DefaultColorPalettes[Theme].ForegroundColor), UpdateDocument);
            _ = SetBackgroundColorAsync(_PreviousSessionSettings.GetBackgroundColor(DefaultColorPalettes[Theme].BackgroundColor), UpdateDocument);
            _ = SetHighlightColorAsync(_PreviousSessionSettings.GetHighlightColor(DefaultColorPalettes[Theme].HighlightColor), UpdateDocument);
        }

        private Dictionary<string, AuthorSettings> AuthorSettingsByName { get; }
        /// <summary>Key is given by <see cref="MainViewModel.GetStoryUniqueKey(string, string)"/></summary>
        private Dictionary<string, StorySettings> StorySettingsByAuthorAndTitle { get; }

        public AuthorSettings GetPreviousSessionAuthorSettings(string AuthorName, AuthorSettings DefaultValue)
        {
            if (AuthorSettingsByName.TryGetValue(AuthorName, out AuthorSettings Result))
                return Result;
            else
                return DefaultValue;
        }

        public StorySettings GetPreviousSessionStorySettings(LiteroticaStory Story, StorySettings DefaultValue)
        {
            string Key = MainViewModel.GetStoryUniqueKey(Story.AuthorName, Story.Title);
            if (StorySettingsByAuthorAndTitle.TryGetValue(Key, out StorySettings StorySettings))
                return StorySettings;
            else
                return DefaultValue;
        }

        public Task SaveAsync(bool TryCreateBackup) => SaveAsync(TryCreateBackup, null);

        public async Task SaveAsync(bool TryCreateBackup, IProgress<(int current, int total, string status)> Progress)
        {
            if (MVM.SelectedStory != null)
                MVM.SelectedStory.RecentPosition = await Bookmark.CreateAsync(MVM.WebView);

            //  Materialize the per-story / per-author DTO lists with explicit iteration so we can report progress.
            //  When the load didn't complete this session, fall back to the previous session's values verbatim
            //  (see F1 in the PR notes — protects against close-while-loading clobbering user metadata).
            int StoryTotal = MVM.IsStoriesLoaded ? MVM.Stories.Count : (PreviousSessionSettings.StorySettings?.Count ?? 0);
            Progress?.Report((0, StoryTotal, StoryTotal == 0 ? "Saving settings..." : $"Saving 0 of {StoryTotal} stories..."));

            List<StorySettings> StorySettingsList;
            List<AuthorSettings> AuthorSettingsList;
            if (MVM.IsStoriesLoaded)
            {
                StorySettingsList = new List<StorySettings>(StoryTotal);
                int Done = 0;
                foreach (LiteroticaStory Story in MVM.Stories)
                {
                    StorySettingsList.Add(new StorySettings(Story));
                    Done++;
                    //  Throttle the report rate — for thousands of stories, reporting every iteration floods the dispatcher.
                    if (Progress != null && (Done % 50 == 0 || Done == StoryTotal))
                        Progress.Report((Done, StoryTotal, $"Saving {Done} of {StoryTotal} stories..."));
                }
                AuthorSettingsList = MVM.AuthorGroups.Values.Select(x => new AuthorSettings(x)).ToList();
            }
            else
            {
                StorySettingsList = PreviousSessionSettings.StorySettings;
                AuthorSettingsList = PreviousSessionSettings.AuthorSettings;
                Progress?.Report((StoryTotal, StoryTotal, $"{StoryTotal} stor{(StoryTotal == 1 ? "y" : "ies")} preserved (load did not complete this session)."));
            }

            ColorConverter ColorConverter = new();

            SavedSettings Settings = new()
            {
                OpenedCount = PreviousSessionSettings.OpenedCount + 1,

                Theme = Theme,

                WindowLeftPosition = MVM.Window.Left,
                WindowTopPosition = MVM.Window.Top,
                WindowWidth = MVM.Window.Width,
                WindowHeight = MVM.Window.Height,

                SidebarWidth = MVM.Window.SidebarColumn.ActualWidth,

                FontSize = IsUsingDefaultFontSize ? null : FontSize,
                FontFamily = IsUsingDefaultFontFamily ? null : FontFamily,

                ForegroundColor = IsUsingDefaultForegroundColor ? null : ColorConverter.ConvertToString(ForegroundColor),
                BackgroundColor = IsUsingDefaultBackgroundColor ? null : ColorConverter.ConvertToString(BackgroundColor),
                HighlightColor = IsUsingDefaultHighlightColor ? null : ColorConverter.ConvertToString(HighlightColor),

                //  StoriesBaseFolder is no longer written to settings.json (the bootstrap pointer + the file's own
                //  location are authoritative). ShouldSerializeStoriesBaseFolder returns false so the field is dropped
                //  from the output regardless of what we assign here.

                RecentSelectedStory = !MVM.IsStoriesLoaded
                    ? PreviousSessionSettings.RecentSelectedStory
                    : (MVM.SelectedStory == null ? null : MainViewModel.GetStoryUniqueKey(MVM.SelectedStory.AuthorName, MVM.SelectedStory.Title)),

                HistorySize = HistorySize,
                HistoryList = MVM.IsStoriesLoaded
                    ? MVM.RecentStories.Select(x => MainViewModel.GetStoryUniqueKey(x.AuthorName, x.Title)).ToList()
                    : PreviousSessionSettings.HistoryList,

                GroupAllByAuthor = GroupAllByAuthor,
                GroupFavoritesByAuthor = GroupFavoritesByAuthor,

                SortingMode = SortingMode,

                Keywords = CommaDelimitedKeywords,

                //  Lists were built above with progress reporting; assign them here.
                AuthorSettings = AuthorSettingsList,
                StorySettings = StorySettingsList,

                ShowCategory = DisplaySettings.ShowCategory,
                ShowDateApproved = DisplaySettings.ShowDateApproved,
                ShowReadState = DisplaySettings.ShowReadState,
                ShowOverallRating = DisplaySettings.ShowOverallRating,
                ShowPageCount = DisplaySettings.ShowPageCount,
                ShowWordCount = DisplaySettings.ShowWordCount,
                ShowUserRating = DisplaySettings.ShowUserRating,
                ShowDateDownloaded = DisplaySettings.ShowDateDownloaded,

                SaveAfterDownloading = MVM.Downloader.SaveAfterDownloading,

                WarnIfClosingUnsavedStory = WarnIfClosingUnsavedStory
            };
            Progress?.Report((StoryTotal, StoryTotal, "Writing settings.json..."));
            await Settings.SaveAsync(SavedSettings.GetSettingsPath(StoriesDirectory));

            if (TryCreateBackup)
            {
                //  Backups now live alongside settings.json in the stories folder, so a copy of the stories folder
                //  carries its full revision history with it.
                string Folder = StoriesDirectory;

                //  Find the oldest backup to overwrite
                DateTime OldestBackupWriteTime = DateTime.Now;
                string OldestBackupFilePath = null;
                for (int i = 1; i <= SavedSettings.MaxBackups; i++)
                {
                    string BackupFilePath = SavedSettings.GetBackupPath(Folder, i);
                    if (!File.Exists(BackupFilePath))
                    {
                        OldestBackupFilePath = BackupFilePath;
                        break;
                    }
                    else
                    {
                        DateTime WriteTime = File.GetLastWriteTime(BackupFilePath);
                        if (i == 1 || WriteTime < OldestBackupWriteTime)
                        {
                            OldestBackupWriteTime = WriteTime;
                            OldestBackupFilePath = BackupFilePath;
                        }
                    }
                }

                //  Determine how recent the newest backup is
                DateTime? NewestBackupWriteTime = null;
                for (int i = 1; i <= SavedSettings.MaxBackups; i++)
                {
                    string BackupFilePath = SavedSettings.GetBackupPath(Folder, i);
                    if (File.Exists(BackupFilePath))
                    {
                        DateTime WriteTime = File.GetLastWriteTime(BackupFilePath);
                        if (!NewestBackupWriteTime.HasValue || WriteTime > NewestBackupWriteTime.Value)
                            NewestBackupWriteTime = WriteTime;
                    }
                }

                //  Save the backup file to the oldest slot, as long as the oldest slot is empty or the newest backup isn't too recent
                TimeSpan RecencyThreshold = TimeSpan.FromHours(1.0);
                if (!File.Exists(OldestBackupFilePath) || !NewestBackupWriteTime.HasValue || DateTime.Now.Subtract(NewestBackupWriteTime.Value) >= RecencyThreshold)
                {
                    Progress?.Report((StoryTotal, StoryTotal, "Writing backup..."));
                    await Settings.SaveAsync(OldestBackupFilePath);
                }
            }
        }

        public DelegateCommand<object> BrowseStoriesFolder => new(_ =>
        {
            try
            {
                CommonOpenFileDialog FolderBrowser = new CommonOpenFileDialog();
                FolderBrowser.InitialDirectory = StoriesDirectory;
                FolderBrowser.IsFolderPicker = true;
                if (FolderBrowser.ShowDialog() == CommonFileDialogResult.Ok && FolderBrowser.FileName != StoriesDirectory)
                {
                    //  Hand off to MVM so the reload sequence (save-to-old-folder, switch settings, reset MVM state,
                    //  load-from-new-folder) runs as a single coordinated transition with full isolation between libraries.
                    _ = MVM.ReloadStoriesFolderAsync(FolderBrowser.FileName);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        });

        //  Settings (and rotating backups) now live in the stories folder itself, so opening the "settings folder"
        //  is the same as opening the stories folder.
        public DelegateCommand<object> OpenSettingsFolder => new(_ => { GeneralUtils.ShellExecute(StoriesDirectory); });
    }

    public readonly record struct ColorPalette(string Name, Color ForegroundColor, Color BackgroundColor, Color HighlightColor);
}
