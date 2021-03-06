﻿using System;

namespace Silphid.Showzup
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class VariantAttribute : Attribute
    {
        public string Variant { get; }

        public VariantAttribute(string variant)
        {
            Variant = variant;
        }
    }
}