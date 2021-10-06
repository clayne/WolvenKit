using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ReactiveUI.Fody.Helpers;
using DynamicData;
using ReactiveUI;
using WolvenKit.Common;
using WolvenKit.Common.Model;
using WolvenKit.Common.Services;
using WolvenKit.Functionality.Commands;
using WolvenKit.Functionality.Controllers;
using WolvenKit.Functionality.Services;
using Microsoft.Extensions.FileSystemGlobbing;
using System.IO;
using System.Text.RegularExpressions;
using WolvenKit.Common.Interfaces;

namespace WolvenKit.ViewModels.Tools
{
    public class AssetBrowserViewModel : ToolViewModel
    {
        #region constants

        /// <summary>
        /// Identifies the <see ref="ContentId"/> of this tool window.
        /// </summary>
        public const string ToolContentId = "AssetBrowser_Tool";

        /// <summary>
        /// Identifies the caption string used for this tool window.
        /// </summary>
        public const string ToolTitle = "Asset Browser";

        public enum ESearchKeys
        {
            Hash,
            Kind,
            Name,
            Limit,
        }

        public const int SEARCH_LIMIT = 1000;

        #endregion constants

        #region fields

        private readonly INotificationService _notificationService;
        private readonly IGameControllerFactory _gameController;
        private readonly IArchiveManager _archiveManager;
        private readonly ISettingsManager _settings;

        private readonly ReadOnlyObservableCollection<RedFileSystemModel> _boundRootNodes;

        private bool _isModBrowserActive;

        #endregion fields

        #region ctor

        public AssetBrowserViewModel(
            INotificationService notificationService,
            IGameControllerFactory gameController,
            IArchiveManager archiveManager,
            ISettingsManager settings
        ) : base(ToolTitle)
        {
            _notificationService = notificationService;
            _gameController = gameController;
            _archiveManager = archiveManager;
            _settings = settings;

            ContentId = ToolContentId;

            TogglePreviewCommand = new RelayCommand(ExecuteTogglePreview, CanTogglePreview);
            OpenFileSystemItemCommand = new RelayCommand(ExecuteOpenFile, CanOpenFile);
            AddSelectedCommand = new RelayCommand(ExecuteAddSelected, CanAddSelected);
            ToggleModBrowserCommand = new RelayCommand(ExecuteToggleModBrowser, CanToggleModBrowser);
            OpenFileLocationCommand = new RelayCommand(ExecuteOpenFileLocationCommand, CanOpenFileLocationCommand);

            ExpandAll = ReactiveCommand.Create(() => { });
            CollapseAll = ReactiveCommand.Create(() => { });
            Collapse = ReactiveCommand.Create(() => { });
            Expand = ReactiveCommand.Create(() => { });

            AddSearchKeyCommand = ReactiveCommand.Create<string>(x => SearchBarText += $" {x}:");

            archiveManager.ConnectGameRoot()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _boundRootNodes)
                .Subscribe(
                _ =>
                {
                    // binds only the root node
                    LeftItems = new ObservableCollection<RedFileSystemModel>(_boundRootNodes);
                });

            archiveManager
                .WhenAnyValue(x => x.IsManagerLoaded)
                .Subscribe(b =>
                {
                    LoadVisibility = b ? Visibility.Collapsed : Visibility.Visible;
                    if (b)
                    {
                        _notificationService.Success($"Asset Browser is initialized");
                    }
                });


            Classes = _gameController
                .GetController()
                .GetAvaliableClasses();
        }

        #endregion ctor

        #region properties

        // binding properties. do not make private
        [Reactive] public GridLength PreviewWidth { get; set; } = new(0, GridUnitType.Pixel);

        [Reactive] public Visibility LoadVisibility { get; set; } = Visibility.Visible;

        /// <summary>
        /// Bound RootNodes to left navigation
        /// </summary>
        [Reactive] public ObservableCollection<RedFileSystemModel> LeftItems { get; set; } = new();

        /// <summary>
        /// Selected Root node in left navigation
        /// </summary>
        [Reactive] public object LeftSelectedItem { get; set; }

        /// <summary>
        /// Selected File in right navigaiton
        /// </summary>
        [Reactive] public IFileSystemViewModel RightSelectedItem { get; set; }

        /// <summary>
        /// Items (Files) inside a Node (Folder) bound to right navigation
        /// </summary>
        [Reactive] public ObservableCollection<IFileSystemViewModel> RightItems { get; set; } = new();

