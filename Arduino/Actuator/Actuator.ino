/* 
   Firmware for Fluxus Fluidics Head Actuator Controller
   
   Arduino UNO R3
   Arduino IDE 1.8.13
   
   Sean Rhinehart
   sean.rhinehart@gmail.com
*/

#include <string.h>

// Output pins
const int pinMtrEnable = 10; // HIGH enables L293D driver chip
const int pinMtrA = 9;      // pinMtrA and pinMtrB pins must be at opposite levels, and pinMtrEnable must be HIGH, for motion to occur.
const int pinMtrB = 8;
const int pinTest = 4;      // A pin used for test purposes, such as indicating controller reset.

#if 1
// Prototype hardware (Arduino Uno and proto-board)
// On the Uno board, the LED pin is shared with SCK (ICSP/debugging clock).
// In the production case, we just move the LED to a new pin to avoid headaches.
const int pinLED = 13;  // toggles for every command prompt
#define PART_NUM_PCBA "Actuator Arduino Prototype"
#else
// Production hardware (Official PCBA)
const int pinLED = 3; // toggles for every command prompt
#define PART_NUM_PCBA "Actuator PCBA xxxx-yyyy"	// PCBA P/N TBD
#endif

// Input pins
const int pinLimUp = 6;   // Upper Limit Switch. HIGH means upper limit reached (NO connected to +5V)
const int pinLimLo = 7;   // Lower Limit Switch. HIGH means lower limit reached (NO connected to +5V)
const int pinMtrPol = 5;  // Motor Polarity from JP1. HIGH means pinMtrA must be HIGH to reached upper limit, LOW (via grounding jumper) reverses. 

// Constants

// Actuator status/position constants (Not enums, since these values are used for multiple constants and variables.)
const int STATUS_FAULTED    = -2; // A limit or timeout fault has been detected.
const int POSITION_UNKNOWN  = -1; // Actuator is between limits, but not faulted.
const int POSITION_LOWER    = 0;  // Actuator is at lower limit.
const int POSITION_UPPER    = 1;  // Actuator is at upper limit.

const int TARGET0 = POSITION_UPPER; // should always be POSITION_UPPER or POSITION_LOWER, and nothing else.
const unsigned long TIMEOUT_MS = 3000;

// Types
typedef enum { stWaitingForCommand, stParamSet, stParamQuery, stActionCommand, stIgnoreToEOL } state_t;

typedef enum { asAtLimitLO, asMovingLO, asMovingHI, asAtLimitHI, asFaultTimeOut, asFaultSwitches} act_state_t;

typedef enum { itUnknown, itCommand, itQuery, itParamSet } idterm_t;  // identifier terminator character meanings

typedef enum {
  eNone,
  eCmdBufOverflow,
  eBadCRC,
  
  eMissingCRC,
  eMissingIdTerminator, // '!','=', or '?'
  eBadParamId,
  eBadCmdId,
  
  eBadValue,
  eReadOnlyParam,
  eNotInteractive,
  eFaultTimeOut,
  eFaultSwitches, // both limit switches high
  eFaultPolarity, // hit limit in wrong direction
  eFaulted,   // generic fault condition
} error_t;

// Command and parameter id strings are lower case for matching efficiency.
// They must also remain synchronized with their corresponding enums.

typedef enum { ptVersion, ptPartNum, ptError, ptTarg0, ptPos, ptStatReg, ptTarg, ptCOUNT } param_t;
const char* Params[ptCOUNT] =
{
  "ver",      // Read-only
  "pn",       // Read-only
  "error",    // Read-only (last error)
  "targ0",    // starting target, read only
  "pos",      // current position, read only (-2=Faulted, -1=Unknown, 0=Lower limit, 1=Upper Limit)
  "status",   // status, read only
  "targ"      // target position (0=lower limit, 1=upper limit), read/write
};

typedef enum { cmEnumAll, cmEnumCmds, cmEnumParams, cmHelp, cmJogUp, cmJogDn, cmReset, cmToggle, cmCOUNT } cmd_t;
const char* Commands[cmCOUNT] =
{
  "enum",
  "enumc",
  "enump",
  "help",
  "jogup",
  "jogdn",
  "reset",
  "toggle"
};

