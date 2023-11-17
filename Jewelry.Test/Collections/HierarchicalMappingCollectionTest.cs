using System.Collections.ObjectModel;
using Jewelry.Collections;
using Xunit;

namespace Jewelry.Test.Collections;

public class HierarchicalMappingCollectionTest
{
    public class TreeNode
    {
        public int Param1 { get; set; }

        public ObservableCollection<TreeNode> Children { get; set; } = [];
    }

    public class TreeNodeViewModel(TreeNode model)
    {
        public int Param1
        {
            get => model.Param1;
            set => model.Param1 = value;
        }
        public bool IsSelected { get; set; }

        public ObservableCollection<TreeNodeViewModel> Children { get; set; } = [];
    }

    [Fact]
    public void InitialChildren()
    {
        var source = new ObservableCollection<TreeNode>
        {
            new() {Param1 = 123},
            new() {Param1 = 456},
            new() {Param1 = 789}
        };

        var target = new HierarchicalMappingCollection<TreeNode, TreeNodeViewModel>(
            source, 
            m => new TreeNodeViewModel(m),
            s => s.Children,
            t => t.Children);

        Assert.Equal(3, source.Count);
        Assert.Equal(3, target.Count);
        Assert.Equal(123, target[0].Param1);
        Assert.Equal(456, target[1].Param1);
        Assert.Equal(789, target[2].Param1);
    }

    [Fact]
    public void Add()
    {
        var source = new ObservableCollection<TreeNode>();

        var target = new HierarchicalMappingCollection<TreeNode, TreeNodeViewModel>(
            source, 
            m => new TreeNodeViewModel(m),
            s => s.Children,
            t => t.Children);

        Assert.Empty(source);
        Assert.Empty(target);

        source.Add(new TreeNode { Param1 = 123 });
        source.Add(new TreeNode { Param1 = 456 });
        source.Add(new TreeNode { Param1 = 789 });
            
        Assert.Equal(3, source.Count);
        Assert.Equal(3, target.Count);
        Assert.Equal(123, target[0].Param1);
        Assert.Equal(456, target[1].Param1);
        Assert.Equal(789, target[2].Param1);
    }

    [Fact]
    public void Remove()
    {
        var source = new ObservableCollection<TreeNode>();

        var target = new HierarchicalMappingCollection<TreeNode, TreeNodeViewModel>(
            source, 
            m => new TreeNodeViewModel(m),
            s => s.Children,
            t => t.Children);

        Assert.Empty(source);
        Assert.Empty(target);

        source.Add(new TreeNode { Param1 = 123 });
        source.Add(new TreeNode { Param1 = 456 });
        source.Add(new TreeNode { Param1 = 789 });
            
        Assert.Equal(3, source.Count);
        Assert.Equal(3, target.Count);
        Assert.Equal(123, target[0].Param1);
        Assert.Equal(456, target[1].Param1);
        Assert.Equal(789, target[2].Param1);

        source.RemoveAt(1);

        Assert.Equal(2, source.Count);
        Assert.Equal(2, target.Count);
        Assert.Equal(123, target[0].Param1);
        Assert.Equal(789, target[1].Param1);
    }

    [Fact]
    public void ChildrenAdd()
    {
        var source = new ObservableCollection<TreeNode>();

        var target = new HierarchicalMappingCollection<TreeNode, TreeNodeViewModel>(
            source, 
            m => new TreeNodeViewModel(m),
            s => s.Children,
            t => t.Children);

        Assert.Empty(source);
        Assert.Empty(target);

        source.Add(new TreeNode { Param1 = 123 });
        source.Add(new TreeNode { Param1 = 456 });
        source.Add(new TreeNode { Param1 = 789 });

        source[1].Children.Add(new TreeNode{Param1 = 111});

        Assert.Equal(111, target[1].Children[0].Param1);
    }

    [Fact]
    public void ChildrenRemove()
    {
        var source = new ObservableCollection<TreeNode>();

        var target = new HierarchicalMappingCollection<TreeNode, TreeNodeViewModel>(
            source, 
            m => new TreeNodeViewModel(m),
            s => s.Children,
            t => t.Children);

        Assert.Empty(source);
        Assert.Empty(target);

        source.Add(new TreeNode { Param1 = 123 });
        source.Add(new TreeNode { Param1 = 456 });
        source.Add(new TreeNode { Param1 = 789 });

        source[1].Children.Add(new TreeNode{Param1 = 111});
        Assert.Equal(111, target[1].Children[0].Param1);

        source[1].Children.RemoveAt(0);
        Assert.Empty(target[1].Children);
    }

    [Fact]
    public void DeepChildrenAdd()
    {
        var source = new ObservableCollection<TreeNode>();

        var target = new HierarchicalMappingCollection<TreeNode, TreeNodeViewModel>(
            source, 
            m => new TreeNodeViewModel(m),
            s => s.Children,
            t => t.Children);

        Assert.Empty(source);
        Assert.Empty(target);


        var childNode1 = new TreeNode {Param1 = 111};
        var childNode2 = new TreeNode {Param1 = 222};
        var childNode3 = new TreeNode {Param1 = 333};

        childNode1.Children.Add(childNode2);
        childNode2.Children.Add(childNode3);

        source.Add(childNode1);

        Assert.Equal(111, target[0].Param1);
        Assert.Equal(222, target[0].Children[0].Param1);
        Assert.Equal(333, target[0].Children[0].Children[0].Param1);
    }

    [Fact]
    public void ToHierarchicalMappingCollection()
    {
        var source = new ObservableCollection<TreeNode>
        {
            new TreeNode {Param1 = 123},
            new TreeNode {Param1 = 456},
            new TreeNode {Param1 = 789}
        };

        var target = source.ToHierarchicalMappingCollection<TreeNode, TreeNodeViewModel>(
            m => new TreeNodeViewModel(m),
            s => s.Children,
            t => t.Children);

        Assert.Equal(3, source.Count);
        Assert.Equal(3, target.Count);
        Assert.Equal(123, target[0].Param1);
        Assert.Equal(456, target[1].Param1);
        Assert.Equal(789, target[2].Param1);
    }
}