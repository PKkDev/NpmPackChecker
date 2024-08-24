using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using NpmPackChecker.WUI.MVVM.View;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NpmPackChecker.WUI.Services;

public class NavigationHelperService
{
    private NavigationView NavigationView { get; set; }
    private Frame ContentFrame { get; set; }

    private readonly Dictionary<string, Type> _pages = new()
    {
        //{ "Board", typeof(BoardView) },
        //{ "MergReq", typeof(MergReqView) },
        //{ "FavoriteTask", typeof(FavoriteTaskView) },
        { "Settings", typeof(SettingsView) },
        { "NpmPackDep", typeof(NpmPackDepView) }
    };

    public NavigationHelperService() { }

    public void Initialize(NavigationView navigationView, Frame contentFrame)
    {
        NavigationView = navigationView;
        ContentFrame = contentFrame;

        NavigationView.BackRequested += OnBackRequested;
        NavigationView.ItemInvoked += OnItemInvoked;
        ContentFrame.NavigationFailed += OnNavigationFailed;
    }

    private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    { }

    private void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        // _contentFrame.GoBack();
    }

    private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked == true)
        {
            Navigate("Settings");
        }
        else if (args.InvokedItemContainer != null)
        {
            var navItemTag = args.InvokedItemContainer.Tag.ToString();
            Navigate(navItemTag);
        }
    }

    public void ClearBackStack()
    {
        ContentFrame.BackStack.Clear();
    }

    public void Navigate(string navItemTag)
    {
        Type _page = null;

        var item = _pages.FirstOrDefault(p => p.Key.Equals(navItemTag));
        _page = item.Value;

        var preNavPageType = ContentFrame.CurrentSourcePageType;

        if (!(_page is null) && !Type.Equals(preNavPageType, _page))
        {
            ContentFrame.Navigate(_page, null, new DrillInNavigationTransitionInfo());
        }
    }
}