typedef struct StatBits {
  unsigned char Interactive: 1; // When high, this bit relaxes CRC requirement, and enables serial echo
  unsigned char Reserved:    3;
  unsigned char Flt_LimSw:   1; // invalid readings (both HIGH) from limit switches detected
  unsigned char Flt_MtrPol:  1; // Polarity fault: hit wrong limit 
  unsigned char Flt_TimeOut: 1; // unable to reach target position within time limit
  unsigned char Faulted    : 1; // driver has been disabled due to a detected problem.
} StatBits_t;

typedef union Stat_Reg {
  StatBits_t Bits;
  byte Value;
} Stat_Reg_t;

// Variables
int TargetPos = POSITION_UNKNOWN; //TARGET0;
int CurPos = POSITION_UNKNOWN;  // current position will be updated by polling limit switches
int UpDirA = HIGH; // Level required on pinMtrA to move toward upper limit.
int InitDirA = HIGH;  // Initial value for pinMtrA level. Will change according to direction and polarity setting.
error_t LastError = eNone;

unsigned long tickTargetSet;  // millis() value when target parameter was last set.

// A 16-bit CRC is used to validate all commands, unless in interactive mode.
#define CRC_TABLE_SIZE 256
//#define CRC_POLY 0x1021     // Truncated polynomial for CRC-16 CCITT
#define CRC_INIT 0x1D0F     // corresponds to empty message CRC
uint16_t table[CRC_TABLE_SIZE];

#define ESC 0x1b
#define VERSION "Fluxus Fluidics Head Controller Firmware Version 1.0"	// used for connection verification
#define PROMPT "Cmd> "

// Indent for "purpose" column, used when enumerating and explaining commands and parameters
#define INDENT 8

Stat_Reg_t StatReg;
state_t state = stWaitingForCommand;
int cmdflag = 0;

#define BUFFER_CAP 31
char cmdBuffer[BUFFER_CAP+1]; // add extra byte for terminator
char repBuffer[BUFFER_CAP+1];
int iBufBytes = 0;

// The following macros move string constants from RAM to FLASH, to preserve our modest RAM space
#define fmem_print(cstr) Serial.print(F(cstr))
#define fmem_println(cstr) Serial.println(F(cstr))

void sendPrompt()
{
  state = stWaitingForCommand;
  iBufBytes = 0;
  cmdBuffer[0] = 0;
  fmem_print(PROMPT);  
  cmdflag = !cmdflag;
  digitalWrite(pinLED, cmdflag == 0 ? LOW : HIGH); // Toggles yellow LED at every command prompt, to show communication
}

void explainCommand(cmd_t cmd)
{
  // This kludge (an on-demand lookup table) is a workaround for limited RAM space,
  // and the need to store string constants in PROGMEM. 
  switch(cmd)
  {
    case cmEnumAll:
      fmem_println("Enumerates commands and parameters.");
      break;
    case cmEnumCmds:
      fmem_println("Enumerates commands.");
      break;
    case cmEnumParams:
      fmem_println("Enumerates parameters.");
      break;
    case cmHelp:
      fmem_println("Displays high-level help, and sets Interactive mode.");
      break;
    case cmJogUp:
      fmem_println("Takes small step toward upper limit. Only works in interactive mode.");
      break;
    case cmJogDn:
      fmem_println("Takes small step toward lower limit. Only works in interactive mode.");
      break;
    case cmReset:
      fmem_println("Resets this controller. This allows recovery from a Faulted condition.");
      break;
    case cmToggle:
      fmem_println("Toggles the current target position, if driver enabled.");
      break;
    default: Serial.println(); break;
  }
  
}

void enumCmds()
{
  int index = 0;
  int len;
  char* s;
  while (index < (int) cmCOUNT)
  {
    s = (char*)Commands[index];
    len = strlen(s);
    Serial.print(s);
    while (len++ < INDENT)
      Serial.print(' ');
    explainCommand((cmd_t)index);
    index++;
  }
}

cmd_t recognizeCommand(char* s)
{
  cmd_t match = cmCOUNT;  // invalid value
  int index = 0;
  while(index < (int) cmCOUNT)
  {
    if (strcmp(s, Commands[index]) == 0)
    {
      match = (cmd_t) index;
      break;
    }
    index++;
  }
  return match;
}

