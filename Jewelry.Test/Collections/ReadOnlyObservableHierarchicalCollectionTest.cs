using Jewelry.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Xunit;

namespace Jewelry.Test.Collections;

[DebuggerDisplay("Path = {Path}, Param1 = {Param}")]
public class Data
{
    public string Path { get; set; } = "";
    public int Param { get; set; }
}

public class ReadOnlyObservableHierarchicalCollectionTest
{
    [Fact]
    public void Empty()
    {
        var source = new ObservableCollection<Data>();

        using var sut = source.ToReadOnlyObservableHierarchicalCollection(static x => x.Path);

        Assert.Single(sut._allFolderNodes);
        Assert.Empty(sut.Folders);
        Assert.Empty(sut.Children);
    }

    [Fact]
    public void AllRoot()
    {
        var source = new ObservableCollection<Data>
        {
            new() { Path = "name0", Param = 0 },
            new() { Path = "name1", Param = 1 },
            new() { Path = "name2", Param = 2 },
            new() { Path = "name3", Param = 3 },
            new() { Path = "name4", Param = 4 }
        };

        using var sut = source.ToReadOnlyObservableHierarchicalCollection(static x => x.Path);

        Assert.Single(sut._allFolderNodes);
        Assert.Empty(sut.Folders);
        Assert.Equal(5, sut.Children.Count);
        Assert.Equal(0, sut.Children[0].Param);
        Assert.Equal(1, sut.Children[1].Param);
        Assert.Equal(2, sut.Children[2].Param);
        Assert.Equal(3, sut.Children[3].Param);
        Assert.Equal(4, sut.Children[4].Param);
    }

    [Fact]
    public void Folder()
    {
        var source = new ObservableCollection<Data>
        {
            new() { Path = "name0", Param = 0 },
            new() { Path = "aaa/name1", Param = 1 },
            new() { Path = "aaa/name2", Param = 2 },
            new() { Path = "bbb/name3", Param = 3 },
            new() { Path = "bbb/name4", Param = 4 },
            new() { Path = "bbb/name5", Param = 5 }
        };

        using var sut = source.ToReadOnlyObservableHierarchicalCollection(static x => x.Path);

        Assert.Equal(3, sut._allFolderNodes.Count);

        Assert.Single(sut.Children);
        Assert.Equal(0, sut.Children[0].Param);

        Assert.Equal(2, sut.Folders.Count);

        Assert.Equal("aaa", sut.Folders[0].Path);
        Assert.Equal(2, sut.Folders[0].Children.Count);
        Assert.Equal(1, sut.Folders[0].Children[0].Param);
        Assert.Equal(2, sut.Folders[0].Children[1].Param);

        Assert.Equal("bbb", sut.Folders[1].Path);
        Assert.Equal(3, sut.Folders[1].Children.Count);
        Assert.Equal(3, sut.Folders[1].Children[0].Param);
        Assert.Equal(4, sut.Folders[1].Children[1].Param);
        Assert.Equal(5, sut.Folders[1].Children[2].Param);
    }

    [Fact]
    public void SubFolder()
    {
        var source = new ObservableCollection<Data>
        {
            new() { Path = "name0", Param = 0 },
            new() { Path = "aaa/name1", Param = 1 },
            new() { Path = "aaa/xxx/name2", Param = 2 },
            new() { Path = "aaa/xxx/name3", Param = 3 },
            new() { Path = "aaa/yyy/name4", Param = 4 },
            new() { Path = "aaa/yyy/name5", Param = 5 },
            new() { Path = "aaa/yyy/zzz/name6", Param = 6 },
            new() { Path = "aaa/yyy/zzz/name7", Param = 7 }
        };

        using var sut = source.ToReadOnlyObservableHierarchicalCollection(static x => x.Path);

        Assert.Equal(5, sut._allFolderNodes.Count);

        Assert.Single(sut.Children);
        Assert.Equal(0, sut.Children[0].Param);

        Assert.Single(sut.Folders);
        Assert.Equal("aaa", sut.Folders[0].Path);

        Assert.Single(sut.Folders[0].Children);
        Assert.Equal(1, sut.Folders[0].Children[0].Param);

        Assert.Equal(2, sut.Folders[0].Folders.Count);
        Assert.Equal("aaa/xxx", sut.Folders[0].Folders[0].Path);
        Assert.Equal("aaa/yyy", sut.Folders[0].Folders[1].Path);

        Assert.Equal(2, sut.Folders[0].Folders[0].Children.Count);
        Assert.Equal(2, sut.Folders[0].Folders[0].Children[0].Param);
        Assert.Equal(3, sut.Folders[0].Folders[0].Children[1].Param);

        Assert.Equal(2, sut.Folders[0].Folders[1].Children.Count);
        Assert.Equal(4, sut.Folders[0].Folders[1].Children[0].Param);
        Assert.Equal(5, sut.Folders[0].Folders[1].Children[1].Param);

        Assert.Single(sut.Folders[0].Folders[1].Folders);
        Assert.Equal("aaa/yyy/zzz", sut.Folders[0].Folders[1].Folders[0].Path);
        Assert.Equal(6, sut.Folders[0].Folders[1].Folders[0].Children[0].Param);
        Assert.Equal(7, sut.Folders[0].Folders[1].Folders[0].Children[1].Param);
    }

