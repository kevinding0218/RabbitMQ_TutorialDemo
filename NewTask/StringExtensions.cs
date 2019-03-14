using System;
using System.Linq;

public static class StringExtensions
{
    public static string Repeat(this string s, int n) => new String(Enumerable.Range(0, n).SelectMany(x => s).ToArray());
}