#ifndef _HARDWARE_h
#define _HARDWARE_h
//****************************************************************************************
#if defined(ARDUINO) && ARDUINO >= 100
	#include "Arduino.h"
#else
	#include "WProgram.h"
#endif

#define EEPROM_OFFSET			10 // 6 bytes reserved for 1-Wire slave address
//****************************************************************************************
typedef enum
{
	Relay,
	PWM,
	Temperature,
	Liquid,
	Ph,
    ORP,
    Conductivity, // + Salinity


} ControlLineType_t;
typedef enum
{
	Invalid,	// family=0 hangs the 1-wire bus
	Test,		// test module
	D5,			// AE-D5
	D6,			// AE-D6
	D8,			// AE-D8


} ModuleType_t;
//****************************************************************************************
// current module type!!!:
#define MODULE_TYPE				Test
//****************************************************************************************
// commands (from BusHub)
//****************************************************************************************
#define CMD_GET_TYPE							0
#define CMD_GET_CONTROL_LINE_COUNT				1
#define CMD_GET_CONTROL_LINE_STATE				2
#define CMD_SET_CONTROL_LINE_STATE				3
//****************************************************************************************
#endif