    [Fact]
    public void SparseSubFolder()
    {
        var source = new ObservableCollection<Data>
        {
            new() { Path = "aaa/bbb/ccc/name1", Param = 1 },
        };

        using var sut = source.ToReadOnlyObservableHierarchicalCollection(static x => x.Path);

        Assert.Equal(4, sut._allFolderNodes.Count);

        Assert.Single(sut.Folders);
        Assert.Equal("aaa", sut.Folders[0].Path);

        Assert.Single(sut.Folders[0].Folders);
        Assert.Equal("aaa/bbb", sut.Folders[0].Folders[0].Path);

        Assert.Single(sut.Folders[0].Folders[0].Folders);
        Assert.Equal("aaa/bbb/ccc", sut.Folders[0].Folders[0].Folders[0].Path);

        Assert.Single(sut.Folders[0].Folders[0].Folders[0].Children);
        Assert.Equal(1, sut.Folders[0].Folders[0].Folders[0].Children[0].Param);
    }

    [Fact]
    public void AddItem()
    {
        var source = new ObservableCollection<Data>();

        using var sut = source.ToReadOnlyObservableHierarchicalCollection(static x => x.Path);
        source.Add(new() { Path = "name0", Param = 0 });
        source.Add(new() { Path = "name1", Param = 1 });

        Assert.Single(sut._allFolderNodes);

        Assert.Equal(2, sut.Children.Count);
        Assert.Equal(0, sut.Children[0].Param);
        Assert.Equal(1, sut.Children[1].Param);
    }

    [Fact]
    public void AddItemWithFolder()
    {
        var source = new ObservableCollection<Data>();

        using var sut = source.ToReadOnlyObservableHierarchicalCollection(static x => x.Path);
        source.Add(new() { Path = "name0", Param = 0 });
        source.Add(new() { Path = "aaa/name1", Param = 1 });
        source.Add(new() { Path = "aaa/bbb/name2", Param = 2 });
        source.Add(new() { Path = "aaa/bbb/name3", Param = 3 });
        
        Assert.Equal(3, sut._allFolderNodes.Count);

        Assert.Single(sut.Children);
        Assert.Single(sut.Folders);
        Assert.Equal("aaa", sut.Folders[0].Path);
        Assert.Single(sut.Folders[0].Children);
        Assert.Equal(1, sut.Folders[0].Children[0].Param);
        Assert.Single(sut.Folders[0].Folders);
        Assert.Equal(2, sut.Folders[0].Folders[0].Children.Count);
        Assert.Equal(2, sut.Folders[0].Folders[0].Children[0].Param);
        Assert.Equal(3, sut.Folders[0].Folders[0].Children[1].Param);
    }

    [Fact]
    public void RemoveItem()
    {
        var source = new ObservableCollection<Data>
        {
            new() { Path = "name0", Param = 0 },
            new() { Path = "name1", Param = 1 },
            new() { Path = "name2", Param = 2 },
            new() { Path = "name3", Param = 3 },
            new() { Path = "name4", Param = 4 }
        };

        using var sut = source.ToReadOnlyObservableHierarchicalCollection(static x => x.Path);

        source.RemoveAt(2);
        
        Assert.Single(sut._allFolderNodes);

        Assert.Equal(4, sut.Children.Count);
        Assert.Equal(0, sut.Children[0].Param);
        Assert.Equal(1, sut.Children[1].Param);
        Assert.Equal(3, sut.Children[2].Param);
        Assert.Equal(4, sut.Children[3].Param);
    }