void explainParam(param_t p)
{
  // This kludge is a workaround for the limited RAM space,
  // and the need to store string constants in PROGMEM. 
  switch(p)
  {
    case ptVersion:
      fmem_println("Read-only firmware version string.");
      break;
    case ptPartNum:
      fmem_println("Read-only PCBA part number for CRISP controller.");
      break;
    case ptError:
      fmem_println("Read-only copy of last error reported.");
      break;
    case ptTarg0:
      fmem_println("Read-only initial actuator target position.");
      break;
    case ptPos:
      fmem_println("Read-only current position. -2=faulted, -1=unknown, 0=lower limit, 1=upper limit.");
      break;
    case ptStatReg:
      fmem_println("Read-only Status register.");
      break;
    case ptTarg:
      fmem_println("Actuator target position.");
      break;

    default: Serial.println(); break;
  }
}

void enumParams()
{
  int index = 0;
  int len;
  char* s;
  while (index < (int) ptCOUNT)
  {
    s = (char*)Params[index];
    len = strlen(s);
    Serial.print(s);
    while (len++ < INDENT)
      Serial.print(' ');
    explainParam((param_t)index);
    index++;
  }
}

param_t recognizeParam(char* s)
{
  param_t match = ptCOUNT;  // invalid value
  int index = 0;
  while(index < (int) ptCOUNT)
  {
    if (strcmp(s, Params[index]) == 0)
    {
      match = (param_t) index;
      break;
    }
    index++;
  }
  return match;
}

idterm_t identifierKind(char c)
{
  switch (c)
  {
    case '!':
      return itCommand;
    case '?':
      return itQuery;
    case '=':
      return itParamSet;
    default:
      return itUnknown;
  }
}

error_t showHelp()
{
  fmem_println("There are 3 kinds of command line operations supported:");
  fmem_println("1. Action commands, such as help!, enum!, or toggle!");
  fmem_println("2. Parameter queries, such as pos?, targ?, or ver?");
  fmem_println("3. Parameter assignments, such as targ=1, or targ=0");
  fmem_println("Use the enum! command to enumerate commands and parameters.");
  fmem_println("Since this session is interactive, the CRC requirement has been relaxed.");
  StatReg.Bits.Interactive = 1; // relax CRC requirements
  return eNone;
}

void reportErrorContext(error_t err)
{
  switch(err)
  {
    case eNone:
      fmem_println("No errors reported.");
      break;
    case eCmdBufOverflow:
      fmem_println("Command line buffer overflow.");
      break;
    
    case eBadCRC:
      fmem_println("CRC value mismatch.");
      break;
    case eMissingCRC:
      fmem_println("CRC required, but not provided.");
      break;
    case eMissingIdTerminator: // '!','=', or '?'
      fmem_println("Missing command or parameter name terminator.");
      fmem_println("Commands must end with '!'");
      fmem_println("Parameter queries must end with '?'");
      fmem_println("Parameter value assignments must have '=' between name and value");
      break;
    case eBadParamId:
      fmem_println("Bad parameter name. Recognized params are:");
      enumParams();
      break;
    case eBadCmdId:
      fmem_println("Unrecognized command. Recognized commands are:");
      enumCmds();
      break;
    case eBadValue:
      fmem_println("Bad parameter value.");
      break;
    case eReadOnlyParam:
      fmem_println("Read-only parameter.");
      break;
      
    case eNotInteractive:
      fmem_println("Jogging commands can only be run when in interactive mode, via !help."); // Not true anymore
      break;
      
    case eFaultSwitches:
      fmem_println("Motor driver has been disabled, because upper and lower limit switches went HIGH at the same time.");
      break;
    case eFaultTimeOut:
      fmem_println("Motor driver has been disabled, because target position was not reached in time. Check for issues with motor power, motor drive polarity, or binding.");
      break;
    case eFaultPolarity:
      fmem_println("Motor driver has been disabled, possibly due to motor drive polarity issue.");
      break;
    case eFaulted:
      fmem_println("Motor driver has been disabled, due to a fault condition.");
      break;
    
    default:
      fmem_println("Unspecified error condition.");
      break;
  }
}

void reportError(error_t err, char* OkMsg)
{
  if (err == eNone)
  {
    if (OkMsg != NULL)
      Serial.println(OkMsg);
    return;
  }
  LastError = err;
  fmem_print("?Error ");
  Serial.print(err, DEC);
  fmem_print(" detected: ");
  reportErrorContext(err);

}


