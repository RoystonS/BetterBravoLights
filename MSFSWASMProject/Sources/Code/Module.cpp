#include <MSFS\MSFS.h>
#include <MSFS\MSFS_WindowsTypes.h>
#include <SimConnect.h>

#include "Module.h"

HANDLE g_hSimConnect;

enum eEvents
{
	EVENT_FLIGHT_LOADED
};

void CALLBACK MyDispatchProc(SIMCONNECT_RECV* pData, DWORD cbData, void* pContext);

extern "C" MSFS_CALLBACK void module_init(void)
{

	fprintf(stderr, "BetterBravoLightsLVars X init");
	g_hSimConnect = 0;
	HRESULT hr = SimConnect_Open(&g_hSimConnect, "BetterBravoLightsLVars", NULL, 0, 0, 0);
	if (hr != S_OK)
	{
		fprintf(stderr, "Could not open SimConnect connection.\n");
		return;
	}
	hr = SimConnect_SubscribeToSystemEvent(g_hSimConnect, EVENT_FLIGHT_LOADED, "FlightLoaded");
	if (hr != S_OK)
	{
		fprintf(stderr, "Could not subscripte to \"FlightLoaded\" system event.\n");
		return;
	}
	hr = SimConnect_CallDispatch(g_hSimConnect, MyDispatchProc, NULL);
	if (hr != S_OK)
	{
		fprintf(stderr, "Could not set dispatch proc.\n");
		return;
	}

}

extern "C" MSFS_CALLBACK void module_deinit(void)
{
	fprintf(stderr, "BetterBravoLightsLVars X deinit");

	if (!g_hSimConnect)
		return;
	HRESULT hr = SimConnect_Close(g_hSimConnect);
	if (hr != S_OK)
	{
		fprintf(stderr, "Could not close SimConnect connection.\n");
		return;
	}

}

void CALLBACK MyDispatchProc(SIMCONNECT_RECV* pData, DWORD cbData, void* pContext)
{
	switch (pData->dwID)
	{
	case SIMCONNECT_RECV_ID_EVENT_FILENAME:
	{
		SIMCONNECT_RECV_EVENT_FILENAME* evt = (SIMCONNECT_RECV_EVENT_FILENAME*)pData;
		switch (evt->uEventID)
		{
		case EVENT_FLIGHT_LOADED:

			fprintf(stderr, "New Flight Loaded: %s\n", evt->szFileName);
			break;
		default:
			break;
		}
		break;
	}
	default:
		break;
	}
}
