//+------------------------------------------------------------------+
//|                                                   HttpClient.mqh |
//|                                      Copyright 2024, Yuto Nagano |
//+------------------------------------------------------------------+
#property copyright "Copyright 2024, Yuto Nagano"
#property strict

#include <KopiLib/Dictionary.mqh>

struct HttpResponse
{
    int statusCode;                     // HTTP ステータスコード
    int errorCode;                      // エラーコード(GetLastError)
    Dictionary<string, string> headers; // レスポンスヘッダー
    string body;                        // レスポンスボディ
};

class HttpClient
{
private:
    string m_userAgent;                   // ユーザーエージェント
    string m_contentType;                 // コンテントタイプ
    string m_baseUrl;                     // ベースURL
    Dictionary<string, string> m_cookies; // クッキーを保存するためのディクショナリー

public:
    HttpClient(string baseUrl, string userAgent = "MQL4/5 HTTP Client", string contentType = "application/x-www-form-urlencoded")
    {
        m_baseUrl = baseUrl;
        m_userAgent = userAgent;
        m_contentType = contentType;
        m_cookies.Clear(); // クッキーのディクショナリーを初期化
    }

    void SetUserAgent(string ua)
    {
        m_userAgent = ua;
    }

    void SetContentType(string ct)
    {
        m_contentType = ct;
    }

    HttpResponse Get(string url);
    HttpResponse Get(string url, Dictionary<string, string> &headers);
    HttpResponse Post(string url, string body);
    HttpResponse Post(string url, string body, Dictionary<string, string> &headers);
    HttpResponse SendRequest(string method, string uri, string body, Dictionary<string, string> &headers);
};

// HTTPClient のメソッド実装
HttpResponse HttpClient::Get(string url)
{
    Dictionary<string, string> headers;
    return SendRequest("GET", url, "", headers);
}

HttpResponse HttpClient::Get(string url, Dictionary<string, string> &headers)
{
    return SendRequest("GET", url, "", headers);
}

HttpResponse HttpClient::Post(string url, string body)
{
    Dictionary<string, string> headers;
    return SendRequest("POST", url, body, headers);
}

HttpResponse HttpClient::Post(string url, string body, Dictionary<string, string> &headers)
{
    return SendRequest("POST", url, body, headers);
}

HttpResponse HttpClient::SendRequest(string method, string url, string body, Dictionary<string, string> &headers)
{
    HttpResponse response;
    string requestHeaders = "";
    if (!headers.ContainsKey("Content-Type"))
    {
        requestHeaders += "Content-Type: " + m_contentType + "\r\n";
    }
    if (!headers.ContainsKey("User-Agent"))
    {
        requestHeaders += "User-Agent: " + m_userAgent + "\r\n";
    }
    if (!headers.ContainsKey("Cookie") && m_cookies.Size() > 0)
    {
        string cookieHeader = "";
        for (int i = 0; i < m_cookies.Size(); i++)
        {
            KeyValuePair<string, string> kvp = m_cookies.Get(i);
            if (i > 0)
                cookieHeader += "; ";
            cookieHeader += kvp.key + "=" + kvp.value;
        }
        requestHeaders += "Cookie: " + cookieHeader + "\r\n";
    }
    if (headers.Size() > 0)
    {
        // 追加ヘッダーの処理
        for (int i = 0; i < headers.Size(); i++)
        {
            KeyValuePair<string, string> kvp = headers.Get(i);
            requestHeaders += kvp.key + ": " + kvp.value + "\r\n";
        }
    }
    char data[];
    char result[];
    string result_headers;
    if (StringLen(body) != 0)
    {
        StringToCharArray(body, data, 0, StringLen(body), CP_UTF8);
    }
    int statusCode = WebRequest(method, m_baseUrl + url, requestHeaders, 5000, data, result, result_headers);
    if (statusCode > 0)
    {
        // ステータスコードとレスポンスボディを設定
        response.statusCode = statusCode;
        response.body = CharArrayToString(result);
        string lines[];
        StringSplit(result_headers, '\n', lines);

        for (int i = 0; i < ArraySize(lines); i++)
        {
            string line = StringTrimRight(lines[i]);
            if (StringFind(line, "Set-Cookie:") == 0)
            {
                string cookie = StringSubstr(line, StringLen("Set-Cookie:"));
                int semicolonPos = StringFind(cookie, ";");
                if (semicolonPos > 0)
                    cookie = StringSubstr(cookie, 0, semicolonPos);
                int equalPos = StringFind(cookie, "=");
                if (equalPos > 0)
                {
                    string key = StringSubstr(cookie, 0, equalPos);
                    string value = StringSubstr(cookie, equalPos + 1);
                    m_cookies.Put(key, value);
                }
            }
            int delimiterPos = StringFind(line, ":");
            if (delimiterPos > 0)
            {
                string key = StringSubstr(line, 0, delimiterPos);
                string value = StringSubstr(line, delimiterPos + 1);
                key = StringTrimRight(StringTrimLeft(key));
                value = StringTrimRight(StringTrimLeft(value));
                response.headers.Put(key, value);
            }
        }
    }
    else
    {
        // エラーハンドリング
        response.statusCode = -1; // エラーを示すために負のステータスコードを設定
        response.errorCode = GetLastError();
    }
    return response;
}
