#include <MSFS/MSFS.h>
#include <MSFS/MSFS_WindowsTypes.h>
#include <MSFS/MSFS_Render.h>
#include <SimConnect.h>
#include <stdio.h>
#include <MSFS/Legacy/gauges.h>

#include "WasmModule.h"

#include <map>
#include <string>

// #define DEBUG 1
// How often do we check for LVar changes, in sim frame counts?
#if DEBUG
#define CHECK_EVERY_FRAME_COUNTS 120
#else
#define CHECK_EVERY_FRAME_COUNTS 4
#endif

void CALLBACK BBLDispatchProc(SIMCONNECT_RECV* pData, DWORD cbData, void* pContext);

const int MaxDataValuesInPacket = 10;

struct OutgoingData {
    UINT16 valueCount;
    UINT16 ids[MaxDataValuesInPacket];
    FLOAT64 values[MaxDataValuesInPacket];
};

const UINT16 OutgoingDataSize = sizeof(OutgoingData);

OutgoingData outgoingData;

HANDLE hSimConnect = 0;

const int requestAreaSize = 256;
const int responseAreaSize = 256;

const SIMCONNECT_CLIENT_DATA_ID CDA_SIMVAR = 0;
const SIMCONNECT_CLIENT_DATA_ID CDA_REQUEST = 1;
const SIMCONNECT_CLIENT_DATA_ID CDA_RESPONSE = 2;
const char* CDA_NAME_SIMVAR = "BetterBravoLights.LVars";
const char* CDA_NAME_REQUEST = "BetterBravoLights.Request";
const char* CDA_NAME_RESPONSE = "BetterBravoLights.Response";

const SIMCONNECT_DATA_REQUEST_ID SIMCONNECT_REQUEST_ID_REQUEST = 1;

const SIMCONNECT_CLIENT_DATA_DEFINITION_ID DEF_ID_SIMVAR = 0;
const SIMCONNECT_CLIENT_DATA_DEFINITION_ID DEF_ID_REQUEST = 1;
const SIMCONNECT_CLIENT_DATA_DEFINITION_ID DEF_ID_RESPONSE = 2;

// Clears all subscriptions
const char* CMD_CLEAR = "CLEAR";
// Requests that the WASM module checks for any new LVars; if there are any, ALL lvars are sent
const char* CMD_CHECKLVARS = "CHECKLVARS";
// Requests that the WASM module checks for any new LVars; whether there are new lvars, ALL lvars are sent
const char* CMD_LISTLVARS = "LISTLVARS";
// Subscribes to a 0-indexed lvar
const char* CMD_SUBSCRIBE = "SUBSCRIBE";
// Unsubscribes from a 0-indexed lvar
const char* CMD_UNSUBSCRIBE = "UNSUBSCRIBE";

const char* RESPONSE_LVAR_START = "!LVARS-START";
const char* RESPONSE_LVAR_END = "!LVARS-END";

std::map<int, std::string> idToLvar;
std::map<std::string, int> lvarToId;
std::map<int, FLOAT64> subscribedLvars;

void SendResponse(const char* message)
{
#if DEBUG
    fprintf(stderr, "BetterBravoLights: sending response %s", message);
#endif

    SimConnect_SetClientData(
        hSimConnect,
        CDA_RESPONSE,
        DEF_ID_RESPONSE,
        SIMCONNECT_CLIENT_DATA_SET_FLAG_DEFAULT,
        0,
        responseAreaSize,
        (void*)message
    );
}

