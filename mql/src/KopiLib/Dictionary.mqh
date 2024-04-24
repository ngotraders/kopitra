//+------------------------------------------------------------------+
//|                                                   Dictionary.mqh |
//|                                      Copyright 2024, Yuto Nagano |
//+------------------------------------------------------------------+
#property copyright "Copyright 2024, Yuto Nagano"
#property strict

template <typename TKey, typename TValue>
struct KeyValuePair
{
    TKey key;
    TValue value;
};

template <typename TKey, typename TValue>
class Dictionary
{
private:
    KeyValuePair<TKey, TValue> array[];
    int size;
    int capacity;

    void EnsureCapacity(int minCapacity)
    {

        if (capacity == 0)
        {
            capacity = 8;
            ArrayResize(array, capacity);
        }
        if (capacity < minCapacity)
        {
            while (capacity < minCapacity)
                capacity *= 2;
            ArrayResize(array, capacity);
        }
    }

public:
    Dictionary()
    {
        size = 0;
        capacity = 8;
        ArrayResize(array, capacity);
    }

    void Put(TKey key, TValue value)
    {
        for (int i = 0; i < size; i++)
        {
            if (array[i].key == key)
            {
                array[i].value = value;
                return;
            }
        }
        EnsureCapacity(size + 1);
        array[size].key = key;
        array[size].value = value;
        size++;
    }

    void Remove(TKey key)
    {
        for (int i = 0; i < size; i++)
        {
            if (array[i].key == key)
            {
                for (int j = i + 1; j < size; j++)
                {
                    array[j - 1] = array[j];
                }
                size--;
                return;
            }
        }
        Print("Key not found");
    }

    TValue Get(TKey key)
    {
        for (int i = 0; i < size; i++)
        {
            if (array[i].key == key)
            {
                return array[i].value;
            }
        }
        Print("Key not found");
        return TValue();
    }

    void Keys(TKey &keys[])
    {
        ArrayResize(keys, size);
        for (int i = 0; i < size; i++)
        {
            keys[i] = array[i].key;
        }
    }

    void Values(TValue &values[])
    {
        ArrayResize(values, size);
        for (int i = 0; i < size; i++)
        {
            values[i] = array[i].value;
        }
    }

    int Size()
    {
        return size;
    }

    void Clear()
    {
        size = 0;
    }

    bool ContainsKey(TKey key)
    {
        for (int i = 0; i < size; i++)
        {
            if (array[i].key == key)
            {
                return true;
            }
        }
        return false;
    }

    bool ContainsValue(TValue value)
    {
        for (int i = 0; i < size; i++)
        {
            if (array[i].value == value)
            {
                return true;
            }
        }
        return false;
    }
};
