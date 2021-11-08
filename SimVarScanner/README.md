# Sim Var Scanner

This is a NodeJS project which scrapes the Microsoft
SimVar web pages (https://docs.flightsimulator.com/html/index.htm#t=Programming_Tools%2FSimVars%2FSimulation_Variables.htm) to produce a CSV file containing all known `A:` variables.

Two particularly tricky parts are determining units and ranges of `:index` variables.

## Determining units

There are many problems in the Microsoft tables; whilst many of the 'Units' cells are perfectly readable, many are simply blank, and many simply contain the letter 's'. Most that reference feet have stray parentheses, which aren't always closed.

So we have to have a big block of code to cope with the special cases.

## Determining index ranges

Variables of the form `FOO BAR:index` can't be read directly, and indexed forms `FOO BAR:1`, `FOO BAR:2` need to be read. It's not always at all obvious what the ranges should be: for engines it's typically 1-4; for wings, 1-2; for contact points, 0-19.

So we have to have a big block of code to cope with the special cases.
