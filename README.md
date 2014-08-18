RS
==

Collectiion of useful development utilities

(Updated: 2014-08-18)

RS is a work in progress. I created this class library with the intention of compiling methods and classes I frequently use into a single location. Highlights include:

##Utilities.Windows

This namespace houses methods for interacting with program windows - useful, if not overly exciting. Methods to identify and close windows can be particularly useful when one needs to shell out an application to complete a certain process and/or programatically interact with an application's UI (via SendKeys, for example).

##Utilities.FileWriter

I developed this generic file writer in order to assist in writing large amounts of data from query results to file. The WriteFile method accepts either a DataTable or a List as an argument, along with parameters related to file format, and handles the header record and variable typing through reflection.
