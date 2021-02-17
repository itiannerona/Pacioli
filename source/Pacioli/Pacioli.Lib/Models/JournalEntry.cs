﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System;

namespace Pacioli.Lib.Models
{
    public record JournalEntry
    {
        public JournalEntry()
        {
            Debits = new List<JournalEntryItem>();
            Credits = new List<JournalEntryItem>();
        }

        public JournalEntry([NotNull] List<JournalEntryItem> debits, [NotNull] List<JournalEntryItem> credits)
        {
            if (debits is null || credits is null)
                throw new ArgumentNullException("Debits and credits should not be null");

            if (!debits.Any() || !credits.Any())
                throw new ArgumentException("Debits and credits length should not be empty.");
        }

        public List<JournalEntryItem> Debits { get; set; }
        public List<JournalEntryItem> Credits { get; set; }
    }
}