void reportDataHexFloat(uint16_t hex, float val);
void reportDataHex(uint16_t hex);
void(* resetFunc) (void) = 0;

error_t disableDriver(error_t err)
{
  TargetPos = POSITION_UNKNOWN; // Disable timeout checking
  digitalWrite(pinMtrEnable, LOW);
  
  if (err != eNone)
  {
    // Standard fault handling
    LastError = err;
    StatReg.Bits.Faulted = 1;
    CurPos = STATUS_FAULTED;
    
    // Update error-specific flag bits
    switch(err)
    {
      case eFaultSwitches:
        StatReg.Bits.Flt_LimSw = 1;
        break;
      case eFaultPolarity:
        StatReg.Bits.Flt_MtrPol = 1;
        break;
      case eFaultTimeOut:
        StatReg.Bits.Flt_TimeOut = 1;
        break;
    }
  }

  //fmem_print("@"); // @ character informs software that this is an asynchronous message, and not part of a command response
  //reportErrorContext(err, "Driver disabled after completing motion.");
  return err;
}

error_t checkLimitSwitches()
{
  // This routine assumes switches have already been debounced in hardware.
  static int swUpper;
  static int swLower;
  static unsigned long tickNow;
  
  if (CurPos == STATUS_FAULTED)
    return eFaulted;  // Faulted status is permanent until reset.
  
  swUpper = digitalRead(pinLimUp);
  swLower = digitalRead(pinLimLo);

  if (swUpper == HIGH)
  {
    // We closed the upper limit switch
    if (swLower == HIGH)
      return disableDriver(eFaultSwitches); // error if both limit switches HIGH at same time.
    if (CurPos == POSITION_UNKNOWN)
    {
      // We just reached the upper position now, because CurPos has not been updated
      // TargetPos can be POSITION_UNKNOWN if jogging
      if (TargetPos == POSITION_LOWER)
        return disableDriver(eFaultPolarity); // hit wrong limit switch
      else if (TargetPos == POSITION_UPPER)
        disableDriver(eNone); // reached destination, turn off driver to save power
    }
    CurPos = POSITION_UPPER;
  }
  else if (swLower == HIGH)
  {
    // We closed the lower limit switch
    if (CurPos == POSITION_UNKNOWN)
    {
      // We just reached the lower position now, because CurPos has not been updated
      // TargetPos can be POSITION_UNKNOWN if jogging
      if (TargetPos == POSITION_UPPER)
        return disableDriver(eFaultPolarity); // hit wrong limit switch
      else if (TargetPos == POSITION_LOWER)
        disableDriver(eNone); // reached destination, turn off driver to save power
    }
    CurPos = POSITION_LOWER;
  }
  else
  {
    // Neither switch is currently closed, so we don't know where we are.
    CurPos = POSITION_UNKNOWN;
    
    if (TargetPos > POSITION_UNKNOWN)
    {
      // If specific target (not jogging), check for timeout
      tickNow = millis();
      if (tickNow - tickTargetSet > TIMEOUT_MS)
        return disableDriver(eFaultTimeOut);
    }
  }
  return eNone;
}


error_t setTarget(int newTarget)
{
  if (newTarget != POSITION_LOWER && newTarget != POSITION_UPPER)
    return eBadValue;

  tickTargetSet = millis();
  error_t err = checkLimitSwitches();
    
  if (err == eNone)
  {
    digitalWrite(pinMtrEnable, LOW); // disable driver while we are setting up the mtrA and mtrB pin states
    // good practice, and creates rising edge trigger condition for debugging
    fmem_print("New target position: ");
    Serial.println(newTarget, DEC);
    TargetPos = newTarget;
    
    if (TargetPos == POSITION_UPPER)
    {
      InitDirA = UpDirA;
    }
    else  
    {
      // newTarget must be POSITION_LOWER, rather than POSITION_UPPER, which means that UpDirA is exactly the wrong level for pinMtrA.
      InitDirA = !UpDirA;
    }

    digitalWrite(pinMtrA, InitDirA);
    digitalWrite(pinMtrB, !InitDirA); // pinMtrB must be opposite of pinMtrA for motion 
    digitalWrite(pinMtrEnable, HIGH);
  }
  return err;
}

