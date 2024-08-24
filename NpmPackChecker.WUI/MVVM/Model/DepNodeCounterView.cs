using CommunityToolkit.Mvvm.ComponentModel;

namespace NpmPackChecker.WUI.MVVM.Model;

public class DepNodeCounterView : ObservableObject
{
    private int _totalDeps;
    public int TotalDeps { get => _totalDeps; set => SetProperty(ref _totalDeps, value); }

    private int _totalFounded;
    public int TotalFounded { get => _totalFounded; set => SetProperty(ref _totalFounded, value); }

    private int _totalNotFound;
    public int TotalNotFound { get => _totalNotFound; set => SetProperty(ref _totalNotFound, value); }

    private int _totalError;
    public int TotalError { get => _totalError; set => SetProperty(ref _totalError, value); }

    public string ViewTitle { get => $"TotalDeps: {TotalDeps} TotalFounded: {TotalFounded} TotalNotFound: {TotalNotFound} TotalError: {TotalError} "; }

    public DepNodeCounterView()
    {
        TotalDeps = 0;
        TotalFounded = 0;
        TotalNotFound = 0;
        TotalError = 0;
    }

    public DepNodeCounterView(DepNodeView node)
    {
        CehckPack(node);
    }

    private void CehckPack(DepNodeView node)
    {
        TotalDeps++;
        if (node.State == DepStateType.Error)
            TotalError++;
        if (node.State == DepStateType.Founded)
            TotalFounded++;
        if (node.State == DepStateType.NotFounded)
            TotalNotFound++;

        foreach (var dep in node.Dependencies)
            CehckPack(dep);
    }
}

