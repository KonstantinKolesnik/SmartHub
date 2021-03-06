/*
----------- Connection guide---------------------------------------------------------------------------
* 13  Radio & Baro SPI SCK
* 12  Radio & Baro SPI MISO(SO)
* 11  Radio & Baro SPI MOSI(SI)
* 10  Radio CS pin
* 9   Radio CE pin
* 8   Baro CS pin
* 7   Baro CE pin

BH1750 (Light) Connection:
VCC-5v
GND-GND
SCL-SCL(A5)
SDA-SDA(A4)
ADD-NC or GND
*/
//--------------------------------------------------------------------------------------------------------------------------------------------
#include <SPI.h>
#include <MySensor.h>
#include <DHT.h>
#include <Wire.h>
#include <Adafruit_BMP085.h>
#include <BH1750.h>
#include <math.h>
//#include <eeprom.h>
//--------------------------------------------------------------------------------------------------------------------------------------------
#define LAST_MS						-100000
//--------------------------------------------------------------------------------------------------------------------------------------------
#define TEMPERATURE_INNER_SENSOR_ID	0
#define HUMIDITY_INNER_SENSOR_ID	1
#define TEMPERATURE_OUTER_SENSOR_ID	2
#define HUMIDITY_OUTER_SENSOR_ID	3
MyMessage msgTemperatureInner(TEMPERATURE_INNER_SENSOR_ID, V_TEMP);
MyMessage msgHumidityInner(HUMIDITY_INNER_SENSOR_ID, V_HUM);
MyMessage msgTemperatureOuter(TEMPERATURE_OUTER_SENSOR_ID, V_TEMP);
MyMessage msgHumidityOuter(HUMIDITY_OUTER_SENSOR_ID, V_HUM);
float lastTemperatureInner = -1000;
float lastHumidityInner = 0;
float lastTemperatureOuter = -1000;
float lastHumidityOuter = 0;
unsigned long prevMsDHT = LAST_MS;
const long intervalDHT = 60000;

//--------------------------------------------------------------------------------------------------------------------------------------------
#define PRESSURE_SENSOR_ID			4
const float ALTITUDE = 200; // sea level, meters
MyMessage msgPressure(PRESSURE_SENSOR_ID, V_PRESSURE);
int32_t lastPressure = 0;
unsigned long prevMsPressure = LAST_MS;
const unsigned long intervalPressure = 60000; // must be 1 minute!!!

//--------------------------------------------------------------------------------------------------------------------------------------------
#define FORECAST_SENSOR_ID			5
MyMessage msgForecast(FORECAST_SENSOR_ID, V_FORECAST);
const int LAST_SAMPLES_COUNT = 5;
float lastPressureSamples[LAST_SAMPLES_COUNT]; // in kPa
//float dP_dt;
int minuteCount = 0;
bool firstRound = true;
float pressureAvg; // average value is used in forecast algorithm
float pressureAvg2; // average after 2 hours is used as reference value for the next iteration
enum FORECAST
{
	STABLE = 0,			// "Stable Weather Pattern"
	SUNNY = 1,			// "Slowly rising Good Weather", "Clear/Sunny "
	CLOUDY = 2,			// "Slowly falling L-Pressure ", "Cloudy/Rain "
	UNSTABLE = 3,		// "Quickly rising H-Press",     "Not Stable"
	THUNDERSTORM = 4,	// "Quickly falling L-Press",    "Thunderstorm"
	UNKNOWN = 5			// "Unknown (More Time needed)
};
int lastForecast = -1;
const char *weather[] = { "Stable", "Sunny", "Cloudy", "Unstable", "Thunderstorm", "Unknown" };

//--------------------------------------------------------------------------------------------------------------------------------------------
// MQ2 sensor: LPG, i-butane, CO, Hydrogen (H2), methane (CH4), alcohol, smoke, propane

