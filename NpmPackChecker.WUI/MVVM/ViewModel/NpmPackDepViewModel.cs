using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using NpmPackChecker.WUI.Dto;
using NpmPackChecker.WUI.MVVM.Model;
using NpmPackChecker.WUI.Services;
using Semver;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace NpmPackChecker.WUI.MVVM.ViewModel
{
    public class NpmPackDepViewModel : ObservableRecipient
    {
        private readonly InfoBarService _infoBarService;
        private readonly NpmRegService _npmRegService;
        //private readonly DataStorageService _dataStorage;

        private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

        private string _registryUrl;
        public string RegistryUrl { get => _registryUrl; set => SetProperty(ref _registryUrl, value); }

        private string _pacNameVersion;
        public string PacNameVersion
        {
            get => _pacNameVersion;
            set
            {
                SetProperty(ref _pacNameVersion, value);
                OnAnalyze?.NotifyCanExecuteChanged();
            }
        }

        public RelayCommand OnOpenPackageJson { get; set; }
        public RelayCommand OnAnalyze { get; set; }
        public RelayCommand OnCopyAllNotFounded { get; set; }
        public RelayCommand OnSave { get; set; }
        public RelayCommand OnOpen { get; set; }
        public RelayCommand OnFilterByError { get; set; }

        public ObservableCollection<DepNodeView> DataSource { get; set; }
        private List<DepNodeView> DataSourceOrig { get; set; }
        //public List<string> TotalDeps;

        public DepNodeCounterView DepNodeCounterView { get; set; }

        private RichTextBlock RichTextBlockDepToImport;

        public NpmPackDepViewModel(
             InfoBarService infoBarService, NpmRegService npmRegService) // DataStorageService dataStorage,
        {
            _infoBarService = infoBarService;
            _npmRegService = npmRegService;
            //_dataStorage = dataStorage;

            RegistryUrl = "http://proxyp.dmzp.local/dmzart1/repository/npmjs/";
            //RegistryUrl = "https://registry.npmjs.org/";

            _npmRegService.SetRegistryUrl(RegistryUrl);

            PacNameVersion = "make-fetch-happen@9.1.0\rbl@4.1.0\r@angular/cli@12.1.4";
            PacNameVersion = "make-fetch-happen@9.1.0\rbl@4.1.0";
            PacNameVersion = "@isaacs/cliui@8.0.2";
            PacNameVersion = "nx@13.8.1";
            //PacNameVersion = "make-fetch-happen@9.1.0";

            DataSource = new();
            //TotalDeps = new();

            //SavedChecks = new();

            OnOpenPackageJson = new RelayCommand(async () =>
            {
                var openPicker = new FileOpenPicker();
                var window = App.MainWindow;
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

                openPicker.ViewMode = PickerViewMode.Thumbnail;
                openPicker.FileTypeFilter.Add(".json");

                Windows.Storage.StorageFile file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    var jsonString = File.ReadAllText(file.Path);

                    PacNameVersion = "";

                    using JsonDocument document = JsonDocument.Parse(jsonString);

                    var isDependencies = document.RootElement.TryGetProperty("dependencies", out var dependencies);
                    if (isDependencies)
                    {
                        using JsonDocument dependenciesDoc = JsonDocument.Parse(dependencies.Clone().ToString());
                        foreach (JsonProperty property in dependenciesDoc.RootElement.EnumerateObject())
                        {
                            var pack = property.Name.ToString();
                            var vers = property.Value.ToString();
                            var depToCheck = $"{pack}@{vers}\r";

                            if (!PacNameVersion.Contains(depToCheck))
                                PacNameVersion += depToCheck;
                        }
                    }
                    else
                        _infoBarService.Show("Раздел dependencies не найден");

                    var isDevDependencies = document.RootElement.TryGetProperty("devDependencies", out var devDependencies);
                    if (isDevDependencies)
                    {
                        using JsonDocument devDependenciesDoc = JsonDocument.Parse(devDependencies.Clone().ToString());
                        foreach (JsonProperty property in devDependenciesDoc.RootElement.EnumerateObject())
                        {
                            var pack = property.Name.ToString();
                            var vers = property.Value.ToString();
                            var depToCheck = $"{pack}@{vers}\r";

                            if (!PacNameVersion.Contains(depToCheck))
                                PacNameVersion += depToCheck;
                        }
                    }
                    else
                        _infoBarService.Show("Раздел devDependencies не найден");
                }
            });

            OnAnalyze = new RelayCommand(
                async () =>
                {
                    _npmRegService.SetRegistryUrl(RegistryUrl);

                    _dispatcherQueue.TryEnqueue(() => RichTextBlockDepToImport.Blocks.Clear());
                    _dispatcherQueue.TryEnqueue(() => IsLoading = true);
                    await ViewDeps(PacNameVersion.Split("\r", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
                    await StartCheckDepsInRegistry();
                    DataSourceOrig = new(DataSource);
                    StartFeedRichTextBlockDepToImport();
                    _dispatcherQueue.TryEnqueue(() => IsLoading = false);

                    OnSave.NotifyCanExecuteChanged();
                    OnOpen.NotifyCanExecuteChanged();
                    OnCopyAllNotFounded.NotifyCanExecuteChanged();
                },
                () => !string.IsNullOrEmpty(PacNameVersion.Trim()));

            OnCopyAllNotFounded = new RelayCommand(
                () =>
                {
                    var saved = new List<string>();
                    StringBuilder sb = new();
                    foreach (var item in DataSource)
                        GetAllErrorPackagesStr(item, sb, saved);

                    var s = sb.ToString();
                    DataPackage dataPackage = new();
                    dataPackage.RequestedOperation = DataPackageOperation.Copy;
                    dataPackage.SetText(sb.ToString());
                    Clipboard.SetContent(dataPackage);
                },
                () => DataSource.Any());

            OnSave = new RelayCommand(
                async () =>
                {
                    if (!DataSource.Any()) return;

                    FileSavePicker savePicker = new FileSavePicker();
                    var window = App.MainWindow;
                    var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);

                    savePicker.SuggestedStartLocation = PickerLocationId.Desktop;
                    savePicker.FileTypeChoices.Add("JSON", new List<string>() { ".json" });
                    var date = DateTime.Today.ToString("dd-MM-yyyy");
                    var fileName = DataSource.Count > 1
                        ? $"packajejson_{date}"
                        : $"{DataSource.First().Title.Replace(' ', '_')}_{date}";
                    savePicker.SuggestedFileName = fileName;

                    StorageFile file = await savePicker.PickSaveFileAsync();
                    if (file != null)
                    {
                        var txt = JsonSerializer.Serialize(DataSource);
                        await File.WriteAllTextAsync(file.Path, txt);
                    }
                },
                () => DataSource.Any());

            OnOpen = new RelayCommand(
                async () =>
                {
                    var openPicker = new FileOpenPicker();
                    var window = App.MainWindow;
                    var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

                    openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
                    openPicker.ViewMode = PickerViewMode.Thumbnail;
                    openPicker.FileTypeFilter.Add(".json");

                    StorageFile file = await openPicker.PickSingleFileAsync();
                    if (file != null)
                    {
                        var jsonString = File.ReadAllText(file.Path);
                        var obj = JsonSerializer.Deserialize<List<DepNodeView>>(jsonString);
                        DataSourceOrig = obj;

                        DataSource = [.. obj];
                        OnPropertyChanged(nameof(DataSource));

                        OnSave.NotifyCanExecuteChanged();
                        OnOpen.NotifyCanExecuteChanged();
                        OnCopyAllNotFounded.NotifyCanExecuteChanged();

                        DepNodeCounterView = new();
                        OnPropertyChanged(nameof(DepNodeCounterView));
                        StartFeedRichTextBlockDepToImport();
                    }
                },
                () => !IsLoading);

            OnFilterByError = new RelayCommand(() =>
            {
                FilterTreeByStatus(DepStateType.Error | DepStateType.NotFounded);
            });
        }

        public void Init(RichTextBlock richTextBlockDepToImport)
        {
            RichTextBlockDepToImport = richTextBlockDepToImport;
        }

        private void GetAllErrorPackagesStr(DepNodeView item, StringBuilder sb, List<string> saved)
        {
            var key = $"{item.Title}@{item.TrueVersion}";
            if (item.State != DepStateType.Founded && saved.Find(x => x == key) == null)
            {
                var str = $"{item.Title}@{item.TrueVersion} {item.TrueVersionDate:yyyy-MM-dd}";
                sb.AppendLine(str);
                saved.Add(key);
            }

            foreach (var item2 in item.Dependencies)
                GetAllErrorPackagesStr(item2, sb, saved);
        }

        private async Task ViewDeps(string[] arr)
        {
            List<DepNodeView> depsToCheck = [];
            foreach (string dep in arr)
            {
                var index = dep.LastIndexOf('@');
                var pack = dep[..index];
                var version = dep[(index + 1)..];
                depsToCheck.Add(new DepNodeView(pack, version));
            }

            DepNodeCounterView = new();
            OnPropertyChanged(nameof(DepNodeCounterView));

            DataSource = new();
            OnPropertyChanged(nameof(DataSource));

            //TotalDeps = new();

            foreach (var item in depsToCheck)
            {
                List<string> alreadyChecked = new();

                var depNodeView = new DepNodeView(item.Title, item.DepVersion);

                var packInfo = await GetPackInfoBase(item.Title, depNodeView);

                if (packInfo == null)
                {
                    _infoBarService.Show($"Пакет '{item.Title}' не найден");
                    return;
                }

                var isVersionFounded = GetAndCheckVersion(
                    item.DepVersion, packInfo.Versions, packInfo.DistTags,
                    out var needVersion);

                if (!isVersionFounded && !depNodeView.FromDefault)
                {
                    packInfo = await GetPackInfoBase(item.Title, depNodeView, true);

                    isVersionFounded = GetAndCheckVersion(
                        item.DepVersion, packInfo.Versions, packInfo.DistTags,
                        out needVersion);
                }

                //if (!TotalDeps.Any(x => x == DepNodeView.Title))
                //    TotalDeps.Add(DepNodeView.Title);

                if (isVersionFounded)
                {
                    depNodeView.TrueVersion = needVersion.Version;
                    packInfo.Time.TryGetValue(needVersion.Version, out var trueVersionDate);
                    depNodeView.TrueVersionDate = trueVersionDate;
                    depNodeView.TarballUrl = needVersion.Dist.Tarball;

                    DepNodeCounterView.TotalDeps++;
                    OnPropertyChanged(nameof(DepNodeCounterView));

                    DataSource.Add(depNodeView);

                    alreadyChecked.Add(depNodeView.ViewTitle);
                    await LoadDependencies(depNodeView, needVersion.Dependencies, alreadyChecked);
                }
                else
                {
                    depNodeView.State = DepStateType.Error;
                    depNodeView.ErrorText = "Искомая версия не найдена";
                    DepNodeCounterView.TotalError++;
                    OnPropertyChanged(nameof(DepNodeCounterView));
                    _infoBarService.Show($"Версия '{item.DepVersion}' не найдена");
                }

                //DepNodeView.TotalDeps = _tempoTotalDeps.Distinct().ToList();
                OnSave.NotifyCanExecuteChanged();
            }
        }
        private async Task LoadDependencies(
            DepNodeView root, Dictionary<string, string> dependencies, List<string> alreadyChecked)
        {
            foreach (var item in dependencies)
            {
                var pack = item.Key;
                var version = item.Value;

                // обрабокта пакетов типа 'string-width-cjs': 'npm:string-width@^4.2.0'
                if (pack.Contains("-cjs") && version.StartsWith("npm:") && version.Contains("@"))
                {
                    var index1 = version.LastIndexOf('@');
                    var packNew = version.Substring(4, index1 - 4);
                    var versionNew = version[(index1 + 1)..];

                    pack = packNew;
                    version = versionNew;
                }

                var chDep = new DepNodeView(pack, version, root);

                var packInfo = await GetPackInfoBase(pack, chDep);

                if (packInfo == null)
                {
                    chDep.State = DepStateType.Error;
                    chDep.ErrorText = "Инфрмация о пакете не найдена";
                    DepNodeCounterView.TotalError++;
                    OnPropertyChanged(nameof(DepNodeCounterView));
                    _infoBarService.Show($"Пакет '{pack}' не найден");

                    root.Dependencies.Add(chDep);
                }
                else
                {
                    var isVersionFounded = GetAndCheckVersion(
                        version, packInfo.Versions, packInfo.DistTags,
                        out var needVersion);

                    if (!isVersionFounded && !chDep.FromDefault)
                    {
                        packInfo = await GetPackInfoBase(pack, chDep, true);

                        isVersionFounded = GetAndCheckVersion(
                          version, packInfo.Versions, packInfo.DistTags,
                          out needVersion);
                    }

                    if (isVersionFounded)
                    {
                        chDep.TrueVersion = needVersion.Version;
                        packInfo.Time.TryGetValue(needVersion.Version, out var trueVersionDate);
                        chDep.TrueVersionDate = trueVersionDate;
                        chDep.TarballUrl = needVersion.Dist.Tarball;

                        DepNodeCounterView.TotalDeps++;
                        OnPropertyChanged(nameof(DepNodeCounterView));

                        root.Dependencies.Add(chDep);

                        var isAlreadyChecked = alreadyChecked.FirstOrDefault(x => x == chDep.ViewTitle) != null;
                        if (!isAlreadyChecked)
                        {
                            alreadyChecked.Add(chDep.ViewTitle);
                            await LoadDependencies(chDep, needVersion.Dependencies, alreadyChecked);
                        }

                        //if (needVersion.Dependencies != null)
                        //{
                        //    if (alreadyChecked.FirstOrDefault(x => x == chDep.ViewTitle) == null)
                        //        await LoadDependencies(chDep, needVersion.Dependencies, alreadyChecked);

                        //    alreadyChecked.Add(chDep.ViewTitle);
                        //}

                        //if (!TotalDeps.Any(x => x == chDep.Title))
                        //    TotalDeps.Add(chDep.Title);
                    }
                    else
                    {
                        chDep.State = DepStateType.Error;
                        chDep.ErrorText = "Искомая версия не найдена";
                        DepNodeCounterView.TotalError++;
                        OnPropertyChanged(nameof(DepNodeCounterView));

                        root.Dependencies.Add(chDep);
                    }
                }

                //root.Dependencies.Add(chDep);

                //OnPropertyChanged(nameof(DepNodeView));
                //OnPropertyChanged(nameof(DataSource));

                //DataSource.RemoveAt(0);
                //DataSource.Add(DepNodeView);
            }
        }


        private async Task<PackDetailDto> GetPackInfoBase(string pack, DepNodeView chDep, bool fromDefault = false)
        {
            if (fromDefault)
            {
                var packInfo = await _npmRegService.GetPackInfoBase(pack, NpmChekType.Default);
                chDep.FromDefault = true;
                chDep.State = DepStateType.NotFounded;

                return packInfo.Data;
            }
            else
            {
                var packInfo = await _npmRegService.GetPackInfoBase(pack);
                if (packInfo.Data == null)
                {
                    return await GetPackInfoBase(pack, chDep, true);
                }

                return packInfo.Data;
            }
        }

        /// <summary>
        /// https://dev-scout.hashnode.dev/decoding-version-numbers-how-semver-helps-versioning-a-software
        /// </summary>
        /// <param name="searchVersion"></param>
        /// <param name="versions"></param>
        /// <param name="distTags"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        private bool GetAndCheckVersion(
            string searchVersion, Dictionary<string, VersionDto> versions, DistTagsDto distTags, out VersionDto version)
        {
            try
            {
                if (searchVersion == null)
                {
                    version = null;
                    return false;
                }

                if (searchVersion.Equals("*"))
                {
                    if (distTags.Latest != null)
                    {
                        searchVersion = distTags.Latest;
                    }
                }

                var mapVersion = searchVersion;

                var versionKeyesStr = string.Join(" || ", versions.Select(x => x.Key));
                var totalVersions = SemVersionRange.ParseNpm(versionKeyesStr, false, versionKeyesStr.Length);

                var isDone = SemVersionRange.TryParseNpm(searchVersion, false, out var prereleaseRange);

                if (isDone)
                {
                    var first = prereleaseRange.First();
                    if (first.Start != null)
                    {
                        var totalVersionsFiltered = totalVersions.Where(x => first.Contains(x.Start)).ToList();
                        var totalVersionsFilteredRange = SemVersionRange.Create(totalVersionsFiltered);

                        var resVersion = totalVersionsFilteredRange.Max(x => x.Start);
                        if (!string.IsNullOrEmpty(distTags.Latest))
                        {
                            var isDoneLatest = SemVersionRange.TryParseNpm(distTags.Latest, false, out var latest);
                            if (isDoneLatest)
                            {
                                var res = totalVersionsFilteredRange.Where(x => latest.Contains(x.Start)).ToList();
                                if (res.Count != 0)
                                {
                                    resVersion = res.First().ToString();
                                }
                            }
                        }

                        mapVersion = resVersion.ToString();
                    }
                }
                else
                {
                    _infoBarService.Show($"версия '{searchVersion}' не найдена в спсике");
                }

                var isVersionFounded = versions.TryGetValue(mapVersion, out var needVersion2);
                version = needVersion2;
                return isVersionFounded;
            }
            catch (Exception)
            {
                version = null;
                return false;
            }
        }

        private async Task StartCheckDepsInRegistry()
        {
            if (DataSource == null) return;
            foreach (var item in DataSource)
                await CheckDepsInRegistry(item);
        }
        private async Task CheckDepsInRegistry(DepNodeView item)
        {
            try
            {
                if (item.State != DepStateType.Error && item.State != DepStateType.NotFounded)
                {
                    var check = await _npmRegService.CheckPackage(item.TarballUrl);
                    if (check)
                    {
                        item.SetState(DepStateType.Founded);
                        DepNodeCounterView.TotalFounded++;
                        OnPropertyChanged(nameof(DepNodeCounterView));
                    }
                    else
                    {
                        item.SetState(DepStateType.NotFounded);
                        //DepNodeCounterView.TotalNotFound++;
                        OnPropertyChanged(nameof(DepNodeCounterView));
                    }
                }

                foreach (var itemDep in item.Dependencies)
                    await CheckDepsInRegistry(itemDep);
            }
            catch (Exception)
            {
                item.SetState(DepStateType.Error);
                item.ErrorText = "При проврки наличия пакета в репозитории инфрмация не найдена";
                DepNodeCounterView.TotalError++;
                OnPropertyChanged(nameof(DepNodeCounterView));
            }
        }

        private void StartFeedRichTextBlockDepToImport()
        {
            if (DataSource == null) return;
            List<string> visited = new();
            foreach (var item in DataSource)
                StartFeedRichTextBlockDepToImportReq(item, visited);

        }
        private void StartFeedRichTextBlockDepToImportReq(DepNodeView item, List<string> visited)
        {
            if (item.State == DepStateType.NotFounded)
            {
                var text = $"{item.Title}@{item.TrueVersion} {item.TrueVersionDate:yyyy-MM-dd}";
                if (!visited.Contains(text))
                {
                    DepNodeCounterView.TotalNotFound++;
                    OnPropertyChanged(nameof(DepNodeCounterView));
                    visited.Add(text);

                    Paragraph para = new();
                    para.Inlines.Add(new Run { Text = text, FontSize = 15 });
                    _dispatcherQueue.TryEnqueue(() => RichTextBlockDepToImport.Blocks.Add(para));
                }
            }

            foreach (var itemDep in item.Dependencies)
                StartFeedRichTextBlockDepToImportReq(itemDep, visited);
        }

        #region Filters

        public void FilterTree(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                if (DataSourceOrig.Count != 0)
                {
                    DataSource = [.. DataSourceOrig];
                    OnPropertyChanged(nameof(DataSource));
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(searchText.Trim()) && searchText.Length >= 3) { }

                DataSourceOrig = [.. DataSource];

                List<DepNodeView> dataSourceClone = new();

                foreach (var item in DataSource)
                {
                    var clone = item.Clone();
                    clone.Dependencies = FilterTreeCycle(clone.Dependencies, searchText);

                    dataSourceClone.Add(clone);
                }

                DataSource = [.. dataSourceClone];
                OnPropertyChanged(nameof(DataSource));
            }
        }
        private ObservableCollection<DepNodeView> FilterTreeCycle(ObservableCollection<DepNodeView> dep, string SearchText)
        {
            foreach (var item in dep)
                item.Dependencies = FilterTreeCycle(item.Dependencies, SearchText);
            return new(dep.Where(x => x.ViewTitle.Contains(SearchText) || x.Dependencies.Any()));
        }

        public void FilterTreeByStatus(params DepStateType[] status)
        {
            if (DataSource == null) return;

            DataSourceOrig = [.. DataSource];

            List<DepNodeView> dataSourceClone = new();

            foreach (var item in DataSource)
            {
                var clone = item.Clone();
                clone.Dependencies = FilterTreByStatuseCycle(clone.Dependencies, status);

                dataSourceClone.Add(clone);
            }

            DataSource = [.. dataSourceClone];
            OnPropertyChanged(nameof(DataSource));
        }
        private ObservableCollection<DepNodeView> FilterTreByStatuseCycle(ObservableCollection<DepNodeView> dep, params DepStateType[] status)
        {
            foreach (var item in dep)
                item.Dependencies = FilterTreByStatuseCycle(item.Dependencies, status);
            return new(dep.Where(x => status.Contains(x.State) || x.Dependencies.Any()));
        }

        #endregion Filters
    }
}
