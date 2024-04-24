//+------------------------------------------------------------------+
//|                                                 KopiLibTests.mq5 |
//|                                      Copyright 2024, Yuto Nagano |
//+------------------------------------------------------------------+
#property copyright "Copyright 2024, Yuto Nagano"
#property script_show_inputs
#property strict

#include <KopiLib/List.mqh>
#include <KopiLib/Dictionary.mqh>
#include <KopiLib/Queue.mqh>
#include <KopiLib/Stack.mqh>
#include <KopiLib/Json.mqh>
#include <KopiLib/HttpClient.mqh>

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
    TestList();
    TestListString();
    TestDictionary();
    TestQueue();
    TestStack();
    TestSimpleString();
    TestNumber();
    TestBoolean();
    TestNull();
    TestComplexObject();
    TestArray();
    TestHttpClient();
    Print("All tests completed.");
    //---
    return (INIT_SUCCEEDED);
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
    //---
}
//+------------------------------------------------------------------+

void TestList()
{
    List<double> list;

    // Test 1: Add and Get
    list.Add(1.1);
    list.Add(2.2);
    if (list.Get(0) == 1.1 && list.Get(1) == 2.2)
        Print("Test 1 Passed: Add and Get work correctly.");
    else
        Print("Test 1 Failed: Add and Get do not work correctly.");

    // Test 2: AddRange
    double values[] = {3.3, 4.4, 5.5};
    list.AddRange(values);
    if (list.Size() == 5 && list.Get(4) == 5.5)
        Print("Test 2 Passed: AddRange works correctly.");
    else
        Print("Test 2 Failed: AddRange does not work correctly.");

    // Test 3: RemoveAt
    list.RemoveAt(1); // Remove 2.2
    if (list.Size() == 4 && list.Get(1) == 3.3)
        Print("Test 3 Passed: RemoveAt works correctly.");
    else
        Print("Test 3 Failed: RemoveAt does not work correctly.");

    // Test 4: InsertAt
    list.InsertAt(1, 2.2); // Insert 2.2 back at index 1
    if (list.Get(1) == 2.2 && list.Size() == 5)
        Print("Test 4 Passed: InsertAt works correctly.");
    else
        Print("Test 4 Failed: InsertAt does not work correctly.");

    // Test 5: Contains
    if (list.Contains(2.2))
        Print("Test 5 Passed: Contains works correctly.");
    else
        Print("Test 5 Failed: Contains does not work correctly.");

    // Test 6: Clear
    list.Clear();
    if (list.Size() == 0)
        Print("Test 6 Passed: Clear works correctly.");
    else
        Print("Test 6 Failed: Clear does not work correctly.");
}
void TestListString()
{
    List<string> list;

    // Test 1: Add and Get
    list.Add("Hello");
    list.Add("World");
    if (list.Get(0) == "Hello" && list.Get(1) == "World")
        Print("Test 1 Passed: Add and Get work correctly.");
    else
        Print("Test 1 Failed: Add and Get do not work correctly.");

    // Test 2: AddRange
    string values[] = {"one", "two", "three"};
    list.AddRange(values);
    if (list.Size() == 5 && list.Get(4) == "three")
        Print("Test 2 Passed: AddRange works correctly.");
    else
        Print("Test 2 Failed: AddRange does not work correctly.");

    // Test 3: RemoveAt
    list.RemoveAt(1); // Remove "World"
    if (list.Size() == 4 && list.Get(1) == "one")
        Print("Test 3 Passed: RemoveAt works correctly.");
    else
        Print("Test 3 Failed: RemoveAt does not work correctly.");

    // Test 4: InsertAt
    list.InsertAt(1, "World"); // Insert "World" back at index 1
    if (list.Get(1) == "World" && list.Size() == 5)
        Print("Test 4 Passed: InsertAt works correctly.");
    else
        Print("Test 4 Failed: InsertAt does not work correctly.");

    // Test 5: Contains
    if (list.Contains("World"))
        Print("Test 5 Passed: Contains works correctly.");
    else
        Print("Test 5 Failed: Contains does not work correctly.");

    // Test 6: Clear
    list.Clear();
    if (list.Size() == 0)
        Print("Test 6 Passed: Clear works correctly.");
    else
        Print("Test 6 Failed: Clear does not work correctly.");
}

