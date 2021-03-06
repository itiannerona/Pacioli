﻿using Pacioli.Lib.Contracts.Models;
using Pacioli.Lib.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Pacioli.Tests.Unit
{
    public class JournalTests
    {
        [Theory, ClassData(typeof(JournalEntriesAreBalancedByNetZeroNormalBalance_TestData))]
        public void JournalEntriesAreBalancedByNetZeroNormalBalance(DateTime date, 
            List<JournalEntryDebitLine> debits, List<JournalEntryCreditLine> credits)
        {
            var sut = new JournalEntry(date, debits, credits);
            const decimal expectedVariance = 0m;

            decimal debitsSum = sut.Debits.Sum(dr => dr.Amount);
            decimal creditsSum = sut.Credits.Sum(cr => cr.Amount);
            var actualVariance = debitsSum + creditsSum;

            Assert.Equal(expectedVariance, actualVariance);
        }

        private class JournalEntriesAreBalancedByNetZeroNormalBalance_TestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[]
                {
                    DateTime.UtcNow, 
                    new List<JournalEntryDebitLine>
                    {
                        new JournalEntryDebitLine(new Account("Account", NormalBalance.Debit), 100m)
                    },
                    new List<JournalEntryCreditLine>
                    {
                        new JournalEntryCreditLine(new Account("Another Account", NormalBalance.Credit), -100m)
                    }
                };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Fact]
        public void JournalEntryPropertiesAreImmutable()
        {
            var sut = typeof(JournalEntry);
            var properties = sut.GetProperties();

            var anyPropertyIsMutable = AnyPropertyIsMutable(properties);

            Assert.False(anyPropertyIsMutable);
        }

        private static bool AnyPropertyIsMutable(IEnumerable<PropertyInfo> properties)
        {
            return properties.Any(prop =>
            {
                var genericTypeArgs = prop.PropertyType.GenericTypeArguments;
                if (genericTypeArgs.Any())
                {
                    //Check if generic type T is also mutable. 
                    var genericTypeProperties = genericTypeArgs.SelectMany(type => type.GetProperties());
                    return prop.CanWrite && AnyPropertyIsMutable(genericTypeProperties);
                }
                return prop.CanWrite;
            });
        }

        [Fact]
        public void AccountsAreComparedByValueSemantics()
        {
            const string accountName = "Account";
            var account = new Account(accountName, NormalBalance.Debit);
            var sameAccount = new Account(accountName, NormalBalance.Debit);

            Assert.Equal(account, sameAccount);
        }

        [Theory, ClassData(typeof(AccountsAreExclusiveToDebitOrCreditSide_TestData))]
        public void AccountsAreExclusiveToDebitOrCreditSide(DateTime date, 
            List<JournalEntryDebitLine> debits, 
            List<JournalEntryCreditLine> credits)
        {
            JournalEntry CreateJournalEntry() => new JournalEntry(date, debits, credits);
            
            Assert.Throws<ArgumentException>(CreateJournalEntry);
        }
        
        private class AccountsAreExclusiveToDebitOrCreditSide_TestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[]
                {
                    DateTime.UtcNow, 
                    new List<JournalEntryDebitLine>
                    {
                        new JournalEntryDebitLine(new Account("Account", NormalBalance.Debit), 1m)
                    },
                    new List<JournalEntryCreditLine>
                    {
                        new JournalEntryCreditLine(new Account("Account", NormalBalance.Debit), -1m)
                    }
                };
                yield return new object[]
                {
                    DateTime.UtcNow, 
                    new List<JournalEntryDebitLine>
                    {
                        new JournalEntryDebitLine(new Account("Another Account", NormalBalance.Debit), 5.23m)
                    },
                    new List<JournalEntryCreditLine>
                    {
                        new JournalEntryCreditLine(new Account("Another Account", NormalBalance.Debit), -5.23m)
                    },
                };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        
        [Fact]
        public void JournalEntryMayHaveDescription()
        {
            var sut = typeof(JournalEntry);
            var properties = sut.GetProperties();

            var descriptionProperty = properties
                .FirstOrDefault(prop => prop.PropertyType == typeof(string)
                                        && prop.Name == "Description");

            Assert.NotNull(descriptionProperty);
        }

        [Fact]
        public void JournalEntryItemPropertiesCanBeRead()
        {
            var sut = typeof(IJournalEntryLine);
            var properties = sut.GetProperties();

            bool propertiesCanBeRead = properties.All(prop => prop.CanRead);

            Assert.True(propertiesCanBeRead);
        }

        [Fact]
        public void JournalEntryItemPropertiesAreImmutable()
        {
            var sut = typeof(IJournalEntryLine);
            var properties = sut.GetProperties();

            bool propertiesAreImmutable = properties.All(prop => prop.CanWrite is false);

            Assert.True(propertiesAreImmutable);
        }

        [Theory, ClassData(typeof(JournalDoesNotAcceptUnbalancedEntries_TestData))]
        public void JournalDoesNotAcceptUnbalancedEntries(DateTime date, 
            List<JournalEntryDebitLine> debits, 
            List<JournalEntryCreditLine> credits)
        {
            JournalEntry CreateJournalEntry() => new JournalEntry(date, debits, credits);

            Assert.ThrowsAny<Exception>(CreateJournalEntry);
        }

        private class JournalDoesNotAcceptUnbalancedEntries_TestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[]
                {
                    DateTime.UtcNow, 
                    new List<JournalEntryDebitLine>
                    {
                        new JournalEntryDebitLine(new Account("Account", NormalBalance.Debit), 10m)
                    },
                    new List<JournalEntryCreditLine>
                    {
                        new JournalEntryCreditLine(new Account("Another Account", NormalBalance.Credit), -20m)
                    }
                };
                yield return new object[]
                {
                    DateTime.UtcNow,
                    new List<JournalEntryDebitLine>
                    {
                        new JournalEntryDebitLine(new Account("Account", NormalBalance.Debit), 8324m)
                    },
                    new List<JournalEntryCreditLine>
                    {
                        new JournalEntryCreditLine(new Account("Another Account", NormalBalance.Credit), -2313m)
                    }
                };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Theory, ClassData(typeof(JournalEntryThrowsExceptionOnInvalidArgument_TestData))]
        public void JournalEntryThrowsExceptionOnInvalidArgument(DateTime date, 
            List<JournalEntryDebitLine> debits, 
            List<JournalEntryCreditLine> credits)
        {
            JournalEntry CreateJournalEntry() => new JournalEntry(date, debits, credits);
            
            Assert.ThrowsAny<Exception>(CreateJournalEntry);
        }

        private class JournalEntryThrowsExceptionOnInvalidArgument_TestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[]
                {
                    new DateTime(),
                    new List<JournalEntryDebitLine>(),
                    new List<JournalEntryCreditLine>()
                };
                yield return new object[]
                {
                    null,
                    new List<JournalEntryDebitLine>
                    {
                        new JournalEntryDebitLine(new Account("Account", NormalBalance.Debit), 1m)
                    },
                    new List<JournalEntryCreditLine>()
                };
                yield return new object[]
                {
                    null,
                    new List<JournalEntryDebitLine>(),
                    new List<JournalEntryCreditLine>
                    {
                        new JournalEntryCreditLine(new Account("Another Account", NormalBalance.Credit), -1m)
                    }
                };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Theory, ClassData(typeof(JournalEntryDoesNotThrowExceptionWithValidArguments_TestData))]
        public void JournalEntryDoesNotThrowExceptionWithValidArguments(DateTime date, 
            List<JournalEntryDebitLine> debits, 
            List<JournalEntryCreditLine> credits)
        {
            JournalEntry CreateJournalEntry() => new JournalEntry(date, debits, credits);

            var exception = Record.Exception(CreateJournalEntry);

            Assert.Null(exception);
        }

        private class JournalEntryDoesNotThrowExceptionWithValidArguments_TestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[]
                {
                    DateTime.UtcNow,
                    new List<JournalEntryDebitLine>
                    {
                        new JournalEntryDebitLine(new Account("Account", NormalBalance.Debit), 1m)
                    },
                    new List<JournalEntryCreditLine>
                    {
                        new JournalEntryCreditLine(new Account("Another Account", NormalBalance.Credit), -1m)
                    }
                };
                yield return new object[]
                {
                    new DateTime(2020, 2, 15),
                    new List<JournalEntryDebitLine>
                    {
                        new JournalEntryDebitLine(new Account("Account", NormalBalance.Debit), 10_101m)
                    },
                    new List<JournalEntryCreditLine>
                    {
                        new JournalEntryCreditLine(new Account("Another Account", NormalBalance.Credit), -10_101m)
                    }
                };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Theory, ClassData(typeof(JournalEntryMembersValuesAreTheSameAsConstructorArguments_TestData))]
        public void JournalEntryMembersValuesAreTheSameAsConstructorArguments(DateTime date, 
            List<JournalEntryDebitLine> debits, 
            List<JournalEntryCreditLine> credits)
        {
            JournalEntry sut = new(date, debits, credits);
            
            var debitsNotInDebits = debits.Except(sut.Debits);
            var creditsNotInCredits = credits.Except(sut.Credits);
            
            Assert.True(sut.Date == date);
            Assert.True(debitsNotInDebits.Any() is false && creditsNotInCredits.Any() is false);
        }

        private class JournalEntryMembersValuesAreTheSameAsConstructorArguments_TestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[]
                {
                    DateTime.UtcNow,
                    new List<JournalEntryDebitLine>
                    {
                        new JournalEntryDebitLine(new Account("Account", NormalBalance.Debit), 1m)
                    },
                    new List<JournalEntryCreditLine>
                    {
                        new JournalEntryCreditLine(new Account("Another Account", NormalBalance.Credit), -1m)
                    }
                };
                yield return new object[]
                {
                    new DateTime(2020, 2, 15),
                    new List<JournalEntryDebitLine>
                    {
                        new JournalEntryDebitLine(new Account("Account", NormalBalance.Debit), 10_101m)
                    },
                    new List<JournalEntryCreditLine>
                    {
                        new JournalEntryCreditLine(new Account("Another Account", NormalBalance.Credit), -10_101m)
                    }
                };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    
}
