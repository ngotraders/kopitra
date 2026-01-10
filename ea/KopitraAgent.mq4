#property strict
#property version   "0.2.0"
#property description "Kopitra copy-trading agent for MT4 terminals (kopitra)"
#property copyright "Kopitra"

#include "KopitraLib.mqh"

input string InpApiBaseUrl            = "https://localhost:8080";
input string InpAccountId             = "";
input string InpAuthMethod            = "account_session_key";
input string InpAuthKey               = "";
input string InpDeviceId              = "";
input int    InpHeartbeatSeconds      = 15;
input int    InpPollSeconds           = 5;
input int    InpSnapshotSeconds       = 60;
input int    InpSessionRetrySeconds   = 10;
input int    InpHttpTimeoutMs         = 5000;
input bool   InpEnableOrderSubmission = false;

KopitraAgentContext g_kopitraContext;

// MT4: Tracking open orders to detect execution changes
int g_lastOrderCount = 0;

void KopitraApplyInputConfig(KopitraConfig &config)
  {
   config.apiBaseUrl           = KopitraTrimString(InpApiBaseUrl);
   config.accountId            = KopitraTrimString(InpAccountId);
   config.authMethod           = KopitraNormalizeAuthMethod(InpAuthMethod);
   config.authKey              = KopitraTrimString(InpAuthKey);
   config.deviceId             = KopitraTrimString(InpDeviceId);
   config.enableOrderSubmission= InpEnableOrderSubmission;
   config.heartbeatIntervalSeconds = (InpHeartbeatSeconds<=0 ? 15 : InpHeartbeatSeconds);
   config.pollIntervalSeconds      = (InpPollSeconds<=0 ? 5 : InpPollSeconds);
   config.snapshotIntervalSeconds  = (InpSnapshotSeconds<=0 ? 60 : InpSnapshotSeconds);
   config.sessionRetrySeconds      = (InpSessionRetrySeconds<=0 ? 10 : InpSessionRetrySeconds);
   config.httpTimeoutMs            = (InpHttpTimeoutMs<1000 ? 5000 : InpHttpTimeoutMs);
  }

void KopitraDetectExecutionChanges()
  {
   int currentOrderCount = OrdersTotal();
   
   if(currentOrderCount != g_lastOrderCount)
     {
      KopitraLogInfo(StringFormat("Order count changed from %d to %d - detecting executions",g_lastOrderCount,currentOrderCount));
      
      // Scan open orders for execution details
      for(int i=0; i<currentOrderCount; i++)
        {
         if(!OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
            continue;
         
         int digits=KopitraSymbolDigits(OrderSymbol());
         string executionData="{";
         executionData+="\"orderTicket\":"+IntegerToString(OrderTicket());
         executionData+=",\"symbol\":\""+KopitraJsonEscape(OrderSymbol())+"\"";
         executionData+=",\"volume\":"+KopitraDoubleToJson(OrderLots(),2);
         executionData+=",\"openPrice\":"+KopitraDoubleToJson(OrderOpenPrice(),digits);
         executionData+=",\"stopLoss\":"+KopitraDoubleToJson(OrderStopLoss(),digits);
         executionData+=",\"takeProfit\":"+KopitraDoubleToJson(OrderTakeProfit(),digits);
         executionData+=",\"profit\":"+KopitraDoubleToJson(OrderProfit(),2);
         executionData+=",\"type\":"+IntegerToString(OrderType());
         executionData+=",\"openTime\":\""+KopitraFormatIso8601(OrderOpenTime())+"\"";
         executionData+=",\"executionStatus\":\"Executed\"";
         executionData+="}";
         
         KopitraSubmitExecution(g_kopitraContext,executionData);
        }
      
      g_lastOrderCount=currentOrderCount;
     }
  }

int OnInit()
  {
   KopitraLogInfo("Initializing KopitraAgent (MT4 - kopitra)");
   KopitraConfig config;
   KopitraApplyInputConfig(config);

   if(!KopitraStartup(g_kopitraContext,config))
      return(INIT_FAILED);

   if(!EventSetTimer(1))
      KopitraLogWarn("Failed to configure the 1-second timer; relying on ticks for background work.");

   if(!KopitraEnsureSession(g_kopitraContext))
      KopitraLogWarn("Initial session handshake deferred; will retry automatically.");

   g_lastOrderCount=OrdersTotal();
   KopitraOnTimer(g_kopitraContext);
   return(INIT_SUCCEEDED);
  }

void OnDeinit(const int reason)
  {
   EventKillTimer();
   KopitraShutdown(g_kopitraContext,reason);
  }

void OnTick()
  {
   KopitraOnTick(g_kopitraContext);
   KopitraDetectExecutionChanges();
  }

void OnTimer()
  {
   KopitraOnTimer(g_kopitraContext);
  }

void OnTrade()
  {
   KopitraOnTrade(g_kopitraContext);
   KopitraDetectExecutionChanges();
  }

