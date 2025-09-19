#property strict

#ifndef __KOPITRA_LIB_MQH__
#define __KOPITRA_LIB_MQH__

#define KOPITRA_LIB_VERSION "0.1.0"

enum KopitraSessionState
  {
   SESSION_STATE_IDLE = 0,
   SESSION_STATE_PENDING = 1,
   SESSION_STATE_AUTHENTICATED = 2
  };

struct KopitraConfig
  {
   string apiBaseUrl;
   string accountId;
   string authMethod;
   string authKey;
   string deviceId;
   bool   enableOrderSubmission;
   int    heartbeatIntervalSeconds;
   int    pollIntervalSeconds;
   int    snapshotIntervalSeconds;
   int    sessionRetrySeconds;
   int    httpTimeoutMs;
  };

struct KopitraSession
  {
   KopitraSessionState state;
   string              sessionId;
   string              authToken;
   string              outboxCursor;
   datetime            lastHeartbeat;
   datetime            lastPoll;
   datetime            lastAttempt;
   int                 retryAfterHint;
  };

struct KopitraAgentContext
  {
   KopitraConfig config;
   KopitraSession session;
   datetime nextHeartbeatAt;
   datetime nextPollAt;
   datetime nextSnapshotAt;
   bool     initRequestSent;
   bool     initialized;
   int      consecutiveFailures;
   ulong    requestSequence;
  };

string KopitraJsonEscape(string value);
string KopitraFormatIso8601(const datetime value);
string KopitraDoubleToJson(const double value,const int digits);
string KopitraSessionStateToString(const KopitraSessionState state);
KopitraSessionState KopitraParseSessionState(string statusText);
long   KopitraAccountLogin();
string KopitraAccountCurrency();
string KopitraAccountCompany();
int    KopitraAccountLeverage();
double KopitraAccountBalance();
double KopitraAccountEquity();
double KopitraAccountMarginFree();
string KopitraCollectSubscribedSymbolsJson();
string KopitraCollectOpenTradesJson();

void KopitraLog(const string level,const string message)
  {
   PrintFormat("[Kopitra][%s] %s",level,message);
  }

void KopitraLogInfo(const string message)
  {
   KopitraLog("INFO",message);
  }

void KopitraLogWarn(const string message)
  {
   KopitraLog("WARN",message);
  }

void KopitraLogError(const string message)
  {
   KopitraLog("ERROR",message);
  }

string KopitraTrimString(const string value)
  {
   string trimmed=value;
   StringTrimLeft(trimmed);
   StringTrimRight(trimmed);
   return(trimmed);
  }

string KopitraSanitizeIdentifier(string value)
  {
   StringReplace(value," ","-");
   StringReplace(value,"/","-");
   StringReplace(value,"\\","-");
   StringReplace(value,"--","-");
   return(value);
  }

string KopitraDefaultDeviceId()
  {
   const long login=KopitraAccountLogin();
   string terminal=TerminalInfoString(TERMINAL_NAME);
   terminal=KopitraSanitizeIdentifier(terminal);
#ifdef __MQL5__
   return(StringFormat("mt5-%s-%I64d",terminal,login));
#else
   return(StringFormat("mt4-%s-%I64d",terminal,login));
#endif
  }

string KopitraBuildUrl(const string baseUrl,const string path)
  {
   if(StringLen(path)>0 && (StringFind(path,"http://")==0 || StringFind(path,"https://")==0))
      return(path);

   string normalizedBase=baseUrl;
   if(StringLen(normalizedBase)==0)
      return(path);

   if(StringSubstr(normalizedBase,StringLen(normalizedBase)-1,1)=="/")
      normalizedBase=StringSubstr(normalizedBase,0,StringLen(normalizedBase)-1);

   string normalizedPath=path;
   if(StringLen(normalizedPath)==0)
      normalizedPath="";
   else
     {
      if(StringSubstr(normalizedPath,0,1)!="/")
         normalizedPath="/"+normalizedPath;
     }
   return(normalizedBase+normalizedPath);
  }

string KopitraSessionStateToString(const KopitraSessionState state)
  {
   switch(state)
     {
      case SESSION_STATE_PENDING:        return("pending");
      case SESSION_STATE_AUTHENTICATED:  return("authenticated");
      default:                           return("idle");
     }
  }

