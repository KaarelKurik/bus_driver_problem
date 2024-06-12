using System.Text.RegularExpressions;
using Microsoft.VisualBasic;

namespace turnit_problem;

partial class Program
{
    static void PrintInstructions() {
        Console.WriteLine(
            "Instructions:\n" +
            "   The program takes inputs in the form `hh:mmhh:mm`,\n" +
            "   indicating a start time (first hh:mm) and end time (second hh:mm) for a bus driver's break.\n" +
            "   The break time is specified inclusively, i.e. it includes the start minute and the end minute.\n" +
            "   A break must start and end on the same day.\n" +
            "   Each input must occupy its own line, both in interactive mode and when reading from a file.\n" +
            "   Write `quit` or `exit` to end an interactive session and stop the program."
        );
    }
    static void PrintUsage() {
        string programName = "turnit_problem";
        // Formatting convention for program usage info stolen from common unix utilities.
        (string, string)[] usageForms = [
            ($"{programName}", "start an interactive session"),
            ($"{programName} filename [FILE]", "read input from file [FILE], then continue interactively")
        ];
        string[] usagePrefixes = ["Usage:", "   or:"];
        int maxUsageFormLength = usageForms.Select(pair => pair.Item1.Length).Max();
        string[] paddedForms = usageForms.Select(pair => $"{pair.Item1.PadRight(maxUsageFormLength+8)}{pair.Item2}").ToArray();
        string[] outUsageLines = paddedForms.Select((val, index) => $"{usagePrefixes[Math.Min(index,1)]} {val}").ToArray();
        foreach (string l in outUsageLines) {
            Console.WriteLine(l);
        }
    }
    static (int,int) ParseTimeRange(string timeRange)
    {
        string generalFailure = $"Failed parsing time range `{timeRange}`!";
        Regex r = TimeRangeRegex();
        Match match = r.Match(timeRange);
        if (!match.Success) {
            throw new ArgumentException($"{generalFailure} Pattern does not match.");
        }
        int startHour = int.Parse(match.Groups[1].ToString());
        int startMinutes = int.Parse(match.Groups[2].ToString());
        int endHour = int.Parse(match.Groups[3].ToString());
        int endMinutes = int.Parse(match.Groups[4].ToString());


        if (startHour > 23) {
            throw new ArgumentException($"{generalFailure} Start hour value is too large.");
        }
        if (startMinutes > 59) {
            throw new ArgumentException($"{generalFailure} Start minute value is too large.");
        }
        if (endHour > 23) {
            throw new ArgumentException($"{generalFailure} End hour value is too large.");
        }
        if (endMinutes > 59) {
            throw new ArgumentException($"{generalFailure} End minute value is too large.");
        }
        int startTime = startHour * 60 + startMinutes;
        int endTime = endHour * 60 + endMinutes;
        if (startTime > endTime) {
            throw new ArgumentException($"{generalFailure} Start time exceeds end time.");
        }
        return (startTime, endTime);
    }

    static void IncrementCounts(int[] timeCounts, (int, int) timeRange) {
        foreach (int index in Enumerable.Range(timeRange.Item1, timeRange.Item2-timeRange.Item1+1)) {
            timeCounts[index] += 1;
        }
    }

    static string TimeToHourMinutesString(int time) {
        return $"{time/60:00}:{time%60:00}";
    }

    // Finds longest contiguous block with maximal number of breaks.
    // Will only print first such block!
    static void PrintBusiestPeriod(int[] timeCounts) {
        int maxVal = timeCounts.Max();

        int longestBlockStart = 0;
        int longestBlockEnd = 0; // exclusive

        int curBlockStart = 0;
        int curBlockEnd = 0;
        bool inBlock = false;

        for (int i = 0; i < timeCounts.Length; ++i) {
            if (timeCounts[i] == maxVal) {
                if (!inBlock) {
                    curBlockStart = i;
                    inBlock = true;
                }
            } else {
                if (inBlock) {
                    curBlockEnd = i;
                    if (longestBlockEnd - longestBlockStart < curBlockEnd - curBlockStart) {
                        longestBlockStart = curBlockStart;
                        longestBlockEnd = curBlockEnd;
                    }
                    inBlock = false;
                }
            }
        }
        if (inBlock) {
            curBlockEnd = timeCounts.Length;
            if (longestBlockEnd - longestBlockStart < curBlockEnd - curBlockStart) {
                longestBlockStart = curBlockStart;
                longestBlockEnd = curBlockEnd;
            }
            inBlock = false;
        }

        string rangeStartString = TimeToHourMinutesString(longestBlockStart);
        string rangeEndString = TimeToHourMinutesString(longestBlockEnd-1);
        string plural = maxVal > 1 ? "s" : "";
        Console.WriteLine($"Busiest range is {rangeStartString}-{rangeEndString} with {maxVal} driver{plural} taking a break.");
    }

    static void Main(string[] args)
    {
        PrintInstructions();

        // 24 * 60 is a small number, and computers are fast, so we're going to use the simplest algorithm
        // default value for int is 0 so fine to leave uninitialized
        int[] timeCounts = new int[24*60];

        // Input & validation
        if (args.Length >= 1) {
            if (args[0] != "filename") {
                Console.WriteLine($"Program does not recognize flag `{args[0]}.`");
                PrintUsage();
                return;
            }
            if (args.Length < 2) {
                Console.WriteLine("Too few arguments: filename missing!");
                PrintUsage();
                return;
            }
            if (args.Length > 2) {
                Console.WriteLine("Too many arguments: program takes only one filename!");
                PrintUsage();
                return;
            }
            // Since we only have one possible argument with one kind of input,
            // we're going to deal with it immediately. Would be bad style for a more complex program.

            string filename = args[1];
            string[] lines;
            try {
                lines = File.ReadAllLines(filename);
            } catch (Exception e) {
                Console.WriteLine($"Failed to read file `{filename}`!");
                Console.WriteLine(e.Message);
                return;
            }
            foreach (string line in lines) {
                (int, int) timeRange = ParseTimeRange(line); 
                IncrementCounts(timeCounts, timeRange);
            }
            PrintBusiestPeriod(timeCounts);
        }
        for (string? userInput = Console.ReadLine(); userInput != null && userInput != "exit" && userInput != "quit"; userInput = Console.ReadLine()) {
            (int, int) timeRange;
            try {
                timeRange = ParseTimeRange(userInput);
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                continue;
            }
            IncrementCounts(timeCounts, timeRange);
            PrintBusiestPeriod(timeCounts);
        }

        Console.WriteLine("Goodbye!");
    }

    [GeneratedRegex(@"([0-9]{2}):([0-9]{2})([0-9]{2}):([0-9]{2})")]
    private static partial Regex TimeRangeRegex();
}
