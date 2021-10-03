# Dealing with corrupt `exe.xml` files

If you've got to this page you've probably seen an "Installation Aborted" message when trying to install _Better Bravo Lights_.

## What's the problem?

Something you've previously tried to install has
corrupted one of your Flight Simulator configuration files; specifically, a file called `exe.xml` which controls the programs that Flight Simulator should launch when the simulator starts.

In order for Better Bravo Lights to install itself properly - by arranging for itself to be auto-launched when the simulator starts - it needs to modify that file. But if that file is _already_ corrupt, Better Bravo Lights can't safely modify it and will refuse to try to install.

## Oh, never mind then. I just won't install Better Bravo Lights

That's certainly an option BUT this corrupted file will prevent other programs - not just Better Bravo Lights - from installing into MSFS correctly, and may indeed already have broken some installations. You need to fix it whether or not you're installing Better Bravo Lights.

(Also, you _can_ run BetterBravoLights without installing it, but you'd need to start it manually.)

## So what do I do?

You've a few options to fix your `exe.xml` file:

1. Fix it yourself

   You'll need some technical expertise - specifically a little understanding of XML files - to do this. The bulk of this document contains the technical details that will enable those of you with this expertise to fix it yourself.

1. Get somebody else to help you fix it

   If you're not technical, you should get somebody to help you fix the XML file. If you point them at this document it'll help them figure out how to fix it.

1. Send it to us. If you don't have a friendly neighbourhood XML techie, raise a support question at our [GitHub](https://github.com/RoystonS/BetterBravoLights/issues/new/choose) and if we have some time we'll try to take a look at it for you. Please be sure to paste your entire `exe.xml` file into the query! It's a bit sad that we're having to fix files broken by other installers, but if we can help, we will.

# Technical details for fixing the corrupt file

Here are the technical details for fixing the corrupt file.

## Which file is broken?

If you have a Windows Store installation, it'll be

`%LOCALAPPDATA%\Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe\LocalCache\exe.xml`

If you have a Steam installation, it'll be

`%APPDATA%\Microsoft Flight Simulator\exe.xml`

<!--
The Better Bravo Lights installer should have given you the precise path to the broken file; the path will vary from installation to installation.
-->

## How did the file get corrupt?

Some software installers are a little broken and will break `exe.xml` if they find anything unexpected. (The Better Bravo Lights installer is very careful, refusing even to _attempt_ to install if the file is already broken.)

## How is it broken?

The `exe.xml` file that controls the auto-launch behaviour of Flight Simulator is an [XML](https://en.wikipedia.org/wiki/XML) file. If you've ever looked at the source of a web page you'll have seen HTML, which is a bit like a 'loose' version of XML.

XML files have to follow a set of rules. Your `exe.xml` file is breaking at least one of those rules.

Here's how a good `exe.xml` file should look:

```xml
<?xml version="1.0"?>
<SimBase.Document Type="SimConnect" version="1,0">
  <Descr>Auto launch external applications on MSFS start</Descr>
  <Filename>exe.xml</Filename>
  <Disabled>False</Disabled>
  <Launch.Addon>
    <Name>Addon1</Name>
    <Disabled>False</Disabled>
    <Path>C:\Somewhere\Program1.exe</Path>
  </Launch.Addon>
  <Launch.Addon>
    <Name>Addon2</Name>
    <Disabled>False</Disabled>
    <Path>C:\Somewhere\Program2.exe</Path>
  </Launch.Addon>
</SimBase.Document>
```

Don't worry about the precise details. Notice the _shape_: there's a `<SimBase.Document>` line near the top and a matching `</SimBase.Document>` line at the bottom. In fact, _every_ such 'element' consists of an opening tag, a closing tag, and some more XML inside.

The two most important rules are:

- Every element that is opened must be closed
- There must only be one element at the top of the file; all elements must be inside it. In the case of the `exe.xml` file this means that _everything_ must be inside a single `<SimBase.Document>` element.

**Your `exe.xml` file almost certainly breaks one or both of those rules.**

## An example

Let's look at a real-life example of a corrupt `exe.xml` file I've seen in the wild:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<SimBase.Document version="1,0" Type="SimConnect">
  <Descr>Auto launch external applications on MSFS start</Descr>
  <Filename>exe.xml</Filename>
  <Disabled>False</Disabled>
  <Launch.Addon>
    <Name>RealSimGear Add-on</Name>
    <Disabled>False</Disabled>
    <Path>E:\FSdata\Community\RealSimGear\bin\RealSimGear.exe</Path>
  </Launch.Addon>
</SimBase.Document>
<SimBase.Document version="1,0" Type="SimConnect">
  <Descr>Auto launch external applications on MSFS start</Descr>
  <Filename>exe.xml</Filename>
  <Disabled>False</Disabled>
  <Launch.Addon>
    <Name>RealSimGear Add-on</Name>
    <Disabled>False</Disabled>
    <Path>E:\FSdata\Community\RealSimGear\bin\RealSimGear.exe</Path>
  </Launch.Addon>
</SimBase.Document>
  <Launch.Addon>
    <Name>Logitech Microsoft Flight Simulator Plugin Steam</Name>
    <Disabled>False</Disabled>
    <Path>C:\Program Files\Logitech\Microsoft Flight Simulator Plugin\LogiMicrosoftFlightSimulator.exe</Path>
    <CommandLine>-r</CommandLine>
  </Launch.Addon>
  <Launch.Addon>
    <Name>AFCBridge</Name>
    <Disabled>False</Disabled>
    <Path>E:\FSdata\community\AFC_Bridge\bin\AFC_Bridge.exe</Path>
  </Launch.Addon>
</SimBase.Document>
```

It's a bit longer, but again, ignore the detail and look at the _shape_:

- There are multiple top-level `<SimBase.Document>` elements
- The last two `<Launch.Addon>` elements aren't inside a `<SimBase.Document>` element: the closing `</SimBase.Document>` tag at the end of the file has no matching `<SimBase.Document>` opening tag.

In fact, we can see that there are duplicate stray `<SimBase.Document>` elements at the top of the file, both from "RealSimGear Add-on". This suggests that possibly the RealSimGear installer was run several times and incorrectly modified the `exe.xml` file each time.

Repairing it involves putting a single `<SimBase.Document>` element at the top-level and putting the `<Launch.Addon>` elements inside that.

The repaired version of this file looks like this:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<SimBase.Document version="1,0" Type="SimConnect">
  <Descr>Auto launch external applications on MSFS start</Descr>
  <Filename>exe.xml</Filename>
  <Disabled>False</Disabled>
  <Launch.Addon>
    <Name>RealSimGear Add-on</Name>
    <Disabled>False</Disabled>
    <Path>E:\FSdata\Community\RealSimGear\bin\RealSimGear.exe</Path>
  </Launch.Addon>
  <Launch.Addon>
    <Name>Logitech Microsoft Flight Simulator Plugin Steam</Name>
    <Disabled>False</Disabled>
    <Path>C:\Program Files\Logitech\Microsoft Flight Simulator Plugin\LogiMicrosoftFlightSimulator.exe</Path>
    <CommandLine>-r</CommandLine>
  </Launch.Addon>
  <Launch.Addon>
    <Name>AFCBridge</Name>
    <Disabled>False</Disabled>
    <Path>E:\FSdata\Community\AFC_Bridge\bin\AFC_Bridge.exe</Path>
  </Launch.Addon>
</SimBase.Document>
```

## Why hasn't MSFS complained about my corrupt `exe.xml` file?

This is a good question. MSFS seems to try to read the bits of `exe.xml` that it can understand. In the corrupt example above, MSFS was processing the "RealSimGear Add-on" but no others. It read the _first_ root element it came to and simply ignored the rest of the file.

Such 'relaxed' behaviour sounds good at first - it did _something_ with the file instead of complaining - but it meant that in this particular case, the other programs that were _supposed_ to be auto-launched ... weren't. It was silently failing to run them, causing the owner of this particular file to wonder why bits of his hardware hadn't been working properly for 10 months.
