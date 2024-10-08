﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NpmPackChecker.WUI.Dto;
using NpmPackChecker.WUI.MVVM.Model;
using NpmPackChecker.WUI.Services;
using Semver;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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

        public RelayCommand OnLoadPackage { get; set; }
        public RelayCommand OnAnalyze { get; set; }

        public RelayCommand OnSave { get; set; }
        public RelayCommand OnRemove { get; set; }

        public RelayCommand OnFilterByError { get; set; }

        //public DepNodeView DepNodeView { get; set; }
        public ObservableCollection<DepNodeView> DataSource { get; set; }
        private List<DepNodeView> DataSourceOrig { get; set; }
        public List<string> TotalDeps;

        //public ObservableCollection<SavedNpmChecks> SavedChecks { get; set; }
        //private SavedNpmChecks _selectedSavedChecks;
        //public SavedNpmChecks SelectedSavedChecks
        //{
        //    get { return _selectedSavedChecks; }
        //    set
        //    {
        //        SetProperty(ref _selectedSavedChecks, value);
        //        if (DataSource.Count > 0) DataSource.RemoveAt(0);
        //        if (SelectedSavedChecks != null)
        //        {
        //            PacName = SelectedSavedChecks.Package;
        //            PacVersion = SelectedSavedChecks.Version;

        //            DataSource.Add(SelectedSavedChecks.DepNodeView);
        //            DepNodeView = SelectedSavedChecks.DepNodeView;
        //            DepNodeCounterView = new(SelectedSavedChecks.DepNodeView);
        //        }
        //        else
        //        {
        //            DepNodeCounterView = new();
        //        }
        //        OnPropertyChanged(nameof(DepNodeCounterView));
        //        OnRemove.NotifyCanExecuteChanged();
        //    }
        //}

        public DepNodeCounterView DepNodeCounterView { get; set; }

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

            DataSource = new();
            TotalDeps = new();

            //SavedChecks = new();

            OnLoadPackage = new RelayCommand(async () =>
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
                    _dispatcherQueue.TryEnqueue(() => IsLoading = true);
                    await ViewDeps(PacNameVersion.Split("\r", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
                    _dispatcherQueue.TryEnqueue(() => IsLoading = false);
                    await StartCheckDepsInRegistry();
                },
                () => !string.IsNullOrEmpty(PacNameVersion.Trim()));

            OnSave = new RelayCommand(async () =>
                {
                    //if (DepNodeView == null) return;

                    //var savedNpmChecks = new SavedNpmChecks(DepNodeView);

                    //var check = SavedChecks.FirstOrDefault(x => x.ViewTitle == savedNpmChecks.ViewTitle);
                    //if (check != null)
                    //    SavedChecks.Remove(check);

                    //SavedChecks.Add(savedNpmChecks);
                    //await dataStorage.SetByKey(SavedChecks, nameof(SavedNpmChecks), true);
                }); // () => DepNodeView != null)

            OnRemove = new RelayCommand(async () =>
                {
                    //var find = SavedChecks
                    //    .FirstOrDefault(x =>
                    //        x.Package == SelectedSavedChecks.Package
                    //        && x.Version == SelectedSavedChecks.Version
                    //        && x.Date == SelectedSavedChecks.Date);
                    //SavedChecks.Remove(find);
                    //await dataStorage.SetByKey(SavedChecks, nameof(SavedNpmChecks), true);
                    //SelectedSavedChecks = null;

                    //OnSave.NotifyCanExecuteChanged();
                    //OnRemove.NotifyCanExecuteChanged();
                }); // () => SelectedSavedChecks != null

            OnFilterByError = new RelayCommand(() =>
            {
                FilterTreeByStatus(DepStateType.Error | DepStateType.NotFounded);
            });

            //Task.Run(() =>
            //{
            //    var saved = dataStorage.GetByKey<List<SavedNpmChecks>>(nameof(SavedNpmChecks));
            //    if (saved != null)
            //        SavedChecks = new(saved);
            //});
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

            TotalDeps = new();

            foreach (var item in depsToCheck)
            {
                List<string> alreadyChecked = new();

                var DepNodeView = new DepNodeView(item.Title, item.DepVersion);

                var packInfo = await _npmRegService.GetPackInfoBase(item.Title);
                if (packInfo == null)
                {
                    packInfo = await _npmRegService.GetPackInfoBase(item.Title, NpmChekType.Default);
                    DepNodeView.FromDefault = true;
                    DepNodeView.State = DepStateType.NotFounded;
                }

                if (packInfo == null)
                {
                    _infoBarService.Show($"Пакет '{item.Title}' не найден");
                    return;
                }

                var isVersionFounded = GetAndCheckVersion(
                    item.DepVersion, packInfo.Versions, packInfo.DistTags,
                    out var needVersion);

                alreadyChecked.Add(DepNodeView.ViewTitle);

                if (!TotalDeps.Any(x => x == DepNodeView.Title))
                    TotalDeps.Add(DepNodeView.Title);

                if (isVersionFounded)
                {
                    DepNodeView.TrueVersion = needVersion.Version;
                    packInfo.Time.TryGetValue(needVersion.Version, out var trueVersionDate);
                    DepNodeView.TrueVersionDate = trueVersionDate;
                    DepNodeView.TarballUrl = needVersion.Dist.Tarball;

                    DepNodeCounterView.TotalDeps++;
                    OnPropertyChanged(nameof(DepNodeCounterView));

                    DataSource.Add(DepNodeView);

                    await LoadDeps(DepNodeView, needVersion.Dependencies, alreadyChecked);
                }
                else
                {
                    DepNodeView.State = DepStateType.Error;
                    DepNodeCounterView.TotalError++;
                    OnPropertyChanged(nameof(DepNodeCounterView));
                    _infoBarService.Show($"Версия '{item.DepVersion}' не найдена");
                }

                //DepNodeView.TotalDeps = _tempoTotalDeps.Distinct().ToList();
                OnSave.NotifyCanExecuteChanged();
            }
        }
        private async Task LoadDeps(
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
                    var pack1 = version.Substring(4, index1 - 4);
                    var version1 = version[(index1 + 1)..];

                    pack = pack1;
                    version = version1;
                }

                var chDep = new DepNodeView(pack, version, root);

                var packInfo = await _npmRegService.GetPackInfoBase(pack);

                if (packInfo == null)
                {
                    packInfo = await _npmRegService.GetPackInfoBase(pack, NpmChekType.Default);
                    chDep.FromDefault = true;
                    chDep.State = DepStateType.NotFounded;
                }

                if (packInfo != null)
                {
                    var isVersionFounded = GetAndCheckVersion(
                        version, packInfo.Versions, packInfo.DistTags,
                        out var needVersion);

                    if (isVersionFounded)
                    {
                        chDep.TrueVersion = needVersion.Version;
                        packInfo.Time.TryGetValue(needVersion.Version, out var trueVersionDate);
                        chDep.TrueVersionDate = trueVersionDate;
                        chDep.TarballUrl = needVersion.Dist.Tarball;

                        DepNodeCounterView.TotalDeps++;
                        OnPropertyChanged(nameof(DepNodeCounterView));

                        if (needVersion.Dependencies != null)
                        {
                            if (alreadyChecked.FirstOrDefault(x => x == chDep.ViewTitle) == null)
                                await LoadDeps(chDep, needVersion.Dependencies, alreadyChecked);

                            alreadyChecked.Add(chDep.ViewTitle);
                        }

                        if (!TotalDeps.Any(x => x == chDep.Title))
                            TotalDeps.Add(chDep.Title);
                    }
                    else
                    {
                        chDep.State = DepStateType.Error;
                        DepNodeCounterView.TotalError++;
                        OnPropertyChanged(nameof(DepNodeCounterView));
                    }
                }
                else
                {
                    chDep.State = DepStateType.Error;
                    DepNodeCounterView.TotalError++;
                    OnPropertyChanged(nameof(DepNodeCounterView));
                    _infoBarService.Show($"Пакет '{pack}' не найден");
                }

                root.Dependencies.Add(chDep);

                //OnPropertyChanged(nameof(DepNodeView));
                //OnPropertyChanged(nameof(DataSource));

                //DataSource.RemoveAt(0);
                //DataSource.Add(DepNodeView);
            }
        }

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
        private async Task CheckDepsInRegistry(DepNodeView root)
        {
            try
            {
                if (root.State != DepStateType.Error && root.State != DepStateType.NotFounded)
                {
                    var check = await _npmRegService.CheckPackage(root.TarballUrl);
                    if (check)
                    {
                        root.SetState(DepStateType.Founded);
                        DepNodeCounterView.TotalFounded++;
                        OnPropertyChanged(nameof(DepNodeCounterView));
                    }
                    else
                    {
                        root.SetState(DepStateType.NotFounded);
                        DepNodeCounterView.TotalNotFound++;
                        OnPropertyChanged(nameof(DepNodeCounterView));
                    }
                }

                foreach (var item in root.Dependencies)
                    await CheckDepsInRegistry(item);
            }
            catch (Exception)
            {
                root.SetState(DepStateType.Error);
                DepNodeCounterView.TotalError++;
                OnPropertyChanged(nameof(DepNodeCounterView));
            }
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
