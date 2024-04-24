//+------------------------------------------------------------------+
//|                                                         Json.mqh |
//|                                      Copyright 2024, Yuto Nagano |
//+------------------------------------------------------------------+
#property copyright "Copyright 2024, Yuto Nagano"
#property strict

class JValue;
class JArray;
class JObject;

// JToken: JSONデータの基底クラス
class JToken
{
private:
public:
    virtual string GetType() { return "JToken"; }
    virtual string ToString() { return ""; }

    // JSON文字列を解析してJTokenオブジェクトを返す静的メソッド
    static JToken *Parse(string json);
};

// JValue: 基本的なJSON値を表すクラス
class JValue : public JToken
{
private:
    enum ValueType
    {
        JV_STRING,
        JV_NUMBER,
        JV_BOOLEAN,
        JV_NULL_TYPE
    };
    ValueType type;
    string sValue; // 文字列としての値
    double nValue; // 数値としての値
    bool bValue;   // ブール値としての値

public:
    virtual string GetType() override { return "JValue"; }

    // JValueの型に応じて適切な値を文字列として返す
    virtual string ToString()
    {
        switch (type)
        {
        case JV_STRING:
            return "\"" + sValue + "\"";
        case JV_NUMBER:
            return DoubleToString(nValue);
        case JV_BOOLEAN:
            return bValue ? "true" : "false";
        default:
            return "null";
        }
    }

    // 文字列値を持つJValueのコンストラクタ
    JValue(string value) : sValue(value), type(JV_STRING) {}

    // 数値値を持つJValueのコンストラクタ
    JValue(double value) : nValue(value), type(JV_NUMBER) {}

    // ブール値を持つJValueのコンストラクタ
    JValue(bool value) : bValue(value), type(JV_BOOLEAN) {}

    // null を表すJValueのコンストラクタ
    JValue() : type(JV_NULL_TYPE) {}

    // STRING 判定
    bool IsString() const
    {
        return type == JV_STRING;
    }

    // NUMBER 判定
    bool IsNumber() const
    {
        return type == JV_NUMBER;
    }

    // BOOLEAN 判定
    bool IsBoolean() const
    {
        return type == JV_BOOLEAN;
    }

    // null 判定
    bool IsNull() const
    {
        return type == JV_NULL_TYPE;
    }

    // 文字列として値を返す
    string AsString()
    {
        if (type == JV_STRING)
            return sValue;
        else
            return ToString(); // 数値やブール値の場合、文字列に変換して返す
    }

    // 数値として値を返す
    double AsNumber()
    {
        if (type == JV_NUMBER)
            return nValue;
        else
            return 0; // 数値以外の型の場合は0を返す（エラーハンドリングを考慮）
    }

    // ブール値として値を返す
    bool AsBoolean()
    {
        if (type == JV_BOOLEAN)
            return bValue;
        else
            return false; // ブール値以外の型の場合はfalseを返す（エラーハンドリングを考慮）
    }
};

// JArray: JSON配列を表すクラス
class JArray : public JToken
{
private:
    JToken *values[];
    int size;     // 実際に格納されている要素の数
    int capacity; // 配列の容量

    // 配列の容量を増やす（必要に応じて倍増）
    void EnsureCapacity(int minCapacity)
    {
        if (capacity == 0)
        {
            capacity = 8;
            ArrayResize(values, capacity);
        }
        if (capacity < minCapacity)
        {
            while (capacity < minCapacity)
                capacity *= 2;
            ArrayResize(values, capacity);
        }
    }

public:
    ~JArray()
    {
        for (int i = 0; i < size; i++)
        {
            delete values[i];
        }
    }

    virtual string GetType() override { return "JArray"; }

    virtual string ToString()
    {
        string json = "[";
        for (int i = 0; i < size; i++)
        {
            if (i > 0)
                json += ",";
            json += values[i].ToString();
        }
        json += "]";
        return json;
    }

    void Add(JToken *value)
    {
        EnsureCapacity(size + 1);
        values[size++] = value;
    }

    JToken *Get(int i)
    {
        if (i >= size)
        {
            return NULL;
        }
        return values[i];
    }

    JObject *GetObject(int i)
    {
        JToken *token = Get(i);
        if (token.GetType() == "JObject")
        {
            return dynamic_cast<JObject *>(token);
        }
        else
        {
            return NULL;
        }
    }