        /// <summary>
        /// Selected Files in right navigaiton
        /// </summary>
        [Reactive] public ObservableCollection<object> RightSelectedItems { get; set; } = new();
        [Reactive] public List<string> Classes { get; set; }
        [Reactive] public string SelectedClass { get; set; }
        [Reactive] public string SelectedExtension { get; set; }

        [Reactive] public string SearchBarText { get; set; }
        [Reactive] public string OptionsSearchBarText { get; set; }

        [Reactive] public bool IsRegexSearchEnabled { get; set; }

        #endregion properties

        #region commands

        public ReactiveCommand<string, Unit> AddSearchKeyCommand { get; set; }

        public ICommand AddSelectedCommand { get; private set; }
        private bool CanAddSelected() => RightSelectedItems != null && RightSelectedItems.Any();
        private void ExecuteAddSelected()
        {
            foreach (var o in RightSelectedItems)
            {
                switch (o)
                {
                    case RedFileViewModel fileVm:
                        AddFile(fileVm);
                        break;
                    case RedDirectoryViewModel dirVm:
                        AddFolderRecursive(dirVm.GetModel());
                        break;
                }
            }
        }


        public ICommand ToggleModBrowserCommand { get; private set; }
        private bool CanToggleModBrowser() => true;//_archiveManager.IsManagerLoaded;
        private void ExecuteToggleModBrowser()
        {
            if (!_isModBrowserActive)
            {
                _archiveManager.LoadModsArchives(new DirectoryInfo(_settings.GetRED4GameModDir()), null);
                LeftItems = new ObservableCollection<RedFileSystemModel>(_archiveManager.ModRoots);
            }
            else
            {
                LeftItems = new ObservableCollection<RedFileSystemModel>(_boundRootNodes);
            }

            RightItems = new ObservableCollection<IFileSystemViewModel>();
            _isModBrowserActive = !_isModBrowserActive;
        }

        public ICommand OpenFileLocationCommand { get; private set; }
        private bool CanOpenFileLocationCommand() => RightSelectedItems != null && RightSelectedItems.OfType<RedFileViewModel>().Any();
        private void ExecuteOpenFileLocationCommand()
        {
            if (RightSelectedItems.First() is not RedFileViewModel fileVm)
            {
                return;
            }

            var parentPath = fileVm.GetParentPath();
            var dir = _archiveManager.LookupDirectory(parentPath, true);
            if (dir != null)
            {
                MoveToFolder(dir);
            }
        }