error_t jog(int dir)
{
    digitalWrite(pinMtrEnable, LOW); // disable driver while we are setting up the mtrA and mtrB pin states

    //if (!StatReg.Bits.Interactive)
    //  return eNotInteractive;
      
    // good practice, and creates rising edge trigger condition for debugging
    fmem_print("Jogging: ");
    Serial.println(dir, DEC);
    TargetPos = POSITION_UNKNOWN;
    
    if (dir > 0)
    {
      InitDirA = UpDirA; // moving towards upper limit position
    }
    else  
    {
      InitDirA = !UpDirA; // moving towards lower limit position
    }
    digitalWrite(pinMtrA, InitDirA); 
    digitalWrite(pinMtrB, !InitDirA);
    digitalWrite(pinMtrEnable, HIGH);
    delay(200);
    return disableDriver(eNone);  
}

error_t processCmd()
{
  state = stActionCommand;
  error_t err = eNone;
  cmd_t cmd = recognizeCommand(cmdBuffer);

  switch(cmd)
  {
    case cmEnumAll:
      fmem_println("Commands:");
      enumCmds();
      fmem_println("Parameters:");
      enumParams();
      break;

    case cmEnumCmds:
      enumCmds();
      break;
    
    case cmEnumParams:
      enumParams();
      break;
      
    case cmHelp:
      showHelp();
      break;

    case cmJogUp:
      err = jog(1);
      break;
      
    case cmJogDn:
      err = jog(-1);
      break;
      
    case cmReset:
      fmem_println("Resetting controller.");
      delay(30);  // allow serial data to be sent
      resetFunc();
      break;
      
    case cmToggle:
      int NewTarget = (TargetPos == POSITION_UNKNOWN) ? TARGET0 : !TargetPos;
      err = setTarget(NewTarget);
      break;

    default:
      err = eBadCmdId;
      break;
  }
  return err;
}


error_t processQuery()
{
  state = stParamQuery;
  error_t err = eNone;
  param_t param = recognizeParam(cmdBuffer);

  switch(param)
  {
    case ptVersion:
      fmem_println(VERSION);
      break;
    case ptPartNum:
      fmem_println(PART_NUM_PCBA);
      break;
    case ptError:
      Serial.print(LastError, DEC);
      Serial.print(": ");
      reportErrorContext(LastError);
      break;
    case ptTarg0:
      err = checkLimitSwitches();
      Serial.println(TARGET0, DEC);
      break;
    case ptPos:
      err = checkLimitSwitches();
      Serial.println(CurPos, DEC);
      break;
    
    case ptStatReg:
      reportDataHex(StatReg.Value);
      break;

    case ptTarg:
      Serial.println(TargetPos, DEC);
      break;

    default: // ptCOUNT
      err = eBadParamId;
      break;    
  }
  return err;
}

error_t parseInt(char* decval, int* pDest, int minVal, int maxVal)
{
  error_t err = eBadValue;
  char* pEnd;
  long v = strtol(decval, &pEnd, 10);
  if (*pEnd == 0 && v >= minVal && v <= maxVal)
  {
    err = eNone;
    *pDest = (int) v;
  }
  return err;
}
error_t parseHexU8(char* hexval, uint8_t* pDest)
{
  error_t err = eBadValue;
  char* pEnd;
  long v = strtol(hexval, &pEnd, 16);
  if (*pEnd == 0 && v <= 255)
  {
    err = eNone;
    *pDest = (uint8_t) v;
  }
  return err;
}

error_t parseHexU16(char* hexval, uint16_t* pDest)
{
  error_t err = eBadValue;
  char* pEnd;
  long v = strtol(hexval, &pEnd, 16);
  if (*pEnd == 0 && v <= 65535)
  {
    err = eNone;
    *pDest = (uint16_t) v;
  }
  return err;
}


