
using System;

public class FrequentlyRequestException : Exception
{
    public FrequentlyRequestException(Exception ex) : base("Request tool frequently", ex)
    {

    }
}