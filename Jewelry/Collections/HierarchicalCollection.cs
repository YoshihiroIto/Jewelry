using Jewelry.Memory;
using Jewelry.Text;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Jewelry.Collections;

using static ReadOnlyObservableHierarchicalCollectionHelper;

public sealed class ReadOnlyObservableHierarchicalCollection<T> : HierarchicalNode<T>, IDisposable
{
    internal readonly Func<T, string> GetPath;

    // ReSharper disable once InconsistentNaming
    internal readonly Dictionary<string, HierarchicalNode<T>> _allFolderNodes = new();

    private readonly INotifyCollectionChanged _notifyCollectionChanged;

    internal ReadOnlyObservableHierarchicalCollection(
        IEnumerable<T> source,
        INotifyCollectionChanged notifyCollectionChanged,
        Func<T, string> getPath)
    {
        SetupAllFolderNodes();

        _notifyCollectionChanged = notifyCollectionChanged;
        GetPath = getPath;

        foreach (var item in source)
            Add(item);

        _notifyCollectionChanged.CollectionChanged += SourceOnCollectionChanged;
    }

    public void Dispose()
    {
        _notifyCollectionChanged.CollectionChanged -= SourceOnCollectionChanged;
    }

    internal void RemoveFolder(string folderPath)
    {
        _allFolderNodes.Remove(folderPath);
    }

    private void Add(T item)
    {
        var itemPath = GetPath(item);
        var (folderPath, pathLeaf) = PickFolderPath(itemPath);

        if (_allFolderNodes.TryGetValue(folderPath, out var targetNode) == false)
        {
            using var ss = new StringSplitter(stackalloc StringSplitter.StringSpan[32]);

            var folders = ss.Split(folderPath, FolderSeparator, StringSplitOptions.RemoveEmptyEntries);

            HierarchicalNode<T> parentNode = this;
            var folder = "";

            foreach (var folderSpan in folders)
            {
                var folderPathLeaf = folderSpan.ToSpan(folderPath);

                folder = folder == ""
                    ? folderPathLeaf.ToString()
                    : $"{folder}/{folderPathLeaf}";

                if (_allFolderNodes.TryGetValue(folder, out targetNode) == false)
                {
                    targetNode = parentNode.AddFolder(folder, folderPathLeaf.ToString());
                    _allFolderNodes[folder] = targetNode;
                }

                parentNode = targetNode;
            }

            _ = targetNode ?? throw new InvalidOperationException();
        }

        targetNode.AddChild(item, pathLeaf);
    }

    private void SetupAllFolderNodes()
    {
        _allFolderNodes.Clear();
        _allFolderNodes[""] = this;
    }

    private void SourceOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                _ = e.NewItems ?? throw new InvalidOperationException();

                foreach (T item in e.NewItems)
                    Add(item);

                break;

            case NotifyCollectionChangedAction.Remove:
                _ = e.OldItems ?? throw new InvalidOperationException();

                foreach (T item in e.OldItems)
                {
                    var (folderPath, _) = PickFolderPath(GetPath(item));

                    if (_allFolderNodes.TryGetValue(folderPath, out var targetNode) == false)
                        throw new InvalidOperationException();

                    targetNode.RemoveChild(item);
                }

                break;

            case NotifyCollectionChangedAction.Reset:
                Clear();
                SetupAllFolderNodes();
                break;

            case NotifyCollectionChangedAction.Move:
                break;

            case NotifyCollectionChangedAction.Replace:
                throw new NotImplementedException();

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

public class HierarchicalNode<T>
{
    public string Path { get; }
    public string PathLeaf { get; }

    public ReadOnlyObservableCollection<HierarchicalNode<T>> Folders { get; }
    public ReadOnlyObservableCollection<T> Children { get; }

    private readonly ObservableCollectionEx<HierarchicalNode<T>> _folders = [];
    private readonly ObservableCollectionEx<T> _children = [];

    private readonly HierarchicalNode<T>? _parent;
    private readonly ReadOnlyObservableHierarchicalCollection<T>? _root;

    private ReadOnlyObservableHierarchicalCollection<T> Root =>
        _root ?? (ReadOnlyObservableHierarchicalCollection<T>)this;

    // for root
    internal HierarchicalNode()
    {
        Path = "";
        PathLeaf = "";

        Folders = new(_folders);
        Children = new(_children);
    }

    private HierarchicalNode(string path, HierarchicalNode<T>? parent, ReadOnlyObservableHierarchicalCollection<T> root)
    {
        _parent = parent;
        _root = root;
        Path = path;
        (_, PathLeaf) = PickFolderPath(Path);

        Folders = new(_folders);
        Children = new(_children);
    }

    internal HierarchicalNode<T> AddFolder(string folderPath, string folderPathLeaf)
    {
        var folder = new HierarchicalNode<T>(folderPath, this, Root);

        var insertIndex = SpanHelper.UpperBound<HierarchicalNode<T>, string>(
            _folders.AsSpan(),
            folderPathLeaf,
            (l, r) => string.Compare(l.PathLeaf, r, StringComparison.OrdinalIgnoreCase));

        _folders.Insert(insertIndex, folder);

        return folder;
    }

    internal void AddChild(T item, string itemPathLeaf)
    {
        var insertIndex = SpanHelper.UpperBound<T, string>(
            _children.AsSpan(),
            itemPathLeaf,
            (l, r) => string.Compare(Root.GetPath(l), r, StringComparison.OrdinalIgnoreCase)
        );

        _children.Insert(insertIndex, item);
    }

    internal void RemoveChild(T item)
    {
        _children.Remove(item);

        for (var targetNode = this; targetNode is not null; targetNode = targetNode._parent)
        {
            if (targetNode._children.Count > 0)
                break;
            if (targetNode._folders.Count > 0)
                break;
            if (targetNode._parent is null)
                break;

            targetNode._parent?._folders.Remove(targetNode);
            Root.RemoveFolder(targetNode.Path);
        }
    }

    internal void Clear()
    {
        foreach (var folder in _folders)
            folder.Clear();

        _folders.Clear();
        _children.Clear();
    }
}

public static class ReadOnlyObservableHierarchicalCollectionExtensions
{
    public static ReadOnlyObservableHierarchicalCollection<T> ToReadOnlyObservableHierarchicalCollection<T>(
        this ReadOnlyObservableCollection<T> source,
        Func<T, string> getPath)
    {
        return new ReadOnlyObservableHierarchicalCollection<T>(source, source, getPath);
    }

    public static ReadOnlyObservableHierarchicalCollection<T> ToReadOnlyObservableHierarchicalCollection<T>(
        this ObservableCollection<T> source,
        Func<T, string> getPath)
    {
        return new ReadOnlyObservableHierarchicalCollection<T>(source, source, getPath);
    }
}

static file class ReadOnlyObservableHierarchicalCollectionHelper
{
    public const char FolderSeparator = '/';

    public static (string FolderPath, string PathLeaf) PickFolderPath(string path)
    {
        var pathSpan = path.AsSpan();

        var lastSeparatorIndex = pathSpan.LastIndexOf(FolderSeparator);
        var folderPath = lastSeparatorIndex >= 0 ? pathSpan[..lastSeparatorIndex].ToString() : "";
        var pathLeaf = lastSeparatorIndex >= 0 ? pathSpan[(lastSeparatorIndex + 1)..].ToString() : pathSpan.ToString();

        return (folderPath, pathLeaf);
    }
}