#include "Hardware.h"
#include "ADC\ADC.h"
#include "OW\onewire.h"
#include "IIC\IIC_ultimate.h"
#include "Relays.h"
#include "WaterSensors.h"
#include "TemperatureSensors.h"
//****************************************************************************************
typedef struct
{
	bool Relays[RELAY_COUNT];
	bool WaterSensors[WATER_SENSOR_COUNT];
	uint8_t PhSensors[PH_SENSOR_COUNT];
	uint8_t OrpSensors[ORP_SENSOR_COUNT];
	float TempSensors[TEMP_SENSOR_COUNT];
} State;
//****************************************************************************************
static volatile State moduleState;
//****************************************************************************************
void InitHardware()
{
	MCUCR &= 0b01111111;			// PUD bit = Off: pull up enabled
	ACSR |= (1 << ACD);				// ACD bit = On: Analog comparator disabled
	
	DDRD |=  (1<<LED_MSG);
	LED_MSG_OFF;
	
	InitADC(true); // use internal Vcc reference
	InitOW();
	Init_i2c();
	Init_Slave_i2c(SlaveOutFunc);   // ����������� ������� ������ ��� �������� ��� Slave
	
    //TIMSK = (1<<OCIE1A)     // Timer1/Counter1 Compare A Interrupt; reassigned in InitDCCOut
          //| (0<<OCIE1B)     // Timer1/Counter1 Compare B Interrupt
          //| (0<<TOIE1)      // Timer1/Counter1 Overflow Interrupt
		  //| (0<<TICIE1)     // Timer1/Counter1 Capture Interrupt
		  //
	      //| (0<<OCIE2)      // Timer2/Counter2 Compare Interrupt
          //| (0<<TOIE2)	  // Timer2/Counter2 Overflow Interrupt
		  //
		  //| (1<<OCIE0)      // Timer0/Counter0 Compare Interrupt
		  //| (0<<TOIE0);     // Timer0/Counter0 Overflow Interrupt

	sei();
}
//****************************************************************************************
int main()
{
	InitHardware();
	
	InitRelays();
	//SetRelays(RELAY_DEFAULT_STATE);
	
    while (true)
    {
		wdt_reset();
		
		// for test:
		//SetRelay(0, GetRelay(0) ? false : true);
		//_delay_ms(200);
		
		
		// populate the module state:
		for (uint8_t i = 0; i < WATER_SENSOR_COUNT; i++)
			moduleState.WaterSensors[i] = IsWaterSensorWet(i);
		
		for (uint8_t i = 0; i < RELAY_COUNT; i++)
			moduleState.Relays[i] = GetRelay(i);
			
		for (uint8_t i = 0; i < TEMP_SENSOR_COUNT; i++)
			moduleState.TempSensors[i] = ReadTemperature(i);
	
    }
}
//****************************************************************************************
