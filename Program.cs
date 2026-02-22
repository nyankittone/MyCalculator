// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
// using System.Text;
namespace Calculator;

class Program {
    private static IEnumerable<string> Tokenize(string input) {
        foreach(string bigToken in input.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)) {
            yield return bigToken;
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
        }
    }
}