    [Fact]
    public void RemoveItemWithFolder()
    {
        var source = new ObservableCollection<Data>
        {
            new() { Path = "aaa/name0", Param = 0 },
            new() { Path = "aaa/bbb/name1", Param = 1 },
        };

        using var sut = source.ToReadOnlyObservableHierarchicalCollection(static x => x.Path);

        source.RemoveAt(1);

        Assert.Equal(2, sut._allFolderNodes.Count);
        
        Assert.Single(sut.Folders);
        Assert.Single(sut.Folders[0].Children);
        Assert.Empty(sut.Folders[0].Folders);
    }

    [Fact]
    public void RemoveItemWithNestFolder()
    {
        var source = new ObservableCollection<Data>
        {
            new() { Path = "aaa/name0", Param = 0 },
            new() { Path = "aaa/bbb/ccc/ddd/eee/name1", Param = 1 },
        };

        using var sut = source.ToReadOnlyObservableHierarchicalCollection(static x => x.Path);

        source.RemoveAt(1);

        Assert.Single(sut.Folders);
        Assert.Single(sut.Folders[0].Children);
        Assert.Empty(sut.Folders[0].Folders);
    }

    [Fact]
    public void RemoveItemWithNestFolderOnlyRoot()
    {
        var source = new ObservableCollection<Data>
        {
            new() { Path = "aaa/bbb/ccc/ddd/eee/name0", Param = 0 },
        };

        using var sut = source.ToReadOnlyObservableHierarchicalCollection(static x => x.Path);

        source.RemoveAt(0);
        
        Assert.Single(sut._allFolderNodes);

        Assert.Empty(sut.Folders);
        Assert.Empty(sut.Children);
    }

    [Fact]
    public void Clear()
    {
        var source = new ObservableCollection<Data>
        {
            new() { Path = "name0", Param = 0 },
            new() { Path = "name1", Param = 1 },
            new() { Path = "name2", Param = 2 },
            new() { Path = "name3", Param = 3 },
            new() { Path = "name4", Param = 4 }
        };

        using var sut = source.ToReadOnlyObservableHierarchicalCollection(static x => x.Path);

        source.Clear();

        Assert.Single(sut._allFolderNodes);
        Assert.Empty(sut.Folders);
        Assert.Empty(sut.Children);
    }
    
    [Fact]
    public void SortItem()
    {
        var source = new ObservableCollection<Data>
        {
            new() { Path = "name4", Param = 4 },
            new() { Path = "name3", Param = 3 },
            new() { Path = "name2", Param = 2 },
            new() { Path = "name1", Param = 1 },
            new() { Path = "name0", Param = 0 },
        };

        using var sut = source.ToReadOnlyObservableHierarchicalCollection(static x => x.Path);

        Assert.Equal(5, sut.Children.Count);
        Assert.Equal(0, sut.Children[0].Param);
        Assert.Equal(1, sut.Children[1].Param);
        Assert.Equal(2, sut.Children[2].Param);
        Assert.Equal(3, sut.Children[3].Param);
        Assert.Equal(4, sut.Children[4].Param);
    }
    
    [Fact]
    public void SortFolder()
    {
        var source = new ObservableCollection<Data>
        {
            new() { Path = "zzz/name4", Param = 4 },
            new() { Path = "zzz/name3", Param = 3 },
            new() { Path = "yyy/name2", Param = 2 },
            new() { Path = "yyy/name1", Param = 1 },
            new() { Path = "xxx/name0", Param = 0 },
        };

        using var sut = source.ToReadOnlyObservableHierarchicalCollection(static x => x.Path);

        Assert.Equal(3, sut.Folders.Count);
        Assert.Equal("xxx", sut.Folders[0].Path);
        Assert.Equal("yyy", sut.Folders[1].Path);
        Assert.Equal("zzz", sut.Folders[2].Path);
    }
}