    JArray *GetArray(int i)
    {
        JToken *token = Get(i);
        if (token.GetType() == "JArray")
        {
            return dynamic_cast<JArray *>(token);
        }
        else
        {
            return NULL;
        }
    }

    JValue *GetValue(int i)
    {
        JToken *token = Get(i);
        if (token.GetType() == "JValue")
        {
            return dynamic_cast<JValue *>(token);
        }
        else
        {
            return NULL;
        }
    }
};

// JObject: JSONオブジェクトを表すクラス
class JObject : public JToken
{
private:
    string keys[];
    JToken *values[];
    int size;     // 実際に格納されている要素の数
    int capacity; // 配列の容量

    // 配列の容量を増やす（必要に応じて倍増）
    void EnsureCapacity(int minCapacity)
    {
        if (capacity == 0)
        {
            capacity = 8;
            ArrayResize(keys, capacity);
            ArrayResize(values, capacity);
        }
        if (capacity < minCapacity)
        {
            while (capacity < minCapacity)
                capacity *= 2;
            ArrayResize(keys, capacity);
            ArrayResize(values, capacity);
        }
    }

public:
    ~JObject()
    {
        for (int i = 0; i < size; i++)
        {
            delete values[i];
        }
    }
    virtual string GetType() override { return "JObject"; }

    virtual string ToString()
    {
        string json = "{";
        for (int i = 0; i < size; i++)
        {
            if (i > 0)
                json += ", ";
            string key = keys[i];
            JToken *value = values[i];
            json += "\"" + key + "\":" + value.ToString();
        }
        json += "}";
        return json;
    }

    void Add(string key, JToken *value)
    {
        EnsureCapacity(capacity + 1);
        keys[size] = key;
        values[size] = value;
        size++;
    }

    JToken *Get(string key)
    {
        for (int i = 0; i < size; i++)
        {
            if (keys[i] == key)
            {
                return values[i];
            }
        }
        return NULL;
    }

    JObject *GetObject(string key)
    {
        JToken *token = Get(key);
        if (token.GetType() == "JObject")
        {
            return dynamic_cast<JObject *>(token);
        }
        else
        {
            return NULL;
        }
    }

    JArray *GetArray(string key)
    {
        JToken *token = Get(key);
        if (token.GetType() == "JArray")
        {
            return dynamic_cast<JArray *>(token);
        }
        else
        {
            return NULL;
        }
    }

    JValue *GetValue(string key)
    {
        JToken *token = Get(key);
        if (token.GetType() == "JValue")
        {
            return dynamic_cast<JValue *>(token);
        }
        else
        {
            return NULL;
        }
    }
};

class JsonParser
{
private:
    enum JsonTokenType
    {
        OBJECT_START,  // {
        OBJECT_END,    // }
        ARRAY_START,   // [
        ARRAY_END,     // ]
        STRING,        // "abc"
        NUMBER,        // 123, 12.3
        BOOLEAN_TRUE,  // true
        BOOLEAN_FALSE, // false
        NULL_VALUE,    // null
        COMMA,         // ,
        COLON,         // :
        EOF,           // 文字列の終わり
        ERROR          // 解析エラー
    };
    string json; // JSON 文字列
    int index;   // 現在の解析位置

    // 空白をスキップするヘルパー関数
    void SkipWhitespace()
    {
        while (index < StringLen(json) && (json[index] == ' ' || json[index] == '\n' || json[index] == '\r' || json[index] == '\t'))
        {
            index++;
        }
    }

    // 次のトークンを取得する
    JsonTokenType NextToken(string &tokenValue)
    {
        SkipWhitespace();
        if (index >= StringLen(json))
            return EOF; // 文字列の終わりをチェック

        switch (json[index])
        {
        case '{':
            index++;
            return OBJECT_START;
        case '}':
            index++;
            return OBJECT_END;
        case '[':
            index++;
            return ARRAY_START;
        case ']':
            index++;
            return ARRAY_END;
        case ',':
            index++;
            return COMMA;
        case ':':
            index++;
            return COLON;
        case '"': // 文字列の処理
            return ParseString(tokenValue);
        case 't': // true
            if (StringSubstr(json, index, 4) == "true")
            {
                index += 4;
                return BOOLEAN_TRUE;
            }
            break;
        case 'f': // false
            if (StringSubstr(json, index, 5) == "false")
            {
                index += 5;
                return BOOLEAN_FALSE;
            }
            break;
        case 'n': // null
            if (StringSubstr(json, index, 4) == "null")
            {
                index += 4;
                return NULL_VALUE;
            }
        default:
            if ((json[index] >= '0' && json[index] <= '9') || json[index] == '-')
            {
                return ParseNumber(tokenValue);
            }
            return ERROR; // 数値以外であればエラー
        }

        return ERROR;
    }

