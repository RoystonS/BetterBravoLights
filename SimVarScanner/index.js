// @ts-check

import fetch from 'node-fetch';

import { JSDOM } from 'jsdom';

let badUnits = 0;

const startUrl =
  'https://docs.flightsimulator.com/html/Programming_Tools/SimVars/Simulation_Variables.htm';

async function main() {
  const request = await fetch(startUrl);
  const contents = await request.text();
  const stuff = await JSDOM.fromURL(startUrl);
  const variableLinks = stuff.window.document.querySelectorAll(
    '.columns-multiple a'
  );

  const variableNames = new Set();
  const variablePages = new Set();
  for (const l of variableLinks) {
    const variableUrl = l.getAttribute('href');
    const url = new URL(variableUrl, startUrl);
    url.hash = '';
    const variableName = l.textContent;

    variableNames.add(variableName);
    variablePages.add(url.toString());
  }

  console.log('"Group","Variable","Units","Description"');

  for (const variablePage of variablePages) {
    await scanVariablePage(variablePage);
  }

  console.error(`Variables with bad units: ${badUnits}`);
}

/**
 * @param {string} page
 */
async function scanVariablePage(page) {
  const dom = await JSDOM.fromURL(page);
  const doc = dom.window.document;

  for (const tableElement of doc.querySelectorAll('table')) {
    let maybeHeading = tableElement.previousElementSibling;

    while (maybeHeading && !maybeHeading.localName.match(/^h\d$/)) {
      maybeHeading = maybeHeading.previousElementSibling;
    }

    const heading = maybeHeading ? maybeHeading.textContent : 'Unknown';

    let doubleDescriptionColumn = false;

    for (const row of tableElement.querySelectorAll('tr')) {
      if (row.querySelector('th')) {
        // Header row
        row.querySelectorAll('th').forEach((elem, index) => {
          if (elem.textContent === 'Description') {
            doubleDescriptionColumn = elem.colSpan == 2;
          }
        });

        continue;
      }

      if (heading === 'Fuel Tank Selection') {
        // This table is not variables
        break;
      }

      const cells = row.querySelectorAll('td');

      // Sometimes the description is split into two columns to indicate 'Shared cockpit'.
      // Sometimes it isn't.
      const varCell = cells[0];
      const descriptionCell = cells[1];
      if (!descriptionCell) {
        // Probably the mis-spelling of COM RECEIVE ALL
        continue;
      }

      const variableName = tidyString(varCell.textContent);
      const description = tidyString(descriptionCell.textContent);

      const unitCellIndex =
        descriptionCell.colSpan == 2 ? 2 : doubleDescriptionColumn ? 3 : 2;
      const unitsCell = cells[unitCellIndex];

      if (!unitsCell) {
        console.log(`Missing units cell ${page}: ${heading}: ${row.outerHTML}`);
        throw new Error();
      }

      const units = extractUnits(
        tidyString(unitsCell.textContent),
        variableName
      );
      if (!units) {
        continue;
      }

      writeEntry(heading, variableName, units, description);
    }
  }
}

function tidyString(text) {
  // Strings in the web pages have all sorts of embedded and leading spaces and newlines

  // Remove CR/LF
  text = text.replace(/[\r\n]/g, ' ');

  // Remove multiple strings
  text = text.replace(/\s+/g, ' ');

  text = text.trim();

  return text;
}
/**
 * @param {string} unitsText
 * @param {string} variableName
 */
