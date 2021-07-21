using NPS;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace NPSLibrary
{
    [Export(typeof(IMainViewModel))]
    public class MainViewModel : ViewModelBase, IMainViewModel
    {
        private readonly Regex invalidCharactersRegex;

        public class NPSTitle
        {
            public string Title { get; set; } = string.Empty;

            public string TitleId { get; set; } = string.Empty;

            public string? ImageUrl { get; set; }

            public string ContentType { get; set; } = string.Empty;

            public string Path { get; set; } = string.Empty;
        }

        public CollectionView ItemsView { get; private set; }

        public ObservableCollection<NPSTitle> Items
        {
            get { return _Items; }
            set
            {
                if (_Items != value)
                {
                    _Items = value;
                    RaisePropertyChanged(ItemsPropertyName);
                }
            }
        }
        private ObservableCollection<NPSTitle> _Items = new();
        public const string ItemsPropertyName = "Items";

        public string Status
        {
            get => _Status;
            set
            {
                if (_Status != value)
                {
                    _Status = value;
                    RaisePropertyChanged(StatusPropertyName);
                }
            }
        }
        private string _Status = string.Empty;
        public const string StatusPropertyName = "Status";

        public bool IsVITAChecked
        {
            get { return _IsVITAChecked; }
            set
            {
                if (_IsVITAChecked != value)
                {
                    _IsVITAChecked = value;
                    RaisePropertyChanged(IsVITACheckedPropertyName);
                    UpdateFilterText(_FilterText);
                }
            }
        }
        private bool _IsVITAChecked = true;
        public const string IsVITACheckedPropertyName = "IsVITAChecked";

        public bool IsPSPChecked
        {
            get { return _IsPSPChecked; }
            set
            {
                if (_IsPSPChecked != value)
                {
                    _IsPSPChecked = value;
                    RaisePropertyChanged(IsPSPCheckedPropertyName);
                    UpdateFilterText(_FilterText);
                }
            }
        }
        private bool _IsPSPChecked = true;
        public const string IsPSPCheckedPropertyName = "IsPSPChecked";

        public bool IsPSMChecked
        {
            get { return _IsPSMChecked; }
            set
            {
                if (_IsPSMChecked != value)
                {
                    _IsPSMChecked = value;
                    RaisePropertyChanged(IsPSMCheckedPropertyName);
                    UpdateFilterText(_FilterText);
                }
            }
        }
        private bool _IsPSMChecked = true;
        public const string IsPSMCheckedPropertyName = "IsPSMChecked";

        public bool IsPS3Checked
        {
            get { return _IsPS3Checked; }
            set
            {
                if (_IsPS3Checked != value)
                {
                    _IsPS3Checked = value;
                    RaisePropertyChanged(IsPS3CheckedPropertyName);
                    UpdateFilterText(_FilterText);

                }
            }
        }
        private bool _IsPS3Checked = true;
        public const string IsPS3CheckedPropertyName = "IsPS3Checked";

        public bool IsPSXChecked
        {
            get { return _IsPSXChecked; }
            set
            {
                if (_IsPSXChecked != value)
                {
                    _IsPSXChecked = value;
                    RaisePropertyChanged(IsPSXCheckedPropertyName);
                    UpdateFilterText(_FilterText);
                }
            }
        }
        private bool _IsPSXChecked = true;
        public const string IsPSXCheckedPropertyName = "IsPSXChecked";

        public string FilterText
        {
            get { return _FilterText; }
            set
            {
                if (_FilterText != value)
                {
                    _FilterText = value;
                    RaisePropertyChanged(FilterTextPropertyName);
                    UpdateFilterText(_FilterText);
                }
            }
        }
        private string _FilterText = string.Empty;
        public const string FilterTextPropertyName = "FilterText";

        public ICommand CopyCommand { get; set; }
        public ICommand OpenDirectoryCommand { get; set; }
        public ICommand YouTubeCommand { get; set; }

        public MainViewModel()
        {
            CopyCommand = new AsyncRelayCommand(HandleCopyCommand);
            OpenDirectoryCommand = new AsyncRelayCommand(HandleOpenDirectoryCommand);
            YouTubeCommand = new AsyncRelayCommand(HandleYouTubeCommand);

            ItemsView = (CollectionView)CollectionViewSource.GetDefaultView(_Items);
            ItemsView.SortDescriptions.Add(new SortDescription("Title", ListSortDirection.Ascending));

            var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()));
            invalidCharactersRegex = new Regex($"[{invalidChars}]", RegexOptions.Compiled);
        }

        public async Task Load()
        {
            Status = "Loading...";

            await Task.Delay(1);

            Settings.Load();

            var rootDirectory = new[] { Settings.Instance.downloadDir };

            var paths = ConfigurationManager.AppSettings["AdditionalPaths"];
            if (paths != null)
            {
                var additionalPaths = paths.Split(';');
                if (additionalPaths != null)
                {
                    rootDirectory = rootDirectory.Concat(additionalPaths).ToArray();
                }
            }

            var vitaAppDirectories = rootDirectory.Select(r => Path.Combine(r, "app")).ToArray();
            var packagesDirectories = rootDirectory.Select(r => Path.Combine(r, "packages")).ToArray();
            var pspemuISODirectories = rootDirectory.Select(r => Path.Combine(r, "pspemu", "ISO")).ToArray();
            var pspemuGAMEDirectories = rootDirectory.Select(r => Path.Combine(r, "pspemu", "GAME")).ToArray();

            var imageDatabase = NPCache.I.renasceneCache
                .ToLookup(i => i.itm.TitleId, i => i);

            var titleIdDatabase = NPCache.I.localDatabase
                .Where(i => !i.IsDLC && !i.IsTheme && !i.IsAvatar)
                .ToLookup(i => i.TitleId, i => i);

            var nameDatabase = NPCache.I.localDatabase
                .Where(i => !i.IsDLC && !i.IsTheme && !i.IsAvatar)
                .Where(i => i.ItsPS3 || i.ItsPS4 || !i.ItsPsp)
                .ToLookup(i => FriendlyFilename(i), i => i);

            var vitaApplicationPaths = vitaAppDirectories
                .Where(d => Directory.Exists(d))
                .SelectMany(d => Directory.GetDirectories(d, string.Empty, SearchOption.TopDirectoryOnly))
                .ToArray();

            foreach (var vitaAppPath in vitaApplicationPaths)
            {
                var directoryInfo = new DirectoryInfo(vitaAppPath);
                var directoryName = directoryInfo.Name;

                if (titleIdDatabase.Contains(directoryName))
                {
                    var item = titleIdDatabase[directoryName].First();
                    var url = imageDatabase[item.TitleId].FirstOrDefault()?.imgUrl;
                    AddNPSTitleItem(vitaAppPath, item, url);
                }
            }

            var packagePaths = packagesDirectories
                .Where(d => Directory.Exists(d))
                .SelectMany(d => Directory.GetFiles(d, "*.pkg", SearchOption.TopDirectoryOnly))
                .ToArray();

            foreach (var packagePath in packagePaths)
            {
                var fileName = Path.GetFileNameWithoutExtension(packagePath);

                if (nameDatabase.Contains(fileName))
                {
                    var item = nameDatabase[fileName].First();
                    var url = imageDatabase[item.TitleId].FirstOrDefault()?.imgUrl;
                    AddNPSTitleItem(packagePath, item, url);
                }
            }

            var pspemuISOPaths = pspemuISODirectories
                .Where(d => Directory.Exists(d))
                .SelectMany(d => Directory.GetFiles(d, "*.iso", SearchOption.TopDirectoryOnly))
                .ToArray();

            foreach (var pspemuISOPath in pspemuISOPaths)
            {
                var fileName = Path.GetFileNameWithoutExtension(pspemuISOPath);
                var titleIdRegex = Regex.Match(fileName, @"\[([A-Z0-9]+)\]");
                if (titleIdRegex.Success)
                {
                    var titleId = titleIdRegex.Groups[1].Value;
                    if (titleIdDatabase.Contains(titleId))
                    {
                        var item = titleIdDatabase[titleId].First();
                        var url = imageDatabase[item.TitleId].FirstOrDefault()?.imgUrl;
                        AddNPSTitleItem(pspemuISOPath, item, url);
                    }
                }
            }

            var pspemuGAMEPaths = pspemuGAMEDirectories
                .Where(d => Directory.Exists(d))
                .SelectMany(d => Directory.GetDirectories(d, string.Empty, SearchOption.TopDirectoryOnly))
                .ToArray();

            foreach (var pspemuGAMEPath in pspemuGAMEPaths)
            {
                var directoryInfo = new DirectoryInfo(pspemuGAMEPath);
                var titleId = directoryInfo.Name;

                if (titleIdDatabase.Contains(titleId))
                {
                    var item = titleIdDatabase[titleId].First();
                    var url = imageDatabase[item.TitleId].FirstOrDefault()?.imgUrl;
                    AddNPSTitleItem(pspemuGAMEPath, item, url);
                }
            }

            Status = $"Loaded {Items.Count} titles";
        }

        private Task HandleCopyCommand(object? arg)
        {
            if (arg is NPSTitle npsTitle)
            {
                StringCollection paths = new();
                paths.Add(npsTitle.Path);
                Clipboard.SetFileDropList(paths);
            }

            return Task.FromResult(0);
        }

        private Task HandleOpenDirectoryCommand(object? arg)
        {
            if (arg is NPSTitle npsTitle)
            {
                Process.Start("explorer.exe", "/select, " + npsTitle.Path);
            }

            return Task.FromResult(0);
        }

        private Task HandleYouTubeCommand(object? arg)
        {
            if (arg is NPSTitle npsTitle)
            {
                var searchUrl = "https://www.youtube.com/results?search_query=" + WebUtility.UrlEncode($"{npsTitle.ContentType} trailer {npsTitle.Title}");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {searchUrl}"));
            }
            ;
            return Task.FromResult(0);
        }

        private void AddNPSTitleItem(string path, Item item, string? imageUrl)
        {
            var npsTitle = new NPSTitle()
            {
                Title = item.TitleName,
                TitleId = item.TitleId,
                ContentType = item.contentType,
                Path = path,
                ImageUrl = imageUrl,
            };

            Items.Add(npsTitle);
        }

        private string FriendlyFilename(Item item)
        {
            var text = item.ItsPS3 || item.ItsCompPack
                ? item.TitleName
                : item.ContentId ?? item.TitleId;

            if (!string.IsNullOrEmpty(item.offset))
            {
                text = text + "_" + item.offset;
            }

            return invalidCharactersRegex.Replace(text, string.Empty);
        }

        private void UpdateFilterText(string filterText)
        {
            ItemsView.Filter = _ => _ is not NPSTitle item || FilterNSPTitle(item);
            ItemsView.Refresh();

            bool FilterNSPTitle(NPSTitle item)
            {
                if (!string.IsNullOrEmpty(filterText))
                {
                    var hasFilterText = item.Title.Contains(filterText, StringComparison.OrdinalIgnoreCase) || item.TitleId.Contains(filterText, StringComparison.OrdinalIgnoreCase);
                    if (!hasFilterText)
                        return false;
                }

                if (!_IsVITAChecked && item.ContentType == "VITA")
                    return false;

                if (!_IsPSPChecked && item.ContentType == "PSP")
                    return false;

                if (!_IsPSMChecked && item.ContentType == "PSM")
                    return false;

                if (!_IsPS3Checked && item.ContentType == "PS3")
                    return false;

                if (!_IsPSXChecked && item.ContentType == "PSX")
                    return false;

                return true;
            }
        }
    }
}