KopitraSessionState KopitraParseSessionState(string statusText)
  {
   statusText=StringToLower(statusText);
   if(statusText=="authenticated")
      return(SESSION_STATE_AUTHENTICATED);
   if(statusText=="pending")
      return(SESSION_STATE_PENDING);
   return(SESSION_STATE_IDLE);
  }

string KopitraFormatIso8601(const datetime value)
  {
   MqlDateTime dt;
   TimeToStruct(value,dt);
   return(StringFormat("%04d-%02d-%02dT%02d:%02d:%02dZ",dt.year,dt.mon,dt.day,dt.hour,dt.min,dt.sec));
  }

string KopitraDoubleToJson(const double value,const int digits)
  {
   return(DoubleToString(value,digits));
  }

string KopitraJsonEscape(string value)
  {
   StringReplace(value,"\\","\\\\");
   StringReplace(value,"\"","\\\"");
   StringReplace(value,"\r","\\r");
   StringReplace(value,"\n","\\n");
   StringReplace(value,"\t","\\t");
   return(value);
  }

string KopitraGenerateRequestId(KopitraAgentContext &ctx)
  {
   ctx.requestSequence++;
   ulong timeValue=(ulong)TimeCurrent();
   return(StringFormat("%s-%u-%u",(StringLen(ctx.config.deviceId)>0 ? ctx.config.deviceId : "kopitra"),(uint)(timeValue%1000000000),(uint)ctx.requestSequence));
  }

string KopitraGenerateIdempotencyKey(KopitraAgentContext &ctx,const string prefix)
  {
   ctx.requestSequence++;
   int partA=MathRand();
   int partB=MathRand();
   ulong tick=(ulong)GetTickCount();
   return(StringFormat("%s-%u-%u-%u",prefix,(uint)(tick%1000000),(uint)partA,(uint)ctx.requestSequence));
  }

long KopitraAccountLogin()
  {
#ifdef __MQL5__
   return((long)AccountInfoInteger(ACCOUNT_LOGIN));
#else
   return((long)AccountNumber());
#endif
  }

string KopitraAccountCurrency()
  {
#ifdef __MQL5__
   return(AccountInfoString(ACCOUNT_CURRENCY));
#else
   return(AccountCurrency());
#endif
  }

string KopitraAccountCompany()
  {
#ifdef __MQL5__
   return(AccountInfoString(ACCOUNT_COMPANY));
#else
   return(AccountCompany());
#endif
  }

int KopitraAccountLeverage()
  {
#ifdef __MQL5__
   return((int)AccountInfoInteger(ACCOUNT_LEVERAGE));
#else
   return(AccountLeverage());
#endif
  }

double KopitraAccountBalance()
  {
#ifdef __MQL5__
   return(AccountInfoDouble(ACCOUNT_BALANCE));
#else
   return(AccountBalance());
#endif
  }

double KopitraAccountEquity()
  {
#ifdef __MQL5__
   return(AccountInfoDouble(ACCOUNT_EQUITY));
#else
   return(AccountEquity());
#endif
  }

double KopitraAccountMarginFree()
  {
#ifdef __MQL5__
   return(AccountInfoDouble(ACCOUNT_MARGIN_FREE));
#else
   return(AccountFreeMargin());
#endif
  }

int KopitraSymbolDigits(const string symbol)
  {
#ifdef __MQL5__
   int digits=(int)SymbolInfoInteger(symbol,SYMBOL_DIGITS);
   if(digits<=0)
      digits=_Digits;
#else
   int digits=(int)MarketInfo(symbol,MODE_DIGITS);
   if(digits<=0)
      digits=Digits;
#endif
   return(digits);
  }

string KopitraCollectSubscribedSymbolsJson()
  {
   string json="[";
   bool first=true;
   int total=SymbolsTotal(true);
   for(int i=0;i<total;i++)
     {
      string sym=SymbolName(i,true);
      if(StringLen(sym)==0)
         continue;
      if(!first)
         json+=",";
      json+="\""+KopitraJsonEscape(sym)+"\"";
      first=false;
     }
   if(first)
      json+="\""+KopitraJsonEscape(Symbol())+"\"";
   json+="]";
   return(json);
  }