void RegisterDataAreas()
{
    HRESULT hr = SimConnect_MapClientDataNameToID(hSimConnect, CDA_NAME_SIMVAR, CDA_SIMVAR);
    if (hr != S_OK)
    {
        fprintf(stderr, "BetterBravoLights: cannot create Client Data Area: %lu", hr);
        return;
    }
    SimConnect_CreateClientData(hSimConnect, CDA_SIMVAR, OutgoingDataSize, SIMCONNECT_CREATE_CLIENT_DATA_FLAG_DEFAULT);
#if DEBUG
    fprintf(stderr, "BetterBravoLights: created CDA %s", CDA_NAME_SIMVAR);
#endif

    hr = SimConnect_MapClientDataNameToID(hSimConnect, CDA_NAME_REQUEST, CDA_REQUEST);
    if (hr != S_OK)
    {
        fprintf(stderr, "BetterBravoLights: cannot create Client Data Area: %lu", hr);
        return;
    }
    SimConnect_CreateClientData(hSimConnect, CDA_REQUEST, requestAreaSize, SIMCONNECT_CREATE_CLIENT_DATA_FLAG_DEFAULT);
#if DEBUG
    fprintf(stderr, "BetterBravoLights: created CDA %s", CDA_NAME_REQUEST);
#endif

    hr = SimConnect_MapClientDataNameToID(hSimConnect, CDA_NAME_RESPONSE, CDA_RESPONSE);
    if (hr != S_OK)
    {
        fprintf(stderr, "BetterBravoLights: cannot create Client Data Area: %lu", hr);
        return;
    }
    SimConnect_CreateClientData(hSimConnect, CDA_RESPONSE, responseAreaSize, SIMCONNECT_CREATE_CLIENT_DATA_FLAG_DEFAULT);
#if DEBUG
    fprintf(stderr, "BetterBravoLights: created CDA %s", CDA_NAME_RESPONSE);
#endif

    hr = SimConnect_AddToClientDataDefinition(
        hSimConnect,
        DEF_ID_SIMVAR,
        0,
        OutgoingDataSize
    );

    hr = SimConnect_AddToClientDataDefinition(
        hSimConnect,
        DEF_ID_REQUEST,
        0,
        requestAreaSize
    );

    hr = SimConnect_AddToClientDataDefinition(
        hSimConnect,
        DEF_ID_RESPONSE,
        0,
        responseAreaSize
    );

    SimConnect_RequestClientData(
        hSimConnect,
        CDA_REQUEST,
        SIMCONNECT_REQUEST_ID_REQUEST,
        DEF_ID_REQUEST,
        SIMCONNECT_CLIENT_DATA_PERIOD_ON_SET,
        SIMCONNECT_CLIENT_DATA_REQUEST_FLAG_DEFAULT,
        0,
        0,
        0);
}

void SendPacket(int count)
{
#if DEBUG
    fprintf(stderr, "BetterBravoLights: sending %u data points", count);
#endif

    outgoingData.valueCount = count;

    HRESULT hr = SimConnect_SetClientData(
        hSimConnect,
        CDA_SIMVAR,
        DEF_ID_SIMVAR,
        SIMCONNECT_CLIENT_DATA_SET_FLAG_DEFAULT,
        0,
        OutgoingDataSize,
        &outgoingData
    );

    if (hr != S_OK)
    {
        fprintf(stderr, "BetterBravoLights: failed to send lvars");
    }
}

void CheckSubscribedLVars()
{
#if DEBUG
    fprintf(stderr, "Checking %u LVars", subscribedLvars.size());
#endif

    int i = 0;

    for (auto&& keyValue : subscribedLvars)
    {
        auto msfsId = keyValue.first;
        auto lastValue = keyValue.second;

#if DEBUG
        fprintf(stderr, "BetterBravoLights: retrieving lvar (%u) (old %f)", msfsId, lastValue);
#endif
        auto newValue = get_named_variable_value(msfsId);
#if DEBUG
        fprintf(stderr, "BetterBravoLights: got value %f", newValue);
#endif		
        if (newValue != lastValue)
        {
            subscribedLvars[msfsId] = newValue;
            outgoingData.ids[i] = msfsId;
            outgoingData.values[i] = newValue;
            i++;

            if (i == MaxDataValuesInPacket)
            {
                SendPacket(i);
                i = 0;
            }

#if DEBUG
            fprintf(stderr, "BetterBravoLights: lvar changed (%u) = %f", msfsId, newValue);
#endif
        }
    }

    if (i > 0)
    {
        SendPacket(i);
    }

    if (subscribedLvars.size() > 0)
    {
#if DEBUG
        fprintf(stderr, "BetterBravoLights: completed variable scan");
#endif
    }
}

