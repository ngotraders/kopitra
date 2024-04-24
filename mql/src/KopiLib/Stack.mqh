//+------------------------------------------------------------------+
//|                                                        Stack.mqh |
//|                                      Copyright 2023, Yuto Nagano |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2023, Yuto Nagano"
#property link "https://www.mql5.com"
#property strict

template <typename T>
class Stack
{
private:
    T elements[]; // 要素を格納する動的配列
    int top;      // スタックのトップのインデックス
    int capacity; // 現在の配列の容量

    // 配列のサイズを調整する（必要に応じて容量を倍増）
    void EnsureCapacity()
    {
        if (top + 1 >= capacity)
        {
            capacity *= 2;                   // 容量を倍増
            ArrayResize(elements, capacity); // 配列のサイズを調整
        }
    }

public:
    // コンストラクタ
    Stack()
    {
        top = -1;
        capacity = 8; // 初期容量を8に設定
        ArrayResize(elements, capacity);
    }

    // スタックに要素を追加する（プッシュ）
    void Push(T element)
    {
        EnsureCapacity();          // 必要に応じて容量を確認・調整
        elements[++top] = element; // 要素を追加して、トップのインデックスを更新
    }

    // スタックから要素を取り出す（ポップ）
    bool Pop(T &element)
    {
        if (IsEmpty())
        {
            return false; // スタックが空のため、要素を取り出せない
        }
        element = elements[top--]; // トップの要素を取り出して、トップのインデックスを更新
        return true;               // 要素の取り出しが成功
    }

    // スタックが空かどうかを確認する
    bool IsEmpty() const
    {
        return top == -1;
    }

    // スタックに格納されている要素の数を返す
    int Size() const
    {
        return top + 1;
    }
};