using System.Collections.ObjectModel;
using Jewelry.Collections;
using Xunit;

namespace Jewelry.Test.Collections;

public class MappingCollectionTest
{
    private class SourceData
    {
        public int Param1 { get; set; }
    }

    private class TargetData(SourceData sourceData)
    {
        public string Param1 { get; set; } = sourceData.Param1.ToString();
    }

    [Fact]
    public void InitialChildren()
    {
        var source = new ObservableCollection<SourceData>
        {
            new() { Param1 = 123 },
            new() { Param1 = 456 },
            new() { Param1 = 789 }
        };

        using var target = source.ToReadOnlyMappingCollection(x => new TargetData(x));

        Assert.Equal(3, target.Count);
        Assert.Equal("123", target[0].Param1);
        Assert.Equal("456", target[1].Param1);
        Assert.Equal("789", target[2].Param1);
    }

    [Fact]
    public void InitialChildrenReadOnly()
    {
        var source = new ObservableCollection<SourceData>
        {
            new() { Param1 = 123 },
            new() { Param1 = 456 },
            new() { Param1 = 789 }
        };

        var readOnlySource = new ReadOnlyObservableCollection<SourceData>(source);

        using var target = readOnlySource.ToReadOnlyMappingCollection(x => new TargetData(x));

        Assert.Equal(3, target.Count);
        Assert.Equal("123", target[0].Param1);
        Assert.Equal("456", target[1].Param1);
        Assert.Equal("789", target[2].Param1);
    }

    [Fact]
    public void Add()
    {
        var source = new ObservableCollection<SourceData>
        {
            new() { Param1 = 123 },
            new() { Param1 = 456 },
            new() { Param1 = 789 }
        };

        using var target = source.ToReadOnlyMappingCollection(x => new TargetData(x));
        source.Add(new() { Param1 = 999 });
        source.Add(new() { Param1 = 888 });


        Assert.Equal(5, target.Count);
        Assert.Equal("123", target[0].Param1);
        Assert.Equal("456", target[1].Param1);
        Assert.Equal("789", target[2].Param1);
        Assert.Equal("999", target[3].Param1);
        Assert.Equal("888", target[4].Param1);
    }

    [Fact]
    public void Insert()
    {
        var source = new ObservableCollection<SourceData>
        {
            new() { Param1 = 123 },
            new() { Param1 = 456 },
            new() { Param1 = 789 }
        };

        using var target = source.ToReadOnlyMappingCollection(x => new TargetData(x));
        source.Insert(2, new() { Param1 = 999 });

        Assert.Equal(4, target.Count);
        Assert.Equal("123", target[0].Param1);
        Assert.Equal("456", target[1].Param1);
        Assert.Equal("999", target[2].Param1);
        Assert.Equal("789", target[3].Param1);
    }

    [Fact]
    public void Remove()
    {
        var source = new ObservableCollection<SourceData>
        {
            new() { Param1 = 123 },
            new() { Param1 = 456 },
            new() { Param1 = 789 }
        };

        using var target = source.ToReadOnlyMappingCollection(x => new TargetData(x));
        source.Remove(source[1]);

        Assert.Equal(2, target.Count);
        Assert.Equal("123", target[0].Param1);
        Assert.Equal("789", target[1].Param1);
    }

    [Fact]
    public void RemoveAt()
    {
        var source = new ObservableCollection<SourceData>
        {
            new() { Param1 = 123 },
            new() { Param1 = 456 },
            new() { Param1 = 789 }
        };

        using var target = source.ToReadOnlyMappingCollection(x => new TargetData(x));
        source.RemoveAt(1);

        Assert.Equal(2, target.Count);
        Assert.Equal("123", target[0].Param1);
        Assert.Equal("789", target[1].Param1);
    }

    [Fact]
    public void Move()
    {
        var source = new ObservableCollection<SourceData>
        {
            new() { Param1 = 123 },
            new() { Param1 = 456 },
            new() { Param1 = 789 }
        };

        using var target = source.ToReadOnlyMappingCollection(x => new TargetData(x));
        source.Move(0, 2);

        Assert.Equal(3, target.Count);
        Assert.Equal("456", target[0].Param1);
        Assert.Equal("789", target[1].Param1);
        Assert.Equal("123", target[2].Param1);
    }

    [Fact]
    public void Replace()
    {
        var source = new ObservableCollection<SourceData>
        {
            new() { Param1 = 123 },
            new() { Param1 = 456 },
            new() { Param1 = 789 }
        };

        using var target = source.ToReadOnlyMappingCollection(x => new TargetData(x));
        source.Move(0, 2);

        Assert.Equal(3, target.Count);
        Assert.Equal("456", target[0].Param1);
        Assert.Equal("789", target[1].Param1);
        Assert.Equal("123", target[2].Param1);
    }

    [Fact]
    public void Reset()
    {
        var source = new ObservableCollection<SourceData>
        {
            new() { Param1 = 123 },
            new() { Param1 = 456 },
            new() { Param1 = 789 }
        };

        using var target = source.ToReadOnlyMappingCollection(x => new TargetData(x));
        source.Clear();

        Assert.Empty(target);
    }
}