    // 数値を解析する補助関数
    JsonTokenType ParseString(string &tokenValue)
    {
        int start = ++index;
        while (index < StringLen(json) && json[index] != '"')
        {
            if (json[index] == '\\' && index + 1 < StringLen(json))
                index++; // エスケープシーケンスをスキップ
            index++;
        }
        if (index >= StringLen(json) || json[index] != '"')
            return ERROR; // 閉じ引用符がない場合
        tokenValue = StringSubstr(json, start, index - start);
        index++;
        return STRING;
    }

    // 数値を解析する補助関数
    JsonTokenType ParseNumber(string &tokenValue)
    {
        int start = index;
        bool isNegative = json[index] == '-';
        if (isNegative)
            index++;

        double number = 0;
        while (index < StringLen(json) && json[index] >= '0' && json[index] <= '9')
        {
            index++;
        }

        if (index < StringLen(json) && json[index] == '.')
        {
            index++;
            while (index < StringLen(json) && json[index] >= '0' && json[index] <= '9')
            {
                index++;
            }
        }

        tokenValue = StringSubstr(json, start, index - start);
        return NUMBER;
    }

public:
    // コンストラクタ
    JsonParser(const string &jsonStr) : json(jsonStr), index(0) {}

    JToken *ParseValue(JsonParser &parser)
    {
        string tokenValue;
        JsonTokenType tokenType = parser.NextToken(tokenValue);

        switch (tokenType)
        {
        case OBJECT_START:
            return ParseObject(parser);
        case ARRAY_START:
            return ParseArray(parser);
        case STRING:
            return new JValue(tokenValue);
        case NUMBER:
            return new JValue(StringToDouble(tokenValue));
        case BOOLEAN_TRUE:
            return new JValue(true);
        case BOOLEAN_FALSE:
            return new JValue(false);
        case NULL_VALUE:
            return new JValue();
        default:
            // Handle parsing error or unexpected token
            return NULL; // Placeholder for error handling
        }
    }

    JObject *ParseObject(JsonParser &parser)
    {
        JObject *object = new JObject();
        JToken *value;
        string tokenValue;

        while (true)
        {
            JsonTokenType tokenType = parser.NextToken(tokenValue);
            if (tokenType == OBJECT_END)
                break;
            if (tokenType != STRING)
            {
                delete object; // Cleanup on error
                return NULL;
            }

            tokenType = parser.NextToken(tokenValue); // Expecting COLON
            if (tokenType != COLON)
            {
                delete object; // Cleanup on error
                return NULL;
            }

            value = ParseValue(parser);
            if (!value)
            {
                delete object; // Cleanup on error
                return NULL;
            }

            object.Add(tokenValue, value);

            tokenType = parser.NextToken(tokenValue);
            if (tokenType == OBJECT_END)
                break;
            if (tokenType != COMMA)
            {
                delete object; // Cleanup on error
                return NULL;
            }
        }

        return object;
    }

    JArray *ParseArray(JsonParser &parser)
    {
        JArray *array = new JArray();
        JToken *value;
        string tokenValue;

        while (true)
        {
            value = ParseValue(parser);
            if (!value)
            {
                delete array; // Cleanup on error
                return NULL;
            }

            array.Add(value);
            JsonTokenType tokenType = parser.NextToken(tokenValue);
            if (tokenType == ARRAY_END)
                break;
            if (tokenType != COMMA)
            {
                delete array; // Cleanup on error
                return NULL;
            }
        }

        return array;
    }
};

// Parse different JSON structures based on the token from JsonParser
static JToken *JToken::Parse(string json)
{
    JsonParser parser(json);
    return parser.ParseValue(parser);
};