/// <summary>
/// Checks for new LVars and, if there are some (or forceSend is true), send all of them.
/// </summary>
void CheckLVars(bool forceSend)
{ 
#if DEBUG
    fprintf(stderr, "BetterBravoLights: checking for lvars. current size: %u", lvarToId.size());
 #endif

    auto startIdCheckingAt = lvarToId.size();

    bool send = forceSend;

    int i = startIdCheckingAt;
    while (true) {
#if DEBUG
        if (startIdCheckingAt > 0)
        {
            fprintf(stderr, "BetterBravoLights: checking lvar id %d (started: %d)", i, startIdCheckingAt);
        }
#endif
        
        const char* name = get_name_of_named_variable(i);
        if (name == NULLPTR) {
            break;
        }

        // New lvar
        auto key = std::string(name);
        idToLvar[i] = key;
        lvarToId[key] = i;

        send = TRUE;
#if DEBUG
        auto newValue = get_named_variable_value(i);
        fprintf(stderr, "BetterBravoLights: new lvar: %s (%d) %f", name, i, newValue);
 #endif

        i++;
    }

    if (send)
    {
        auto count = lvarToId.size();

        SendResponse(RESPONSE_LVAR_START);
        for (i = 0; i <= count; i++) {
            auto lvarName = idToLvar[i];
#if DEBUG
            fprintf(stderr, "BetterBravoLights: sending lvar %d", i);
#endif
            SendResponse(lvarName.c_str());
        }
        SendResponse(RESPONSE_LVAR_END);
    }
}


unsigned int frameCount = 0;

void CALLBACK BBLDispatchProc(SIMCONNECT_RECV* pData, DWORD cbData, void* pContext)
{
    switch (pData->dwID)
    {
        case SIMCONNECT_RECV_ID_CLIENT_DATA: {			
            auto recv_data = static_cast<SIMCONNECT_RECV_CLIENT_DATA*>(pData);

            auto str = std::string((char*)(&recv_data->dwData));
                    
            // CLEAR
            if (str.compare(CMD_CLEAR) == 0) {
                subscribedLvars.clear();
                return;
            }

            // LISTLVARS
            if (str.compare(CMD_LISTLVARS) == 0) {
                CheckLVars(true);
                return;
            }

            // CHECKLVARS
            if (str.compare(CMD_CHECKLVARS) == 0) {
                CheckLVars(false);
                return;
            }

            // SUBSCRIBE <lvar-id>
            auto pos = str.find(CMD_SUBSCRIBE);
            if (pos != std::string::npos) {
                int id = atoi(str.c_str() + pos + strlen(CMD_SUBSCRIBE) + 1);
#if DEBUG
                fprintf(stderr, "BetterBravoLights: SUBSCRIBE %d", id);
#endif
                subscribedLvars[id] = std::numeric_limits<float>::infinity();
                return;
            }

            // UNSUBSCRIBE <lvar-id>
            pos = str.find(CMD_UNSUBSCRIBE);
            if (pos != std::string::npos) {
                int id = atoi(str.c_str() + pos + strlen(CMD_UNSUBSCRIBE) + 1);
#if DEBUG
                fprintf(stderr, "BetterBravoLights: UNSUBSCRIBE %d", id);
#endif
                subscribedLvars.erase(id);
                return;
            }

            break;
        }

        case SIMCONNECT_RECV_ID_EVENT_FRAME: {
            frameCount++;
            if (frameCount % CHECK_EVERY_FRAME_COUNTS == 0)
            {
#if DEBUG
                fprintf(stderr, "Frame %u", frameCount);
#endif
                CheckSubscribedLVars();
            }

            break;
        }
    }
}

SIMCONNECT_CLIENT_EVENT_ID EV_FRAME = 1;

extern "C" MSFS_CALLBACK void module_init(void) {
    HRESULT hr = SimConnect_Open(&hSimConnect, "Better Bravo Lights LVar Access", nullptr, 0, 0, 0);
    if (hr != S_OK) {
        fprintf(stderr, "BetterBravoLights: SimConnect Open failed");
        return;
    }
    hr = SimConnect_SubscribeToSystemEvent(hSimConnect, EV_FRAME, "Frame");
    if (hr != S_OK) {
        fprintf(stderr, "BetterBravoLights: Frame subscription failed");
        return;
    }
    hr = SimConnect_CallDispatch(hSimConnect, BBLDispatchProc, NULL);
    if (hr != S_OK) {
        fprintf(stderr, "BetterBravoLights: Dispatch failed");
        return;
    }

    RegisterDataAreas();
}

extern "C" MSFS_CALLBACK void module_deinit(void) {
    SimConnect_Close(hSimConnect);
    fprintf(stderr, "BetterBravoLights: unloaded");
}