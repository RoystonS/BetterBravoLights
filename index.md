**NOTE: THIS TOOL IS CURRENTLY IN DEVELOPMENT AND IN ALPHA STATUS!**

**If you're happy helping to test and report issues on incomplete and buggy software, give it a go. If you're not, and expect something perfect and polished, you'll want to wait until it's a little more stable and complete.**

**Please don’t download it then discover it’s not complete enough for you and give it a bad rating!**

# What is it?

This tool aims to be a 'better' replacement for the standard Honeycomb Bravo Throttle lights tool (the "AFC bridge").

The standard AFC Bridge _works_ but has some issues:

- editing the configuration file is not easy. It's all at the level of bytes and bits rather than actual light names, and specifying the conditions through JSON is painful. I have documented it [here](https://github.com/RoystonS/honeycomb/blob/main/bravo-lights.md), but it's still not at all easy
- changes to the configuration file require restarting the flight
- it's not possible to define different configurations for different aircraft; this is problematic because, for instance, the 'low oil pressure' level varies considerably from one aircraft to another
- light conditions can only be based on "A:" simulator variables. This means that it's impossible to map MASTER WARNING and MASTER CAUTION correctly, as they are "L": variables
- there are no tools to help with debugging conditions
- it responds slowly (around 1 second) to simulator changes; pressing autopilot buttons, for instance, feels very sluggish because it takes a second for the light to light up

The 'Better Bravo Lights' tool makes significant improvements:

