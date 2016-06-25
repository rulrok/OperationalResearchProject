#include<Windows.h>
#include<winuser.h>
#include <stdio.h>
#include "MSCorEE.h"
#include <metahost.h>
#include <iostream>

#pragma comment(lib, "mscoree.lib")

//int main(int argc, char **argv) {
//
//
//	/*CAPSEP_SeparateCapCuts();*/
//
//	return 0;
//}

HANDLE hPipe2;
BOOL Finished;
HANDLE hThread = NULL;
HHOOK hook = 0;
HINSTANCE hinst;

unsigned long __stdcall onReceiveMessage(void * pParam) {

	BOOL fSuccess;
	char chBuf[100];
	DWORD dwBytesToWrite = (DWORD)strlen(chBuf);
	DWORD cbRead;

	while (1)
	{
		fSuccess = ReadFile(hPipe2, chBuf, dwBytesToWrite, &cbRead, NULL);

		if (fSuccess)
		{			
			MessageBoxA(NULL, "Read!", "onReceiveMessage()", MB_OK);
		}
		else {
			MessageBoxA(NULL, "Reding pipe failed.", "onReceiveMessage()", MB_OK);
		}

		return 0;
	}

}

void LoadPipe() {

	char buffer[50];
	LPTSTR lpszPipename2 = TEXT("\\\\.\\pipe\\poPipe");

	Finished = FALSE;

	// Keep looking for connection.
	do {

		MessageBoxA(NULL, "Trying to open pipe...", "LoadPipe()", MB_OK);

		hPipe2 = CreateFile(lpszPipename2, GENERIC_READ, 0, NULL, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, NULL);

		if (hPipe2 == NULL || hPipe2 == INVALID_HANDLE_VALUE)
		{
			sprintf_s(buffer, "Could not open the pipe  - (error %d)\n", GetLastError());
			MessageBoxA(NULL, buffer, "Error on LoadPipe()", MB_OK);
		}

	} while (hPipe2 == NULL || hPipe2 == INVALID_HANDLE_VALUE);

	MessageBoxA(NULL, "PIPE CONNECTED", "LoadPipe() success!", MB_OK);

	hThread = CreateThread(NULL, 0, &onReceiveMessage, NULL, 0, NULL);
}

extern "C" __declspec(dllexport) bool WINAPI
DllMain(HINSTANCE hInstDll, // handle to DLL module
	DWORD fdwReason, // reason for calling function
	LPVOID lpvReserved)  // reserved
{
	// Perform actions based on the reason for calling.
	switch (fdwReason)
	{
	case DLL_PROCESS_ATTACH:
	{
		// Initialize once for each new process.
		// Return FALSE to fail DLL load.

		/*char buffer [50];
		sprintf_s(buffer, "hInstDll is %X", hInstDll);
		MessageBoxA(NULL, buffer, "DLL_PROCESS_ATTACH says:", MB_OK);*/
		//hinst = hInstDll;

		//LoadPipe();

		break;
	}

	case DLL_PROCESS_DETACH:
	{
		// Perform any necessary cleanup.

		//MessageBoxA(NULL, "DLL_PROCESS_DETACH", "Dll says:", MB_OK);
		/*
		CloseHandle(hThread);
		CloseHandle(hPipe2);
		Finished = TRUE;*/

		MessageBoxA(NULL, "VRP Dll was detached!", "CVRSP", MB_OK);

		break;
	}

	case DLL_THREAD_ATTACH:
	{
		// Do thread-specific initialization.
		/*MessageBoxA(NULL, "DLL_THREAD_ATTACH", "Dll says:", MB_OK);*/

		break;
	}

	case DLL_THREAD_DETACH:
	{
		// Do thread-specific cleanup.
		/*MessageBoxA(NULL, "DLL_THREAD_DETACH", "Dll says:", MB_OK);*/

		break;
	}
	}

	// Successful DLL_PROCESS_ATTACH.
	return true;
}

extern "C" __declspec(dllexport) bool openPipe()
{
	LoadPipe();
	//MessageBoxA(NULL, "PIPE CONNECTED", "CRVPSEP", MB_OK);

	return hook != NULL;
}