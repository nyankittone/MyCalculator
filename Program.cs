// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using System.Linq;
// using System.Text;
using RE = System.Text.RegularExpressions;
namespace Calculator;

class Program {
    private static IEnumerable<string> Tokenize(string input) {
        RE.Regex re = new(@"[\+\-\*\/]");

        foreach(string bigToken in String.Concat(input.Select((thing) => thing == '\t' ? ' ' : thing))
            .Split(" ", StringSplitOptions.RemoveEmptyEntries))
        {
            var matches = re.Matches(bigToken);
            int numberBegin = 0;

            foreach(RE.Match match in matches) {
                // get token to the left of the operator
                int numberEnd = match.Index;
                string number = bigToken[numberBegin..numberEnd];
                if(number.Length > 0) {
                    yield return number;
                }

                // get operator
                switch(match.Value) {
                    case "+":
                        yield return "+";
                        break;
                    case "-":
                        yield return "-";
                        break;
                    case "*":
                        yield return "*";
                        break;
                    case "/":
                        yield return "/";
                        break;
                    default:
                        throw new NotImplementedException("wtf if this operator bruh");
                }

                numberBegin = numberEnd + match.Length;
            }

            string finalNumber = bigToken[numberBegin..];
            if(finalNumber.Length > 0) {
                yield return finalNumber;
            }
        }
    }

    private static decimal Resolve(string input) {
        // My expression resolver should support:
        // addition, subtraction, multiplication, division, and exponents
        // predefined functions to compute
        // order of operations
        // reasonable errors

        // Create a tokenizer first
        return 42;
    }

    static void Main(string[] args) {
        Console.Error.Write("> ");
        while(Console.ReadLine() is string line) {
            foreach(string token in Tokenize(line)) {
                Console.WriteLine($"\"{token}\"");
            }

            Console.Error.Write("> ");
        }
    }
}