#define GAS_SENSOR_ID				6
MyMessage msgGas(GAS_SENSOR_ID, V_VAR1);
int lastGas = -10;
unsigned long prevMsGas = LAST_MS;
const unsigned long intervalGas = 60000;

#define GAS_SENSOR_ANALOG_PIN		A0		// define which analog input channel you are going to use
#define RO_CLEAN_AIR_FACTOR         9.83	// RO_CLEAR_AIR_FACTOR=(Sensor resistance in clean air)/RO, which is derived from the chart in datasheet

#define CALIBARAION_SAMPLE_COUNT    50		// define how many samples you are going to take in the calibration phase
#define CALIBRATION_SAMPLE_INTERVAL 500		// define the time interal (in milisecond) between each samples in the cablibration phase
#define READ_SAMPLE_COUNT           5		// define how many samples you are going to take in normal operation
#define READ_SAMPLE_INTERVAL        50		// define the time interal (in milisecond) between each samples in normal operation

float Ro;

typedef enum
{
	GAS_LPG = 0,
	GAS_CO = 1,
	GAS_SMOKE = 2
} GasType;

//#define GAS_LPG                     0
//#define GAS_CO                      1
//#define GAS_SMOKE                   2

// two points are taken from the curve; with these two points, a line is formed which is "approximately equivalent" to the original curve.
// data format: {x, y, slope}; 
float LPGCurve[3] = { 2.3, 0.21, -0.47 };	// point1: (lg200, 0.21), point2: (lg10000, -0.59)
float COCurve[3] = { 2.3, 0.72, -0.34 };	// point1: (lg200, 0.72), point2: (lg10000,  0.15)
float SmokeCurve[3] = { 2.3, 0.53, -0.44 };	// point1: (lg200, 0.53), point2: (lg10000, -0.22)
//--------------------------------------------------------------------------------------------------------------------------------------------
#define RAIN_SENSOR_ID				7
#define RAIN_SENSOR_ANALOG_PIN		A1
MyMessage msgRain(RAIN_SENSOR_ID, V_RAINRATE); //V_RAIN
uint8_t lastRain = 0;
unsigned long prevMsRain = LAST_MS;
const unsigned long intervalRain = 60000;

//--------------------------------------------------------------------------------------------------------------------------------------------
#define LIGHT_SENSOR_ID				8
MyMessage msgLight(LIGHT_SENSOR_ID, V_LIGHT_LEVEL);
uint16_t lastLight = 0;
unsigned long prevMsLight = LAST_MS;
const long intervalLight = 60000;

//--------------------------------------------------------------------------------------------------------------------------------------------
MyTransportNRF24 transport(RF24_CE_PIN, RF24_CS_PIN, RF24_PA_LEVEL);
MyHwATMega328 hw;
MySensor gw(transport, hw);
bool isMetric = true;

