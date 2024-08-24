using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NpmPackChecker.WUI.MVVM.Model;

public class DepNodeView : ObservableObject
{
    public string Title { get; set; }
    public string DepVersion { get; set; }
    public string TrueVersion { get; set; }
    public DateTime TrueVersionDate { get; set; }
    public string TarballUrl { get; set; }

    //public DepNodeView? Parent { get; set; }
    public ObservableCollection<DepNodeView> Dependencies { get; set; }

    public bool FromDefault { get; set; }
    public string TextForeground { get => FromDefault == true ? "Red" : null; }

    public List<string> TotalDeps { get; set; }

    public DepStateType State { get; set; }

    public string ToolTip
    {
        get
        {
            if (FromDefault)
                return "Информация загружена из репозитория по умолчанию";

            if (State == DepStateType.Pending)
                return "Пакет ещё не проверился";

            if (State == DepStateType.Founded)
                return "Пакет присутствует в репозитории";

            if (State == DepStateType.NotFounded)
                return "Пакет не надйен в репозитории";

            if (State == DepStateType.Founded)
                return "Произошла ошибка при проверки пакета";

            return ViewTitle;
        }
    }

    public string ViewTitle => $"{Title}@{DepVersion}({TrueVersion} - {TrueVersionDate:dd.MM.yyyy})";
    public string StateIcon
    {
        get
        {
            return State switch
            {
                DepStateType.Pending => "\uE823",
                DepStateType.Founded => "\uE73A",
                DepStateType.NotFounded => "\uE711",
                DepStateType.Error => "\uE783",
                _ => "\uE823",
            };
        }
    }
    public string StateIconColor
    {
        get
        {
            return State switch
            {
                DepStateType.Pending => "Blue",
                DepStateType.Founded => "Green",
                DepStateType.NotFounded => "Red",
                DepStateType.Error => "Red",
                _ => "Blue",
            };
        }
    }
    public string StateIconToolTip
    {
        get
        {
            return State switch
            {
                DepStateType.Pending => "В процессе",
                DepStateType.Founded => "Найден",
                DepStateType.NotFounded => "Не найден",
                DepStateType.Error => "Ошибка",
                _ => "NONE",
            };
        }
    }

    public DepNodeView Clone()
    {
        //DepNodeView res = new(Title, DepVersion, Parent);
        DepNodeView res = new(Title, DepVersion, null);
        res.TrueVersion = TrueVersion;
        res.TrueVersionDate = TrueVersionDate;
        res.TarballUrl = TarballUrl;
        res.State = State;
        res.Dependencies = new(Dependencies.Select(x => x.Clone()));
        return res;
    }

    public void SetState(DepStateType State)
    {
        this.State = State;
        OnPropertyChanged(nameof(StateIcon));
        OnPropertyChanged(nameof(StateIconColor));
    }

    public DepNodeView()
    {
        Dependencies = new();
        TotalDeps = new();
        State = DepStateType.Pending;
    }

    public DepNodeView(string title, string version)
        : this()
    {
        Title = title;
        DepVersion = version;
        //Parent = null;
    }

    public DepNodeView(string title, string version, DepNodeView? parent)
        : this(title, version)
    {
        //Parent = parent;
    }
}

public enum DepStateType
{
    Pending,
    Founded,
    NotFounded,
    Error
}