- the configuration file is a fraction of the size of the AFC configuration file, and much more readable
- changes to the configuration file are respected within a couple of seconds, with no flight restart required
- the configuration file supports different configurations for different aircraft
- it supports both A: variables and L: variables (L: variables requires [FSUIPC](http://www.fsuipc.com/) [free edition] and its WASM module)
- it provides a user-interface to help with the development of conditions, showing condition expressions for each light and a live view of the relevant simulator variables
- lights respond immediately (1 simulator frame for A: variables and 0.1s for L: variables)
- more sophisticated conditions can be specified (specifically, any combinations of variables, constants and arithmetic)

# What's the status of it?

- With a fair following wind, it all works fine; I've been using it day-to-day for some time
- Error-handling is minimal right now so you may not get good feedback if you get the configuration wrong.
- It may not be able to find all possible installation locations for all varieties of Windows Store and Steam installations. It's safe: if it can't figure things out, it won't change anything.

# How do I install/uninstall it?

Simply:

1. download it
1. unpack the zip (anywhere you like: it doesn't need to go in the Community folder)
1. run `install.bat`

The installation process will modify your MSFS startup so that it no longer runs the Honeycomb AFC Bridge but instead runs Better Bravo Lights.

To uninstall, just run `uninstall.bat`; that'll remove Better Bravo Lights from your MSFS startup and restore the Honeycomb AFC Bridge.

If you want to use L: variables (which you should: it gets you MASTER WARNING/CAUTION) you should also install FSUIPC (the free edition is fine) and enable its WASM module.

## Try it without installing

You can also just try it out without making any installation changes, or even without restarting MSFS:

- once MSFS is running and at the main menu, start Windows Task Manager and terminate the AFC_Bridge
- run the BetterBravoLights.exe file

# How do I configure it?

Firstly: you may not need to. The configuration that comes with it has a lot of entries already populated - fixing a lot of the issues with the standard AFC bridge - but you may need to add entries for other aircraft, or change the light configurations.

Configuration is simply a matter of editing the `Config.ini` file. Better Bravo Lights monitors that file and automatically reconfigures when it's changed.

It's a good idea not to _guess_ at values such as the threshold for low oil pressure; instead, check the POH, or check in the actual aircraft module itself. (`panel.xml` is often the place to look as it shows red and yellow warning ranges for such things, or annunciator conditions.)

If you make some useful configuration entries, please send submit an issue/PR and we'll look at adding them to the default configuration!

## Configuration Syntax

The configuration file is a standard [.ini file](https://en.wikipedia.org/wiki/INI_file).

- Lines starting with a semicolon (;) are treated as comments and are ignored
- Each section is delimited with a section name in square brackets, e.g. `[Default]`
- Configuration for an aircraft is in a section named `[Aircraft.<aircraftname>]`
  - e.g. `[Aircraft.Asobo_TBM930]` or `[Aircraft.Asobo_Pitts]`
- Light configurations in the `[Default]` section apply to every aircraft unless an aircraft overrides a light
- Each light setting line is of the format
  - `lightname = expression`
- Expression types:
  - variable expressions
    - `A:ELECTRICAL MAIN BUS VOLTAGE, volts`
      - Note that the full A: variable name is required together with the units.
      - A full list of A: variables can be found here: https://docs.flightsimulator.com/html/Programming_Tools/SimConnect/Status/Status_Of_Simulation_Variables.htm
      - _Indexed_ A: variables are supported. For instance, `A:ENG OIL PRESSURE:1` and `A:ENG OIL PRESSURE:2` are the different oil pressures for engines 1 and 2
    - `L:Generic_Master_Warning_Active`
      - No units are needed for L: variables
      - Many L: variables are aircraft-specific
  - literal numbers
    - decimal: 0, 1, 2.5
      - N.B. negative literals are not yet supported. Coming soon!
    - hexadecimal: 0x5678
  - fixed light values
    - `ON`
    - `OFF`
  - comparison expressions
    - `<` (less than)
    - `<=` (less than or equal to)
    - `==` (equal to)
    - `!=` (not equal to)
    - `>=` (greater than or equal to)
    - `>` (greater than)
  - arithmetic expressions
    - `+` (add)
    - `-` (subtract)
    - `*` (multiply)
    - `/` (divide)
  - logical expressions
    - `AND` (can also be written as `&&`)
    - `OR` (can also be written as `||`)
  - grouping
    - standard arithmetic precedence (multiplication and division bind more tightly than addition and subtraction; AND binds more tightly than OR); this can be overridden with parentheses
    - `(1 + 2) * 3` is different from `1 + 2 * 3`

## Examples

- `LowHydPressure = OFF`

  Specifies that the 'LOW HYD PRESSURE' light should be permanently off. This can be used to override configuration from the [Default] section which isn't appropriate for some specific aircraft

- `LowVolts = A:ELECTRICAL MAIN BUS VOLTAGE, volts < 24.5`

  Specifies that the 'LOW VOLTS' light should be on if the `ELECTRICAL MAIN BUS VOLTAGE` variable (measured in volts) is less than 24.5. Otherwise it should be off.

- `LowOilPressure = A:GENERAL ENG OIL PRESSURE:1, bar < 2.5`

  Specifies that the 'LOW OIL PRESSURE' light should be on if the `GENERAL ENG OIL PRESSURE` for engine 1 (measured in bars) is less than 2.5.

- `LowOilPressure = A:GENERAL ENG OIL PRESSURE:1, psi < 10`

  Specifies that the 'LOW OIL PRESSURE' light should be on if the `GENERAL ENG OIL PRESSURE` for engine 1 (measured in psi) is less than 10.

- `GearLRed = A:GEAR LEFT POSITION, percent over 100 > 0 AND A:GEAR LEFT POSITION, percent over 100 < 1`

  Specifies that the left gear red light should be on if the `GEAR LEFT POSITION` (measured in percent/100) is between 0 and 1 (but isn't exactly 0 nor exactly 1); this is how we specify that a gear light should be red if the gear is _partially_ down.

- `LowOilPressure = (A:GENERAL ENG OIL PRESSURE:1, psf > 0 AND A:GENERAL ENG OIL PRESSURE:1, psf < 15120) OR (A:GENERAL ENG OIL PRESSURE:2, psf > 0 AND A:GENERAL ENG OIL PRESSURE:2, psf < 15120)`

  Specifies that the 'LOW OIL PRESSURE' light should be on if either of the two engine's oil pressures is between 0 and 15120. It will _not_ be lit if the oil pressure is precisely 0 (which means it still works on a single-engine aircraft).

## Light Names

The light names largely match those found on the Bravo itself:

- Autopilot buttons
  - `HDG`, `NAV`, `APR`, `REV`, `ALT`, `VS`, `IAS`, `AUTOPILOT`
- Gear lights; 3 pairs of green and red lights
  - `GearLGreen`, `GearLRed`
  - `GearCGreen`, `GearCRed`
  - `GearRGreen`, `GearRRed`
- Annunciator lights
  - `MasterWarning`, `EngineFire`, `LowOilPressure`, `LowFuelPressure`, `AntiIce`, `StarterEngaged`, `APU`
  - `MasterCaution`, `Vacuum`, `LowHydPressure`, `AuxFuelPump`, `ParkingBrake`, `LowVolts`, `Door`

## How do I debug the configuration?

Whilst Better Bravo Lights is running, it'll show an icon in the system tray. Right-click that and click 'Debugger'. A diagnostics window will open up, showing the status of each light. Clicking on the radio button next to a light will show the configured expression and the current values of all the variables mentioned in the expression.

![](./DebuggerUI.png)
In the above picture we can see:

- several autopilot and annunciator lights are lit
- all of the gear lights are green
- we've selected the LOW VOLTS light to find out what's driving that light
  - we can see the light is using the expression `(A:ELECTRICAL MAIN BUS VOLTAGE, volts < 24.5)`
  - we can see the current value of `A:ELECTRICAL MAIN BUS VOLTAGE, volts` is 22.96. This is less than 24.5, which is why the light is lit

# FAQ

## Will this mess up my existing Bravo configuration?

No. It doesn't change your existing Bravo configuration; it's a completely new Bravo lights tool written from scratch. Installing it will change your MSFS startup configuration to run Better Bravo Lights instead of the Honeycomb AFC Bridge, but that's the only change.

## Will it cause CTDs?

It shouldn't. It just uses the standard SimConnect APIs to request variable information from the simulator.

## What features are missing?

There's a limit on what features can realistically be added to a tool which simply turns lights on and off, but here are some things coming soon:

- better error reporting for invalid expressions; e.g. if you forget the units for an A: variable the message isn't very helpful
- better error reporting for FSUIPC being missing
- a UI for showing all of the L: variables available in the simulator
- more complete default configurations
  - the ideal would be that all of the built-in and common aircraft are fully configured out of the box
  - you can help with that – send me your configuration changes!

## What bugs are there?

1. When the app starts up it briefly flashes up the debugging window. [If you’re technical and know how to get an HWnd (needed to talk to SimConnect) from a WPF without showing a UI, please let me know!]
1. Virtually no nice error-checking and reporting.
1. The debugger UI is really messy. I’ll tidy it up when everything else is stable. Right now, it works, so making it prettier is lower priority.
1. When returning to the main menu, the lights will turn on and off a few times and then remain on in a strange state. This is because the simulator tells us that it’s not running, then it is, then it isn’t, and then it tells us it’s running whilst on the main menu. [If you’re technical and know how to detect that we’re at the main menu and not in the sim, please let me know!]

## Can I see the code?

Of course. It's all available on GitHub: [https://github.com/RoystonS/BetterBravoLights](https://github.com/RoystonS/BetterBravoLights)

## How do I report problems or make suggestions?

Please raise an issue at the GitHub repository: [https://github.com/RoystonS/BetterBravoLights/issues](https://github.com/RoystonS/BetterBravoLights/issues)
