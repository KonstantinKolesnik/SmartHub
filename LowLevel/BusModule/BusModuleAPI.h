#ifndef BUSMODULECOMMANDS_H_
#define BUSMODULECOMMANDS_H_
//****************************************************************************************
#define MAX_CONTROL_LINE_TYPES					255
#define MAX_1_WIRE_DEVICES						10
//****************************************************************************************
// commands (from BusMaster)
//****************************************************************************************
#define CMD_GET_TYPE							0
#define CMD_GET_CONTROL_LINE_COUNT				1
#define CMD_GET_CONTROL_LINE_STATE				2
#define CMD_SET_CONTROL_LINE_STATE				3
//****************************************************************************************
// control line types
//****************************************************************************************
#define CONTROL_LINE_TYPE_RELAY					0
#define CONTROL_LINE_TYPE_WATER_SENSOR			1
#define CONTROL_LINE_TYPE_PH_SENSOR				2
#define CONTROL_LINE_TYPE_ORP_SENSOR			3
#define CONTROL_LINE_TYPE_TEMPERATURE_SENSOR	4
#define CONTROL_LINE_TYPE_CONDUCTIVITY_SENSOR	5
#define CONTROL_LINE_TYPE_DIMMER				6
//****************************************************************************************
#endif