error_t processParamSet(char* strVal)
{
  state = stParamSet;
  error_t err = eNone;
  param_t param = recognizeParam(cmdBuffer);
  //uint8_t val8;
  int val;
  
  switch(param)
  {
    case ptVersion:
    case ptPartNum:
    case ptTarg0:
    case ptStatReg:
    case ptError:
      err = eReadOnlyParam;
      break;
      
    case ptPos:
      err = checkLimitSwitches();
      if (err == eNone)
        err = parseInt(strVal, &val, 0, 1);
      if (err == eNone)
        err = setTarget(val);
      break;

    case ptTarg:
      err = checkLimitSwitches();
      if (err == eNone)
        err = parseInt(strVal, &val, -1, 1);
      if (err == eNone)
      {
        if (val == -1)
          TargetPos = POSITION_UNKNOWN;
        else
          err = setTarget(val);
      }
      break;
    
        
    default: // ptCOUNT
      err = eBadParamId;
      break;    
  }
  return err;
}

uint16_t calcCRC(byte* szData);

void reportDataCRC()
{
  uint16_t CRC = calcCRC((byte*)repBuffer);
  Serial.print(repBuffer);
  Serial.print('$');
  Serial.println(CRC, HEX);
}

void reportDataHexFloat(uint16_t hex, float val)
{
  // Report data in hex and float format, with CRC
  // Arduino snprintf() does not support floats by default, so we use dtostrf()
  char floatBuffer[16];
  dtostrf(val,4,2,floatBuffer);
  snprintf(repBuffer, BUFFER_CAP, "%x:%s", hex, floatBuffer);
  reportDataCRC();
}

void reportDataHex(uint16_t hex)
{
  // Report data in hex and float format, with CRC
  snprintf(repBuffer, BUFFER_CAP, "%x", hex);
  reportDataCRC();
}

void handleError(error_t err, char* OkMsg)
{
  // Called processInputLine() and processByte()
  reportError(err, OkMsg);
  sendPrompt();
}

void processInputLine()
{
  error_t err = eNone;
  char* OkMsg = NULL; // only parameter set operations need an "Ok" acknowledgement
  char* s = strchr(cmdBuffer, '$'); // look for CRC prefix char
  uint16_t CRCprovided = 0;
  uint16_t CRCdetermined = 0;

  if (*cmdBuffer == 0)
  {
    // exit if empty command line
    sendPrompt();
    return;
  }
  
#if 0 //DISABLE_CRC
  if (s != NULL) *s = 0;  // ignore CRC for now
  iBufBytes = strlen(cmdBuffer);
  
#else  
  if (s == NULL || s[1] == 0)
  {
    err = eMissingCRC; // CRC is missing
    // However, we will excuse a missing CRC if in Interactive mode, or about to be.
    if (StatReg.Bits.Interactive || strcasecmp(cmdBuffer, "help!")==0)
      err = eNone;
  }
  else
  {
    // verify CRC whenever provided
    *s++ = 0;  // terminate command line string at CRC marker ('$'), and advance to first hex digit
    err = parseHexU16(s, &CRCprovided);
    if (err == eNone)
    {
      CRCdetermined = calcCRC((byte*)cmdBuffer);
      if (CRCdetermined !=  CRCprovided)
        err = eBadCRC;
    }
    else
      err = eBadCRC;
  }
#endif

if (err == eNone)
  {
    err = eMissingIdTerminator; // assume the worst
    s = strpbrk(cmdBuffer, "!?=");
    if (StatReg.Bits.Interactive)
      Serial.println(); // echo back command's CR
    if (s != NULL)
    {
      char c = *s;
      idterm_t idKind = identifierKind(c);
      *s++ = 0; //terminate identifier, and advance to value, if present
      char* t = cmdBuffer;
      
      // Now that CRC has been verified, convert identifier to lower case for quicker comparisons
      while (*t != 0)
      {
        *t = tolower(*t);
        t++;
      }
  
      switch(idKind)
      {
        case itCommand:
          err = processCmd();
          break;
        case itQuery:
          err = processQuery();
          break;
        case itParamSet:
          err = processParamSet(s);
          OkMsg = (char*)"Ok";
          break; 
      }
    }
  }
  handleError(err, OkMsg);
}