function extractUnits(unitsText, variableName) {
  if (variableName === 'HEADING INDICATOR') {
    // Nobody wants radians!
    return 'degrees';
  }
  unitsText = unitsText.trim().toLowerCase();

  // Remove NBSPs
  unitsText = unitsText.replace(/\u00A0/g, ' ');

  unitsText = unitsText.replace('degrees.', 'degrees');
  unitsText = unitsText.replace('inch ()', 'inch');
  unitsText = unitsText.replace('feet ()', 'feet');
  unitsText = unitsText.replace('feet (ft)', 'feet');
  unitsText = unitsText.replace('feet ( ', 'feet ');
  unitsText = unitsText.replace('feet (ft per second', 'feet per second');
  unitsText = unitsText.replace('foot ()', 'foot');
  unitsText = unitsText.replace('feet/minute', 'feet per minute');
  unitsText = unitsText.replace(
    'slugs per feet squared (slug sqft)',
    'slugs per feet squared'
  );
  unitsText = unitsText.replace('foot pounds ()', 'foot pounds');
  unitsText = unitsText.replace('foot pounds (ftlbs)', 'foot pounds');
  unitsText = unitsText.replace('feet squared ()', 'feet squared');
  unitsText = unitsText.replace(
    'pound force per square foot (psf)',
    'pounds per square foot'
  );
  unitsText = unitsText.replace(
    'pounds per square foot (psf)',
    'pounds per square foot'
  );
  unitsText = unitsText.replace(
    'pounds per square foot, psf',
    'pounds per square foot'
  );
  unitsText = unitsText.replace(
    'pounds per square inch (psi)',
    'pounds per square inch'
  );

  unitsText = unitsText.replace(
    'pounds per square inch (psi',
    'pounds per square inch'
  );

  unitsText = unitsText.replace('psi scalar 16k (psi * 16384)', 'psi');
  unitsText = unitsText.replace(
    'percent scalar 16k (max load * 16384)',
    'percent over 100'
  );

  // RETRACT x FLOAT EXTENDED
  unitsText = unitsText.replace(/percent \(0 is fully.*\)/, 'percent over 100');

  // SPOILERS x POSITION
  unitsText = unitsText.replace(/percent over 100 or (.*)/, 'percent over 100');

  // ENG PRESSURE RATIO:index
  unitsText = unitsText.replace('ratio (0-16384)', 'ratio');

  if (unitsText.endsWith('(')) {
    // Stray ( on the end of the units!
    unitsText = unitsText.replace(/\s+\($/, '');
  }

  if (unitsText.startsWith('inches of mercury')) {
    return 'inhg';
  }

  if (unitsText === 'foot pound') {
    return 'foot pounds';
  }

  // These units are absolutely fine and can go straight through
  switch (unitsText) {
    case 'scalar':
    case 'bool':
    case 'percent':
    case 'percent over 100':
    case 'radians':
    case 'radians per second':
    case 'radians per second squared':
    case 'gforce':
    case 'degrees':
    case 'feet':
    case 'nautical miles':
    case 'meters':
    case 'centimeters':
    case 'number':
    case 'seconds':
    case 'celsius':
    case 'rankine':
    case 'millibars':
    case 'kilo pascal':
    case 'ratio':
    case 'volts':
    case 'amps':
    case 'amperes':
    case 'inhg':
    case 'gallons':
    case 'gallons per hour':
    case 'pounds':
    case 'pounds per hour':
    case 'hours':
    case 'foot pounds':
    case 'foot pounds per second':
    case 'hz':
    case 'mhz':
    case 'feet per minute':
    case 'feet per second':
    case 'mach':
    case 'meters per second':
    case 'knots':
    case 'slugs per feet squared':
    case 'slugs per cubic feet':
    case 'feet per second squared':
    case 'psi':

    case 'pound force per square foot':
    case 'per radian':
    case 'per second':
    case 'square feet':

    case 'frequency bcd16':
    case 'position':
      return unitsText;
  }

  // These units have nicer forms
  switch (unitsText) {
    case 'meter':
      return 'meters';
    case 'psf':
    case 'pounds per square foot':
    case 'pounds per square inch':
      return 'psi';
    case 'percent_over_100':
    case 'percentage':
      return 'percent over 100';
  }

  if (unitsText.startsWith('enum')) {
    return 'enum';
  }

  if (unitsText.startsWith('mask')) {
    return 'mask';
  }

  if (unitsText.startsWith('position') || unitsText.startsWith('or position')) {
    return 'position';
  }

  if (unitsText.startsWith('string')) {
    // Not supported by Better Bravo Lights
    return undefined;
  }

  switch (variableName) {
    case 'AIRSPEED MACH':
      return 'mach';
    case 'GROUND VELOCITY':
      return 'knots';
    case 'CANOPY OPEN':
      return 'percent over 100';
  }

  if (variableName.match(/WATER.*ANGLE/)) {
    return 'degrees';
  }
  if (variableName.match(/WATER.*POSITION/)) {
    return 'postion';
  }
  
  if (variableName === 'SLIGHT OBJECT ATTACHED') {
	  // supports both bool + string; we only support bool
    return 'bool';
  }

  if (
    variableName.endsWith(' PCT') ||
    variableName.endsWith(' PCT:index') ||
    variableName.endsWith(' PERCENT') ||
    variableName.endsWith(' PERCENT:index')
  ) {
    return 'percent';
  }

  if (variableName.match(/GEAR.*POSITION/)) {
    return 'percent over 100';
  }

  if (variableName.match(/LIGHT.*INTENSITY/)) {
    return 'percent over 100';
  }

  if (
    variableName.startsWith('AUTOPILOT AIRSPEED ') ||
    variableName.startsWith('AIRSPEED ') ||
    variableName.startsWith('AIRCRAFT WIND ')
  ) {
    return 'knots';
  }

  if (variableName.endsWith('MACH')) {
    return 'mach';
  }

  if (variableName.startsWith('BREAKER ')) {
    return 'bool';
  }

  if (variableName.startsWith('CG ')) {
    return 'percent over 100';
  }

  // The web pages list the fuel variables as of type 's'. :(
  if (variableName.match(/FUEL.*LEVEL/)) {
    return 'percent over 100';
  }
  if (
    variableName.match(/FUEL.*CAPACITY/) ||
    variableName.match(/FUEL.*QUANTITY/)
  ) {
    return 'gallons';
  }

  if (variableName.includes('TEMPERATURE')) {
    return 'degrees';
  }

  if (variableName.endsWith(' RPM') || variableName.endsWith(' RPM:index')) {
    return 'number';
  }

  if (unitsText.startsWith('simconnect_data_')) {
    // Not straightforward numbers
    return undefined;
  }
  if (unitsText.startsWith('pid_struct')) {
    // Not straightforward numbers
    return undefined;
  }

  console.error(`Unknown units: ${unitsText} for ${variableName}`);
  badUnits++;
  return undefined;
}

function writeEntry(heading, variableName, units, description) {
  // There are some variables that the Microsoft pages INCORRECTLY list as not indexed.
  // Let's fix them to make them indexed:
  switch (variableName) {
    case 'NAV ACTIVE FREQUENCY':
    case 'NAV STANDBY FREQUENCY':
      writeEntry(heading, variableName + ':index', units, description);
      return;
  }

  if (variableName.match(/:index$/)) {
    let start = -1;
    let end = -1;

    // It's an indexed variable. What indexes should we generate?
    const [, variableBase] = variableName.match(/^(.*):index$/);

    if (
      heading === 'AIRCRAFT ENGINE VARIABLES' ||
      variableBase === 'FUEL CROSS FEED' ||
      variableBase === 'FUELSYSTEM ENGINE PRESSURE'
    ) {
      // Engines
      start = 1;
      end = 4;
    } else if (
      (heading === 'COM' && variableBase.endsWith('FREQUENCY')) ||
      variableBase === 'COM AVAILABLE' ||
      variableBase === 'COM RECEIVE' ||
      variableBase === 'COM RECEIVE EX1' ||
      variableBase === 'COM SPACING MODE' ||
      variableBase === 'COM STATUS' ||
      variableBase === 'COM TEST' ||
      variableBase === 'COM TRANSMIT' ||
	  variableBase.startsWith('COM ACTIVE ')
    ) {
      start = 1;
      end = 3;
    } else if (
	  ((heading === 'NAV') && variableBase.endsWith('FREQUENCY')) ||
	  variableBase.startsWith('NAV CLOSE ')
	) {
      start = 1;
      end = 4;
	} else if (heading === 'TACAN') {
      start = 1;
      end = 2;
      // TODO: CHECK
    } else if (variableName.match(/AVIONICS.*SWITCH/)) {
      start = 1;
      end = 2;
    } else if (variableName.match(/KOHLSMAN SETTING/)) {
      start = 1;
      end = 2;
    } else if (variableName.match(/ENG MASTER ALTERNATOR/)) {
      start = 1;
      end = 4;
    } else if (heading === 'APU') {
      start = 1;
      end = 3;
    } else if (heading === 'Landing Gear') {
      start = 1;
      end = 3;
    } else if (variableName.match(/CONTACT POINT/)) {
      start = 0;
      end = 19;
    } else if (variableName.match(/COCKPIT VIEW/)) {
      start = 0;
      end = 9;
    } else if (variableName.match(/CAMERA GAMEPLAY/)) {
      start = 0;
      end = 1;
    } else if (variableName.match(/FUEL.*PUMP/)) {
      start = 1;
      end = 4;
    } else if (variableName.match(/FUEL.*JUNCTION/)) {
      // Junction.N
      start = 1;
      end = 4;
    } else if (variableBase.match(/FUELSYSTEM LINE FUEL.*/)) {
      // Line.N
      start = 1;
      end = 4;
    } else if (
      variableBase === 'FUEL TANK SELECTOR' ||
      variableBase.match(/FUELSYSTEM TANK.*/)
    ) {
      // Tank.N
      start = 1;
      end = 4;
    } else if (variableBase.startsWith('FUEL SELECTED QUANTITY')) {
      // Tank
      start = 0;
      end = 20;
    } else if (variableName.match(/TRIGGER STATUS/)) {
      // Trigger.N
      start = 1;
      end = 4;
    } else if (variableName.match(/FUEL.*VALVE/)) {
      // Valve.N
      start = 1;
      end = 4;
    } else if (variableName.match(/PITOT HEAT SWITCH/)) {
      start = 1;
      end = 2;
    } else if (
      variableName.match(/DROPPABLE OBJECTS/) ||
      variableName.match(/PAYLOAD STATION/)
    ) {
      start = 1;
      end = 4;
    } else if (variableName.match(/EXIT TYPE/)) {
      start = 1;
      end = 4;
    } else if (variableName.match(/PUSHBACK STATE/)) {
      start = 1;
      end = 4;
    } else if (heading === 'Helicopter Only (DEPRECATED)') {
      // 1==main, 2==tail
      start = 1;
      end = 2;
    } else if (variableBase === 'CAMERA VIEW TYPE AND INDEX MAX') {
      start = 0;
      end = 5;
    } else if (variableBase === 'SMART CAMERA INFO') {
      start = 0;
      end = 1;
    } else if (variableBase === 'SMART CAMERA LIST') {
      start = 0;
      end = 12;
    } else if (variableBase === 'CAMERA VIEW TYPE AND INDEX') {
      writeEntry(heading, `${variableBase}:0`, 'Enum', '');
      writeEntry(heading, `${variableBase}:1`, 'Number', '');
      return;
    } else if (variableBase === 'WING FLEX PCT') {
      start = 1;
      end = 2;
    } else if (variableBase === 'LIGHT POTENTIOMETER') {
      // Lights are indexed from 1-13
      start = 1;
      end = 13;
	}

    if (start < 0) {
      throw new Error(`What indexes for ${heading}/${variableName}?`);
    }

    for (let i = start; i <= end; i++) {
      writeEntry(heading, `${variableBase}:${i}`, units, description);
    }
    return;
  }

  console.log(
    `${JSON.stringify(heading)}, ${JSON.stringify(
      variableName
    )}, ${JSON.stringify(units)}, ${JSON.stringify(description)}`
  );
}
void main();
