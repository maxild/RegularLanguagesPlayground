%namespace LexScanner

%option noparser, verbose

%{
// Since we create a new scanner for each new input file, we make these counter variables static.
public static int lineTot = 0;
public static int wordTot = 0;
public static int intTot = 0;
public static int fltTot = 0;
%}

newline (\n|\r\n?)
alpha [a-zA-Z]
alphaplus [a-zA-Z\-’]
digits [0-9]+

%%

// these are turned in to locale variables in the main driver (scan/yylex)
%{
int lineNum = 0;
int wordNum = 0;
int intNum = 0;
int fltNum = 0;
%}

{newline}                   { lineNum++; lineTot++; }
{alpha}{alphaplus}*{alpha}  { wordNum++; wordTot++; }
{digits} intNum++;          { intTot++; }
{digits}\.{digits}          { fltNum++; fltTot++; }

<<EOF>> {
    Console.Write("Lines: " + lineNum);
    Console.Write(", Words: " + wordNum);
    Console.Write(", Ints: " + intNum);
    Console.WriteLine(", Floats: " + fltNum);
}

%%

// No user code