void processByte(char byteIn)
{
  if (byteIn == ESC)
  {
    Serial.flush();
    delay(10);
    fmem_println(" ESC");
    sendPrompt();
  }
  else
  switch(state)
  {
    case stWaitingForCommand:
      
      if (byteIn == '\n')
        return; // linefeed from previous command- ignore it
        
      if (byteIn == '\r')
        processInputLine();
      else
      {
        if (StatReg.Bits.Interactive)
          Serial.write(byteIn); // echoes all except for ESC, /r, /n
        if (iBufBytes < BUFFER_CAP)
        {
          cmdBuffer[iBufBytes++] = byteIn;
          cmdBuffer[iBufBytes] = 0; // terminate
        }
        else
        {
          state = stIgnoreToEOL;  // buffer overflow
          Serial.write('\a'); // let user know character was ignored
        }
      }
      break;
     
    case stIgnoreToEOL:
    //default:
      if (byteIn >= ' ' && StatReg.Bits.Interactive)
        Serial.write(byteIn); // echo non-control characters
      
      if (byteIn == '\r')
      {
        if (StatReg.Bits.Interactive)
          Serial.println(); // echo back command's CR
        handleError(eCmdBufOverflow, NULL); // transitions back to stWaitingForCommand
      }
      else
        Serial.write('\a'); // let user know character was ignored

      break;
  }
}

void buildCRCtable()
{
    const uint16_t poly = 0x1021; // CRC-16 CCITT truncated polynomial (actual value is 0x11021, but highest bit is understood)
    uint16_t temp, a;
    for (int i = 0; i < CRC_TABLE_SIZE; ++i)
    {
        temp = 0;
        a = (uint16_t)(i << 8);
        for (int j = 0; j < 8; ++j)
        {
            if (((temp ^ a) & 0x8000) != 0)
                temp = (uint16_t)((temp << 1) ^ poly);
            else
                temp <<= 1;
            a <<= 1;
        }
        table[i] = temp;
    }
}

uint16_t calcCRC(byte* szData)
{
    uint16_t crc = CRC_INIT;
    // CRC_INIT is 0x1D0F, instead of 0xFFFF, to compensate for the fact that we are not adding 16 zero bits to the end of the data.
    // 0x1D0F would be the CRC for an empty message, after adding the appropriate padding of 16 zero bits.
    while(*szData != 0)
    {
        crc = (uint16_t)((crc << 8) ^ table[((crc >> 8) ^ (*szData++))]);
    }
    return crc;
}

void setup()
{
  //Wire.begin();
  Serial.begin(115200);
  
  // Setup output pins
  pinMode(pinTest, OUTPUT);
  pinMode(pinLED, OUTPUT);
  pinMode(pinMtrEnable, OUTPUT);
  pinMode(pinMtrA, OUTPUT);
  pinMode(pinMtrB, OUTPUT);

  digitalWrite(pinTest, LOW);
  digitalWrite(pinLED, LOW);
  digitalWrite(pinMtrEnable, LOW);
  
  // Setup input pins
  pinMode(pinLimUp, INPUT);
  pinMode(pinLimLo, INPUT);
  pinMode(pinMtrPol, INPUT_PULLUP); // Pull-up sets default state to HIGH. Grounded (jumpered) state is LOW.
 
  // Generate ~10us negative pulse on pinTest to signify reset
  delayMicroseconds(8);
  digitalWrite(pinTest, HIGH);
  
  fmem_println(VERSION);

  StatReg.Value = 0;
  buildCRCtable();

  // Set motor polarity: How must pinMtrA be set to reach the upper limit? pinMtrB will be opposite.
  fmem_print("JP1 motor polarity jumper ");
  if (digitalRead(pinMtrPol) == LOW)
  {
    fmem_println("installed. Polarity reversed.");
    UpDirA = LOW;
  }
  else
  {
    fmem_println("absent. Using default polarity.");
    UpDirA = HIGH;
  }

 
  fmem_println("FW initialization complete.");
  Serial.println();
  
  // automatic moving to specified target position has been disabled, because limit switches are not reliably stopping my homebuilt actuator simulator.
  //error_t err = setTarget(TARGET0); // attempt to move actuator to initial position
  //reportError(err, NULL); // report any error to console, but do not issue prompt yet.

  
  fmem_println("At the ""Cmd>"" prompt, type ""help!"" and then <Enter> for interactive mode.");
  fmem_println("Interactive mode will relax the command line CRC requirements, and enable serial echo.");
  Serial.println();
  sendPrompt();
}

void loop()
{
  static char byteIn;
  
  checkLimitSwitches(); // checks for switch issues and timeouts
  
  if (Serial.available()>0)
  {
    byteIn = (char)Serial.read();
    processByte(byteIn);  
  }
  
  delayMicroseconds(10);
}