string KopitraCollectOpenTradesJson()
  {
   string json="[";
   bool first=true;
#ifdef __MQL5__
   int total=PositionsTotal();
   for(int i=0;i<total;i++)
     {
      if(!PositionSelectByIndex(i))
         continue;
      string symbol=PositionGetString(POSITION_SYMBOL);
      if(StringLen(symbol)==0)
         symbol=Symbol();
      double volume=PositionGetDouble(POSITION_VOLUME);
      double price=PositionGetDouble(POSITION_PRICE_OPEN);
      double stopLoss=PositionGetDouble(POSITION_SL);
      double takeProfit=PositionGetDouble(POSITION_TP);
      double profit=PositionGetDouble(POSITION_PROFIT);
      long ticket=(long)PositionGetInteger(POSITION_TICKET);
      int type=(int)PositionGetInteger(POSITION_TYPE);
      int digits=KopitraSymbolDigits(symbol);
      string entry=StringFormat("{\"ticket\":%s,\"symbol\":\"%s\",\"volume\":%s,\"type\":%d,\"price\":%s,\"stopLoss\":%s,\"takeProfit\":%s,\"profit\":%s}",
                                LongToString(ticket),
                                KopitraJsonEscape(symbol),
                                KopitraDoubleToJson(volume,2),
                                type,
                                KopitraDoubleToJson(price,digits),
                                KopitraDoubleToJson(stopLoss,digits),
                                KopitraDoubleToJson(takeProfit,digits),
                                KopitraDoubleToJson(profit,2));
      if(!first)
         json+=",";
      json+=entry;
      first=false;
     }
#else
   int total=OrdersTotal();
   for(int i=0;i<total;i++)
     {
      if(!OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
         continue;
      string symbol=OrderSymbol();
      double volume=OrderLots();
      double price=OrderOpenPrice();
      double stopLoss=OrderStopLoss();
      double takeProfit=OrderTakeProfit();
      double profit=OrderProfit();
      long ticket=OrderTicket();
      int type=OrderType();
      int digits=KopitraSymbolDigits(symbol);
      string entry=StringFormat("{\"ticket\":%s,\"symbol\":\"%s\",\"volume\":%s,\"type\":%d,\"price\":%s,\"stopLoss\":%s,\"takeProfit\":%s,\"profit\":%s}",
                                LongToString(ticket),
                                KopitraJsonEscape(symbol),
                                KopitraDoubleToJson(volume,2),
                                type,
                                KopitraDoubleToJson(price,digits),
                                KopitraDoubleToJson(stopLoss,digits),
                                KopitraDoubleToJson(takeProfit,digits),
                                KopitraDoubleToJson(profit,2));
      if(!first)
         json+=",";
      json+=entry;
      first=false;
     }
#endif
   json+="]";
   return(json);
  }

bool KopitraHttpRequest(KopitraAgentContext &ctx,const string method,const string path,const string body,string &response,const int timeoutMs,const string idempotencyKey="")
  {
   string url=KopitraBuildUrl(ctx.config.apiBaseUrl,path);
   string headers="User-Agent: KopitraAgent/"+KOPITRA_LIB_VERSION+"\r\n";
   if(StringLen(ctx.config.accountId)>0)
      headers+="X-TradeAgent-Account: "+ctx.config.accountId+"\r\n";
   headers+="X-TradeAgent-Request-ID: "+KopitraGenerateRequestId(ctx)+"\r\n";
   if(StringLen(idempotencyKey)>0)
      headers+="Idempotency-Key: "+idempotencyKey+"\r\n";
   if(StringLen(ctx.session.authToken)>0)
      headers+="Authorization: Bearer "+ctx.session.authToken+"\r\n";
   if(StringLen(body)>0)
      headers+="Content-Type: application/json\r\n";

   uchar requestData[];
   if(StringLen(body)>0)
     {
      int length=StringToCharArray(body,requestData,0,WHOLE_ARRAY,CP_UTF8);
      if(length>0)
         ArrayResize(requestData,length-1);
     }
   else
      ArrayResize(requestData,0);

   uchar result[];
   string resultHeaders="";
   ResetLastError();
   int status=WebRequest(method,url,headers,timeoutMs,requestData,result,resultHeaders);
   if(status==-1)
     {
      const int errorCode=GetLastError();
      ResetLastError();
      KopitraLogError(StringFormat("WebRequest failed (%d) for %s %s",errorCode,method,url));
      return(false);
     }

   response=CharArrayToString(result,0,WHOLE_ARRAY,CP_UTF8);

   if(status>=200 && status<300)
     {
      ctx.consecutiveFailures=0;
      return(true);
     }

   ctx.consecutiveFailures++;
   KopitraLogWarn(StringFormat("HTTP %d for %s %s (payload: %s)",status,method,url,response));
   return(false);
  }

string KopitraExtractJsonField(const string json,const string field)
  {
   string token="\""+field+"\":";
   int position=StringFind(json,token);
   if(position<0)
      return("");
   position+=StringLen(token);
   int length=StringLen(json);
   while(position<length)
     {
      int ch=StringGetCharacter(json,position);
      if(ch==' '||ch=='\t'||ch=='\r'||ch=='\n')
        {
         position++;
         continue;
        }
      if(ch=='"')
        {
         int start=position+1;
         int idx=start;
         while(idx<length)
           {
            int current=StringGetCharacter(json,idx);
            if(current=='\\')
              {
               idx+=2;
               continue;
              }
            if(current=='"')
              return(StringSubstr(json,start,idx-start));
            idx++;
           }
         return("");
        }
      int start=position;
      while(position<length)
        {
         int current=StringGetCharacter(json,position);
         if(current==','||current=='}'||current==']')
            break;
         position++;
        }
      string value=StringSubstr(json,start,position-start);
      value=KopitraTrimString(value);
      return(value);
     }
   return("");
  }

string KopitraExtractJsonFieldFromChunk(const string chunk,const string field)
  {
   return(KopitraExtractJsonField(chunk,field));
  }

bool KopitraExtractNextEventChunk(const string payload,int startIndex,int &chunkStart,int &chunkEnd)
  {
   int search=startIndex;
   int length=StringLen(payload);
   while(search<length)
     {
      int idIndex=StringFind(payload,"\"id\":\"",search);
      if(idIndex<0)
         return(false);
      int prefix=idIndex-1;
      while(prefix>=0)
        {
         int ch=StringGetCharacter(payload,prefix);
         if(ch==' '||ch=='\r'||ch=='\n'||ch=='\t')
           {
            prefix--;
            continue;
           }
         break;
        }
      if(prefix<0||StringGetCharacter(payload,prefix)!='{')
        {
         search=idIndex+4;
         continue;
        }
      int depth=0;
      for(int idx=prefix; idx<length; idx++)
        {
         int current=StringGetCharacter(payload,idx);
         if(current=='{')
            depth++;
         else if(current=='}')
           {
            depth--;
            if(depth==0)
              {
               chunkStart=prefix;
               chunkEnd=idx;
               return(true);
              }
           }
        }
      return(false);
     }
   return(false);
  }

void KopitraResetSession(KopitraAgentContext &ctx)
  {
   ctx.session.state=SESSION_STATE_IDLE;
   ctx.session.sessionId="";
   ctx.session.authToken="";
   ctx.session.outboxCursor="";
   ctx.session.lastHeartbeat=0;
   ctx.session.lastPoll=0;
   ctx.session.lastAttempt=0;
   ctx.session.retryAfterHint=0;
   ctx.nextHeartbeatAt=0;
   ctx.nextPollAt=0;
   ctx.nextSnapshotAt=0;
   ctx.initRequestSent=false;
  }

void KopitraContextInit(KopitraAgentContext &ctx,const KopitraConfig &config)
  {
   ctx.config=config;
   if(StringLen(ctx.config.deviceId)==0)
      ctx.config.deviceId=KopitraDefaultDeviceId();
   ctx.initialized=true;
   ctx.consecutiveFailures=0;
   ctx.requestSequence=0;
   MathSrand((uint)GetTickCount());
   KopitraResetSession(ctx);
  }

bool KopitraStartup(KopitraAgentContext &ctx,const KopitraConfig &config)
  {
   if(StringLen(KopitraTrimString(config.apiBaseUrl))==0)
     {
      KopitraLogError("API base URL is required.");
      return(false);
     }
   KopitraContextInit(ctx,config);
   KopitraLogInfo(StringFormat("Context initialized for account %s",config.accountId));
   return(true);
  }

bool KopitraCreateSession(KopitraAgentContext &ctx)
  {
   datetime now=TimeCurrent();
   ctx.session.lastAttempt=now;

   string payload="{";
   payload+="\"account_id\":\""+KopitraJsonEscape(ctx.config.accountId)+"\"";
   payload+="\",\"auth_method\":\""+KopitraJsonEscape(ctx.config.authMethod)+"\"";
   payload+="\",\"auth_key\":\""+KopitraJsonEscape(ctx.config.authKey)+"\"";
   payload+="\",\"device_id\":\""+KopitraJsonEscape(ctx.config.deviceId)+"\"";
   payload+="\",\"platform\":{\"name\":\""+KopitraJsonEscape(TerminalInfoString(TERMINAL_NAME))+"\",\"build\":"+IntegerToString((int)TerminalInfoInteger(TERMINAL_BUILD))+"}";
   payload+="\",\"capabilities\":{\"events\":[\"InitAck\",\"OrderCommand\",\"ShutdownNotice\"],\"allowsOrderSubmission\":"+(ctx.config.enableOrderSubmission ? "true" : "false")+"}";
   payload+="}";

   string response="";
   bool ok=KopitraHttpRequest(ctx,"POST","/trade-agent/v1/sessions",payload,response,ctx.config.httpTimeoutMs,KopitraGenerateIdempotencyKey(ctx,"session"));
   if(!ok)
      return(false);

   string sessionId=KopitraExtractJsonField(response,"session_id");
  if(StringLen(sessionId)==0)
      sessionId=KopitraExtractJsonField(response,"id");
   if(StringLen(sessionId)==0)
     {
      KopitraLogError("Session created but response lacked an identifier.");
      return(false);
     }
   string token=KopitraExtractJsonField(response,"token");
   if(StringLen(token)==0)
      token=KopitraExtractJsonField(response,"session_token");
   string statusText=KopitraExtractJsonField(response,"status");

   KopitraResetSession(ctx);
   ctx.session.sessionId=sessionId;
   ctx.session.authToken=token;
   ctx.session.state=KopitraParseSessionState(statusText);
   ctx.nextHeartbeatAt=now;
   ctx.nextPollAt=now;
   ctx.nextSnapshotAt=now;

   KopitraLogInfo(StringFormat("Session established (id=%s, state=%s)",sessionId,KopitraSessionStateToString(ctx.session.state)));
   return(true);
  }

bool KopitraEnsureSession(KopitraAgentContext &ctx)
  {
   if(StringLen(ctx.session.sessionId)>0)
      return(true);

   datetime now=TimeCurrent();
   if(now-ctx.session.lastAttempt<ctx.config.sessionRetrySeconds && ctx.session.lastAttempt!=0)
      return(false);
   return(KopitraCreateSession(ctx));
  }

bool KopitraDeleteSession(KopitraAgentContext &ctx)
  {
   if(StringLen(ctx.session.sessionId)==0)
      return(true);
   string response="";
   bool ok=KopitraHttpRequest(ctx,"DELETE","/trade-agent/v1/sessions/current","",response,ctx.config.httpTimeoutMs,KopitraGenerateIdempotencyKey(ctx,"session-close"));
   if(ok)
      KopitraLogInfo("Session closed successfully.");
   else
      KopitraLogWarn("Failed to close session cleanly; server will time out the lease.");
   KopitraResetSession(ctx);
   return(ok);
  }

bool KopitraSubmitEvent(KopitraAgentContext &ctx,const string payload)
  {
   if(StringLen(ctx.session.sessionId)==0)
      return(false);
   string response="";
   bool ok=KopitraHttpRequest(ctx,"POST","/trade-agent/v1/sessions/current/inbox",payload,response,ctx.config.httpTimeoutMs,KopitraGenerateIdempotencyKey(ctx,"event"));
   if(!ok)
      KopitraLogWarn(StringFormat("Failed to submit event payload: %s",payload));
   return(ok);
  }

bool KopitraSubmitNamedEvent(KopitraAgentContext &ctx,const string eventType,string dataJson)
  {
   if(StringLen(dataJson)==0)
      dataJson="{}";
   string payload="{";
   payload+="\"eventType\":\""+KopitraJsonEscape(eventType)+"\"";
   payload+="\",\"timestamp\":\""+KopitraFormatIso8601(TimeCurrent())+"\"";
   payload+="\",\"accountId\":\""+KopitraJsonEscape(ctx.config.accountId)+"\"";
   payload+="\",\"sessionId\":\""+KopitraJsonEscape(ctx.session.sessionId)+"\"";
   payload+="\",\"data\":"+dataJson;
   payload+="}";
   return(KopitraSubmitEvent(ctx,payload));
  }

bool KopitraSendInitRequest(KopitraAgentContext &ctx)
  {
   string data="{";
   data+="\"accountLogin\":\""+LongToString(KopitraAccountLogin())+"\"";
   data+="\",\"broker\":\""+KopitraJsonEscape(KopitraAccountCompany())+"\"";
   data+="\",\"currency\":\""+KopitraJsonEscape(KopitraAccountCurrency())+"\"";
   data+="\",\"leverage\":"+IntegerToString(KopitraAccountLeverage());
   data+="\",\"subscribedSymbols\":"+KopitraCollectSubscribedSymbolsJson();
   data+="\",\"supportsOrderSubmission\":"+(ctx.config.enableOrderSubmission ? "true" : "false");
   data+="}";
   bool ok=KopitraSubmitNamedEvent(ctx,"InitRequest",data);
   if(ok)
      ctx.initRequestSent=true;
   return(ok);
  }

bool KopitraSendHeartbeat(KopitraAgentContext &ctx)
  {
   string data="{";
   data+="\"state\":\""+KopitraJsonEscape(KopitraSessionStateToString(ctx.session.state))+"\"";
   data+="\",\"chartSymbol\":\""+KopitraJsonEscape(Symbol())+"\"";
   data+="\",\"equity\":"+KopitraDoubleToJson(KopitraAccountEquity(),2);
   data+="\",\"marginFree\":"+KopitraDoubleToJson(KopitraAccountMarginFree(),2);
   data+="}";
   bool ok=KopitraSubmitNamedEvent(ctx,"StatusHeartbeat",data);
   if(ok)
     {
      ctx.session.lastHeartbeat=TimeCurrent();
      KopitraLogInfo("Heartbeat delivered.");
     }
   return(ok);
  }

bool KopitraSendSyncSnapshot(KopitraAgentContext &ctx)
  {
   string data="{";
   data+="\"balance\":"+KopitraDoubleToJson(KopitraAccountBalance(),2);
   data+="\",\"equity\":"+KopitraDoubleToJson(KopitraAccountEquity(),2);
   data+="\",\"currency\":\""+KopitraJsonEscape(KopitraAccountCurrency())+"\"";
   data+="\",\"marginFree\":"+KopitraDoubleToJson(KopitraAccountMarginFree(),2);
   data+="\",\"openTrades\":"+KopitraCollectOpenTradesJson();
   data+="}";
   bool ok=KopitraSubmitNamedEvent(ctx,"SyncSnapshot",data);
   if(ok)
      KopitraLogInfo("SyncSnapshot transmitted.");
   return(ok);
  }

bool KopitraAckOutboxEvent(KopitraAgentContext &ctx,const string eventId)
  {
   if(StringLen(eventId)==0)
      return(false);
   string path=StringFormat("/trade-agent/v1/sessions/current/outbox/%s/ack",eventId);
   string response="";
   bool ok=KopitraHttpRequest(ctx,"POST",path,"{\"status\":\"received\"}",response,ctx.config.httpTimeoutMs,KopitraGenerateIdempotencyKey(ctx,"ack"));
   if(ok)
     {
      ctx.session.outboxCursor=eventId;
      KopitraLogInfo(StringFormat("Acknowledged event %s",eventId));
     }
   return(ok);
  }

bool KopitraFetchOutbox(KopitraAgentContext &ctx,string &response)
  {
   string path="/trade-agent/v1/sessions/current/outbox";
   if(StringLen(ctx.session.outboxCursor)>0)
      path+="?cursor="+ctx.session.outboxCursor;
   return(KopitraHttpRequest(ctx,"GET",path,"",response,ctx.config.httpTimeoutMs));
  }

void KopitraProcessOutbox(KopitraAgentContext &ctx,const string &payload)
  {
   if(StringLen(payload)==0)
      return;

   string retryAfterValue=KopitraExtractJsonField(payload,"retryAfter");
   if(StringLen(retryAfterValue)>0)
     {
      int retry=StringToInteger(retryAfterValue);
      if(retry>0)
         ctx.session.retryAfterHint=retry;
     }

   int cursor=0;
   int chunkStart=0;
   int chunkEnd=0;
   while(KopitraExtractNextEventChunk(payload,cursor,chunkStart,chunkEnd))
     {
      string chunk=StringSubstr(payload,chunkStart,chunkEnd-chunkStart+1);
      string eventId=KopitraExtractJsonFieldFromChunk(chunk,"id");
      string eventType=KopitraExtractJsonFieldFromChunk(chunk,"type");
      string status=KopitraExtractJsonFieldFromChunk(chunk,"status");
      if(StringLen(eventType)>0)
         KopitraLogInfo(StringFormat("Received event %s (%s)",eventId,eventType));
      if(StringLen(eventType)>0 && (eventType=="SessionStateChanged" || eventType=="session.authenticated" || eventType=="session.pending"))
        {
         if(StringLen(status)==0)
            status=KopitraExtractJsonFieldFromChunk(chunk,"state");
         KopitraSessionState newState=KopitraParseSessionState(status);
         if(newState!=ctx.session.state)
           {
            ctx.session.state=newState;
            KopitraLogInfo(StringFormat("Session state updated to %s",KopitraSessionStateToString(newState)));
           }
        }
      if(StringLen(eventId)>0)
         KopitraAckOutboxEvent(ctx,eventId);
      cursor=chunkEnd+1;
     }
   ctx.session.lastPoll=TimeCurrent();
  }

void KopitraOnTimer(KopitraAgentContext &ctx)
  {
   if(!ctx.initialized)
      return;

   if(!KopitraEnsureSession(ctx))
      return;

   if(StringLen(ctx.session.sessionId)==0)
      return;

   datetime now=TimeCurrent();

   if(!ctx.initRequestSent)
      KopitraSendInitRequest(ctx);

   if(ctx.nextHeartbeatAt==0 || now>=ctx.nextHeartbeatAt)
     {
      if(KopitraSendHeartbeat(ctx))
         ctx.nextHeartbeatAt=now+ctx.config.heartbeatIntervalSeconds;
      else
         ctx.nextHeartbeatAt=now+ctx.config.sessionRetrySeconds;
     }

   if(ctx.nextSnapshotAt==0 || now>=ctx.nextSnapshotAt)
     {
      if(KopitraSendSyncSnapshot(ctx))
         ctx.nextSnapshotAt=now+ctx.config.snapshotIntervalSeconds;
      else
         ctx.nextSnapshotAt=now+ctx.config.sessionRetrySeconds;
     }

   if(ctx.nextPollAt==0 || now>=ctx.nextPollAt)
     {
      string outbox="";
      if(KopitraFetchOutbox(ctx,outbox))
        {
         KopitraProcessOutbox(ctx,outbox);
         int delay=ctx.session.retryAfterHint;
         if(delay<=0)
            delay=ctx.config.pollIntervalSeconds;
         ctx.nextPollAt=now+delay;
        }
      else
        {
         ctx.nextPollAt=now+ctx.config.sessionRetrySeconds;
        }
     }
  }

void KopitraOnTick(KopitraAgentContext &ctx)
  {
   if(!ctx.initialized)
      return;
   KopitraEnsureSession(ctx);
  }

void KopitraOnTrade(KopitraAgentContext &ctx)
  {
   if(!ctx.initialized)
      return;
   KopitraSendSyncSnapshot(ctx);
  }

void KopitraShutdown(KopitraAgentContext &ctx,const int reason)
  {
   KopitraLogInfo(StringFormat("Shutting down (reason=%d)",reason));
   KopitraDeleteSession(ctx);
  }

#endif // __KOPITRA_LIB_MQH__
