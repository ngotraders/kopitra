//+------------------------------------------------------------------+
//|                                                        Queue.mqh |
//|                                      Copyright 2023, Yuto Nagano |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2023, Yuto Nagano"
#property link "https://www.mql5.com"
#property strict

template <typename T>
class Queue
{
private:
    T elements[]; // 要素を格納する動的配列
    int front;    // キューの先頭のインデックス
    int rear;     // キューの末尾のインデックス
    int size;     // キューに格納されている要素の数
    int capacity; // 初期キャパシティ

    // 配列のサイズを調整する
    void EnsureCapacity(int minCapacity)
    {
        if (minCapacity > capacity)
        {
            int original_capacity = capacity;
            while (capacity < minCapacity)
                capacity *= 2;
            ArrayResize(elements, capacity);

            for (int i = 0; i < size; i++)
            {
                elements[(original_capacity + i) % capacity] = elements[(front + i) % original_capacity];
            }
            front = original_capacity;
            rear = (original_capacity + size - 1) % capacity;
        }
    }

public:
    Queue()
    {
        front = 0;
        rear = -1;
        size = 0;
        capacity = 8; // 初期容量を8に設定
        ArrayResize(elements, capacity);
    }

    // キューに要素を追加する
    void Enqueue(T element)
    {
        EnsureCapacity(size + 1);
        rear = (rear + 1) % capacity;
        elements[rear] = element;
        size++;
    }

    // キューから1つの要素を取り出す
    bool Dequeue(T &element)
    {
        if (IsEmpty())
        {
            return false; // キューが空のため、要素を取り出せない
        }
        element = elements[front];
        front = (front + 1) % capacity;
        size--;
        if (size == 0)
        {
            // キューが空になったらインデックスをリセット
            front = 0;
            rear = -1;
        }
        return true; // 要素の取り出しが成功
    }

    // キューから指定された個数の要素を一括で取り出す
    int DequeueMultiple(int count, T &outElements[])
    {
        int num_dequeue = count;
        if (num_dequeue > size)
        {
            num_dequeue = size;
        }
        ArrayResize(outElements, num_dequeue);
        for (int i = 0; i < num_dequeue; i++)
        {
            T element;
            Dequeue(element);
            outElements[i] = element;
        }
        return num_dequeue;
    }

    // キューから全ての要素を一括で取り出す
    void DequeueAll(T &outElements[])
    {
        ArrayResize(outElements, size);
        int size_original = size;
        T element;
        for (int i = 0; i < size_original; i++)
        {
            Dequeue(element);
            outElements[i] = element;
        }
    }

    // キューが空かどうかを確認する
    bool IsEmpty() const
    {
        return size == 0;
    }

    // キューに格納されている要素の数を返す
    int Size() const
    {
        return size;
    }
};
