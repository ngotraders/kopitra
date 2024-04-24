//+------------------------------------------------------------------+
//|                                                    ApiClient.mqh |
//|                                      Copyright 2023, Yuto Nagano |
//+------------------------------------------------------------------+
#property copyright "Copyright 2023, Yuto Nagano"
#property strict

#include <KopiLib/Dictionary.mqh>
#include <KopiLib/Json.mqh>
#include <KopiLib/HttpClient.mqh>

struct Config
{
    string role;
    double percentage;
    double leverage;
    string followStrategy;
    double balanceRatio;
    double fixedLot;

    bool FromJson(string json)
    {
        // JSONからConfigオブジェクトを生成
        JToken *token = JToken::Parse(json);
        if (token != NULL && token.GetType() == "JObject")
        {
            JObject *obj = dynamic_cast<JObject *>(token);
            JValue *roleValue = obj.GetValue("role");
            JObject *settings = obj.GetObject("settings");
            if (roleValue == NULL)
            {
                return false;
            }

            role = roleValue == NULL ? "" : roleValue.AsString();
            if (settings == NULL)
            {
                return true;
            }
            JValue *leverageValue = settings.GetValue("leverage");
            JValue *percentageValue = settings.GetValue("percentage");
            JValue *followStrategyValue = settings.GetValue("followStrategy");
            JValue *balanceRatioValue = settings.GetValue("balanceRatio");
            JValue *fixedLotValue = settings.GetValue("fixedLot");

            leverage = leverageValue == NULL ? 0 : leverageValue.AsNumber();
            percentage = percentageValue == NULL ? 0 : percentageValue.AsNumber();
            followStrategy = followStrategyValue == NULL ? "" : followStrategyValue.AsString();
            balanceRatio = balanceRatioValue == NULL ? 0 : balanceRatioValue.AsNumber();
            fixedLot = fixedLotValue == NULL ? 0 : fixedLotValue.AsNumber();
            delete obj;
        }
        return true;
    }

    string ToJson()
    {
        return "";
    }
};

class ApiClient
{
private:
    string m_baseUrl;
    string m_eaKey;
    string m_eaVersion;
    string m_sessionToken;
    int m_lastErrorCode; // 最後のエラーコードを保持
    HttpClient *m_httpClient;

public:
    // コンストラクタ
    ApiClient(string baseUrl, string eaKey, string eaVersion)
    {
        m_baseUrl = baseUrl;
        m_eaKey = eaKey;
        m_eaVersion = eaVersion;
        m_sessionToken = "";
        m_lastErrorCode = 0;
        m_httpClient = new HttpClient(baseUrl);
    }

    // セッショントークンのゲッター
    string GetSessionToken()
    {
        return m_sessionToken;
    }

    // 最後のエラーコードのゲッター
    int GetLastError()
    {
        return m_lastErrorCode;
    }

    // 認証メソッド。成功時はtrue、失敗時はfalseを返す。
    bool Authenticate()
    {
        // ヘッダーの設定
        Dictionary<string, string> headers;
        headers.Put("X-EA-Key", m_eaKey);
        headers.Put("X-EA-Version", m_eaVersion);

        // POSTリクエストを送信
        HttpResponse response = m_httpClient.Post("/session", "", headers);

        // レスポンスのステータスコードに応じて処理
        if (response.statusCode == 200)
        {
            // 正常にセッショントークンを取得
            JToken *token = JToken::Parse(response.body);
            if (token != NULL && token.GetType() == "JObject")
            {
                JObject *obj = dynamic_cast<JObject *>(token);
                m_sessionToken = obj.GetValue("sessionToken").AsString();
                delete obj;
                m_lastErrorCode = 0; // エラーコードをリセット
                return true;
            }
            m_lastErrorCode = -1; // 適切なレスポンスだが、期待するデータがない場合
        }
        else
        {
            // ステータスコードに基づくエラーコードを設定
            m_lastErrorCode = response.statusCode;
        }

        return false;
    }

    bool GetConfig(Config &config)
    {
        if (m_sessionToken == "")
        {
            m_lastErrorCode = -1; // 適切なエラーコードを設定
            return false;
        }

        // ヘッダーの設定
        Dictionary<string, string> headers;
        headers.Put("X-EA-Key", m_eaKey);
        headers.Put("X-EA-Version", m_eaVersion);
        headers.Put("Authorization", "Bearer " + m_sessionToken);

        HttpResponse response = m_httpClient.Get("/config", headers);
        if (response.statusCode == 401)
        {
            if (!Authenticate())
            {
                return false;
            }
            headers.Put("Authorization", "Bearer " + m_sessionToken);
            response = m_httpClient.Get("/config", headers);
        }

        if (response.statusCode == 200)
        {
            if (config.FromJson(response.body))
            {
                m_lastErrorCode = 0; // エラーコードをリセット
                return true;
            }
            else
            {
                m_lastErrorCode = -2; // 適切なレスポンスだが、期待するデータがない場合
                return false;
            }
        }
        else
        {
            m_lastErrorCode = response.statusCode;
            return false;
        }
    }

    // クライアント設定を更新 (POST /config)
    bool UpdateConfig(Config &config)
    {
        if (m_sessionToken == "")
        {
            m_lastErrorCode = -1; // 適切なエラーコードを設定
            return false;
        }

        // ヘッダーの設定
        Dictionary<string, string> headers;
        headers.Put("X-EA-Key", m_eaKey);
        headers.Put("X-EA-Version", m_eaVersion);
        headers.Put("Authorization", "Bearer " + m_sessionToken);

        // ConfigオブジェクトをJSONに変換
        string jsonConfig = config.ToJson();
        HttpResponse response = m_httpClient.Post("/config", jsonConfig, headers);
        if (response.statusCode == 401)
        {
            if (!Authenticate())
            {
                return false;
            }
            headers.Put("Authorization", "Bearer " + m_sessionToken);
            response = m_httpClient.Post("/config", jsonConfig, headers);
        }

        if (response.statusCode == 200)
        {
            m_lastErrorCode = 0;
            return true;
        }
        else
        {
            m_lastErrorCode = response.statusCode;
            return false;
        }
    }
};
