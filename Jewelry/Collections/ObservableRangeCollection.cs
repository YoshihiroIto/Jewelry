using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace Jewelry.Collections;

/// <summary>
/// 範囲削除時の挙動を指定する列挙型
/// </summary>
public enum RangeRemoveBehavior
{
    /// <summary>
    /// 1つずつ削除通知を発行します。
    /// <para>メリット: 選択状態(SelectedItems)やフォーカス、スクロール位置が維持されます。</para>
    /// <para>デメリット: 大量(数千件以上)の削除ではUIがフリーズする可能性があります。</para>
    /// </summary>
    Iterative,

    /// <summary>
    /// 内部リストを一括削除し、Reset(全更新)通知を1回だけ発行します。
    /// <para>メリット: 非常に高速です。数万件の削除も一瞬で完了します。</para>
    /// <para>デメリット: 選択状態やスクロール位置がリセットされます。</para>
    /// </summary>
    Reset,

    /// <summary>
    /// 削除する件数に応じて自動的に切り替えます。
    /// (閾値以下ならIterative、それ以上ならReset)
    /// </summary>
    Smart
}

/// <summary>
/// WPFでの高速な範囲操作をサポートするコレクション。
/// コンストラクタで削除時の挙動（パフォーマンス重視 vs UX重視）を指定可能です。
/// </summary>
public sealed class ObservableRangeCollection<T> : ObservableCollection<T>
{
    private readonly RangeRemoveBehavior _removeBehavior;
    private const int SmartResetThreshold = 50; // Smartモードの閾値

    /// <summary>
    /// デフォルトの挙動（Smart）で初期化します。
    /// </summary>
    public ObservableRangeCollection() : this(RangeRemoveBehavior.Smart)
    {
    }

    /// <summary>
    /// 指定した削除挙動で初期化します。
    /// </summary>
    public ObservableRangeCollection(RangeRemoveBehavior removeBehavior)
    {
        _removeBehavior = removeBehavior;
    }

    /// <summary>
    /// 初期コレクションと削除挙動を指定して初期化します。
    /// </summary>
    public ObservableRangeCollection(IEnumerable<T> collection,
        RangeRemoveBehavior removeBehavior = RangeRemoveBehavior.Smart)
        : base(collection)
    {
        _removeBehavior = removeBehavior;
    }

    /// <summary>
    /// 複数のアイテムを末尾に追加します。
    /// (InsertRangeの最適化により高速です)
    /// </summary>
    public void AddRange(IEnumerable<T> collection)
    {
        InsertRange(Count, collection);
    }

    /// <summary>
    /// 複数のアイテムを指定したインデックスに挿入します。
    /// 内部List<T>へ直接アクセスすることでメモリコピーを最小限に抑えます。
    /// </summary>
    public void InsertRange(int index, IEnumerable<T> collection)
    {
        if (collection is null)
            throw new ArgumentNullException(nameof(collection));

        if (index < 0 || index > Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        var list = collection as List<T> ?? collection.ToList();
        if (list.Count is 0)
            return;

        CheckReentrancy();

        // 高速化: 実体がList<T>なら一括挿入（配列シフト回数を1回にする）
        if (Items is List<T> itemsList)
            itemsList.InsertRange(index, list);
        else
            for (int i = 0; i < list.Count; i++)
                Items.Insert(index + i, list[i]);

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

        // WPF 4.5+ は範囲追加(Action.Add)に対応しているため高速通知が可能
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, list, index));
    }

    /// <summary>
    /// 指定範囲のアイテムを削除します。
    /// コンストラクタで指定された Behavior に従って動作します。
    /// </summary>
    public void RemoveRange(int index, int count)
    {
        if (index < 0 || count < 0 || index + count > Count)
            throw new ArgumentOutOfRangeException();

        if (count is 0)
            return;

        // 挙動の決定
        var useReset = _removeBehavior switch
        {
            RangeRemoveBehavior.Reset => true,
            RangeRemoveBehavior.Iterative => false,
            RangeRemoveBehavior.Smart => count > SmartResetThreshold,
            _ => false
        };

        if (useReset)
        {
            // --- Reset モード (高速) ---
            CheckReentrancy();

            if (Items is List<T> itemsList)
                itemsList.RemoveRange(index, count);
            else
                for (int i = 0; i < count; i++)
                    Items.RemoveAt(index);

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

            // WPF例外回避のため Reset 通知を行う
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        else
        {
            // --- Iterative モード (選択維持) ---
            // 後ろから1つずつ消すことで、インデックスずれを防ぎつつ
            // WPFに正しく通知を送る
            for (int i = 0; i < count; i++)
            {
                // RemoveAt内部で CheckReentrancy と Event発生が行われる
                RemoveAt(index);
            }
        }
    }

    /// <summary>
    /// コレクションを完全に置き換えます。常にReset通知を発行します。
    /// </summary>
    public void ReplaceAll(IEnumerable<T> collection)
    {
        if (collection is null)
            throw new ArgumentNullException(nameof(collection));

        var list = collection as List<T> ?? collection.ToList();

        CheckReentrancy();

        Items.Clear();

        if (Items is List<T> itemsList)
            itemsList.AddRange(list);
        else
            foreach (var item in list)
                Items.Add(item);

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public Span<T> AsSpan()
    {
        if (Items is not List<T> list)
            throw new NotSupportedException();

        return CollectionsMarshal.AsSpan(list);
    }

    public Span<T> AsSpan(int start)
    {
        return AsSpan()[start..];
    }

    public Span<T> AsSpan(int start, int length)
    {
        return AsSpan().Slice(start, length);
    }

    public int EnsureCapacity(int capacity)
    {
        if (Items is not List<T> list)
            throw new NotSupportedException();

        return list.EnsureCapacity(capacity);
    }

    public void TrimExcess()
    {
        if (Items is not List<T> list)
            throw new NotSupportedException();

        list.TrimExcess();
    }
}