void TestDictionary()
{
    Dictionary<string, int> dict;

    // Test 1: Add and Get
    dict.Put("apple", 1);
    dict.Put("banana", 2);
    if (dict.Get("apple") == 1 && dict.Get("banana") == 2)
        Print("Test 1 Passed: Add and Get work correctly.");
    else
        Print("Test 1 Failed: Add and Get do not work correctly.");

    // Test 2: Remove
    dict.Remove("apple");
    if (!dict.ContainsKey("apple"))
        Print("Test 2 Passed: Remove works correctly.");
    else
        Print("Test 2 Failed: Remove does not work correctly.");

    // Test 3: Contains Key and Value
    if (dict.ContainsKey("banana") && dict.ContainsValue(2))
        Print("Test 3 Passed: ContainsKey and ContainsValue work correctly.");
    else
        Print("Test 3 Failed: ContainsKey and ContainsValue do not work correctly.");

    // Test 4: Clear
    dict.Clear();
    if (dict.Size() == 0)
        Print("Test 4 Passed: Clear works correctly.");
    else
        Print("Test 4 Failed: Clear does not work correctly.");
}

void TestQueue()
{
    Queue<double> queue;
    double element;

    // Test 1: Enqueue and Dequeue
    queue.Enqueue(10.5);
    queue.Enqueue(20.5);
    if (queue.Dequeue(element) && element == 10.5)
        Print("Test 1 Passed: Enqueue and Dequeue work correctly.");
    else
        Print("Test 1 Failed: Enqueue and Dequeue do not work correctly.");

    // Test 2: Dequeue from empty queue
    queue.Dequeue(element); // Remove remaining element
    if (!queue.Dequeue(element))
        Print("Test 2 Passed: Dequeue from empty queue returns false.");
    else
        Print("Test 2 Failed: Dequeue from empty queue did not return false.");

    // Test 3: Automatic capacity expansion
    for (int i = 0; i < 100; i++)
        queue.Enqueue(i);
    bool capacityTestPassed = true;
    for (int i = 0; i < 100; i++)
    {
        queue.Dequeue(element);
        if (element != i)
        {
            capacityTestPassed = false;
            break;
        }
    }
    if (capacityTestPassed)
        Print("Test 3 Passed: Automatic capacity expansion works.");
    else
        Print("Test 3 Failed: Automatic capacity expansion does not work.");

    // Test 4: Dequeue All
    for (int i = 0; i < 5; i++)
        queue.Enqueue(i);
    double allElements[];
    queue.DequeueAll(allElements);
    bool dequeueAllTestPassed = ArraySize(allElements) == 5;
    for (int i = 0; i < ArraySize(allElements); i++)
    {
        if (allElements[i] != i)
        {
            dequeueAllTestPassed = false;
            break;
        }
    }
    if (dequeueAllTestPassed)
        Print("Test 4 Passed: Dequeue All works correctly.");
    else
        Print("Test 4 Failed: Dequeue All does not work correctly.");
}

void TestStack()
{
    Stack<double> stack;

    // Test 1: Push and Pop
    stack.Push(10.1);
    stack.Push(20.2);
    double element;
    if (stack.Pop(element) && element == 20.2 && stack.Pop(element) && element == 10.1)
        Print("Test 1 Passed: Push and Pop work correctly.");
    else
        Print("Test 1 Failed: Push and Pop do not work correctly.");

    // Test 2: Pop from empty stack
    if (!stack.Pop(element))
        Print("Test 2 Passed: Pop from an empty stack returns false.");
    else
        Print("Test 2 Failed: Pop from an empty stack did not return false.");

    // Test 3: Stack size and IsEmpty
    stack.Push(30.3);
    stack.Push(40.4);
    if (stack.Size() == 2 && !stack.IsEmpty())
        Print("Test 3 Passed: Size and IsEmpty work correctly after pushes.");
    else
        Print("Test 3 Failed: Size and IsEmpty do not work correctly after pushes.");

    // Clearing the stack and checking IsEmpty
    stack.Pop(element);
    stack.Pop(element);
    if (stack.IsEmpty())
        Print("Test 3 Additional: Stack is empty after clearing.");
    else
        Print("Test 3 Additional: Stack is not empty after clearing.");
}

