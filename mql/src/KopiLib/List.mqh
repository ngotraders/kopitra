//+------------------------------------------------------------------+
//|                                                         List.mqh |
//|                                      Copyright 2024, Yuto Nagano |
//+------------------------------------------------------------------+
#property copyright "Copyright 2024, Yuto Nagano"
#property strict

template <typename T>
class List
{
private:
    T array[];
    int size;     // 実際に格納されている要素の数
    int capacity; // 配列の容量

    // 配列の容量を増やす（必要に応じて倍増）
    void EnsureCapacity(int minCapacity)
    {
        if (capacity < minCapacity)
        {
            while (capacity < minCapacity)
                capacity *= 2;
            ArrayResize(array, capacity);
        }
    }

public:
    // コンストラクタ
    List()
    {
        size = 0;
        capacity = 8; // 初期容量を8に設定
        ArrayResize(array, capacity);
    }

    // 要素をリストの末尾に追加
    void Add(T value)
    {
        EnsureCapacity(size + 1);
        array[size++] = value;
    }

    // 配列の要素をリストの末尾に追加
    void AddRange(T &values[])
    {
        EnsureCapacity(size + ArraySize(values));
        for (int i = 0; i < ArraySize(values); i++)
        {
            array[size++] = values[i];
        }
    }

    // 指定されたインデックスの要素を取得
    T Get(int index)
    {
        if (index >= 0 && index < size)
        {
            return array[index];
        }
        else
        {
            // エラーハンドリング
            Print("Index out of bounds");
            return NULL;
        }
    }

    // 指定されたインデックスの要素を削除
    void RemoveAt(int index)
    {
        if (index >= 0 && index < size)
        {
            for (int i = index; i < size - 1; i++)
            {
                array[i] = array[i + 1];
            }
            size--;
        }
        else
        {
            Print("Index out of bounds");
        }
    }

    // 指定されたインデックスに要素を挿入
    void InsertAt(int index, T value)
    {
        if (index >= 0 && index <= size)
        { // 挿入はsizeに等しい場合も許容する（末尾への挿入）
            EnsureCapacity(size + 1);
            for (int i = size; i > index; i--)
            {
                array[i] = array[i - 1];
            }
            array[index] = value;
            size++;
        }
        else
        {
            Print("Index out of bounds");
        }
    }

    // リストのサイズを取得
    int Size()
    {
        return size;
    }

    // リストの全要素を削除
    void Clear()
    {
        size = 0;
        // 容量はそのままにするか、あるいは初期容量に戻すかは設計次第
    }

    // 要素のインデックスを検索（存在しない場合は-1を返す）
    int IndexOf(T value)
    {
        for (int i = 0; i < size; i++)
        {
            if (array[i] == value)
            { // 値の比較は型Tに依存
                return i;
            }
        }
        return -1;
    }

    // 要素を削除
    void Remove(T value)
    {
        int index = IndexOf(value);
        if (index > 0)
        {
            RemoveAt(index);
        }
    }

    // 要素がリストに含まれているかどうかを確認
    bool Contains(T value)
    {
        return IndexOf(value) != -1;
    }
};
