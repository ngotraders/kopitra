#property strict
#property version   "0.1.0"
#property description "Kopitra copy-trading agent for MT4 terminals"
#property copyright "Kopitra"

#include "KopitraLib.mqh"

input string InpApiBaseUrl            = "https://localhost:8080";
input string InpAccountId             = "";
input string InpAuthMethod            = "AccountSessionKey";
input string InpAuthKey               = "";
input string InpDeviceId              = "";
input int    InpHeartbeatSeconds      = 15;
input int    InpPollSeconds           = 5;
input int    InpSnapshotSeconds       = 60;
input int    InpSessionRetrySeconds   = 10;
input int    InpHttpTimeoutMs         = 5000;
input bool   InpEnableOrderSubmission = false;

KopitraAgentContext g_kopitraContext;

void KopitraApplyInputConfig(KopitraConfig &config)
  {
   config.apiBaseUrl           = KopitraTrimString(InpApiBaseUrl);
   config.accountId            = KopitraTrimString(InpAccountId);
   config.authMethod           = KopitraTrimString(InpAuthMethod);
   if(StringLen(KopitraTrimString(config.authMethod))==0)
      config.authMethod        = "AccountSessionKey";
   config.authKey              = KopitraTrimString(InpAuthKey);
   config.deviceId             = KopitraTrimString(InpDeviceId);
   config.enableOrderSubmission= InpEnableOrderSubmission;
   config.heartbeatIntervalSeconds = (InpHeartbeatSeconds<=0 ? 15 : InpHeartbeatSeconds);
   config.pollIntervalSeconds      = (InpPollSeconds<=0 ? 5 : InpPollSeconds);
   config.snapshotIntervalSeconds  = (InpSnapshotSeconds<=0 ? 60 : InpSnapshotSeconds);
   config.sessionRetrySeconds      = (InpSessionRetrySeconds<=0 ? 10 : InpSessionRetrySeconds);
   config.httpTimeoutMs            = (InpHttpTimeoutMs<1000 ? 5000 : InpHttpTimeoutMs);
  }

int OnInit()
  {
   KopitraLogInfo("Initializing KopitraAgent (MT4)");
   KopitraConfig config;
   KopitraApplyInputConfig(config);

   if(!KopitraStartup(g_kopitraContext,config))
      return(INIT_FAILED);

   if(!EventSetTimer(1))
      KopitraLogWarn("Failed to configure the 1-second timer; relying on ticks for background work.");

   if(!KopitraEnsureSession(g_kopitraContext))
      KopitraLogWarn("Initial session handshake deferred; will retry automatically.");

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
  }

void OnTimer()
  {
   KopitraOnTimer(g_kopitraContext);
  }

void OnTrade()
  {
   KopitraOnTrade(g_kopitraContext);
  }