// 単純な文字列の値を持つJSONオブジェクトのテスト
void TestSimpleString()
{
    string json = "{\"name\":\"John\"}";
    JToken *token = JToken::Parse(json);
    JObject *obj = dynamic_cast<JObject *>(token);
    if (obj != NULL && StringCompare(dynamic_cast<JValue *>(obj.Get("name")).AsString(), "John") == 0)
        Print("TestSimpleString passed.");
    else
        Print("TestSimpleString failed.");
}

// 数値を持つJSONオブジェクトのテスト
void TestNumber()
{
    string json = "{\"age\":30}";
    JToken *token = JToken::Parse(json);
    JObject *obj = dynamic_cast<JObject *>(token);
    if (obj != NULL && dynamic_cast<JValue *>(obj.Get("age")).AsNumber() == 30)
        Print("TestNumber passed.");
    else
        Print("TestNumber failed.");
}

// ブール値を持つJSONオブジェクトのテスト
void TestBoolean()
{
    string json = "{\"valid\":true}";
    JToken *token = JToken::Parse(json);
    JObject *obj = dynamic_cast<JObject *>(token);
    if (obj != NULL && dynamic_cast<JValue *>(obj.Get("valid")).AsBoolean())
        Print("TestBoolean passed.");
    else
        Print("TestBoolean failed.");
}

// nullを含むJSONオブジェクトのテスト
void TestNull()
{
    string json = "{\"item\":null}";
    JToken *token = JToken::Parse(json);
    JObject *obj = dynamic_cast<JObject *>(token);
    if (obj != NULL && dynamic_cast<JValue *>(obj.Get("item")).IsNull())
        Print("TestNull passed.");
    else
        Print("TestNull failed.");
}

// 複合的なJSONオブジェクトのテスト
void TestComplexObject()
{
    string json = "{\"person\":{\"name\":\"Alice\",\"age\":25},\"valid\":true}";
    JToken *token = JToken::Parse(json);
    JObject *obj = dynamic_cast<JObject *>(token);
    JToken *person_token = obj.Get("person");
    JObject *person = dynamic_cast<JObject *>(person_token);
    if (obj != NULL && StringCompare(dynamic_cast<JValue *>(person.Get("name")).AsString(), "Alice") == 0 && dynamic_cast<JValue *>(person.Get("age")).AsNumber() == 25 && dynamic_cast<JValue *>(obj.Get("valid")).AsBoolean())
        Print("TestComplexObject passed.");
    else
        Print("TestComplexObject failed.");
}

// JSON配列のテスト
void TestArray()
{
    string json = "[1, 2, 3, \"four\"]";
    JToken *token = JToken::Parse(json);
    JArray *array = dynamic_cast<JArray *>(token);
    if (array != NULL && dynamic_cast<JValue *>(array.Get(0)).AsNumber() == 1 && dynamic_cast<JValue *>(array.Get(1)).AsNumber() == 2 && dynamic_cast<JValue *>(array.Get(2)).AsNumber() == 3 && StringCompare(dynamic_cast<JValue *>(array.Get(3)).AsString(), "four") == 0)
        Print("TestArray passed.");
    else
        Print("TestArray failed.");
}

void TestHttpClient()
{
    // HttpClient インスタンスの初期化
    HttpClient client("https://httpbin.org/");

    // GET リクエストのテスト
    HttpResponse getResponse = client.Get("/get");
    Print("GET /get Response:");
    Print("Status Code: ", getResponse.statusCode);
    Print("Body: ", getResponse.body);

    // POST リクエストのテスト
    string postData = "key1=value1&key2=value2";
    HttpResponse postResponse = client.Post("/post", postData);
    Print("POST /post Response:");
    Print("Status Code: ", postResponse.statusCode);
    Print("Body: ", postResponse.body);

    // 応答ヘッダーをチェック
    Print("Response Headers for POST:");
    for (int i = 0; i < postResponse.headers.Size(); i++)
    {
        KeyValuePair<string, string> kvp = postResponse.headers.Get(i);
        Print(kvp.key, ": ", kvp.value);
    }
}