DHT dhtOuter, dhtInner;
#define DHT_INNER_PIN	2
#define DHT_OUTER_PIN	3
Adafruit_BMP085 bmp = Adafruit_BMP085();
BH1750 lightMeter;
//--------------------------------------------------------------------------------------------------------------------------------------------
void setup()
{
	//for (int i = 0; i < 512; i++)
	//	EEPROM.write(i, 255);
	//return;

	gw.begin(onMessageReceived);
	gw.sendSketchInfo("Meteo Station", "1.0");

	isMetric = gw.getConfig().isMetric;

	dhtInner.setup(DHT_INNER_PIN);
	gw.wait(1000);
	dhtOuter.setup(DHT_OUTER_PIN);
	gw.wait(1000);

	if (!bmp.begin())
	{
		Serial.println("Could not find a valid BMP085/BMP180 sensor!");
		while (1) {}
	}

	//lightMeter.begin();

	Ro = getGasSensorRo(); // calibrating the sensor; please make sure the sensor is in clean air when you perform the calibration

	pinMode(RAIN_SENSOR_ANALOG_PIN, INPUT);

	gw.present(TEMPERATURE_INNER_SENSOR_ID, S_TEMP, "Temperature in");
	gw.present(HUMIDITY_INNER_SENSOR_ID, S_HUM, "Humidity in");
	gw.present(TEMPERATURE_OUTER_SENSOR_ID, S_TEMP, "Temperature out");
	gw.present(HUMIDITY_OUTER_SENSOR_ID, S_HUM, "Humidity out");
	gw.present(PRESSURE_SENSOR_ID, S_BARO, "Atmosphere pressure");
	gw.present(FORECAST_SENSOR_ID, S_BARO, "Forecast");
	gw.present(GAS_SENSOR_ID, S_AIR_QUALITY, "Gases level");
	gw.present(RAIN_SENSOR_ID, S_RAIN, "Rain rate");
	gw.present(LIGHT_SENSOR_ID, S_LIGHT_LEVEL, "Light level");
}
void loop()
{
	processDHTs();

	processPressure();
	gw.process();

	processGas();
	gw.process();

	processRain();
	gw.process();

	//processLight();
	//gw.process();
}
//--------------------------------------------------------------------------------------------------------------------------------------------
void onMessageReceived(const MyMessage &message)
{
	uint8_t cmd = mGetCommand(message);

	if (cmd == C_REQ)
	{
		if (message.sensor == TEMPERATURE_OUTER_SENSOR_ID && message.type == V_TEMP)
		{
			float temperature = isMetric ? lastTemperatureOuter : dhtOuter.toFahrenheit(lastTemperatureOuter);
			gw.send(msgTemperatureOuter.set(temperature, 1));
		}
		else if (message.sensor == HUMIDITY_OUTER_SENSOR_ID && message.type == V_HUM)
			gw.send(msgHumidityOuter.set(lastHumidityOuter, 1));
		else if (message.sensor == TEMPERATURE_INNER_SENSOR_ID && message.type == V_TEMP)
		{
			float temperature = isMetric ? lastTemperatureInner : dhtInner.toFahrenheit(lastTemperatureInner);
			gw.send(msgTemperatureInner.set(temperature, 1));
		}
		else if (message.sensor == HUMIDITY_INNER_SENSOR_ID && message.type == V_HUM)
			gw.send(msgHumidityInner.set(lastHumidityInner, 1));
		else if (message.sensor == PRESSURE_SENSOR_ID && message.type == V_PRESSURE)
			gw.send(msgPressure.set(lastPressure));
		else if (message.sensor == FORECAST_SENSOR_ID && message.type == V_FORECAST)
			gw.send(msgForecast.set(lastForecast));
		else if (message.sensor == GAS_SENSOR_ID && message.type == V_VAR1)
			gw.send(msgGas.set(lastGas));
		else if (message.sensor == RAIN_SENSOR_ID && message.type == V_RAIN)
			gw.send(msgRain.set(lastRain));
		else if (message.sensor == LIGHT_SENSOR_ID && message.type == V_LIGHT_LEVEL)
			gw.send(msgLight.set(lastLight));
	}
}
//--------------------------------------------------------------------------------------------------------------------------------------------
void processDHTs()
{
	if (hasIntervalElapsed(&prevMsDHT, intervalDHT))
	{
		processDHT(false);
		gw.process();

		processDHT(true);
		gw.process();
	}
}
void processDHT(bool isOuter)
{
	DHT* pDht = isOuter ? &dhtOuter : &dhtInner;
	MyMessage* msgTemperature = isOuter ? &msgTemperatureOuter : &msgTemperatureInner;
	MyMessage* msgHumidity = isOuter ? &msgHumidityOuter : &msgHumidityInner;
	float* lastTemperature = isOuter ? &lastTemperatureOuter : &lastTemperatureInner;
	float* lastHumidity = isOuter ? &lastHumidityOuter : &lastHumidityInner;

	gw.wait(pDht->getMinimumSamplingPeriod());

	float temperature = roundFloat(pDht->getTemperature(), 1);
	float humidity = roundFloat(pDht->getHumidity(), 1);

	if (!isnan(temperature))
	{
		if (*lastTemperature != temperature)
		{
			*lastTemperature = temperature;

			if (!isMetric)
				temperature = pDht->toFahrenheit(temperature);
			gw.send(msgTemperature->set(temperature, 1));

//#ifdef DEBUG
			Serial.print(isOuter ? "Temperature Outer: " : "Temperature Inner: ");
			Serial.print(temperature, 1);
			Serial.println(isMetric ? " C" : " F");
//#endif
		}
	}
//#ifdef DEBUG
	else
		Serial.println(isOuter ? "Failed reading Temperature Outer" : "Failed reading Temperature Inner");
//#endif


	if (!isnan(humidity))
	{
		if (*lastHumidity != humidity)
		{
			*lastHumidity = humidity;
			gw.send(msgHumidity->set(humidity, 1));

//#ifdef DEBUG
			Serial.print(isOuter ? "Humidity Outer: " : "Humidity Inner: ");
			Serial.print(humidity, 1);
			Serial.println("%");
//#endif
		}
	}
//#ifdef DEBUG
	else
		Serial.println(isOuter ? "Failed reading Humidity Outer" : "Failed reading Humidity Inner");
//#endif
}
void processPressure()
{
	if (hasIntervalElapsed(&prevMsPressure, intervalPressure))
	{
		int32_t pressure = bmp.readPressure(); // in Pa
		//int32_t pressure = bmp.readSealevelPressure(ALTITUDE); // in Pa

		if (!isnan(pressure))
		{
			if (lastPressure != pressure)
			{
				lastPressure = pressure;
				gw.send(msgPressure.set(pressure));

//#ifdef DEBUG
				Serial.print("Pressure: ");
				Serial.print(pressure);
				Serial.println(" Pa");
//#endif
			}

			int forecast = samplePressure((float)pressure / 1000.0f); // in kPa!!!

			if (lastForecast != forecast) // && forecast != UNKNOWN
			{
				lastForecast = forecast;
				gw.send(msgForecast.set(forecast));

//#ifdef DEBUG
				Serial.print("Forecast: ");
				Serial.println(weather[forecast]);
//#endif
			}
		}
//#ifdef DEBUG
		else
			Serial.println("Failed reading Pressure");
//#endif
	}
}
void processGas()
{
	if (hasIntervalElapsed(&prevMsGas, intervalGas))
	{
		float Rs_Ro = getGasSensorRatio();
		//Serial.print("Rs_Ro: ");
		//Serial.println(Rs_Ro);

		int val = getGasPercentage(Rs_Ro, GAS_CO);

		//Serial.print("LPG: ");
		//Serial.print(getGasPercentage(Rs_Ro, GAS_LPG));
		//Serial.println(" ppm");

		//Serial.print("CO: ");
		//Serial.print(getGasPercentage(Rs_Ro, GAS_CO));
		//Serial.println(" ppm");

		//Serial.print("SMOKE: ");
		//Serial.print(getGasPercentage(Rs_Ro, GAS_SMOKE));
		//Serial.println(" ppm");

		//Serial.println();

		if (val != lastGas)
		{
			lastGas = val;
			gw.send(msgGas.set(val));

			Serial.print("Gas: ");
			Serial.print(val);
			Serial.println(" ppm");
		}
	}
}
void processRain()
{
	if (hasIntervalElapsed(&prevMsRain, intervalRain))
	{
		int val = analogRead(RAIN_SENSOR_ANALOG_PIN);

		//Serial.print("Rain: ");
		//Serial.println(val);

		//1010 - dry
		//250 - wet


		////greater than 1000, probably not touching anything
		//if (val >= 1000)
		//{
		//	Serial.println("I think your prong's come loose.");
		//}
		//if (val < 1000 && val >= 650)
		//	//less than 1000, greater than 650, dry soil
		//{
		//	Serial.println("Soil's rather dry, really.");
		//}
		//if (val < 650 && val >= 400)
		//	//less than 650, greater than 400, somewhat moist
		//{
		//	Serial.println("Soil's a bit damp.");
		//}
		//if (val < 400)
		//	//less than 400, quite moist
		//	Serial.println("Soil is quite most, thank you very much.");

		uint8_t range = map(val, 1023, 0, 0, 3);

		//Serial.print("Rain: ");
		//Serial.println(range);

		if (range != lastRain)
		{
			lastRain = range;
			gw.send(msgRain.set(range));
		}
	}
}
void processLight()
{
	if (hasIntervalElapsed(&prevMsLight, intervalLight))
	{
		uint16_t lux = lightMeter.readLightLevel();

		//Serial.print("Light: ");
		//Serial.println(lux);

		if (lux != lastLight)
		{
			lastLight = lux;
			gw.send(msgLight.set(lux));
		}
	}
}
//--------------------------------------------------------------------------------------------------------------------------------------------
float getLastPressureSamplesAverage()
{
	float average = 0;

	for (int i = 0; i < LAST_SAMPLES_COUNT; i++)
		average += lastPressureSamples[i];

	average /= LAST_SAMPLES_COUNT;

	return average;
}
int samplePressure(float pressure /* in kPa */)
{
	// Algorithm found here
	// http://www.freescale.com/files/sensors/doc/app_note/AN3914.pdf

	// Calculate the average of the last n minutes.
	int index = minuteCount % LAST_SAMPLES_COUNT;
	lastPressureSamples[index] = pressure;

	float dP_dt;

	minuteCount++;
	if (minuteCount > 185)
		minuteCount = 6;

	if (minuteCount == 5)
		pressureAvg = getLastPressureSamplesAverage();
	else if (minuteCount == 35)
	{
		float lastPressureAvg = getLastPressureSamplesAverage();
		float change = lastPressureAvg - pressureAvg;
		if (firstRound) // first time initial 3 hour
			dP_dt = change * 2; // note this is for t = 0.5hour
		else
			dP_dt = change / 1.5; // divide by 1.5 as this is the difference in time from 0 value.
	}
	else if (minuteCount == 65)
	{
		float lastPressureAvg = getLastPressureSamplesAverage();
		float change = lastPressureAvg - pressureAvg;
		if (firstRound) //first time initial 3 hour
			dP_dt = change; //note this is for t = 1 hour
		else
			dP_dt = change / 2; //divide by 2 as this is the difference in time from 0 value
	}
	else if (minuteCount == 95)
	{
		float lastPressureAvg = getLastPressureSamplesAverage();
		float change = lastPressureAvg - pressureAvg;
		if (firstRound) // first time initial 3 hour
			dP_dt = change / 1.5; // note this is for t = 1.5 hour
		else
			dP_dt = change / 2.5; // divide by 2.5 as this is the difference in time from 0 value
	}
	else if (minuteCount == 125)
	{
		float lastPressureAvg = getLastPressureSamplesAverage();
		pressureAvg2 = lastPressureAvg; // store for later use.
		float change = lastPressureAvg - pressureAvg;
		if (firstRound) // first time initial 3 hour
			dP_dt = change / 2; // note this is for t = 2 hour
		else
			dP_dt = change / 3; // divide by 3 as this is the difference in time from 0 value
	}
	else if (minuteCount == 155)
	{
		float lastPressureAvg = getLastPressureSamplesAverage();
		float change = lastPressureAvg - pressureAvg;
		if (firstRound) // first time initial 3 hour
			dP_dt = change / 2.5; // note this is for t = 2.5 hour
		else
			dP_dt = change / 3.5; // divide by 3.5 as this is the difference in time from 0 value
	}
	else if (minuteCount == 185)
	{
		float lastPressureAvg = getLastPressureSamplesAverage();
		float change = lastPressureAvg - pressureAvg;
		if (firstRound) // first time initial 3 hour
			dP_dt = change / 3; // note this is for t = 3 hour
		else
			dP_dt = change / 4; // divide by 4 as this is the difference in time from 0 value

		pressureAvg = pressureAvg2; // Equating the pressure at 0 to the pressure at 2 hour after 3 hours have past.
		firstRound = false; // flag to let you know that this is on the past 3 hour mark. Initialized to 0 outside main loop.
	}

	int forecast = UNKNOWN;

	if (minuteCount < 35 && firstRound) //if time is less than 35 min on the first 3 hour interval.
		forecast = UNKNOWN;
	else if (dP_dt < -0.25)
		forecast = THUNDERSTORM;
	else if (dP_dt > 0.25)
		forecast = UNSTABLE;
	else if (dP_dt > -0.25 && dP_dt < -0.05)
		forecast = CLOUDY;
	else if (dP_dt > 0.05 && dP_dt < 0.25)
		forecast = SUNNY;
	else if (dP_dt > -0.05 && dP_dt < 0.05)
		forecast = STABLE;
	else
		forecast = UNKNOWN;

	return forecast;
}
//--------------------------------------------------------------------------------------------------------------------------------------------
float getGasSensorRo()
{
	float adc = 0;
	for (int i = 0; i < CALIBARAION_SAMPLE_COUNT; i++)
	{
		adc += analogRead(GAS_SENSOR_ANALOG_PIN);
		gw.wait(CALIBRATION_SAMPLE_INTERVAL);
	}
	adc /= CALIBARAION_SAMPLE_COUNT;

	float Rs = (1024.0f - adc) / adc; // omit *Rl
	float Ro = Rs / RO_CLEAN_AIR_FACTOR; // The ratio of Rs/Ro is ~10 in a clear air

	return Ro;
}
float getGasSensorRatio()
{
	float adc = 0;
	for (int i = 0; i < READ_SAMPLE_COUNT; i++)
	{
		adc += analogRead(GAS_SENSOR_ANALOG_PIN);
		gw.wait(READ_SAMPLE_INTERVAL);
	}
	adc /= READ_SAMPLE_COUNT;

	float Rs = (1024.0f - adc) / adc; // omit *Rl
	float RsRoRatio = Rs / Ro;

	return RsRoRatio;
}
/*****************************  getGasPercentage **********************************
Output:  ppm of the target gas
Remarks: This function calculates the ppm (parts per million) of the target gas.
		By using the slope and a point of the line, the X (logarithmic value of ppm)
		of the line could be derived if Y (rs_ro_ratio) is provided. As it is a
		logarithmic coordinate, power of 10 is used to convert the result to non-logarithmic value.
************************************************************************************/
float getGasPercentage(float rs_ro_ratio, int gasType)
{
	float *pCurve = NULL;
	switch (gasType)
	{
		case GAS_LPG: pCurve = LPGCurve; break;
		case GAS_CO: pCurve = COCurve; break;
		case GAS_SMOKE: pCurve = SmokeCurve; break;
		default: return 0;
	}

	//100ppm ... 10000ppm, i.e. 0.01% ... 1%

	float x0 = pCurve[0];
	float y0 = pCurve[1];
	float k = pCurve[2];

	float y = log(rs_ro_ratio);
	float x = (y - y0) / k + x0;

	return pow(10, x);
}
//--------------------------------------------------------------------------------------------------------------------------------------------
float roundFloat(float val, uint8_t dec)
{
	double k = pow(10, dec);
	return (float)(round(val * k) / k);
}
bool hasIntervalElapsed(unsigned long* prevMs, const unsigned long interval)
{
	unsigned long ms = millis();

	if ((unsigned long)(ms - *prevMs) >= interval)
	{
		*prevMs = ms;
		return true;
	}

	return false;
}