        public ICommand TogglePreviewCommand { get; private set; }
        private bool CanTogglePreview() => true;
        private void ExecuteTogglePreview() =>
            PreviewWidth = PreviewWidth.GridUnitType != System.Windows.GridUnitType.Pixel
                ? new System.Windows.GridLength(0, System.Windows.GridUnitType.Pixel)
                : new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);

        public ICommand OpenFileSystemItemCommand { get; private set; }
        private bool CanOpenFile() => true;
        private void ExecuteOpenFile()
        {
            switch (RightSelectedItem)
            {
                case RedFileViewModel fileVm:
                    AddFile(fileVm);
                    break;
                case RedDirectoryViewModel dirVm:
                    MoveToFolder(dirVm);
                    break;
            }
        }

        public ReactiveCommand<Unit, Unit> ExpandAll { get; set; }
        public ReactiveCommand<Unit, Unit> CollapseAll { get; set; }
        public ReactiveCommand<Unit, Unit> Expand { get; set; }
        public ReactiveCommand<Unit, Unit> Collapse { get; set; }

        #endregion commands

        #region methods

        private void MoveToFolder(RedFileSystemModel dir) => LeftSelectedItem = dir;

        private void MoveToFolder(RedDirectoryViewModel dir) => LeftSelectedItem = dir.GetModel();

        private void AddFile(RedFileViewModel item) => Task.Run(() => _gameController.GetController().AddToMod(item.GetGameFile().Key));

        private void AddFile(ulong hash) => Task.Run(() => _gameController.GetController().AddToMod(hash));

        private void AddFolderRecursive(RedFileSystemModel item)
        {
            foreach (var dir in item.Directories)
            {
                AddFolderRecursive(dir);
            }
            foreach(var file in item.Files)
            {
                AddFile(file);
            }
        }

        /// <summary>
        /// Filters all game files by given keys or regex pattern
        /// </summary>
        /// <param name="query"></param>
        public void PerformSearch(string query)
        {
            if (IsRegexSearchEnabled)
            {
                RegexSearch();
            }
            else
            {
                KeywordSearch();
            }
        }

        /// <summary>
        /// Parses the search bar and filters all game files by given regex pattern
        /// Glob patterns from the additional search bar are evaluated first
        /// Sets the right hand filelist to the result
        /// </summary>
        public void RegexSearch()
        {
            var matcher = new Matcher();
            matcher.AddInclude(OptionsSearchBarText);

            var rx = new Regex(SearchBarText,
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            _archiveManager.Archives
                .Connect()
                .TransformMany(x => x.Files.Values, y => y.Key)
                .Filter(x => string.IsNullOrEmpty(OptionsSearchBarText) || matcher.Match(x.Name).HasMatches)
                .Filter(x => rx.IsMatch(x.Name))
                .Transform(x => new RedFileViewModel(x))
                .Bind(out var list)
                .Subscribe()
                .Dispose();

            RightItems.Clear();
            RightItems.AddRange(list);
        }

        /// <summary>
        /// Parses the search bar and filters all game files by given search keys
        /// Glob patterns from the additional search bar are evaluated first
        /// Sets the right hand filelist to the result
        /// </summary>
        public void KeywordSearch()
        {
            if (string.IsNullOrEmpty(SearchBarText))
            {
                return;
            }

            var inputs = SearchBarText.Split(' ');
            var keyDict = Enum
                .GetValues<ESearchKeys>()
                .ToDictionary(key => key, _ => new List<string>());
            keyDict[ESearchKeys.Limit] = new List<string>() { SEARCH_LIMIT.ToString() };

            // e.g. judy ext:mesh,ent test:xxx whatever
            // Name < judy,whatever
            // Ext < mesh,ent
            foreach (var item in inputs)
            {
                //check if keyword
                if (item.Contains(':'))
                {
                    var split = item.Split(':');
                    if (split.Length != 2)
                    {
                        // incorrect format -> disregard
                        continue;
                    }

                    var key = split[0].ToLower();
                    var value = split[1];
                    var names = Enum.GetNames<ESearchKeys>().Select(x => x.ToLower());
                    if (names.Contains(key))
                    {
                        var ekey = (ESearchKeys)Enum.Parse(typeof(ESearchKeys), key, true);
                        // get multiple
                        var multiple = value.Split(',').ToList();
                        // add to query
                        keyDict[ekey] = multiple;
                    }
                }
                else
                {
                    // default to filename search term
                    keyDict[ESearchKeys.Name].Add(item);
                }
            }


            // order from most specific to least
            // 0. Glob

            var matcher = new Matcher();
            matcher.AddInclude(OptionsSearchBarText);
            //var allfiles = _managers.First().Items
            //    .KeyValues.Select(x => x.Value.Name);
            //var match = matcher.Match(allfiles);
            //var debugresult = match.Files.Select(x => x.Path);

            // 1. Hash
            var qhashes = keyDict[ESearchKeys.Hash]
                .Where(x => ulong.TryParse(x, out _))
                .Select(ulong.Parse);
            // 2. Extension
            var qextensions = keyDict[ESearchKeys.Kind]
                .Where(x => Enum.TryParse<ERedExtension>(x, true, out _))
                .Select(x => $".{x}");
            // 3. Name
            var qnames = keyDict[ESearchKeys.Name];
            // 4. Limit
            if (!int.TryParse(keyDict[ESearchKeys.Limit].First(), out var limit))
            {
                limit = SEARCH_LIMIT;
            }

            _archiveManager.Archives
                .Connect()
                .TransformMany(x => x.Files.Values, y => y.Key)
                .Filter(x => string.IsNullOrEmpty(OptionsSearchBarText) || matcher.Match(x.Name).HasMatches)
                .Filter(x =>
                {
                    var enumerable = qhashes as ulong[] ?? qhashes.ToArray();
                    return !enumerable.Any() || enumerable.Contains(x.Key);
                })
                .Filter(x =>
                {
                    var enumerable = qextensions as string[] ?? qextensions.ToArray();
                    return !enumerable.Any() ||
                           enumerable.Any(y => y.Equals(x.Extension, StringComparison.OrdinalIgnoreCase));
                })
                .Filter(x => !qnames.Any() || qnames.Any(y => x.Name.Contains(y, StringComparison.OrdinalIgnoreCase)))
                .LimitSizeTo(limit)
                .Transform(x => new RedFileViewModel(x))
                .Bind(out var list)
                .Subscribe()
                .Dispose();

            RightItems.Clear();
            RightItems.AddRange(list);
        }

        public IGameFile LookupGameFile(ulong hash)
        {
            var f = _archiveManager.Lookup(hash);
            return f.HasValue ? f.Value : null;
        }

        #endregion methods
    }
}